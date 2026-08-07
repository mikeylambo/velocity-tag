// CooldownWeapon.cs
// Base class enforcing the "exactly one shot per cooldown" shot clock from
// combat.js. TagBlaster derives from this and implements OnFire(). Exposes a
// 0..1 cooldown fraction the reticle/HUD can read.

using UnityEngine;

namespace ShooterCore
{
    public abstract class CooldownWeapon : MonoBehaviour
    {
        [SerializeField] protected float _cooldown = 0.75f;
        protected float _lastFireTime = -999f;

        public bool IsReady => Time.time - _lastFireTime >= _cooldown;

        // 1 right after firing, decaying to 0 when ready again.
        public float CooldownRemaining01 =>
            Mathf.Clamp01(1f - (Time.time - _lastFireTime) / Mathf.Max(0.0001f, _cooldown));

        public bool TryFire()
        {
            if (!IsReady) return false;
            _lastFireTime = Time.time;
            OnFire();
            return true;
        }

        protected abstract void OnFire();
    }
}
