// TargetSpawnNetwork.cs
// Owns the target pool and the reposition rule from targets.js: a repositioned
// dummy must land on a spawn point at least MIN_TARGET_RESPAWN_DISTANCE (5.0u,
// planar) from the player. Spawns the pool on TimeAttackStartEvent.

using System.Collections.Generic;
using UnityEngine;
using ShooterCore;

namespace VelocityTag
{
    public class TargetSpawnNetwork : MonoBehaviour
    {
        [SerializeField] private GameConfig _config;
        [SerializeField] private ArenaBounds _arena;
        [SerializeField] private TargetDummy _dummyPrefab;
        [SerializeField] private List<Transform> _spawnPoints = new List<Transform>();
        [SerializeField] private JetpackLocomotion _player;

        private readonly List<TargetDummy> _targets = new List<TargetDummy>();

        public Vector3 PlayerPosition => _player != null ? _player.transform.position : Vector3.zero;

        private void OnEnable() => GameEventBus.On<TimeAttackStartEvent>(OnRoundStart);
        private void OnDisable() => GameEventBus.Off<TimeAttackStartEvent>(OnRoundStart);

        private void OnRoundStart(TimeAttackStartEvent _)
        {
            ClearTargets();
            if (_dummyPrefab == null || _spawnPoints.Count == 0) return;

            for (int i = 0; i < _spawnPoints.Count; i++)
            {
                var dummy = Instantiate(_dummyPrefab, _spawnPoints[i].position, Quaternion.identity, transform);
                float angle = (i / (float)Mathf.Max(1, _spawnPoints.Count)) * Mathf.PI * 2f;
                dummy.Init(this, _arena, angle);
                _targets.Add(dummy);
            }
        }

        private void ClearTargets()
        {
            foreach (var t in _targets)
                if (t != null) Destroy(t.gameObject);
            _targets.Clear();
        }

        // Convenience used by TargetDummy at the end of its ghost window.
        public void RequestReposition(TargetDummy target)
        {
            RepositionTarget(target, PlayerPosition);
        }

        public void RepositionTarget(TargetDummy target, Vector3 playerPos)
        {
            if (target == null) return;
            float minDist = _config != null ? _config.minTargetRespawnDistance : 5.0f;

            var valid = new List<Transform>();
            foreach (var s in _spawnPoints)
            {
                if (s == null) continue;
                float planar = Vector2.Distance(
                    new Vector2(s.position.x, s.position.z),
                    new Vector2(playerPos.x, playerPos.z));
                if (planar >= minDist) valid.Add(s);
            }

            // If everything is too close, fall back to any spawn rather than stall.
            if (valid.Count == 0)
                foreach (var s in _spawnPoints) if (s != null) valid.Add(s);
            if (valid.Count == 0) return;

            var pick = valid[Random.Range(0, valid.Count)];
            target.ResetAndReposition(pick.position);
        }
    }
}
