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

        private Collider2D[] _colliders2D = System.Array.Empty<Collider2D>();
        private SpriteRenderer[] _spriteRenderers = System.Array.Empty<SpriteRenderer>();
        private Renderer[] _otherRenderers = System.Array.Empty<Renderer>();
        private MaterialPropertyBlock _mpb;

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
            _otherRenderers = GetComponentsInChildren<Renderer>(true);
            _mpb = new MaterialPropertyBlock();

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

            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * wobbleSpeed * 0.7f);
            pulse = Mathf.Clamp01(pulse);
            ApplyColor(Color.Lerp(unobservedColor * 0.6f, unobservedColor, pulse));
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

            ApplyColor(observed ? observedColor : unobservedColor);
        }

        private void ApplyColor(Color color)
        {
            if (_spriteRenderers != null && _spriteRenderers.Length > 0)
            {
                for (int i = 0; i < _spriteRenderers.Length; i++)
                {
                    if (_spriteRenderers[i] != null)
                    {
                        _spriteRenderers[i].color = color;
                    }
                }

                return;
            }

            if (_otherRenderers == null || _otherRenderers.Length == 0)
            {
                return;
            }

            if (_mpb == null)
            {
                _mpb = new MaterialPropertyBlock();
            }

            _mpb.Clear();
            _mpb.SetColor("_Color", color);
            _mpb.SetColor("_BaseColor", color);
            for (int i = 0; i < _otherRenderers.Length; i++)
            {
                Renderer r = _otherRenderers[i];
                if (r != null)
                {
                    r.SetPropertyBlock(_mpb);
                }
            }
        }
    }
}
