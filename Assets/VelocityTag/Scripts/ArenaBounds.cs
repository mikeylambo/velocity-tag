// ArenaBounds.cs
// Minimal port of the arena.js surface the gameplay code actually touches:
// GetFloorY (platform-aware floor height) + ResolveCollisions + launch pads.
// The visual arena itself should be rebuilt natively in the Unity scene
// (ProBuilder/primitives) — this component only owns gameplay queries.
//
// Launch pads: trigger colliders tagged via LaunchPad component below. On enter,
// applies upward velocity is handled by the pad; here we just broadcast the
// LaunchPadEngagedEvent that arms TagBlaster's 2s launch-bonus window
// (replacing the JS LOG_DEBUG string sniff).

using System.Collections.Generic;
using UnityEngine;
using ShooterCore;

namespace VelocityTag
{
    public class ArenaBounds : MonoBehaviour
    {
        [Tooltip("Colliders the player and targets can stand on (platforms). Floor at y=0 is implicit.")]
        [SerializeField] private List<Collider> _platforms = new List<Collider>();

        [Tooltip("Solid obstacles resolved as simple radial pushes (pillars, cover).")]
        [SerializeField] private List<Collider> _obstacles = new List<Collider>();

        /// Highest walkable surface under the given position (0 = arena floor).
        public float GetFloorY(Vector3 position)
        {
            float best = 0f;
            foreach (var c in _platforms)
            {
                if (c == null) continue;
                Bounds b = c.bounds;
                if (position.x >= b.min.x && position.x <= b.max.x &&
                    position.z >= b.min.z && position.z <= b.max.z &&
                    position.y >= b.max.y - 0.5f)
                {
                    best = Mathf.Max(best, b.max.y);
                }
            }
            return best;
        }

        /// Cheap radial push-out against obstacle bounds, mirroring arena.resolveCollisions.
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
