// CrossPlatformInput.cs
// Produces a NormalizedInputState each frame that the game layer reads, plus
// edge-triggered DashEvent / QuickDropEvent intents. Desktop keymap below uses
// the legacy Input Manager (guarded), so set Player Settings -> Active Input
// Handling to "Both" for desktop testing alongside XRI. On Quest, feed the
// analog sticks/trigger into State from your XRI action driver.
//
// Input contract (per the JS control scheme):
//   Strafe = planar (x strafe, y fwd/back)   Thrust = vertical jetpack (-1..1)
//   Look.x = smooth turn                      Firing = trigger
//   DashPressed = grip                        QuickDropPressed = right-stick click

using UnityEngine;

namespace ShooterCore
{
    public struct NormalizedInputState
    {
        public Vector2 Strafe;
        public float Thrust;
        public Vector2 Look;
        public bool Firing;
        public bool DashPressed;
        public bool QuickDropPressed;
    }

    // Core input-intent events (emitted here, consumed by the game layer).
    public struct DashEvent { }
    public struct QuickDropEvent { }

    public class CrossPlatformInput : MonoBehaviour
    {
        public NormalizedInputState State { get; private set; }

        // VR/XRI drivers can push a state in instead of using the desktop keymap.
        public void SetState(NormalizedInputState state) => State = state;

        private void Update()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            var s = new NormalizedInputState();

            float x = (Input.GetKey(KeyCode.D) ? 1f : 0f) - (Input.GetKey(KeyCode.A) ? 1f : 0f);
            float y = (Input.GetKey(KeyCode.W) ? 1f : 0f) - (Input.GetKey(KeyCode.S) ? 1f : 0f);
            s.Strafe = new Vector2(x, y);

            float up = (Input.GetKey(KeyCode.Space) ? 1f : 0f) - (Input.GetKey(KeyCode.LeftShift) ? 1f : 0f);
            s.Thrust = up;

            float turn = (Input.GetKey(KeyCode.E) ? 1f : 0f) - (Input.GetKey(KeyCode.Q) ? 1f : 0f);
            s.Look = new Vector2(turn, 0f);

            s.Firing = Input.GetKey(KeyCode.Mouse0) || Input.GetKey(KeyCode.Return);
            s.DashPressed = Input.GetKeyDown(KeyCode.LeftControl);
            s.QuickDropPressed = Input.GetKeyDown(KeyCode.C);

            State = s;

            if (s.DashPressed) GameEventBus.Emit(new DashEvent());
            if (s.QuickDropPressed) GameEventBus.Emit(new QuickDropEvent());
#endif
        }
    }
}
