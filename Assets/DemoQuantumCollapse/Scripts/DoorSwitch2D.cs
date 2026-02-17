using UnityEngine;

namespace StrangePlaces.DemoQuantumCollapse
{
    [DisallowMultipleComponent]
    public sealed class DoorSwitch2D : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private string doorObjectName = "Door";
        [SerializeField] private DoorController2D door;

        [Header("Trigger")]
        [Tooltip("只在红箱明显向上运动时触发。")]
        [SerializeField] private float minUpVelocity = 1.5f;

        [Header("Lock Box")]
        [Tooltip("红色方块完全进入黄色区域时，将其固定住不再移动。")]
        [SerializeField] private bool lockBoxWhenFullyInside = true;
        [SerializeField] private float lockContainmentPadding = 0.02f;
        [SerializeField] private RigidbodyType2D lockedBodyType = RigidbodyType2D.Static;
        [SerializeField] private bool disableBoxScriptWhenLocked = true;

        [Header("Visual")]
        [SerializeField] private Color idleColor = new(1f, 0.9f, 0.35f, 1f);
        [SerializeField] private Color triggeredColor = new(0.25f, 1f, 0.45f, 0.6f);

        private BoxCollider2D _trigger;
        private DemoColorRenderer _colorRenderer;
        private bool _triggered;
        private bool _lockedBox;

        private void Awake()
        {
            Ensure2D();
            TryAutoBindDoor();
            Apply();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryTrigger(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            TryTrigger(other);
        }

        public void ResetSwitch()
        {
            _triggered = false;
            _lockedBox = false;
            Apply();
        }

        private void TryTrigger(Collider2D other)
        {
            if (other == null)
            {
                return;
            }

            NegativeMassBox2D box = other.GetComponentInParent<NegativeMassBox2D>();
            if (box == null || !box.CanTriggerDoorSwitch)
            {
                return;
            }

            if (lockBoxWhenFullyInside && !_lockedBox && TryLockBoxIfContained(box))
            {
                if (door == null)
                {
                    TryAutoBindDoor();
                }

                _triggered = true;
                if (door != null)
                {
                    door.Open();
                }
                Apply();
                return;
            }

            if (_triggered)
            {
                return;
            }

            Rigidbody2D rb = box.Body != null ? box.Body : box.GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                return;
            }

            if (rb.velocity.y < minUpVelocity)
            {
                return;
            }

            if (rb.worldCenterOfMass.y > transform.position.y + 0.15f)
            {
                return;
            }

            if (door == null)
            {
                TryAutoBindDoor();
            }

            _triggered = true;
            if (door != null)
            {
                door.Open();
            }
            Apply();
        }

        private bool TryLockBoxIfContained(NegativeMassBox2D box)
        {
            if (box == null || _trigger == null)
            {
                return false;
            }

            Collider2D boxCollider = box.GetComponent<Collider2D>();
            if (boxCollider == null)
            {
                return false;
            }

            Bounds zone = _trigger.bounds;
            Bounds b = boxCollider.bounds;

            float pad = Mathf.Max(0f, lockContainmentPadding);
            if (pad > 0.0001f)
            {
                zone.Expand(new Vector3(-pad * 2f, -pad * 2f, 0f));
            }

            if (zone.size.x <= 0.0001f || zone.size.y <= 0.0001f)
            {
                return false;
            }

            if (!zone.Contains(b.min) || !zone.Contains(b.max))
            {
                return false;
            }

            Rigidbody2D rb = box.Body != null ? box.Body : box.GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                return false;
            }

            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeAll;
            rb.bodyType = lockedBodyType;

            if (disableBoxScriptWhenLocked)
            {
                box.enabled = false;
            }

            _lockedBox = true;
            return true;
        }

        private void Apply()
        {
            if (_colorRenderer != null)
            {
                _colorRenderer.SetColor(_triggered ? triggeredColor : idleColor);
            }

            if (_trigger != null)
            {
                _trigger.enabled = !_triggered;
            }
        }

        private void TryAutoBindDoor()
        {
            if (door != null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(doorObjectName))
            {
                return;
            }

            GameObject doorGo = GameObject.Find(doorObjectName.Trim());
            if (doorGo == null)
            {
                return;
            }

            door = doorGo.GetComponent<DoorController2D>();
        }

        private void Ensure2D()
        {
            Remove3DColliders(gameObject);

            if (_trigger == null)
            {
                _trigger = GetComponent<BoxCollider2D>();
            }

            if (_trigger == null)
            {
                _trigger = gameObject.AddComponent<BoxCollider2D>();
            }

            _trigger.size = Vector2.one;
            _trigger.offset = Vector2.zero;
            _trigger.isTrigger = true;

            if (_colorRenderer == null)
            {
                _colorRenderer = GetComponent<DemoColorRenderer>();
            }

            if (_colorRenderer == null)
            {
                _colorRenderer = gameObject.AddComponent<DemoColorRenderer>();
            }
        }

        private static void Remove3DColliders(GameObject go)
        {
            if (go == null)
            {
                return;
            }

            Collider[] colliders = go.GetComponents<Collider>();
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(colliders[i]);
                    }
                    else
                    {
                        DestroyImmediate(colliders[i]);
                    }
                }
            }
        }
    }
}
