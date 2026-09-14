// ArenaBounds.cs
// Line-faithful port of src/arena.js's gameplay queries: getFloorY,
// resolveCollisions, and the two pad trigger gates.
//
// This replaces an earlier collider-list approximation that diverged in three
// ways that all matter for a game built on vertical routing:
//   1. No ceiling collision at all — the player flew up through platforms.
//      arena.js resolves the slab against a 1.6m standing height, kills upward
//      velocity on a ceiling strike, and slides laterally otherwise.
//   2. getFloorY caught the player from any height, using a 0.5 window that
//      appears in no source. arena.js catches only within [y - 0.25, y + 1.5).
//   3. Pad cooldowns were per-pad, so four pads could be chained. arena.js
//      keeps ONE launch timer and ONE sync timer for the whole arena, which is
//      what LAUNCH_PAD_COOLDOWN and SYNC_PAD_COOLDOWN were tuned against.
//
// Geometry is held as map data (slabs and columns), not as collider bounds,
// so the queries read the same way they do in arena.js. The scene builder fills
// these from src/maps/*.js. Visual meshes are independent of them.

using System;
using System.Collections.Generic;
using UnityEngine;
using ShooterCore;

namespace VelocityTag
{
    public class ArenaBounds : MonoBehaviour
    {
        [Serializable]
        public struct PlatformSlab
        {
            public float minX, maxX, minZ, maxZ;
            public float y;              // walkable top surface
        }

        [Serializable]
        public struct PillarColumn
        {
            public float x, z;
            public float radius, height;
        }

        [SerializeField] private List<PlatformSlab> _platforms = new List<PlatformSlab>();
        [SerializeField] private List<PillarColumn> _pillars = new List<PillarColumn>();

        // arena.js constants
        private const float PlayerHeight = 1.6f;    // upper head boundary above foot origin
        private const float SlabThickness = 0.4f;   // box height, for the ceiling face
        private const float LandingCatchBelow = 0.25f;
        private const float LandingCatchAbove = 1.5f;

        // ONE timer each, arena-wide — see note 3 above.
        private float _lastLaunch = -999f;
        private float _lastSync = -999f;

        public IReadOnlyList<PlatformSlab> Platforms => _platforms;
        public IReadOnlyList<PillarColumn> Pillars => _pillars;

        public void SetGeometry(List<PlatformSlab> platforms, List<PillarColumn> pillars)
        {
            _platforms = platforms;
            _pillars = pillars;
        }

        /// Highest walkable surface under this position (0 = arena floor).
        public float GetFloorY(Vector3 position)
        {
            float groundY = 0f;
            foreach (var plat in _platforms)
            {
                if (position.x <= plat.minX || position.x >= plat.maxX ||
                    position.z <= plat.minZ || position.z >= plat.maxZ) continue;

                // Catch only within the landing window; outside it the player
                // passes the slab rather than snapping to it from any height.
                if (position.y >= plat.y - LandingCatchBelow &&
                    position.y < plat.y + LandingCatchAbove)
                    groundY = Mathf.Max(groundY, plat.y);
            }
            return groundY;
        }

        /// Targets resolve without a velocity to zero, matching arena.js's null.
        public void ResolveCollisions(Transform t, float radius)
        {
            Vector3 pos = t.position;
            Vector3 ignored = Vector3.zero;
            ResolveCollisions(ref pos, ref ignored, radius, applyVelocity: false);
            t.position = pos;
        }

        public void ResolveCollisions(ref Vector3 pos, ref Vector3 velocity, float radius)
            => ResolveCollisions(ref pos, ref velocity, radius, applyVelocity: true);

        private void ResolveCollisions(ref Vector3 pos, ref Vector3 velocity, float radius, bool applyVelocity)
        {
            // 1. Cylindrical pillars — radial push-out, only below the column top.
            foreach (var p in _pillars)
            {
                float dx = pos.x - p.x;
                float dz = pos.z - p.z;
                float dist = Mathf.Sqrt(dx * dx + dz * dz);
                float minSafe = p.radius + radius;

                if (dist < minSafe && pos.y < p.height)
                {
                    float angle = Mathf.Atan2(dz, dx);
                    pos.x = p.x + Mathf.Cos(angle) * minSafe;
                    pos.z = p.z + Mathf.Sin(angle) * minSafe;
                }
            }

            // 2. Platform slabs — ceiling strike and lateral wall slide.
            foreach (var plat in _platforms)
            {
                float platTop = plat.y;
                float platBottom = plat.y - SlabThickness;

                float minX = plat.minX - radius, maxX = plat.maxX + radius;
                float minZ = plat.minZ - radius, maxZ = plat.maxZ + radius;

                if (pos.x <= minX || pos.x >= maxX || pos.z <= minZ || pos.z >= maxZ) continue;

                // Above the landing snap window? Then this is a floor, not an obstacle.
                if (pos.y >= platTop - LandingCatchBelow) continue;

                // Does the standing profile reach the underside of the slab?
                if (pos.y + PlayerHeight <= platBottom) continue;

                float insetX = Mathf.Min(pos.x - minX, maxX - pos.x);
                float insetZ = Mathf.Min(pos.z - minZ, maxZ - pos.z);
                float insetY = (pos.y + PlayerHeight) - platBottom;

                if (insetY < insetX && insetY < insetZ)
                {
                    // Ceiling impact: stop just under the slab and kill the climb.
                    pos.y = platBottom - PlayerHeight;
                    if (applyVelocity && velocity.y > 0f) velocity.y = 0f;
                }
                else if (insetX < insetZ)
                {
                    pos.x = (pos.x - minX < maxX - pos.x) ? minX : maxX;
                }
                else
                {
                    pos.z = (pos.z - minZ < maxZ - pos.z) ? minZ : maxZ;
                }
            }
        }

        // --- Arena-wide pad gates (one timer each, as in arena.js) ---

        /// True if the arena-wide launch cooldown has elapsed, consuming it.
        public bool TryConsumeLaunch(GameConfig config, Vector3 playerPos)
        {
            if (Time.time - _lastLaunch < config.launchPadCooldown) return false;
            // arena.js also gates on height above the local floor.
            if (playerPos.y >= GetFloorY(playerPos) + 1.8f) return false;
            _lastLaunch = Time.time;
            return true;
        }

        /// True if the arena-wide sync cooldown has elapsed, consuming it.
        public bool TryConsumeSync(GameConfig config, Vector3 playerPos)
        {
            if (Time.time - _lastSync < config.syncPadCooldown) return false;
            if (playerPos.y >= 1.5f) return false;
            _lastSync = Time.time;
            return true;
        }
    }
}
