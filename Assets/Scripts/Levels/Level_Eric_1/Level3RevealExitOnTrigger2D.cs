using StrangePlaces.DemoQuantumCollapse;
using UnityEngine;

namespace StrangePlaces.Level3_ColorSwap
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public sealed class Level3RevealExitOnTrigger2D : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Objects to show/enable when triggered.")]
        [SerializeField] private GameObject[] objectsToReveal;

        [Header("行为")]
        [Tooltip("游戏开始时是否隐藏出口物体。")]
        [SerializeField] private bool hideExitOnStart = true;
        [Tooltip("显示出口后是否禁用本触发器脚本，避免重复触发。")]
        [SerializeField] private bool disableAfterReveal = true;

        private bool _revealed;

        private void Awake()
        {
            Collider2D c = GetComponent<Collider2D>();
            if (c != null)
            {
                c.isTrigger = true;
            }

            if (hideExitOnStart)
            {
                SetExitActive(false);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_revealed)
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

            _revealed = true;
            SetExitActive(true);

            if (disableAfterReveal)
            {
                gameObject.SetActive(false);
            }
        }

        private void SetExitActive(bool active)
        {
            if (objectsToReveal == null || objectsToReveal.Length == 0)
            {
                Debug.LogWarning("[Level3] No objects to reveal are assigned.", this);
                return;
            }

            foreach (var obj in objectsToReveal)
            {
                if (obj != null)
                {
                    obj.SetActive(active);
                }
            }
        }
    }
}


