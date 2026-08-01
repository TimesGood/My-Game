# V2 液体模拟使用指南

## 概述

V2 模式是专为解决液体混合时的抽搐问题而设计的液体模拟方案。它采用渐进式密度交换和体积守恒原则，提供最稳定、最真实的液体物理效果。

## 核心特性

### 1. 渐进式密度交换

**问题**：传统方案在密度交换时会突然切换位置，导致视觉抽搐。

**解决方案**：V2 模式使用渐进式交换，根据密度差计算交换量：

```csharp
float swapFactor = Mathf.Clamp01(Mathf.Abs(densityDiff) / 50f);
float maxSwapVolume = Mathf.Min(curVolume, downVolume) * swapFactor * STABILITY_FACTOR;
```

- 密度差越大，交换越快
- 密度差越小，交换越慢（甚至混合）
- 避免突然的位置变化

### 2. 体积守恒原则

**原则**：液体流动时，总体积保持不变。

**实现**：
```csharp
// 流动前
totalVolume = curVolume + targetVolume;

// 流动后
newCurVolume + newTargetVolume = totalVolume;
```

**效果**：液体不会凭空消失或出现。

### 3. 密度加权混合

**场景**：当两种液体密度差很小时，进行混合而不是交换。

**实现**：
```csharp
// 按体积加权计算平均密度
float avgDensity = (materialDef.density * curVolume + targetMaterialDef.density * targetVolume) / totalVolume;

// 混合后分配体积
float newCurVolume = totalVolume * 0.5f;
float newTargetVolume = totalVolume * 0.5f;
```

**效果**：密度相近的液体会自然混合。

### 4. 混合阈值控制

**配置**：`MIXING_THRESHOLD = 0.1f`

**作用**：
- 密度差 < 0.1：进行混合
- 密度差 >= 0.1：进行渐进交换

**调整建议**：
- 增大阈值：更多液体混合
- 减小阈值：更多液体分层

## 使用方法

### 1. 启用 V2 模式

#### 在 Unity Editor 中

1. 选择 `PhysicsSimulationHandler` 对象
2. 在 Inspector 中找到 `Liquid Simulation Mode`
3. 选择 `V2`

#### 在代码中

```csharp
PhysicsSimulationHandler handler = PhysicsSimulationHandler.Instance;
handler.SetLiquidSimulationMode(LiquidSimulationMode.V2);
```

#### 运行时切换

按 **M** 键循环切换模式。

### 2. 配置参数

#### 混合阈值

在 `LiquidSimulationV2.cs` 中调整：

```csharp
private const float MIXING_THRESHOLD = 0.1f; // 混合阈值
```

- **0.05**：更严格的分层，只有密度差很小才混合
- **0.1**：默认值，平衡混合和分层
- **0.2**：更多混合，密度差较大也会混合

#### 稳定系数

```csharp
private const float STABILITY_FACTOR = 0.8f; // 稳定系数
```

- **0.5**：更稳定的交换，但速度较慢
- **0.8**：默认值，平衡稳定性和速度
- **1.0**：更快的交换，但可能不稳定

#### 最小流动体积

```csharp
private const float MIN_FLOW_VOLUME = 0.01f; // 最小流动体积
```

- **0.005**：更精细的流动控制
- **0.01**：默认值
- **0.02**：更粗糙的流动，性能更好

### 3. 材料密度配置

在 `MaterialPhysicsConfig` 中设置不同液体的密度：

```csharp
// 油 - 低密度
density = 10

// 水 - 中等密度
density = 30

// 毒液 - 较高密度
density = 50

// 岩浆 - 高密度
density = 88
```

**建议**：
- 密度差 >= 20：明显的分层效果
- 密度差 < 10：可能混合

## 测试场景

### 测试 1：密度分层

**目标**：验证不同密度液体的分层效果

**步骤**：
1. 创建一个容器（用固体方块围起来）
2. 在底部放置高密度液体（岩浆，密度88）
3. 在上面放置低密度液体（水，密度30）
4. 观察分层过程

