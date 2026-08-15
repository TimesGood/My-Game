# AGENTS.md

此文件为所有智能体在处理此代码库中的代码时提供指导。

## 项目概述

一款使用 **Unity 2022.3.58f1c1** 构建的 2D 瓦片沙盒/生存游戏（类似泰拉瑞亚）。具有程序化生成的世界，包含采矿、建造、液体物理、动态 2D 光照、生物群落区域、植物生长以及横版卷轴玩家角色。

## 注意事项

作者为Java开发出身，并未有过Unity游戏的开发经验，本项目为作者初次尝试开发 2D 沙盒生存建造类游戏，对游戏开发的并不熟悉，有些架构设计可能并不合理，可能会经常出现重构的情况，
如果市面上有相对成熟的技术契合作者的想法， 请及时给予作者建议，合作进行改进。
如果涉及到重构，请维护一下本文档相关内容。
在与开发者沟通交流时，请使用中文进行


## 常用命令

- **打开项目**：在 Unity Editor 2022.3.58f1c1 中打开（唯一场景为 `Assets/Scenes/SampleScene.unity`）
- **生成世界（旧）**：在编辑器中，右键点击 `MapGenerator` 组件 → `GenerateWorld`
- **生成世界（新）**：在编辑器中，右键点击 `NewMapGenerator` 组件 → `GenerateWorld (New)`
- 此仓库中未配置 CLI 构建、代码检查或测试命令，也没有 CI/CD 流水线。

## 核心架构

### 世界数据模型（系统核心）

世界大小为 **6000 × 2000 瓦片**，划分为 **20 × 20 个区块**（每个区块 300 × 100 瓦片）。最近的重构将所有瓦片数据统一为以下设计：

- `TileData` 结构体 — 每个格子一个，持有全部四个图层的 ID：`groundId`、`wallId`、`liquidId`（+ `liquidVolume`）、`addonId`（+ `growthData`）
- `Chunk` — 封装单个区块的 `TileData[,]` 数组
- `ChunkManager`（单例） — 所有 `Chunk[]` 数据的权威持有者。通过它而非直接通过 Tilemap 来查询/修改瓦片数据
- `ChunkHandler`（单例） — 基于相机的区块渲染器：为可见区块创建/销毁 Unity Tilemap GameObject，带有可配置加载半径的 LRU 缓存

四个瓦片图层定义在 `Utilities/Enums.cs` 中：
```
Layers { Addons, Background, Ground, Liquid }
```

### 启动与生成流程

1. `WorldManager.Awake()` 通过 `WorldSetting` 静态类设置种子和世界配置
2. `WorldManager` 带有 `[RuntimeInitializeOnLoadMethod]`，调用 `TileRegistry.Initialize()` — 将 `Assets/Data/Tiles/` 中的所有 `TileClass` ScriptableObject 加载到双向字典中（blockId ↔ TileClass）
3. `MapGenerator`（单例）通过协程协调生成：
   - Compute Shader 生成噪声纹理（Perlin、Worley、FBM 变体）
   - `BaseTerrain` 生成地面图层（泥土/石头/草地）及洞穴噪声
   - `MapGridLayout` 通过泊松盘采样放置生物群落
   - 每个 `SurfaceBiome`/`DeepBiome` 在其区域内生成各自的地形、树木、矿石
4. `ChunkHandler` 每帧渲染相机范围内的区块
5. `MapSaveManager` + `TilemapExporter` 保存到 `StreamingAssets/tilemap_data.bin`（MessagePack + GZip，格式版本 2）

### 地形生成架构（`Assets/Scripts/Terrains/New/`）—— "先分配、后生成"

**核心理念**：将生成流程分为三个阶段：分配（Distribute）→ 生成（Generate）→ 后处理（PostProcess）。

**新生成流程**（`WorldGenerator.Generate()`）：
1. Phase 0 — 初始化 `BiomeGeneratorRegistry`（Id → BiomeGeneratorBase）
2. Phase 1 — **分配**：遍历 `DistributorBase[]`，每个分配器产出 `List<BiomeInstance>`（矩形区域 + 群落定义）
3. Phase 2 — **生成**：遍历每个 `BiomeInstance`，通过 `BiomeDefinition.GeneratorId` 找到对应 `BiomeGeneratorBase`，调用 `Generate(ctx, instance)`
4. Phase 3 — **后处理**：按 Order 执行 `PostProcessorBase[]`

**核心类**：

| 类 | 类型 | 职责 |
|---|---|---|
| `WorldGenerator` | MonoBehaviour | 总控：持有配置、分配器、生成器、后处理器 |
| `MapConfig` | SO | 全局地图配置（宽、高、地表Y、种子） |
| `GenerationContext` | class | 运行时共享上下文（Config、ChunkManager、已分配实例） |
| `BiomeDefinition` | SO | 群落定义（名称、大小、适合度范围 + 内联 Feature 列表） |
| `BiomeInstance` | class | 运行时群落实例（Def 引用、Bounds 矩形、Seed） |
| `DistributorBase` | SO 抽象 | 分配器基类（Priority、Distribute()） |
| `PoissonDiscDistributor` | SO | 泊松盘分配实现 |
| `PostProcessorBase` | SO 抽象 | 后处理器基类 |

