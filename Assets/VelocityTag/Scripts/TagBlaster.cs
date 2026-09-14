// TagBlaster.cs
// Port of src/combat.js CombatManager: strict shot clock (playerFireCooldown),
// raycast from aim origin against TargetZone colliders, contextual bonus
// windows (launch <=2.0s since pad, quick-drop landing arms a 1.5s window if
// the landing came within 2.0s of the drop trigger), airborne flag from
// telemetry, and reticle ready/cooldown state broadcast for the HUD.
//
// Hostile fire resolution lives here because combat.js owns it: the incoming
// shot is a claim, and this is where it becomes a hit. The JS build never wrote
// the shooter (nothing ever emits HOSTILE_FIRE_INCOMING), but its receiver is
// complete and is ported exactly — avatar centre 1.0 above the foot origin,
// hit inside 1.2, PLAYER_HIT damage 50.

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

        private float LaunchBonusWindow => _config.launchBonusWindow;
        private float QuickDropLandGrace => _config.quickDropLandGrace;     // land within 2s of the drop
        private float QuickDropBonusWindow => _config.quickDropBonusWindow; // then 1.5s to convert the tag

        public bool IsReady => _cooldown <= 0f && _matchState != null && _matchState.IsPlaying;

        /// camera.js getAimRay() prefers the right controller and falls back to the
        /// camera. CameraModeSwitch makes that choice once, on device connect.
        public void SetAimOrigin(Transform origin)
        {
            if (origin != null) _aimOrigin = origin;
        }

        private void OnEnable()
        {
            GameEventBus.On<QuickDropEvent>(OnQuickDrop);
            GameEventBus.On<PlayerTelemetryEvent>(OnTelemetry);
            GameEventBus.On<LaunchPadEngagedEvent>(OnLaunchPad);
            GameEventBus.On<TimeAttackStartEvent>(OnRoundStart);
            GameEventBus.On<HostileFireEvent>(OnHostileFire);
        }

        private void OnDisable()
        {
            GameEventBus.Off<QuickDropEvent>(OnQuickDrop);
            GameEventBus.Off<PlayerTelemetryEvent>(OnTelemetry);
            GameEventBus.Off<LaunchPadEngagedEvent>(OnLaunchPad);
            GameEventBus.Off<TimeAttackStartEvent>(OnRoundStart);
            GameEventBus.Off<HostileFireEvent>(OnHostileFire);
        }

        private void OnRoundStart(TimeAttackStartEvent _)
        {
            _cooldown = 0f;
            _lastLaunchTime = _lastQuickDropTime = _lastQuickDropLandTime = -999f;
        }

        private void OnLaunchPad(LaunchPadEngagedEvent _) => _lastLaunchTime = Time.time;

        /// combat.js: the shot is already drawn; this only decides whether it connects.
        private void OnHostileFire(HostileFireEvent e)
        {
            if (_matchState == null || !_matchState.IsPlaying) return;

            Vector3 avatarCentre = _playerPos + Vector3.up * _config.hostileAimCentreHeight;
            if (Vector3.Distance(e.TargetAim, avatarCentre) >= _config.hostileHitRadius) return;

            GameEventBus.Emit(new PlayerHitEvent
            {
                Damage = _config.hostileDamage,
                ImpactPoint = e.TargetAim,
            });
        }

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
