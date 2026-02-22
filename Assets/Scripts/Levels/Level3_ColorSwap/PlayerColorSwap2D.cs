using UnityEngine;
using StrangePlaces.DemoQuantumCollapse;

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



        private ColorSwapManager2D _manager;

        public BinaryColor CurrentColor
        {
            get
            {
                bool invert = followGlobalSwap && _manager != null && _manager.IsInverted;
                return baseColor.InvertIf(invert);
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

            PlayerController2D player = GetComponent<PlayerController2D>();
            if (player != null)
            {
                player.OnRespawn += HandleRespawn;
            }

            ApplyVisual();
        }

        private void OnDisable()
        {
            if (_manager != null)
            {
                _manager.InvertedChanged -= OnInvertedChanged;
            }

            PlayerController2D player = GetComponent<PlayerController2D>();
            if (player != null)
            {
                player.OnRespawn -= HandleRespawn;
            }
        }

        private void HandleRespawn()
        {
            if (_manager != null)
            {
                _manager.ResetToStartColor();
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

        [Header("双轨动画渲染器 (Dual Track Animators)")]
        [Tooltip("由于采用双生Animator切换平滑动画，此处填入黑色外观的Renderer")]
        [SerializeField] private SpriteRenderer rendererBlack;
        [Tooltip("填入白色外观的Renderer")]
        [SerializeField] private SpriteRenderer rendererWhite;

        private void ApplyVisual()
        {
            BinaryColor c = CurrentColor;

            if (rendererBlack != null && rendererWhite != null)
            {
                bool isBlack = c == BinaryColor.Black;
                rendererBlack.enabled = isBlack;
                rendererWhite.enabled = !isBlack;
            }
        }
    }
}


