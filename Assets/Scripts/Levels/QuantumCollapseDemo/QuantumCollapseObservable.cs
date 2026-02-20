using UnityEngine;

namespace StrangePlaces.DemoQuantumCollapse
{
    [DisallowMultipleComponent]
    public sealed class QuantumCollapseObservable : MonoBehaviour, IObservationTarget, IEntanglementReceiver
    {
        [Header("Superposition (Unobserved)")]
        [SerializeField] private Vector2[] possibleWorldPositions = new Vector2[2];
        [SerializeField] private float switchIntervalSeconds = 0.18f;
        [SerializeField] private float wobblePositionAmplitude = 0.05f;
        [SerializeField] private float wobbleScaleAmplitude = 0.08f;
        [SerializeField] private float wobbleSpeed = 10f;

        [Header("Collapse (Observed)")]
        [Tooltip("关闭后：即使光锥直接照到本体，也不会坍缩；只能通过纠缠/其它外部机制使其进入稳定态。")]
        [SerializeField] private bool allowDirectObservation = true;
        [SerializeField] private bool solidOnlyWhenObserved = true;

        [Header("外观")]
        [Tooltip("为 true 时，通过切换 SpriteRenderer.sprite 来表现“量子态/稳定态”。")]
        [SerializeField] private bool driveSpriteSwap = true;
        [SerializeField] private Sprite unobservedSprite;
        [SerializeField] private Sprite observedSprite;
        [Tooltip("切换 Sprite 时，是否将 SpriteRenderer.color 归一为白色，避免被旧的染色影响。")]
        [SerializeField] private bool resetColorToWhiteWhenSwapping = true;


        private Collider2D[] _colliders2D = System.Array.Empty<Collider2D>();
        private SpriteRenderer[] _spriteRenderers = System.Array.Empty<SpriteRenderer>();

        private bool _directObserved;
        private bool _entanglementObserved;
        private bool _effectiveObserved;
        private int _index;
        private float _nextSwitchTime;
        private Vector3 _baseScale;

        public Vector2 ObservationPoint => transform.position;
        public Collider2D PrimaryCollider => _colliders2D != null && _colliders2D.Length > 0 ? _colliders2D[0] : null;

        private void Awake()
        {
            _baseScale = transform.localScale;

            _colliders2D = GetComponentsInChildren<Collider2D>(true);
            _spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);

            if (possibleWorldPositions == null || possibleWorldPositions.Length == 0)
            {
                possibleWorldPositions = new[] { (Vector2)transform.position };
            }

            _index = 0;
            _nextSwitchTime = Time.time + switchIntervalSeconds;
            ApplyVisuals();
        }

        private void Update()
        {
            if (_effectiveObserved)
            {
                return;
            }

            if (possibleWorldPositions.Length > 1 && Time.time >= _nextSwitchTime)
            {
                _index = (_index + 1) % possibleWorldPositions.Length;
                _nextSwitchTime = Time.time + switchIntervalSeconds;
            }

            Vector2 target = possibleWorldPositions[Mathf.Clamp(_index, 0, possibleWorldPositions.Length - 1)];
            float wobble = Mathf.Sin(Time.time * wobbleSpeed);
            Vector2 wobbleOffset = new(wobble * wobblePositionAmplitude, Mathf.Cos(Time.time * wobbleSpeed * 1.3f) * wobblePositionAmplitude);
            transform.position = (Vector3)(target + wobbleOffset);

            float scaleWobble = 1f + wobble * wobbleScaleAmplitude;
            transform.localScale = _baseScale * scaleWobble;

        }

        public void SetPossibleWorldPositions(Vector2[] positions)
        {
            possibleWorldPositions = positions != null && positions.Length > 0 ? positions : new[] { (Vector2)transform.position };
            _index = 0;
            _nextSwitchTime = Time.time + switchIntervalSeconds;
        }

        public void SetObserved(bool observed)
        {
            if (_directObserved == observed)
            {
                return;
            }

            _directObserved = observed;
            ApplyObservationState();
        }

        public void SetEntanglementObserved(bool observed)
        {
            if (_entanglementObserved == observed)
            {
                return;
            }

            _entanglementObserved = observed;
            ApplyObservationState();
        }

        public bool OwnsCollider(Collider2D collider)
        {
            if (collider == null)
            {
                return false;
            }

            return collider.transform.IsChildOf(transform);
        }

        private bool ComputeEffectiveObserved()
        {
            bool direct = allowDirectObservation && _directObserved;
            return direct || _entanglementObserved;
        }

        private void ApplyObservationState()
        {
            bool observedNow = ComputeEffectiveObserved();
            if (_effectiveObserved == observedNow)
            {
                ApplyVisuals(observedNow);
                return;
            }

            _effectiveObserved = observedNow;
            if (_effectiveObserved)
            {
                Vector2 collapsed = possibleWorldPositions[Mathf.Clamp(_index, 0, possibleWorldPositions.Length - 1)];
                transform.position = collapsed;
                transform.localScale = _baseScale;
            }
            else
            {
                _nextSwitchTime = Time.time + switchIntervalSeconds;
            }

            ApplyVisuals(_effectiveObserved);
        }

        private void ApplyVisuals()
        {
            ApplyVisuals(_effectiveObserved);
        }

        private void ApplyVisuals(bool observed)
        {
            if (solidOnlyWhenObserved && _colliders2D != null && _colliders2D.Length > 0)
            {
                for (int i = 0; i < _colliders2D.Length; i++)
                {
                    Collider2D c = _colliders2D[i];
                    if (c == null)
                    {
                        continue;
                    }

                    c.enabled = true;
                    c.isTrigger = !observed;
                }
            }

            ApplySprite(observed);
        }

        private void ApplySprite(bool observed)
        {
            if (!driveSpriteSwap)
            {
                return;
            }

            if (_spriteRenderers == null || _spriteRenderers.Length == 0)
            {
                return;
            }

            Sprite sprite = observed ? observedSprite : unobservedSprite;
            if (sprite == null)
            {
                return;
            }

            for (int i = 0; i < _spriteRenderers.Length; i++)
            {
                SpriteRenderer r = _spriteRenderers[i];
                if (r == null)
                {
                    continue;
                }

                r.sprite = sprite;
                if (resetColorToWhiteWhenSwapping)
                {
                    r.color = Color.white;
                }
            }
        }

    }
}

