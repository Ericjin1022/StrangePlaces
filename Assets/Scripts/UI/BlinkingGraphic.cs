using UnityEngine;
using UnityEngine.UI;

namespace StrangePlaces.UI
{
    [RequireComponent(typeof(Graphic))]
    public class BlinkingGraphic : MonoBehaviour
    {
        [SerializeField] private float blinkSpeed = 2f;
        [SerializeField] private float minAlpha = 0.2f;
        [SerializeField] private float maxAlpha = 1.0f;
        
        [Tooltip("If true, uses unscaled time so it continues blinking when paused.")]
        [SerializeField] private bool useUnscaledTime = true;

        private Graphic _graphic;

        private void Awake()
        {
            _graphic = GetComponent<Graphic>();
        }

        private void Update()
        {
            if (_graphic != null)
            {
                Color c = _graphic.color;
                float time = useUnscaledTime ? Time.unscaledTime : Time.time;
                // PingPong oscillates between 0 and 1
                float t = Mathf.PingPong(time * blinkSpeed, 1f);
                
                c.a = Mathf.Lerp(minAlpha, maxAlpha, t);
                _graphic.color = c;
            }
        }
    }
}
