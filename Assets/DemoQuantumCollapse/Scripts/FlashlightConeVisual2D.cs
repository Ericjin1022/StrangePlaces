using UnityEngine;

namespace StrangePlaces.DemoQuantumCollapse
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public sealed class FlashlightConeVisual2D : MonoBehaviour
    {
        private const string MaterialResourcePath = "FlashlightCone2D_Mat";

        [Header("Shape")]
        [SerializeField] private float length = 7f;
        [SerializeField] private float angleDegrees = 60f;
        [SerializeField] private int segments = 28;

        [Header("Look")]
        [SerializeField] private Color color = new(1f, 1f, 0.3f, 0.35f);
        [SerializeField, Range(0f, 0.5f)] private float edgeSoftness = 0.18f;
        [SerializeField, Range(0f, 1f)] private float lengthSoftness = 0.35f;
        [SerializeField, Range(0f, 2f)] private float centerBoost = 0.35f;
        [SerializeField] private int sortingOrder = 100;

        [Header("Drive")]
        [SerializeField] private PlayerController2D player;

        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private Mesh _mesh;
        private Material _material;

        private float _lastLength = -1f;
        private float _lastAngle = -1f;
        private int _lastSegments = -1;

        private void Awake()
        {
            _meshFilter = GetComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();

            if (player == null)
            {
                player = GetComponentInParent<PlayerController2D>();
                if (player == null)
                {
                    player = FindFirstObjectByType<PlayerController2D>();
                }
            }

            _mesh = new Mesh { name = "FlashlightCone2D" };
            _meshFilter.sharedMesh = _mesh;

            _material = CreateMaterialInstance();
            _meshRenderer.sharedMaterial = _material;
            _meshRenderer.sortingOrder = sortingOrder;
            _meshRenderer.sortingLayerName = "Default";
        }

        private static Material CreateMaterialInstance()
        {
            Material template = Resources.Load<Material>(MaterialResourcePath);
            if (template != null)
            {
                return new Material(template);
            }

            Shader shader = Shader.Find("StrangePlaces/FlashlightCone2D");
            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Unlit");
            }
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }
            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }

            if (shader == null)
            {
                Debug.LogError("FlashlightConeVisual2D: 找不到可用 Shader，手电筒光可能无法显示。");
                return new Material(Shader.Find("Sprites/Default"));
            }

            return new Material(shader);
        }

        private void OnDestroy()
        {
            if (Application.isPlaying)
            {
                if (_mesh != null) Destroy(_mesh);
                if (_material != null) Destroy(_material);
            }
            else
            {
                if (_mesh != null) DestroyImmediate(_mesh);
                if (_material != null) DestroyImmediate(_material);
            }
        }

        private void LateUpdate()
        {
            Vector2 aim = player != null ? player.AimDirection : (Vector2)transform.right;
            if (aim.sqrMagnitude > 0.001f)
            {
                transform.right = new Vector3(aim.x, aim.y, 0f);
            }

            ApplyMaterial();
            RebuildMeshIfNeeded();
        }

        public void SetPlayer(PlayerController2D newPlayer)
        {
            player = newPlayer;
        }

        public void Configure(float newLength, float newAngleDegrees)
        {
            length = newLength;
            angleDegrees = newAngleDegrees;
        }

        public void SetSortingOrder(int newSortingOrder)
        {
            sortingOrder = newSortingOrder;
            if (_meshRenderer != null)
            {
                _meshRenderer.sortingOrder = sortingOrder;
            }
        }

        private void ApplyMaterial()
        {
            if (_material == null)
            {
                return;
            }

            _material.SetColor("_Color", color);
            _material.SetFloat("_EdgeSoftness", edgeSoftness);
            _material.SetFloat("_LengthSoftness", lengthSoftness);
            _material.SetFloat("_CenterBoost", centerBoost);
        }

        private void RebuildMeshIfNeeded()
        {
            if (_mesh == null)
            {
                return;
            }

            segments = Mathf.Clamp(segments, 3, 128);
            length = Mathf.Max(0.1f, length);
            angleDegrees = Mathf.Clamp(angleDegrees, 1f, 175f);

            if (Mathf.Approximately(_lastLength, length) &&
                Mathf.Approximately(_lastAngle, angleDegrees) &&
                _lastSegments == segments)
            {
                return;
            }

            _lastLength = length;
            _lastAngle = angleDegrees;
            _lastSegments = segments;

            float halfRad = angleDegrees * 0.5f * Mathf.Deg2Rad;

            int vertexCount = segments + 2; // origin + arc vertices
            Vector3[] vertices = new Vector3[vertexCount];
            Vector2[] uvs = new Vector2[vertexCount];
            int[] triangles = new int[segments * 3];

            vertices[0] = Vector3.zero;
            uvs[0] = new Vector2(0.5f, 0f);

            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments; // 0..1
                float angle = Mathf.Lerp(-halfRad, halfRad, t);
                Vector3 dir = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
                vertices[i + 1] = dir * length;
                uvs[i + 1] = new Vector2(t, 1f);
            }

            for (int i = 0; i < segments; i++)
            {
                int tri = i * 3;
                triangles[tri + 0] = 0;
                triangles[tri + 1] = i + 1;
                triangles[tri + 2] = i + 2;
            }

            _mesh.Clear();
            _mesh.vertices = vertices;
            _mesh.uv = uvs;
            _mesh.triangles = triangles;
            _mesh.RecalculateBounds();
        }
    }
}
