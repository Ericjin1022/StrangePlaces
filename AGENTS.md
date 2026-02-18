# Agent Rules (StrangePlaces)

## Unity 场景修改（强制）

- 任何对 Unity 场景/层级/Prefab 的改动（创建/移动/删除 GameObject、修改组件字段、摆放关卡物体、修改 UI 文本、保存场景等）**必须通过 Unity MCP（mcp-unity）完成**。
- **禁止**手工编辑 `.unity` / `.prefab` / `.asset` 的 YAML 来改场景内容（除非用户明确要求并确认 MCP 不可用）。
- 每次通过 MCP 改完：
  - 调用保存（例如 `save_scene`）
  - 拉取并处理 Console error（例如 `get_console_logs(logType="error")`）

## 文本规则（强制）

- 所有游戏内可见文本（UI、提示牌、结算等）必须为中文。

## 行尾格式（强制）

- 所有脚本文件（例如 `.cs`）行尾必须为 `CRLF`。
