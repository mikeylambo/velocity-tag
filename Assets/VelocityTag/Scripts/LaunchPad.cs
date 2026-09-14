// LaunchPad.cs
// Port of arena.js's launch pad gate. Distance-based per frame, exactly as in
// the JS build — not a physics trigger. arena.js tests `hypot(pos - pad) < 1.5`
// against the player every update, which is both simpler and more reliable than
// depending on kinematic-rigidbody trigger callbacks that can stop firing when
// the body sleeps.
//
// The cooldown is arena-wide, not per-pad: arena.js keeps one triggerMem.launch
// for every pad on the map, which is what LAUNCH_PAD_COOLDOWN was tuned against.
// ArenaBounds owns that timer, and also applies the height gate.

using UnityEngine;
using ShooterCore;

namespace VelocityTag
{
    public class LaunchPad : MonoBehaviour
    {
        [SerializeField] private GameConfig _config;
        [SerializeField] private ArenaBounds _arena;
        [SerializeField] private JetpackLocomotion _player;

        [Tooltip("Per-pad ejection velocity. Training Cylinder pads are all power 22.")]
        [SerializeField] private float _launchVelocity = 22f;   // maps/trainingCylinder.js pad.power

        [Tooltip("arena.js triggers the pad within hypot < 1.5 of its centre.")]
        [SerializeField] private float _triggerRadius = 1.5f;

        private void Update()
        {
            if (_player == null || _arena == null || _config == null) return;

            Vector3 p = _player.transform.position;
            float dx = p.x - transform.position.x;
            float dz = p.z - transform.position.z;
            if (dx * dx + dz * dz >= _triggerRadius * _triggerRadius) return;

            if (!_arena.TryConsumeLaunch(_config, p)) return;

            _player.ApplyLaunch(_launchVelocity);
            GameEventBus.Emit(new LaunchPadEngagedEvent { PadPosition = transform.position });
        }
    }
}
