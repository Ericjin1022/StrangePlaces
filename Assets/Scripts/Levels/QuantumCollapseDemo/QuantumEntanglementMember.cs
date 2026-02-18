using UnityEngine;

namespace StrangePlaces.DemoQuantumCollapse
{
    [DisallowMultipleComponent]
    public sealed class QuantumEntanglementMember : MonoBehaviour
    {
        [Tooltip("相同的“纠缠键”会被视为同一组纠缠对象：观察任意一个，整组都会坍缩为稳定态。")]
        [SerializeField] private string entanglementKey = "";

        public string EntanglementKey => entanglementKey;
    }
}

