// TargetFX.cs
// Feel Pass 01 target feedback, per the developer handover FX spec:
//   - body flash by zone (chest red/pink, helmet gold, flank violet)
//   - short recoil/stagger jab
//   - ghost shimmer while invulnerable
//   - respawn pulse when the target comes back live
// Lives on the TargetDummy prefab root. Pure presentation driven by
// TargetTaggedEvent (filtered to this dummy) and TargetDummy.IsGhosting —
// no gameplay state, timers here are visual-only.

using UnityEngine;
using ShooterCore;

namespace VelocityTag
{
    [RequireComponent(typeof(TargetDummy))]
    public class TargetFX : MonoBehaviour
    {
        [Header("Zone flash colors (handover spec)")]
        [SerializeField] private Color _chestFlash = new Color(1f, 0.3f, 0.4f);   // red/pink
        [SerializeField] private Color _helmetFlash = new Color(1f, 0.85f, 0.2f); // gold
        [SerializeField] private Color _flankFlash = new Color(0.7f, 0.35f, 1f);  // violet

        [Header("Visual timings (presentation only)")]
        [SerializeField] private float _flashDuration = 0.25f;
        [SerializeField] private float _respawnPulseDuration = 0.2f;

        private TargetDummy _dummy;
        private Renderer[] _renderers;
        private MaterialPropertyBlock _mpb;
        private Vector3 _baseScale;
        private Quaternion _baseRotation;

        private float _flashTimer;
        private Color _flashColor = Color.white;
        private Vector3 _recoilAxis = Vector3.right;
        private float _pulseTimer;
        private bool _wasGhosting;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int LegacyColorId = Shader.PropertyToID("_Color");

        private void Awake()
        {
            _dummy = GetComponent<TargetDummy>();
            _renderers = GetComponentsInChildren<Renderer>(true);
            _mpb = new MaterialPropertyBlock();
            _baseScale = transform.localScale;
            _baseRotation = transform.localRotation;
        }

        private void OnEnable() => GameEventBus.On<TargetTaggedEvent>(OnTagged);

        private void OnDisable()
        {
            GameEventBus.Off<TargetTaggedEvent>(OnTagged);
            ApplyTint(Color.white);
            transform.localScale = _baseScale;
            transform.localRotation = _baseRotation;
        }

        private void OnTagged(TargetTaggedEvent e)
        {
            if (e.Target != _dummy) return;

            _flashColor = e.Zone == ZoneType.Helmet ? _helmetFlash
                        : e.Zone == ZoneType.Flank ? _flankFlash
                        : _chestFlash;
            _flashTimer = _flashDuration;

            Vector3 toImpact = e.ImpactPoint - transform.position;
            toImpact.y = 0f;
            _recoilAxis = Vector3.Cross(Vector3.up, toImpact.sqrMagnitude > 0.001f ? toImpact.normalized : Vector3.forward);
        }

        private void Update()
        {
            bool ghosting = _dummy != null && _dummy.IsGhosting;

            // respawn pulse on the ghost -> live transition
            if (_wasGhosting && !ghosting) _pulseTimer = _respawnPulseDuration;
            _wasGhosting = ghosting;

            // recoil/stagger: brief tilt away from the impact while flashing
            if (_flashTimer > 0f)
            {
                _flashTimer -= Time.deltaTime;
                float k = Mathf.Clamp01(_flashTimer / _flashDuration);
                transform.localRotation = _baseRotation * Quaternion.AngleAxis(10f * k, _recoilAxis);
                ApplyTint(Color.Lerp(Color.white, _flashColor, k));
                if (_flashTimer <= 0f) transform.localRotation = _baseRotation;
            }
            else if (ghosting)
            {
                // shimmer: brightness oscillation while invulnerable
                float s = 0.55f + 0.35f * Mathf.PingPong(Time.time * 6f, 1f);
                ApplyTint(new Color(s, s, s, 1f));
            }
            else if (_pulseTimer > 0f)
            {
                _pulseTimer -= Time.deltaTime;
                float k = Mathf.Clamp01(_pulseTimer / _respawnPulseDuration);
                transform.localScale = _baseScale * (1f + 0.15f * k);
                ApplyTint(Color.Lerp(Color.white, new Color(0.7f, 1f, 0.8f), k));
                if (_pulseTimer <= 0f) transform.localScale = _baseScale;
            }
            else
            {
                ApplyTint(Color.white);
            }
        }

        private void ApplyTint(Color c)
        {
            if (_renderers == null) return;
            foreach (var r in _renderers)
            {
                if (r == null) continue;
                r.GetPropertyBlock(_mpb);
                _mpb.SetColor(BaseColorId, c);
                _mpb.SetColor(LegacyColorId, c);
                r.SetPropertyBlock(_mpb);
            }
        }
    }
}
