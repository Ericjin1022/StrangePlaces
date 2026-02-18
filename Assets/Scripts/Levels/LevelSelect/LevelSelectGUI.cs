using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StrangePlaces.DemoQuantumCollapse
{
    public sealed class LevelSelectGUI : MonoBehaviour
    {
        [Serializable]
        private sealed class LevelEntry
        {
            [Tooltip("显示在按钮上的关卡名称（必须为中文）。")]
            public string displayName = "（未命名关卡）";

            [Tooltip("Build Settings 里的场景名（不含路径与扩展名），例如：QuantumCollapseDemo。")]
            public string sceneName = "QuantumCollapseDemo";

            [TextArea(1, 3)]
            [Tooltip("可选：显示在关卡名下方的简短描述（建议中文）。")]
            public string description = "";
        }

        [Header("Content")]
        [SerializeField] private string title = "关卡选择";
        [SerializeField] private LevelEntry[] levels =
        {
            new LevelEntry
            {
                displayName = "量子观测坍缩（演示）",
                sceneName = "QuantumCollapseDemo",
                description = "最小可玩 Demo：光锥观测、视线遮挡、量子门、纠缠信标。",
            }
        };

        [Header("Layout")]
        [SerializeField] private int panelWidth = 720;
        [SerializeField] private int panelHeight = 520;

        [Header("Controls")]
        [SerializeField] private KeyCode reloadKey = KeyCode.R;

        private GUIStyle _titleStyle;
        private GUIStyle _levelNameStyle;
        private GUIStyle _descStyle;
        private GUIStyle _hintStyle;

        private void Update()
        {
            if (Input.GetKeyDown(reloadKey))
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
        }

        private void OnGUI()
        {
            EnsureStyles();

            int w = Mathf.Clamp(panelWidth, 420, 1200);
            int h = Mathf.Clamp(panelHeight, 320, 900);
            Rect area = new((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);

            GUI.color = Color.white;
            GUILayout.BeginArea(area, GUI.skin.box);
            GUILayout.Label($"<b>{title}</b>", _titleStyle);
            GUILayout.Space(8);

            if (levels == null || levels.Length == 0)
            {
                GUILayout.Label("暂无可选关卡。", _levelNameStyle);
                GUILayout.Label("你可以在 LevelSelectGUI 组件里添加关卡条目。", _hintStyle);
                GUILayout.EndArea();
                return;
            }

            for (int i = 0; i < levels.Length; i++)
            {
                LevelEntry entry = levels[i];
                if (entry == null)
                {
                    continue;
                }

                string name = string.IsNullOrWhiteSpace(entry.displayName) ? "（未命名关卡）" : entry.displayName;
                string scene = entry.sceneName != null ? entry.sceneName.Trim() : "";

                using (new GUILayout.VerticalScope(GUI.skin.box))
                {
                    GUILayout.Label(name, _levelNameStyle);
                    if (!string.IsNullOrWhiteSpace(entry.description))
                    {
                        GUILayout.Label(entry.description, _descStyle);
                    }

                    GUILayout.Space(4);
                    GUI.enabled = !string.IsNullOrWhiteSpace(scene);
                    if (GUILayout.Button("进入", GUILayout.Height(34)))
                    {
                        SceneManager.LoadScene(scene);
                    }
                    GUI.enabled = true;
                }

                GUILayout.Space(6);
            }

            GUILayout.FlexibleSpace();
            GUILayout.Label("提示：按 R 重新载入本界面。", _hintStyle);
            GUILayout.EndArea();
        }

        private void EnsureStyles()
        {
            if (_titleStyle != null)
            {
                return;
            }

            GUIStyle baseStyle = GUI.skin.label;
            _titleStyle = new GUIStyle(baseStyle) { richText = true, fontSize = 22, alignment = TextAnchor.MiddleCenter };
            _levelNameStyle = new GUIStyle(baseStyle) { richText = true, fontSize = 16 };
            _descStyle = new GUIStyle(baseStyle) { richText = true, fontSize = 12, wordWrap = true };
            _hintStyle = new GUIStyle(baseStyle) { richText = true, fontSize = 12, wordWrap = true, alignment = TextAnchor.MiddleLeft };
        }
    }
}

