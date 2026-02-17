using UnityEngine;

namespace StrangePlaces.DemoQuantumCollapse
{
    [DisallowMultipleComponent]
    public sealed class NegativeMassRiderSensor2D : MonoBehaviour
    {
        private NegativeMassBox2D _box;

        public void Bind(NegativeMassBox2D box)
        {
            _box = box;
        }

        private void Awake()
        {
            if (_box == null)
            {
                _box = GetComponentInParent<NegativeMassBox2D>();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_box == null || other == null)
            {
                return;
            }

            PlayerController2D player = other.GetComponent<PlayerController2D>();
            if (player == null)
            {
                return;
            }

            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                return;
            }

            _box.RegisterRider(rb);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (_box == null || other == null)
            {
                return;
            }

            PlayerController2D player = other.GetComponent<PlayerController2D>();
            if (player == null)
            {
                return;
            }

            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                return;
            }

            _box.UnregisterRider(rb);
        }
    }
}

