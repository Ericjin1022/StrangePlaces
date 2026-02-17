using UnityEngine;

namespace StrangePlaces.DemoQuantumCollapse
{
    public sealed class SimpleCameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new(0f, 1.5f, -10f);
        [SerializeField] private float followSharpness = 12f;

        private void LateUpdate()
        {
            if (target == null)
            {
                PlayerController2D player = FindFirstObjectByType<PlayerController2D>();
                if (player != null)
                {
                    target = player.transform;
                }

                if (target == null)
                {
                return;
                }
            }

            Vector3 desired = target.position + offset;
            float t = 1f - Mathf.Exp(-followSharpness * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, desired, t);
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }
    }
}
