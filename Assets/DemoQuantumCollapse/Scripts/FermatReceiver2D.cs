using UnityEngine;

namespace StrangePlaces.DemoQuantumCollapse
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class FermatReceiver2D : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private string doorObjectName = "Door";
        [SerializeField] private Collider2D doorCollider;
        [SerializeField] private Renderer doorRenderer;

        [Header("Visual")]
        [SerializeField] private Color closedColor = new(1f, 0.25f, 0.25f, 1f);
        [SerializeField] private Color openColor = new(0.25f, 1f, 0.45f, 0.35f);

        private bool _opened;
        private Material _doorMaterialInstance;

        private void Awake()
        {
            Collider2D c = GetComponent<Collider2D>();
            c.isTrigger = true;

            if (doorCollider == null || doorRenderer == null)
            {
                TryAutoBindDoor();
            }

            if (doorRenderer != null)
            {
                _doorMaterialInstance = new Material(Shader.Find("Sprites/Default"));
                doorRenderer.sharedMaterial = _doorMaterialInstance;
            }
            Apply();
        }

        public void OnProbeArrived(FermatProbe2D probe)
        {
            if (_opened)
            {
                return;
            }

            if (probe == null)
            {
                return;
            }

            _opened = true;
            Apply();
        }

        private void Apply()
        {
            if (doorCollider != null)
            {
                doorCollider.enabled = !_opened;
            }

            if (_doorMaterialInstance != null)
            {
                _doorMaterialInstance.color = _opened ? openColor : closedColor;
            }
        }

        private void TryAutoBindDoor()
        {
            if (string.IsNullOrWhiteSpace(doorObjectName))
            {
                return;
            }

            GameObject door = GameObject.Find(doorObjectName.Trim());
            if (door == null)
            {
                return;
            }

            if (doorCollider == null)
            {
                doorCollider = door.GetComponent<Collider2D>();
            }

            if (doorRenderer == null)
            {
                doorRenderer = door.GetComponent<Renderer>();
            }
        }
    }
}
