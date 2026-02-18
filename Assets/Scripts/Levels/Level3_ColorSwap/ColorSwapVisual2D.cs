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

            Color color = CurrentColor.ToUnityColor();
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

