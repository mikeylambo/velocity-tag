// LaunchPad.cs
// Trigger volume that launches the player upward and arms TagBlaster's launch-
// bonus window (via JetpackLocomotion.ApplyLaunch, which stamps the launch time).
// Replaces the JS pad's debug-log sniff with a typed LaunchPadEngagedEvent.
//
// _launchVelocity is a port default (config.js carries LAUNCH_PAD_COOLDOWN but no
// pad velocity). 14 sits above VERTICAL_MAX (10.5) so a pad clearly out-lifts a
// normal thrust hold — tune on the component if pads should feel stronger/softer.

using UnityEngine;
using ShooterCore;

namespace VelocityTag
{
    [RequireComponent(typeof(Collider))]
    public class LaunchPad : MonoBehaviour
    {
        [SerializeField] private float _launchVelocity = 14f;
        [SerializeField] private float _cooldown = 0.5f; // LAUNCH_PAD_COOLDOWN
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
