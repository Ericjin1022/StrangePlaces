using System;
using UnityEngine;
using System.Collections.Generic;

namespace StrangePlaces.DemoQuantumCollapse
{
    public sealed class ObserverCone2D : MonoBehaviour
    {
        [Header("Cone")]
        [SerializeField] private float maxDistance = 7f;
        [SerializeField] private float coneAngleDegrees = 60f;
        [SerializeField] private bool alwaysOn = true;
        [SerializeField] private KeyCode holdKey = KeyCode.Mouse1;
        [SerializeField] private float observationGraceSeconds = 0.08f;

        [Header("Debug")]
        [SerializeField] private bool debugLogging = false;
        [SerializeField] private float debugIntervalSeconds = 0.5f;
        [SerializeField] private string debugTargetName = "SpeedMedium";

        [Header("Debug Visual")]
        [SerializeField] private bool showDebugLines = false;

        [Header("Line of Sight")]
        [SerializeField] private bool requireLineOfSight = true;
        [SerializeField] private LayerMask raycastMask = ~0;

        [Header("Visual")]
        [SerializeField] private Color coneColor = new(1f, 1f, 0.3f, 1f);

        private PlayerController2D _player;
        private Collider2D _selfCollider;
        private LineRenderer _leftLine;
        private LineRenderer _rightLine;
        private LineRenderer _centerLine;

        private IObservationTarget[] _targets = System.Array.Empty<IObservationTarget>();
        private readonly Dictionary<IObservationTarget, float> _lastObservedTime = new();
        private readonly Dictionary<IObservationTarget, bool> _lastObservedState = new();
        private readonly Dictionary<IObservationTarget, string> _entanglementKeyByTarget = new();
        private readonly HashSet<IObservationTarget> _entanglementBeacons = new();

        private int _lastTargetsHash;
        private float _nextDebugTime;
        private float _lastRefreshTime;
        private const float AutoRefreshIntervalSeconds = 1.0f;

        private void Awake()
        {
            _player = GetComponent<PlayerController2D>();
            _selfCollider = GetComponent<Collider2D>();
            if (showDebugLines)
            {
                CreateLineRenderers();
            }
        }

        private void Start()
        {
            RefreshObservables();

            if (debugLogging)
            {
                Debug.LogWarning($"[观察] 启动：alwaysOn={(alwaysOn ? "是" : "否")} 按键={holdKey} 距离={maxDistance:0.00} 角度={coneAngleDegrees:0.0}（已开启调试日志）");
            }
        }

