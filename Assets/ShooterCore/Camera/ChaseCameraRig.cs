// ChaseCameraRig.cs
// Desktop-only over-the-shoulder camera using CAM_DIST / CAM_HEIGHT / CAM_SIDE.
// Gimbal-safe (LookRotation with an explicit up). In VR the XR Origin rig
// replaces this entirely — leave it disabled on Quest builds.

using UnityEngine;

namespace ShooterCore
{
    public class ChaseCameraRig : MonoBehaviour
    {
        [SerializeField] private Transform _target;
        [SerializeField] private GameConfig _config;
        [SerializeField] private float _followLerp = 10f;

        private void LateUpdate()
        {
            if (_target == null) return;

            float dist = _config != null ? _config.camDist : 5.5f;
            float height = _config != null ? _config.camHeight : 2.5f;
            float side = _config != null ? _config.camSide : 0.8f;

            Vector3 desired = _target.position
                              - _target.forward * dist
                              + _target.right * side
                              + Vector3.up * height;

            float t = 1f - Mathf.Exp(-_followLerp * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, desired, t);

            Vector3 look = _target.position + Vector3.up * (height * 0.6f);
            Vector3 dir = look - transform.position;
            if (dir.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
        }
    }
}
