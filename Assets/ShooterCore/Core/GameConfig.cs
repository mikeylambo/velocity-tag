// GameConfig.cs
// Generalizes Velocity Tag's src/config.js CONFIG object — the single flat object
// every system (player.js, combat.js, round.js) reads tunables from — into a
// Unity ScriptableObject.
//
// This buys you two things the JS version has to fake:
//   1. Velocity Tag's main.js wires a lil-gui runtime tuner directly to CONFIG
//      fields (MOVE_SPEED, DASH_SPEED, VERTICAL_THRUST) for live playt¬est tuning.
//      A ScriptableObject gets that for free in the Unity Inspector, in Play Mode,
//      with no extra GUI library.
//   2. Per-game variants become separate assets (VelocityTagConfig.asset,
//      NeonGalacticConfig.asset) that all reference this same class, instead of
//      two divergent copy-pasted config.js files drifting apart over time.
//
// Values below are ported directly from Velocity Tag's actual config.js, since
// that's the one build with real, playtested numbers. Neon Galactic's config
// doesn't exist yet (only the CAM_DIST/CAM_HEIGHT/CAM_SIDE ship-camera constants
// referenced in its docs) — extend this asset per-project rather than treating
// these defaults as gospel for a 6DOF ship instead of a jetpack avatar.

using UnityEngine;

namespace ShooterCore
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Shooter/Game Config")]
    public class GameConfig : ScriptableObject
    {
        [Header("Arena")]
        public float arenaRadius = 25f;
        public float arenaHeight = 18f;
        public float roundTime = 120f;

        [Header("Movement")]
        public float moveSpeed = 7.5f;
        public float airMoveSpeed = 6.0f;
        public float turnSpeed = 2.2f;
        public float gravity = 16.0f;
        public float verticalThrust = 15.0f;
        public float verticalMax = 10.5f;
        public float dashSpeed = 18.0f;

        [Header("Combat")]
        public float laserRange = 60f;
        public float fireCooldown = 0.75f;
        public float playerFireCooldown = 0.75f;

        [Header("Camera Rig (chase/over-the-shoulder)")]
        public float camDist = 5.5f;
        public float camHeight = 2.5f;
        public float camSide = 0.8f;
        public float camPitchOffset = -0.15f;

        [Header("Suit / Health Charges")]
        public int playerMaxCharges = 3;
        public int targetMaxCharges = 1;
        public float ghostDuration = 0.75f;
        public float rechargeTime = 2.0f;

        [Header("Scoring")]
        public float comboWindow = 3.0f;
        public int maxCombo = 5;
        public int playerHitPenalty = 250;
        public int zeroIntegrityPenalty = 1000;
        public float minTargetRespawnDistance = 5.0f;

        [Header("Medal Ladder")]
        public int bronzeThreshold = 3000;
        public int silverThreshold = 6000;
        public int goldThreshold = 10000;
        public int platinumThreshold = 14000;
        public int sRankThreshold = 18000;
    }
}
