// PlayerInputRouter.cs
// The missing link between input and gameplay. CrossPlatformInput reports HELD
// states; JetpackLocomotion listens for DashEvent/QuickDropEvent on the bus and
// TagBlaster exposes TryFire() — but in the hand-off port nothing connected the
// two, so a fully wired scene still could not dash, quick drop, or shoot.
//
// Edge detection lives here, once, for every platform: both drivers report held
// booleans, so desktop and Quest produce identical press semantics rather than
// each re-implementing the JS build's per-button `mem` latches.
//
// Mirrors src/input.js's emit points:
//   trigger press      -> TagBlaster.TryFire()   (INPUT_FIRE)
//   grip press         -> DashEvent              (INPUT_DASH)
//   stick click press  -> QuickDropEvent         (EXECUTE_QUICK_DROP)
//   B/Y press          -> pause toggle           (TOGGLE_PAUSE)

using UnityEngine;
using ShooterCore;
using ShooterCore.Input;

namespace VelocityTag
{
    [DefaultExecutionOrder(-100)]
    [RequireComponent(typeof(CrossPlatformInput))]
    public class PlayerInputRouter : MonoBehaviour
    {
        [SerializeField] private TagBlaster _blaster;
        [SerializeField] private MatchStateMachine _matchState;

        private CrossPlatformInput _input;
        private bool _wasFiring;
        private bool _wasDashing;
        private bool _wasQuickDropping;
        private bool _wasPausing;

        private void Awake() => _input = GetComponent<CrossPlatformInput>();

        private void Update()
        {
            var s = _input.State;

            // Fire is edge-triggered, not held: the JS build latches mem.fire so a
            // held trigger produces exactly one shot. TagBlaster's own shot clock
            // then gates the cadence at playerFireCooldown.
            if (Pressed(s.Firing, ref _wasFiring) && _blaster != null)
                _blaster.TryFire();

            if (Pressed(s.DashPressed, ref _wasDashing))
                GameEventBus.Emit(new DashEvent());

            if (Pressed(s.QuickDropPressed, ref _wasQuickDropping))
                GameEventBus.Emit(new QuickDropEvent());

            if (Pressed(s.PausePressed, ref _wasPausing) && _matchState != null)
            {
                if (_matchState.Phase == MatchPhase.Playing) _matchState.Pause();
                else if (_matchState.Phase == MatchPhase.Paused) _matchState.Resume();
            }
        }

        private static bool Pressed(bool now, ref bool was)
        {
            bool rising = now && !was;
            was = now;
            return rising;
        }
    }
}
