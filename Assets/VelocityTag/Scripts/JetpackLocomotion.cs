// JetpackLocomotion.cs
// Port of player.js physics: planar move (grounded vs air speed), vertical
// jetpack thrust clamped to VERTICAL_MAX, gravity, smooth turn, grip dash, and
// right-stick quick drop. Also tracks the movement context TagBlaster reads for
// route bonuses (airborne / launch window / quick-drop window).
//
// Fuel is now implemented: the earlier note said config.js carried no fuel
// tuning, which is true of its config.js but not of player.js, which hardcodes
// drain 25/s, regen 40 grounded / 12 airborne, and 20 per dash. Those are now
// in GameConfig with their provenance recorded.

using UnityEngine;
using ShooterCore;

namespace VelocityTag
{
    public class JetpackLocomotion : MonoBehaviour
    {
        [SerializeField] private GameConfig _config;
        [SerializeField] private ArenaBounds _arena;
        [SerializeField] private CrossPlatformInput _input;

        private Vector3 _velocity;
        private Vector3 _dashVelocity;
        private float _dashTimer;
        private float _lastLaunchTime = -999f;
        private float _lastQuickDropLand = -999f;
        private bool _quickDropping;

        public bool IsGrounded { get; private set; }
        public float Fuel { get; private set; } = 100f;

        public bool LaunchActive =>
            _config != null && Time.time - _lastLaunchTime <= _config.launchBonusWindow;

        public bool QuickDropActive =>
            _config != null && Time.time - _lastQuickDropLand <= _config.quickDropBonusWindow;

        private void OnEnable()
        {
            GameEventBus.On<DashEvent>(OnDash);
            GameEventBus.On<QuickDropEvent>(OnQuickDrop);
        }

        private void OnDisable()
        {
            GameEventBus.Off<DashEvent>(OnDash);
            GameEventBus.Off<QuickDropEvent>(OnQuickDrop);
        }

        private void Update()
        {
            if (_config == null) return;
            float dt = Time.deltaTime;
            var state = _input != null ? _input.State : default;

            // Smooth turn (yaw) from Look.x. TURN_SPEED is radians/sec.
            float yawDeg = state.Look.x * _config.turnSpeed * Mathf.Rad2Deg * dt;
            transform.Rotate(0f, yawDeg, 0f, Space.World);

            // Planar movement, local to facing. Speed differs on ground vs air.
            // A dash overrides the stick for DASH_DURATION — without that the next
            // frame's planar assignment overwrote it and the dash did nothing.
            if (_dashTimer > 0f)
            {
                _dashTimer -= dt;
                _velocity.x = _dashVelocity.x;
                _velocity.z = _dashVelocity.z;
            }
            else
            {
                float planarSpeed = IsGrounded ? _config.moveSpeed : _config.airMoveSpeed;
                Vector3 planar = (transform.right * state.Strafe.x + transform.forward * state.Strafe.y) * planarSpeed;
                _velocity.x = planar.x;
                _velocity.z = planar.z;
            }

            // Vertical, matching player.js exactly: thrust and gravity are mutually
            // exclusive branches. While jetting, thrust accelerates with NO gravity
            // that frame (climbs to VERTICAL_MAX); thrust-down descends at
            // DESCEND_SPEED; gravity applies only when not thrusting. The previous
            // port applied thrust and gravity together, which made 15 vs 16 a net
            // -1 and grounded the jetpack entirely.
            bool jetting = state.Thrust > 0.05f && Fuel > 0f;
            if (jetting)
            {
                _velocity.y += _config.verticalThrust * state.Thrust * dt;
                _velocity.y = Mathf.Min(_velocity.y, _config.verticalMax);
                Fuel = Mathf.Max(0f, Fuel - _config.fuelDrainThrust * dt);
            }
            else if (state.Thrust < -0.05f)
            {
                _velocity.y = Mathf.Max(_velocity.y - _config.descendSpeed * dt, -_config.verticalMax);
            }
            else
            {
                _velocity.y -= _config.gravity * dt;
            }

            // Integrate then clamp to floor.
            Vector3 next = transform.position + _velocity * dt;
            float floorY = _arena != null ? _arena.FloorY(next) : 0f;

            if (next.y <= floorY)
            {
                if (_quickDropping) { _lastQuickDropLand = Time.time; _quickDropping = false; }
                next.y = floorY;
                if (_velocity.y < 0f) _velocity.y = 0f;
                IsGrounded = true;
            }
            else
            {
                IsGrounded = false;
            }

            transform.position = next;

            // player.js: regen only while not jetting, faster on the ground.
            if (!jetting)
            {
                float regen = IsGrounded ? _config.fuelRegenGround : _config.fuelRegenAir;
                Fuel = Mathf.Min(_config.fuelMax, Fuel + regen * dt);
            }

            // player.js clamps to a cylinder at ARENA_RADIUS - 1. Without this the
            // player can simply fly out of the arena.
            Vector2 flat = new Vector2(transform.position.x, transform.position.z);
            float maxR = _config.arenaRadius - 1f;
            if (flat.magnitude > maxR)
            {
                flat = flat.normalized * maxR;
                transform.position = new Vector3(flat.x, transform.position.y, flat.y);
            }

            if (_arena != null)
            {
                _arena.ResolveCeiling(transform, ref _velocity);
                _arena.ResolveCollisions(transform, _config.playerRadius);
            }
        }

        private void OnDash(DashEvent _)
        {
            // player.js: costs fuel, and refuses while a dash is already running.
            if (_config == null || Fuel < _config.dashCost || _dashTimer > 0f) return;

            var state = _input != null ? _input.State : default;
            Vector3 dir = transform.right * state.Strafe.x + transform.forward * state.Strafe.y;
            if (dir.sqrMagnitude < 0.01f) dir = transform.forward;
            dir.y = 0f;
            dir.Normalize();

            _dashVelocity = dir * _config.dashSpeed;
            _dashTimer = _config.dashDuration;
            Fuel -= _config.dashCost;
        }

        private void OnQuickDrop(QuickDropEvent _)
        {
            if (IsGrounded) return;
            // 2.0 Polish player.js assigns outright: velocity.y = -(DASH_SPEED) * 1.5.
            // golden_core_v02 clamps with min() instead; the target build does not.
            _velocity.y = -_config.quickDropSpeed;
            _quickDropping = true;
        }

        // Called by LaunchPad triggers. arena.js assigns playerAvatar.velocity.y
        // outright rather than taking a max, so a fast descent is cancelled by the
        // pad instead of surviving it. Opens the launch-bonus window.
        public void ApplyLaunch(float upwardVelocity)
        {
            _velocity.y = upwardVelocity;
            _lastLaunchTime = Time.time;
            IsGrounded = false;
        }
    }
}
