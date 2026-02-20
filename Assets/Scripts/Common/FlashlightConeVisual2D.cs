using UnityEngine;

namespace StrangePlaces.DemoQuantumCollapse
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public sealed class FlashlightConeVisual2D : MonoBehaviour
    {
        private const string MaterialResourcePath = "FlashlightCone2D_Mat";

        [Header("Shape")]
        [SerializeField] private float length = 7f;
        [SerializeField] private float angleDegrees = 60f;
        [SerializeField] private int segments = 28;

        [Header("Look")]
        [SerializeField] private Color color = new(1f, 1f, 0.3f, 0.35f);
        [SerializeField, Range(0f, 0.5f)] private float edgeSoftness = 0.18f;
        [SerializeField, Range(0f, 1f)] private float lengthSoftness = 0.35f;
        [SerializeField, Range(0f, 2f)] private float centerBoost = 0.35f;
        [SerializeField] private int sortingOrder = 100;

        [Header("Drive")]
        [SerializeField] private PlayerController2D player;

        private MeshRenderer _meshRenderer;
        private bool _warnedMissingPlayer;

        private void Awake()
        {
            _meshRenderer = GetComponent<MeshRenderer>();
            WarnIfMissingPlayer();
        }

        private void LateUpdate()
        {
            if (!WarnIfMissingPlayer())
            {
                return;
            }

            Vector2 aim = player != null ? player.AimDirection : (Vector2)transform.right;
            if (aim.sqrMagnitude > 0.001f)
            {
                transform.right = new Vector3(aim.x, aim.y, 0f);
            }
        }

        private bool WarnIfMissingPlayer()
        {
            if (player != null)
            {
                return true;
            }

            if (_warnedMissingPlayer)
            {
                return false;
            }

            _warnedMissingPlayer = true;
            Debug.LogWarning("[手电筒] FlashlightConeVisual2D 未绑定 player，请在 Inspector 中把玩家上的 PlayerController2D 拖到此字段。", this);
            return false;
        }

        public void SetPlayer(PlayerController2D newPlayer)
        {
            player = newPlayer;
        }

        public void Configure(float newLength, float newAngleDegrees)
        {
            length = newLength;
            angleDegrees = newAngleDegrees;
        }

        public void SetSortingOrder(int newSortingOrder)
        {
            sortingOrder = newSortingOrder;
            if (_meshRenderer != null)
            {
                _meshRenderer.sortingOrder = sortingOrder;
            }
        }

    }
}
