// CooldownWeapon.cs
// Direct generalization of Velocity Tag's combat.js CombatManager.executeAimFire() —
// the one piece of combat logic in either codebase that's actually implemented and
// working: cooldown gate -> raycast from aim origin -> filter valid targets
// (not ghosted/depleted) -> emit hit report with contextual bonus flags.
//
// Swapped from a single hardcoded blaster to reading a WeaponDefinition asset,
// so the same component serves any of Neon Galactic's 12 planned weapons or
// Velocity Tag's tag-blaster without rewriting fire logic per weapon.
//
// Bonus-window timing (launch pad, quick drop) is left as hooks — port the
// specific timing rules from combat.js's lastLaunchTime/lastQuickDropLandTime
// per-game, since those are gameplay-specific, not structural.

using UnityEngine;
using ShooterCore;

namespace ShooterCore.Combat
{
    public class CooldownWeapon : MonoBehaviour
    {
        [SerializeField] private WeaponDefinition _weapon;
        [SerializeField] private Transform _aimOrigin;       // camera or controller-tip transform
        [SerializeField] private LayerMask _hittableMask;
        [SerializeField] private MatchStateMachine _matchState;

        private float _cooldownRemaining;

        public bool IsReady => _cooldownRemaining <= 0f && (_matchState == null || _matchState.IsPlaying);

        private void Update()
        {
            if (_cooldownRemaining > 0f) _cooldownRemaining -= Time.deltaTime;
        }

        /// Call from input handling (mouse click / VR trigger) — mirrors
        /// EventBus.on('INPUT_FIRE', () => this.executeAimFire()) from combat.js.
        public void TryFire()
        {
            if (!IsReady) return;
            _cooldownRemaining = _weapon.fireCooldown;

            Ray ray = new Ray(_aimOrigin.position, _aimOrigin.forward);
            GameEventBus.Emit(new WeaponFiredEvent
            {
                WeaponId = _weapon.weaponId,
                Origin = ray.origin,
                Direction = ray.direction
            });

            if (Physics.Raycast(ray, out RaycastHit hit, _weapon.range, _hittableMask))
            {
                var hitTag = ResolveHitZone(hit); // e.g. "chest" | "helmet" | "flank"
                GameEventBus.Emit(new TargetHitEvent
                {
                    Target = hit.collider.gameObject,
                    TagType = hitTag,
                    IsAirborne = false,   // wire to player controller's grounded state
                    IsBonusWindow = false // wire to your launch-pad / quick-drop timers
                });
            }
        }

        private string ResolveHitZone(RaycastHit hit)
        {
            // Port zone-tagging from Velocity Tag's targets.js hitZones
            // (userData.tagType per collider) here, per-project.
            return hit.collider.CompareTag("Helmet") ? "helmet"
                 : hit.collider.CompareTag("Flank") ? "flank"
                 : "chest";
        }
    }
}