**Feature 系统**（`New/Features/`，通过 `BiomeDefinition` 内联使用）：

| Feature | 职责 |
|---|---|
| `TerrainFeature` | 地形填充（曲线高度 + 瓦片映射） |
| `CaveFeature` | 洞穴掏空（噪声驱动） |
| `OreFeature` | 矿石散布 |
| `TreeFeature` | 树木/植物放置（地表/洞穴） |
| `VineFeature` | 洞穴藤蔓 |
| `BorderBlendFeature` | 边界混合（高度渐变/曲线混合） |
| `StructureFeature` | 预制结构放置 |
| `NestedBiomeFeature` | 嵌套子群落 |

**与旧架构的区别**：
- 旧：`BaseBiome` 子类各自实现 `GenerateBiome()`，代码大量重复
- 新：`BiomeDefinition`（是什么 + 怎么生成）内联 Feature 列表，无代码重复
- Feature 是 `[Serializable]` 内联类，数据存储在 `BiomeDefinition` 的 `.asset` 中，不需要独立文件

**关键概念**：
- `BiomeCandidate`：规划器的输入，包含群落类型 + 适合度配置 + 期望数量 + 选择权重
- `BiomeSlot`：规划器的输出，表示一个矩形区域已分配给某群落
- `SuitabilityCondition`：单个评估因子（`AbsoluteHeight`/`RelativeToSurface`/`SurfaceExposure`/`NoiseMatch`），带权重和参数
- `BiomeSuitability`：组合多个条件的 ScriptableObject，挂在群落上使用

**与旧架构的区别**：
- 旧：随机撒点（泊松圆盘）→ 逐个群落就地生成（群落彼此独立，可能冲突）
- 新：基础地形 → 分析地形适合度 → 规划所有群落位置 → 统一生成（无重叠、位置有依据）

### 单例模式

核心系统通过 `Tools/Singleton.cs` 实现单例（提供 `StaticInstance<T>` → `Singleton<T>` → `PersistentSingleton<T>`）。以下单例驱动着游戏运行：

| 单例 | 职责 |
|---|---|
| `WorldManager` | 中心枢纽：瓦片注册表、世界配置、地形曲线 |
| `ChunkManager` | 权威瓦片数据存储 |
| `ChunkHandler` | Tilemap 渲染、区块加载/卸载 |
| `MapGenerator` | 世界生成协调 |
| `MapSaveManager` | 存档/读档协调 |
| `PhysicsSimulationHandler` | 液体物理主循环（默认 Terraria 模式，见下） |
| `LiquidHandler` | **已停用**（旧协程液体模拟，场景中 `m_Enabled: 0`，保留仅供回滚） |
| `LightHandler` | 2D 光照更新队列 |
| `GrowthHandler` | 植物生长计时更新循环 |

### 液体物理模拟（`Assets/Scripts/World/Simulation/`）

液体数据（`liquidId` + `liquidVolume` 0~1）存于 `ChunkManager` 的 `TileData`，物理由 `PhysicsSimulationHandler`（单例）每帧驱动，有 3 种可切换模式：

| 模式 | 类 | 说明 |
|---|---|---|
| `Terraria`（默认，推荐） | `LiquidSimulationTerraria.cs` | 泰拉瑞亚风格：下落→斜向流动→上浮→横向均衡。单格容量封顶、无向上溢出；半差分均衡成平整水面；密度分层用不对称交换不回弹。**依赖确定性遍历顺序**（Handler 按 Y 底→顶排序活跃格子） |
| `Custom` | `LiquidSimulationTest.cs` | 旧方案，已知问题：无确定性顺序、向上溢出、异种液体覆盖 ID |
| `PixelAlchemy` | `LiquidSimulationPixelAlchemy.cs` | 粒子式位移方案 |

**关键约定**：
- 活跃格子由 `SimulationGrid` 追踪（3×3 唤醒 + 区块休眠），水面静止数帧后自动休眠
- 每帧预算（`maxProcessedCellsPerFrame`）分配策略：**屏幕内瓦片优先**（可视范围 + `screenPriorityPadding` 外扩，相机懒加载自 `ChunkHandler.renderCamera`，回退 `Camera.main`），剩余预算再处理屏幕外瓦片；预算外的活跃格子通过 `SimulationGrid.KeepActive` **顺延到下一帧而非抛弃**，避免本该模拟的瓦片静止
- **空闲加速**：上一帧模拟耗时（平滑值）低于 `idleTimeThresholdMs` 时，预算放大 `idleBoostMultiplier` 倍，加速追赶屏幕外积压；屏幕内外两遍遍历均按 Y 底→顶保持确定性顺序
- 玩家放置液体走 `LiquidLayer.Build` → `PhysicsSimulationHandler.MarkForUpdate`；挖方块走 `ConstructionLayer.Destory` → `MarkAreaForUpdate`（都不再依赖已停用的 `LiquidHandler`）
- 液体物理配置在 `Assets/Data/Physics/Water.asset`（Water 密度 30、Magma 密度 88）

