using System;
using UnityEngine;

namespace StrangePlaces.Level3_ColorSwap
{
    [DisallowMultipleComponent]
    public sealed class ColorSwapManager2D : MonoBehaviour
    {
        [Header("\u5F00\u5C40")]
        [Tooltip("\u8BBE\u7F6E\u672C\u5173\u5361\u7684\u5F00\u5C40\u4E16\u754C\u989C\u8272\u3002")]
        [SerializeField] private BinaryColor startWorldColor = BinaryColor.White;

        [Header("\u53EF\u9009\uFF1A\u9A71\u52A8 Shader")]
        [Tooltip("\u5982\u679C\u4F60\u7684\u6750\u8D28/Shader \u652F\u6301\u5168\u5C40\u53CD\u8F6C\uFF0C\u53EF\u586B\u5199\u5168\u5C40 float \u53C2\u6570\u540D\uFF08\u4F8B\u5982 _SP_Inverted\uFF09\u3002\u7559\u7A7A\u5219\u4E0D\u8BBE\u7F6E\u5168\u5C40 Shader \u53C2\u6570\u3002")]
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
                Debug.LogWarning("[\u9ED1\u767D\u5207\u6362] \u573A\u666F\u4E2D\u5B58\u5728\u591A\u4E2A ColorSwapManager2D\uFF0C\u5C06\u7981\u7528\u91CD\u590D\u7EC4\u4EF6\u3002", this);
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

