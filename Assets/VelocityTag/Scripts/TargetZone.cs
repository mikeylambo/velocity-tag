// TargetZone.cs
// Hit-zone marker, porting the JS mesh.userData = { tagType, parentTarget }.
// Put one on each zone collider (Chest / Helmet / Flank) under a TargetDummy.
// Base scores live in TimeAttackRound (the scoring authority), same as round.js.

using UnityEngine;

namespace VelocityTag
{
    [RequireComponent(typeof(Collider))]
    public class TargetZone : MonoBehaviour
    {
        [SerializeField] private ZoneType _zone = ZoneType.Chest;

        public ZoneType Zone => _zone;
        public TargetDummy Parent { get; private set; }

        private Collider _collider;

        private void Awake()
        {
            _collider = GetComponent<Collider>();
            Parent = GetComponentInParent<TargetDummy>();
        }

        // Disabled during the ghost window so the target is briefly invulnerable.
        public void SetEnabled(bool on)
        {
            if (_collider == null) _collider = GetComponent<Collider>();
            if (_collider != null) _collider.enabled = on;
        }
    }
}
