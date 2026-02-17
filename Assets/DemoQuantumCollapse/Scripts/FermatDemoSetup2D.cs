using UnityEngine;
using System;

namespace StrangePlaces.DemoQuantumCollapse
{
    public sealed class FermatDemoSetup2D : MonoBehaviour
    {
        [Header("Names")]
        [SerializeField] private string nodeStartName = "Node_Start";
        [SerializeField] private string nodeTop1Name = "Node_Top1";
        [SerializeField] private string nodeTop2Name = "Node_Top2";
        [SerializeField] private string nodeBottomName = "Node_Bottom";
        [SerializeField] private string nodeGoalName = "Node_Goal";
        [SerializeField] private string mediumName = "SpeedMedium";
        [SerializeField] private string probeName = "Probe";

        [Header("Speeds")]
        [SerializeField] private float fastEdgeMultiplier = 1.75f;

        [Header("Debug")]
        [SerializeField] private bool debugLogs = false;

        private int _ensureAttempts;
        private FermatSpeedMedium2D _lastMedium;
        private bool _didRefreshObserverCone;

        private void Awake()
        {
            if (debugLogs)
            {
                Debug.Log("[费尔马] DemoSetup Awake：开始确保场景对象与配置图");
            }
            EnsureSceneObjects();
            TryConfigureGraphAndProbe();
            TryRefreshObserverCone();
        }

        private void Update()
        {
            // When scripts recompile during play mode, Awake may not re-run on existing objects depending on editor settings.
            // Keep the demo resilient by retrying a few times until required components exist.
            if (IsSceneReady())
            {
                return;
            }

            _ensureAttempts++;
            if (_ensureAttempts > 60)
            {
                return;
            }

            TryEnsureSceneObjects();
            TryConfigureGraphAndProbe();
            TryRefreshObserverCone();
        }

        private void TryConfigureGraphAndProbe()
        {
            FermatNode2D start = FindNode(nodeStartName);
            FermatNode2D top1 = FindNode(nodeTop1Name);
            FermatNode2D top2 = FindNode(nodeTop2Name);
            FermatNode2D bottom = FindNode(nodeBottomName);
            FermatNode2D goal = FindNode(nodeGoalName);
            FermatSpeedMedium2D medium = FindByName<FermatSpeedMedium2D>(mediumName);
            FermatProbe2D probe = FindByName<FermatProbe2D>(probeName);

            if (start == null || top1 == null || top2 == null || bottom == null || goal == null || probe == null)
            {
                if (debugLogs)
                {
                    Debug.Log($"[费尔马] 图配置未就绪：start={(start!=null)} top1={(top1!=null)} top2={(top2!=null)} bottom={(bottom!=null)} goal={(goal!=null)} probe={(probe!=null)} medium={(medium!=null)}");
                }
                return;
            }

            bool mediumBecameAvailable = _lastMedium == null && medium != null;
            bool mediumChanged = _lastMedium != medium;
            _lastMedium = medium;

            start.SetEdges(new[]
            {
                new FermatNode2D.Edge { target = top1, speedMultiplier = fastEdgeMultiplier },
                new FermatNode2D.Edge { target = bottom, medium = medium, speedMultiplier = 1f },
            });

            top1.SetEdges(new[]
            {
                new FermatNode2D.Edge { target = top2, speedMultiplier = fastEdgeMultiplier },
            });

            top2.SetEdges(new[]
            {
                new FermatNode2D.Edge { target = goal, speedMultiplier = fastEdgeMultiplier },
            });

            bottom.SetEdges(new[]
            {
                new FermatNode2D.Edge { target = goal, medium = medium, speedMultiplier = 1f },
            });

            goal.SetEdges(System.Array.Empty<FermatNode2D.Edge>());

            // Ensure the probe is configured once. Avoid yanking it mid-run unless it's still at the start.
            if (probe.StartNode != start || probe.GoalNode != goal)
            {
                bool safeToConfigure =
                    probe.StartNode == null ||
                    probe.GoalNode == null ||
                    (Vector2)probe.transform.position == start.Position;

                if (safeToConfigure)
                {
                    if (debugLogs)
                    {
                        Debug.Log($"[费尔马] 配置探针：start={start.name} goal={goal.name} medium={(medium!=null ? "有" : "无")}");
                    }
                    probe.Configure(start, goal);
                }
            }
            else if (mediumBecameAvailable || mediumChanged)
            {
                // If medium linkage changed, replan so the route can switch immediately.
                if (debugLogs)
                {
                    Debug.Log($"[费尔马] 介质连接变化：medium={(medium!=null ? medium.name : "null")}，请求重规划");
                }
                probe.RequestReplan();
            }
        }

