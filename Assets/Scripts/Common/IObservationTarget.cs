using UnityEngine;

namespace StrangePlaces.DemoQuantumCollapse
{
    public interface IObservationTarget
    {
        Vector2 ObservationPoint { get; }
        Collider2D PrimaryCollider { get; }
        void SetObserved(bool observed);
        bool OwnsCollider(Collider2D collider);
    }
}
