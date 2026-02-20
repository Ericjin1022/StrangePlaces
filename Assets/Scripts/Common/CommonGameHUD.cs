using UnityEngine;
using UnityEngine.SceneManagement;

public class CommonGameHUD : MonoBehaviour
{
    [SerializeField] private bool showControls = true;

    [SerializeField] private string controlContent = "移动：A/D 或 ←/→    跳跃：空格    切换黑白：E\n" +
                                                     "重开：R    返回选关：Esc\n" +
                                                     "规则：玩家只能站在与自身同色的物体上。\n" +
                                                     "目标：到达出口（绿色方块）。";

    [SerializeField] private string title = "第三关：黑白切换";

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
        GUILayout.Label($"<b>{title}</b>", _titleStyle);

        if (showControls)
        {
            GUILayout.Label(controlContent, _labelStyle);
        }

        if (_won)
        {
            GUILayout.Space(10);
            GUI.color = new Color(0.3f, 1f, 0.4f, 1f);
            GUILayout.Label("通关！（已暂停）", _winStyle);
        }

        GUILayout.EndArea();

        if (_won)
        {
            DrawWinModal();
        }
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
        GUILayout.Label("游戏已暂停。\n按 Esc 返回选关，按 R 重新开始本关。", _modalBodyStyle);
        GUILayout.Space(16);

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("返回选关（Esc）", _buttonStyle, GUILayout.Width(200f)))
        {
            TryUnpause();
            SceneManager.LoadScene("LevelSelect");
        }

        GUILayout.Space(12);
        if (GUILayout.Button("重开本关（R）", _buttonStyle, GUILayout.Width(180f)))
        {
            TryUnpause();
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        GUILayout.EndArea();
    }
}