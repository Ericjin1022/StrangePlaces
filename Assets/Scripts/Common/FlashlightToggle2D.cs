using UnityEngine;

namespace StrangePlaces.DemoQuantumCollapse
{
    public sealed class FlashlightToggle2D : MonoBehaviour
    {
        [Header("\u8F93\u5165")]
        [SerializeField] private KeyCode toggleKey = KeyCode.F;

        [Header("\u5F15\u7528")]
        [Tooltip("\u624B\u7535\u7B52\u7684\u6839\u7269\u4F53\uFF08\u4F8B\u5982 Player/FlashlightCone\uFF09\u3002\u6309\u952E\u4F1A\u5207\u6362\u5B83\u7684\u6FC0\u6D3B\u72B6\u6001\u3002")]
        [SerializeField] private GameObject flashlightRoot;

        [Tooltip("\u89C2\u6D4B\u68C0\u6D4B\u7EC4\u4EF6\uFF08ObserverCone2D\uFF09\u3002\u6536\u8D77\u624B\u7535\u7B52\u65F6\u4E5F\u4F1A\u4E00\u8D77\u7981\u7528\uFF0C\u907F\u514D\u4ECD\u7136\u89C2\u6D4B\u5230\u91CF\u5B50\u7269\u4F53\u3002")]
        [SerializeField] private ObserverCone2D observerCone;

        [Header("\u521D\u59CB\u72B6\u6001")]
        [SerializeField] private bool startDeployed = true;

        private bool warnedMissingRoot;
        private bool warnedMissingObserver;
        private bool deployed;

        public bool IsDeployed => flashlightRoot != null && flashlightRoot.activeSelf;

        private void Reset()
        {
            if (observerCone == null)
            {
                observerCone = GetComponentInChildren<ObserverCone2D>(true);
            }

            if (flashlightRoot == null)
            {
                if (observerCone != null)
                {
                    flashlightRoot = observerCone.gameObject;
                }
            }
        }

        private void Awake()
        {
            deployed = startDeployed;
            Apply();
        }

        private void Update()
        {
            if (!Input.GetKeyDown(toggleKey))
            {
                return;
            }

            deployed = !deployed;
            Apply();
        }

        public void SetDeployed(bool value)
        {
            deployed = value;
            Apply();
        }

        private void Apply()
        {
            if (flashlightRoot == null)
            {
                if (!warnedMissingRoot)
                {
                    warnedMissingRoot = true;
                    Debug.LogWarning("[\u624B\u7535\u7B52] \u8BF7\u7ED1\u5B9A flashlightRoot(\u73A9\u5BB6\u7684 FlashlightCone)\u3002", this);
                }
            }

            if (observerCone == null)
            {
                if (!warnedMissingObserver)
                {
                    warnedMissingObserver = true;
                    Debug.LogWarning("[\u624B\u7535\u7B52] \u672A\u7ED1\u5B9A observerCone\uFF08ObserverCone2D\uFF09\uFF0C\u6536\u8D77\u624B\u7535\u7B52\u53EA\u80FD\u5173\u6389\u5149\u6548\uFF0C\u65E0\u6CD5\u540C\u65F6\u7981\u7528\u89C2\u6D4B\u68C0\u6D4B\u903B\u8F91\u3002", this);
                }
            }
            else
            {
                observerCone.SetObservationEnabled(deployed);
            }

            if (flashlightRoot != null)
            {
                flashlightRoot.SetActive(deployed);
            }
        }
    }
}

