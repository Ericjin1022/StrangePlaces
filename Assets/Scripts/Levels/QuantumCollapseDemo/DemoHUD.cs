using UnityEngine;
using UnityEngine.SceneManagement;

namespace StrangePlaces.DemoQuantumCollapse
{
    public sealed class DemoHUD : MonoBehaviour
    {
        private bool showControls = true;
        private bool showStageHints = false;

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

            if (showControls)
            {
                GUILayout.Label("Move: A/D or ←/→    Jump: Space    Look: Mouse", _labelStyle);
                GUILayout.Label("Flashlight: F    Restart: R    Level Select: Esc", _labelStyle);
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
                GUILayout.Label("Stage Cleared! (Paused)", _winStyle);
            }

            GUILayout.EndArea();

            if (_won)
            {
                DrawWinModal();
            }
        }

        private static string[] GetStageHints()
        {
            return new string[0];
        }

        private void EnsureStyles()
        {
            if (_labelStyle != null)
            {
                return;
            }

            GUIStyle baseStyle = GUI.skin.label;
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
            GUILayout.Label("Level Complete", _modalTitleStyle);
            GUILayout.Space(8);
            GUILayout.Label("Game Paused.\nPress <b>Esc</b> to return to Level Select, or <b>R</b> to restart.", _modalBodyStyle);
            GUILayout.Space(16);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Level Select (Esc)", _buttonStyle, GUILayout.Width(180f)))
            {
                TryUnpause();
                SceneManager.LoadScene("LevelSelect");
            }
            GUILayout.Space(12);
            if (GUILayout.Button("Restart (R)", _buttonStyle, GUILayout.Width(160f)))
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

