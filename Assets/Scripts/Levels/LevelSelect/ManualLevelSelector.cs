using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace StrangePlaces.DemoQuantumCollapse
{
    /// <summary>
    /// 允许开发者在 Inspector 面板中手动将节点按钮与 Scene 文件一一对应，
    /// 并在运行时点击按钮自动加载场景。
    /// </summary>
    public class ManualLevelSelector : MonoBehaviour
    {
        [System.Serializable]
        public class LevelMapping
        {
            [Tooltip("自己搭建的关卡节点按钮")]
            public Button nodeButton;

#if UNITY_EDITOR
            [Tooltip("拖入对应的 Scene 文件（请确保它已经加入到 Build Settings 中）")]
            public UnityEditor.SceneAsset sceneAsset;
#endif

            // 保存最终用于运行时的场景名称
            [HideInInspector]
            public string sceneName;
        }

        [Header("手动配置的节点与关卡映射列表")]
        public List<LevelMapping> levelMappings = new List<LevelMapping>();

        private void OnValidate()
        {
#if UNITY_EDITOR
            // 如果在编辑器中挂载了 Scene 文件，自动提取其名称以供运行时使用
            if (levelMappings != null)
            {
                foreach (var mapping in levelMappings)
                {
                    if (mapping.sceneAsset != null)
                    {
                        mapping.sceneName = mapping.sceneAsset.name;
                    }
                    else
                    {
                        mapping.sceneName = string.Empty;
                    }
                }
            }
#endif
        }

        private void Start()
        {
            if (levelMappings == null) return;

            foreach (var mapping in levelMappings)
            {
                if (mapping.nodeButton != null)
                {
                    // 运行前先清除已有的 onClick 事件以防重复绑定
                    mapping.nodeButton.onClick.RemoveAllListeners();

                    if (!string.IsNullOrEmpty(mapping.sceneName))
                    {
                        // 局部变量捕获循环变量
                        string targetScene = mapping.sceneName;
                        mapping.nodeButton.onClick.AddListener(() => 
                        {
                            Debug.Log($"[ManualLevelSelector] 即将进入场景: {targetScene}");
                            SceneManager.LoadScene(targetScene);
                        });
                        
                        mapping.nodeButton.interactable = true;
                    }
                    else
                    {
                        // 如果没有配置对应的场景，就把按钮禁用掉
                        mapping.nodeButton.interactable = false;
                    }
                }
            }
        }
    }
}
