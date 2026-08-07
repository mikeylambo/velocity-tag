// ScorePopupFX.cs
// World-space score popups at the impact point ("+150 HELMET x3" style), per
// the handover FX spec. Uses the breakdown text TimeAttackRound already emits
// in ScoreAwardedEvent, colored by zone (chest red/pink, helmet gold, flank
// violet), floating up and fading. Pure presentation.

using UnityEngine;
using ShooterCore;

namespace VelocityTag
{
    public class ScorePopupFX : MonoBehaviour
    {
        [SerializeField] private Font _font; // wired by setup so the font ships in builds

        [Header("Zone colors (handover spec)")]
        [SerializeField] private Color _chestColor = new Color(1f, 0.3f, 0.4f);
        [SerializeField] private Color _helmetColor = new Color(1f, 0.85f, 0.2f);
        [SerializeField] private Color _flankColor = new Color(0.7f, 0.35f, 1f);

        [Header("Visual timings (presentation only)")]
        [SerializeField] private float _life = 0.9f;
        [SerializeField] private float _riseSpeed = 1.2f;

        private void OnEnable() => GameEventBus.On<ScoreAwardedEvent>(OnScore);
        private void OnDisable() => GameEventBus.Off<ScoreAwardedEvent>(OnScore);

        private void OnScore(ScoreAwardedEvent e)
        {
            Color c = e.Zone == ZoneType.Helmet ? _helmetColor
                    : e.Zone == ZoneType.Flank ? _flankColor
                    : _chestColor;

            var go = new GameObject("ScorePopup");
            go.transform.position = e.ImpactPoint + Vector3.up * 0.35f;

            var tm = go.AddComponent<TextMesh>();
            tm.text = "+" + e.AwardedPoints + "  " + e.BreakdownText;
            tm.font = _font;
            if (_font != null)
            {
                var mr = go.GetComponent<MeshRenderer>();
                mr.material = _font.material;
            }
            tm.fontSize = 48;
            tm.characterSize = 0.035f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = c;

            var anim = go.AddComponent<ScorePopupInstance>();
            anim.Init(_life, _riseSpeed);
        }
    }

    // One floating popup: rise, face the camera, fade, self-destroy.
    public class ScorePopupInstance : MonoBehaviour
    {
        private float _life = 0.9f;
        private float _riseSpeed = 1.2f;
        private float _age;
        private TextMesh _tm;

        public void Init(float life, float riseSpeed)
        {
            _life = life;
            _riseSpeed = riseSpeed;
            _tm = GetComponent<TextMesh>();
        }

        private void Update()
        {
            _age += Time.deltaTime;
            if (_age >= _life) { Destroy(gameObject); return; }

            transform.position += Vector3.up * (_riseSpeed * Time.deltaTime);

            var cam = Camera.main;
            if (cam != null)
                transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);

            if (_tm != null)
            {
                Color c = _tm.color;
                c.a = 1f - Mathf.Clamp01(_age / _life);
                _tm.color = c;
            }
        }
    }
}
