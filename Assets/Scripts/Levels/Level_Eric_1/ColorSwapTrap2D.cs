using StrangePlaces.DemoQuantumCollapse;
using UnityEngine;

namespace StrangePlaces.Level3_ColorSwap
{
    [DisallowMultipleComponent]
    public sealed class ColorSwapTrap2D : MonoBehaviour
    {
        [Header("颜色")]
        [SerializeField] private BinaryColor baseColor = BinaryColor.Black;
        [Tooltip("是否跟随全局黑白切换一起翻转颜色。")]
        [SerializeField] private bool followGlobalSwap = true;

        [Header("触发")]
        [Tooltip("同色触发后的冷却时间（避免触发器持续重叠导致反复触发）。")]
        [SerializeField, Min(0f)] private float triggerCooldownSeconds = 0.2f;

        [Tooltip("如未手动指定，将自动收集本物体(含子物体)上的 Collider2D 作为陷阱命中体。")]
        [SerializeField] private bool autoCollectColliders = true;
        [SerializeField] private bool includeChildColliders = true;
        [SerializeField] private Collider2D[] colliders = System.Array.Empty<Collider2D>();

        [Header("可选：外观")]
        [SerializeField] private bool driveSpriteRendererColor = false;
        [Tooltip("如未手动指定，将自动收集本物体(含子物体)上的 SpriteRenderer。")]
        [SerializeField] private bool autoCollectSpriteRenderers = true;
        [SerializeField] private bool includeChildSpriteRenderers = true;
        [SerializeField] private SpriteRenderer[] spriteRenderers = System.Array.Empty<SpriteRenderer>();

        private ColorSwapManager2D _manager;
        private float _nextAllowedTriggerTime;

        public BinaryColor CurrentColor
        {
            get
            {
                bool invert = followGlobalSwap && _manager != null && _manager.IsInverted;
                return baseColor.InvertIf(invert);
            }
        }

        private void Awake()
        {
            if (autoCollectColliders && (colliders == null || colliders.Length == 0))
            {
                colliders = includeChildColliders ? GetComponentsInChildren<Collider2D>(true) : GetComponents<Collider2D>();
            }

            if (driveSpriteRendererColor && autoCollectSpriteRenderers && (spriteRenderers == null || spriteRenderers.Length == 0))
            {
                spriteRenderers = includeChildSpriteRenderers ? GetComponentsInChildren<SpriteRenderer>(true) : GetComponents<SpriteRenderer>();
            }
        }

        private void OnEnable()
        {
            _manager = ColorSwapManager2D.Instance != null ? ColorSwapManager2D.Instance : FindFirstObjectByType<ColorSwapManager2D>();
            if (_manager != null)
            {
                _manager.InvertedChanged += OnInvertedChanged;
                _manager.PlayerRegistered += OnPlayerRegistered;
            }

            EnsureHitboxes();
            Refresh();
        }

        private void OnDisable()
        {
            if (_manager != null)
            {
                _manager.InvertedChanged -= OnInvertedChanged;
                _manager.PlayerRegistered -= OnPlayerRegistered;
            }
        }

        private void OnValidate()
        {
            if (autoCollectColliders && (colliders == null || colliders.Length == 0))
            {
                colliders = includeChildColliders ? GetComponentsInChildren<Collider2D>(true) : GetComponents<Collider2D>();
            }

            if (driveSpriteRendererColor && autoCollectSpriteRenderers && (spriteRenderers == null || spriteRenderers.Length == 0))
            {
                spriteRenderers = includeChildSpriteRenderers ? GetComponentsInChildren<SpriteRenderer>(true) : GetComponents<SpriteRenderer>();
            }
        }

        private void OnInvertedChanged(bool _)
        {
            Refresh();
        }

        private void OnPlayerRegistered()
        {
            Refresh();
        }

        private void Refresh()
        {
            ApplyVisual();
        }

        private void ApplyVisual()
        {
            if (!driveSpriteRendererColor)
            {
                return;
            }

            if (spriteRenderers == null || spriteRenderers.Length == 0)
            {
                return;
            }

            Color color = CurrentColor.ToUnityColor();
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                if (spriteRenderers[i] != null)
                {
                    spriteRenderers[i].color = color;
                }
            }
        }

        private void EnsureHitboxes()
        {
            if (colliders == null || colliders.Length == 0)
            {
                return;
            }

            for (int i = 0; i < colliders.Length; i++)
            {
                Collider2D c = colliders[i];
                if (c == null)
                {
                    continue;
                }

                ColorSwapTrapHitbox2D hitbox = c.GetComponent<ColorSwapTrapHitbox2D>();
                if (hitbox == null)
                {
                    hitbox = c.gameObject.AddComponent<ColorSwapTrapHitbox2D>();
                }

                hitbox.Bind(this);
            }
        }

        internal void HandleTrigger(Collider2D other)
        {
            TryTriggerOnPlayer(other);
        }

        internal void HandleCollision(Collision2D collision)
        {
            if (collision == null)
            {
                return;
            }

            TryTriggerOnPlayer(collision.otherCollider);
        }

        private void TryTriggerOnPlayer(Collider2D other)
        {
            if (other == null)
            {
                return;
            }

            if (Time.time < _nextAllowedTriggerTime)
            {
                return;
            }

            PlayerColorSwap2D playerColor = other.GetComponentInParent<PlayerColorSwap2D>();
            if (playerColor == null)
            {
                return;
            }

            if (playerColor.CurrentColor != CurrentColor)
            {
                return;
            }

            PlayerController2D player = playerColor.GetComponentInParent<PlayerController2D>();
            if (player == null)
            {
                player = other.GetComponentInParent<PlayerController2D>();
            }

            if (player == null)
            {
                return;
            }

            _nextAllowedTriggerTime = Time.time + Mathf.Max(0f, triggerCooldownSeconds);
            player.Respawn();
        }
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    [AddComponentMenu("")]
    internal sealed class ColorSwapTrapHitbox2D : MonoBehaviour
    {
        private ColorSwapTrap2D _parent;

        public void Bind(ColorSwapTrap2D parent)
        {
            _parent = parent;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_parent != null)
            {
                _parent.HandleTrigger(other);
            }
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (_parent != null)
            {
                _parent.HandleTrigger(other);
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (_parent != null)
            {
                _parent.HandleCollision(collision);
            }
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            if (_parent != null)
            {
                _parent.HandleCollision(collision);
            }
        }
    }
}


