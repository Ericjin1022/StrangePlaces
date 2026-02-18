using System.Collections.Generic;
using UnityEngine;

namespace StrangePlaces.DemoQuantumCollapse
{
    [DisallowMultipleComponent]
    public sealed class NegativeMassBox2D : MonoBehaviour
    {
        [Header("门开关")]
        [SerializeField] private bool canTriggerDoorSwitch = true;

        [Header("Gravity")]
        [SerializeField] private float unblockedGravityScale = -3.2f;
        [SerializeField] private bool useCeilingBlock = false;
        [SerializeField] private float ceilingCheckDistance = 1.0f;
        [SerializeField] private LayerMask ceilingMask = ~0;
        [SerializeField] private float blockedGravityScale = 3.0f;

        [Header("负质量：受力反向响应")]
        [Tooltip("开启后：与动态刚体碰撞时，会根据相对速度施加反向力（近似“负质量”效果：受到推力会朝反方向运动）。")]
        [SerializeField] private bool invertPushResponse = true;
        [SerializeField] private float invertAccelerationGain = 30f;
        [SerializeField] private float maxInvertForce = 360f;
        [SerializeField] private float minRelativeSpeed = 0.05f;
        [SerializeField] private LayerMask invertAffectMask = ~0;
        [SerializeField] private float minPlayerPushAxis = 0.15f;
        [SerializeField] private float playerAssumedPushSpeed = 8f;
        [SerializeField, Range(0f, 1f)] private float sideContactNormalThreshold = 0.45f;
        [SerializeField] private bool pushBackPlayer = true;
        [SerializeField] private float playerPushbackMultiplier = 1.25f;
        [SerializeField] private float maxPlayerPushbackForce = 900f;

        [Header("骑乘加速（电梯）")]
        [SerializeField] private bool enableRiderBoost = false;
        [SerializeField] private float riderWeightToLiftMultiplier = 4.0f;
        [SerializeField] private float maxExtraLiftForce = 260f;
        [SerializeField] private float targetUpSpeed = 14f;
        [SerializeField] private float speedAssistGain = 28f;
        [SerializeField] private float takeoffImpulsePerWeight = 0.06f;
        [SerializeField] private float maxTakeoffImpulse = 8f;

        private Rigidbody2D _body;
        private BoxCollider2D _boxCollider2D;
        private readonly HashSet<Rigidbody2D> _riders = new();

        public Rigidbody2D Body => _body;
        public bool CanTriggerDoorSwitch => canTriggerDoorSwitch;

        private void OnCollisionEnter2D(Collision2D collision)
        {
            ApplyInversePush(collision);
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            ApplyInversePush(collision);
        }

        private void Awake()
        {
            Ensure2D();
            EnsureRiderSensor();
        }

        private void FixedUpdate()
        {
            Ensure2D();

            if (_body != null)
            {
                // Always use upward (negative) gravity for the negative-mass box.
                // We intentionally ignore the previous "ceiling block -> flip gravity" behavior.
                _body.gravityScale = unblockedGravityScale;

                if (enableRiderBoost && unblockedGravityScale < 0f && _riders.Count > 0)
                {
                    float extra = ComputeExtraLiftForce();
                    if (extra > 0.01f)
                    {
                        _body.AddForce(Vector2.up * extra, ForceMode2D.Force);
                    }
                }
            }
        }