**预期结果**：
- 岩浆沉到底部
- 水浮在上面
- 分层稳定，无抽搐

### 测试 2：液体混合

**目标**：验证密度相近液体的混合效果

**步骤**：
1. 创建一个容器
2. 在一侧放置水（密度30）
3. 在另一侧放置毒液（密度35）
4. 观察混合过程

**预期结果**：
- 两种液体逐渐混合
- 混合区域平滑过渡
- 无突然的位置变化

### 测试 3：流动稳定性

**目标**：验证流动过程中的稳定性

**步骤**：
1. 创建一个斜坡
2. 在顶部放置液体
3. 观察流动过程

**预期结果**：
- 液体平稳流动
- 无抽搐或闪烁
- 流动自然

## 调试技巧

### 1. 查看当前模式

```csharp
Debug.Log("当前模式: " + PhysicsSimulationHandler.Instance.GetLiquidSimulationMode());
```

### 2. 监控性能

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

### 3. 查看液体信息

使用 `LiquidSimulationDebugger` 组件：
1. 添加组件到场景
2. 按 **D** 键切换调试面板
3. 点击"打印鼠标位置液体信息"

### 4. 调整参数

如果效果不理想，可以调整：

**问题**：液体混合太快
- 减小 `MIXING_THRESHOLD`（如改为 0.05）
- 增大密度差异

**问题**：分层不明显
- 增大密度差异
- 减小 `MIXING_THRESHOLD`

**问题**：流动太慢
- 增大 `STABILITY_FACTOR`（如改为 1.0）
- 增大 `flowSpeed`

**问题**：仍有轻微抽搐
- 减小 `STABILITY_FACTOR`（如改为 0.6）
- 减小 `MIN_FLOW_VOLUME`

## 与其他模式的对比

| 特性 | V2 模式 | Custom 模式 | PixelAlchemy 模式 |
|------|---------|-------------|-------------------|
| 混合稳定性 | ✅ 优秀 | ❌ 有抽搐 | ❌ 有抽搐 |
| 密度分层 | ✅ 平滑 | ✅ 稳定 | ✅ 自然 |
| 体积守恒 | ✅ | ✅ | ✅ |
| 视觉效果 | ✅ 最佳 | 中等 | 中等 |
| 性能 | 较好 | 较好 | 较好 |
| 复杂度 | 中等 | 低 | 中等 |

## 最佳实践

### 1. 密度配置

- 密度差异要足够大（建议 >= 20）
- 避免密度过于接近的液体（容易混合）
- 使用整数值便于管理

### 2. 流速配置

- 根据游戏节奏调整 `flowSpeed`
- 较慢的流速更稳定
- 较快的流速更真实

### 3. 容器设计

- 使用足够深的容器
- 避免太窄的空间
- 给液体足够的扩散空间

### 4. 测试流程

1. 先测试单一液体流动
2. 再测试两种液体混合
3. 最后测试多种液体共存

## 常见问题

### Q: 液体混合后不分离？

A: 这是正常现象。V2 模式使用密度加权混合，混合后的液体会保持混合状态。如果需要分离，需要增大密度差异或减小 `MIXING_THRESHOLD`。

### Q: 流动速度太慢？

A: 检查以下设置：
- `flowSpeed` 是否足够大
- `STABILITY_FACTOR` 是否足够大
- `GlobalSpeedMultiplier` 是否为 1.0

### Q: 仍有轻微抽搐？

A: 尝试：
- 减小 `STABILITY_FACTOR`（如 0.6）
- 减小 `MIN_FLOW_VOLUME`（如 0.005）
- 增大密度差异

### Q: 性能下降？

A: V2 模式比其他模式稍慢，如果性能问题：
- 减小 `maxProcessedCellsPerFrame`
- 增大 `flowSpeed`（减少更新频率）
- 使用更简单的容器形状

## 总结

V2 模式是最完善的液体模拟方案，特别适合：
- 需要稳定混合效果的游戏
- 需要平滑密度分层的场景
- 需要避免视觉抽搐的项目

通过合理配置参数，你可以获得最真实、最稳定的液体物理效果。
