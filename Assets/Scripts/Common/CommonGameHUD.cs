using UnityEngine;
using UnityEngine.SceneManagement;

public class CommonGameHUD : MonoBehaviour
{
    private bool showControls = true;

    private string controlContent = "Move: A/D or ←/→    Jump: Space    Swap Color: E\n" +
                                                     "Flashlight: F    Restart: R    Level Select: Esc";

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

        GUILayout.BeginArea(new Rect(12, 12, 760, 220));

        if (showControls)
        {
            GUILayout.Label(controlContent, _labelStyle);
        }

        if (_won)
        {
            GUILayout.Space(10);
            GUI.color = new Color(0.9f, 0.9f, 0.9f, 1f); // White text instead of green
            GUILayout.Label("Stage Cleared! (Paused)", _winStyle);
        }

        GUILayout.EndArea();

        if (_won)
        {
            DrawWinModal();
        }
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
        GUI.color = new Color(0f, 0f, 0f, 0.85f); // Darker background overlay
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = old;

        GUI.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.95f); // Dark semi-transparent window
        GUILayout.BeginArea(r, GUI.skin.window);
        GUILayout.Space(4);
        GUILayout.Label("Level Complete", _modalTitleStyle);
        GUILayout.Space(8);
        GUILayout.Label("Game Paused.\nPress Esc to return to Level Select, or R to restart.", _modalBodyStyle);
        GUILayout.Space(16);

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Level Select (Esc)", _buttonStyle, GUILayout.Width(200f)))
        {
            TryUnpause();
            SceneManager.LoadScene("LevelSelect");
        }

        GUILayout.Space(12);
        if (GUILayout.Button("Restart (R)", _buttonStyle, GUILayout.Width(180f)))
        {
            TryUnpause();
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        GUILayout.EndArea();
    }
}