        private void Update()
        {
            if ((_targets == null || _targets.Length == 0) && (Time.time - _lastRefreshTime) >= AutoRefreshIntervalSeconds)
            {
                RefreshObservables();
            }

            bool active = alwaysOn || Input.GetKey(holdKey);
            Vector2 aim = _player != null ? _player.AimDirection : (Vector2)transform.right;

            if (debugLogging && Time.time >= _nextDebugTime)
            {
                _nextDebugTime = Time.time + Mathf.Max(0.05f, debugIntervalSeconds);
                DebugLogSnapshot(active, aim);
            }

            if (!active)
            {
                SetAllObserved(false);
                SetAllEntanglementObserved(false);
                SetConeVisible(false);
                return;
            }

            SetConeVisible(true);
            UpdateConeLines(aim);

            Vector2 origin = transform.position;
            float halfAngle = coneAngleDegrees * 0.5f;

            bool[] observedIncludingGrace = new bool[_targets.Length];
            for (int i = 0; i < _targets.Length; i++)
            {
                IObservationTarget target = _targets[i];
                if (target == null)
                {
                    continue;
                }

                bool directlyObserved = IsObserved(origin, aim, halfAngle, target);
                bool observedNow = directlyObserved;
                if (!observedNow &&
                    observationGraceSeconds > 0f &&
                    _lastObservedTime.TryGetValue(target, out float lastTime) &&
                    (Time.time - lastTime) <= observationGraceSeconds)
                {
                    observedNow = true;
                }

                observedIncludingGrace[i] = observedNow;

                // IMPORTANT: Only refresh the timestamp on direct observation.
                // If we refresh while in grace, the grace window never expires and targets will "stick" observed forever.
                if (directlyObserved)
                {
                    _lastObservedTime[target] = Time.time;
                }

                if (debugLogging)
                {
                    bool had = _lastObservedState.TryGetValue(target, out bool last);
                    if (!had || last != observedNow)
                    {
                        _lastObservedState[target] = observedNow;
                        MonoBehaviour mb = target as MonoBehaviour;
                        string name = mb != null ? mb.name : target.GetType().Name;
                        Debug.Log($"[观察] 目标={name} 观察={(observedNow ? "是" : "否")} 直接={(directlyObserved ? "是" : "否")}");
                    }
                }

                target.SetObserved(observedNow);
            }

            HashSet<string> entanglementKeysObservedByBeacons = null;
            for (int i = 0; i < _targets.Length; i++)
            {
                if (!observedIncludingGrace[i])
                {
                    continue;
                }

                IObservationTarget target = _targets[i];
                if (target == null || !_entanglementBeacons.Contains(target))
                {
                    continue;
                }

                if (_entanglementKeyByTarget.TryGetValue(target, out string key) && !string.IsNullOrWhiteSpace(key))
                {
                    entanglementKeysObservedByBeacons ??= new HashSet<string>();
                    entanglementKeysObservedByBeacons.Add(key);
                }
            }

            for (int i = 0; i < _targets.Length; i++)
            {
                IObservationTarget target = _targets[i];
                if (target == null)
                {
                    continue;
                }

                if (target is not IEntanglementReceiver receiver)
                {
                    continue;
                }

                if (!_entanglementKeyByTarget.TryGetValue(target, out string key) || string.IsNullOrWhiteSpace(key))
                {
                    receiver.SetEntanglementObserved(false);
                    continue;
                }

                bool entangledObserved = entanglementKeysObservedByBeacons != null && entanglementKeysObservedByBeacons.Contains(key);
                receiver.SetEntanglementObserved(entangledObserved);
            }
        }

        public void RefreshObservables()
        {
            _lastRefreshTime = Time.time;

            MonoBehaviour[] behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (behaviours == null || behaviours.Length == 0)
            {
                // Fallback for some Unity/editor configs where FindObjectsByType<MonoBehaviour> may return nothing.
                // This older API is slower but reliable for scene objects.
                behaviours = FindObjectsOfType<MonoBehaviour>(includeInactive: true);
            }

            List<IObservationTarget> targets = new(behaviours.Length);
            _entanglementKeyByTarget.Clear();
            _entanglementBeacons.Clear();
            _lastObservedState.Clear();
            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour b = behaviours[i];
                if (b == null)
                {
                    continue;
                }

                // Ignore assets/prefabs not in a loaded scene (paranoia for some editor APIs).
                if (!b.gameObject.scene.IsValid() || !b.gameObject.scene.isLoaded)
                {
                    continue;
                }

                if (b is IObservationTarget t)
                {
                    targets.Add(t);

                    QuantumEntanglementMember entanglement = b.GetComponent<QuantumEntanglementMember>();
                    if (entanglement != null && !string.IsNullOrWhiteSpace(entanglement.EntanglementKey))
                    {
                        _entanglementKeyByTarget[t] = entanglement.EntanglementKey.Trim();
                    }

                    if (b.GetComponent<QuantumEntanglementBeacon>() != null)
                    {
                        _entanglementBeacons.Add(t);
                    }
                }
            }

            _targets = targets.ToArray();

            if (debugLogging)
            {
                GameObject mediumGo = !string.IsNullOrWhiteSpace(debugTargetName) ? GameObject.Find(debugTargetName) : null;
                FermatSpeedMedium2D medium = mediumGo != null ? mediumGo.GetComponent<FermatSpeedMedium2D>() : null;
                bool mediumIsTarget = medium != null && medium is IObservationTarget;

                int hash = 17;
                for (int i = 0; i < _targets.Length; i++)
                {
                    hash = hash * 31 + (_targets[i] != null ? _targets[i].GetHashCode() : 0);
                }

                if (_lastTargetsHash != hash)
                {
                    _lastTargetsHash = hash;
                    Debug.LogWarning($"[观察] 刷新目标：behaviours={behaviours.Length} targets={_targets.Length} 距离={maxDistance:0.00} 角度={coneAngleDegrees:0.0} mediumIsTarget={(mediumIsTarget ? "是" : "否")}");
                    for (int i = 0; i < _targets.Length; i++)
                    {
                        MonoBehaviour mb = _targets[i] as MonoBehaviour;
                        if (mb != null)
                        {
                            Debug.Log($"[观察] 目标[{i}]={mb.name} 类型={mb.GetType().Name} 激活={(mb.isActiveAndEnabled ? "是" : "否")}");
                        }
                    }
                }
            }
        }

