# 解决液体冒泡问题指南

## 问题描述

当高密度液体和低密度液体混合时，会出现"冒泡"现象：
- 高密度液体下沉
- 低密度液体被抬升
- 低密度液体立即尝试向下流动
- 但下方被高密度液体占据，又被抬升
- 形成循环，看起来像冒泡

## 解决方案

我们添加了"置换冷却时间"机制来解决这个问题。

### 工作原理

1. **置换冷却**：当液体被其他液体置换（抬升）后，会进入一个短暂的冷却期
2. **冷却期内**：被抬升的液体不会尝试向下流动
3. **冷却期结束后**：液体恢复正常流动行为

### 代码实现

```csharp
// 置换冷却字典
private readonly Dictionary<Vector2Int, float> displacedCooldown = new Dictionary<Vector2Int, float>();
private const float DISPLACED_COOLDOWN_TIME = 0.3f; // 冷却时间（秒）

// 检查是否在冷却期内
private bool IsInDisplacedCooldown(Vector2Int pos) {
    if (displacedCooldown.TryGetValue(pos, out float cooldownEndTime)) {
        if (Time.time < cooldownEndTime) {
            return true; // 还在冷却期内
        }
        displacedCooldown.Remove(pos);
    }
    return false;
}

// 设置冷却
private void SetDisplacedCooldown(Vector2Int pos) {
    displacedCooldown[pos] = Time.time + DISPLACED_COOLDOWN_TIME;
}
```

### 使用方法

#### 1. 在 TryFlowDown 中添加冷却检查

```csharp
private bool TryFlowDown(int x, int y, ...) {
    var pos = new Vector2Int(x, y);

    // 检查是否在冷却期内
    if (IsInDisplacedCooldown(pos)) {
        return false; // 冷却期内不向下流动
    }

    // ... 其他逻辑

    // 密度交换时设置冷却
    if (materialDef.density > downMaterialDef.density) {
        UpdateVolume(liquidId, downPos, curVolume);
        UpdateVolume(downLiquid.blockId, pos, downVolume);

        // 设置被抬升液体的冷却时间
        SetDisplacedCooldown(pos);

        return true;
    }
}
```

#### 2. 在 TryDiffusion 中设置冷却

```csharp
private bool TryDiffusion(int x, int y, ...) {
    // 横向扩散时，如果抬升了其他液体，设置冷却
    if (materialDef.density > targetMaterialDef.density) {
        // 抬升目标液体
        UpdateVolume(targetLiquid.blockId, upDir, upVolume + targetVolume);
        SetDisplacedCooldown(upDir); // 设置冷却
    }
}
```

## 配置参数

### 冷却时间

在 `LiquidSimulation.cs` 中调整冷却时间：

```csharp
private const float DISPLACED_COOLDOWN_TIME = 0.3f; // 0.3秒
```

- **较短时间（0.1-0.2秒）**：液体更快恢复流动，但可能仍有轻微冒泡
- **较长时间（0.4-0.5秒）**：更稳定，但液体响应变慢
- **推荐值**：0.3秒（平衡稳定性和响应性）

## 测试方法

### 1. 创建测试场景

1. 创建一个容器（用固体方块围起来）
2. 在底部放置高密度液体（如岩浆，密度88）
3. 在上面放置低密度液体（如水，密度30）
4. 观察是否还有冒泡现象

### 2. 使用调试器

添加 `LiquidSimulationDebugger` 组件：
- 按 **D** 键切换调试面板
- 点击"打印鼠标位置液体信息"查看液体状态
- 观察液体流动是否平稳

### 3. 调整参数

如果仍有冒泡：
1. 增加 `DISPLACED_COOLDOWN_TIME`（如改为0.5秒）
2. 减小 `flowSpeed`（如改为5）
3. 增大密度差异（如将油设为5，岩浆设为100）

## 常见问题

### Q: 冷却期内液体不流动？

A: 这是正常的。冷却期只阻止向下流动，横向扩散仍然允许。如果完全不流动，检查：
- `flowSpeed` 是否大于0
- `moveProbability` 是否大于0
- 是否在冷却期内

### Q: 冷却时间太长/太短？

A: 调整 `DISPLACED_COOLDOWN_TIME`：
- 太长：液体响应变慢
- 太短：可能仍有冒泡
- 推荐：0.2-0.4秒

### Q: 不同液体不交换位置？

A: 检查：
- 两种液体的 `density` 值是否不同
- `canBeDisplaced` 是否设置为 `true`
- 两种液体是否都在 `MaterialPhysicsConfig` 中配置

## 最佳实践

### 1. 密度配置

确保密度差异足够大：
```csharp
// 油 - 低密度
density = 10

// 水 - 中等密度
density = 30

// 岩浆 - 高密度
density = 88
```

### 2. 流速配置

适当降低流速可以减少冒泡：
```csharp
// 水
flowSpeed = 8  // 而不是10

// 岩浆
flowSpeed = 3  // 保持较慢
```

### 3. 容器设计

- 使用较深的容器
- 避免太窄的空间
- 给液体足够的扩散空间

## 调试技巧

### 1. 查看冷却状态

在代码中添加调试输出：
```csharp
if (IsInDisplacedCooldown(pos)) {
    Debug.Log($"位置 {pos} 在冷却期内");
}
```

### 2. 监控液体交换

在密度交换时添加日志：
```csharp
Debug.Log($"液体交换: {liquidId} 下沉到 {downPos}, {downLiquid.blockId} 上浮到 {pos}");
```

### 3. 可视化冷却区域

在 Scene 视图中绘制冷却区域（需要在编辑器脚本中实现）。

## 总结

通过添加置换冷却时间，我们解决了液体冒泡问题：
- ✅ 高密度液体正常下沉
- ✅ 低密度液体被抬升后不会立即下沉
- ✅ 液体分层更加稳定
- ✅ 保留了横向扩散能力

如果仍有问题，可以调整冷却时间和流速参数来找到最适合你游戏的配置。
