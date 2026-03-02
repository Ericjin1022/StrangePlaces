using UnityEngine;
using StrangePlaces.DemoQuantumCollapse;

namespace StrangePlaces.Level3_ColorSwap
{
    [DisallowMultipleComponent]
    public sealed class PlayerColorSwap2D : MonoBehaviour
    {
        [Header("输入")]
        [SerializeField] private bool allowKeyboardToggle = true;
        [SerializeField] private KeyCode toggleKey = KeyCode.E;

        private ColorSwapManager2D _manager;

        public BinaryColor CurrentColor
        {
            get
            {
                if (_manager == null) return BinaryColor.Black; // 缺省保护机制

                // 玩家颜色永远和当前世界绝对颜色保持一致
                return _manager.StartWorldColor.InvertIf(_manager.IsInverted);
            }
        }


        private void OnEnable()
        {
            _manager = ColorSwapManager2D.Instance != null ? ColorSwapManager2D.Instance : FindFirstObjectByType<ColorSwapManager2D>();
            if (_manager != null)
            {
                _manager.RegisterPlayer(this);
            }

            PlayerController2D player = GetComponent<PlayerController2D>();
            if (player != null)
            {
                player.OnRespawn += HandleRespawn;
            }
        }

        private void Start()
        {
            ApplyVisual();
        }

        private void OnDisable()
        {
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
            ApplyVisual();

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


