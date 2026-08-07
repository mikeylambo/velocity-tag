// WeaponBeamFX.cs
// Laser beam flash on WeaponFiredEvent: a LineRenderer from just under the
// aim origin to the hit point (or max-range point on a miss), fading out over
// a few frames. Rebuilt natively per the port README (FX were deliberately
// not ported). Pure presentation.

using UnityEngine;
using ShooterCore;

namespace VelocityTag
{
    public class WeaponBeamFX : MonoBehaviour
    {
        [SerializeField] private LineRenderer _line;
        [SerializeField] private Color _hitColor = new Color(1f, 0.85f, 0.2f);  // gold on tag
        [SerializeField] private Color _missColor = new Color(0.3f, 0.9f, 1f);  // cyan on miss
        [SerializeField] private float _beamLife = 0.10f; // fx.js beam life

        private float _timer;
        private Color _color;

        private void OnEnable() => GameEventBus.On<WeaponFiredEvent>(OnFired);
        private void OnDisable()
        {
            GameEventBus.Off<WeaponFiredEvent>(OnFired);
            if (_line != null) _line.enabled = false;
        }

        private void OnFired(WeaponFiredEvent e)
        {
            if (_line == null) return;

            // drop the start a touch so the beam reads like a muzzle, not the camera
            Vector3 start = e.Origin + e.Direction * 0.6f + Vector3.down * 0.18f;
            _line.positionCount = 2;
            _line.SetPosition(0, start);
            _line.SetPosition(1, e.HitPoint);

            _color = e.Hit ? _hitColor : _missColor;
            _timer = _beamLife;
            _line.enabled = true;
        }

        private void Update()
        {
            if (_line == null || !_line.enabled) return;

            _timer -= Time.deltaTime;
            float a = Mathf.Clamp01(_timer / _beamLife);
            Color c = new Color(_color.r, _color.g, _color.b, a);
            _line.startColor = c;
            _line.endColor = c;

            if (_timer <= 0f) _line.enabled = false;
        }
    }
}
