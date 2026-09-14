// TargetZone.cs
// Hit-zone marker, porting the JS mesh.userData = { tagType, parentTarget }.
// Put on each zone collider (Chest / Helmet / FlankPack) under a TargetDummy.
// Base scores live in TimeAttackRound (the scoring authority), same as round.js.

using UnityEngine;

namespace VelocityTag
{
    public class TargetZone : MonoBehaviour
    {
        public TagType zone;
        [HideInInspector] public TargetDummy parent;

        private void Awake()
        {
            parent = GetComponentInParent<TargetDummy>();
        }
    }
}
