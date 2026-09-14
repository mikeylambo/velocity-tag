// TimeAttackScoringTests.cs
// Covers TimeAttackRound, the single scoring authority ported from round.js.
// This is where score integrity has to be provable rather than spot-checked:
// zone values, route bonuses, combo multiplication and clamping, the combo
// window, suit-charge penalties, and the medal ladder.
//
// EditMode, not PlayMode, so the whole suite runs headless in a couple of
// seconds (-runTests -batchmode -testPlatform EditMode). The cost is that Unity
// does not fire Awake/OnEnable outside Play Mode, so the harness invokes them
// explicitly — see Boot(). That hand-cranking is a fair signal that the scoring
// core would be cleaner as a plain C# class with the MonoBehaviour as a shell;
// worth doing if this file starts fighting the lifecycle.

using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using ShooterCore;
using VelocityTag;

namespace VelocityTag.Tests
{
    public class TimeAttackScoringTests
    {
        private const string BestScoreKey = "JLT_TIME_ATTACK_CLASSIC_BEST";

        private GameConfig _config;
        private GameObject _matchGo;
        private GameObject _targetGo;
        private MatchStateMachine _state;
        private TimeAttackRound _round;
        private TargetDummy _target;

        [SetUp]
        public void SetUp()
        {
            GameEventBus.ClearAll();
            PlayerPrefs.DeleteKey(BestScoreKey);

            _config = ScriptableObject.CreateInstance<GameConfig>();   // defaults == config.js

            _matchGo = new GameObject("Match");
            _state = _matchGo.AddComponent<MatchStateMachine>();
            _round = _matchGo.AddComponent<TimeAttackRound>();
            SetPrivate(_round, "_config", _config);
            SetPrivate(_round, "_matchState", _state);

            _targetGo = new GameObject("Target");
            _target = _targetGo.AddComponent<TargetDummy>();

            Boot(_round);
        }

        [TearDown]
        public void TearDown()
        {
            GameEventBus.ClearAll();
            PlayerPrefs.DeleteKey(BestScoreKey);
            Object.DestroyImmediate(_matchGo);
            Object.DestroyImmediate(_targetGo);
            Object.DestroyImmediate(_config);
        }

        // --- helpers ---------------------------------------------------------

        private static void SetPrivate(object target, string field, object value)
        {
            var info = target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(info, $"{target.GetType().Name} has no private field '{field}'");
            info.SetValue(target, value);
        }

        /// Unity skips Awake/OnEnable in EditMode; call them so the round reads
        /// its best score and subscribes to the event bus.
        private static void Boot(MonoBehaviour behaviour)
        {
            foreach (var name in new[] { "Awake", "OnEnable" })
                behaviour.GetType()
                         .GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance)
                         ?.Invoke(behaviour, null);
        }

        /// StartTimeAttack enters Countdown; step past it to reach Playing.
        private void StartRun()
        {
            _round.StartTimeAttack();
            _round.Tick(_config.timeAttackCountdown + 0.01f);
            Assert.AreEqual(MatchPhase.Playing, _state.Phase, "countdown should hand off to Playing");
        }

        private void Tag(TagType zone, bool airborne = false, bool launch = false, bool quickDrop = false)
        {
            GameEventBus.Emit(new TargetHitReportEvent
            {
                Target = _target,
                Zone = zone,
                IsAirborne = airborne,
                IsLaunchBonus = launch,
                IsQuickDropBonus = quickDrop,
            });
        }

        // --- zone base values ------------------------------------------------

        [Test]
        public void ChestTag_ScoresBaseChestPoints()
        {
            StartRun();
            Tag(TagType.Chest);
            Assert.AreEqual(_config.chestPoints, _round.Score);
            Assert.AreEqual(1, _round.Tags);
        }

        [Test]
        public void HelmetTag_ScoresHelmetPointsAndCounts()
        {
            StartRun();
            Tag(TagType.Helmet);
            Assert.AreEqual(_config.helmetPoints, _round.Score);
            Assert.AreEqual(1, _round.HelmetTags);
        }

        [Test]
        public void FlankTag_ScoresFlankPointsAndCounts()
        {
            StartRun();
            Tag(TagType.FlankPack);
            Assert.AreEqual(_config.flankPackPoints, _round.Score);
            Assert.AreEqual(1, _round.FlankTags);
        }

        // --- route bonuses ----------------------------------------------------

        [Test]
        public void RouteBonuses_StackOntoTheBaseValue()
        {
            StartRun();
            Tag(TagType.Chest, airborne: true, launch: true, quickDrop: true);

            int expected = _config.chestPoints + _config.airborneBonus
                         + _config.launchBonus + _config.quickDropBonus;
            Assert.AreEqual(expected, _round.Score);
            Assert.AreEqual(1, _round.AirTags);
            Assert.AreEqual(1, _round.LaunchTags);
            Assert.AreEqual(1, _round.QuickDropTags);
        }

        // --- combo ------------------------------------------------------------

        [Test]
        public void Combo_MultipliesTheWholeBasketIncludingBonuses()
        {
            StartRun();
            Tag(TagType.Chest);                      // combo 1
            int afterFirst = _round.Score;

            Tag(TagType.Helmet, airborne: true);     // combo 2

            int second = (_config.helmetPoints + _config.airborneBonus) * 2;
            Assert.AreEqual(afterFirst + second, _round.Score);
            Assert.AreEqual(2, _round.Combo);
        }

        [Test]
        public void Combo_ClampsAtMaxCombo()
        {
            StartRun();
            for (int i = 0; i < _config.maxCombo + 3; i++) Tag(TagType.Chest);

            Assert.AreEqual(_config.maxCombo, _round.Combo);
            Assert.AreEqual(_config.maxCombo, _round.MaxCombo);
        }

