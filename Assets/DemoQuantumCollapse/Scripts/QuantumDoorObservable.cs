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

        [Header("Visual")]
        [SerializeField] private Color closedColor = new(1f, 0.35f, 0.35f, 1f);
        [SerializeField] private Color openColor = new(0.35f, 1f, 0.55f, 0.35f);

        private Collider2D _collider2D;
        private Renderer _renderer;
        private Material _materialInstance;

        private bool _observed;
        private bool _closed;
        private float _nextSwitchTime;

        public Vector2 ObservationPoint => transform.position;
        public Collider2D PrimaryCollider => _collider2D;

        private void Awake()
        {
            _collider2D = GetComponent<Collider2D>();
            _renderer = GetComponent<Renderer>();

            if (_renderer != null)
            {
                _materialInstance = new Material(Shader.Find("Sprites/Default"));
                _renderer.sharedMaterial = _materialInstance;
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

            if (_materialInstance != null)
            {
                _materialInstance.color = _closed ? closedColor : openColor;
            }
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