        private void TryRefreshObserverCone()
        {
            if (_didRefreshObserverCone && IsSceneReady())
            {
                return;
            }

            ObserverCone2D observer = FindFirstObjectByType<ObserverCone2D>();
            if (observer == null)
            {
                if (debugLogs)
                {
                    Debug.LogWarning("[费尔马] 未找到观察锥（ObserverCone2D）");
                }
                return;
            }

            observer.RefreshObservables();
            _didRefreshObserverCone = true;

            if (debugLogs)
            {
                GameObject mediumGo = GameObject.Find(mediumName);
                FermatSpeedMedium2D medium = mediumGo != null ? mediumGo.GetComponent<FermatSpeedMedium2D>() : null;
                Debug.LogWarning($"[费尔马] 已刷新观察锥目标列表：mediumGo={(mediumGo != null ? "有" : "无")} medium脚本={(medium != null ? "有" : "无")} active={(mediumGo != null && mediumGo.activeInHierarchy ? "是" : "否")}");
            }
        }

        private static bool IsSceneReady()
        {
            GameObject ground = GameObject.Find("Ground");
            GameObject probe = GameObject.Find("Probe");
            GameObject receiver = GameObject.Find("Receiver");
            GameObject medium = GameObject.Find("SpeedMedium");
            GameObject door = GameObject.Find("Door");

            if (ground == null || probe == null || receiver == null || medium == null || door == null)
            {
                return false;
            }

            if (ground.GetComponent<BoxCollider2D>() == null)
            {
                return false;
            }

            if (probe.GetComponent<FermatProbe2D>() == null || probe.GetComponent<BoxCollider2D>() == null)
            {
                return false;
            }

            if (receiver.GetComponent<FermatReceiver2D>() == null || receiver.GetComponent<BoxCollider2D>() == null)
            {
                return false;
            }

            if (medium.GetComponent<FermatSpeedMedium2D>() == null || medium.GetComponent<BoxCollider2D>() == null)
            {
                return false;
            }

            if (door.GetComponent<BoxCollider2D>() == null)
            {
                return false;
            }

            return true;
        }

        private void EnsureSceneObjects()
        {
            // This demo scene is intentionally low-cost; ensure critical colliders/transforms exist even if the scene was created from Quads.
            EnsureGround();
            EnsureDoor();
            EnsureMedium();
            EnsureReceiver();
            EnsureProbe();
            EnsureNodes();
            EnsurePlayerBody();
        }

        private void EnsureGround()
        {
            GameObject ground = GameObject.Find("Ground");
            if (ground == null)
            {
                return;
            }

            ground.transform.position = new Vector3(0f, -2.2f, 0f);
            ground.transform.localScale = new Vector3(20f, 1f, 1f);

            Remove3DCollider(ground);
            BoxCollider2D box = EnsureComponent<BoxCollider2D>(ground);
            if (box == null)
            {
                return;
            }
            box.isTrigger = false;
            box.size = Vector2.one;

            DemoColorRenderer color = EnsureComponent<DemoColorRenderer>(ground);
            if (color != null)
            {
                color.SetColor(new Color(0.22f, 0.22f, 0.25f, 1f));
            }
        }

        private void EnsureDoor()
        {
            GameObject door = GameObject.Find("Door");
            if (door == null)
            {
                return;
            }

            door.transform.position = new Vector3(8.2f, -0.8f, 0f);
            door.transform.localScale = new Vector3(1f, 3f, 1f);

            Remove3DCollider(door);
            BoxCollider2D box = EnsureComponent<BoxCollider2D>(door);
            if (box == null)
            {
                return;
            }
            box.isTrigger = false;
            box.size = Vector2.one;

            DemoColorRenderer color = EnsureComponent<DemoColorRenderer>(door);
            if (color != null)
            {
                color.SetColor(new Color(1f, 0.25f, 0.25f, 1f));
            }
        }

