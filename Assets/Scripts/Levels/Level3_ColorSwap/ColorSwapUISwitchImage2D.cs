using UnityEngine;
using UnityEngine.UI;

namespace StrangePlaces.Level3_ColorSwap
{
    [DisallowMultipleComponent]
    public sealed class ColorSwapUISwitchImage2D : MonoBehaviour
    {
        [Header("\u5F15\u7528")]
        [SerializeField] private ColorSwapManager2D manager;
        [SerializeField] private Image targetImage;

        [Header("\u8D34\u56FE")]
        [Tooltip("\u5168\u5C40\u72B6\u6001\u4E3A\u201C\u975E\u53CD\u8F6C\u201D\u65F6\u663E\u793A\u7684\u8D34\u56FE\u3002")]
        [SerializeField] private Sprite normalSprite;
        [Tooltip("\u5168\u5C40\u72B6\u6001\u4E3A\u201C\u53CD\u8F6C\u201D\u65F6\u663E\u793A\u7684\u8D34\u56FE\u3002")]
        [SerializeField] private Sprite invertedSprite;

        [Header("\u53EF\u9009")]
        [Tooltip("\u5207\u6362\u8D34\u56FE\u540E\u662F\u5426\u8C03\u7528 Image.SetNativeSize()\uFF0C\u4EE5\u8D34\u56FE\u539F\u59CB\u5C3A\u5BF8\u4E3A\u51C6\u3002")]
        [SerializeField] private bool setNativeSize = false;

        private bool warnedMissing;

        private void Reset()
        {
            targetImage = GetComponent<Image>();
        }

        private void OnEnable()
        {
            EnsureRefs();

            if (manager != null)
            {
                manager.InvertedChanged += OnInvertedChanged;
            }

            Apply();
        }

        private void OnDisable()
        {
            if (manager != null)
            {
                manager.InvertedChanged -= OnInvertedChanged;
            }
        }

        private void OnValidate()
        {
            if (targetImage == null)
            {
                targetImage = GetComponent<Image>();
            }
        }

        private void OnInvertedChanged(bool _)
        {
            Apply();
        }

        private void EnsureRefs()
        {
            if (targetImage == null)
            {
                targetImage = GetComponent<Image>();
            }

            if (manager == null)
            {
                manager = ColorSwapManager2D.Instance != null ? ColorSwapManager2D.Instance : FindFirstObjectByType<ColorSwapManager2D>();
            }
        }

        private void Apply()
        {
            EnsureRefs();

            if (targetImage == null || manager == null)
            {
                WarnMissingOnce();
                return;
            }

            Sprite s = manager.IsInverted ? invertedSprite : normalSprite;
            if (s == null)
            {
                // \u6CA1\u914D\u8D34\u56FE\u65F6\u4E0D\u5F3A\u884C\u6E05\u7A7A\uff0c\u907F\u514D\u4F60\u60F3\u7528\u73B0\u6709\u56FE\u7247\u505A\u9ED8\u8BA4\u3002
                return;
            }

            if (targetImage.sprite == s)
            {
                return;
            }

            targetImage.sprite = s;
            if (setNativeSize)
            {
                targetImage.SetNativeSize();
            }
        }

        private void WarnMissingOnce()
        {
            if (warnedMissing)
            {
                return;
            }

            warnedMissing = true;
            Debug.LogWarning("[\u9ED1\u767D\u5207\u6362UI] \u672A\u7ED1\u5B9A\u5FC5\u8981\u5F15\u7528\uFF1A\u8BF7\u5728 Inspector \u4E2D\u786E\u8BA4\u6B64\u7269\u4F53\u4E0A\u6709 Image\uFF0C\u5E76\u7ED1\u5B9A\u6216\u53EF\u641C\u7D22\u5230 ColorSwapManager2D\u3002", this);
        }

        public void ToggleFromUI()
        {
            EnsureRefs();
            if (manager == null)
            {
                WarnMissingOnce();
                return;
            }

            manager.Toggle();
        }
    }
}

