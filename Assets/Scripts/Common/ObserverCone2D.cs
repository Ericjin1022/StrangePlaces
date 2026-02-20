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
        [SerializeField] private bool showDebugConeGizmos = false;
        [SerializeField, Range(6, 128)] private int debugConeArcSegments = 36;
        [SerializeField] private Color debugConeGizmoColor = new(1f, 0.95f, 0.25f, 0.35f);
        [SerializeField] private bool showDebugSamplePoints = false;
        [SerializeField] private Color debugSamplePointColor = new(0.25f, 1f, 0.65f, 0.95f);
        [SerializeField, Range(0.01f, 0.25f)] private float debugSamplePointRadius = 0.05f;

        [Header("Line of Sight")]
        [SerializeField] private bool requireLineOfSight = true;
        [SerializeField] private LayerMask raycastMask = ~0;
        [Tooltip("为 true 时，射线遮挡检测会忽略非目标的 Trigger（常用于避免触发器体积挡光）。注意：如果目标本身是 Trigger，仍然会被当作可观察对象。")]
        [SerializeField] private bool ignoreTriggerOccluders = true;

        [Header("Line of Sight Sampling")]
        [Tooltip("射线采样点数量（越大越不容易漏判“擦边可见”，但开销更高）。")]
        [SerializeField, Range(4, 64)] private int lineOfSightPerimeterSamples = 20;

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
        private readonly RaycastHit2D[] _rayHits = new RaycastHit2D[32];
        private readonly List<Collider2D> _colliderCache = new();
        private readonly List<Collider2D> _ownedColliderCache = new();

        private int _lastTargetsHash;
        private float _nextDebugTime;
        private float _lastRefreshTime;
        private const float AutoRefreshIntervalSeconds = 1.0f;

        private void Awake()
        {
            _player = GetComponentInParent<PlayerController2D>();
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
            int samples = Mathf.Clamp(lineOfSightPerimeterSamples, 4, 64);

            int ownedCount = GetOwnedColliders(target, _ownedColliderCache);
            if (ownedCount <= 0)
            {
                return IsObservedAtPoint(origin, aimDir, halfAngle, target, target.ObservationPoint);
            }

            for (int cIndex = 0; cIndex < ownedCount; cIndex++)
            {
                Collider2D collider = _ownedColliderCache[cIndex];
                if (collider == null)
                {
                    continue;
                }

                Bounds b = collider.bounds;
                if (IsObservedAtPoint(origin, aimDir, halfAngle, target, b.center))
                {
                    return true;
                }

                Vector2[] perimeter = GetBoundsPerimeterSamples(b, samples);
                for (int i = 0; i < perimeter.Length; i++)
                {
                    if (IsObservedAtPoint(origin, aimDir, halfAngle, target, perimeter[i]))
                    {
                        return true;
                    }
                }

                Vector2 closest = collider.ClosestPoint(origin);
                if ((closest - origin).sqrMagnitude > 0.0001f && IsObservedAtPoint(origin, aimDir, halfAngle, target, closest))
                {
                    return true;
                }
            }

            return false;
        }

        private int GetOwnedColliders(IObservationTarget target, List<Collider2D> results)
        {
            results.Clear();

            if (target is Component component)
            {
                _colliderCache.Clear();
                component.GetComponentsInChildren(true, _colliderCache);
                for (int i = 0; i < _colliderCache.Count; i++)
                {
                    Collider2D c = _colliderCache[i];
                    if (c != null && target.OwnsCollider(c))
                    {
                        results.Add(c);
                    }
                }
            }

            if (results.Count == 0)
            {
                Collider2D primary = target.PrimaryCollider;
                if (primary != null)
                {
                    results.Add(primary);
                }
            }

            return results.Count;
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
            float nearest = float.PositiveInfinity;
            Collider2D nearestCollider = null;

            int hitCount = Physics2D.RaycastNonAlloc(rayOrigin, dir, _rayHits, distance, raycastMask);
            for (int i = 0; i < hitCount; i++)
            {
                Collider2D collider = _rayHits[i].collider;
                if (collider == null)
                {
                    continue;
                }

                if (_selfCollider != null && collider == _selfCollider)
                {
                    continue;
                }

                if (_player != null && collider.transform.IsChildOf(_player.transform))
                {
                    continue;
                }

                if (ignoreTriggerOccluders && collider.isTrigger && !target.OwnsCollider(collider))
                {
                    continue;
                }

                if (_rayHits[i].distance < nearest)
                {
                    nearest = _rayHits[i].distance;
                    nearestCollider = collider;
                }
            }

            if (nearestCollider == null || float.IsPositiveInfinity(nearest))
            {
                return true;
            }

            return target.OwnsCollider(nearestCollider);
        }

        private static Vector2[] GetBoundsPerimeterSamples(Bounds b, int samples)
        {
            samples = Mathf.Clamp(samples, 4, 256);

            Vector2 min = b.min;
            Vector2 max = b.max;
            float w = Mathf.Max(0.0001f, max.x - min.x);
            float h = Mathf.Max(0.0001f, max.y - min.y);
            float perimeter = 2f * (w + h);

            Vector2[] pts = new Vector2[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (i + 0.5f) / samples * perimeter;

                if (t < w)
                {
                    pts[i] = new Vector2(min.x + t, min.y);
                    continue;
                }

                t -= w;
                if (t < h)
                {
                    pts[i] = new Vector2(max.x, min.y + t);
                    continue;
                }

                t -= h;
                if (t < w)
                {
                    pts[i] = new Vector2(max.x - t, max.y);
                    continue;
                }

                t -= w;
                pts[i] = new Vector2(min.x, max.y - Mathf.Min(t, h));
            }

            return pts;
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

        private void OnDrawGizmos()
        {
            DrawDebugSamplePointsGizmos();

            if (!showDebugConeGizmos)
            {
                return;
            }

            Vector3 origin = transform.position;
            float distance = Mathf.Max(0.01f, maxDistance);

            Vector2 aim = _player != null ? _player.AimDirection : (Vector2)transform.right;
            if (aim.sqrMagnitude < 0.0001f)
            {
                aim = Vector2.right;
            }
            aim.Normalize();

            float halfAngleRad = coneAngleDegrees * 0.5f * Mathf.Deg2Rad;
            Vector2 leftDir = Rotate(aim, -halfAngleRad);
            Vector2 rightDir = Rotate(aim, halfAngleRad);

            Color old = Gizmos.color;
            Gizmos.color = debugConeGizmoColor;

            Gizmos.DrawLine(origin, origin + (Vector3)(leftDir * distance));
            Gizmos.DrawLine(origin, origin + (Vector3)(rightDir * distance));

            int segments = Mathf.Clamp(debugConeArcSegments, 6, 256);
            float start = -halfAngleRad;
            float end = halfAngleRad;
            Vector3 prev = origin + (Vector3)(Rotate(aim, start) * distance);
            for (int i = 1; i <= segments; i++)
            {
                float t = i / (float)segments;
                float a = Mathf.Lerp(start, end, t);
                Vector3 p = origin + (Vector3)(Rotate(aim, a) * distance);
                Gizmos.DrawLine(prev, p);
                prev = p;
            }

            Gizmos.color = old;
        }

        private void DrawDebugSamplePointsGizmos()
        {
            if (!showDebugSamplePoints)
            {
                return;
            }

            Color old = Gizmos.color;
            Gizmos.color = debugSamplePointColor;

            float r = Mathf.Max(0.001f, debugSamplePointRadius);

            int samples = Mathf.Clamp(lineOfSightPerimeterSamples, 4, 64);
            bool drewAny = false;

            if (!string.IsNullOrWhiteSpace(debugTargetName))
            {
                GameObject targetGo = GameObject.Find(debugTargetName);
                if (targetGo != null && TryFindObservationTarget(targetGo, out IObservationTarget target))
                {
                    DrawTargetSamplePoints(target, samples, r);
                    drewAny = true;
                }
            }

            if (!drewAny)
            {
                MonoBehaviour[] behaviours = FindObjectsOfType<MonoBehaviour>(true);
                for (int i = 0; i < behaviours.Length; i++)
                {
                    MonoBehaviour mb = behaviours[i];
                    if (mb == null)
                    {
                        continue;
                    }

                    if (mb is not IObservationTarget target)
                    {
                        continue;
                    }

                    DrawTargetSamplePoints(target, samples, r);
                    drewAny = true;
                }
            }

            Gizmos.color = old;
        }

        private void DrawTargetSamplePoints(IObservationTarget target, int samples, float radius)
        {
            int ownedCount = GetOwnedColliders(target, _ownedColliderCache);
            if (ownedCount <= 0)
            {
                Gizmos.DrawSphere(target.ObservationPoint, radius);
                return;
            }

            for (int cIndex = 0; cIndex < ownedCount; cIndex++)
            {
                Collider2D c = _ownedColliderCache[cIndex];
                if (c == null)
                {
                    continue;
                }

                Bounds b = c.bounds;
                Gizmos.DrawSphere(b.center, radius);

                Vector2[] points = GetBoundsPerimeterSamples(b, samples);
                for (int i = 0; i < points.Length; i++)
                {
                    Gizmos.DrawSphere(points[i], radius);
                }
            }
        }

        private static bool TryFindObservationTarget(GameObject go, out IObservationTarget target)
        {
            target = null;
            if (go == null)
            {
                return false;
            }

            MonoBehaviour[] self = go.GetComponents<MonoBehaviour>();
            for (int i = 0; i < self.Length; i++)
            {
                if (self[i] is IObservationTarget t)
                {
                    target = t;
                    return true;
                }
            }

            MonoBehaviour[] parents = go.GetComponentsInParent<MonoBehaviour>(true);
            for (int i = 0; i < parents.Length; i++)
            {
                if (parents[i] is IObservationTarget t)
                {
                    target = t;
                    return true;
                }
            }

            return false;
        }
    }
}





