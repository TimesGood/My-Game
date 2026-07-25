# 物理模拟系统迁移指南

## 概述

新的物理模拟系统借鉴了 PixelAlchemy 的设计理念，提供了以下改进：

1. **材料驱动设计**：通过 MaterialPhysicsConfig 集中管理材料物理属性
2. **活跃区域优化**：只处理发生变化的区域，大幅提升性能
3. **系统分离**：液体和粉末模拟独立，便于维护和扩展
4. **密度驱动位移**：支持沙子沉入水中等真实物理效果

## 迁移步骤

### 第一步：创建材料物理配置

1. 在 Unity 编辑器中，右键点击 `Assets/Data/Physics/` 文件夹
2. 选择 `Create > Physics > Material Physics Config`
3. 配置你的材料物理属性：
   - **Water**（水）：Liquid 模式，密度 30，横向搜索距离 6
   - **Sand**（沙子）：Powder 模式，密度 70
   - **Gravel**（砾石）：Powder 模式，密度 80
   - **Lava**（岩浆）：Liquid 模式，密度 88，横向搜索距离 3

### 第二步：设置 PhysicsSimulationHandler

1. 在场景中创建一个空 GameObject，命名为 `PhysicsSimulationHandler`
2. 添加 `PhysicsSimulationHandler` 组件
3. 将创建的 MaterialPhysicsConfig 拖拽到 `Physics Config` 字段
4. 配置模拟参数：
   - **Simulation Seed**：随机种子（0 表示随机）
   - **Chunk Size**：区块大小（默认 16）
   - **Chunk Sleep Delay**：区块休眠延迟帧数（默认 3）
   - **Max Processed Cells Per Frame**：每帧最大处理格子数（默认 10000）

### 第三步：更新 LiquidClass

在 LiquidClass 中添加对新系统的支持：

```csharp
// 在 LiquidClass.cs 中添加
public void UpdateVolumeViaNewSystem(Vector2Int pos, float volume) {
    // 通过新的 PhysicsSimulationHandler 更新体积
    if (PhysicsSimulationHandler.Instance != null) {
        PhysicsSimulationHandler.Instance.MarkForUpdate(pos);
    }
}
```

### 第四步：禁用旧的 LiquidHandler

1. 找到场景中的 LiquidHandler 对象
2. 禁用或删除 LiquidHandler 组件
3. 或者将 `openFlow` 设置为 false 来禁用旧系统

## 新旧系统对比

| 功能 | 旧 LiquidHandler | 新 PhysicsSimulationHandler |
|------|------------------|----------------------------|
| 液体流动 | ✅ | ✅ |
| 沙砾下落 | ❌ | ✅ |
| 活跃区域优化 | ❌ | ✅ |
| 密度驱动位移 | ❌ | ✅ |
| 横向搜索 | ❌ | ✅ |
| 性能统计 | ❌ | ✅ |

## 配置示例

### 材料物理属性配置

```csharp
// 水的配置
new SimulationMaterialDefinition {
    movementMode = MaterialMovementMode.Liquid,
    density = 30,
    moveProbability = 1f,
    lateralProbability = 0.8f,
    horizontalSearchDistance = 6,
    canBeDisplaced = true,
    minVolume = 0.005f,
    maxVolume = 1f
}

// 沙子的配置
new SimulationMaterialDefinition {
    movementMode = MaterialMovementMode.Powder,
    density = 70,
    moveProbability = 1f,
    canBeDisplaced = false,
    minVolume = 0.005f,
    maxVolume = 1f
}
```

## 性能优化建议

1. **调整区块大小**：较大的区块可以减少区块切换开销，但会增加单个区块的处理时间
2. **调整休眠延迟**：增加休眠延迟可以减少频繁唤醒，但可能导致液体流动不连贯
3. **限制每帧处理量**：根据目标帧率调整 `Max Processed Cells Per Frame`
4. **使用统计信息**：启用统计信息监控性能瓶颈

## 故障排除

### 问题：液体不流动
- 检查 MaterialPhysicsConfig 中是否正确配置了材料
- 确认材料的 `movementMode` 设置为 `Liquid`
- 检查 `moveProbability` 是否大于 0

### 问题：沙子不沉入水中
- 确认沙子的密度大于水的密度
- 检查 `canBeDisplaced` 设置是否正确
- 确认 PowderSimulation 的回调已正确绑定

### 问题：性能问题
- 减小 `Max Processed Cells Per Frame`
- 增加 `Chunk Sleep Delay`
- 检查统计信息，确定瓶颈位置

## 扩展指南

### 添加新材料

1. 在 MaterialPhysicsConfig 中添加新的 MaterialDefinitionEntry
2. 配置材料的物理属性
3. 如果需要新的运动模式，在 MaterialMovementMode 中添加新枚举值

### 添加新的物理效果

1. 在 LiquidSimulation 或 PowderSimulation 中添加新的移动规则
2. 在 PhysicsSimulationHandler 中添加新的处理逻辑
3. 更新 SimulationGrid 的活跃区域标记逻辑

## 注意事项

1. **向后兼容**：新系统与旧的 LiquidClass 完全兼容，无需修改现有液体数据
2. **渐进迁移**：可以逐步迁移液体类型，不需要一次性迁移所有材料
3. **性能监控**：建议在开发阶段启用统计信息，监控性能表现
4. **测试充分**：在正式使用前，充分测试各种物理效果

## 技术支持

如有问题，请检查：
1. Console 窗口是否有错误信息
2. 统计信息是否正常显示
3. 材料物理配置是否正确
4. PhysicsSimulationHandler 是否正确初始化
