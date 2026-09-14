// HostileFireTests.cs
// Covers the hostile-fire resolver in TagBlaster — the half that IS sourced.
// combat.js never had a shooter (nothing emits HOSTILE_FIRE_INCOMING), but its
// receiver is complete: the avatar centre sits 1.0 above the foot origin, a shot
// lands inside 1.2, and a landed shot emits PLAYER_HIT with damage 50.
//
// Worth testing carefully because this is the path that reaches the whole suit
// system — before it existed, PlayerHitEvent was emitted by nothing, so charges,
// both penalties and the recharge gates were all unreachable in play.

using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using ShooterCore;
using VelocityTag;

namespace VelocityTag.Tests
{
    public class HostileFireTests
    {
        private GameConfig _config;
        private GameObject _go;
        private MatchStateMachine _state;
        private TagBlaster _blaster;
        private int _hits;
        private int _lastDamage;

        [SetUp]
        public void SetUp()
        {
            GameEventBus.ClearAll();
            _config = ScriptableObject.CreateInstance<GameConfig>();

            _go = new GameObject("Player");
            _state = _go.AddComponent<MatchStateMachine>();
            _blaster = _go.AddComponent<TagBlaster>();
            SetPrivate(_blaster, "_config", _config);
            SetPrivate(_blaster, "_matchState", _state);
            Invoke(_blaster, "OnEnable");

            _hits = 0;
            _lastDamage = 0;
            GameEventBus.On<PlayerHitEvent>(e => { _hits++; _lastDamage = e.Damage; });

            _state.SetPhase(MatchPhase.Playing);
            // TagBlaster tracks the player through telemetry, as combat.js does.
            GameEventBus.Emit(new PlayerTelemetryEvent { Position = Vector3.zero, IsGrounded = true });
        }

        [TearDown]
        public void TearDown()
        {
            GameEventBus.ClearAll();
            Object.DestroyImmediate(_go);
            Object.DestroyImmediate(_config);
        }

        private static void SetPrivate(object target, string field, object value)
        {
            var info = target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(info, $"{target.GetType().Name} has no private field '{field}'");
            info.SetValue(target, value);
        }

        private static void Invoke(object target, string method) =>
            target.GetType()
                  .GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance)
                  ?.Invoke(target, null);

        /// The avatar centre for a player standing at the origin.
        private Vector3 Centre => Vector3.up * _config.hostileAimCentreHeight;

        private void Shoot(Vector3 aim) =>
            GameEventBus.Emit(new HostileFireEvent { Origin = new Vector3(0f, 1.1f, 10f), TargetAim = aim });

        [Test]
        public void ShotOnTheAvatarCentre_Hits()
        {
            Shoot(Centre);
            Assert.AreEqual(1, _hits);
            Assert.AreEqual(_config.hostileDamage, _lastDamage);
        }

        [Test]
        public void ShotJustInsideTheRadius_Hits()
        {
            Shoot(Centre + Vector3.right * (_config.hostileHitRadius * 0.9f));
            Assert.AreEqual(1, _hits);
        }

        [Test]
        public void ShotJustOutsideTheRadius_Misses()
        {
            Shoot(Centre + Vector3.right * (_config.hostileHitRadius * 1.1f));
            Assert.AreEqual(0, _hits);
        }

        [Test]
        public void ShotAtFootLevel_StillHits()
        {
            // The centre sits at 1.0 and the radius is 1.2, so ground level is
            // exactly 1.0 away and inside the window. Pinned deliberately: the
            // hit volume is a generous sphere around the torso, not a tight one,
            // and that is what makes incoming fire feel like area pressure.
            Shoot(Vector3.zero);
            Assert.AreEqual(1, _hits);
        }

        [Test]
        public void ShotWellBelowTheFeet_Misses()
        {
            Shoot(new Vector3(0f, -0.5f, 0f));   // 1.5 from the centre
            Assert.AreEqual(0, _hits);
        }

        [Test]
        public void ShotsAreIgnored_OutsidePlayingPhase()
        {
            _state.SetPhase(MatchPhase.MatchOver);
            Shoot(Centre);
            Assert.AreEqual(0, _hits);
        }

        [Test]
        public void ResolverTracksThePlayer_ThroughTelemetry()
        {
            GameEventBus.Emit(new PlayerTelemetryEvent { Position = new Vector3(20f, 0f, 0f), IsGrounded = false });

            Shoot(Centre);                                   // old position
            Assert.AreEqual(0, _hits, "stale position should not register");

            Shoot(new Vector3(20f, _config.hostileAimCentreHeight, 0f));
            Assert.AreEqual(1, _hits, "resolver should follow telemetry");
        }
    }
}
