// TargetDummy.cs
// Port of targets.js: a strict 1-charge dummy. On a valid hit it enters a
// GHOST_DURATION (0.75s) invulnerability window with its zone colliders off,
// then asks the spawn network to reposition it to a valid point >=5u from the
// player. It does NOT evaporate — the old disableTarget() invisibility bug is
// gone; visible flash/recoil/shimmer are an FX-pass concern driven by events.

using UnityEngine;
using ShooterCore;

namespace VelocityTag
{
    public class TargetDummy : MonoBehaviour
    {
        [SerializeField] private GameConfig _config;
        [SerializeField] private TargetZone[] _zones;

        public bool IsGhosting { get; private set; }

        private TargetSpawnNetwork _network;
        private ArenaBounds _arena;
        private float _spawnAngle;
        private float _ghostTimer;

        // Called by TargetSpawnNetwork right after Instantiate.
        public void Init(TargetSpawnNetwork network, ArenaBounds arena, float startAngle)
        {
            _network = network;
            _arena = arena;
            _spawnAngle = startAngle;

            if (_zones == null || _zones.Length == 0)
                _zones = GetComponentsInChildren<TargetZone>(true);

            SetZonesEnabled(true);
            IsGhosting = false;
        }

        // Called by TagBlaster on a confirmed hit against a live zone.
        public void RegisterHit()
        {
            if (IsGhosting) return;
            IsGhosting = true;
            _ghostTimer = _config != null ? _config.ghostDuration : 0.75f;
            SetZonesEnabled(false);
        }

        private void Update()
        {
            if (!IsGhosting) return;

            _ghostTimer -= Time.deltaTime;
            if (_ghostTimer <= 0f)
            {
                if (_network != null) _network.RequestReposition(this);
                else EndGhost(); // standalone fallback
            }
        }

        // Called by the network with a validated spawn position.
        public void ResetAndReposition(Vector3 position)
        {
            transform.position = position;
            EndGhost();
        }

        private void EndGhost()
        {
            IsGhosting = false;
            SetZonesEnabled(true);
        }

        private void SetZonesEnabled(bool on)
        {
            if (_zones == null) return;
            foreach (var z in _zones)
                if (z != null) z.SetEnabled(on);
        }
    }
}