        private void DebugLogSnapshot(bool active, Vector2 aim)
        {
            Vector2 origin = transform.position;
            if (string.IsNullOrWhiteSpace(debugTargetName) || _targets == null)
            {
                return;
            }

            IObservationTarget named = null;
            string wanted = debugTargetName.Trim();
            for (int i = 0; i < _targets.Length; i++)
            {
                MonoBehaviour mb = _targets[i] as MonoBehaviour;
                if (mb != null && string.Equals(mb.name, wanted, StringComparison.Ordinal))
                {
                    named = _targets[i];
                    break;
                }
            }

            if (named == null)
            {
                Debug.LogWarning($"[观察] 调试目标未找到：debugTargetName='{wanted}'。请把它改成某个可观察目标的物体名，或清空此字段关闭该目标诊断。");
                return;
            }

            Vector2 p = named.ObservationPoint;
            Vector2 to = p - origin;
            float dist = to.magnitude;
            float angle = dist > 0.001f ? Vector2.Angle(aim, to / dist) : 0f;
            float halfAngle = coneAngleDegrees * 0.5f;

            bool inDistance = dist <= maxDistance;
            bool inAngle = angle <= halfAngle;
            bool observed = IsObserved(origin, aim, halfAngle, named);

            if (!observed && inDistance && inAngle && requireLineOfSight && dist > 0.001f)
            {
                Vector2 dir = to / dist;
                Vector2 rayOrigin = origin + dir * 0.15f;
                RaycastHit2D[] hits = Physics2D.RaycastAll(rayOrigin, dir, dist, raycastMask);
                float nearest = float.PositiveInfinity;
                Collider2D nearestCollider = null;
                for (int i = 0; i < hits.Length; i++)
                {
                    Collider2D collider = hits[i].collider;
                    if (collider == null)
                    {
                        continue;
                    }

                    if (_selfCollider != null && collider == _selfCollider)
                    {
                        continue;
                    }

                    if (hits[i].distance < nearest)
                    {
                        nearest = hits[i].distance;
                        nearestCollider = collider;
                    }
                }

                if (nearestCollider == null)
                {
                    Debug.Log("[观察] 目标诊断：射线未命中任何 2D 碰撞体（视线判定会当作无遮挡）");
                }
                else
                {
                    Debug.Log($"[观察] 目标诊断：射线最近命中={nearestCollider.name} layer={LayerMask.LayerToName(nearestCollider.gameObject.layer)} 距离={nearest:0.00} 归属目标={(named.OwnsCollider(nearestCollider) ? "是" : "否")}");
                }
            }
        }

        private bool IsObserved(Vector2 origin, Vector2 aimDir, float halfAngle, IObservationTarget target)
        {
            Collider2D targetCollider = target.PrimaryCollider;
            if (targetCollider == null)
            {
                return IsObservedAtPoint(origin, aimDir, halfAngle, target, target.ObservationPoint);
            }

            Bounds b = targetCollider.bounds;
            Vector2 c = b.center;
            Vector2 min = b.min;
            Vector2 max = b.max;

            // Approximate “any part enters the cone” by checking several points on the collider bounds.
            // This is intentionally generous for long platforms.
            Vector2[] points =
            {
                c,
                new Vector2(min.x, c.y),
                new Vector2(max.x, c.y),
                new Vector2(c.x, min.y),
                new Vector2(c.x, max.y),
                new Vector2(min.x, min.y),
                new Vector2(min.x, max.y),
                new Vector2(max.x, min.y),
                new Vector2(max.x, max.y),
            };

            for (int i = 0; i < points.Length; i++)
            {
                if (IsObservedAtPoint(origin, aimDir, halfAngle, target, points[i]))
                {
                    return true;
                }
            }

            // Fallback: closest point to the observer (handles rotated/odd shapes a bit better).
            Vector2 closest = targetCollider.ClosestPoint(origin);
            if ((closest - origin).sqrMagnitude > 0.0001f)
            {
                return IsObservedAtPoint(origin, aimDir, halfAngle, target, closest);
            }

            return false;
        }

