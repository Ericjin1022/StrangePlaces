using UnityEngine;
using UnityEngine.SceneManagement;

namespace StrangePlaces.DemoQuantumCollapse
{
    public sealed class DemoHUD : MonoBehaviour
    {
        [SerializeField] private bool showControls = true;
        [SerializeField] private string title = "第一关：量子观测坍缩（演示）";
        [SerializeField] private bool showStageHints = false;

        private bool _won;
        private bool _didPause;

        private GUIStyle _titleStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _winStyle;
        private GUIStyle _modalTitleStyle;
        private GUIStyle _modalBodyStyle;
        private GUIStyle _buttonStyle;

        public void SetWin(bool won)
        {
            _won = won;
            if (_won)
            {
                TryPause();
            }
            else
            {
                TryUnpause();
            }
        }

        private void Update()
        {
            if (_won)
            {
                TryPause();
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                TryUnpause();
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                TryUnpause();
                SceneManager.LoadScene("LevelSelect");
            }
        }

        private void OnDisable()
        {
            TryUnpause();
        }

        private void OnGUI()
        {
            EnsureStyles();
            GUI.color = Color.white;

            GUILayout.BeginArea(new Rect(12, 12, 760, 320));
            GUILayout.Label($"<b>{title}</b>", _titleStyle);

            if (showControls)
            {
                GUILayout.Label("移动：A/D 或 ←/→    跳跃：空格    观察：鼠标指向    重开：R", _labelStyle);
                GUILayout.Label("返回选关：Esc", _labelStyle);
                GUILayout.Label("规则 1：量子踏板只有在被观察时才会坍缩成“实体”。", _labelStyle);
                GUILayout.Label("规则 2：有些门在你观察时会保持关闭；不看它，状态才会变化。", _labelStyle);
                GUILayout.Label("目标：到达终点（绿色方块）。", _labelStyle);
            }

            if (showStageHints)
            {
                GUILayout.Space(8);
                foreach (string line in GetStageHints())
                {
                    GUILayout.Label(line, _labelStyle);
                }
            }

            if (_won)
            {
                GUILayout.Space(12);
                GUI.color = new Color(0.3f, 1f, 0.4f, 1f);
                GUILayout.Label("通关！（已暂停）", _winStyle);
            }

            GUILayout.EndArea();

            if (_won)
            {
                DrawWinModal();
            }
        }

        private static string[] GetStageHints()
        {
            PlayerController2D player = FindFirstObjectByType<PlayerController2D>();
            float x = player != null ? player.transform.position.x : 0f;

            if (x < -8f)
            {
                return new[]
                {
                    "提示：第一段是“回头路”。站上量子桥后，边走边回头观察它。",
                    "如果你把视线移开，桥会恢复叠加态，你会掉下去。",
                };
            }

            if (x < 6f)
            {
                return new[]
                {
                    "提示：第二段是“别看门”。你盯着门，它会保持关闭。",
                    "把视线移开，让门的状态变化到“打开”，再快速通过。",
                };
            }

            if (x < 14f)
            {
                return new[]
                {
                    "提示：第三段需要“两次接力”。先观察并稳定第一块平台，到达中间安全点。",
                    "然后换角度观察第二块平台。墙会遮挡视线，站位很重要。",
                };
            }

            return new[]
            {
                "提示：终点在右侧。",
            };
        }

        private void EnsureStyles()
        {
            if (_titleStyle != null)
            {
                return;
            }

            GUIStyle baseStyle = GUI.skin.label;
            _titleStyle = new GUIStyle(baseStyle) { richText = true, fontSize = 16 };
            _labelStyle = new GUIStyle(baseStyle) { richText = true, fontSize = 13, wordWrap = true };
            _winStyle = new GUIStyle(baseStyle) { richText = true, fontSize = 18, fontStyle = FontStyle.Bold };

            _modalTitleStyle = new GUIStyle(baseStyle)
            {
                richText = true,
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperCenter
            };
            _modalBodyStyle = new GUIStyle(baseStyle)
            {
                richText = true,
                fontSize = 14,
                wordWrap = true,
                alignment = TextAnchor.UpperCenter
            };

            GUIStyle btn = new GUIStyle(GUI.skin.button);
            btn.fontSize = 14;
            btn.padding = new RectOffset(14, 14, 10, 10);
            _buttonStyle = btn;
        }

        private void TryPause()
        {
            if (_didPause)
            {
                return;
            }

            _didPause = true;
            Time.timeScale = 0f;
        }

        private void TryUnpause()
        {
            if (!_didPause)
            {
                return;
            }

            _didPause = false;
            Time.timeScale = 1f;
        }

        private void DrawWinModal()
        {
            float w = 520f;
            float h = 220f;
            Rect r = new((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);

            Color old = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = old;

            GUILayout.BeginArea(r, GUI.skin.window);
            GUILayout.Space(4);
            GUILayout.Label("通关成功", _modalTitleStyle);
            GUILayout.Space(8);
            GUILayout.Label("已暂停游戏。\n按 <b>Esc</b> 返回选关，按 <b>R</b> 重新开始本关。", _modalBodyStyle);
            GUILayout.Space(16);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("返回选关（Esc）", _buttonStyle, GUILayout.Width(180f)))
            {
                TryUnpause();
                SceneManager.LoadScene("LevelSelect");
            }
            GUILayout.Space(12);
            if (GUILayout.Button("重开本关（R）", _buttonStyle, GUILayout.Width(160f)))
            {
                TryUnpause();
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }
    }
}

