// RoundStartHarness.cs
// Minimal way to actually start and restart a run. TimeAttackRound exposes
// StartTimeAttack() and the port's README defers the trigger to "a menu button
// (or debug key)" — but nothing called it, so a fully wired scene sat in Menu
// forever and could not be validated at all.
//
// This is a validation harness, not UI: no menu, no art. It also covers the
// retry path, which the hand-off lists as a known bug in the JS build
// ("tap trigger to retry is not working at the end of a run") — here retry is
// the same single entry point as start, so it cannot drift out of sync.
//
// Replace with real menu/results UI when that pass happens.

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using ShooterCore;

namespace VelocityTag
{
    public class RoundStartHarness : MonoBehaviour
    {
        [SerializeField] private TimeAttackRound _round;
        [SerializeField] private MatchStateMachine _matchState;

        [Tooltip("Start a run automatically on entering Play Mode.")]
        [SerializeField] private bool _autoStart;

        private InputDevice _right;
        private bool _wasPressed;

        private void Start()
        {
            if (_autoStart) TryStart();
        }

        private void Update()
        {
            if (_round == null || _matchState == null) return;

            bool pressed = false;

            var kb = Keyboard.current;
            if (kb != null) pressed |= kb.enterKey.isPressed || kb.numpadEnterKey.isPressed;

            if (!_right.isValid) _right = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            if (_right.isValid &&
                _right.TryGetFeatureValue(CommonUsages.primaryButton, out bool aButton))
                pressed |= aButton;

            if (pressed && !_wasPressed) TryStart();
            _wasPressed = pressed;
        }

        private void TryStart()
        {
            // Only from a resting phase — never restart mid-run.
            if (_matchState.Phase == MatchPhase.Menu || _matchState.Phase == MatchPhase.MatchOver)
                _round.StartTimeAttack();
        }
    }
}
