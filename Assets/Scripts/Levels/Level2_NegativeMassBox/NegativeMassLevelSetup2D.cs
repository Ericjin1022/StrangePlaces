using UnityEngine;

namespace StrangePlaces.DemoQuantumCollapse
{
    [ExecuteAlways]
    [DefaultExecutionOrder(-1000)]
    public sealed class NegativeMassLevelSetup2D : MonoBehaviour
    {
        [Header("Names")]
        [SerializeField] private string levelRootName = "LevelRoot";
        [SerializeField] private string environmentName = "Environment";
        [SerializeField] private string doorName = "Door";
        [SerializeField] private string switchName = "DoorSwitch";
        [SerializeField] private string goalName = "Goal";
        [SerializeField] private string redSoloName = "RedBox_Solo";
        [SerializeField] private string redElevatorName = "RedBox_Elevator";
        [SerializeField] private string blackElevatorName = "BlackBox_Elevator";

        [Header("Elevator Tuning")]
        [SerializeField] private float elevatorRedGravityScale = -3.2f;
        [SerializeField] private float elevatorBoostMultiplier = 4.0f;
        [SerializeField] private float elevatorMaxExtraLiftForce = 260f;
        [SerializeField] private float blackBoxMass = 3.5f;
        [SerializeField] private float blackBoxGravityScale = 3.0f;

        [Header("Solo Red Tuning")]
        [SerializeField] private float soloRedUnblockedGravityScale = -3.2f;
        [SerializeField] private float soloRedBlockedGravityScale = 3.0f;
        [SerializeField] private float soloRedCeilingCheckDistance = 1.0f;

        private int _ensureAttempts;

        private void Awake()
        {
            TryEnsure();
        }

        private void OnEnable()
        {
            TryEnsure();
        }

        private void Update()
        {
            if (IsSceneReady())
            {
                return;
            }

            _ensureAttempts++;
            if (_ensureAttempts > 60)
            {
                return;
            }

            TryEnsure();
        }

        private void TryEnsure()
        {
            try
            {
                EnsureHUD();
                EnsurePlayerBody();
                EnsureEnvironmentColliders();
                EnsureDoorSwitchGoal();
                EnsureBoxes();

                Physics2D.SyncTransforms();
            }
            catch
            {
                // Ignore transient errors (e.g. during recompiles); we'll retry next frame.
            }
        }

        private static bool IsSceneReady()
        {
            GameObject floor = GameObject.Find("Floor");
            if (floor == null)
            {
                return false;
            }

            Collider2D collider2D = floor.GetComponent<Collider2D>();
            if (collider2D == null)
            {
                return false;
            }

            if (!collider2D.enabled || collider2D.isTrigger)
            {
                return false;
            }

            return true;
        }

        private void EnsureHUD()
        {
            if (GetComponent<NegativeMassHUD>() == null)
            {
                Debug.LogError("[负质量关卡] 缺少 NegativeMassHUD，请在场景中手动挂载（已停止运行时自动补齐）。");
                if (Application.isPlaying)
                {
                    enabled = false;
                }
            }
        }

        private void EnsureDoorSwitchGoal()
        {
            GameObject doorGo = GameObject.Find(doorName);
            if (doorGo != null && doorGo.GetComponent<DoorController2D>() == null)
            {
                Debug.LogError("[负质量关卡] Door 缺少 DoorController2D，请在场景中手动挂载（已停止运行时自动补齐）。");
                if (Application.isPlaying)
                {
                    enabled = false;
                }
            }

            GameObject switchGo = GameObject.Find(switchName);
            if (switchGo != null && switchGo.GetComponent<DoorSwitch2D>() == null)
            {
                Debug.LogError("[负质量关卡] DoorSwitch 缺少 DoorSwitch2D，请在场景中手动挂载（已停止运行时自动补齐）。");
                if (Application.isPlaying)
                {
                    enabled = false;
                }
            }

            GameObject goalGo = GameObject.Find(goalName);
            if (goalGo != null && goalGo.GetComponent<NegativeMassGoal>() == null)
            {
                Debug.LogError("[负质量关卡] Goal 缺少 NegativeMassGoal，请在场景中手动挂载（已停止运行时自动补齐）。");
                if (Application.isPlaying)
                {
                    enabled = false;
                }
            }
        }

