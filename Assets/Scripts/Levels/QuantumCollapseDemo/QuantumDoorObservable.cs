using UnityEngine;

namespace StrangePlaces.DemoQuantumCollapse
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public sealed class QuantumDoorObservable : MonoBehaviour, IObservationTarget
    {
        [Header("Unobserved (Fluctuating)")]
        [SerializeField] private float switchIntervalSeconds = 0.35f;
        [SerializeField] private bool startClosed = true;

        [Header("Observed (Collapsed)")]
        [SerializeField] private bool observedForcesClosed = true;

        [Header("外观")]
        [Tooltip("为 true 时，通过切换贴图来表现开/关状态（优先级高于颜色）。")]
        [SerializeField] private bool driveSpriteSwap = true;
        [SerializeField] private Sprite closedSprite;
        [SerializeField] private Sprite openSprite;
        [Tooltip("切换贴图时，是否将颜色归一为白色，避免被旧的染色影响。")]
        [SerializeField] private bool resetColorToWhiteWhenSwapping = true;

        [SerializeField, HideInInspector] private Color closedColor = new(1f, 0.35f, 0.35f, 1f);
        [SerializeField, HideInInspector] private Color openColor = new(0.35f, 1f, 0.55f, 0.35f);

        private Collider2D _collider2D;
        private Renderer _renderer;
        private MaterialPropertyBlock _mpb;

        private bool _observed;
        private bool _closed;
        private float _nextSwitchTime;

        public Vector2 ObservationPoint => transform.position;
        public Collider2D PrimaryCollider => _collider2D;

        private void Awake()
        {
            _collider2D = GetComponent<Collider2D>();
            _renderer = GetComponent<Renderer>();
            _mpb = new MaterialPropertyBlock();

            if (_renderer != null)
            {
                if (_renderer.sharedMaterial == null)
                {
                    _renderer.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
                }
            }

            _closed = startClosed;
            _nextSwitchTime = Time.time + switchIntervalSeconds;
            ApplyState();
        }

        private void Update()
        {
            if (_observed)
            {
                return;
            }

            if (Time.time >= _nextSwitchTime)
            {
                _closed = !_closed;
                _nextSwitchTime = Time.time + switchIntervalSeconds;
                ApplyState();
            }
        }

        public void SetObserved(bool observed)
        {
            if (_observed == observed)
            {
                return;
            }

            _observed = observed;

            if (_observed && observedForcesClosed)
            {
                _closed = true;
                ApplyState();
                return;
            }

            if (!_observed)
            {
                _nextSwitchTime = Time.time + switchIntervalSeconds;
            }
        }

        private void ApplyState()
        {
            if (_collider2D != null)
            {
                _collider2D.enabled = true;
                _collider2D.isTrigger = !_closed;
            }

            if (_renderer != null)
            {
                ApplyVisual();
            }
        }

        private void ApplyVisual()
        {
            if (_renderer == null)
            {
                return;
            }

            if (_mpb == null)
            {
                _mpb = new MaterialPropertyBlock();
            }

            _mpb.Clear();

            if (driveSpriteSwap)
            {
                Sprite sprite = _closed ? closedSprite : openSprite;
                if (sprite != null)
                {
                    _mpb.SetTexture("_MainTex", sprite.texture);
                    _mpb.SetVector("_MainTex_ST", ComputeMainTexST(sprite));

                    if (resetColorToWhiteWhenSwapping)
                    {
                        _mpb.SetColor("_Color", Color.white);
                    }

                    _renderer.SetPropertyBlock(_mpb);
                    return;
                }
            }

            _mpb.SetColor("_Color", _closed ? closedColor : openColor);
            _renderer.SetPropertyBlock(_mpb);
        }

        private static Vector4 ComputeMainTexST(Sprite sprite)
        {
            if (sprite == null || sprite.texture == null)
            {
                return new Vector4(1f, 1f, 0f, 0f);
            }

            Texture2D tex = sprite.texture;
            Rect r = sprite.textureRect;
            if (tex.width <= 0 || tex.height <= 0)
            {
                return new Vector4(1f, 1f, 0f, 0f);
            }

            float scaleX = r.width / tex.width;
            float scaleY = r.height / tex.height;
            float offsetX = r.x / tex.width;
            float offsetY = r.y / tex.height;
            return new Vector4(scaleX, scaleY, offsetX, offsetY);
        }

        public bool OwnsCollider(Collider2D collider)
        {
            if (collider == null)
            {
                return false;
            }

            return collider == _collider2D || collider.transform.IsChildOf(transform);
        }
    }
}

