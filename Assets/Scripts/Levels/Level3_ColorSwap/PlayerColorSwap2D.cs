using UnityEngine;

namespace StrangePlaces.Level3_ColorSwap
{
    [DisallowMultipleComponent]
    public sealed class PlayerColorSwap2D : MonoBehaviour
    {
        [Header("颜色")]
        [SerializeField] private BinaryColor baseColor = BinaryColor.Black;
        [Tooltip("玩家是否跟随全局切换一起翻转颜色。")]
        [SerializeField] private bool followGlobalSwap = true;

        [Header("输入")]
        [SerializeField] private bool allowKeyboardToggle = true;
        [SerializeField] private KeyCode toggleKey = KeyCode.E;

        [Header("可选：外观")]
        [Tooltip("为 true 时，通过切换 SpriteRenderer.sprite 表现黑白变化（优先级高于颜色染色）。")]
        [SerializeField] private bool driveSpriteSwap = true;
        [SerializeField] private Sprite blackSprite;
        [SerializeField] private Sprite whiteSprite;
        [Tooltip("切换 Sprite 时，是否将 SpriteRenderer.color 归一为白色，避免被旧的染色影响。")]
        [SerializeField] private bool resetColorToWhiteWhenSwapping = true;

        [Tooltip("为 true 时，使用 SpriteRenderer.color 进行黑白染色（当 driveSpriteSwap 关闭或贴图未配置时生效）。")]
        [SerializeField] private bool driveSpriteRendererColor = true;
        [SerializeField] private SpriteRenderer spriteRenderer;

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
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }
        }

        private void OnEnable()
        {
            _manager = ColorSwapManager2D.Instance != null ? ColorSwapManager2D.Instance : FindFirstObjectByType<ColorSwapManager2D>();
            if (_manager != null)
            {
                _manager.RegisterPlayer(this);
                _manager.InvertedChanged += OnInvertedChanged;
            }

            ApplyVisual();
        }

        private void OnDisable()
        {
            if (_manager != null)
            {
                _manager.InvertedChanged -= OnInvertedChanged;
            }
        }

        private void Update()
        {
            if (!allowKeyboardToggle)
            {
                return;
            }

            if (_manager == null)
            {
                return;
            }

            if (Input.GetKeyDown(toggleKey))
            {
                _manager.Toggle();
            }
        }

        private void OnInvertedChanged(bool _)
        {
            ApplyVisual();
        }

        private void ApplyVisual()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            BinaryColor c = CurrentColor;

            if (driveSpriteSwap && (blackSprite != null || whiteSprite != null))
            {
                Sprite s = c == BinaryColor.Black ? blackSprite : whiteSprite;
                if (s != null)
                {
                    spriteRenderer.sprite = s;
                }

                if (resetColorToWhiteWhenSwapping)
                {
                    spriteRenderer.color = Color.white;
                }

                return;
            }

            if (!driveSpriteRendererColor)
            {
                return;
            }

            spriteRenderer.color = c.ToUnityColor();
        }
    }
}