        private void EnsureBoxes()
        {
            GameObject redSolo = GameObject.Find(redSoloName);
            if (redSolo != null)
            {
                NegativeMassBox2D solo = redSolo.GetComponent<NegativeMassBox2D>();
                if (solo == null)
                {
                    Debug.LogError("[负质量关卡] RedBox_Solo 缺少 NegativeMassBox2D，请在场景中手动挂载（已停止运行时自动补齐）。");
                }
                else
                {
                    float ceilingDistance = Mathf.Max(soloRedCeilingCheckDistance, 2.0f);
                    solo.ConfigureAsSolo(soloRedUnblockedGravityScale, soloRedBlockedGravityScale, ceilingDistance, ~0);
                    EnsureMass(solo.Body, 1.0f);
                }
            }

            GameObject blackGo = GameObject.Find(blackElevatorName);
            Rigidbody2D blackBody = null;
            if (blackGo != null)
            {
                blackBody = blackGo.GetComponent<Rigidbody2D>();
                if (blackBody == null)
                {
                    Debug.LogError("[负质量关卡] BlackBox_Elevator 缺少 Rigidbody2D，请在场景中手动挂载（已停止运行时自动补齐）。");
                }
                else
                {
                    EnsureMass(blackBody, blackBoxMass);
                    blackBody.gravityScale = blackBoxGravityScale;
                }
            }

            GameObject redElevator = GameObject.Find(redElevatorName);
            if (redElevator != null)
            {
                NegativeMassBox2D elevator = redElevator.GetComponent<NegativeMassBox2D>();
                if (elevator == null)
                {
                    Debug.LogError("[负质量关卡] RedBox_Elevator 缺少 NegativeMassBox2D，请在场景中手动挂载（已停止运行时自动补齐）。");
                }
                else
                {
                    elevator.ConfigureAsElevator(elevatorRedGravityScale, elevatorBoostMultiplier, elevatorMaxExtraLiftForce);
                    EnsureMass(elevator.Body, 1.0f);
                }

                if (blackBody != null)
                {
                    DistanceJoint2D joint = redElevator.GetComponent<DistanceJoint2D>();
                    if (joint == null)
                    {
                        Debug.LogError("[负质量关卡] RedBox_Elevator 缺少 DistanceJoint2D，请在场景中手动挂载并配置连接（已停止运行时自动补齐）。");
                        return;
                    }

                    joint.connectedBody = blackBody;
                    joint.enableCollision = false;
                    joint.autoConfigureDistance = false;
                    joint.distance = 1.2f;
                    joint.maxDistanceOnly = true;
                }
            }
        }

        private void EnsureEnvironmentColliders()
        {
            GameObject levelRoot = GameObject.Find(levelRootName);
            if (levelRoot == null)
            {
                return;
            }

            Transform env = levelRoot.transform.Find(environmentName);
            if (env == null)
            {
                return;
            }

            for (int i = 0; i < env.childCount; i++)
            {
                Transform t = env.GetChild(i);
                if (t == null || !t.gameObject.activeSelf)
                {
                    continue;
                }

                Collider2D c = t.GetComponent<Collider2D>();
                if (c == null)
                {
                    Debug.LogError($"[负质量关卡] 环境物体缺少 Collider2D：{t.name}（已停止运行时自动补齐）。");
                    continue;
                }

                if (!c.enabled || c.isTrigger)
                {
                    Debug.LogWarning($"[负质量关卡] 环境物体 Collider2D 可能配置不正确（enabled={c.enabled} isTrigger={c.isTrigger}）：{t.name}");
                }
            }
        }

        private static void EnsureMass(Rigidbody2D rb, float mass)
        {
            if (rb == null)
            {
                return;
            }

            rb.mass = Mathf.Max(0.01f, mass);
        }

        private static void EnsurePlayerBody()
        {
            PlayerController2D player = FindFirstObjectByType<PlayerController2D>();
            if (player == null)
            {
                return;
            }

            Transform existing = player.transform.Find("Body");
            if (existing == null)
            {
                Debug.LogWarning("[负质量关卡] Player 缺少子物体 'Body'（已停止运行时自动创建）。");
            }
        }
    }
}
