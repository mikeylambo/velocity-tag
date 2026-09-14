// TimeAttackRound.cs
// Port of src/round.js — the single scoring authority. Phases via ShooterCore's
// MatchStateMachine (Menu -> MapSelect -> Countdown -> Playing -> MatchOver=results).
// Scoring: base 100/150/250 by zone, +50 air, +75 launch, +75 quick drop,
// combo clamped 1..maxCombo multiplies the whole basket, 3s combo window.
// Suit integrity: hit = -250 pts, combo break, -1 charge; 0 charges = -1000 and reset.
// Recharge gates emit SuitSyncTickEvent to restore charges.
// Best score persists via PlayerPrefs (was localStorage JLT_TIME_ATTACK_CLASSIC_BEST).

using UnityEngine;
using ShooterCore;

namespace VelocityTag
{
    public class TimeAttackRound : MonoBehaviour
    {
        [SerializeField] private GameConfig _config;
        [SerializeField] private MatchStateMachine _matchState;

        private const string BestScoreKey = "JLT_TIME_ATTACK_CLASSIC_BEST";

        // --- Run state (mirrors round.state) ---
        public int Score { get; private set; }
        public int BestScore { get; private set; }
        public int Tags { get; private set; }
        public int ShotsFired { get; private set; }
        public int HitsTaken { get; private set; }
        public int Combo { get; private set; }
        public int MaxCombo { get; private set; }
        public int SuitCharges { get; private set; }
        public int HelmetTags { get; private set; }
        public int FlankTags { get; private set; }
        public int AirTags { get; private set; }
        public int LaunchTags { get; private set; }
        public int QuickDropTags { get; private set; }
        public float Timer { get; private set; }
        public float Accuracy => ShotsFired > 0 ? (float)Tags / ShotsFired : 0f;

        private float _comboTimer;

        public string Medal
        {
            get
            {
                if (Score >= _config.sRankThreshold) return "S-RANK";
                if (Score >= _config.platinumThreshold) return "PLATINUM";
                if (Score >= _config.goldThreshold) return "GOLD";
                if (Score >= _config.silverThreshold) return "SILVER";
                if (Score >= _config.bronzeThreshold) return "BRONZE";
                return "—";
            }
        }

        private void Awake() => BestScore = PlayerPrefs.GetInt(BestScoreKey, 0);

        private void OnEnable()
        {
            GameEventBus.On<TargetHitReportEvent>(OnHitReport);
            GameEventBus.On<WeaponFiredEvent>(OnWeaponFired);
            GameEventBus.On<PlayerHitEvent>(OnPlayerHit);
            GameEventBus.On<SuitSyncTickEvent>(OnSuitSync);
        }

        private void OnDisable()
        {
            GameEventBus.Off<TargetHitReportEvent>(OnHitReport);
            GameEventBus.Off<WeaponFiredEvent>(OnWeaponFired);
            GameEventBus.Off<PlayerHitEvent>(OnPlayerHit);
            GameEventBus.Off<SuitSyncTickEvent>(OnSuitSync);
        }

        /// Call from menu/map-select UI to begin a run.
        public void StartTimeAttack()
        {
            Score = Tags = ShotsFired = HitsTaken = 0;
            Combo = MaxCombo = 0;
            HelmetTags = FlankTags = AirTags = LaunchTags = QuickDropTags = 0;
            _comboTimer = 0f;
            SuitCharges = _config.playerMaxCharges;
            Timer = _config.timeAttackCountdown;

            _matchState.SetPhase(MatchPhase.Countdown);
            GameEventBus.Emit(new TimeAttackStartEvent());
        }

        private void EndRun()
        {
            Timer = 0f;
            if (Score > BestScore)
            {
                BestScore = Score;
                PlayerPrefs.SetInt(BestScoreKey, BestScore);
                PlayerPrefs.Save();
            }
            _matchState.SetPhase(MatchPhase.MatchOver);
            GameEventBus.Emit(new TimeAttackEndEvent());
        }

        private void OnWeaponFired(WeaponFiredEvent _)
        {
            if (_matchState.IsPlaying) ShotsFired++;
        }

        private void OnSuitSync(SuitSyncTickEvent _)
        {
            if (_matchState.IsPlaying && SuitCharges < _config.playerMaxCharges)
                SuitCharges++;
        }

        private void OnPlayerHit(PlayerHitEvent _)
        {
            if (!_matchState.IsPlaying) return;

            HitsTaken++;
            Score = Mathf.Max(0, Score - _config.playerHitPenalty);
            Combo = 0;
            _comboTimer = 0f;
            SuitCharges--;
            GameEventBus.Emit(new ComboDroppedEvent());

            if (SuitCharges <= 0)
            {
                Score = Mathf.Max(0, Score - _config.zeroIntegrityPenalty);
                SuitCharges = _config.playerMaxCharges;   // recalibration reset
            }
        }

        private void OnHitReport(TargetHitReportEvent e)
        {
            if (!_matchState.IsPlaying || e.Target == null || e.Target.IsGhosted) return;

            int basePoints;
            string breakdown;
            switch (e.Zone)
            {
                case TagType.Helmet:
                    basePoints = _config.helmetPoints;    HelmetTags++;
                    breakdown = $"HELMET +{_config.helmetPoints}";    break;
                case TagType.FlankPack:
                    basePoints = _config.flankPackPoints; FlankTags++;
                    breakdown = $"FLANK +{_config.flankPackPoints}";  break;
                default:
                    basePoints = _config.chestPoints;
                    breakdown = $"CHEST +{_config.chestPoints}";      break;
            }

            if (e.IsAirborne)
            {
                basePoints += _config.airborneBonus; AirTags++;
                breakdown += $" | AIR +{_config.airborneBonus}";
            }
            if (e.IsLaunchBonus)
            {
                basePoints += _config.launchBonus; LaunchTags++;
                breakdown += $" | LAUNCH +{_config.launchBonus}";
            }
            if (e.IsQuickDropBonus)
            {
                basePoints += _config.quickDropBonus; QuickDropTags++;
                breakdown += $" | QUICK DROP +{_config.quickDropBonus}";
            }

            Combo = Mathf.Clamp(Combo + 1, 1, _config.maxCombo);
            _comboTimer = _config.comboWindow;
            if (Combo > MaxCombo) MaxCombo = Combo;

            int awarded = basePoints * Combo;
            if (Combo > 1) breakdown += $" | COMBO x{Combo}";

            Score += awarded;
            Tags++;

            GameEventBus.Emit(new TargetHitConfirmedEvent { Target = e.Target });
            GameEventBus.Emit(new ScoreAwardedEvent
            {
                BasePoints = basePoints,
                AwardedPoints = awarded,
                Combo = Combo,
                Zone = e.Zone,
                BreakdownText = breakdown,
                ImpactPoint = e.ImpactPoint
            });
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            if (_matchState.Phase == MatchPhase.Countdown)
            {
                Timer -= dt;
                if (Timer <= 0f)
                {
                    Timer = _config.timeAttackDuration;
                    _matchState.SetPhase(MatchPhase.Playing);
                }
            }
            else if (_matchState.IsPlaying)
            {
                Timer -= dt;
                if (Timer <= 0f) { EndRun(); return; }

                if (_comboTimer > 0f)
                {
                    _comboTimer -= dt;
                    if (_comboTimer <= 0f && Combo > 0)
                    {
                        Combo = 0;
                        GameEventBus.Emit(new ComboDroppedEvent());
                    }
                }
            }
        }
    }
}
