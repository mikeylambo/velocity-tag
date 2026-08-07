// ArenaBounds.cs
// Floor query + obstacle push-out for the arena. JetpackLocomotion calls FloorY()
// to clamp landing height (flat floor, plus optional raycast onto platform tops)
// and ResolveCollisions() to slide the player out of pillars/cover.

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
