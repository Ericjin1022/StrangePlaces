using UnityEngine;

namespace StrangePlaces.DemoQuantumCollapse
{
    [DisallowMultipleComponent]
    public sealed class QuantumCollapseObservable : MonoBehaviour, IObservationTarget, IEntanglementReceiver
    {
        [Header("Superposition (Unobserved)")]
        [Tooltip("Unobserved: each switch picks a random offset around the initial position.")]
        [SerializeField, Min(0f)] private float randomPositionRange = 0.5f;
        [SerializeField, Min(0.01f)] private float switchIntervalSeconds = 0.18f;
        [SerializeField] private float wobblePositionAmplitude = 0.05f;
        [SerializeField] private float wobbleScaleAmplitude = 0.08f;
        [SerializeField] private float wobbleSpeed = 10f;

        [Header("Collapse (Observed)")]
        [Tooltip("If false: direct flashlight observation will not collapse this object.")]
        [SerializeField] private bool allowDirectObservation = true;
        [SerializeField] private bool solidOnlyWhenObserved = true;

        [Header("Visual")]
        [Tooltip("If true, swap SpriteRenderer.sprite to represent observed/unobserved.")]
        [SerializeField] private bool driveSpriteSwap = true;
        [SerializeField] private Sprite unobservedSprite;
        [SerializeField] private Sprite observedSprite;
        [Tooltip("When swapping sprites, optionally reset SpriteRenderer.color to white.")]
        [SerializeField] private bool resetColorToWhiteWhenSwapping = true;

        private Collider2D[] _colliders2D = System.Array.Empty<Collider2D>();
        private SpriteRenderer[] _spriteRenderers = System.Array.Empty<SpriteRenderer>();

        private bool _directObserved;
        private bool _entanglementObserved;
        private bool _effectiveObserved;
        private float _nextSwitchTime;
        private Vector3 _baseScale;
        private Vector3 _basePosition;
        private Vector2 _currentOffset;

        public Vector2 ObservationPoint => transform.position;
        public Collider2D PrimaryCollider => _colliders2D != null && _colliders2D.Length > 0 ? _colliders2D[0] : null;

        private void Awake()
        {
            _basePosition = transform.position;
            _baseScale = transform.localScale;

            _colliders2D = GetComponentsInChildren<Collider2D>(true);
            _spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);

            _currentOffset = Vector2.zero;
            PickNewOffset();
            _nextSwitchTime = Time.time + switchIntervalSeconds;

            ApplyVisuals();
        }

        private void Update()
        {
            if (_effectiveObserved)
            {
                return;
            }

            if (Time.time >= _nextSwitchTime)
            {
                _nextSwitchTime = Time.time + switchIntervalSeconds;
                PickNewOffset();
            }

            float wobble = Mathf.Sin(Time.time * wobbleSpeed);
            Vector2 wobbleOffset = new(
                wobble * wobblePositionAmplitude,
                Mathf.Cos(Time.time * wobbleSpeed * 1.3f) * wobblePositionAmplitude
            );

            Vector2 target = (Vector2)_basePosition + _currentOffset;
            transform.position = (Vector3)(target + wobbleOffset);

            float scaleWobble = 1f + wobble * wobbleScaleAmplitude;
            transform.localScale = _baseScale * scaleWobble;
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
                _currentOffset = Vector2.zero;
                transform.position = _basePosition;
                transform.localScale = _baseScale;
            }
            else
            {
                _nextSwitchTime = Time.time + switchIntervalSeconds;
                PickNewOffset();
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

        private void PickNewOffset()
        {
            float r = Mathf.Max(0f, randomPositionRange);
            if (r <= 0.0001f)
            {
                _currentOffset = Vector2.zero;
                return;
            }

            _currentOffset = new Vector2(Random.Range(-r, r), Random.Range(-r, r));
        }
    }
}

