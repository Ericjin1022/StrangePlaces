using UnityEngine;

namespace StrangePlaces.DemoQuantumCollapse
{
    [DisallowMultipleComponent]
    public sealed class NegativeMassGoal : MonoBehaviour
    {
        private BoxCollider2D _trigger;

        private void Awake()
        {
            Ensure2D();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            PlayerController2D player = other != null ? other.GetComponent<PlayerController2D>() : null;
            if (player == null)
            {
                return;
            }

            NegativeMassHUD hud = FindFirstObjectByType<NegativeMassHUD>();
            if (hud != null)
            {
                hud.SetWin(true);
            }
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
