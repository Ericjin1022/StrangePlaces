using UnityEngine;

namespace StrangePlaces.DemoQuantumCollapse
{
    [DisallowMultipleComponent]
    public sealed class DoorController2D : MonoBehaviour
    {
        [Header("State")]
        [SerializeField] private bool startOpen = false;

        private void Awake()
        {
            Ensure2D();
            if (startOpen)
            {
                Despawn();
            }
        }

        public void Open()
        {
            Despawn();
        }

        private void Ensure2D()
        {
            GameObject go = gameObject;
            if (go == null)
            {
                enabled = false;
                return;
            }

            Remove3DColliders(go);

            BoxCollider2D box = GetComponent<BoxCollider2D>();
            if (box == null)
            {
                box = go.AddComponent<BoxCollider2D>();
            }

            if (box == null)
            {
                Debug.LogError("[门] 初始化失败：无法获取或添加 BoxCollider2D。");
                enabled = false;
                return;
            }

            box.size = Vector2.one;
            box.offset = Vector2.zero;
            box.isTrigger = false;
            box.enabled = true;
        }

        private void Despawn()
        {
            if (Application.isPlaying)
            {
                Destroy(gameObject);
            }
            else
            {
                gameObject.SetActive(false);
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

