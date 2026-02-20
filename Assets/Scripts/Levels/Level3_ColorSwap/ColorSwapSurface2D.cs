using UnityEngine;

namespace StrangePlaces.Level3_ColorSwap
{
    [DisallowMultipleComponent]
    public sealed class ColorSwapSurface2D : MonoBehaviour
    {
        [Header("颜色")]
        [SerializeField] private BinaryColor baseColor = BinaryColor.Black;
        [Tooltip("是否跟随全局黑白切换一起翻转颜色。")]
        [SerializeField] private bool followGlobalSwap = true;

        [Header("碰撞规则")]
        [Tooltip("为 true 时：只有当“地面当前颜色 == 玩家当前颜色”时才启用碰撞体。")]
        [SerializeField] private bool colliderOnlyWhenMatchesPlayer = true;

        [Tooltip("如果未手动指定，将自动收集本物体(含子物体)上的 Collider2D。")]
        [SerializeField] private bool autoCollectColliders = true;
        [SerializeField] private bool includeChildColliders = true;
        [SerializeField] private Collider2D[] colliders = System.Array.Empty<Collider2D>();

        [Header("可选：外观")]
        [Tooltip("为 true 时，通过切换 SpriteRenderer.sprite 表现黑白变化（优先级高于颜色染色）。")]
        [SerializeField] private bool driveSpriteSwap = true;
        [SerializeField] private Sprite blackSprite;
        [SerializeField] private Sprite whiteSprite;
        [Tooltip("切换 Sprite 时，是否将 SpriteRenderer.color 归一为白色，避免被旧的染色影响。")]
        [SerializeField] private bool resetColorToWhiteWhenSwapping = true;

        [Tooltip("为 true 时，使用 SpriteRenderer.color 进行黑白染色（当 driveSpriteSwap 关闭或贴图未配置时生效）。")]
        [SerializeField] private bool driveSpriteRendererColor = true;
        [Tooltip("如果未手动指定，将自动收集本物体(含子物体)上的 SpriteRenderer。")]
        [SerializeField] private bool autoCollectSpriteRenderers = true;
        [SerializeField] private bool includeChildSpriteRenderers = true;
        [SerializeField] private SpriteRenderer[] spriteRenderers = System.Array.Empty<SpriteRenderer>();

        private ColorSwapManager2D _manager;

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

            if ((driveSpriteSwap || driveSpriteRendererColor) && autoCollectSpriteRenderers && (spriteRenderers == null || spriteRenderers.Length == 0))
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
            if ((driveSpriteSwap || driveSpriteRendererColor) && autoCollectSpriteRenderers && (spriteRenderers == null || spriteRenderers.Length == 0))
            {
                spriteRenderers = includeChildSpriteRenderers ? GetComponentsInChildren<SpriteRenderer>(true) : GetComponents<SpriteRenderer>();
            }

            if (autoCollectColliders && (colliders == null || colliders.Length == 0))
            {
                colliders = includeChildColliders ? GetComponentsInChildren<Collider2D>(true) : GetComponents<Collider2D>();
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
            ApplyCollisionRule();
        }

        private void ApplyVisual()
        {
            if (spriteRenderers == null || spriteRenderers.Length == 0)
            {
                return;
            }

            BinaryColor c = CurrentColor;

            if (driveSpriteSwap && (blackSprite != null || whiteSprite != null))
            {
                Sprite s = c == BinaryColor.Black ? blackSprite : whiteSprite;
                for (int i = 0; i < spriteRenderers.Length; i++)
                {
                    SpriteRenderer r = spriteRenderers[i];
                    if (r == null)
                    {
                        continue;
                    }

                    if (s != null)
                    {
                        r.sprite = s;
                    }

                    if (resetColorToWhiteWhenSwapping)
                    {
                        r.color = Color.white;
                    }
                }

                return;
            }

            if (!driveSpriteRendererColor)
            {
                return;
            }

            Color color = c.ToUnityColor();
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                if (spriteRenderers[i] != null)
                {
                    spriteRenderers[i].color = color;
                }
            }
        }

        private void ApplyCollisionRule()
        {
            if (colliders == null || colliders.Length == 0)
            {
                return;
            }

            bool enable = true;
            if (colliderOnlyWhenMatchesPlayer)
            {
                if (_manager == null || _manager.Player == null)
                {
                    enable = true;
                }
                else
                {
                    enable = CurrentColor == _manager.Player.CurrentColor;
                }
            }

            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null)
                {
                    colliders[i].enabled = enable;
                }
            }
        }
    }
}


