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
                EnsureLevelRoot2DOnly();
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
                gameObject.AddComponent<NegativeMassHUD>();
            }
        }

        private void EnsureDoorSwitchGoal()
        {
            GameObject doorGo = GameObject.Find(doorName);
            if (doorGo != null && doorGo.GetComponent<DoorController2D>() == null)
            {
                doorGo.AddComponent<DoorController2D>();
            }

            GameObject switchGo = GameObject.Find(switchName);
            if (switchGo != null && switchGo.GetComponent<DoorSwitch2D>() == null)
            {
                switchGo.AddComponent<DoorSwitch2D>();
            }

            GameObject goalGo = GameObject.Find(goalName);
            if (goalGo != null && goalGo.GetComponent<NegativeMassGoal>() == null)
            {
                goalGo.AddComponent<NegativeMassGoal>();
            }
        }

        private void EnsureBoxes()
        {
            GameObject redSolo = GameObject.Find(redSoloName);
            if (redSolo != null)
            {
                NegativeMassBox2D solo = EnsureComponent<NegativeMassBox2D>(redSolo);
                float ceilingDistance = Mathf.Max(soloRedCeilingCheckDistance, 2.0f);
                solo.ConfigureAsSolo(soloRedUnblockedGravityScale, soloRedBlockedGravityScale, ceilingDistance, ~0);
                EnsureMass(solo.Body, 1.0f);
            }

            GameObject blackGo = GameObject.Find(blackElevatorName);
            Rigidbody2D blackBody = null;
            if (blackGo != null)
            {
                blackBody = EnsureDynamicBody(blackGo, blackBoxMass, blackBoxGravityScale);
            }

            GameObject redElevator = GameObject.Find(redElevatorName);
            if (redElevator != null)
            {
                NegativeMassBox2D elevator = EnsureComponent<NegativeMassBox2D>(redElevator);
                elevator.ConfigureAsElevator(elevatorRedGravityScale, elevatorBoostMultiplier, elevatorMaxExtraLiftForce);
                EnsureMass(elevator.Body, 1.0f);

                if (blackBody != null)
                {
                    DistanceJoint2D joint = redElevator.GetComponent<DistanceJoint2D>();
                    if (joint == null)
                    {
                        joint = redElevator.AddComponent<DistanceJoint2D>();
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

                EnsureStaticCollider2D(t.gameObject);
            }

            EnsureFloorColliderFallback(levelRoot.transform, env);
        }

        private void EnsureLevelRoot2DOnly()
        {
            GameObject levelRoot = GameObject.Find(levelRootName);
            if (levelRoot == null)
            {
                return;
            }

            Remove3DCollidersRecursive(levelRoot.transform);
            Remove3DRigidbodiesRecursive(levelRoot.transform);
        }

        private static void Remove3DCollidersRecursive(Transform root)
        {
            if (root == null)
            {
                return;
            }

            Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider c = colliders[i];
                if (c == null)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Destroy(c);
                }
                else
                {
                    DestroyImmediate(c);
                }
            }
        }

        private static void Remove3DRigidbodiesRecursive(Transform root)
        {
            if (root == null)
            {
                return;
            }

            Rigidbody[] rigidbodies = root.GetComponentsInChildren<Rigidbody>(true);
            for (int i = 0; i < rigidbodies.Length; i++)
            {
                Rigidbody rb = rigidbodies[i];
                if (rb == null)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Destroy(rb);
                }
                else
                {
                    DestroyImmediate(rb);
                }
            }
        }

        private static void EnsureFloorColliderFallback(Transform levelRoot, Transform environment)
        {
            Transform floorT = environment != null ? environment.Find("Floor") : null;
            if (floorT == null)
            {
                floorT = levelRoot != null ? levelRoot.Find("Environment/Floor") : null;
            }

            if (floorT == null)
            {
                return;
            }

            Collider2D floorCollider = floorT.GetComponent<Collider2D>();
            if (floorCollider != null && floorCollider.enabled && !floorCollider.isTrigger)
            {
                return;
            }

            Transform fallback = environment.Find("FloorCollider");
            GameObject go;
            if (fallback != null)
            {
                go = fallback.gameObject;
            }
            else
            {
                go = new GameObject("FloorCollider");
                go.transform.SetParent(environment, false);
            }

            go.transform.position = floorT.position;
            go.transform.rotation = floorT.rotation;
            go.transform.localScale = floorT.localScale;

            BoxCollider2D c = go.GetComponent<BoxCollider2D>();
            if (c == null)
            {
                c = go.AddComponent<BoxCollider2D>();
            }

            c.size = Vector2.one;
            c.offset = Vector2.zero;
            c.isTrigger = false;
            c.enabled = true;
        }

        private static void EnsureStaticCollider2D(GameObject go)
        {
            Remove3DColliders(go);

            BoxCollider2D c = go.GetComponent<BoxCollider2D>();
            if (c == null)
            {
                c = go.AddComponent<BoxCollider2D>();
            }

            c.size = Vector2.one;
            c.offset = Vector2.zero;
            c.isTrigger = false;
            c.enabled = true;
        }

        private static Rigidbody2D EnsureDynamicBody(GameObject go, float mass, float gravityScale)
        {
            Remove3DColliders(go);

            BoxCollider2D box = go.GetComponent<BoxCollider2D>();
            if (box == null)
            {
                box = go.AddComponent<BoxCollider2D>();
            }
            box.size = Vector2.one;
            box.offset = Vector2.zero;
            box.isTrigger = false;

            Rigidbody2D rb = go.GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                rb = go.AddComponent<Rigidbody2D>();
            }

            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.mass = Mathf.Max(0.01f, mass);
            rb.gravityScale = gravityScale;
            rb.freezeRotation = true;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            return rb;
        }

        private static void EnsureMass(Rigidbody2D rb, float mass)
        {
            if (rb == null)
            {
                return;
            }

            rb.mass = Mathf.Max(0.01f, mass);
        }

        private static T EnsureComponent<T>(GameObject go) where T : Component
        {
            if (go == null)
            {
                return null;
            }

            T c = go.GetComponent<T>();
            if (c != null)
            {
                return c;
            }

            return go.AddComponent<T>();
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

        private static void EnsurePlayerBody()
        {
            PlayerController2D player = FindFirstObjectByType<PlayerController2D>();
            if (player == null)
            {
                return;
            }

            Transform existing = player.transform.Find("Body");
            if (existing != null)
            {
                // Respect manual edits in the scene.
                return;
            }

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Quad);
            body.name = "Body";
            body.transform.SetParent(player.transform, false);
            body.transform.localPosition = new Vector3(0f, 0f, 0.1f);
            body.transform.localScale = Vector3.one;

            Collider c = body.GetComponent<Collider>();
            if (c != null)
            {
                c.enabled = false;
            }

            DemoColorRenderer color = body.AddComponent<DemoColorRenderer>();
            if (color != null)
            {
                color.SetColor(new Color(0.95f, 0.95f, 0.97f, 1f));
            }
        }
    }
}
