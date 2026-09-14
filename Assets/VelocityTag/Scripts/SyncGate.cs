// SyncGate.cs
// Port of arena.js's recharge pad ("BASE GATE: SUIT SYNCING..."). The port
// README deferred this, but the spec is fully determined by arena.js:
// within hypot < 1.8 of the gate, below y 1.5, on an arena-wide 1.5s cooldown
// (CONFIG.SYNC_PAD_COOLDOWN), emit SUIT_SYNC_TICK.
//
// TimeAttackRound already listens for SuitSyncTickEvent and restores one suit
// charge per tick, so this completes the recovery half of the suit system:
// hostile fire costs charges, map control wins them back.

using UnityEngine;
using ShooterCore;

namespace VelocityTag
{
    public class SyncGate : MonoBehaviour
    {
        [SerializeField] private GameConfig _config;
        [SerializeField] private ArenaBounds _arena;
        [SerializeField] private JetpackLocomotion _player;

        [Tooltip("arena.js gates the pad within hypot < 1.8 of its centre.")]
        [SerializeField] private float _triggerRadius = 1.8f;

        private void Update()
        {
            if (_player == null || _arena == null || _config == null) return;

            Vector3 p = _player.transform.position;
            float dx = p.x - transform.position.x;
            float dz = p.z - transform.position.z;
            if (dx * dx + dz * dz >= _triggerRadius * _triggerRadius) return;

            if (!_arena.TryConsumeSync(_config, p)) return;

            GameEventBus.Emit(new SuitSyncTickEvent());
        }
    }
}
