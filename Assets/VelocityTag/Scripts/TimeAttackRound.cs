// TimeAttackRound.cs
// Port of round.js: the single scoring authority. Runs the countdown -> playing
// -> results clock, applies zone base scores + route bonuses + combo multiplier,
// tracks the telemetry the results screen teaches from, persists best score, and
// resolves the medal ladder. Suit-charge logic is present but dormant until the
// hostile-fire pass adds a caller for RegisterPlayerHit().

using UnityEngine;
using ShooterCore;

namespace VelocityTag
{
    public class TimeAttackRound : MonoBehaviour
    {
        [SerializeField] private GameConfig _config;
        [SerializeField] private MatchStateMachine _matchState;

        // --- Live run state (read by HUD/results) ---
        public int Score { get; private set; }
        public int Combo { get; private set; }
        public float Timer { get; private set; }
        public int Tags { get; private set; }
        public int ShotsFired { get; private set; }
        public int HitsTaken { get; private set; }
        public int MaxCombo { get; private set; }
        public int HelmetTags { get; private set; }
        public int FlankTags { get; private set; }
        public int AirTags { get; private set; }
        public int LaunchTags { get; private set; }
        public int SuitCharges { get; private set; }
        public int BestScore { get; private set; }

        public float Accuracy => ShotsFired > 0 ? (float)Tags / ShotsFired : 0f;
        public Medal CurrentMedal => ComputeMedal(Score);

        private float _comboTimer;
        private float _countdownTimer;
        private int _lastCountdownSecond;

        private const string BestKey = "JLT_TIME_ATTACK_BEST_SCORE";

        private void Awake()
        {
            BestScore = PlayerPrefs.GetInt(BestKey, 0);
            SuitCharges = _config != null ? _config.playerMaxCharges : 3;
        }

        private void OnEnable()
        {
            GameEventBus.On<TargetTaggedEvent>(OnTargetTagged);
            GameEventBus.On<WeaponFiredEvent>(OnWeaponFired);
        }

        private void OnDisable()
        {
            GameEventBus.Off<TargetTaggedEvent>(OnTargetTagged);
            GameEventBus.Off<WeaponFiredEvent>(OnWeaponFired);
        }

        // Hook a menu button or debug key to this.
        public void StartTimeAttack()
        {
            Score = 0; Combo = 0; Tags = 0; ShotsFired = 0; HitsTaken = 0;
            MaxCombo = 0; HelmetTags = 0; FlankTags = 0; AirTags = 0; LaunchTags = 0;
            SuitCharges = _config != null ? _config.playerMaxCharges : 3;
            _comboTimer = 0f;

            _countdownTimer = _config != null ? _config.countdown : 3;
            _lastCountdownSecond = Mathf.CeilToInt(_countdownTimer);
            Timer = _config != null ? _config.roundTime : 120f;

            if (_matchState != null) _matchState.SetPhase(MatchPhase.Countdown);
        }

        private void OnWeaponFired(WeaponFiredEvent _)
        {
            if (_matchState != null && _matchState.IsPlaying) ShotsFired++;
        }

        private void OnTargetTagged(TargetTaggedEvent e)
        {
            if (_matchState != null && !_matchState.IsPlaying) return;

            int baseScore = e.Zone switch
            {
                ZoneType.Helmet => _config != null ? _config.helmetScore : 150,
                ZoneType.Flank  => _config != null ? _config.flankScore  : 250,
                _               => _config != null ? _config.chestScore  : 100,
            };

            int bonus = 0;
            string breakdown = ZoneName(e.Zone) + " +" + baseScore;

            if (e.Airborne)
            {
                int b = _config != null ? _config.airborneBonus : 50;
                bonus += b; breakdown += " AIR +" + b; AirTags++;
            }
            if (e.LaunchWindow)
            {
                int b = _config != null ? _config.launchBonus : 75;
                bonus += b; breakdown += " LAUNCH +" + b; LaunchTags++;
            }
            if (e.QuickDropWindow)
            {
                int b = _config != null ? _config.quickDropBonus : 75;
                bonus += b; breakdown += " DROP +" + b;
            }

            // Advance combo (caps at MAX_COMBO), refresh its window.
            Combo = Mathf.Min(Combo + 1, _config != null ? _config.maxCombo : 5);
            _comboTimer = _config != null ? _config.comboWindow : 3f;
            if (Combo > MaxCombo) MaxCombo = Combo;

            int awarded = (baseScore + bonus) * Mathf.Max(1, Combo);
            if (Combo > 1) breakdown += " x" + Combo;
            Score += awarded;

            Tags++;
            if (e.Zone == ZoneType.Helmet) HelmetTags++;
            if (e.Zone == ZoneType.Flank) FlankTags++;

            GameEventBus.Emit(new ScoreAwardedEvent
            {
                AwardedPoints = awarded,
                Combo = Combo,
                Zone = e.Zone,
                BreakdownText = breakdown,
                ImpactPoint = e.ImpactPoint
            });
        }

