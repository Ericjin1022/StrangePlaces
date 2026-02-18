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
        [SerializeField] private LineRenderer _leftLine;
        [SerializeField] private LineRenderer _rightLine;
        [SerializeField] private LineRenderer _centerLine;

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
                if (_leftLine == null || _rightLine == null || _centerLine == null)
                {
                    Debug.LogWarning("[ObserverCone2D] showDebugLines is enabled, but LineRenderers are not assigned.");
                }
            }
        }

        private void Start()
        {
            RefreshObservables();

            if (debugLogging)
            {
                // Debug log removed
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
                        // Debug log removed
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
                MonoBehaviour[] mediumBehaviours = mediumGo != null ? mediumGo.GetComponents<MonoBehaviour>() : null;
                bool mediumIsTarget = false;
                if (mediumBehaviours != null)
                {
                    for (int i = 0; i < mediumBehaviours.Length; i++)
                    {
                        if (mediumBehaviours[i] is IObservationTarget)
                        {
                            mediumIsTarget = true;
                            break;
                        }
                    }
                }

                int hash = 17;
                for (int i = 0; i < _targets.Length; i++)
                {
                    hash = hash * 31 + (_targets[i] != null ? _targets[i].GetHashCode() : 0);
                }

                if (_lastTargetsHash != hash)
                {
                    _lastTargetsHash = hash;
                    // Debug log removed
                    for (int i = 0; i < _targets.Length; i++)
                    {
                        MonoBehaviour mb = _targets[i] as MonoBehaviour;
                        if (mb != null)
                        {
                            // Debug log removed
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
                // Debug log removed
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
                    // Debug log removed
                }
                else
                {
                    // Debug log removed
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

            // Approximate 鈥渁ny part enters the cone鈥?by checking several points on the collider bounds.
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