### ScriptableObject 驱动的配置

几乎所有游戏数据都是 `Assets/Data/` 下的 ScriptableObject：
- **瓦片**：`TileClass` 基类（blockId、图层、瓦片引用、光照发射、掉落物），子类包括 `AddonClass`（生长阶段）、`LiquidClass`（流动物理）、`TreeClass`（基于网格的生成）、`OreClass_New`
- **噪声**：噪声工具 → `NoiseSampler`，包含多种噪声的实现，其中许多由 `Assets/Resources/Shader/` 下的 Compute Shader 支持；轮廓生成工具 → `ShapeSampler`，用于对分配的群落矩阵再次形成随机的图形轮廓，决定群落轮廓的生成。
- **生物群落**：`BiomeDefinition` 群落基类。局部群落 → `LocalDefinition`，参与群落分配，作用与群落自身；全局群落 → `GobalDefinition`，作用于全局的地形生成逻辑
- **地形生成逻辑**：插槽的概念，群落通过组合不同的插槽决定最终群落的生成
- **物品**：`ItemData` → `ItemData_Buildable`

### 玩家系统

`Entity`（抽象 MonoBehaviour）→ `Player`，使用经典有限状态机：
- `Player` 创建 `PlayerStateMachine` 并注册状态：`Idle`、`Move`、`Jump`、`Air`
- `PlayerState` 基类：`Enter()`、`Exit()`、`Update()`，包含 `stateTimer` 和 `triggerCalled`（动画事件钩子）

### 建造系统

- `BuildingPlacer` 处理玩家输入（左键放置、右键摧毁），带有预览虚影
- `TilemapLayer`（抽象基类：Tilemap + Layers 枚举）→ `ConstructionLayer` → `AddonLayer` / `LiquidLayer`
- `ConstructionLayer.Build()` / `Destory()` 使用 BFS 邻居搜索进行连接瓦片验证
- `PreviewLayer` 渲染放置预览虚影

### 输入系统

通过 `Assets/Input/InputActions.inputactions` 使用 Unity Input System。`InputActions.ext.cs` 提供 `static Instance` 单例。`MouseUser` 将输入桥接到世界坐标。

### 序列化

MessagePack 3.1.3（通过 NuGetForUnity 安装到 `Assets/Packages/`）。存档数据流经 `IMapSaveManager` 接口 → `MapSaveManager` → `FileDataHandler` → GZip 压缩的二进制文件。存档格式版本为 2。

## 代码组织

```
Assets/Scripts/
  World/          核心世界模型：区块、瓦片数据、地形配置、模拟系统
  BuildingSystem/ 面向玩家的建造与 Tilemap 图层封装
  Tile/           瓦片 ScriptableObject：瓦片、液体、附加物、树木、矿石、绳索物理
  Player/         玩家实体 + 有限状态机状态
  Terrains/       程序化地形生成：
    Noise/Script/  噪声类型（NoiseConfig 基类 + 15+ 子类：Perlin、Worley、FBM、Shape 等）
    Biome/         群落
    Terrain/       地形布局（MapGridLayout、MapHorizontalLayout、BaseTerrain）
    Random/        泊松盘采样
  GameInput/      Input System 封装
  Save And Load/  持久化层
  Tools/          单例工具基类
  Utilities/      枚举、自定义 Tilemap 笔刷
```

项目中**没有 `.asmdef` 文件** — 所有代码编译到 `Assembly-CSharp.dll` 中。只有 `GameInput` 和 `BuildingSystem` 使用了显式的 C# 命名空间；其余均为全局命名空间。

`Assets/Student/` 目录包含实验性的波函数坍缩（WFC）原型代码，未集成到主游戏中。

## 编码约定

- 注释使用中文
- 方法参数使用 `_下划线前缀` 命名约定（例如 `_x`、`_newState`）
- 使用 `[SerializeField] private` 配合公共属性 getter；较新的代码使用 `[field: SerializeField] public ... { get; private set; }`
- 协程使用 yield 限流（`if (++processed % 200 == 0) yield return null`）以保证生成过程帧率友好
- 使用 `#if UNITY_EDITOR` 守卫保护仅编辑器代码
- 使用 `[ContextMenu]` 和 `[CreateAssetMenu]` 特性支持编辑器驱动的工作流
