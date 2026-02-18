using UnityEngine;

namespace StrangePlaces.Level3_ColorSwap
{
    [DisallowMultipleComponent]
    public sealed class ColorSwapActiveByWorldColor2D : MonoBehaviour
    {
        [Header("世界颜色")]
        [Tooltip("当 ColorSwapManager2D.IsInverted 为 false 时，世界处于什么颜色模式。")]
        [SerializeField] private BinaryColor baseWorldColor = BinaryColor.White;

        [Header("激活切换")]
        [Tooltip("当世界为黑色时激活这些物体（并关闭白色列表）。")]
        [SerializeField] private GameObject[] activeWhenWorldBlack = System.Array.Empty<GameObject>();
        [Tooltip("当世界为白色时激活这些物体（并关闭黑色列表）。")]
        [SerializeField] private GameObject[] activeWhenWorldWhite = System.Array.Empty<GameObject>();

        private ColorSwapManager2D _manager;

        private void OnEnable()
        {
            _manager = ColorSwapManager2D.Instance != null
                ? ColorSwapManager2D.Instance
                : FindFirstObjectByType<ColorSwapManager2D>();

            if (_manager != null)
            {
                _manager.InvertedChanged += OnInvertedChanged;
            }

            Refresh();
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
            if (!isActiveAndEnabled)
            {
                return;
            }

            Refresh();
        }

        private void OnInvertedChanged(bool _)
        {
            Refresh();
        }

        private void Refresh()
        {
            BinaryColor worldColor = baseWorldColor;
            if (_manager != null)
            {
                worldColor = baseWorldColor.InvertIf(_manager.IsInverted);
            }

            bool worldIsBlack = worldColor == BinaryColor.Black;
            SetActive(activeWhenWorldBlack, worldIsBlack);
            SetActive(activeWhenWorldWhite, !worldIsBlack);
        }

        private static void SetActive(GameObject[] list, bool active)
        {
            if (list == null || list.Length == 0)
            {
                return;
            }

            for (int i = 0; i < list.Length; i++)
            {
                GameObject go = list[i];
                if (go != null && go.activeSelf != active)
                {
                    go.SetActive(active);
                }
            }
        }
    }
}