        private void EnsureMedium()
        {
            GameObject mediumGo = GameObject.Find("SpeedMedium");
            if (mediumGo == null)
            {
                return;
            }

            mediumGo.transform.position = new Vector3(0f, -0.6f, 0f);
            mediumGo.transform.localScale = new Vector3(6f, 2f, 1f);

            Remove3DCollider(mediumGo);
            BoxCollider2D box = EnsureComponent<BoxCollider2D>(mediumGo);
            if (box == null)
            {
                return;
            }
            box.isTrigger = true;
            box.size = Vector2.one;

            EnsureComponent<FermatSpeedMedium2D>(mediumGo);
        }

        private void EnsureReceiver()
        {
            GameObject receiver = GameObject.Find("Receiver");
            if (receiver == null)
            {
                return;
            }

            receiver.transform.position = new Vector3(7f, -0.8f, 0f);
            receiver.transform.localScale = new Vector3(0.7f, 0.7f, 1f);

            Remove3DCollider(receiver);
            BoxCollider2D box = EnsureComponent<BoxCollider2D>(receiver);
            if (box == null)
            {
                return;
            }
            box.isTrigger = true;
            box.size = Vector2.one;

            FermatReceiver2D r = EnsureComponent<FermatReceiver2D>(receiver);
            // Door auto-binds by name; keep default.

            DemoColorRenderer color = EnsureComponent<DemoColorRenderer>(receiver);
            if (color != null)
            {
                color.SetColor(new Color(1f, 1f, 1f, 0.25f));
            }
        }

        private void EnsureProbe()
        {
            GameObject probe = GameObject.Find("Probe");
            if (probe == null)
            {
                return;
            }

            probe.transform.position = new Vector3(-6f, -0.8f, 0f);
            probe.transform.localScale = new Vector3(0.35f, 0.35f, 1f);

            Remove3DCollider(probe);
            BoxCollider2D box = EnsureComponent<BoxCollider2D>(probe);
            if (box == null)
            {
                return;
            }
            box.isTrigger = true;
            box.size = Vector2.one;

            EnsureComponent<FermatProbe2D>(probe);

            DemoColorRenderer color = EnsureComponent<DemoColorRenderer>(probe);
            if (color != null)
            {
                color.SetColor(new Color(1f, 0.9f, 0.35f, 1f));
            }
        }

        private void EnsureNodes()
        {
            EnsureNode("Node_Start", new Vector3(-6f, -0.8f, 0f));
            EnsureNode("Node_Top1", new Vector3(-2f, 1.8f, 0f));
            EnsureNode("Node_Top2", new Vector3(2f, 1.8f, 0f));
            EnsureNode("Node_Bottom", new Vector3(0f, -0.8f, 0f));
            EnsureNode("Node_Goal", new Vector3(6f, -0.8f, 0f));
        }

        private void EnsureNode(string name, Vector3 position)
        {
            GameObject node = GameObject.Find(name);
            if (node == null)
            {
                node = new GameObject(name);
            }

            node.transform.position = position;
            EnsureComponent<FermatNode2D>(node);
        }

        private void EnsurePlayerBody()
        {
            PlayerController2D player = FindFirstObjectByType<PlayerController2D>();
            if (player == null)
            {
                return;
            }

            GameObject body = player.transform.Find("Body").gameObject;
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

        private void TryEnsureSceneObjects()
        {
            try
            {
                EnsureSceneObjects();
            }
            catch
            {
                // Ignore transient errors (e.g. during recompiles); we'll retry next frame.
            }
        }

        private static void Remove3DCollider(GameObject go)
        {
            if (go == null)
            {
                return;
            }

            Collider meshCollider = go.GetComponent<Collider>();
            if (meshCollider != null)
            {
                Destroy(meshCollider);
            }

            MeshCollider mc = go.GetComponent<MeshCollider>();
            if (mc != null)
            {
                Destroy(mc);
            }
        }

        private static T EnsureComponent<T>(GameObject go) where T : Component
        {
            T c = go.GetComponent<T>();
            if (c != null)
            {
                return c;
            }

            return go.AddComponent<T>();
        }

        private FermatNode2D FindNode(string name)
        {
            return FindByName<FermatNode2D>(name);
        }

        private static T FindByName<T>(string name) where T : Component
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            GameObject go = GameObject.Find(name.Trim());
            if (go == null)
            {
                return null;
            }

            return go.GetComponent<T>();
        }
    }
}
