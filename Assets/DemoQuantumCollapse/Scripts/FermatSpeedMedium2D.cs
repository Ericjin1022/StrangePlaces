using UnityEngine;

namespace StrangePlaces.DemoQuantumCollapse
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public sealed class FermatSpeedMedium2D : MonoBehaviour, IObservationTarget
    {
        [Header("Speed")]
        [SerializeField] private float slowMultiplier = 0.35f;
        [SerializeField] private float fastMultiplier = 1.75f;
        [SerializeField] private bool observedForcesFast = true;

        [Header("Visual")]
        [SerializeField] private Color slowColor = new(0.2f, 0.45f, 1f, 0.7f);
        [SerializeField] private Color fastColor = new(1f, 1f, 0.25f, 0.55f);

        private Collider2D _collider2D;
        private Renderer _renderer;
        private Material _materialInstance;
        private bool _observed;
        private float _currentMultiplier;

        public float CurrentSpeedMultiplier => _currentMultiplier;
        public bool IsObserved => _observed;

        public Vector2 ObservationPoint => transform.position;
        public Collider2D PrimaryCollider => _collider2D;

        private void Awake()
        {
            _collider2D = GetComponent<Collider2D>();
            _renderer = GetComponent<Renderer>();

            // As a "medium zone" it should be a trigger.
            _collider2D.isTrigger = true;

            if (_renderer != null)
            {
                _materialInstance = new Material(Shader.Find("Sprites/Default"));
                _renderer.sharedMaterial = _materialInstance;
            }

            _currentMultiplier = slowMultiplier;
            ApplyVisual();
        }

        public void SetObserved(bool observed)
        {
            if (_observed == observed)
            {
                return;
            }

            _observed = observed;
            if (_observed && observedForcesFast)
            {
                _currentMultiplier = fastMultiplier;
            }
            else if (!_observed)
            {
                _currentMultiplier = slowMultiplier;
            }

            ApplyVisual();

            Debug.Log($"[费尔马] 介质观察变化：{name} observed={(_observed ? "是" : "否")} 倍率={_currentMultiplier:0.00}");

            FermatSceneController2D controller = FindFirstObjectByType<FermatSceneController2D>();
            if (controller != null)
            {
                controller.RequestReplan();
                return;
            }

            FermatProbe2D probe = FindFirstObjectByType<FermatProbe2D>();
            if (probe != null)
            {
                probe.RequestReplan();
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

        private void ApplyVisual()
        {
            if (_materialInstance == null)
            {
                return;
            }

            bool fast = _currentMultiplier >= (slowMultiplier + fastMultiplier) * 0.5f;
            _materialInstance.color = fast ? fastColor : slowColor;
        }
    }
}
