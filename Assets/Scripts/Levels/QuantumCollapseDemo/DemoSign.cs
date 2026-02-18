using UnityEngine;

namespace StrangePlaces.DemoQuantumCollapse
{
    public sealed class DemoSign : MonoBehaviour
    {
        [TextArea(2, 10)]
        [SerializeField] private string text = "（空白牌子）";

        [Header("Layout")]
        [SerializeField] private Vector2 size = new(5.2f, 1.6f);
        [SerializeField] private float padding = 0.18f;

        [Header("Look")]
        [SerializeField] private Color boardColor = new(0.12f, 0.12f, 0.16f, 0.92f);
        [SerializeField] private Color textColor = new(0.92f, 0.92f, 0.95f, 1f);
        [SerializeField] private int sortingOrder = 5;

        [Header("Eldritch (Optional)")]
        [SerializeField] private bool subtleFlicker = true;
        [SerializeField] private float flickerStrength = 0.06f;
        [SerializeField] private float flickerSpeed = 2.2f;

        private MeshRenderer _boardRenderer;
        private Material _boardMaterialInstance;
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

            if (_boardMaterialInstance != null)
            {
                float t = 0.5f + 0.5f * Mathf.PerlinNoise(Time.time * flickerSpeed, 0.37f);
                float a = Mathf.Clamp01(boardColor.a * (1f - flickerStrength) + boardColor.a * flickerStrength * t);
                Color c = boardColor;
                c.a = a;
                _boardMaterialInstance.color = c;
            }
        }

        private void EnsureVisuals()
        {
            if (_boardRenderer == null)
            {
                _boardRenderer = GetComponent<MeshRenderer>();
            }

            if (_boardRenderer == null)
            {
                Transform boardT = transform.Find("Board");
                if (boardT != null)
                {
                    _boardRenderer = boardT.GetComponent<MeshRenderer>();
                }
            }

            if (_boardRenderer == null)
            {
                _boardRenderer = GetComponentInChildren<MeshRenderer>();
            }

            if (_boardMaterialInstance == null && _boardRenderer != null)
            {
                _boardMaterialInstance = new Material(Shader.Find("Sprites/Default"));
                _boardRenderer.sharedMaterial = _boardMaterialInstance;
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
                _textMesh = GetComponentInChildren<TextMesh>();
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

            Transform board = _boardRenderer != null ? _boardRenderer.transform : null;
            if (board != null)
            {
                board.localScale = new Vector3(size.x, size.y, 1f);
            }

            if (_boardRenderer != null)
            {
                _boardRenderer.sortingLayerName = "Default";
                _boardRenderer.sortingOrder = sortingOrder;
            }

            if (_boardMaterialInstance != null)
            {
                _boardMaterialInstance.color = boardColor;
            }

            if (_textMesh != null)
            {
                _textMesh.text = string.IsNullOrWhiteSpace(text) ? "（空白牌子）" : text;
                _textMesh.color = textColor;

                // CharacterSize is in world units relative to font size; tweak for readability.
                _textMesh.fontSize = 52;
                _textMesh.characterSize = 0.08f;
                _textMesh.lineSpacing = 1.05f;

                float innerW = Mathf.Max(0.1f, size.x - padding * 2f);
                float innerH = Mathf.Max(0.1f, size.y - padding * 2f);
                _textMesh.transform.localPosition = new Vector3(-innerW * 0.5f, 0f, -0.01f);

                if (_textRenderer != null)
                {
                    _textRenderer.sortingLayerName = "Default";
                    _textRenderer.sortingOrder = sortingOrder + 1;
                }
            }
        }
    }
}
