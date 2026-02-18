using UnityEngine;

namespace StrangePlaces.DemoQuantumCollapse
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Renderer))]
    public sealed class DemoColorRenderer : MonoBehaviour
    {
        [SerializeField] private Color color = Color.white;

        private Renderer _renderer;
        private Material _materialInstance;

        public void SetColor(Color newColor)
        {
            color = newColor;
            Ensure();
            Apply();
        }

        private void Awake()
        {
            Ensure();
            Apply();
        }

        private void OnValidate()
        {
            Ensure();
            Apply();
        }

        private void Ensure()
        {
            if (_renderer == null)
            {
                _renderer = GetComponent<Renderer>();
            }

            if (_materialInstance == null && _renderer != null)
            {
                _materialInstance = new Material(Shader.Find("Sprites/Default"));
                _renderer.sharedMaterial = _materialInstance;
            }
        }

        private void Apply()
        {
            if (_materialInstance != null)
            {
                _materialInstance.color = color;
            }
        }
    }
}
