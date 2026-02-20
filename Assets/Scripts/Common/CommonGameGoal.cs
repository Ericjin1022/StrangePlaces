using StrangePlaces.DemoQuantumCollapse;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class CommonGameGoal : MonoBehaviour
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

        CommonGameHUD hud = FindFirstObjectByType<CommonGameHUD>();
        if (hud != null)
        {
            hud.SetWin(true);
        }
    }
}
