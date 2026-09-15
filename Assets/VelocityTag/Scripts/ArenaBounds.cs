// ArenaBounds.cs
// Floor query + obstacle push-out for the arena. JetpackLocomotion calls FloorY()
// to clamp landing height (flat floor, plus optional raycast onto platform tops),
// ResolveCeiling() to stop a climb at the underside of a slab, and
// ResolveCollisions() to slide the player out of pillars/cover.

using System.Collections.Generic;
using UnityEngine;

namespace VelocityTag
{
    public class ArenaBounds : MonoBehaviour
    {
        [SerializeField] private float _floorHeight = 0f;
        [SerializeField] private float _playerHeightOffset = 0f;
        [SerializeField] private LayerMask _platformMask = 0;
        [SerializeField] private List<Collider> _obstacles = new List<Collider>();

        [Tooltip("Standing height used for ceiling tests. arena.js uses 1.6.")]
        [SerializeField] private float _standingHeight = 1.6f;

        public float FloorY(Vector3 position)
        {
            float best = _floorHeight;

            if (_platformMask.value != 0)
            {
                Vector3 from = new Vector3(position.x, position.y + 0.1f, position.z);
                if (Physics.Raycast(from, Vector3.down, out RaycastHit hit, 50f, _platformMask,
                                    QueryTriggerInteraction.Ignore))
                {
                    if (hit.point.y > best && hit.point.y <= position.y + 0.1f)
                        best = hit.point.y;
                }
            }

            return best + _playerHeightOffset;
        }

        // arena.js resolves the platform slab against a standing profile and kills
        // upward velocity on a ceiling strike. Without this the jetpack climbs
        // straight through every platform, which matters more here than anywhere
        // else: vertical routing is the whole game.
        public void ResolveCeiling(Transform t, ref Vector3 velocity)
        {
            if (_platformMask.value == 0) return;

            if (!Physics.Raycast(t.position, Vector3.up, out RaycastHit hit, _standingHeight,
                                 _platformMask, QueryTriggerInteraction.Ignore))
                return;

            // Never push below the floor: a gap too small to stand in resolves to
            // the floor, not to a negative height.
            float target = Mathf.Max(hit.point.y - _standingHeight, FloorY(t.position));
            t.position = new Vector3(t.position.x, target, t.position.z);
            if (velocity.y > 0f) velocity.y = 0f;
        }

        public void ResolveCollisions(Transform t, float radius)
        {
            foreach (var c in _obstacles)
            {
                if (c == null) continue;
                Vector3 closest = c.ClosestPoint(t.position);
                Vector3 delta = t.position - closest;
                delta.y = 0f;
                float dist = delta.magnitude;
                if (dist > 0.0001f && dist < radius)
                    t.position += delta.normalized * (radius - dist);
            }
        }
    }
}
