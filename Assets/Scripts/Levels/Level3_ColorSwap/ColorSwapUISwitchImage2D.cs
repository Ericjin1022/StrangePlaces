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
        [Tooltip("世界颜色为 白色 时的 UI 贴图。")]
        [SerializeField] private Sprite whiteSprite;
        [Tooltip("世界颜色为 黑色 时的 UI 贴图。")]
        [SerializeField] private Sprite blackSprite;

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
            Apply();
        }

        private void Update()
        {
            Apply();
        }

        private void OnValidate()
        {
            if (targetImage == null)
            {
                targetImage = GetComponent<Image>();
            }
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

            // 获取当前的绝对世界颜色
            bool currentIsBlack = manager.CurrentWorldColor == BinaryColor.Black;

            Sprite s = currentIsBlack ? blackSprite : whiteSprite;
            if (s == null)
            {
                // 没配贴图时不强行清空，避免你想用现有图片做默认。
                return;
            }

            if (targetImage.sprite == s)
            {
                return;
            }

            targetImage.sprite = s;
            Debug.Log("currentIsBlack: " + currentIsBlack);
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

