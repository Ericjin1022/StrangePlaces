# StrangePlaces：量子观测坍缩 Demo（当前实现总结）

本文档用于总结目前在本仓库中已经落地的“量子观测坍缩”最小可玩 Demo：核心机制、关卡结构、关键脚本与已修复问题，方便后续继续迭代。

## 1. Demo 入口与资产

- 场景入口：`Assets/Scenes/QuantumCollapseDemo.unity`
- 目标：按下 Play 立即可玩（无“生成器/构建场景”依赖）
- 文本规则（强制）：**游戏内所有可见文字必须为中文**（牌子、提示、结算等）

## 2. 玩家操作与交互

- 移动：`A/D` 或方向键左右
- 跳跃：`Space`
- 观测方向：鼠标指向（以鼠标位置作为瞄准方向）
- 手电筒光锥：默认常开；**是否坍缩以“锥形光区域 + 视线遮挡”判定为准**

## 3. 核心机制概览

### 3.1 观测坍缩（量子桥/踏板）

- 未被观测：平台处于“量子态”（位置在候选点之间切换并轻微抖动；碰撞体为 Trigger，不可站立）
- 被观测：平台“坍缩为稳定态”（固定到某个候选点；碰撞体变为非 Trigger，可站立）
- 离开观测：平台在短暂宽限期后回到量子态（防止边缘抖动导致频繁闪烁）

### 3.2 视线遮挡（Line of Sight）

- 光锥判定支持遮挡：若玩家到目标的射线最先命中的是遮挡物，则目标不算被观测
- 目标判定为“只要目标任意部分进入锥形区域即可”（对长平台更友好）

### 3.3 “别看门”（量子门）

- 门在“被观测/不被观测”之间切换其阻挡状态，用于构建“转身/背光”解谜

### 3.4 量子纠缠（观测信标）

为避免“低头直走就能过”，纠缠段改为“看信标而非看桥”：

- 纠缠组通过 `entanglementKey` 绑定
- **只有带 `QuantumEntanglementBeacon` 的对象**（高处信标）被观测时，才会让同组桥进入稳定态
- 纠缠桥可禁用“直接观测坍缩”（即使光锥照到桥本体也不会稳定），必须依赖信标触发纠缠

## 4. 当前关卡结构（从左到右）

场景采用单条主线，依次展示并组合机制：

1. 起点与基础跳跃
2. “别看门”教学段（背光/转身通过）
3. 接力虚桥段（两段虚桥 + 墙体遮挡，提示玩家换角度观测）
4. 纠缠段（高处信标 + 纠缠桥，要求抬头维持观测，避免低头通关）
5. 终点（Goal）

牌子（`DemoSign`）已用于在场景内固定提示，并使用偏“克苏鲁风”语气表达核心意思。

## 5. 关键脚本与职责

- `Assets/Scripts/Common/PlayerController2D.cs`
  - 2D 移动/跳跃/重生
  - 已修复：地面检测不再命中自身碰撞体导致“无限连跳”

- `Assets/Scripts/Common/ObserverCone2D.cs`
  - 光锥观测判定（角度/距离/遮挡）
  - 支持宽限期（`observationGraceSeconds`）减少闪烁
  - 已修复：宽限期时间戳刷新导致“被照过就永远稳定”的逻辑错误
  - 纠缠：仅信标触发纠缠组稳定

- `Assets/Scripts/Levels/QuantumCollapseDemo/QuantumCollapseObservable.cs`
  - 量子平台：候选位置切换、观测坍缩、碰撞体 Trigger/非 Trigger 切换
  - 新增：`allowDirectObservation`（可让某些平台“不能被直接观测坍缩”，只能通过纠缠等外部机制稳定）
  - 新增：支持纠缠强制稳定（`IEntanglementReceiver`）

- `Assets/Scripts/Levels/QuantumCollapseDemo/QuantumDoorObservable.cs`
  - 量子门：观测/不观测下的开关行为

- `Assets/Scripts/Levels/QuantumCollapseDemo/QuantumEntanglementMember.cs`
  - 纠缠分组键 `entanglementKey`（同 key 的对象视为同组）

- `Assets/Scripts/Levels/QuantumCollapseDemo/QuantumEntanglementBeacon.cs`
  - 信标标记组件：仅带该标记的对象可触发纠缠组稳定

- `Assets/Scripts/Levels/QuantumCollapseDemo/DemoSign.cs`
  - 场景内牌子生成与文本显示（固定中文提示）
  - 已处理：避免在不合适的生命周期使用 `DestroyImmediate` 引发停止时警告

- `Assets/Scripts/Common/FlashlightConeVisual2D.cs`
  - 手电筒光锥 Mesh 生成与朝向驱动
  - Build 兼容：优先从 `Resources` 加载材质模板，避免 shader stripping 导致打包后不可见

## 6. 资源与渲染（手电筒光）

问题表现：编辑器 Play 可见，但 Build 后光锥不可见。

处理方式：

- 新增材质模板：`Assets/Resources/FlashlightCone2D_Mat.mat`
- `FlashlightConeVisual2D` 在运行时 `Resources.Load` 材质并克隆实例，确保 Build 时 shader/材质被打包

## 7. 已修复问题清单（近期）

- `GUI` 调用时机错误（只能在 `OnGUI` 调用 GUI API）
- 观测判定边缘抖动导致平台频繁在稳定/不稳定之间切换（增加宽限期/更宽松的“进入锥形即算观测”）
- 宽限期实现错误导致“照过一次永远稳定”（已修复为：仅直接观测刷新时间戳）
- 停止游戏时 `DestroyImmediate` 警告（已规避）
- Build 后手电筒光不显示（引入 `Resources` 材质模板避免剥离）
- 角色无限连跳（地面检测过滤自身 Collider 与 Trigger）

## 8. 约定与后续建议

- 场景改动：优先使用 Unity MCP 进行场景编辑（避免手改 `.unity` YAML 引发冲突/不可预期）
- 后续可做的增强：
  - 更明确的失败反馈（例如掉落时的中文提示/音效/短暂慢动作）
  - 增加“遮挡窗口/窥视缝隙”让抬头观测更有路线规划感
  - 将 `docs/plan.md` 中的文本编码问题统一为 UTF-8（当前出现乱码）