        [Test]
        public void Combo_DropsOnceTheWindowExpires()
        {
            StartRun();
            Tag(TagType.Chest);
            Assert.AreEqual(1, _round.Combo);

            bool dropped = false;
            GameEventBus.On<ComboDroppedEvent>(_ => dropped = true);

            _round.Tick(_config.comboWindow + 0.01f);

            Assert.AreEqual(0, _round.Combo, "combo should reset after the window");
            Assert.IsTrue(dropped, "combo expiry should announce itself");
        }

        [Test]
        public void Combo_SurvivesInsideTheWindow()
        {
            StartRun();
            Tag(TagType.Chest);
            _round.Tick(_config.comboWindow * 0.5f);
            Assert.AreEqual(1, _round.Combo);
        }

        // --- suit integrity ---------------------------------------------------

        [Test]
        public void PlayerHit_CostsPointsAChargeAndTheCombo()
        {
            StartRun();
            for (int i = 0; i < 3; i++) Tag(TagType.Chest);
            int before = _round.Score;
            int charges = _round.SuitCharges;

            GameEventBus.Emit(new PlayerHitEvent());

            Assert.AreEqual(before - _config.playerHitPenalty, _round.Score);
            Assert.AreEqual(charges - 1, _round.SuitCharges);
            Assert.AreEqual(0, _round.Combo);
            Assert.AreEqual(1, _round.HitsTaken);
        }

        [Test]
        public void DrainingEveryCharge_AppliesTheIntegrityPenaltyAndRecalibrates()
        {
            StartRun();
            for (int i = 0; i < 20; i++) Tag(TagType.FlankPack);   // bank enough to see the penalty
            int before = _round.Score;

            for (int i = 0; i < _config.playerMaxCharges; i++)
                GameEventBus.Emit(new PlayerHitEvent());

            int expected = before
                         - _config.playerHitPenalty * _config.playerMaxCharges
                         - _config.zeroIntegrityPenalty;

            Assert.AreEqual(expected, _round.Score);
            Assert.AreEqual(_config.playerMaxCharges, _round.SuitCharges, "charges should recalibrate to full");
        }

        [Test]
        public void Score_NeverGoesNegative()
        {
            StartRun();
            for (int i = 0; i < 10; i++) GameEventBus.Emit(new PlayerHitEvent());
            Assert.GreaterOrEqual(_round.Score, 0);
        }

        [Test]
        public void SuitSync_RestoresOneChargeButNeverOverfills()
        {
            StartRun();
            GameEventBus.Emit(new PlayerHitEvent());
            Assert.AreEqual(_config.playerMaxCharges - 1, _round.SuitCharges);

            GameEventBus.Emit(new SuitSyncTickEvent());
            Assert.AreEqual(_config.playerMaxCharges, _round.SuitCharges);

            GameEventBus.Emit(new SuitSyncTickEvent());
            Assert.AreEqual(_config.playerMaxCharges, _round.SuitCharges, "sync should cap at max");
        }

        // --- ghosting ---------------------------------------------------------

        [Test]
        public void GhostedTarget_ScoresNothing()
        {
            StartRun();
            SetPrivate(_target, "<IsGhosted>k__BackingField", true);

            Tag(TagType.Helmet);

            Assert.AreEqual(0, _round.Score);
            Assert.AreEqual(0, _round.Tags);
        }

        // --- accuracy and medals ---------------------------------------------

        [Test]
        public void Accuracy_IsTagsOverShotsFired()
        {
            StartRun();
            for (int i = 0; i < 4; i++) GameEventBus.Emit(new WeaponFiredEvent());
            Tag(TagType.Chest);

            Assert.AreEqual(4, _round.ShotsFired);
            Assert.AreEqual(0.25f, _round.Accuracy, 0.0001f);
        }

        [Test]
        public void Accuracy_IsZeroBeforeAnyShot()
        {
            StartRun();
            Assert.AreEqual(0f, _round.Accuracy);
        }

        [TestCase(0,     "—")]
        [TestCase(2999,  "—")]
        [TestCase(3000,  "BRONZE")]
        [TestCase(6000,  "SILVER")]
        [TestCase(10000, "GOLD")]
        [TestCase(14000, "PLATINUM")]
        [TestCase(18000, "S-RANK")]
        [TestCase(99999, "S-RANK")]
        public void Medal_MatchesTheLadderThresholds(int score, string expected)
        {
            SetPrivate(_round, "<Score>k__BackingField", score);
            Assert.AreEqual(expected, _round.Medal);
        }

        // --- run lifecycle ----------------------------------------------------

        [Test]
        public void RunEnds_WhenTheClockExpires()
        {
            StartRun();
            Tag(TagType.Chest);

            _round.Tick(_config.timeAttackDuration + 1f);

            Assert.AreEqual(MatchPhase.MatchOver, _state.Phase);
        }

        [Test]
        public void BestScore_PersistsAcrossRuns()
        {
            StartRun();
            Tag(TagType.FlankPack);
            int scored = _round.Score;

            _round.Tick(_config.timeAttackDuration + 1f);   // ends the run

            Assert.AreEqual(scored, _round.BestScore);
            Assert.AreEqual(scored, PlayerPrefs.GetInt(BestScoreKey, 0));
        }

        [Test]
        public void ScoringIsIgnored_OutsidePlayingPhase()
        {
            // Still in Menu: no StartRun() call.
            Tag(TagType.FlankPack);
            Assert.AreEqual(0, _round.Score);
        }
    }
}
