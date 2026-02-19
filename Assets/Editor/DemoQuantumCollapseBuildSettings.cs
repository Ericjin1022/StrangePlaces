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
        private const string Level1Path = "Assets/Scenes/Level1_QuantumCollapse.unity";
        private const string Level2Path = "Assets/Scenes/Level2_NegativeMassBox.unity";
        private const string Level3Path = "Assets/Scenes/Level3_Color.unity";

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

            scenes = PruneMissingScenes(scenes);

            EnsurePresent(scenes, LevelSelectPath, enabled: true);
            EnsurePresent(scenes, Level1Path, enabled: true);
            EnsurePresent(scenes, Level2Path, enabled: true);
            EnsurePresent(scenes, Level3Path, enabled: true);

            scenes = scenes
                .OrderBy(s => s.path != LevelSelectPath)
                .ThenBy(s => s.path != Level1Path)
                .ThenBy(s => s.path != Level2Path)
                .ThenBy(s => s.path != Level3Path)
                .ThenBy(s => s.path)
                .ToList();

            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static List<EditorBuildSettingsScene> PruneMissingScenes(List<EditorBuildSettingsScene> scenes)
        {
            if (scenes == null || scenes.Count == 0)
            {
                return scenes ?? new List<EditorBuildSettingsScene>();
            }

            HashSet<string> seen = new HashSet<string>();
            List<EditorBuildSettingsScene> kept = new List<EditorBuildSettingsScene>(scenes.Count);
            for (int i = 0; i < scenes.Count; i++)
            {
                EditorBuildSettingsScene s = scenes[i];
                if (s == null)
                {
                    continue;
                }

                string path = s.path ?? "";
                if (string.IsNullOrWhiteSpace(path))
                {
                    continue;
                }

                // Drop missing assets (e.g. renamed scenes left behind in Build Settings).
                if (string.IsNullOrWhiteSpace(AssetDatabase.AssetPathToGUID(path)))
                {
                    continue;
                }

                if (!seen.Add(path))
                {
                    continue;
                }

                kept.Add(s);
            }

            return kept;
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

