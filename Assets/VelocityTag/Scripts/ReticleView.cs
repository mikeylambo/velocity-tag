// ReticleView.cs
// "There is no reticle for the shot" is one of the hand-off's top open issues,
// and TagBlaster has been emitting ReticleStateEvent every frame with nothing
// listening. This consumes it.
//
// Behaviour is transcribed from the JS build, where the reticle is split across
// three files: camera.js builds it (0.1 sphere, wireframe, additive) and keeps
// it facing the camera; main.js positions it at the first aim hit or 25 units
// down the ray; combat.js recolours it green when the shot clock is ready and
// crimson while it is reloading.

using UnityEngine;
using ShooterCore;

namespace VelocityTag
{
    [DefaultExecutionOrder(100)]   // after locomotion and the camera rig have moved
    public class ReticleView : MonoBehaviour
    {
        [SerializeField] private Renderer _renderer;
        [SerializeField] private Transform _aimOrigin;
        [SerializeField] private Camera _billboardTarget;

        [Tooltip("main.js projects the reticle 25 units out when the ray hits nothing.")]
        [SerializeField] private float _projectionDistance = 25f;

        [SerializeField] private LayerMask _aimMask = ~0;

        [Header("Colours (combat.js update())")]
        [SerializeField] private Color _readyColour = new Color32(0x00, 0xFF, 0x66, 0xFF);
        [SerializeField] private Color _cooldownColour = new Color32(0xFF, 0x24, 0x5F, 0xFF);

        private MaterialPropertyBlock _block;
        private bool _isReady;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private void Awake() => _block = new MaterialPropertyBlock();

        private void OnEnable() => GameEventBus.On<ReticleStateEvent>(OnReticleState);
        private void OnDisable() => GameEventBus.Off<ReticleStateEvent>(OnReticleState);

        private void OnReticleState(ReticleStateEvent e) => _isReady = e.IsReady;

        public void SetAimOrigin(Transform origin)
        {
            if (origin != null) _aimOrigin = origin;
        }

        public void SetBillboardTarget(Camera camera)
        {
            if (camera != null) _billboardTarget = camera;
        }

        private void LateUpdate()
        {
            if (_aimOrigin == null) return;

            var ray = new Ray(_aimOrigin.position, _aimOrigin.forward);
            transform.position = Physics.Raycast(ray, out RaycastHit hit, _projectionDistance, _aimMask)
                ? hit.point
                : ray.origin + ray.direction * _projectionDistance;

            // camera.js keeps the reticle square to the viewer so it reads at a
            // constant size regardless of range.
            if (_billboardTarget != null)
                transform.rotation = Quaternion.LookRotation(
                    transform.position - _billboardTarget.transform.position);

            if (_renderer == null) return;
            Color c = _isReady ? _readyColour : _cooldownColour;
            _renderer.GetPropertyBlock(_block);
            _block.SetColor(BaseColorId, c);
            _block.SetColor(ColorId, c);
            _renderer.SetPropertyBlock(_block);
        }
    }
}
