// TargetDummy.cs
// Port of one target from src/targets.js: strict 1-charge dummy with
// hit flash (80ms zone flashes -> ghost wireframe), recoil spin, 0.75s ghost
// window, then instant reposition to a valid spawn >= 5 units from the player.
// Patrol: planar drift, bounce at arenaRadius-2, yaw lerps toward velocity (dt*5).
//
// Materials: assign zone renderers + a ghost material in the Inspector.
// FX (flash colors per zone) should be Unity materials, not ported hex codes —
// but the reference palette from targets.js is noted on each field.

using UnityEngine;
using ShooterCore;

namespace VelocityTag
{
    public class TargetDummy : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private GameConfig _config;

        [Header("Hostile fire")]
        [Tooltip("The port README kept dummies passive for the first build. With this " +
                 "off, PlayerHitEvent is never emitted and the whole suit-charge system " +
                 "— penalties, zero-integrity reset, recharge gates — is unreachable.")]
        [SerializeField] private bool _returnsFire = true;

        [Header("Zone renderers")]
        [SerializeField] private Renderer _body;
        [SerializeField] private Renderer _chest;   // team visor: red 0xff245f / blue 0x00eaff
        [SerializeField] private Renderer _head;    // gold 0xffd76a
        [SerializeField] private Renderer _back;    // team pack color

        [Header("Materials")]
        [SerializeField] private Material _matBody;
        [SerializeField] private Material _matChestNormal;
        [SerializeField] private Material _matHeadNormal;
        [SerializeField] private Material _matBackNormal;
        [SerializeField] private Material _matHitChest;   // 0xff66cc
        [SerializeField] private Material _matHitHead;    // white
        [SerializeField] private Material _matHitBack;    // 0xbf55ec
        [SerializeField] private Material _matGhost;      // cyan wireframe-style, 40% alpha

        public bool IsGhosted { get; private set; }
        public int Charges { get; private set; } = 1;

        private float _ghostTimer;
        private Vector3 _velocity;
        private Vector3 _spinVel;
        private float _speed;
        private float _fireTimer;
        private TargetSpawnNetwork _spawner;
        private ArenaBounds _arena;
        private const float FlashDuration = 0.08f;
        private float _flashTimer;

        public void Init(TargetSpawnNetwork spawner, ArenaBounds arena, float initialAngle)
        {
            _spawner = spawner;
            _arena = arena;
            _speed = 2.0f + Random.value * 1.5f;
            _velocity = new Vector3(Mathf.Cos(initialAngle), 0f, Mathf.Sin(initialAngle)) * _speed;
            Charges = _config != null ? _config.targetMaxCharges : 1;

            // Stagger the first shot by spawn phase so six dummies do not volley in
            // unison. The interval itself is untouched — only the phase is offset.
            float interval = _config != null ? _config.hostileFireInterval : 2.5f;
            _fireTimer = interval * (initialAngle / (Mathf.PI * 2f));
        }

        private void OnEnable() => GameEventBus.On<TargetHitConfirmedEvent>(OnHitConfirmed);
        private void OnDisable() => GameEventBus.Off<TargetHitConfirmedEvent>(OnHitConfirmed);

        private void OnHitConfirmed(TargetHitConfirmedEvent e)
        {
            if (e.Target != this || IsGhosted) return;

            Charges--;
            IsGhosted = true;
            _ghostTimer = _config != null ? _config.ghostDuration : 0.75f;
            _flashTimer = FlashDuration;

            // Instant zone flash, settles into ghost material after 80ms (JS setTimeout)
            _chest.sharedMaterial = _matHitChest;
            _head.sharedMaterial = _matHitHead;
            _back.sharedMaterial = _matHitBack;

            // Recoil spin, same random range as JS
            _spinVel = new Vector3((Random.value - 0.5f) * 8f, (Random.value - 0.5f) * 8f, 0f);

            SetZoneCollidersEnabled(false);   // ghost = untaggable
        }

