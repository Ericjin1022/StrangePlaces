#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace StrangePlaces.DemoQuantumCollapse
{
    [InitializeOnLoad]
    public static class DemoQuantumCollapseBuildSettings
    {
        private const string LevelSelectPath = "Assets/Scenes/LevelSelect.unity";
        private const string DemoPath = "Assets/Scenes/QuantumCollapseDemo.unity";

        static DemoQuantumCollapseBuildSettings()
        {
            EnsureOrder();
        }

        [MenuItem("Tools/StrangePlaces/修复 Build Settings 顺序")]
        private static void EnsureOrderMenu()
        {
            EnsureOrder();
        }

        private static void EnsureOrder()
        {
            List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes?.ToList() ?? new List<EditorBuildSettingsScene>();

            EnsurePresent(scenes, LevelSelectPath, enabled: true);
            EnsurePresent(scenes, DemoPath, enabled: true);

            scenes = scenes
                .OrderBy(s => s.path != LevelSelectPath)
                .ThenBy(s => s.path != DemoPath)
                .ThenBy(s => s.path)
                .ToList();

            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void EnsurePresent(List<EditorBuildSettingsScene> scenes, string path, bool enabled)
        {
            if (scenes.Any(s => s.path == path))
            {
                for (int i = 0; i < scenes.Count; i++)
                {
                    if (scenes[i].path == path)
                    {
                        scenes[i].enabled = enabled;
                    }
                }
                return;
            }

            string guid = AssetDatabase.AssetPathToGUID(path);
            if (string.IsNullOrWhiteSpace(guid))
            {
                return;
            }

            scenes.Add(new EditorBuildSettingsScene(path, enabled));
        }
    }
}
#endif
