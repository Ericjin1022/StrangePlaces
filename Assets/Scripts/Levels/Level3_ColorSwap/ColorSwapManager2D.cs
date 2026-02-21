using System;
using UnityEngine;

namespace StrangePlaces.Level3_ColorSwap
{
    [DisallowMultipleComponent]
    public sealed class ColorSwapManager2D : MonoBehaviour
    {
        [Header("开局")]
        [Tooltip("设置本关卡的开局世界颜色。")]
        [SerializeField] private BinaryColor startWorldColor = BinaryColor.White;

        [Header("可选：驱动 Shader")]
        [Tooltip("如果你的材质/Shader 支持全局反转，可填写全局 float 参数名（例如 _SP_Inverted）。留空则不设置全局 Shader 参数。")]
        [SerializeField] private string globalShaderFloatName = "_SP_Inverted";

        private static ColorSwapManager2D _instance;
        public static ColorSwapManager2D Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<ColorSwapManager2D>();
                }

                return _instance;
            }
            private set => _instance = value;
        }

        public bool IsInverted { get; private set; }
        public PlayerColorSwap2D Player { get; private set; }

        public event Action<bool> InvertedChanged;
        public event Action PlayerRegistered;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[黑白切换] 场景中存在多个 ColorSwapManager2D，将禁用重复组件。", this);
                enabled = false;
                return;
            }

            Instance = this;
            IsInverted = startWorldColor == BinaryColor.Black;
            ApplyGlobalShader(IsInverted);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void Toggle()
        {
            SetInverted(!IsInverted);
        }

        public void SetWorldColor(BinaryColor worldColor)
        {
            SetInverted(worldColor == BinaryColor.Black);
        }

        public void SetInverted(bool inverted)
        {
            if (IsInverted == inverted)
            {
                return;
            }

            IsInverted = inverted;
            ApplyGlobalShader(IsInverted);
            InvertedChanged?.Invoke(IsInverted);
        }

        public void RegisterPlayer(PlayerColorSwap2D player)
        {
            if (player == null)
            {
                return;
            }

            if (Player == player)
            {
                return;
            }

            Player = player;
            PlayerRegistered?.Invoke();
        }

        private void ApplyGlobalShader(bool inverted)
        {
            if (string.IsNullOrWhiteSpace(globalShaderFloatName))
            {
                return;
            }

            Shader.SetGlobalFloat(globalShaderFloatName, inverted ? 1f : 0f);
        }
    }
}

