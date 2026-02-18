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
                // Require scene configuration (no runtime AddComponent).
            }

            if (_trigger == null)
            {
                Debug.LogError("[终点] 初始化失败：未在场景中配置 BoxCollider2D（且应为 Trigger）。");
                enabled = false;
                return;
            }

            if (!_trigger.isTrigger)
            {
                Debug.LogWarning("[终点] BoxCollider2D 不是 Trigger，可能导致无法触发通关。");
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
