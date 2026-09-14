// XRNodePoseDriver.cs
// Applies a tracked XR node's pose to this transform, in local space, so a
// rig can be assembled as: Origin (game-driven) -> Head/Hands (device-driven).
//
// Why not XROrigin + TrackedPoseDriver from the XR Interaction Toolkit: this
// build needs a rig that compiles and runs BEFORE the XR packages are installed
// (package install is a separate Editor-side step), and Velocity Tag drives its
// blaster from a raw controller pose + raycast rather than XRI interactors, so
// none of XRI's interaction stack is load-bearing here. UnityEngine.XR's
// InputDevices is a built-in module — the same one XRInputDriver already polls,
// which keeps pose and button reads in one subsystem.
//
// If you later adopt XRI for teleport/UI rays, replace this component with a
// real XROrigin; the hierarchy shape is deliberately identical.

using UnityEngine;
using UnityEngine.XR;

namespace VelocityTag
{
    [DefaultExecutionOrder(-200)]
    public class XRNodePoseDriver : MonoBehaviour
    {
        [SerializeField] private XRNode _node = XRNode.Head;
        [SerializeField] private bool _trackPosition = true;
        [SerializeField] private bool _trackRotation = true;

        private InputDevice _device;

        private void Update()
        {
            if (!_device.isValid)
            {
                _device = InputDevices.GetDeviceAtXRNode(_node);
                if (!_device.isValid) return;
            }

            if (_trackPosition &&
                _device.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 pos))
                transform.localPosition = pos;

            if (_trackRotation &&
                _device.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rot))
                transform.localRotation = rot;
        }
    }
}
