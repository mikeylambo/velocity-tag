// XRInputDriver.cs
// Quest 3 driver for ShooterCore's CrossPlatformInput mailbox. This is the
// per-game "driver decision" the ShooterCore README defers: it ports the exact
// control layout from the JS build's src/input.js, translated to Unity's
// InputDevice/CommonUsages API rather than copied by raw gamepad index.
//
// src/input.js layout (authoritative):
//   RIGHT hand: axes[2]          -> turn           (0.15 deadzone)
//               buttons[0]       -> trigger, fire  (>0.5 press, <=0.1 release)
//               buttons[3]       -> stick click, quick drop
//               buttons[5]       -> pause
//   LEFT  hand: axes[2]/axes[3]  -> planar move    (0.15 deadzone)
//               buttons[0].value -> thrust         (ANALOG trigger pull)
//               buttons[1]       -> dash           (grip)
//               buttons[5]       -> pause
//
// Two index-to-Unity translations that are NOT one-to-one, and are the reason
// this file exists instead of a literal transcription:
//
//   1. WebXR axes[2]/[3] are the THUMBSTICK (axes[0]/[1] are the touchpad on
//      devices that have one). Unity exposes the thumbstick as primary2DAxis,
//      so primary2DAxis IS axes[2]/[3]. That is the JS build's "axes[2]/[3],
//      not axes[0]/[1]" hardware fix, already handled by Unity's abstraction.
//   2. The JS build negates axes[3] (`planar.z = -axes[3]`) because raw WebXR
//      reports stick-forward as NEGATIVE y. Unity's primary2DAxis already
//      reports stick-forward as POSITIVE y, so the negation must NOT be copied
//      or forward/back inverts. Strafe.y takes primary2DAxis.y directly.
//
// Verify trigger/grip/stick-click on hardware before trusting this in a build —
// same caveat the JS build flags on its own axis fix.

using UnityEngine;
using UnityEngine.XR;
using ShooterCore.Input;

namespace VelocityTag
{
    [DefaultExecutionOrder(-200)]
    [RequireComponent(typeof(CrossPlatformInput))]
    public class XRInputDriver : MonoBehaviour
    {
        [Tooltip("Stick deadzone. input.js uses 0.15 on both turn and planar move.")]
        [SerializeField] private float _deadzone = 0.15f;

        [Tooltip("Trigger pull that counts as a press / release. input.js: >0.5 press, <=0.1 release.")]
        [SerializeField] private float _triggerPressThreshold = 0.5f;
        [SerializeField] private float _triggerReleaseThreshold = 0.1f;

        private CrossPlatformInput _mailbox;
        private InputDevice _left;
        private InputDevice _right;

        // Schmitt trigger on the fire button, mirroring input.js's mem.fire latch:
        // a single pull must cross 0.5 to arm and fall to 0.1 to re-arm, so a
        // finger resting at half-pull cannot chatter the shot clock.
        private bool _firingLatched;

        private void Awake() => _mailbox = GetComponent<CrossPlatformInput>();

        private void Update()
        {
            if (!AcquireDevices()) return;   // no headset yet: desktop fallback stays live

            var s = new NormalizedInputState { Platform = "VR" };

            // --- RIGHT: aim arm ---
            if (_right.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 rightStick))
                s.Turn = Deadzone(rightStick.x);

            float rightTrigger = ReadAnalog(_right, CommonUsages.trigger);
            if (_firingLatched)
            {
                if (rightTrigger <= _triggerReleaseThreshold) _firingLatched = false;
            }
            else if (rightTrigger > _triggerPressThreshold)
            {
                _firingLatched = true;
            }
            s.Firing = _firingLatched;

            if (_right.TryGetFeatureValue(CommonUsages.primary2DAxisClick, out bool stickClick))
                s.QuickDropPressed = stickClick;

            // --- LEFT: mobility arm ---
            if (_left.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 leftStick))
            {
                s.Strafe.x = Deadzone(leftStick.x);
                s.Strafe.y = Deadzone(leftStick.y);   // NOT negated — see header note 2
            }

            s.Thrust = Mathf.Clamp01(ReadAnalog(_left, CommonUsages.trigger));

            if (_left.TryGetFeatureValue(CommonUsages.gripButton, out bool leftGrip))
                s.DashPressed = leftGrip;

            // --- Pause: B/Y on either hand (buttons[5]) ---
            s.PausePressed = ReadButton(_right, CommonUsages.secondaryButton)
                          || ReadButton(_left, CommonUsages.secondaryButton);

            _mailbox.SetState(s);
        }

        private float Deadzone(float v) => Mathf.Abs(v) > _deadzone ? v : 0f;

        private static float ReadAnalog(InputDevice d, InputFeatureUsage<float> usage)
            => d.TryGetFeatureValue(usage, out float v) ? v : 0f;

        private static bool ReadButton(InputDevice d, InputFeatureUsage<bool> usage)
            => d.TryGetFeatureValue(usage, out bool v) && v;

        private bool AcquireDevices()
        {
            if (!_left.isValid) _left = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            if (!_right.isValid) _right = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            return _left.isValid && _right.isValid;
        }
    }
}