        private void SetZoneCollidersEnabled(bool enabled)
        {
            foreach (var z in GetComponentsInChildren<TargetZone>())
            {
                var c = z.GetComponent<Collider>();
                if (c != null) c.enabled = enabled;
            }
        }

        public void ResetAndReposition(Vector3 spawnPos)
        {
            IsGhosted = false;
            Charges = _config != null ? _config.targetMaxCharges : 1;
            transform.rotation = Quaternion.identity;
            _spinVel = Vector3.zero;

            _body.sharedMaterial = _matBody;
            _chest.sharedMaterial = _matChestNormal;
            _head.sharedMaterial = _matHeadNormal;
            _back.sharedMaterial = _matBackNormal;
            SetZoneCollidersEnabled(true);

            transform.position = spawnPos;
            float angle = Mathf.Atan2(-spawnPos.z, -spawnPos.x);
            _velocity = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * _speed;
        }

        /// Emits the claim; TagBlaster decides whether it connects, exactly as
        /// combat.js splits shooter from resolver.
        private void TickHostileFire(Vector3 playerPos, float dt)
        {
            if (!_returnsFire || _config == null) return;

            _fireTimer -= dt;
            if (_fireTimer > 0f) return;
            _fireTimer = _config.hostileFireInterval;

            Vector3 muzzle = transform.position + Vector3.up * 1.1f;   // chest height
            Vector3 aimCentre = playerPos + Vector3.up * _config.hostileAimCentreHeight;

            float range = _config.hostileFireRange;
            float distance = Vector3.Distance(muzzle, aimCentre);
            if (distance > range) return;

            // Scatter grows with range so distant fire reads as pressure rather
            // than an unavoidable tax. Shots that miss still draw, and still
            // telegraph where the dummy is.
            float scatter = _config.hostileAimSpread * Mathf.Clamp01(distance / Mathf.Max(range, 0.01f));
            Vector3 aim = aimCentre + Random.insideUnitSphere * scatter;

            GameEventBus.Emit(new HostileFireEvent
            {
                Origin = muzzle,
                TargetAim = aim,
                Shooter = this,
            });
        }

        public void Tick(Vector3 playerPos, bool isPlaying, float dt)
        {
            float floorY = _arena != null ? _arena.GetFloorY(transform.position) : 0f;
            if (transform.position.y < floorY + 1.0f)
            {
                var p = transform.position; p.y = floorY + 1.0f; transform.position = p;
                if (_velocity.y < 0f) _velocity.y = 0f;
            }

            if (_arena != null) _arena.ResolveCollisions(transform, 0.4f);

            if (IsGhosted)
            {
                _ghostTimer -= dt;

                if (_flashTimer > 0f)
                {
                    _flashTimer -= dt;
                    if (_flashTimer <= 0f)
                    {
                        _body.sharedMaterial = _matGhost;
                        _chest.sharedMaterial = _matGhost;
                        _head.sharedMaterial = _matGhost;
                        _back.sharedMaterial = _matGhost;
                    }
                }

                transform.Rotate(_spinVel.x * dt * Mathf.Rad2Deg, _spinVel.y * dt * Mathf.Rad2Deg, 0f, Space.Self);

                if (_ghostTimer <= 0f)
                    _spawner.RepositionTarget(this, playerPos);
                return;
            }

            if (!isPlaying) return;

            transform.position += _velocity * dt;
            float arenaRadius = _config != null ? _config.arenaRadius : 25f;
            Vector2 planar = new Vector2(transform.position.x, transform.position.z);
            if (planar.magnitude > arenaRadius - 2f)
            {
                _velocity.x *= -1f;
                _velocity.z *= -1f;
            }

            TickHostileFire(playerPos, dt);

            // Yaw toward travel direction (JS lerp dt*5)
            float targetYaw = Mathf.Atan2(-_velocity.x, -_velocity.z) * Mathf.Rad2Deg;
            float currentYaw = transform.eulerAngles.y;
            transform.rotation = Quaternion.Euler(0f, Mathf.LerpAngle(currentYaw, targetYaw, dt * 5f), 0f);
        }
    }
}
