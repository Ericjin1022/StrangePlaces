using UnityEngine;

namespace StrangePlaces.DemoQuantumCollapse
{
    public sealed class FermatHUD : MonoBehaviour
    {
        [SerializeField] private string title = "第二关：费尔马最少时间原理（演示）";

        private GUIStyle _titleStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _debugStyle;

        private void OnGUI()
        {
            EnsureStyles();

            GUI.color = Color.white;
            GUILayout.BeginArea(new Rect(12, 12, 820, 320));
            GUILayout.Label($"<b>{title}</b>", _titleStyle);
            GUILayout.Label("目标：让“时间探针”走到接收器，门才会打开。", _labelStyle);
            GUILayout.Label("规则：它选择的是“最省时间”的路线，而不是最短路线。", _labelStyle);
            GUILayout.Label("操作：用鼠标照亮介质区，让它变快；按 T 重置探针；按 Esc 返回选关。", _labelStyle);

            DrawDebug();
            GUILayout.EndArea();
        }

        private void DrawDebug()
        {
            FermatSpeedMedium2D medium = FindFirstObjectByType<FermatSpeedMedium2D>();
            ObserverCone2D observer = FindFirstObjectByType<ObserverCone2D>();

            GUILayout.Space(6);
            if (medium == null)
            {
                GUILayout.Label("介质：未找到（脚本未挂上或初始化失败）", _debugStyle);
            }
            else
            {
                string observed = medium.IsObserved ? "被照亮" : "未照亮";
                GUILayout.Label($"介质：{observed}，速度倍率={medium.CurrentSpeedMultiplier:0.00}", _debugStyle);
            }

            GUILayout.Label(observer == null ? "观察锥：未找到" : "观察锥：已启用", _debugStyle);
        }

        private void EnsureStyles()
        {
            if (_titleStyle != null)
            {
                return;
            }

            GUIStyle baseStyle = GUI.skin.label;
            _titleStyle = new GUIStyle(baseStyle) { richText = true, fontSize = 16 };
            _labelStyle = new GUIStyle(baseStyle) { richText = true, fontSize = 13 };
            _debugStyle = new GUIStyle(baseStyle) { richText = true, fontSize = 12 };
        }
    }
}

