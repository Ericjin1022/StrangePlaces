using System.Collections.Generic;
using UnityEngine;

namespace StrangePlaces.DemoQuantumCollapse
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class FermatProbe2D : MonoBehaviour
    {
        [Header("Graph")]
        [SerializeField] private FermatNode2D startNode;
        [SerializeField] private FermatNode2D goalNode;

        [Header("Movement")]
        [SerializeField] private float baseSpeed = 4.5f;
        [SerializeField] private float arriveDistance = 0.08f;
        [SerializeField] private bool autoStart = true;

        [Header("Visual")]
        [SerializeField] private bool drawPlannedPath = true;
        [SerializeField] private Color pathColor = new(1f, 1f, 1f, 0.25f);

        public bool ReachedGoal { get; private set; }
        public FermatNode2D StartNode => startNode;
        public FermatNode2D GoalNode => goalNode;

        private readonly List<FermatNode2D> _path = new();
        private int _pathIndex;
        private FermatNode2D _currentNode;
        private FermatNode2D _nextNode;

        private float _lastPlanTime;
        private const float MinReplanInterval = 0.05f;

        private void Awake()
        {
            Collider2D c = GetComponent<Collider2D>();
            c.isTrigger = true;
        }

        private void Start()
        {
            if (autoStart)
            {
                RestartFromStart();
            }
        }

        private void Update()
        {
            if (ReachedGoal)
            {
                return;
            }

            if (_nextNode == null)
            {
                return;
            }

            Vector2 from = transform.position;
            Vector2 to = _nextNode.Position;
            Vector2 delta = to - from;
            float dist = delta.magnitude;
            if (dist <= arriveDistance)
            {
                transform.position = to;
                AdvanceToNextNode();
                return;
            }

            Vector2 dir = delta / Mathf.Max(0.0001f, dist);
            float speedMultiplier = GetEdgeSpeedMultiplier(_currentNode, _nextNode);
            float speed = baseSpeed * speedMultiplier;
            transform.position = from + dir * (speed * Time.deltaTime);
        }

        public void RestartFromStart()
        {
            ReachedGoal = false;
            _currentNode = startNode;
            transform.position = startNode != null ? (Vector3)startNode.Position : transform.position;
            PlanPath(fromNode: _currentNode);
        }

        public void Configure(FermatNode2D newStart, FermatNode2D newGoal)
        {
            startNode = newStart;
            goalNode = newGoal;
            RestartFromStart();
        }

        public void RequestReplan()
        {
            if (Time.time - _lastPlanTime < MinReplanInterval)
            {
                return;
            }

            PlanPath(fromNode: _currentNode);
        }

        private void PlanPath(FermatNode2D fromNode)
        {
            _lastPlanTime = Time.time;

            _path.Clear();
            _pathIndex = 0;
            _nextNode = null;

            if (fromNode == null || goalNode == null)
            {
                return;
            }

            if (fromNode == goalNode)
            {
                ReachedGoal = true;
                return;
            }

            if (!TryComputeShortestTimePath(fromNode, goalNode, _path))
            {
                return;
            }

            _pathIndex = 0;
            _currentNode = _path[0];
            _nextNode = _path.Count > 1 ? _path[1] : null;

            if (drawPlannedPath)
            {
                DebugDrawPath(_path);
            }
        }

        private void AdvanceToNextNode()
        {
            if (_path.Count == 0)
            {
                return;
            }

            _pathIndex++;
            if (_pathIndex >= _path.Count - 1)
            {
                if (_nextNode == goalNode)
                {
                    ReachedGoal = true;
                }
                _currentNode = _nextNode;
                _nextNode = null;
                return;
            }

            _currentNode = _path[_pathIndex];
            _nextNode = _path[_pathIndex + 1];
        }

        private float GetEdgeSpeedMultiplier(FermatNode2D from, FermatNode2D to)
        {
            if (from == null || to == null)
            {
                return 1f;
            }

            var edges = from.Edges;
            for (int i = 0; i < edges.Length; i++)
            {
                FermatNode2D.Edge e = edges[i];
                if (e != null && e.target == to)
                {
                    return from.GetEdgeSpeedMultiplier(e);
                }
            }

            return 1f;
        }

        private static bool TryComputeShortestTimePath(FermatNode2D start, FermatNode2D goal, List<FermatNode2D> outPath)
        {
            // Dijkstra: cost = distance / speedMultiplier (baseSpeed cancels out)
            var dist = new Dictionary<FermatNode2D, float>();
            var prev = new Dictionary<FermatNode2D, FermatNode2D>();
            var open = new List<FermatNode2D>();

            dist[start] = 0f;
            open.Add(start);

            while (open.Count > 0)
            {
                int bestIndex = 0;
                float bestCost = dist[open[0]];
                for (int i = 1; i < open.Count; i++)
                {
                    float c = dist[open[i]];
                    if (c < bestCost)
                    {
                        bestCost = c;
                        bestIndex = i;
                    }
                }

                FermatNode2D current = open[bestIndex];
                open.RemoveAt(bestIndex);

                if (current == goal)
                {
                    BuildPath(goal, prev, outPath);
                    return true;
                }

                var edges = current.Edges;
                for (int i = 0; i < edges.Length; i++)
                {
                    FermatNode2D.Edge e = edges[i];
                    if (e == null || e.target == null)
                    {
                        continue;
                    }

                    float speedMultiplier = current.GetEdgeSpeedMultiplier(e);
                    float d = Vector2.Distance(current.Position, e.target.Position);
                    float cost = d / Mathf.Max(0.01f, speedMultiplier);

                    float newDist = dist[current] + cost;
                    if (!dist.TryGetValue(e.target, out float oldDist) || newDist < oldDist)
                    {
                        dist[e.target] = newDist;
                        prev[e.target] = current;
                        if (!open.Contains(e.target))
                        {
                            open.Add(e.target);
                        }
                    }
                }
            }

            return false;
        }

        private static void BuildPath(FermatNode2D goal, Dictionary<FermatNode2D, FermatNode2D> prev, List<FermatNode2D> outPath)
        {
            outPath.Clear();
            FermatNode2D current = goal;
            outPath.Add(current);
            while (prev.TryGetValue(current, out FermatNode2D p) && p != null)
            {
                current = p;
                outPath.Add(current);
            }
            outPath.Reverse();
        }

        private void DebugDrawPath(List<FermatNode2D> path)
        {
            if (path == null || path.Count < 2)
            {
                return;
            }

            for (int i = 0; i < path.Count - 1; i++)
            {
                Vector3 a = path[i].transform.position;
                Vector3 b = path[i + 1].transform.position;
                Debug.DrawLine(a, b, pathColor, 0.15f);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            FermatReceiver2D receiver = other.GetComponent<FermatReceiver2D>();
            if (receiver == null)
            {
                return;
            }

            receiver.OnProbeArrived(this);
        }
    }
}
