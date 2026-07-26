# 液体模拟方案对比指南

## 概述

本项目现在支持两种液体模拟方案，你可以在运行时切换来对比效果：

1. **Custom 模式**：自定义模式，带冷却时间的密度分层
2. **PixelAlchemy 模式**：基于粒子移动的理念

## 如何切换

### 在 Unity Editor 中切换

1. 选择 `PhysicsSimulationHandler` 对象
2. 在 Inspector 中找到 `Liquid Simulation Mode` 下拉菜单
3. 选择想要的模式

### 在代码中切换

```csharp
PhysicsSimulationHandler handler = PhysicsSimulationHandler.Instance;
handler.SetLiquidSimulationMode(LiquidSimulationMode.PixelAlchemy);
```

## 方案对比

### Custom 模式（自定义）

**设计理念**：
- 基于体积的液体系统
- 每个格子有液体体积参数（0~1+）
- 使用冷却时间解决冒泡问题
- 支持密度分层

**核心特点**：
- ✅ 带冷却时间的密度分层（防止冒泡）
- ✅ 基于体积的液体流动
- ✅ 横向扩散支持
- ✅ 流速控制

**适用场景**：
- 需要精确体积控制的游戏
- 需要稳定密度分层的场景
- 不同液体需要明显分层效果

**代码位置**：`LiquidSimulation.cs`

---

### PixelAlchemy 模式

**设计理念**：
- 每个格子是一个粒子
- 基于密度的位移系统
- 随机化方向避免偏向
- 横向搜索多个格子

**核心特点**：
- ✅ 密度驱动的位移（高密度下沉，低密度上浮）
- ✅ 横向搜索距离（液体可以跨过多个空格）
- ✅ 随机化方向（避免规则化痕迹）
- ✅ 帧更新标记（避免同一帧重复移动）

**适用场景**：
- 需要自然流动效果的游戏
- 需要粒子级物理模拟
- 需要避免规则化痕迹

**代码位置**：`LiquidSimulationPixelAlchemy.cs`

## 核心差异对比

| 特性 | Custom 模式 | PixelAlchemy 模式 |
|------|-------------|-------------------|
| 液体表示 | 基于体积（0~1+） | 基于粒子（每个格子是完整粒子） |
| 密度分层 | ✅ 带冷却时间 | ✅ 密度驱动位移 |
| 冒泡问题 | 通过冷却时间解决 | 通过帧标记避免 |
| 横向扩散 | 基于体积差 | 横向搜索多个格子 |
| 随机化 | 方向随机化 | 方向和扫描顺序随机化 |
| 性能 | 较好 | 较好 |

## 详细差异说明

### 1. 液体流动方式

**Custom 模式**：
```csharp
// 基于体积差流动
if (curVolume > targetVolume) {
    float avg = (curVolume + targetVolume) / 2;
    UpdateVolume(liquidId, pos, avg);
    UpdateVolume(liquidId, targetPos, avg);
}
```

**PixelAlchemy 模式**：
```csharp
// 基于粒子移动
if (targetDefinition.IsAir) {
    // 直接移动整个粒子
    grid.SetCell(toX, toY, source);
    grid.SetCell(fromX, fromY, Pixel.FromMaterial(MaterialType.Air));
}
```

### 2. 密度分层处理

**Custom 模式**：
```csharp
// 密度交换 + 冷却时间
if (materialDef.density > downMaterialDef.density) {
    UpdateVolume(liquidId, downPos, curVolume);
    UpdateVolume(downLiquid.blockId, pos, downVolume);
    SetDisplacedCooldown(pos); // 防止冒泡
}
```

**PixelAlchemy 模式**：
```csharp
// 密度驱动位移
if (CanDisplace(definition, targetDefinition, offsetY)) {
    // 交换两个粒子
    grid.SetCell(toX, toY, source);
    grid.SetCell(fromX, fromY, target);
}
```

### 3. 横向搜索

**Custom 模式**：
```csharp
// 检查相邻格子
Vector2Int leftDir = pos + Vector2Int.left;
Vector2Int rightDir = pos + Vector2Int.right;
if (CheckFlowDirection(leftDir, curVolume)) flowDirs.Add(leftDir);
if (CheckFlowDirection(rightDir, curVolume)) flowDirs.Add(rightDir);
```

**PixelAlchemy 模式**：
```csharp
// 搜索多个格子
int maxDistance = definition.HorizontalSearchDistance;
for (int distance = 1; distance <= maxDistance; distance++) {
    if (TryMove(grid, x, y, direction * distance, 0, definition)) {
        return true;
    }
}
```

## 测试建议

### 测试场景 1：密度分层

1. 创建一个容器
2. 在底部放置高密度液体（岩浆，密度88）
3. 在上面放置低密度液体（水，密度30）
4. 观察分层效果

**Custom 模式**：应该看到稳定的分层，没有冒泡
**PixelAlchemy 模式**：应该看到密度驱动的自然分层

### 测试场景 2：流动速度

1. 创建一个斜坡
2. 在顶部放置液体
3. 观察流动速度和形态

**Custom 模式**：基于体积的流动，速度可控
**PixelAlchemy 模式**：基于粒子的流动，更自然

### 测试场景 3：混合液体

1. 创建一个容器
2. 同时倒入多种液体（油、水、岩浆）
3. 观察分层过程

**Custom 模式**：带冷却时间的稳定分层
**PixelAlchemy 模式**：密度驱动的自然分层

## 性能对比

### 内存使用

**Custom 模式**：
- 使用 Dictionary 存储冷却时间
- 使用 Dictionary 存储更新时间

**PixelAlchemy 模式**：
- 使用 HashSet 存储帧标记
- 使用 Dictionary 存储下落帧数

### 计算开销

**Custom 模式**：
- 每帧需要检查冷却时间
- 需要计算体积差

**PixelAlchemy 模式**：
- 每帧需要清除帧标记
- 需要检查密度关系

## 选择建议

### 选择 Custom 模式如果你需要：
- 精确的体积控制
- 稳定的密度分层
- 可调节的流速
- 防止冒泡效果

### 选择 PixelAlchemy 模式如果你需要：
- 自然的流动效果
- 粒子级物理模拟
- 避免规则化痕迹
- 更真实的物理行为

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

## 总结

两种方案各有优势：

- **Custom 模式**更适合需要精确控制和稳定性的游戏
- **PixelAlchemy 模式**更适合需要自然物理效果的游戏

建议你在实际项目中测试两种模式，选择最适合你游戏需求的方案。如果需要，你也可以混合使用两种模式的优点，创建自己的定制方案。