        private void ApplyInversePush(Collision2D collision)
        {
            if (!invertPushResponse)
            {
                return;
            }

            if (_body == null || collision == null)
            {
                return;
            }

            Rigidbody2D otherBody = collision.rigidbody;
            if (otherBody == null || otherBody.bodyType != RigidbodyType2D.Dynamic)
            {
                return;
            }

            int otherLayer = otherBody.gameObject.layer;
            if ((invertAffectMask.value & (1 << otherLayer)) == 0)
            {
                return;
            }

            Vector2 relForForce = collision.relativeVelocity; // other - self

            bool hasSideContact = false;
            int contactCount = collision.contactCount;
            for (int i = 0; i < contactCount; i++)
            {
                Vector2 n = collision.GetContact(i).normal;
                float ax = Mathf.Abs(n.x);
                float ay = Mathf.Abs(n.y);
                if (ax >= sideContactNormalThreshold && ax >= ay)
                {
                    hasSideContact = true;
                    break;
                }
            }

            // When the pusher is the player, keep some "effective" relative speed while holding into the box.
            // Otherwise, once both bodies match velocity, relativeVelocity -> 0 and the inverse response disappears.
            PlayerController2D player = otherBody.GetComponent<PlayerController2D>();
            if (player != null && hasSideContact)
            {
                float axis = player.MoveAxis;
                if (Mathf.Abs(axis) >= minPlayerPushAxis)
                {
                    float toward = ((Vector2)transform.position - otherBody.position).x * axis;
                    bool pushingTowardBox = toward > 0.0001f;
                    if (pushingTowardBox)
                    {
                        float assumed = Mathf.Max(0f, playerAssumedPushSpeed);
                        if (assumed > 0.0001f)
                        {
                            relForForce.x = Mathf.Sign(axis) * Mathf.Max(Mathf.Abs(relForForce.x), assumed);
                        }

                        // Optional: make the box "push back" the player while they keep holding into it.
                        // This avoids the feel of "I can still brute-force push it forward" with strong player drive.
                        if (pushBackPlayer)
                        {
                            float pushMax = Mathf.Max(0f, maxPlayerPushbackForce) * Mathf.Max(0f, playerPushbackMultiplier);

                            // Push opposite to input direction so holding into the box gets you shoved back.
                            float pushbackX = Mathf.Clamp(-axis * pushMax, -pushMax, pushMax);

                            if (Mathf.Abs(pushbackX) > 0.0001f)
                            {
                                otherBody.AddForce(new Vector2(pushbackX, 0f), ForceMode2D.Force);
                            }
                        }
                    }
                }
            }

            if (relForForce.sqrMagnitude < (minRelativeSpeed * minRelativeSpeed))
            {
                return;
            }

            float gain = Mathf.Max(0f, invertAccelerationGain);
            float maxForce = Mathf.Max(0f, maxInvertForce);

            // Convert "desired opposite acceleration" into a force applied to this box.
            Vector2 force = -relForForce * gain * _body.mass;
            if (maxForce > 0.0001f && force.sqrMagnitude > (maxForce * maxForce))
            {
                force = force.normalized * maxForce;
            }

            if (force.sqrMagnitude > 0.000001f)
            {
                _body.AddForce(force, ForceMode2D.Force);
            }
        }

        public void ConfigureAsSolo(float newUnblockedGravityScale, float newBlockedGravityScale, float newCeilingCheckDistance, LayerMask newCeilingMask)
        {
            canTriggerDoorSwitch = true;
            unblockedGravityScale = newUnblockedGravityScale;
            blockedGravityScale = newBlockedGravityScale;
            ceilingCheckDistance = newCeilingCheckDistance;
            ceilingMask = newCeilingMask;
            useCeilingBlock = false;
            invertPushResponse = true;
            enableRiderBoost = false;
        }

        public void ConfigureAsElevator(float newUnblockedGravityScale, float newRiderWeightToLiftMultiplier, float newMaxExtraLiftForce)
        {
            canTriggerDoorSwitch = false;
            unblockedGravityScale = newUnblockedGravityScale;
            riderWeightToLiftMultiplier = newRiderWeightToLiftMultiplier;
            maxExtraLiftForce = newMaxExtraLiftForce;
            useCeilingBlock = false;
            invertPushResponse = true;
            enableRiderBoost = true;
            EnsureRiderSensor();
        }

        public void RegisterRider(Rigidbody2D riderBody)
        {
            if (riderBody == null)
            {
                return;
            }

            bool wasEmpty = _riders.Count == 0;
            if (_riders.Add(riderBody) && wasEmpty && enableRiderBoost)
            {
                TryApplyTakeoffImpulse(riderBody);
            }
        }

        public void UnregisterRider(Rigidbody2D riderBody)
        {
            if (riderBody == null)
            {
                return;
            }

            _riders.Remove(riderBody);
        }

