// CameraModeSwitch.cs
// Chooses which camera owns the rig, once, when the headset appears.
//
// The rig itself is shared. That is the faithful port of camera.js, where
// `this.rig` is a single THREE.Group holding BOTH the camera and the two
// controllers, and updateChaseRig() moves that one group. In Unity terms the
// chase rig IS the XR Origin: gameplay drives the origin in world space, and
// the headset drives the camera's LOCAL pose underneath it. Velocity Tag is
// third-person in VR by design — the CAM_DIST/CAM_HEIGHT/CAM_SIDE tether keeps
// the avatar in view — so the tether stays on in both modes.
//
// Two things must change on the VR path:
//   1. Pitch offset off. The rig is the origin, so pitching it tilts the
//      player's world rather than the camera. See ChaseCameraRig.
//   2. Aim origin moves to the right controller, matching getAimRay(), which
//      prefers the right hand and only falls back to the camera.

using UnityEngine;
using UnityEngine.XR;
using ShooterCore.CameraRig;

namespace VelocityTag
{
    [DefaultExecutionOrder(-50)]
    public class CameraModeSwitch : MonoBehaviour
    {
        [SerializeField] private ChaseCameraRig _rig;
        [SerializeField] private TagBlaster _blaster;

        [Header("Desktop")]
        [SerializeField] private GameObject _desktopCamera;
        [SerializeField] private Transform _desktopAimOrigin;

        [SerializeField] private Camera _desktopCameraComponent;

        [Header("XR")]
        [SerializeField] private GameObject _xrRoot;
        [SerializeField] private Transform _xrAimOrigin;    // RightHand controller
        [SerializeField] private Camera _xrCameraComponent;

        [Header("Attached views")]
        [SerializeField] private ReticleView _reticle;
        [Tooltip("hud.js parents the panel to the camera itself, so it rides the head in VR.")]
        [SerializeField] private Transform _hud;
        [SerializeField] private Vector3 _hudLocalPosition = new Vector3(0f, -0.65f, -2.5f);

        private bool _xrActive;
        private bool _applied;

        private void Start() => Apply(false);   // desktop until a headset says otherwise

        private void Update()
        {
            bool xr = InputDevices.GetDeviceAtXRNode(XRNode.Head).isValid;
            if (xr != _xrActive || !_applied) Apply(xr);
        }

        private void Apply(bool xr)
        {
            _xrActive = xr;
            _applied = true;

            if (_desktopCamera != null) _desktopCamera.SetActive(!xr);
            if (_xrRoot != null) _xrRoot.SetActive(xr);

            // The rig keeps its tether in both modes; only the pitch term differs.
            if (_rig != null) _rig.ApplyPitchOffset = !xr;

            Transform aim = xr ? _xrAimOrigin : _desktopAimOrigin;
            Camera live = xr ? _xrCameraComponent : _desktopCameraComponent;

            if (_blaster != null) _blaster.SetAimOrigin(aim);

            if (_reticle != null)
            {
                _reticle.SetAimOrigin(aim);
                _reticle.SetBillboardTarget(live);
            }

            // hud.js does `cameraManager.camera.add(this.mesh)` — the panel is a
            // child of the camera, not of the rig, so it stays fixed in view.
            if (_hud != null && live != null)
            {
                _hud.SetParent(live.transform, false);
                _hud.localPosition = _hudLocalPosition;
                _hud.localRotation = Quaternion.identity;
            }
        }
    }
}
