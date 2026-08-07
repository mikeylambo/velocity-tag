// WeaponDefinition.cs
// Optional data container for a hitscan weapon. TagBlaster can read its cooldown
// and range from here or from GameConfig; GameConfig wins when both are assigned.

using UnityEngine;

namespace ShooterCore
{
    [CreateAssetMenu(fileName = "WeaponDefinition", menuName = "Shooter/Weapon Definition")]
    public class WeaponDefinition : ScriptableObject
    {
        public float cooldown = 0.75f;
        public float range = 60f;
        public LayerMask hitMask = ~0;
    }
}