        private void EnsureRiderSensor()
        {
            if (!enableRiderBoost)
            {
                return;
            }

            Transform existing = transform.Find("RiderSensor");
            if (existing == null)
            {
                Debug.LogError($"[负质量箱] 缺少子物体 'RiderSensor'（已关闭骑乘加速）。对象：{name}");
                enableRiderBoost = false;
                return;
            }

            GameObject sensorGo = existing.gameObject;
            BoxCollider2D sensorCollider = sensorGo.GetComponent<BoxCollider2D>();
            NegativeMassRiderSensor2D sensor = sensorGo.GetComponent<NegativeMassRiderSensor2D>();
            if (sensorCollider == null || sensor == null)
            {
                Debug.LogError($"[负质量箱] RiderSensor 未配置必要组件 BoxCollider2D/NegativeMassRiderSensor2D（已关闭骑乘加速）。对象：{name}");
                enableRiderBoost = false;
                return;
            }

            if (!sensorCollider.isTrigger)
            {
                Debug.LogWarning($"[负质量箱] RiderSensor 的 BoxCollider2D 不是 Trigger，骑乘检测可能失效。对象：{name}");
            }

            sensor.Bind(this);
        }

        private float ComputeExtraLiftForce()
        {
            float gravityMagnitude = Mathf.Abs(Physics2D.gravity.y);
            float extra = 0f;

            foreach (Rigidbody2D rider in _riders)
            {
                if (rider == null)
                {
                    continue;
                }

                float riderWeight = rider.mass * gravityMagnitude * Mathf.Max(0f, rider.gravityScale);
                extra += riderWeight * riderWeightToLiftMultiplier;
            }

            if (_body != null)
            {
                float speedDeficit = Mathf.Max(0f, targetUpSpeed - _body.velocity.y);
                extra += speedDeficit * speedAssistGain * _body.mass;
            }

            return Mathf.Clamp(extra, 0f, maxExtraLiftForce);
        }

        private void TryApplyTakeoffImpulse(Rigidbody2D riderBody)
        {
            if (_body == null || riderBody == null)
            {
                return;
            }

            if (_body.velocity.y > 0.2f)
            {
                return;
            }

            float gravityMagnitude = Mathf.Abs(Physics2D.gravity.y);
            float riderWeight = riderBody.mass * gravityMagnitude * Mathf.Max(0f, riderBody.gravityScale);
            float impulse = Mathf.Clamp(riderWeight * takeoffImpulsePerWeight, 0f, maxTakeoffImpulse);
            if (impulse > 0.01f)
            {
                _body.AddForce(Vector2.up * impulse, ForceMode2D.Impulse);
            }
        }

        private bool IsCeilingBlocked()
        {
            if (_boxCollider2D == null)
            {
                return false;
            }

            Bounds b = _boxCollider2D.bounds;
            Vector2 origin = new(b.center.x, b.max.y + 0.02f);
            RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.up, ceilingCheckDistance, ceilingMask);

            if (hit.collider == null)
            {
                return false;
            }

            if (hit.collider == _boxCollider2D)
            {
                return false;
            }

            if (hit.collider.isTrigger)
            {
                return false;
            }

            return true;
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

            if (_boxCollider2D == null)
            {
                _boxCollider2D = GetComponent<BoxCollider2D>();
            }

            if (_boxCollider2D == null)
            {
                // Require scene configuration (no runtime AddComponent).
            }

            if (_boxCollider2D == null)
            {
                Debug.LogError($"[负质量箱] 初始化失败：无法获取或添加 BoxCollider2D（对象：{go.name}）。");
                enabled = false;
                return;
            }

            // Collider size/offset are configured in the scene.

            if (_body == null)
            {
                _body = GetComponent<Rigidbody2D>();
            }

            if (_body == null)
            {
                // Require scene configuration (no runtime AddComponent).
            }

            if (_body == null)
            {
                Debug.LogError($"[负质量箱] 初始化失败：无法获取或添加 Rigidbody2D（对象：{go.name}）。");
                enabled = false;
                return;
            }

            // Rigidbody settings are configured in the scene.
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
