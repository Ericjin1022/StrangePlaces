using StrangePlaces.DemoQuantumCollapse;
using UnityEngine;

namespace StrangePlaces.Level3_ColorSwap
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public sealed class Level3ExitGoal2D : MonoBehaviour
    {
        private bool _done;

        private void Awake()
        {
            Collider2D c = GetComponent<Collider2D>();
            if (c != null)
            {
                c.isTrigger = true;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_done)
            {
                return;
            }

            if (other == null)
            {
                return;
            }

            PlayerController2D player = other.GetComponentInParent<PlayerController2D>();
            if (player == null)
            {
                return;
            }

            _done = true;

            Level3HUD hud = FindFirstObjectByType<Level3HUD>();
            if (hud == null)
            {
                Debug.LogWarning("[第三关] 已到达出口，但场景中未找到 Level3HUD，无法显示通关界面。", this);
                return;
            }

            hud.SetWin(true);
        }
    }
}

