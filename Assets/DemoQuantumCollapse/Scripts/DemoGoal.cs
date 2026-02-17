using UnityEngine;

namespace StrangePlaces.DemoQuantumCollapse
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class DemoGoal : MonoBehaviour
    {
        private void Awake()
        {
            Collider2D collider2D = GetComponent<Collider2D>();
            collider2D.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            PlayerController2D player = other.GetComponent<PlayerController2D>();
            if (player == null)
            {
                return;
            }

            DemoHUD hud = FindFirstObjectByType<DemoHUD>();
            if (hud != null)
            {
                hud.SetWin(true);
            }
        }
    }
}

