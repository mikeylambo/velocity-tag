// WeaponDefinition.cs
// Merges two half-systems into one working one:
//   - Neon Galactic's weapons/WeaponDefs.js: a real 12-weapon DATA roster
//     (energyType, tier, chargeable/sustained/homing/piercing flags) with zero
//     firing logic behind it — every entry is metadata only.
//   - Velocity Tag's combat.js: real, working firing logic (cooldown gate,
//     raycast against hitZones, contextual bonus windows) but hardcoded to a
//     single blaster — no data-driven weapon concept at all.
//
// This is a ScriptableObject per weapon (so each of the 12 becomes its own
// asset, editable without touching code) feeding the single CooldownWeapon
// firing component below, instead of Neon Galactic's roster needing 12
// hand-written firing implementations.

using UnityEngine;

namespace ShooterCore.Combat
{
    public enum EnergyType { Kinetic, Plasma, Exotic }
    public enum UnlockTier { Starting, Early, Mid, Late }

    [CreateAssetMenu(fileName = "NewWeapon", menuName = "Shooter/Weapon Definition")]
    public class WeaponDefinition : ScriptableObject
    {
        public string weaponId;
        public string displayName;
        public EnergyType energyType;
        public UnlockTier tier;

        [Header("Firing")]
        public float fireCooldown = 0.75f;
        public float range = 60f;
        public int damage = 50;

        [Header("Behavior flags (from Neon Galactic's roster — wire up as needed)")]
        public bool homing;
        public bool chain;
        public bool chargeable;
        public bool sustained;
        public bool pull;
        public bool piercing;
        public bool isUltimate;

        [Header("Scoring bonuses (from Velocity Tag's contextual bonus system)")]
        public int chestHitScore = 100;
        public int helmetHitScore = 150;
        public int flankHitScore = 250;
        public int airborneBonus = 50;
        public int launchPadBonus = 75;
        public int quickDropBonus = 75;
    }
}
