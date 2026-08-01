# 液体模拟方案对比指南

## 概述

本项目支持三种液体模拟方案，可在运行时切换对比效果：

1. **Custom 模式**：自定义模式，带冷却时间的密度分层（旧方案）
2. **PixelAlchemy 模式**：基于粒子移动的理念
3. **Terraria 模式（推荐）**：泰拉瑞亚风格，下落/平整沉降/密度分层/无冒泡

## 如何切换

### 在 Unity Editor 中切换

1. 选择 `PhysicsSimulationHandler` 对象
2. 在 Inspector 中找到 `Liquid Simulation Mode` 下拉菜单
3. 选择 `Terraria`（默认）

### 在代码中切换

```csharp
PhysicsSimulationHandler handler = PhysicsSimulationHandler.Instance;
handler.SetLiquidSimulationMode(LiquidSimulationMode.Terraria); // 推荐
```

### 运行时切换

添加 `LiquidSimulationModeSwitcher` 组件，按 **M** 键循环切换：
Custom -> PixelAlchemy -> Terraria -> Custom

## 方案对比

### Terraria 模式（推荐，默认）

**设计理念**：复刻《泰拉瑞亚》的瓦片液体行为——单格容量封顶、下落优先、半差分横向均衡成平整水面、密度分层。

**核心特点**：
- ✅ 水面平整沉降（半差分均衡，无抖动收敛）
- ✅ 无向上溢出（体积 >1 时向两侧扩散，消除冒泡/液面升天）
- ✅ 密度分层（重液下沉、轻液上浮，交换规则不对称不回弹）
- ✅ 斜向流动（斜坡滑落 + 悬崖边缘瀑布）
- ✅ 确定性遍历顺序（按 Y 底→顶排序，流动方向稳定）
- ✅ 体积守恒（转移按精确量增减）

**关键规则**（`LiquidSimulationTerraria.cs`）：
- 单格更新顺序：清理 → 下落 → 斜向流动 → 上浮 → 横向均衡
- `MaxVerticalFlowRate` 控制下落速度；`SpreadFactor` 控制平整速度
- `SettleTolerance` 控制水面平整度；`DensitySwapMinDiff` 控制分层灵敏度

**适用场景**：
- 需要泰拉瑞亚式液体的游戏
- 需要水面平整、无抽搐的稳定表现
- 需要不同液体明显分层

**代码位置**：`LiquidSimulationTerraria.cs`

---

### Custom 模式（旧方案）

**设计理念**：基于体积的液体系统，每个格子有体积参数（0~1+），使用冷却时间解决冒泡问题，支持密度分层。

**已确认的问题**：
- ❌ 无确定性遍历顺序 → 流动方向随机、冒泡抽搐
- ❌ `TryOverflow` 把 >1 的体积向上推 → 液面升天
- ❌ 异种液体下落/扩散会覆盖对方 ID → 密度分层失效

**代码位置**：`LiquidSimulationTest.cs`（`LiquidSimulation.cs` 为更早的版本）

---

### PixelAlchemy 模式

**设计理念**：每个格子是一个粒子，基于密度的位移系统，随机化方向避免偏向，横向搜索多个格子。

**核心特点**：
- ✅ 密度驱动的位移（高密度下沉，低密度上浮）
- ✅ 横向搜索距离（液体可以跨过多个空格）
- ✅ 随机化方向（避免规则化痕迹）
- ✅ 帧更新标记（避免同一帧重复移动）

**代码位置**：`LiquidSimulationPixelAlchemy.cs`

## 核心差异对比

| 特性 | Terraria（推荐） | Custom | PixelAlchemy |
|------|-----------------|--------|--------------|
| 水面平整 | ✅ 半差分均衡 | ❌ 沉降不平整 | 中等 |
| 冒泡/液面升天 | ✅ 无 | ❌ 有 | 中等 |
| 密度分层 | ✅ 不对称交换 | ❌ 覆盖 ID | ✅ 密度位移 |
| 确定性顺序 | ✅ Y 升序 | ❌ 无序 | 随机 |
| 体积守恒 | ✅ | ✅ | ✅ |
| 视觉稳定性 | ✅ 高 | 低 | 中等 |
| 性能 | 较好 | 较好 | 较好 |

## 调试技巧

### 查看当前模式

```csharp
Debug.Log("当前模式: " + PhysicsSimulationHandler.Instance.GetLiquidSimulationMode());
```

### 运行时切换模式

```csharp
if (Input.GetKeyDown(KeyCode.M)) {
    var handler = PhysicsSimulationHandler.Instance;
    var currentMode = handler.GetLiquidSimulationMode();
    var newMode = currentMode == LiquidSimulationMode.Custom
        ? LiquidSimulationMode.PixelAlchemy
        : currentMode == LiquidSimulationMode.PixelAlchemy
            ? LiquidSimulationMode.Terraria
            : LiquidSimulationMode.Custom;
    handler.SetLiquidSimulationMode(newMode);
}
```

### 监控性能

```csharp
PhysicsSimulationHandler.Instance.GetStats(
    out float fps,
    out float simTime,
    out int processed,
    out int active,
    out int chunks
);
Debug.Log($"FPS: {fps}, 模拟时间: {simTime}ms");
```

水面静止数帧后 `活跃格子`/`活跃区块` 应下降（区块休眠），说明模拟正确休眠。

## 泰拉瑞亚模式参数调优

在 `PhysicsSimulationHandler` Inspector 的 `泰拉瑞亚液体参数` 分组调整：

| 参数 | 作用 | 调大 | 调小 |
|------|------|------|------|
| `maxVerticalFlowRate` | 下落/斜落速度 | 瀑布更急 | 瀑布更缓 |
| `spreadFactor` | 横向平整速度 | 水面平整更快 | 更缓 |
| `settleTolerance` | 水面平整度 | 更易停止 | 更平整但更耗算 |
| `densitySwapMinDiff` | 分层灵敏度 | 需要更大密度差才分层 | 更容易分层 |

## 常见问题

### Q: 液体混合后不分离？

A: 检查两种液体的 `density` 差值是否 ≥ `DensitySwapMinDiff`。`Assets/Data/Physics/Water.asset` 中 Water=30、Magma=88，差值足够。

### Q: 下落太快/太慢？

A: 调整 `PhysicsSimulationHandler` 的 `maxVerticalFlowRate` 和 `globalSpeedMultiplier`。

### Q: 水面不平整？

A: 减小 `settleTolerance`，或确认 `PhysicsSimulationHandler` 中 `SimulationStep` 已启用 Y 升序遍历（Terraria 模式依赖确定性顺序）。

### Q: 挖洞后水不流入？

A: 确认 `ConstructionLayer.Destory` 调用了 `PhysicsSimulationHandler.Instance.MarkAreaForUpdate(worldCoords, 2)`（已修复，不再依赖已停用的 `LiquidHandler`）。
