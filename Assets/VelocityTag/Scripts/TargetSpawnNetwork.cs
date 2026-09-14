// TargetSpawnNetwork.cs
// Port of TargetManager's spawn/reset logic: owns the spawn point list
// (assign empties in the scene per map, replacing mapData.targetSpawns),
// instantiates dummies, drives their per-frame Tick, and enforces the
// MIN_TARGET_RESPAWN_DISTANCE_FROM_PLAYER (5.0) rule on reposition.

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
        [SerializeField] private MatchStateMachine _matchState;

        private readonly List<TargetDummy> _targets = new List<TargetDummy>();

        private void OnEnable() => GameEventBus.On<TimeAttackStartEvent>(OnRoundStart);
        private void OnDisable() => GameEventBus.Off<TimeAttackStartEvent>(OnRoundStart);

        private void OnRoundStart(TimeAttackStartEvent _)
        {
            ClearTargets();
            for (int i = 0; i < _spawnPoints.Count; i++)
            {
                var dummy = Instantiate(_dummyPrefab, _spawnPoints[i].position, Quaternion.identity, transform);
                float angle = (i / (float)_spawnPoints.Count) * Mathf.PI * 2f;
                dummy.Init(this, _arena, angle);
                _targets.Add(dummy);
            }
        }

        private void ClearTargets()
        {
            foreach (var t in _targets) if (t != null) Destroy(t.gameObject);
            _targets.Clear();
        }

        public void RepositionTarget(TargetDummy target, Vector3 playerPos)
        {
            float minDist = _config != null ? _config.minTargetRespawnDistance : 5.0f;

            var valid = new List<Transform>();
            foreach (var s in _spawnPoints)
            {
                float planar = Vector2.Distance(
                    new Vector2(s.position.x, s.position.z),
                    new Vector2(playerPos.x, playerPos.z));
                if (planar >= minDist) valid.Add(s);
            }
            if (valid.Count == 0) valid = _spawnPoints;

            var pick = valid[Random.Range(0, valid.Count)];
            target.ResetAndReposition(pick.position);
        }

        private void Update()
        {
            bool playing = _matchState != null && _matchState.IsPlaying;
            Vector3 playerPos = _player != null ? _player.transform.position : Vector3.zero;
            float dt = Time.deltaTime;
            foreach (var t in _targets) t.Tick(playerPos, playing, dt);
        }
    }
}
