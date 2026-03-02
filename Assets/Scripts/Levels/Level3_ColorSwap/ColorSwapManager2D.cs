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
        public BinaryColor StartWorldColor => startWorldColor;

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
        
        /// <summary>
        /// 获取当前世界的绝对颜色（即：综合了初始设置与当前翻转状态后的最终颜色）
        /// </summary>
        public BinaryColor CurrentWorldColor => startWorldColor.InvertIf(IsInverted);
        
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
            
            // IsInverted 的语义应该是“当前是否处于被翻转的状态”。
            // 无论开局设定是白还是黑，它一开始都应该是未翻转状态 (false)。
            IsInverted = false;
            
            // 应用初始 Shader 状态。由于 IsInverted 一开始是 false，这就要求 Shader 里的逻辑是：
            // 0 代表默认状态（由 startWorldColor 决定具体表现），1 代表翻转状态。
            // 假设我们通过传递基于当前绝对世界颜色的值给全局 Shader（比如 0=白, 1=黑）
            ApplyGlobalShader(CurrentWorldColor == BinaryColor.Black);
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

        public void ResetToStartColor()
        {
            SetInverted(false);
        }

        public void SetWorldColor(BinaryColor worldColor)
        {
            // 如果你请求的颜色和初始颜色不同，那就说明你处于/需要翻转状态。
            SetInverted(worldColor != startWorldColor);
        }

        public void SetInverted(bool inverted)
        {
            if (IsInverted == inverted)
            {
                return;
            }

            IsInverted = inverted;
            
            // 无论它本身翻转没翻转，发给 Shader 渲染的永远是它呈现出来的绝对终极颜色。
            ApplyGlobalShader(CurrentWorldColor == BinaryColor.Black);
            
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

