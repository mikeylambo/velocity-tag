// CrossPlatformInput.cs
// Merges the best half of each game's input approach:
//   - Neon Galactic's input/InputState.js: the CONTRACT is well-designed —
//     one platform-agnostic shape (strafe, thrust, turn, firing) that every
//     input source writes into, so gameplay code never branches on platform.
//     It's a stub with zero implementation behind it, though.
//   - Velocity Tag's input.js: the IMPLEMENTATION is real and working —
//     desktop key handlers plus polled WebXR gamepad axis/button reads
//     (including the hard-won fix: "axes[2]/[3], not axes[0]/[1]" for Quest 3
//     thumbsticks) — but it writes into a game-specific shape, not a reusable one.
//
// This struct is the reusable contract. This component is a MAILBOX, not a
// polling loop: a per-platform driver (e.g. VelocityTag's XRInputDriver) pushes
// a filled state in via SetState each frame, and gameplay reads State without
// knowing where it came from. Desktop polling is kept as the built-in fallback
// driver so PC playtesting works with nothing else in the scene.

using UnityEngine;
using UnityEngine.InputSystem;

namespace ShooterCore.Input
{
    public struct NormalizedInputState
    {
        /// Planar movement in avatar space. x = strafe (+right), y = forward/back (+forward).
        public Vector2 Strafe;

        /// Jetpack lift, 0..1. Analog on XR (trigger pull), digital on desktop.
        public float Thrust;

        /// Yaw rate, -1..1. +1 turns right. This is the ONLY smooth-turn channel;
        /// locomotion reads it directly (JS: input.state.turn from right stick axes[2]).
        public float Turn;

        /// Free-aim look delta (pitch, yaw) for mouse/gamepad aim. NOT wired to yaw —
        /// avatar facing comes from Turn. Kept for HUD/aim features that want it.
        public Vector2 Look;

        /// Roll, for 6DOF vehicles. Unused by a yaw-only jetpack avatar.
        public float Roll;

        /// Held states. Edge detection is the consuming router's job, so desktop
        /// and XR drivers report identical semantics.
        public bool Firing;
        public bool DashPressed;
        public bool QuickDropPressed;
        public bool PausePressed;

        public string Platform;     // "PC" | "VR" — drives HUD prompts, same as JS
    }

    [DefaultExecutionOrder(-150)]
    public class CrossPlatformInput : MonoBehaviour
    {
        public NormalizedInputState State { get; private set; }

        /// True when an external driver (XR) supplied this frame's state.
        public bool IsDriven { get; private set; }

        private int _lastDrivenFrame = -1;

        /// Called by a platform driver each frame, before gameplay reads State.
        /// While a driver keeps calling this, desktop polling stays out of the way.
        public void SetState(in NormalizedInputState state)
        {
            State = state;
            _lastDrivenFrame = Time.frameCount;
            IsDriven = true;
        }

        private void Update()
        {
            // A driver that pushed this frame or last frame owns input; ordering
            // between its Update and ours is not guaranteed, so allow one frame
            // of slack rather than flickering back to desktop every other frame.
            if (_lastDrivenFrame >= Time.frameCount - 1) return;

            IsDriven = false;
            State = PopulateFromDesktop();
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

            // input.js: ArrowLeft = -1, ArrowRight = +1 into state.turn. Desktop
            // turning is arrow keys, not the mouse — the JS build never steered
            // the avatar with mouse movement.
            s.Turn = (kb.rightArrowKey.isPressed ? 1 : 0) - (kb.leftArrowKey.isPressed ? 1 : 0);

            s.Thrust = kb.spaceKey.isPressed ? 1f : 0f;
            s.DashPressed = kb.leftShiftKey.isPressed;
            s.QuickDropPressed = kb.cKey.isPressed || kb.leftCtrlKey.isPressed;
            s.PausePressed = kb.pKey.isPressed || kb.escapeKey.isPressed;

            if (mouse != null)
            {
                s.Firing = mouse.leftButton.isPressed;
                Vector2 delta = mouse.delta.ReadValue();
                s.Look = new Vector2(-delta.y * 0.003f, -delta.x * 0.003f); // pitch, yaw
            }
            return s;
        }
    }
}
