using UnityEngine;

namespace StrangePlaces.DemoQuantumCollapse
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public sealed class PlayerController2D : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 8f;
        [SerializeField] private float extraGravityScale = 3f;
        [Tooltip("水平移动使用力来逼近期望速度，避免每帧写死 velocity.x 导致无法被碰撞/外力推开。")]
        [SerializeField] private float horizontalSpeedGain = 40f;
        [SerializeField] private float maxHorizontalForce = 600f;

        [Header("Jump（物理驱动）")]
        [Tooltip("起跳瞬间的向上冲量（Impulse），越大跳得越高。")]
        [SerializeField] private float jumpImpulse = 12f;
        [Tooltip("按住跳跃键时持续施加的向上力（Force）。用于“长按跳更高”。")]
        [SerializeField] private float jumpHoldForce = 22f;
        [Tooltip("长按起跳的最长持续时间（秒）。")]
        [SerializeField] private float jumpHoldSeconds = 0.12f;
        [Tooltip("松开跳跃键时（仍在上升）施加的额外下压力倍数，用于“短按跳更矮”。")]
        [SerializeField] private float jumpCutMultiplier = 2.2f;
        [Tooltip("下落时额外重力倍数，用于更利落的下落手感。")]
        [SerializeField] private float fallGravityMultiplier = 2.6f;

        [Header("Ground Check")]
        [SerializeField] private float groundCheckDistance = 0.08f;
        [Tooltip("地面检测的水平偏移（使用多条射线避免站在小平台边缘时中心射线打空）。")]
        [SerializeField] private float groundCheckHorizontalOffset = 0.35f;
        [SerializeField] private LayerMask groundMask = ~0;

        [Header("Respawn")]
        [SerializeField] private float fallY = -12f;

        private Rigidbody2D _rigidbody2D;
        private Collider2D _collider2D;
        private Vector3 _spawnPosition;
        private readonly RaycastHit2D[] _groundHits = new RaycastHit2D[8];
        private float _jumpHoldRemaining;
        private bool _jumpHeldLastFrame;

        public Vector2 AimDirection { get; private set; } = Vector2.right;
        public float MoveAxis { get; private set; }

        private void Awake()
        {
            _rigidbody2D = GetComponent<Rigidbody2D>();
            _collider2D = GetComponent<Collider2D>();
            _spawnPosition = transform.position;

            _rigidbody2D.freezeRotation = true;
            _rigidbody2D.gravityScale = extraGravityScale;
            _rigidbody2D.interpolation = RigidbodyInterpolation2D.Interpolate;
            _rigidbody2D.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        private void Update()
        {
            UpdateAimDirection();
            HandleJump();

            if (transform.position.y < fallY)
            {
                Respawn();
            }
        }

        private void FixedUpdate()
        {
            float moveAxis = Input.GetAxisRaw("Horizontal");
            MoveAxis = moveAxis;
            float desiredVx = moveAxis * moveSpeed;
            float currentVx = _rigidbody2D.velocity.x;

            float gain = Mathf.Max(0f, horizontalSpeedGain);
            float maxForce = Mathf.Max(0f, maxHorizontalForce);

            // Drive towards desired horizontal speed using forces so collisions/impulses can push the player.
            float accel = (desiredVx - currentVx) * gain;
            float forceX = Mathf.Clamp(accel * _rigidbody2D.mass, -maxForce, maxForce);
            _rigidbody2D.AddForce(new Vector2(forceX, 0f), ForceMode2D.Force);

            ApplyJumpForces();
        }

        private void HandleJump()
        {
            bool jumpDown = Input.GetButtonDown("Jump");
            bool jumpHeld = Input.GetButton("Jump");

            if (!jumpDown)
            {
                return;
            }

            if (!IsGrounded())
            {
                return;
            }

            float impulse = Mathf.Max(0f, jumpImpulse);
            if (impulse > 0.0001f)
            {
                _rigidbody2D.AddForce(Vector2.up * impulse, ForceMode2D.Impulse);
            }

            _jumpHoldRemaining = Mathf.Max(0f, jumpHoldSeconds);
            _jumpHeldLastFrame = jumpHeld;
        }

        private void ApplyJumpForces()
        {
            bool jumpHeld = Input.GetButton("Jump");
            float vy = _rigidbody2D.velocity.y;

            // 1) 长按起跳：短时间内持续向上施力（Force）
            if (_jumpHoldRemaining > 0f && jumpHeld && vy >= -0.05f)
            {
                float holdForce = Mathf.Max(0f, jumpHoldForce);
                if (holdForce > 0.0001f)
                {
                    _rigidbody2D.AddForce(Vector2.up * holdForce, ForceMode2D.Force);
                }

                _jumpHoldRemaining = Mathf.Max(0f, _jumpHoldRemaining - Time.fixedDeltaTime);
            }
            else if (!jumpHeld)
            {
                _jumpHoldRemaining = 0f;
            }

            // 2) 松手截断：上升途中松开跳跃键，施加额外下压力
            if (_jumpHeldLastFrame && !jumpHeld && vy > 0.01f)
            {
                float cut = Mathf.Max(0f, jumpCutMultiplier - 1f);
                if (cut > 0.0001f)
                {
                    // Add extra gravity-like force for this fixed step.
                    Vector2 g = Physics2D.gravity * _rigidbody2D.gravityScale;
                    _rigidbody2D.AddForce(g * (_rigidbody2D.mass * cut), ForceMode2D.Force);
                }
            }

            // 3) 下落加速：下落时额外重力，避免“飘”
            if (vy < -0.01f)
            {
                float fall = Mathf.Max(0f, fallGravityMultiplier - 1f);
                if (fall > 0.0001f)
                {
                    Vector2 g = Physics2D.gravity * _rigidbody2D.gravityScale;
                    _rigidbody2D.AddForce(g * (_rigidbody2D.mass * fall), ForceMode2D.Force);
                }
            }

            _jumpHeldLastFrame = jumpHeld;
        }

        private bool IsGrounded()
        {
            Bounds bounds = _collider2D.bounds;
            float y = bounds.min.y + 0.02f;
            float dist = Mathf.Max(0.001f, groundCheckDistance);

            float maxOffset = Mathf.Max(0f, bounds.extents.x - 0.02f);
            float offset = Mathf.Clamp(Mathf.Abs(groundCheckHorizontalOffset), 0f, maxOffset);

            Vector2[] origins =
            {
                new(bounds.center.x, y),
                new(bounds.center.x - offset, y),
                new(bounds.center.x + offset, y),
            };

            for (int o = 0; o < origins.Length; o++)
            {
                int hitCount = Physics2D.RaycastNonAlloc(origins[o], Vector2.down, _groundHits, dist, groundMask);
                for (int i = 0; i < hitCount; i++)
                {
                    Collider2D hitCollider = _groundHits[i].collider;
                    if (hitCollider == null)
                    {
                        continue;
                    }

                    // Ray may start inside our own collider, which would otherwise make us "grounded" forever.
                    if (hitCollider == _collider2D)
                    {
                        continue;
                    }

                    // Don't allow trigger volumes (e.g. quantum bridges when unobserved) to count as ground.
                    if (hitCollider.isTrigger)
                    {
                        continue;
                    }

                    return true;
                }
            }

            return false;
        }

        private void UpdateAimDirection()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                return;
            }

            Vector3 mousePosition = Input.mousePosition;
            mousePosition.z = -camera.transform.position.z;
            Vector3 mouseWorld = camera.ScreenToWorldPoint(mousePosition);
            Vector2 toMouse = (Vector2)(mouseWorld - transform.position);
            if (toMouse.sqrMagnitude < 0.0001f)
            {
                return;
            }

            AimDirection = toMouse.normalized;
        }

        public void SetSpawnPosition(Vector3 newSpawnPosition)
        {
            _spawnPosition = newSpawnPosition;
        }

        public void Respawn()
        {
            _rigidbody2D.velocity = Vector2.zero;
            transform.position = _spawnPosition;
        }
    }
}
