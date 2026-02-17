using System;
using UnityEngine;

namespace StrangePlaces.DemoQuantumCollapse
{
    public sealed class FermatNode2D : MonoBehaviour
    {
        [Serializable]
        public sealed class Edge
        {
            public FermatNode2D target;

            [Tooltip("边的速度倍率（1=正常；越大越快）。如果绑定了介质，则以介质倍率为准。")]
            public float speedMultiplier = 1f;

            [Tooltip("可选：绑定介质对象，用它的倍率动态决定这条边的速度。")]
            public FermatSpeedMedium2D medium;
        }

        [SerializeField] private Edge[] edges = Array.Empty<Edge>();

        public ReadOnlySpan<Edge> Edges => edges;

        public Vector2 Position => transform.position;

        public void SetEdges(Edge[] newEdges)
        {
            edges = newEdges ?? Array.Empty<Edge>();
        }

        public float GetEdgeSpeedMultiplier(Edge edge)
        {
            if (edge == null)
            {
                return 1f;
            }

            if (edge.medium != null)
            {
                return Mathf.Max(0.01f, edge.medium.CurrentSpeedMultiplier);
            }

            return Mathf.Max(0.01f, edge.speedMultiplier);
        }
    }
}
