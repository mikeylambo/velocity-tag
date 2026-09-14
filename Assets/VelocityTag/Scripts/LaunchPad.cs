// LaunchPad.cs
// Trigger volume: launches the player upward and arms TagBlaster's 2s
// launch-bonus window via LaunchPadEngagedEvent (replaces the JS LOG_DEBUG sniff).

using UnityEngine;
using ShooterCore;

namespace VelocityTag
{
    /// Put on a trigger collider. Launches the player and arms the launch bonus.
    [RequireComponent(typeof(Collider))]
    public class LaunchPad : MonoBehaviour
    {
        [SerializeField] private float _launchVelocity = 14f;
        [SerializeField] private float _cooldown = 0.5f;   // LAUNCH_PAD_COOLDOWN
        private float _lastFire = -999f;

        private void OnTriggerEnter(Collider other)
        {
            if (Time.time - _lastFire < _cooldown) return;
            var loco = other.GetComponentInParent<JetpackLocomotion>();
            if (loco == null) return;
            _lastFire = Time.time;
            loco.ApplyLaunch(_launchVelocity);
            GameEventBus.Emit(new LaunchPadEngagedEvent { PadPosition = transform.position });
        }
    }
}