        private bool IsObservedAtPoint(Vector2 origin, Vector2 aimDir, float halfAngle, IObservationTarget target, Vector2 point)
        {
            Vector2 toPoint = point - origin;
            float distance = toPoint.magnitude;
            if (distance > maxDistance || distance < 0.001f)
            {
                return false;
            }

            Vector2 dir = toPoint / distance;
            float angle = Vector2.Angle(aimDir, dir);
            if (angle > halfAngle)
            {
                return false;
            }

            if (!requireLineOfSight)
            {
                return true;
            }

            Vector2 rayOrigin = origin + dir * 0.15f;
            RaycastHit2D[] hits = Physics2D.RaycastAll(rayOrigin, dir, distance, raycastMask);
            float nearest = float.PositiveInfinity;
            Collider2D nearestCollider = null;
            for (int i = 0; i < hits.Length; i++)
            {
                Collider2D collider = hits[i].collider;
                if (collider == null)
                {
                    continue;
                }

                if (_selfCollider != null && collider == _selfCollider)
                {
                    continue;
                }

                if (hits[i].distance < nearest)
                {
                    nearest = hits[i].distance;
                    nearestCollider = collider;
                }
            }

            if (nearestCollider == null || float.IsPositiveInfinity(nearest))
            {
                return true;
            }

            return target.OwnsCollider(nearestCollider);
        }

        private void SetAllObserved(bool observed)
        {
            for (int i = 0; i < _targets.Length; i++)
            {
                if (_targets[i] != null)
                {
                    _targets[i].SetObserved(observed);
                }
            }
        }

        private void SetAllEntanglementObserved(bool observed)
        {
            for (int i = 0; i < _targets.Length; i++)
            {
                if (_targets[i] is IEntanglementReceiver receiver)
                {
                    receiver.SetEntanglementObserved(observed);
                }
            }
        }

        private void CreateLineRenderers()
        {
            _leftLine = CreateLineRenderer("ConeLeft");
            _rightLine = CreateLineRenderer("ConeRight");
            _centerLine = CreateLineRenderer("ConeCenter");
        }

        private LineRenderer CreateLineRenderer(string name)
        {
            GameObject go = new(name);
            go.transform.SetParent(transform, false);
            LineRenderer lr = go.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.startWidth = 0.03f;
            lr.endWidth = 0.01f;
            lr.numCapVertices = 2;
            lr.useWorldSpace = true;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = coneColor;
            lr.endColor = coneColor;
            return lr;
        }

        private void SetConeVisible(bool visible)
        {
            if (!showDebugLines)
            {
                return;
            }

            if (_leftLine == null || _rightLine == null || _centerLine == null)
            {
                return;
            }

            _leftLine.enabled = visible;
            _rightLine.enabled = visible;
            _centerLine.enabled = visible;
        }

        private void UpdateConeLines(Vector2 aimDir)
        {
            if (!showDebugLines)
            {
                return;
            }

            if (_leftLine == null || _rightLine == null || _centerLine == null)
            {
                return;
            }

            Vector3 origin = transform.position;
            float radians = coneAngleDegrees * 0.5f * Mathf.Deg2Rad;

            Vector2 leftDir = Rotate(aimDir, -radians);
            Vector2 rightDir = Rotate(aimDir, radians);

            _leftLine.SetPosition(0, origin);
            _leftLine.SetPosition(1, origin + (Vector3)(leftDir * maxDistance));

            _rightLine.SetPosition(0, origin);
            _rightLine.SetPosition(1, origin + (Vector3)(rightDir * maxDistance));

            _centerLine.SetPosition(0, origin);
            _centerLine.SetPosition(1, origin + (Vector3)(aimDir * maxDistance));
        }

        private static Vector2 Rotate(Vector2 v, float radians)
        {
            float cos = Mathf.Cos(radians);
            float sin = Mathf.Sin(radians);
            return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
        }
    }
}