        // Dormant until hostile fire is added. Deducts penalty, breaks combo,
        // spends a suit charge; at zero, applies the integrity penalty and resets.
        public void RegisterPlayerHit()
        {
            if (_matchState != null && !_matchState.IsPlaying) return;

            HitsTaken++;
            Score = Mathf.Max(0, Score - (_config != null ? _config.playerHitPenalty : 250));
            Combo = 0;
            GameEventBus.Emit(new ComboDroppedEvent());

            SuitCharges--;
            if (SuitCharges <= 0)
            {
                Score = Mathf.Max(0, Score - (_config != null ? _config.zeroIntegrityPenalty : 1000));
                SuitCharges = _config != null ? _config.playerMaxCharges : 3;
            }

            GameEventBus.Emit(new SuitChargeChangedEvent
            {
                Charges = SuitCharges,
                MaxCharges = _config != null ? _config.playerMaxCharges : 3
            });
        }

        private void Update()
        {
            if (_matchState == null) return;
            float dt = Time.deltaTime;

            if (_matchState.Phase == MatchPhase.Countdown)
            {
                _countdownTimer -= dt;
                int sec = Mathf.CeilToInt(Mathf.Max(0f, _countdownTimer));
                if (sec != _lastCountdownSecond)
                {
                    _lastCountdownSecond = sec;
                    GameEventBus.Emit(new TimeAttackCountdownTickEvent { SecondsLeft = sec });
                }

                if (_countdownTimer <= 0f)
                {
                    _matchState.SetPhase(MatchPhase.Playing);
                    GameEventBus.Emit(new TimeAttackStartEvent());
                }
            }
            else if (_matchState.IsPlaying)
            {
                Timer -= dt;
                if (Timer <= 0f) { Timer = 0f; EndRun(); return; }

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

        private void EndRun()
        {
            if (Score > BestScore)
            {
                BestScore = Score;
                PlayerPrefs.SetInt(BestKey, BestScore);
                PlayerPrefs.Save();
            }

            _matchState.SetPhase(MatchPhase.Results);

            GameEventBus.Emit(new RunEndedEvent
            {
                FinalScore = Score,
                BestScore = BestScore,
                Medal = CurrentMedal.ToString(),
                Tags = Tags,
                Accuracy = Accuracy,
                MaxCombo = MaxCombo,
                AirTags = AirTags,
                HelmetTags = HelmetTags,
                FlankTags = FlankTags,
                LaunchTags = LaunchTags,
                HitsTaken = HitsTaken
            });
        }

        private Medal ComputeMedal(int score)
        {
            if (_config == null) return Medal.None;
            if (score >= _config.medalSRank) return Medal.SRank;
            if (score >= _config.medalPlatinum) return Medal.Platinum;
            if (score >= _config.medalGold) return Medal.Gold;
            if (score >= _config.medalSilver) return Medal.Silver;
            if (score >= _config.medalBronze) return Medal.Bronze;
            return Medal.None;
        }

        private static string ZoneName(ZoneType z) => z switch
        {
            ZoneType.Helmet => "HELMET",
            ZoneType.Flank => "FLANK",
            _ => "CHEST"
        };
    }
}
