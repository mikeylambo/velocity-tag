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
        [SerializeField] private GameConfig _config;

        [Tooltip("Per-pad ejection velocity. Training Cylinder pads are all power 22.")]
        [SerializeField] private float _launchVelocity = 22f;   // maps/trainingCylinder.js pad.power

        private float _lastFire = -999f;

        private float Cooldown => _config != null ? _config.launchPadCooldown : 0.5f;

        private void OnTriggerEnter(Collider other)
        {
            if (Time.time - _lastFire < Cooldown) return;
            var loco = other.GetComponentInParent<JetpackLocomotion>();
            if (loco == null) return;
            _lastFire = Time.time;
            loco.ApplyLaunch(_launchVelocity);
            GameEventBus.Emit(new LaunchPadEngagedEvent { PadPosition = transform.position });
        }
    }
}