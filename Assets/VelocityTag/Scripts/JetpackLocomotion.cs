// JetpackLocomotion.cs
// Port of player.js physics: planar move (grounded vs air speed), vertical
// jetpack thrust clamped to VERTICAL_MAX, gravity, smooth turn, grip dash, and
// right-stick quick drop. Also tracks the movement context TagBlaster reads for
// route bonuses (airborne / launch window / quick-drop window).
//
// NOTE: player.js referenced a fuel meter, but config.js carries no fuel tuning
// values, so no fuel gating is invented here. Add it once those values surface.

using UnityEngine;
using ShooterCore;

namespace VelocityTag
{
    public class JetpackLocomotion : MonoBehaviour
    {
        [SerializeField] private GameConfig _config;
        [SerializeField] private ArenaBounds _arena;
        [SerializeField] private CrossPlatformInput _input;
        [SerializeField] private float _collisionRadius = 0.5f;

        private Vector3 _velocity;
        private float _lastLaunchTime = -999f;
        private float _lastQuickDropLand = -999f;
        private bool _quickDropping;

        public bool IsGrounded { get; private set; }

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
            float planarSpeed = IsGrounded ? _config.moveSpeed : _config.airMoveSpeed;
            Vector3 planar = (transform.right * state.Strafe.x + transform.forward * state.Strafe.y) * planarSpeed;
            _velocity.x = planar.x;
            _velocity.z = planar.z;

            // Vertical, matching player.js exactly: thrust and gravity are mutually
            // exclusive branches. While jetting, thrust accelerates with NO gravity
            // that frame (climbs to VERTICAL_MAX); thrust-down descends at
            // DESCEND_SPEED; gravity applies only when not thrusting. The previous
            // port applied thrust and gravity together, which made 15 vs 16 a net
            // -1 and grounded the jetpack entirely.
            if (state.Thrust > 0.05f)
            {
                _velocity.y += _config.verticalThrust * state.Thrust * dt;
                _velocity.y = Mathf.Min(_velocity.y, _config.verticalMax);
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

            if (_arena != null) _arena.ResolveCollisions(transform, _collisionRadius);
        }

        private void OnDash(DashEvent _)
        {
            var state = _input != null ? _input.State : default;
            Vector3 dir = transform.right * state.Strafe.x + transform.forward * state.Strafe.y;
            if (dir.sqrMagnitude < 0.01f) dir = transform.forward;
            dir.y = 0f;
            dir.Normalize();

            Vector3 dash = dir * _config.dashSpeed;
            _velocity.x = dash.x;
            _velocity.z = dash.z;
        }

        private void OnQuickDrop(QuickDropEvent _)
        {
            if (IsGrounded) return;
            // player.js: velocity.y = min(velocity.y, -QUICK_DROP_SPEED)
            _velocity.y = Mathf.Min(_velocity.y, -_config.quickDropSpeed);
            _quickDropping = true;
        }

        // Called by LaunchPad triggers. Hard vertical set, like the JS pads, and
        // opens the launch-bonus window.
        public void ApplyLaunch(float upwardVelocity)
        {
            _velocity.y = Mathf.Max(_velocity.y, upwardVelocity);
            _lastLaunchTime = Time.time;
            IsGrounded = false;
        }
    }
}
