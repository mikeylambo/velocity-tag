// JetpackLocomotion.cs
// Line-faithful port of src/player.js PlayerAvatar physics:
// planar move (ground/air speeds), yaw turn, vertical thrust w/ cap, gravity,
// fuel economy (-25/s thrust, -20/dash, +40/s grounded regen, +12/s air regen),
// 0.18s dash that overrides planar velocity, quick-drop slam (-dashSpeed*1.5),
// cylindrical arena clamp at radius-1, grounded resolution against floor height.
//
// Deliberately kinematic (transform-driven), matching the JS build's feel exactly.
// No Rigidbody/CharacterController — collisions with arena obstacles are resolved
// by ArenaBounds, same as arena.resolveCollisions() did.

using UnityEngine;
using ShooterCore;
using ShooterCore.Input;

namespace VelocityTag
{
    public class JetpackLocomotion : MonoBehaviour
    {
        [SerializeField] private GameConfig _config;
        [SerializeField] private CrossPlatformInput _input;
        [SerializeField] private ArenaBounds _arena;
        [SerializeField] private MatchStateMachine _matchState;

        public float Fuel { get; private set; } = 100f;
        public bool IsGrounded { get; private set; } = true;
        public float FacingAngle { get; private set; }   // radians, matches JS facingAngle
        public Vector3 Velocity => _velocity;

        private Vector3 _velocity;
        private Vector3 _dashVelocity;
        private float _dashTimer;

        private const float DashFuelCost = 20f;
        private const float ThrustFuelPerSecond = 25f;
        private const float GroundRegenPerSecond = 40f;
        private const float AirRegenPerSecond = 12f;
        private const float DashDuration = 0.18f;

        private void OnEnable()
        {
            GameEventBus.On<DashEvent>(OnDash);
            GameEventBus.On<QuickDropEvent>(OnQuickDrop);
            GameEventBus.On<RoundPhaseChangedEvent>(OnPhaseChanged);
        }

        private void OnDisable()
        {
            GameEventBus.Off<DashEvent>(OnDash);
            GameEventBus.Off<QuickDropEvent>(OnQuickDrop);
            GameEventBus.Off<RoundPhaseChangedEvent>(OnPhaseChanged);
        }

        private void OnPhaseChanged(RoundPhaseChangedEvent e)
        {
            if (e.Phase == MatchPhase.Countdown) ResetToSpawn();
        }

        private void ResetToSpawn()
        {
            transform.position = new Vector3(0f, 0f, 12f);   // ROUND_START spawn from player.js
            _velocity = Vector3.zero;
            Fuel = 100f;
        }

        private void OnDash(DashEvent _)
        {
            if (Fuel < DashFuelCost || _dashTimer > 0f) return;

            var s = _input.State;
            Vector3 forward = FacingForward();
            Vector3 right = FacingRight();

            Vector3 dir = Vector3.zero;
            // JS: dash toward stick direction if held, else straight forward
            if (Mathf.Abs(s.Strafe.x) > 0.1f || Mathf.Abs(s.Strafe.y) > 0.1f)
            {
                dir += forward * s.Strafe.y;
                dir += right * s.Strafe.x;
            }
            else dir = forward;

            if (dir.sqrMagnitude > 0f) dir.Normalize();
            _dashVelocity = dir * _config.dashSpeed;
            _dashTimer = DashDuration;
            Fuel -= DashFuelCost;
        }

        private void OnQuickDrop(QuickDropEvent _)
        {
            if (!IsGrounded)
                _velocity.y = -_config.dashSpeed * 1.5f;
        }

        /// Called by LaunchPad triggers — hard vertical velocity set, like the JS pads.
        public void ApplyLaunch(float upwardVelocity)
        {
            _velocity.y = Mathf.Max(_velocity.y, upwardVelocity);
        }

        private Vector3 FacingForward() =>
            Quaternion.AngleAxis(FacingAngle * Mathf.Rad2Deg, Vector3.up) * Vector3.forward;

        private Vector3 FacingRight() =>
            Quaternion.AngleAxis(FacingAngle * Mathf.Rad2Deg, Vector3.up) * Vector3.right;

        private void Update()
        {
            if (_matchState != null && !_matchState.IsPlaying) return;

            float dt = Time.deltaTime;
            var s = _input.State;

            // --- Turn (JS: facingAngle -= turn * TURN_SPEED * dt) ---
            // Reads the dedicated Turn axis. The port previously read Look.x,
            // which carries mouse PITCH on desktop and is never written at all
            // by the XR path — so the avatar could not turn in VR.
            if (Mathf.Abs(s.Turn) > 0.05f)
                FacingAngle += s.Turn * _config.turnSpeed * dt;   // sign flip: Unity forward is +Z
            transform.rotation = Quaternion.AngleAxis(FacingAngle * Mathf.Rad2Deg, Vector3.up);

            Vector3 forward = FacingForward();
            Vector3 right = FacingRight();

            Vector3 moveDir = forward * s.Strafe.y + right * s.Strafe.x;
            if (moveDir.sqrMagnitude > 1f) moveDir.Normalize();

            float floorY = _arena != null ? _arena.GetFloorY(transform.position) : 0f;
            bool thrusting = s.Thrust > 0.1f && Fuel > 0f;

            // --- Vertical ---
            if (thrusting)
            {
                _velocity.y += s.Thrust * _config.verticalThrust * dt;
                _velocity.y = Mathf.Min(_velocity.y, _config.verticalMax);
                Fuel -= ThrustFuelPerSecond * dt;
            }
            else
            {
                _velocity.y -= _config.gravity * dt;
            }

            // --- Planar: dash overrides stick, exactly as in JS ---
            if (_dashTimer > 0f)
            {
                _dashTimer -= dt;
                _velocity.x = _dashVelocity.x;
                _velocity.z = _dashVelocity.z;
            }
            else
            {
                float speed = IsGrounded ? _config.moveSpeed : _config.airMoveSpeed;
                _velocity.x = moveDir.x * speed;
                _velocity.z = moveDir.z * speed;
            }

            transform.position += _velocity * dt;

            // --- Ground resolve ---
            IsGrounded = false;
            if (transform.position.y <= floorY)
            {
                var p = transform.position; p.y = floorY; transform.position = p;
                if (_velocity.y < 0f) _velocity.y = 0f;
                IsGrounded = true;
            }

            // --- Fuel regen ---
            if (!thrusting)
                Fuel = Mathf.Min(100f, Fuel + (IsGrounded ? GroundRegenPerSecond : AirRegenPerSecond) * dt);

            // --- Cylindrical arena clamp (radius - 1.0) ---
            Vector2 planar = new Vector2(transform.position.x, transform.position.z);
            float maxR = _config.arenaRadius - 1.0f;
            if (planar.magnitude > maxR)
            {
                planar = planar.normalized * maxR;
                transform.position = new Vector3(planar.x, transform.position.y, planar.y);
            }

            if (_arena != null) _arena.ResolveCollisions(transform, 0.4f);

            GameEventBus.Emit(new PlayerTelemetryEvent
            {
                Position = transform.position,
                Fuel = Fuel,
                IsGrounded = IsGrounded
            });
        }
    }
}
