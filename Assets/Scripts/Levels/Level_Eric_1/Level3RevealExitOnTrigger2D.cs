using StrangePlaces.DemoQuantumCollapse;
using UnityEngine;

namespace StrangePlaces.Level3_ColorSwap
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public sealed class Level3RevealExitOnTrigger2D : MonoBehaviour
    {
        [Header("引用")]
        [Tooltip("要显示/启用的出口物体（exit）。")]
        [SerializeField] private GameObject exitObject;

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
                enabled = false;
            }
        }

        private void SetExitActive(bool active)
        {
            if (exitObject == null)
            {
                Debug.LogWarning("[第三关] 未指定出口物体（exitObject），无法切换出口显示状态。", this);
                return;
            }

            exitObject.SetActive(active);
        }
    }
}


