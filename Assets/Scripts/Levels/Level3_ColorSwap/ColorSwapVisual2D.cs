using UnityEngine;

namespace StrangePlaces.Level3_ColorSwap
{
    [DisallowMultipleComponent]
    public sealed class ColorSwapVisual2D : MonoBehaviour
    {
        [Header("颜色")]
        [SerializeField] private BinaryColor baseColor = BinaryColor.Black;
        [Tooltip("是否跟随全局黑白切换一起翻转颜色。")]
        [SerializeField] private bool followGlobalSwap = true;

        [Header("渲染器")]
        [SerializeField] private bool autoCollectSpriteRenderers = true;
        [SerializeField] private bool includeChildRenderers = true;
        [SerializeField] private SpriteRenderer[] spriteRenderers = System.Array.Empty<SpriteRenderer>();

        [Header("外观")]
        [Tooltip("为 true 时，通过切换 SpriteRenderer.sprite 表现黑白变化（优先级高于颜色染色）。")]
        [SerializeField] private bool driveSpriteSwap = true;
        [SerializeField] private Sprite blackSprite;
        [SerializeField] private Sprite whiteSprite;
        [Tooltip("切换 Sprite 时，是否将 SpriteRenderer.color 归一为白色，避免被旧的染色影响。")]
        [SerializeField] private bool resetColorToWhiteWhenSwapping = true;

        [Tooltip("为 true 时，使用 SpriteRenderer.color 进行黑白染色（当未配置贴图或关闭贴图切换时生效）。")]
        [SerializeField] private bool driveSpriteRendererColor = false;

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
            if (autoCollectSpriteRenderers && (spriteRenderers == null || spriteRenderers.Length == 0))
            {
                spriteRenderers = includeChildRenderers ? GetComponentsInChildren<SpriteRenderer>(true) : GetComponents<SpriteRenderer>();
            }
        }

        private void OnEnable()
        {
            _manager = ColorSwapManager2D.Instance != null ? ColorSwapManager2D.Instance : FindFirstObjectByType<ColorSwapManager2D>();
            if (_manager != null)
            {
                _manager.InvertedChanged += OnInvertedChanged;
            }

            Apply();
        }

        private void OnDisable()
        {
            if (_manager != null)
            {
                _manager.InvertedChanged -= OnInvertedChanged;
            }
        }

        private void OnValidate()
        {
            if (autoCollectSpriteRenderers && (spriteRenderers == null || spriteRenderers.Length == 0))
            {
                spriteRenderers = includeChildRenderers ? GetComponentsInChildren<SpriteRenderer>(true) : GetComponents<SpriteRenderer>();
            }
        }

        private void OnInvertedChanged(bool _)
        {
            Apply();
        }

        private void Apply()
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
    }
}


