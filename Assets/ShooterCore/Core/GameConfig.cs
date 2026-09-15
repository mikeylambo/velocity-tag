// GameConfig.cs
// Single source of truth for tuned values. Every number here is copied 1:1 from
// the JS config.js "Time Attack 2.0 Polish" build. Do NOT re-tune in code — edit
// the asset. Create via: right-click in Project -> Create -> Shooter -> Game Config.

using UnityEngine;

namespace ShooterCore
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Shooter/Game Config")]
    public class GameConfig : ScriptableObject
    {
        [Header("Arena")]
        public float arenaRadius = 25f;       // ARENA_RADIUS
        public float arenaHeight = 18f;       // ARENA_HEIGHT
        public float roundTime = 120f;        // ROUND_TIME / TIME_ATTACK_DURATION

        [Header("Movement")]
        public float moveSpeed = 7.5f;        // MOVE_SPEED
        public float airMoveSpeed = 6.0f;     // AIR_MOVE_SPEED
        public float turnSpeed = 2.2f;        // TURN_SPEED (radians/sec)
        public float gravity = 16.0f;         // GRAVITY
        public float verticalThrust = 15.0f;  // VERTICAL_THRUST
        public float verticalMax = 10.5f;     // VERTICAL_MAX
        public float dashSpeed = 18.0f;       // DASH_SPEED
        public float playerRadius = 0.4f;     // arena.js resolveCollisions(pos, vel, 0.4)

        // Dash duration/cost are hardcoded in 2.0 Polish player.js, not in its
        // config.js. golden_core_v02 exposes them as DASH_DURATION 0.16 / DASH_COST
        // 15; 2.0 Polish wins on value, golden core on the idea of exposing them.
        public float dashDuration = 0.18f;    // player.js dashTimer = 0.18
        public float dashCost = 20f;          // player.js fuel -= 20 per dash

        // DESCEND_SPEED has no 2.0 Polish equivalent — that build's thrust axis is
        // 0..1 with no down-thrust at all. Adopted from golden_core_v02 as a real
        // addition; this is the one cross-build VALUE in the movement set.
        public float descendSpeed = 8.2f;     // DESCEND_SPEED (golden_core_v02)

        // QUICK_DROP_SPEED exists in both builds with different behaviour.
        // 2.0 Polish player.js: velocity.y = -(DASH_SPEED) * 1.5, assigned outright.
        // golden_core_v02:      velocity.y = min(velocity.y, -QUICK_DROP_SPEED).
        // 2.0 Polish is the target build, so 27 (= 18 * 1.5) and a hard assign.
        public float quickDropSpeed = 27.0f;  // player.js DASH_SPEED * 1.5

        [Header("Fuel (player.js; 2.0 Polish hardcodes these)")]
        // golden_core_v02 exposes FUEL_REGEN_GROUND 34 / FUEL_REGEN_AIR 9, but
        // 2.0 Polish player.js uses 40 / 12, and it is the target build.
        public float fuelMax = 100f;
        public float fuelDrainThrust = 25f;   // player.js fuel -= 25 * dt while jetting
        public float fuelRegenGround = 40f;   // player.js regen when grounded
        public float fuelRegenAir = 12f;      // player.js regen when airborne

        [Header("Combat")]
        public float laserRange = 60f;        // LASER_RANGE
        // PLAYER_FIRE_COOLDOWN: config.js ships 1.0; the tuning pass resolved the
        // sport-arcade feel to 0.75 (the value the port was built around). Kept at
        // 0.75 here — flip to 1.0 in the asset if you want tactical pacing back.
        public float playerFireCooldown = 0.75f;

        [Header("Camera (desktop over-the-shoulder)")]
        public float camDist = 5.5f;          // CAM_DIST
        public float camHeight = 2.5f;        // CAM_HEIGHT
        public float camSide = 0.8f;          // CAM_SIDE

        [Header("Charges / Ghost")]
        public int playerMaxCharges = 3;      // PLAYER_MAX_CHARGES
        public int targetMaxCharges = 1;      // TARGET_MAX_CHARGES (strict 1-charge dummies)
        public float ghostDuration = 0.75f;   // GHOST_DURATION
        public float rechargeTime = 2.0f;     // RECHARGE_TIME

        [Header("Pads")]
        public float launchPadCooldown = 0.5f; // LAUNCH_PAD_COOLDOWN
        public float syncPadCooldown = 1.5f;   // SYNC_PAD_COOLDOWN

        [Header("Time Attack")]
        public int countdown = 3;               // TIME_ATTACK_COUNTDOWN
        public float targetRespawnTime = 0.75f; // TARGET_RESPAWN_TIME
        public float comboWindow = 3.0f;        // COMBO_WINDOW
        public int maxCombo = 5;                // MAX_COMBO
        public int playerHitPenalty = 250;      // PLAYER_HIT_PENALTY
        public int zeroIntegrityPenalty = 1000; // ZERO_INTEGRITY_PENALTY
        public float minTargetRespawnDistance = 5.0f; // MIN_TARGET_RESPAWN_DISTANCE_FROM_PLAYER

        [Header("Scoring - zone base")]
        public int chestScore = 100;
        public int helmetScore = 150;
        public int flankScore = 250;

        [Header("Scoring - route bonuses")]
        public int airborneBonus = 50;
        public int launchBonus = 75;
        public int quickDropBonus = 75;
        public float launchBonusWindow = 2.0f;    // launch tag counts within 2s of pad use
        public float quickDropBonusWindow = 1.5f; // tag within 1.5s of a quick-drop landing

        [Header("Medal ladder")]
        public int medalBronze = 3000;
        public int medalSilver = 6000;
        public int medalGold = 10000;
        public int medalPlatinum = 14000;
        public int medalSRank = 18000;
    }
}
