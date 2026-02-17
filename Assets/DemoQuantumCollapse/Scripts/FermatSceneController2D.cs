using UnityEngine;

namespace StrangePlaces.DemoQuantumCollapse
{
    public sealed class FermatSceneController2D : MonoBehaviour
    {
        [SerializeField] private FermatProbe2D probe;

        private void Awake()
        {
            if (probe == null)
            {
                probe = FindFirstObjectByType<FermatProbe2D>();
            }
        }

        private void Update()
        {
            if (probe == null)
            {
                probe = FindFirstObjectByType<FermatProbe2D>();
            }

            if (Input.GetKeyDown(KeyCode.T))
            {
                if (probe != null)
                {
                    probe.RestartFromStart();
                }
            }
        }

        public void RequestReplan()
        {
            if (probe != null)
            {
                probe.RequestReplan();
            }
        }
    }
}
