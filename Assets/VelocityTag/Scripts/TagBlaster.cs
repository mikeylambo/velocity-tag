// TagBlaster.cs
// Port of combat.js. Enforces the 1-shot-per-cooldown clock (via CooldownWeapon),
// raycasts from the aim origin, and on a live TargetZone hit captures the player's
// movement context (airborne / launch / quick-drop) into a TargetTaggedEvent that
// TimeAttackRound scores. Also drives the reticle state:
//   Ready (green) / Cooldown (red-purple) / TargetLock (gold).

using UnityEngine;
using ShooterCore;

namespace VelocityTag
{
    public class TagBlaster : CooldownWeapon
    {
        [SerializeField] private GameConfig _config;
        [SerializeField] private Transform _aimOrigin;      // camera (desktop) / right controller (VR)
        [SerializeField] private JetpackLocomotion _player;
        [SerializeField] private CrossPlatformInput _input;
        [SerializeField] private LayerMask _targetMask = ~0; // set to the TargetZones layer

        public ReticleState Reticle { get; private set; } = ReticleState.Ready;
        private ReticleState _lastReticle = ReticleState.Ready;

        private void Awake()
        {
            if (_config != null) _cooldown = _config.playerFireCooldown;
            if (_aimOrigin == null && Camera.main != null) _aimOrigin = Camera.main.transform;
        }

        private void Update()
        {
            UpdateReticle();
            if (_input != null && _input.State.Firing) TryFire();
        }

        protected override void OnFire()
        {
            if (_aimOrigin == null) return;

            Vector3 origin = _aimOrigin.position;
            Vector3 dir = _aimOrigin.forward;
            float range = _config != null ? _config.laserRange : 60f;

            bool hit = Physics.Raycast(origin, dir, out RaycastHit info, range, _targetMask,
                                       QueryTriggerInteraction.Collide);
            Vector3 hitPoint = hit ? info.point : origin + dir * range;

            GameEventBus.Emit(new WeaponFiredEvent
            {
                Origin = origin,
                Direction = dir,
                Hit = hit,
                HitPoint = hitPoint
            });

            if (!hit) return;

            var zone = info.collider.GetComponent<TargetZone>();
            if (zone == null || zone.Parent == null || zone.Parent.IsGhosting) return;

            zone.Parent.RegisterHit();

            GameEventBus.Emit(new TargetTaggedEvent
            {
                Zone = zone.Zone,
                ImpactPoint = info.point,
                Airborne = _player != null && !_player.IsGrounded,
                LaunchWindow = _player != null && _player.LaunchActive,
                QuickDropWindow = _player != null && _player.QuickDropActive,
                Target = zone.Parent
            });
        }

        private void UpdateReticle()
        {
            ReticleState next;

            if (!IsReady)
            {
                next = ReticleState.Cooldown;
            }
            else
            {
                next = ReticleState.Ready;
                if (_aimOrigin != null)
                {
                    float range = _config != null ? _config.laserRange : 60f;
                    if (Physics.Raycast(_aimOrigin.position, _aimOrigin.forward, out RaycastHit info,
                                        range, _targetMask, QueryTriggerInteraction.Collide))
                    {
                        var zone = info.collider.GetComponent<TargetZone>();
                        if (zone != null && zone.Parent != null && !zone.Parent.IsGhosting)
                            next = ReticleState.TargetLock;
                    }
                }
            }

            Reticle = next;
            if (next != _lastReticle)
            {
                _lastReticle = next;
                GameEventBus.Emit(new ReticleStateChangedEvent { State = next });
            }
        }
    }
}
