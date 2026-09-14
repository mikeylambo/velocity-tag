// CrossPlatformInput.cs
// Merges the best half of each game's input approach:
//   - Neon Galactic's input/InputState.js: the CONTRACT is well-designed —
//     one platform-agnostic shape (strafe, thrust, look, roll, firing) that
//     every input source writes into, so gameplay code never branches on
//     platform. It's a stub with zero implementation behind it, though.
//   - Velocity Tag's input.js: the IMPLEMENTATION is real and working —
//     desktop keydown/mouse handlers plus actual polled WebXR gamepad axis/
//     button reads (including the hard-won fix: "axes[2]/[3], not axes[0]/[1]"
//     for Quest 3 thumbsticks) — but it writes into a game-specific shape,
//     not a reusable one.
//
// This struct is the reusable contract; PopulateFromDesktop / PopulateFromXR
// are the two "drivers" — port Velocity Tag's actual button/axis mappings into
// PopulateFromXR per-game, since trigger/grip/thumbstick-click mappings differ
// between a jetpack shooter and a 6DOF ship.

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using ShooterCore;

namespace ShooterCore.Input
{
    public struct NormalizedInputState
    {
        public Vector2 Strafe;      // left/right, up/down thrust
        public float Thrust;        // forward/back
        public Vector2 Look;        // pitch, yaw
        public float Roll;
        public bool Firing;
        public bool DashPressed;
        public bool QuickDropPressed;
        public string Platform;     // "PC" | "VR" — drives HUD prompts, same as JS
    }

    public class CrossPlatformInput : MonoBehaviour
    {
        public NormalizedInputState State { get; private set; }

        private InputDevice _rightXRController;
        private InputDevice _leftXRController;
        private bool _xrActive;

        private void Update()
        {
            _xrActive = TryGetXRDevices();
            State = _xrActive ? PopulateFromXR() : PopulateFromDesktop();
        }

        private NormalizedInputState PopulateFromDesktop()
        {
            var s = new NormalizedInputState { Platform = "PC" };
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            if (kb == null) return s;

            // Mirrors input.js's WASD -> planar mapping directly.
            s.Strafe.x = (kb.dKey.isPressed ? 1 : 0) - (kb.aKey.isPressed ? 1 : 0);
            s.Strafe.y = (kb.wKey.isPressed ? 1 : 0) - (kb.sKey.isPressed ? 1 : 0);
            s.Roll = (kb.rightArrowKey.isPressed ? 1 : 0) - (kb.leftArrowKey.isPressed ? 1 : 0);
            s.Thrust = kb.spaceKey.isPressed ? 1f : 0f;
            s.DashPressed = kb.leftShiftKey.wasPressedThisFrame;
            s.QuickDropPressed = kb.cKey.wasPressedThisFrame || kb.leftCtrlKey.wasPressedThisFrame;

            if (mouse != null)
            {
                s.Firing = mouse.leftButton.isPressed;
                Vector2 delta = mouse.delta.ReadValue();
                s.Look = new Vector2(-delta.y * 0.003f, -delta.x * 0.003f); // pitch, yaw — same scalar as PCInput.js
            }
            return s;
        }

        private NormalizedInputState PopulateFromXR()
        {
            var s = new NormalizedInputState { Platform = "VR" };

            // Port Velocity Tag's exact mapping here per-project:
            //   Right controller: axes[2] = turn, buttons[0] = trigger (fire),
            //     buttons[3] = thumbstick click (quick drop), buttons[5] = pause.
            //   Left controller: axes[2]/[3] = planar move, buttons[0].value = thrust,
            //     buttons[1] = dash.
            // Unity's InputDevice API differs from raw WebXR gamepad indices —
            // use CommonUsages (primary2DAxis, triggerButton, gripButton) rather
            // than assuming button index parity with the JS build.

            if (_rightXRController.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 rightStick))
                s.Roll = rightStick.x;

            if (_rightXRController.TryGetFeatureValue(CommonUsages.triggerButton, out bool trigger))
                s.Firing = trigger;

            if (_leftXRController.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 leftStick))
                s.Strafe = leftStick;

            if (_leftXRController.TryGetFeatureValue(CommonUsages.gripButton, out bool grip))
                s.Thrust = grip ? 1f : 0f;

            return s;
        }

        private bool TryGetXRDevices()
        {
            if (!_rightXRController.isValid)
                _rightXRController = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            if (!_leftXRController.isValid)
                _leftXRController = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            return _rightXRController.isValid && _leftXRController.isValid;
        }
    }
}
