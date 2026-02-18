using UnityEngine;

namespace StrangePlaces.Level3_ColorSwap
{
    [DisallowMultipleComponent]
    public sealed class EldritchSign2D : MonoBehaviour
    {
        [TextArea(2, 10)]
        [SerializeField] private string text = "（这里应该写提示）";

        [Header("布局")]
        [SerializeField] private Vector2 size = new(5.2f, 1.6f);
        [SerializeField] private float padding = 0.18f;

        [Header("外观")]
        [SerializeField] private Color boardColor = new(0.12f, 0.12f, 0.16f, 0.92f);
        [SerializeField] private Color textColor = new(0.92f, 0.92f, 0.95f, 1f);
        [SerializeField] private int sortingOrder = 20;

        [Header("克苏鲁（可选）")]
        [SerializeField] private bool subtleFlicker = true;
        [SerializeField] private float flickerStrength = 0.06f;
        [SerializeField] private float flickerSpeed = 2.2f;

        private SpriteRenderer _board;
        private TextMesh _textMesh;
        private Renderer _textRenderer;

        private void Awake()
        {
            EnsureVisuals();
            ApplyNow();
        }

        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                EnsureVisuals();
                ApplyNow();
            }
        }

        private void Update()
        {
            if (!subtleFlicker)
            {
                return;
            }

            if (_board == null)
            {
                return;
            }

            float t = 0.5f + 0.5f * Mathf.PerlinNoise(Time.time * flickerSpeed, 0.37f);
            float a = Mathf.Clamp01(boardColor.a * (1f - flickerStrength) + boardColor.a * flickerStrength * t);
            Color c = boardColor;
            c.a = a;
            _board.color = c;
        }

        private void EnsureVisuals()
        {
            if (_board == null)
            {
                Transform boardT = transform.Find("Board");
                if (boardT != null)
                {
                    _board = boardT.GetComponent<SpriteRenderer>();
                }
            }

            if (_board == null)
            {
                _board = GetComponentInChildren<SpriteRenderer>(true);
            }

            if (_textMesh == null)
            {
                Transform existing = transform.Find("Text");
                if (existing != null)
                {
                    _textMesh = existing.GetComponent<TextMesh>();
                }
            }

            if (_textMesh == null)
            {
                _textMesh = GetComponentInChildren<TextMesh>(true);
            }

            if (_textRenderer == null && _textMesh != null)
            {
                _textRenderer = _textMesh.GetComponent<Renderer>();
            }
        }

        private void ApplyNow()
        {
            size.x = Mathf.Max(0.5f, size.x);
            size.y = Mathf.Max(0.5f, size.y);
            padding = Mathf.Clamp(padding, 0f, 1f);

            if (_board != null)
            {
                _board.transform.localScale = new Vector3(size.x, size.y, 1f);
                _board.color = boardColor;
                _board.sortingLayerName = "Default";
                _board.sortingOrder = sortingOrder;
            }
            else
            {
                Debug.LogWarning("[提示牌] 未找到 Board（需要子物体名为 Board 且包含 SpriteRenderer）。");
            }

            if (_textMesh != null)
            {
                _textMesh.text = string.IsNullOrWhiteSpace(text) ? "（空白牌子）" : text;
                _textMesh.color = textColor;
                _textMesh.anchor = TextAnchor.MiddleLeft;
                _textMesh.alignment = TextAlignment.Left;

                if (_textRenderer != null)
                {
                    _textRenderer.sortingLayerName = "Default";
                    _textRenderer.sortingOrder = sortingOrder + 1;
                }
            }
            else
            {
                Debug.LogWarning("[提示牌] 未找到 Text（需要子物体名为 Text 且包含 TextMesh）。");
            }
        }
    }
}

