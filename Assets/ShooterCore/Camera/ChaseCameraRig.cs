// ChaseCameraRig.cs
// Ports Velocity Tag's camera.js CameraManager.updateChaseRig() — the one system
// in either codebase with a specific, hard-won hardware-correctness fix worth
// preserving exactly rather than re-deriving:
//
//   "FIX: Replace rig.lookAt with explicit Euler rotation to avoid VR flip loops."
//
// rig.lookAt() (Transform.LookAt in Unity) recomputes a rotation toward a point
// every frame, which can flip/snap when the target and up-vector go near-parallel
// — exactly the situation a chase cam behind a rolling/pitching avatar hits
// during a jetpack quick-drop or a 6DOF barrel roll. Setting rotation directly
// from the player's known facing angle (yaw) plus a fixed pitch offset sidesteps
// the whole problem. This is a real, transferable fix for BOTH games — Neon
// Galactic's 6DOF ship will hit the same gimbal issue if/when it gets a chase
// cam, worse, since it rotates on more axes than Velocity Tag's yaw-only avatar.

using UnityEngine;

namespace ShooterCore.CameraRig
{
    public class ChaseCameraRig : MonoBehaviour
    {
        [SerializeField] private GameConfig _config;
        [SerializeField] private Transform _target;      // the avatar/ship transform
        [SerializeField] private Camera _camera;

        [Tooltip("Apply camPitchOffset to the rig. MUST be off in VR: the rig is the " +
                 "XR Origin, so pitching it tilts the player's whole world.")]
        [SerializeField] private bool _applyPitchOffset = true;

        /// Set false when this rig is acting as the XR Origin.
        public bool ApplyPitchOffset { get => _applyPitchOffset; set => _applyPitchOffset = value; }

        private void LateUpdate()
        {
            if (_target == null) return;
            UpdateChaseRig(_target.position, _target.eulerAngles.y * Mathf.Deg2Rad, Time.deltaTime);
        }

        public void SnapTo(Vector3 avatarPos, float facingAngleRad)
            => UpdateChaseRig(avatarPos, facingAngleRad, 0f, instant: true);

        private void UpdateChaseRig(Vector3 avatarPos, float facingAngleRad, float dt, bool instant = false)
        {
            // 1. Tether offset behind the player, rotated to face angle — same
            // math as CONFIG.CAM_SIDE / CAM_HEIGHT / CAM_DIST in config.js.
            Quaternion yawRot = Quaternion.Euler(0, facingAngleRad * Mathf.Rad2Deg, 0);
            Vector3 tether = yawRot * new Vector3(_config.camSide, _config.camHeight, -_config.camDist);
            Vector3 targetPos = avatarPos + tether;

            // 2. Smooth follow — matches the JS lerp(dt * 8.0) damping constant.
            transform.position = instant
                ? targetPos
                : Vector3.Lerp(transform.position, targetPos, Mathf.Clamp01(dt * 8.0f));

            // 3. THE FIX: explicit yaw + fixed pitch offset, never LookAt().
            // Order matches the JS 'YXZ' Euler order explicitly for parity —
            // Unity's default Euler application order differs, so don't assume
            // this is a no-op port; verify pitch/yaw don't cross-couple on your
            // avatar's actual rotation convention before trusting it in VR.
            // In VR the pitch term is dropped, not the fix: yaw still comes from the
            // player's known facing angle rather than LookAt(). camera.js applies
            // -0.15 unconditionally, which is a comfort hazard on a headset — the one
            // place this port deliberately diverges from the JS build's behaviour.
            float pitchDeg = _applyPitchOffset ? _config.camPitchOffset * Mathf.Rad2Deg : 0f;
            transform.rotation = Quaternion.Euler(pitchDeg, facingAngleRad * Mathf.Rad2Deg, 0);
        }
    }
}
