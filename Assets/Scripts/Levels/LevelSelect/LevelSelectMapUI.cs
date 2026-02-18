using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace StrangePlaces.DemoQuantumCollapse
{
    [ExecuteAlways]
    public sealed class LevelSelectMapUI : MonoBehaviour
    {
        private const string GeneratedRootName = "__关卡地图_生成";

        [Serializable]
        private sealed class LevelNode
        {
            [Tooltip("节点 ID（用于连线）。")]
            public string id = "1-1";

            [Tooltip("显示在节点上方的文字（建议中文+编号）。")]
            public string label = "关卡 1-1";

            [Tooltip("Build Settings 里的场景名（不含路径与扩展名）。")]
            public string sceneName = "QuantumCollapseDemo";

            [Tooltip("节点位置：以面板为基准的归一化坐标（0..1）。")]
            public Vector2 normalizedPosition = new(0.2f, 0.7f);

            [Tooltip("是否已解锁（未解锁会变灰且不可点击）。")]
            public bool unlocked = true;

            [Tooltip("是否为当前选中的节点（会高亮）。")]
            public bool isCurrent = false;
        }

        [Serializable]
        private sealed class LevelEdge
        {
            public string fromId = "1-1";
            public string toId = "1-2";
        }

        [Header("面板（可留空，自动查找 Canvas 下的“关卡面板”）")]
        [SerializeField] private RectTransform rootPanel;

        [Header("字体（可选）")]
        [Tooltip("可选：显式指定 UI 字体（推荐包含中文的字体）。留空则尝试使用系统字体/内置字体。")]
        [SerializeField] private Font uiFontOverride;

        [Header("标题（必须为中文）")]
        [SerializeField] private string worldTitle = "第一世界";
        [SerializeField] private string subtitle = "当前：第 1-3 关";

        [Header("生成模式")]
        [Tooltip("自动从 Build Settings 读取关卡场景并生成节点（推荐，便于后续扩展）。")]
        [SerializeField] private bool autoGenerateFromBuildSettings = true;

        [Tooltip("是否允许在运行时重建 UI（会创建对象/调整布局）。关闭后，运行时将完全以场景中的静态 UI 为准。")]
        [SerializeField] private bool rebuildInPlayMode = false;

        [Tooltip("每行最多多少关（用于蛇形布局）。")]
        [SerializeField, Range(2, 8)] private int levelsPerRow = 4;

        [Tooltip("当前所在关卡序号（从 1 开始，例如 1 表示 1-1）。")]
        [SerializeField, Min(1)] private int currentLevelNumber = 1;

        [Header("节点与连线")]
        [SerializeField] private LevelNode[] nodes = Array.Empty<LevelNode>();
        [SerializeField] private LevelEdge[] edges = Array.Empty<LevelEdge>();

        [Header("外观")]
        [SerializeField] private Color backgroundColor = new(0.10f, 0.10f, 0.12f, 0.96f);
        [SerializeField] private Color lineColor = new(0.85f, 0.85f, 0.85f, 0.35f);
        [SerializeField] private Color nodeColor = new(0.95f, 0.95f, 0.97f, 1f);
        [SerializeField] private Color currentNodeColor = new(0.90f, 0.10f, 0.10f, 1f);
        [SerializeField] private Color lockedNodeColor = new(0.55f, 0.55f, 0.60f, 0.9f);
        [SerializeField] private Color labelColor = new(0.80f, 0.80f, 0.85f, 0.95f);

        [Header("尺寸")]
        [SerializeField, Range(20f, 120f)] private float nodeSize = 62f;
        [SerializeField, Range(2f, 16f)] private float lineThickness = 6f;
        [SerializeField, Range(10f, 80f)] private float labelOffsetY = 34f;

        private Sprite _circleSprite;
        private Sprite _lineSprite;
        private Font _font;

        private void Reset()
        {
            ApplySampleLayout();
        }

        private void OnEnable()
        {
            EnsureDefaults();
            if (Application.isPlaying && !rebuildInPlayMode)
            {
                BindExistingNodeButtons();
                return;
            }

            if (!Application.isPlaying || rebuildInPlayMode)
            {
                Rebuild();
            }
        }

        private void OnValidate()
        {
            EnsureDefaults();
            if (!Application.isPlaying || rebuildInPlayMode)
            {
                Rebuild();
            }
        }

        private void BindExistingNodeButtons()
        {
            if (rootPanel == null)
            {
                Canvas canvas = FindFirstObjectByType<Canvas>();
                if (canvas != null)
                {
                    Transform panel = canvas.transform.Find("关卡面板");
                    if (panel != null)
                    {
                        rootPanel = panel as RectTransform;
                    }
                }
            }

            if (rootPanel == null)
            {
                Debug.LogError("[选关] rootPanel 未配置，无法绑定关卡节点按钮。");
                return;
            }

            if (nodes == null || nodes.Length == 0)
            {
                return;
            }

            for (int i = 0; i < nodes.Length; i++)
            {
                LevelNode node = nodes[i];
                if (node == null || string.IsNullOrWhiteSpace(node.id))
                {
                    continue;
                }

                string id = node.id.Trim();
                string nodeObjectName = $"节点_{id}";
                Transform nodeT = FindDeepChild(rootPanel, nodeObjectName);
                if (nodeT == null)
                {
                    nodeT = FindDeepChild(rootPanel, id);
                }

                if (nodeT == null)
                {
                    continue;
                }

                Button btn = nodeT.GetComponent<Button>();
                if (btn == null)
                {
                    continue;
                }

                btn.onClick.RemoveAllListeners();
                string sceneName = node.sceneName != null ? node.sceneName.Trim() : "";
                bool interactable = node.unlocked && !string.IsNullOrWhiteSpace(sceneName);
                btn.interactable = interactable;
                if (interactable)
                {
                    string sn = sceneName;
                    btn.onClick.AddListener(() => SceneManager.LoadScene(sn));
                }
            }
        }

        private static Transform FindDeepChild(Transform root, string name)
        {
            if (root == null || string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child == null)
                {
                    continue;
                }

                if (string.Equals(child.name, name, StringComparison.Ordinal))
                {
                    return child;
                }

                Transform found = FindDeepChild(child, name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private void EnsureDefaults()
        {
            if (autoGenerateFromBuildSettings)
            {
                BuildFromBuildSettings();
            }

            if (!autoGenerateFromBuildSettings && (nodes == null || nodes.Length == 0))
            {
                ApplySampleLayout();
            }

            if (_font == null)
            {
                _font = ResolveUIFont();
            }

            if (_circleSprite == null)
            {
                _circleSprite = CreateCircleSprite(96, 4f);
            }

            if (_lineSprite == null)
            {
                _lineSprite = CreateSquareSprite();
            }
        }

        private void ApplySampleLayout()
        {
            worldTitle = "第一世界";
            subtitle = "当前：第 1-3 关";

            nodes = new[]
            {
                new LevelNode { id = "1-1", label = "关卡 1-1", sceneName = "QuantumCollapseDemo", normalizedPosition = new Vector2(0.20f, 0.68f), unlocked = true },
                new LevelNode { id = "1-2", label = "关卡 1-2", sceneName = "QuantumCollapseDemo", normalizedPosition = new Vector2(0.40f, 0.68f), unlocked = true },
                new LevelNode { id = "1-3", label = "关卡 1-3", sceneName = "QuantumCollapseDemo", normalizedPosition = new Vector2(0.60f, 0.68f), unlocked = true, isCurrent = true },
                new LevelNode { id = "1-4", label = "关卡 1-4", sceneName = "QuantumCollapseDemo", normalizedPosition = new Vector2(0.80f, 0.68f), unlocked = true },

                new LevelNode { id = "1-5", label = "关卡 1-5", sceneName = "QuantumCollapseDemo", normalizedPosition = new Vector2(0.80f, 0.43f), unlocked = true },
                new LevelNode { id = "1-6", label = "关卡 1-6", sceneName = "QuantumCollapseDemo", normalizedPosition = new Vector2(0.60f, 0.43f), unlocked = true },
                new LevelNode { id = "1-7", label = "关卡 1-7", sceneName = "QuantumCollapseDemo", normalizedPosition = new Vector2(0.40f, 0.43f), unlocked = true },
                new LevelNode { id = "1-8", label = "关卡 1-8", sceneName = "QuantumCollapseDemo", normalizedPosition = new Vector2(0.20f, 0.43f), unlocked = true },

                new LevelNode { id = "1-9", label = "关卡 1-9", sceneName = "QuantumCollapseDemo", normalizedPosition = new Vector2(0.20f, 0.20f), unlocked = true },
            };

            edges = new[]
            {
                new LevelEdge { fromId = "1-1", toId = "1-2" },
                new LevelEdge { fromId = "1-2", toId = "1-3" },
                new LevelEdge { fromId = "1-3", toId = "1-4" },
                new LevelEdge { fromId = "1-4", toId = "1-5" },
                new LevelEdge { fromId = "1-5", toId = "1-6" },
                new LevelEdge { fromId = "1-6", toId = "1-7" },
                new LevelEdge { fromId = "1-7", toId = "1-8" },
                new LevelEdge { fromId = "1-8", toId = "1-9" },
            };
        }

        private void BuildFromBuildSettings()
        {
            string[] sceneNames = GetPlayableSceneNamesInBuildOrder();
            if (sceneNames.Length == 0)
            {
                return;
            }

            worldTitle = "第一世界";
            currentLevelNumber = Mathf.Clamp(currentLevelNumber, 1, sceneNames.Length);
            subtitle = $"当前：第 1-{currentLevelNumber} 关";

            int perRow = Mathf.Clamp(levelsPerRow, 2, 8);
            float xMin = 0.20f;
            float xMax = 0.80f;
            float yTop = 0.68f;
            float yStep = 0.25f;

            LevelNode[] builtNodes = new LevelNode[sceneNames.Length];
            for (int i = 0; i < sceneNames.Length; i++)
            {
                int row = i / perRow;
                int col = i % perRow;
                bool reverse = (row % 2) == 1;
                int colForX = reverse ? (perRow - 1 - col) : col;
                float t = perRow <= 1 ? 0.5f : colForX / (float)(perRow - 1);

                float x = Mathf.Lerp(xMin, xMax, t);
                float y = yTop - row * yStep;
                y = Mathf.Clamp01(y);

                int levelNumber = i + 1;
                builtNodes[i] = new LevelNode
                {
                    id = $"1-{levelNumber}",
                    label = $"第 1-{levelNumber} 关",
                    sceneName = sceneNames[i],
                    normalizedPosition = new Vector2(x, y),
                    unlocked = true,
                    isCurrent = levelNumber == currentLevelNumber,
                };
            }

            LevelEdge[] builtEdges = new LevelEdge[Mathf.Max(0, builtNodes.Length - 1)];
            for (int i = 0; i < builtEdges.Length; i++)
            {
                builtEdges[i] = new LevelEdge { fromId = builtNodes[i].id, toId = builtNodes[i + 1].id };
            }

            nodes = builtNodes;
            edges = builtEdges;
        }

        private static string[] GetPlayableSceneNamesInBuildOrder()
        {
            List<string> names = new();

#if UNITY_EDITOR
            // Editor: use EditorBuildSettings to get proper order.
            try
            {
                var scenes = UnityEditor.EditorBuildSettings.scenes;
                for (int i = 0; i < scenes.Length; i++)
                {
                    if (scenes[i] == null || !scenes[i].enabled)
                    {
                        continue;
                    }

                    string path = scenes[i].path ?? "";
                    string name = System.IO.Path.GetFileNameWithoutExtension(path);
                    if (string.IsNullOrWhiteSpace(name) || name == "LevelSelect")
                    {
                        continue;
                    }

                    names.Add(name);
                }
            }
            catch
            {
                // Fall back to runtime API.
            }
#endif

            if (names.Count == 0)
            {
                int count = UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings;
                for (int i = 0; i < count; i++)
                {
                    string path = UnityEngine.SceneManagement.SceneUtility.GetScenePathByBuildIndex(i);
                    string name = System.IO.Path.GetFileNameWithoutExtension(path);
                    if (string.IsNullOrWhiteSpace(name) || name == "LevelSelect")
                    {
                        continue;
                    }

                    names.Add(name);
                }
            }

            return names.ToArray();
        }

        private Font ResolveUIFont()
        {
            if (uiFontOverride != null)
            {
                return uiFontOverride;
            }

            // Prefer OS fonts that include Chinese glyphs on Windows.
            // If none exist, fall back to Unity built-in fonts.
            string[] candidates =
            {
                "Microsoft YaHei UI",
                "Microsoft YaHei",
                "SimHei",
                "PingFang SC",
                "Noto Sans CJK SC",
                "Arial Unicode MS",
            };

            for (int i = 0; i < candidates.Length; i++)
            {
                try
                {
                    Font osFont = Font.CreateDynamicFontFromOSFont(candidates[i], 16);
                    if (osFont != null)
                    {
                        return osFont;
                    }
                }
                catch
                {
                    // Ignore and try next.
                }
            }

            try
            {
                Font builtin = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (builtin != null)
                {
                    return builtin;
                }
            }
            catch
            {
                // Ignore.
            }

            try
            {
                // Unity 2022+ may warn that Arial.ttf is not a valid built-in font; keep as last resort.
                return Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
            catch
            {
                return null;
            }
        }

        private void Rebuild()
        {
            if (Application.isPlaying && !rebuildInPlayMode)
            {
                return;
            }

            if (rootPanel == null)
            {
                Canvas canvas = FindFirstObjectByType<Canvas>();
                if (canvas != null)
                {
                    Transform panel = canvas.transform.Find("关卡面板");
                    if (panel != null)
                    {
                        rootPanel = panel as RectTransform;
                    }
                }
            }

            if (rootPanel == null)
            {
                return;
            }

            Image bg = rootPanel.GetComponent<Image>();
            if (bg == null)
            {
                if (Application.isPlaying)
                {
                    Debug.LogError("[关卡地图] rootPanel 缺少 Image，请在场景中手动配置（已禁止运行时自动补组件）。");
                    return;
                }

                bg = rootPanel.gameObject.AddComponent<Image>();
            }
            bg.color = backgroundColor;

            for (int i = rootPanel.childCount - 1; i >= 0; i--)
            {
                Transform child = rootPanel.GetChild(i);
                if (child != null && child.name == GeneratedRootName)
                {
                    DestroySmart(child.gameObject);
                }
            }

            GameObject generated = new(GeneratedRootName, typeof(RectTransform));
            RectTransform generatedRt = generated.GetComponent<RectTransform>();
            generatedRt.SetParent(rootPanel, false);
            generatedRt.anchorMin = Vector2.zero;
            generatedRt.anchorMax = Vector2.one;
            generatedRt.offsetMin = Vector2.zero;
            generatedRt.offsetMax = Vector2.zero;

            Dictionary<string, LevelNode> nodeById = new(StringComparer.Ordinal);
            for (int i = 0; i < nodes.Length; i++)
            {
                LevelNode n = nodes[i];
                if (n == null || string.IsNullOrWhiteSpace(n.id))
                {
                    continue;
                }
                nodeById[n.id.Trim()] = n;
            }

            DrawTitles(generatedRt);
            DrawEdges(generatedRt, nodeById);
            DrawNodes(generatedRt, nodeById);
        }

        private void DrawTitles(RectTransform parent)
        {
            RectTransform titleRt = CreateText(parent, "标题", worldTitle, 44, Color.white, TextAnchor.UpperCenter);
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.anchoredPosition = new Vector2(0f, -18f);
            titleRt.sizeDelta = new Vector2(0f, 64f);

            RectTransform subRt = CreateText(parent, "副标题", subtitle, 22, new Color(0.92f, 0.92f, 0.95f, 1f), TextAnchor.UpperCenter);
            subRt.anchorMin = new Vector2(0f, 1f);
            subRt.anchorMax = new Vector2(1f, 1f);
            subRt.pivot = new Vector2(0.5f, 1f);
            subRt.anchoredPosition = new Vector2(0f, -78f);
            subRt.sizeDelta = new Vector2(0f, 40f);
        }

        private void DrawEdges(RectTransform parent, Dictionary<string, LevelNode> nodeById)
        {
            RectTransform edgesRoot = new GameObject("连线", typeof(RectTransform)).GetComponent<RectTransform>();
            edgesRoot.SetParent(parent, false);
            edgesRoot.anchorMin = Vector2.zero;
            edgesRoot.anchorMax = Vector2.one;
            edgesRoot.offsetMin = Vector2.zero;
            edgesRoot.offsetMax = Vector2.zero;

            for (int i = 0; i < edges.Length; i++)
            {
                LevelEdge e = edges[i];
                if (e == null)
                {
                    continue;
                }

                if (!TryGetNode(nodeById, e.fromId, out LevelNode from) || !TryGetNode(nodeById, e.toId, out LevelNode to))
                {
                    continue;
                }

                Vector2 fromPos = NormalizedToLocal(from.normalizedPosition, parent);
                Vector2 toPos = NormalizedToLocal(to.normalizedPosition, parent);
                CreateLine(edgesRoot, $"线_{from.id}_{to.id}", fromPos, toPos);
            }
        }

        private void DrawNodes(RectTransform parent, Dictionary<string, LevelNode> nodeById)
        {
            RectTransform nodesRoot = new GameObject("节点", typeof(RectTransform)).GetComponent<RectTransform>();
            nodesRoot.SetParent(parent, false);
            nodesRoot.anchorMin = Vector2.zero;
            nodesRoot.anchorMax = Vector2.one;
            nodesRoot.offsetMin = Vector2.zero;
            nodesRoot.offsetMax = Vector2.zero;

            foreach (KeyValuePair<string, LevelNode> kv in nodeById)
            {
                LevelNode node = kv.Value;

                Vector2 pos = NormalizedToLocal(node.normalizedPosition, parent);
                RectTransform nodeRt = CreateNode(nodesRoot, node, pos);

                RectTransform labelRt = CreateText(nodesRoot, $"标签_{node.id}", node.label, 14, labelColor, TextAnchor.MiddleCenter);
                labelRt.anchoredPosition = pos + new Vector2(0f, labelOffsetY);
                labelRt.sizeDelta = new Vector2(nodeSize * 1.6f, 28f);
                labelRt.SetAsLastSibling();

                // 不再用红色/标记显示“当前关卡”。
            }
        }

        private RectTransform CreateNode(RectTransform parent, LevelNode node, Vector2 anchoredPos)
        {
            GameObject go = new($"节点_{node.id}", typeof(RectTransform), typeof(Image), typeof(Button));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.sizeDelta = new Vector2(nodeSize, nodeSize);
            rt.anchoredPosition = anchoredPos;

            Image img = go.GetComponent<Image>();
            // Keep sprite empty to avoid any missing-asset references in the scene.
            // Unity Image can render a colored rect even when sprite is null.
            img.sprite = null;
            img.type = Image.Type.Simple;
            img.preserveAspect = false;

            Color c = node.unlocked ? nodeColor : lockedNodeColor;
            img.color = c;

            Button btn = go.GetComponent<Button>();
            btn.transition = Selectable.Transition.ColorTint;
            btn.interactable = node.unlocked && !string.IsNullOrWhiteSpace(node.sceneName);
            btn.onClick.RemoveAllListeners();
            string sceneName = node.sceneName != null ? node.sceneName.Trim() : "";
            if (!string.IsNullOrWhiteSpace(sceneName))
            {
                btn.onClick.AddListener(() => SceneManager.LoadScene(sceneName));
            }

            return rt;
        }

        private void CreatePlayerMarker(RectTransform nodeRt)
        {
            GameObject marker = new("当前位置", typeof(RectTransform), typeof(Image));
            RectTransform rt = marker.GetComponent<RectTransform>();
            rt.SetParent(nodeRt, false);
            rt.sizeDelta = new Vector2(nodeSize * 0.55f, nodeSize * 0.55f);
            rt.anchoredPosition = new Vector2(0f, -nodeSize * 0.05f);

            Image img = marker.GetComponent<Image>();
            img.sprite = _circleSprite;
            img.preserveAspect = true;
            img.color = new Color(0.95f, 0.55f, 0.55f, 1f);

            RectTransform textRt = CreateText(rt, "你", "你", 20, Color.white, TextAnchor.MiddleCenter);
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;
        }

        private void CreateLine(RectTransform parent, string name, Vector2 from, Vector2 to)
        {
            Vector2 delta = to - from;
            float length = delta.magnitude;
            if (length < 0.01f)
            {
                return;
            }

            GameObject go = new(name, typeof(RectTransform), typeof(Image));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(length, lineThickness);
            rt.anchoredPosition = (from + to) * 0.5f;

            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            rt.localRotation = Quaternion.Euler(0f, 0f, angle);

            Image img = go.GetComponent<Image>();
            img.sprite = _lineSprite;
            img.type = Image.Type.Simple;
            img.color = lineColor;
        }

        private RectTransform CreateText(RectTransform parent, string name, string text, int fontSize, Color color, TextAnchor alignment)
        {
            GameObject go = new(name, typeof(RectTransform), typeof(Text));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.sizeDelta = new Vector2(300f, 40f);

            Text uiText = go.GetComponent<Text>();
            uiText.text = text ?? "";
            uiText.font = _font;
            uiText.fontSize = fontSize;
            uiText.color = color;
            uiText.alignment = alignment;
            uiText.raycastTarget = false;

            return rt;
        }

        private static Vector2 NormalizedToLocal(Vector2 normalized, RectTransform root)
        {
            float x = Mathf.Lerp(0f, root.rect.width, Mathf.Clamp01(normalized.x));
            float y = Mathf.Lerp(0f, root.rect.height, Mathf.Clamp01(normalized.y));
            return new Vector2(x - root.rect.width * 0.5f, y - root.rect.height * 0.5f);
        }

        private static bool TryGetNode(Dictionary<string, LevelNode> nodeById, string id, out LevelNode node)
        {
            node = null;
            if (nodeById == null)
            {
                return false;
            }

            string key = id != null ? id.Trim() : "";
            if (string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            return nodeById.TryGetValue(key, out node) && node != null;
        }

        private static void DestroySmart(GameObject go)
        {
            if (go == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(go);
            }
            else
            {
#if UNITY_EDITOR
                // Avoid DestroyImmediate during restricted callbacks (e.g. OnValidate).
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    // Editor delayCall may run after entering Play Mode (especially when domain/scene reload is disabled).
                    // Never perform editor-time cleanup while playing, otherwise scene UI may "disappear a few frames later".
                    if (Application.isPlaying)
                    {
                        return;
                    }

                    if (go != null)
                    {
                        DestroyImmediate(go);
                    }
                };
#else
                DestroyImmediate(go);
#endif
            }
        }

        private static Sprite CreateSquareSprite()
        {
            Texture2D tex = Texture2D.whiteTexture;
            Rect rect = new(0, 0, tex.width, tex.height);
            return Sprite.Create(tex, rect, new Vector2(0.5f, 0.5f), 100f);
        }

        private static Sprite CreateCircleSprite(int size, float featherPixels)
        {
            size = Mathf.Clamp(size, 16, 512);
            featherPixels = Mathf.Clamp(featherPixels, 0f, size * 0.25f);

            Texture2D tex = new(size, size, TextureFormat.RGBA32, false, true)
            {
                name = "LevelSelectMapUI_Circle",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave,
            };

            float r = (size - 2f) * 0.5f;
            float feather = Mathf.Max(0.001f, featherPixels);
            Vector2 c = new((size - 1) * 0.5f, (size - 1) * 0.5f);

            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), c);
                    float a = 1f - Mathf.SmoothStep(r - feather, r, d);
                    a = Mathf.Clamp01(a);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, a);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply(false, true);

            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
