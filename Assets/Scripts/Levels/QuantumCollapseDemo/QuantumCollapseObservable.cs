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
        [SerializeField] private Color unobservedColor = new(0.9f, 0.3f, 1f, 1f);
        [SerializeField] private Color observedColor = new(1f, 1f, 1f, 1f);

        private Collider2D _collider2D;
        private Renderer _renderer;
        private Material _materialInstance;

        private bool _directObserved;
        private bool _entanglementObserved;
        private bool _effectiveObserved;
        private int _index;
        private float _nextSwitchTime;
        private Vector3 _baseScale;

        public Vector2 ObservationPoint => transform.position;
        public Collider2D PrimaryCollider => _collider2D;

        private void Awake()
        {
            _collider2D = GetComponent<Collider2D>();
            _renderer = GetComponent<Renderer>();
            _baseScale = transform.localScale;

            if (_renderer != null)
            {
                _materialInstance = new Material(Shader.Find("Sprites/Default"));
                _renderer.sharedMaterial = _materialInstance;
            }

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

            if (_materialInstance != null)
            {
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * wobbleSpeed * 0.7f);
                pulse = Mathf.Clamp01(pulse);
                _materialInstance.color = Color.Lerp(unobservedColor * 0.6f, unobservedColor, pulse);
            }
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

            return collider == _collider2D || collider.transform.IsChildOf(transform);
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
            if (_collider2D != null && solidOnlyWhenObserved)
            {
                _collider2D.enabled = true;
                _collider2D.isTrigger = !observed;
            }

            if (_materialInstance != null)
            {
                _materialInstance.color = observed ? observedColor : unobservedColor;
            }
        }
    }
}
