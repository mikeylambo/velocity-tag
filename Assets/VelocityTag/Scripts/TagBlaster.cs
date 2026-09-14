// TagBlaster.cs
// Port of src/combat.js CombatManager: strict shot clock (playerFireCooldown),
// raycast from aim origin against TargetZone colliders, contextual bonus
// windows (launch <=2.0s since pad, quick-drop landing arms a 1.5s window if
// the landing came within 2.0s of the drop trigger), airborne flag from
// telemetry, and reticle ready/cooldown state broadcast for the HUD.
//
// Hostile fire handling (HOSTILE_FIRE_INCOMING -> proximity check -> PLAYER_HIT)
// is intentionally left for the hostile-fire pass — the JS build sources it
// from targets, which we're keeping as passive dummies for the first Unity build.

using UnityEngine;
using ShooterCore;

namespace VelocityTag
{
    public struct ReticleStateEvent { public bool IsReady; }

    public class TagBlaster : MonoBehaviour
    {
        [SerializeField] private GameConfig _config;
        [SerializeField] private Transform _aimOrigin;          // camera / controller tip
        [SerializeField] private LayerMask _targetZoneMask;     // layer holding TargetZone colliders
        [SerializeField] private MatchStateMachine _matchState;

        private float _cooldown;
        private bool _isAirborne;
        private Vector3 _playerPos;

        // Contextual route/drop bonus timers (seconds, Time.time domain)
        private float _lastLaunchTime = -999f;
        private float _lastQuickDropTime = -999f;
        private float _lastQuickDropLandTime = -999f;
        private bool _wasAirborneLastFrame;

        private const float LaunchBonusWindow = 2.0f;
        private const float QuickDropLandGrace = 2.0f;   // land within 2s of triggering drop
        private const float QuickDropBonusWindow = 1.5f; // then 1.5s to convert the tag

        public bool IsReady => _cooldown <= 0f && _matchState != null && _matchState.IsPlaying;

        private void OnEnable()
        {
            GameEventBus.On<QuickDropEvent>(OnQuickDrop);
            GameEventBus.On<PlayerTelemetryEvent>(OnTelemetry);
            GameEventBus.On<LaunchPadEngagedEvent>(OnLaunchPad);
            GameEventBus.On<TimeAttackStartEvent>(OnRoundStart);
        }

        private void OnDisable()
        {
            GameEventBus.Off<QuickDropEvent>(OnQuickDrop);
            GameEventBus.Off<PlayerTelemetryEvent>(OnTelemetry);
            GameEventBus.Off<LaunchPadEngagedEvent>(OnLaunchPad);
            GameEventBus.Off<TimeAttackStartEvent>(OnRoundStart);
        }

        private void OnRoundStart(TimeAttackStartEvent _)
        {
            _cooldown = 0f;
            _lastLaunchTime = _lastQuickDropTime = _lastQuickDropLandTime = -999f;
        }

        private void OnLaunchPad(LaunchPadEngagedEvent _) => _lastLaunchTime = Time.time;

        private void OnQuickDrop(QuickDropEvent _)
        {
            if (_matchState != null && _matchState.IsPlaying && _isAirborne)
                _lastQuickDropTime = Time.time;
        }

        private void OnTelemetry(PlayerTelemetryEvent e)
        {
            _isAirborne = !e.IsGrounded;
            _playerPos = e.Position;

            // Detect the exact landing frame after an airborne quick drop
            if (_wasAirborneLastFrame && e.IsGrounded)
            {
                if (Time.time - _lastQuickDropTime <= QuickDropLandGrace)
                    _lastQuickDropLandTime = Time.time;
            }
            _wasAirborneLastFrame = _isAirborne;
        }

        /// Wire to input (mouse / VR trigger). Mirrors executeAimFire().
        public void TryFire()
        {
            if (!IsReady) return;
            _cooldown = _config.playerFireCooldown;

            Ray ray = new Ray(_aimOrigin.position, _aimOrigin.forward);
            GameEventBus.Emit(new WeaponFiredEvent
            {
                WeaponId = "tag-blaster",
                Origin = ray.origin,
                Direction = ray.direction
            });

            if (!Physics.Raycast(ray, out RaycastHit hit, _config.laserRange, _targetZoneMask))
                return;   // FX layer draws the miss beam off WeaponFiredEvent

            var zone = hit.collider.GetComponent<TargetZone>();
            if (zone == null || zone.parent == null || zone.parent.IsGhosted)
                return;

            float now = Time.time;
            GameEventBus.Emit(new TargetHitReportEvent
            {
                Target = zone.parent,
                Zone = zone.zone,
                IsAirborne = _isAirborne,
                IsLaunchBonus = (now - _lastLaunchTime) <= LaunchBonusWindow,
                IsQuickDropBonus = (now - _lastQuickDropLandTime) <= QuickDropBonusWindow,
                ImpactPoint = hit.point
            });
        }

        private void Update()
        {
            if (_cooldown > 0f) _cooldown -= Time.deltaTime;
            GameEventBus.Emit(new ReticleStateEvent { IsReady = IsReady });
        }
    }
}
