using System.Collections;
using UnityEngine;

namespace StrangePlaces.Level_Eric_2
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class LoopFallingCrate2D : MonoBehaviour
    {
        [Header("\u8D77\u70B9")]
        [Tooltip("\u6728\u7BB1\u6BCF\u6B21\u91CD\u65B0\u843D\u4E0B\u7684\u8D77\u70B9\u3002\u4E3A\u7A7A\u65F6\u4F7F\u7528\u672C\u7269\u4F53\u7684\u521D\u59CB\u4F4D\u7F6E\u4F5C\u4E3A\u8D77\u70B9\u3002")]
        [SerializeField] private Transform spawnPoint;

        [Header("\u5224\u5B9A")]
        [Tooltip("\u6D88\u5931\u7EBF\u7684\u4F4D\u7F6E\u5F15\u7528\uFF08\u4F8B\u5982\u4E00\u4E2A\u7A7A\u7269\u4F53\u6216\u4E00\u6761\u7EBF\u7684 Transform\uFF09\u3002\u5982\u679C\u7ED1\u5B9A\u4E86\uFF0C\u5C06\u4F7F\u7528\u5B83\u7684 y \u9AD8\u5EA6\u4F5C\u4E3A\u6D88\u5931\u7EBF\u3002")]
        [SerializeField] private Transform disappearLine;

        [Header("\u91CD\u7F6E")]
        [SerializeField, Min(0f)] private float respawnDelaySeconds = 0f;
        [Tooltip("\u91CD\u7F6E\u540E\u7684\u521D\u59CB\u901F\u5EA6\uFF08\u901A\u5E38\u4E3A 0\uFF0C\u4F9D\u9760\u91CD\u529B\u81EA\u7136\u843D\u4E0B\uFF09\u3002")]
        [SerializeField] private Vector2 respawnVelocity = Vector2.zero;
        [SerializeField] private bool resetAngularVelocity = true;

        private Rigidbody2D _rigidbody2D;
        private Collider2D _collider2D;
        private Vector3 _fallbackSpawnPosition;
        private float _fallbackSpawnRotationZ;
        private bool _respawning;
        private bool _warnedMissingSpawn;
        private bool _warnedMissingDisappearLine;

        private void Awake()
        {
            _rigidbody2D = GetComponent<Rigidbody2D>();
            _collider2D = GetComponent<Collider2D>();

            _fallbackSpawnPosition = transform.position;
            _fallbackSpawnRotationZ = transform.eulerAngles.z;
        }

        private void Update()
        {
            if (_respawning)
            {
                return;
            }

            if (disappearLine == null)
            {
                WarnMissingDisappearLineOnce();
                return;
            }

            float y = transform.position.y;
            if (_collider2D != null)
            {
                y = _collider2D.bounds.min.y;
            }

            if (y < disappearLine.position.y)
            {
                TriggerRespawn();
            }
        }

        public void ForceRespawn()
        {
            if (_respawning)
            {
                return;
            }

            TriggerRespawn();
        }

        private void TriggerRespawn()
        {
            if (_respawning)
            {
                return;
            }

            StartCoroutine(RespawnRoutine());
        }

        private IEnumerator RespawnRoutine()
        {
            _respawning = true;

            float delay = Mathf.Max(0f, respawnDelaySeconds);
            if (delay > 0.0001f)
            {
                yield return new WaitForSeconds(delay);
            }

            DoRespawn();
            _respawning = false;
        }

        private void DoRespawn()
        {
            Vector3 pos;
            float rotZ;

            if (spawnPoint != null)
            {
                pos = spawnPoint.position;
                rotZ = spawnPoint.eulerAngles.z;
            }
            else
            {
                pos = _fallbackSpawnPosition;
                rotZ = _fallbackSpawnRotationZ;

                if (!_warnedMissingSpawn)
                {
                    _warnedMissingSpawn = true;
                    Debug.LogWarning("[\u5FAA\u73AF\u6728\u7BB1] \u672A\u914D\u7F6E spawnPoint\uFF0C\u5C06\u4F7F\u7528\u672C\u7269\u4F53\u521D\u59CB\u4F4D\u7F6E\u4F5C\u4E3A\u8D77\u70B9\u3002", this);
                }
            }

            if (_rigidbody2D != null)
            {
                _rigidbody2D.velocity = Vector2.zero;
                if (resetAngularVelocity)
                {
                    _rigidbody2D.angularVelocity = 0f;
                }

                _rigidbody2D.position = pos;
                _rigidbody2D.rotation = rotZ;
                _rigidbody2D.velocity = respawnVelocity;
                _rigidbody2D.WakeUp();
            }
            else
            {
                transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, 0f, rotZ));
            }

            Physics2D.SyncTransforms();
        }

        private void WarnMissingDisappearLineOnce()
        {
            if (_warnedMissingDisappearLine)
            {
                return;
            }

            _warnedMissingDisappearLine = true;
            Debug.LogWarning("[\u5FAA\u73AF\u6728\u7BB1] \u672A\u7ED1\u5B9A disappearLine\uff0c\u65E0\u6CD5\u5224\u5B9A\u6D88\u5931\u7EBF\u9AD8\u5EA6\u3002", this);
        }
    }
}
