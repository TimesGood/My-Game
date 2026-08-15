using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Unity.Entities.UniversalDelegates;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 物理模拟主循环，整合液体和粉末模拟
/// 借鉴 PixelAlchemy 的系统分离和活跃区域优化设计
/// </summary>
public class PhysicsSimulationHandler : Singleton<PhysicsSimulationHandler> {
    [Header("物理配置")]
    public MaterialPhysicsConfig physicsConfig;

    [Header("模拟设置")]
    public int simulationSeed = 0;                     // 随机种子（0表示随机）
    public int chunkSize = 16;                         // 区块大小
    public int chunkSleepDelay = 3;                    // 区块休眠延迟帧数
    public int maxProcessedCellsPerFrame = 10000;      // 每帧最大处理格子数（预算内屏幕内优先）
    [Range(0.1f, 10f)]
    public float globalSpeedMultiplier = 1f;           // 全局速度倍率（影响所有材料流速）

    [Header("屏幕优先模拟")]
    [Tooltip("启用后预算内先模拟屏幕内的瓦片，剩余预算再分配给屏幕外的瓦片")]
    public bool enableScreenPriority = true;
    [Tooltip("屏幕优先区域额外外扩的瓦片数，避免屏幕边缘的液体/粉末静止")]
    public int screenPriorityPadding = 16;

    [Header("空闲加速模拟")]
    [Tooltip("上一帧模拟耗时低于该毫秒数视为“空闲”，触发加速模拟追赶屏幕外积压")]
    public float idleTimeThresholdMs = 2f;
    [Tooltip("空闲时每帧处理预算的放大倍数（加速追赶预算外未被处理的瓦片）")]
    public float idleBoostMultiplier = 2f;

    [Header("性能统计")]
    public bool enableStats = true;                    // 是否启用统计
    public bool openSimulation = true;

    // 模拟组件
    private SimulationGrid simulationGrid;
    private LiquidSimulation liquidSimulation;         // 液体模拟
    private PowderSimulation powderSimulation;         // 沙砾模拟

    // 存储待刷新液体瓦片集合
    private readonly HashSet<Vector2Int> pendingLiquidTiles = new HashSet<Vector2Int>();

    // 排序桶
    // Y 桶缓存：按 Y 升序处理活跃格子，替代每帧全量排序（O(n) 而非 O(n log n)）
    private List<Vector2Int>[] yBuckets;
    private bool[] bucketUsed;
    private readonly List<int> usedBuckets = new List<int>();

    // 区块管理器引用
    private ChunkManager chunkManager;

    // 统计信息
    private Stopwatch simulationStopwatch = new Stopwatch();
    private float lastSimulationTime;
    private int lastProcessedCells;
    private int lastActiveCells;
    private int lastDeferredCells;                     // 本帧预算外被顺延的活跃格子数
    private int lastScreenProcessedCells;              // 本帧屏幕内处理格子数
    private int lastOffscreenProcessedCells;           // 本帧屏幕外处理格子数

    // 屏幕优先模拟：屏幕可视范围（世界坐标轴对齐包围盒，含外扩）
    private Camera simulationCamera;                   // 模拟相机（懒加载：优先 ChunkHandler 渲染相机，回退主相机）
    private bool hasVisibleBounds = false;             // 是否有有效的可视范围（无相机时退化为全部视为屏幕内）
    private int visibleMinX, visibleMaxX, visibleMinY, visibleMaxY; // 屏幕优先区域边界（含外扩）
    private readonly Vector3[] frustumCorners = new Vector3[4];     // 复用视锥角点缓冲，避免每帧分配
    private float smoothedSimulationTime;              // 平滑后的模拟耗时（避免预算放大/缩小来回抖动）

    // 预算外未被处理的活跃格子（下帧保持活跃继续模拟，防止被静默抛弃而静止）
    private readonly List<Vector2Int> deferredCells = new List<Vector2Int>();

    // FPS 统计
    private float fpsUpdateInterval = 0.5f;            // FPS更新间隔（秒）
    private float fpsAccumulator = 0f;
    private int fpsFrameCount = 0;
    private float currentFPS = 0f;
    private float fpsTimer = 0f;

    // 状态标记
    private bool isInitialized = false;
    private bool isSimulationEnabled = true;


    private int logFrameCounter = 0;
    public int logInterval = 60; // 每30帧打印一次
    protected override void Awake() {
        base.Awake();
        chunkManager = ChunkManager.Instance;
    }

    void Start() {
        InitializeSimulation();
    }

    void Update() {
        if (!isInitialized || !isSimulationEnabled) return;

        // 执行模拟步骤
        if(openSimulation) SimulationStep();

        // 更新 FPS 统计
        UpdateFPS();
    }

    /// <summary>
    /// 初始化模拟系统
    /// </summary>
    public void InitializeSimulation() {
        if (chunkManager == null) {
            chunkManager = ChunkManager.Instance;
        }

        if (chunkManager == null || !chunkManager.IsReady) {
            UnityEngine.Debug.LogWarning("[PhysicsSimulationHandler] ChunkManager 未就绪，延迟初始化");
            return;
        }

        // 创建模拟网格
        simulationGrid = new SimulationGrid(
            chunkManager.Width,
            chunkManager.Height,
            chunkSize,
            chunkSleepDelay
        );

        // 初始化 Y 桶缓冲（按世界高度，复用避免每帧分配）
        if (yBuckets == null || yBuckets.Length != simulationGrid.Height) {
            yBuckets = new List<Vector2Int>[simulationGrid.Height];
            for (int i = 0; i < yBuckets.Length; i++) {
                yBuckets[i] = new List<Vector2Int>();
            }
            bucketUsed = new bool[simulationGrid.Height];
        }

        // 创建自定义液体模拟（带冷却时间的密度分层）
        liquidSimulation = new LiquidSimulation(chunkManager, physicsConfig, simulationSeed);
        liquidSimulation.GlobalSpeedMultiplier = globalSpeedMultiplier;
        liquidSimulation.OnUpdateVolume += HandleLiquidVolumeUpdate;

        //// 创建粉末模拟
        powderSimulation = new PowderSimulation(chunkManager, physicsConfig, simulationSeed);
        powderSimulation.OnMoveTile += HandlePowderMove;
        powderSimulation.OnSwapTiles += HandlePowderSwap;

        //// 激活所有区域
        //simulationGrid.ActivateAll();

        isInitialized = true;
    }

    /// <summary>
    /// 执行单个模拟步骤
    /// 预算分配策略（按优先级）：
    /// 1. 屏幕内的瓦片优先处理（保证玩家可见区域的液体/粉末始终流动）
    /// 2. 屏幕外的瓦片使用剩余预算处理
    /// 3. 预算外的瓦片不抛弃：保持活跃状态顺延到下一帧继续模拟
    /// 4. 上一帧模拟耗时低于阈值（空闲）时放大预算，加速追赶屏幕外积压
    /// </summary>
    private void SimulationStep() {
        simulationStopwatch.Restart();

        // 更新屏幕可视范围（屏幕内优先模拟）
        if (enableScreenPriority) {
            UpdateVisibleBounds();
        } else {
            hasVisibleBounds = false; // 关闭屏幕优先时全部视为屏幕内，退化为旧行为
        }

        // 开始模拟步骤
        HashSet<Vector2Int> activetyCells = simulationGrid.Next();

        // 快照活跃格子并分发到 Y 桶（O(n)），替代全量排序（O(n log n)）
        // 桶内顺序继承自活跃集合枚举顺序，与旧实现"仅按 Y 排序、同行无序"的语义一致
        foreach (var cell in activetyCells) {
            if (!bucketUsed[cell.y]) {
                bucketUsed[cell.y] = true;
                usedBuckets.Add(cell.y);
            }
            yBuckets[cell.y].Add(cell);
        }

        int budget = maxProcessedCellsPerFrame > 0 ? maxProcessedCellsPerFrame : int.MaxValue;


        int processedCells = 0;
        int screenProcessed = 0;
        int offscreenProcessed = 0;
        deferredCells.Clear();

        // 第一遍：屏幕内的瓦片优先（按 Y 底→顶，保持确定性顺序）
        // 确定性顺序是泰拉瑞亚模式平整沉降的关键：无序遍历会导致流动方向随机、冒泡抽搐
        for (int y = 0; y < yBuckets.Length; y++) {
            var bucket = yBuckets[y];
            if (bucket.Count == 0) continue;

            foreach (var pos in bucket) {
                if (!IsOnScreen(pos)) continue;

                if (processedCells < budget) {
                    // 处理液体
                    TileData tileData = chunkManager.GetTileData(pos);
                    if (tileData.HasLiquid) {
                        bool changed = liquidSimulation.StepCell(pos, tileData, simulationGrid);
                        if (changed) {
                            processedCells++;
                            screenProcessed++;
                        }
                    }
                } else {
                    // 预算耗尽，顺延到下一帧（保持活跃，防止被休眠而静止）
                    deferredCells.Add(pos);
                }
            }
        }

        // 第二遍：屏幕外的瓦片（使用剩余预算）
        for (int y = 0; y < yBuckets.Length; y++) {
            var bucket = yBuckets[y];
            if (bucket.Count == 0) continue;

            foreach (var pos in bucket) {
                if (IsOnScreen(pos)) continue;

                if (processedCells < budget) {
                    // 处理液体
                    TileData tileData = chunkManager.GetTileData(pos);
                    if (tileData.HasLiquid) {
                        bool changed = liquidSimulation.StepCell(pos, tileData, simulationGrid);
                        if (changed) {
                            processedCells++;
                            offscreenProcessed++;
                        }
                    }
                } else {
                    deferredCells.Add(pos);
                }
            }
        }

        // 预算外的活跃格子保持活跃：下帧继续模拟，而不是被静默抛弃导致瓦片静止
        for (int i = 0; i < deferredCells.Count; i++) {
            simulationGrid.KeepActive(deferredCells[i]);
        }

        // 复用桶：清理本次使用的桶（即使提前触发预算也要清空，避免残留旧坐标导致重复处理）
        for (int i = 0; i < usedBuckets.Count; i++) {
            int y = usedBuckets[i];
            yBuckets[y].Clear();
            bucketUsed[y] = false;
        }
        usedBuckets.Clear();

        // 统一刷新本帧发生变化的液体瓦片（延迟批量 + 去重，避免模拟循环内逐格 SetTile 卡顿）
        FlushPendingLiquidTiles();

        // 更新统计信息
        simulationStopwatch.Stop();
        // 模拟耗时与平滑值在统计开关关闭时也更新，保证"空闲加速"反馈回路始终生效
        lastSimulationTime = (float)simulationStopwatch.Elapsed.TotalMilliseconds;
        smoothedSimulationTime = Mathf.Lerp(smoothedSimulationTime, lastSimulationTime, 0.2f);
        if (enableStats) {
            lastProcessedCells = processedCells;
            lastActiveCells = simulationGrid.ActiveCellCount;
            lastDeferredCells = deferredCells.Count;
            lastScreenProcessedCells = screenProcessed;
            lastOffscreenProcessedCells = offscreenProcessed;
        }
    }

    /// <summary>
    /// 更新屏幕可视范围（世界坐标轴对齐包围盒，含外扩）
    /// 用于"屏幕内优先模拟"：优先处理玩家可见区域的瓦片
    /// </summary>
    private void UpdateVisibleBounds() {
        // 懒加载相机：优先使用 ChunkHandler 的渲染相机，回退主相机
        if (simulationCamera == null) {
            if (ChunkHandler.Instance != null && ChunkHandler.Instance.renderCamera != null) {
                simulationCamera = ChunkHandler.Instance.renderCamera;
            } else {
                simulationCamera = Camera.main;
            }
        }

        if (simulationCamera == null) {
            // 无相机（如编辑模式/纯逻辑场景）：退化为全部视为屏幕内
            hasVisibleBounds = false;
            return;
        }

        // 复用缓冲计算视锥角点（避免每帧分配数组）
        simulationCamera.CalculateFrustumCorners(
            new Rect(0, 0, 1, 1),
            simulationCamera.farClipPlane,
            Camera.MonoOrStereoscopicEye.Mono,
            frustumCorners);

        Matrix4x4 camMatrix = simulationCamera.transform.localToWorldMatrix;
        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;
        for (int i = 0; i < 4; i++) {
            Vector3 corner = camMatrix.MultiplyPoint(frustumCorners[i]);
            minX = Mathf.Min(minX, corner.x);
            maxX = Mathf.Max(maxX, corner.x);
            minY = Mathf.Min(minY, corner.y);
            maxY = Mathf.Max(maxY, corner.y);
        }

        // 外扩 padding 个瓦片，避免屏幕边缘的瓦片因被划为"屏幕外"而模拟滞后
        int padding = Mathf.Max(0, screenPriorityPadding);
        visibleMinX = Mathf.FloorToInt(minX) - padding;
        visibleMaxX = Mathf.CeilToInt(maxX) + padding;
        visibleMinY = Mathf.FloorToInt(minY) - padding;
        visibleMaxY = Mathf.CeilToInt(maxY) + padding;
        hasVisibleBounds = true;
    }

    /// <summary>
    /// 判断瓦片是否位于屏幕优先区域（屏幕可视范围 + 外扩）
    /// 无相机或关闭屏幕优先时全部视为屏幕内
    /// </summary>
    private bool IsOnScreen(Vector2Int pos) {
        if (!hasVisibleBounds) return true;
        return pos.x >= visibleMinX && pos.x <= visibleMaxX &&
               pos.y >= visibleMinY && pos.y <= visibleMaxY;
    }

    /// <summary>
    /// 标记格子需要更新
    /// </summary>
    public void MarkForUpdate(Vector2Int pos) {
        if (!isInitialized) return;
        simulationGrid.MarkChanged(pos);
    }

    /// <summary>
    /// 标记区域需要更新
    /// </summary>
    public void MarkAreaForUpdate(Vector2Int center, int radius) {
        if (!isInitialized) return;
        simulationGrid.MarkActiveArea(center.x, center.y, radius);
    }

    /// <summary>
    /// 处理液体体积更新
    /// </summary>
    private void HandleLiquidVolumeUpdate(long liquidId, Vector2Int pos, float volume) {
        if (!chunkManager.CheckWorldBound(pos.x, pos.y)) return;

        // 单次查找合并写入 ID 与体积（替代原先 3 次分离查找，且不置 isDirty）
        chunkManager.SetLiquid(pos, liquidId, volume);

        // 延迟到本帧模拟结束后统一刷新 Tilemap（同格多次变化只刷一次）
        pendingLiquidTiles.Add(pos);

        // 标记周围区域需要更新
        simulationGrid.MarkChanged(pos);
    }

    /// <summary>
    /// 处理粉末移动
    /// </summary>
    private void HandlePowderMove(long sourceId, Vector2Int sourcePos, long targetId, Vector2Int targetPos) {
        if (!chunkManager.CheckWorldBound(sourcePos.x, sourcePos.y)) return;
        if (!chunkManager.CheckWorldBound(targetPos.x, targetPos.y)) return;

        // 移动粉末
        chunkManager.SetBlockId(LayerType.Foreground, sourcePos.x, sourcePos.y, targetId);
        chunkManager.SetBlockId(LayerType.Foreground, targetPos.x, targetPos.y, sourceId);

        // 更新 Tilemap 渲染
        UpdateForegroundTilemap(sourcePos, sourceId);
        UpdateForegroundTilemap(targetPos, targetId);

        // 标记周围区域需要更新
        simulationGrid.MarkChanged(sourcePos);
        simulationGrid.MarkChanged(targetPos);
    }

    /// <summary>
    /// 处理粉末交换
    /// </summary>
    private void HandlePowderSwap(long sourceId, Vector2Int sourcePos, long targetId, Vector2Int targetPos) {
        if (!chunkManager.CheckWorldBound(sourcePos.x, sourcePos.y)) return;
        if (!chunkManager.CheckWorldBound(targetPos.x, targetPos.y)) return;

        // 交换粉末位置
        chunkManager.SetBlockId(LayerType.Foreground, sourcePos.x, sourcePos.y, targetId);
        chunkManager.SetBlockId(LayerType.Foreground, targetPos.x, targetPos.y, sourceId);

        // 更新 Tilemap 渲染
        UpdateForegroundTilemap(sourcePos, sourceId);
        UpdateForegroundTilemap(targetPos, targetId);

        // 标记周围区域需要更新
        simulationGrid.MarkChanged(sourcePos);
        simulationGrid.MarkChanged(targetPos);
    }

    /// <summary>
    /// 更新液体图层的 Tilemap（读取 ChunkManager 中的最终状态）
    /// </summary>
    private void UpdateLiquidTilemap(Vector2Int pos) {
        TilemapManager tilemapManager = TilemapManager.Instance;
        if (tilemapManager == null) return;

        Tilemap liquidTilemap = tilemapManager.GetTilemap(LayerType.Liquid);
        if (liquidTilemap == null) return;

        TileData data = chunkManager.GetTileData(pos);
        long liquidId = data.liquidId;
        float volume = data.liquidVolume;

        TileBase tile = null;
        if (liquidId != 0 && volume > 0) {
            TileClass tileClass = TileRegistry_.GetTile(liquidId);
            if (tileClass != null && tileClass is LiquidClass liquidClass) {
                TileClass upTile = chunkManager.GetTileClass(LayerType.Liquid, pos + Vector2Int.up);

                float maxVolume = physicsConfig.GetMaxVolume(liquidId);
                float ratio = volume / maxVolume;
                if (upTile != null)
                    tile = liquidClass.GetTileToVolume(maxVolume);
                else
                    tile = liquidClass.GetTileToVolume(ratio);
            }
        }

        liquidTilemap.SetTile(new Vector3Int(pos.x, pos.y, 0), tile);
    }

    /// <summary>
    /// 批量刷新本帧发生变化的液体瓦片。
    /// 模拟过程中只记录脏坐标，帧末统一 SetTile，同一格多次变化只刷新一次。
    /// </summary>
    private void FlushPendingLiquidTiles() {
        if (pendingLiquidTiles.Count == 0) return;

        foreach (var pos in pendingLiquidTiles) {
            UpdateLiquidTilemap(pos);
        }
        pendingLiquidTiles.Clear();
    }

    /// <summary>
    /// 更新前景图层的 Tilemap
    /// </summary>
    private void UpdateForegroundTilemap(Vector2Int pos, long blockId) {
        TilemapManager tilemapManager = TilemapManager.Instance;
        if (tilemapManager == null) return;

        Tilemap foregroundTilemap = tilemapManager.GetTilemap(LayerType.Foreground);
        if (foregroundTilemap == null) return;

        TileBase tile = null;
        if (blockId != 0) {
            TileClass tileClass = TileRegistry_.GetTile(blockId);
            if (tileClass != null) {
                tile = tileClass.tile;
            }
        }

        foregroundTilemap.SetTile(new Vector3Int(pos.x, pos.y, 0), tile);
    }

    /// <summary>
    /// 更新 FPS 统计
    /// </summary>
    private void UpdateFPS() {
        fpsFrameCount++;
        fpsAccumulator += Time.unscaledDeltaTime;
        fpsTimer += Time.unscaledDeltaTime;

        // 每隔指定间隔更新 FPS
        if (fpsTimer >= fpsUpdateInterval) {
            currentFPS = fpsFrameCount / fpsAccumulator;
            fpsFrameCount = 0;
            fpsAccumulator = 0f;
            fpsTimer = 0f;
        }
    }

    /// <summary>
    /// 获取当前 FPS
    /// </summary>
    public float GetCurrentFPS() {
        return currentFPS;
    }

    /// <summary>
    /// 启用/禁用模拟
    /// </summary>
    public void SetSimulationEnabled(bool enabled) {
        isSimulationEnabled = enabled;
    }

    /// <summary>
    /// 设置全局速度倍率
    /// </summary>
    public void SetGlobalSpeedMultiplier(float multiplier) {
        globalSpeedMultiplier = Mathf.Clamp(multiplier, 0.1f, 10f);
        if (liquidSimulation != null) {
            liquidSimulation.GlobalSpeedMultiplier = globalSpeedMultiplier;
        }
    }

    /// <summary>
    /// 重新初始化模拟（世界加载后调用）
    /// </summary>
    public void Reinitialize() {
        isInitialized = false;
        InitializeSimulation();
    }

    /// <summary>
    /// 获取模拟统计信息
    /// </summary>
    public string GetStatsString() {
        if (!enableStats) return "统计已禁用";

        string priorityInfo = enableScreenPriority
            ? $"屏幕内: {lastScreenProcessedCells}  屏幕外: {lastOffscreenProcessedCells}\n"
            : "";
        return $"FPS: {currentFPS:F1}\n" +
               $"模拟时间: {lastSimulationTime:F2}ms\n" +
               $"处理格子: {lastProcessedCells}\n" +
               $"活跃格子: {lastActiveCells}\n" +
               $"{priorityInfo}" +
               $"顺延格子: {lastDeferredCells}";
    }

    /// <summary>
    /// 获取模拟统计信息
    /// </summary>
    public void GetStats(out float fps, out float simulationTime, out int processedCells, out int activeCells, out int activeChunks) {
        fps = currentFPS;
        simulationTime = lastSimulationTime;
        processedCells = lastProcessedCells;
        activeCells = lastActiveCells;
        activeChunks = 0;
    }

    void OnGUI() {
        if (!enableStats || !isInitialized) return;

        // 在屏幕左上角显示统计信息
        GUILayout.BeginArea(new Rect(10, 10, 300, 250));
        GUILayout.Label("物理模拟统计", GUI.skin.box);

        // FPS 显示（带颜色）
        GUIStyle fpsStyle = new GUIStyle(GUI.skin.label);
        if (currentFPS >= 50) {
            fpsStyle.normal.textColor = Color.green;
        } else if (currentFPS >= 30) {
            fpsStyle.normal.textColor = Color.yellow;
        } else {
            fpsStyle.normal.textColor = Color.red;
        }
        GUILayout.Label($"FPS: {currentFPS:F1}", fpsStyle);

        // 其他统计信息
        GUILayout.Label($"模拟时间: {lastSimulationTime:F2}ms");
        GUILayout.Label($"处理格子: {lastProcessedCells}");
        GUILayout.Label($"活跃格子: {lastActiveCells}");
        GUILayout.Label($"顺延格子: {lastDeferredCells}");

        GUILayout.EndArea();
    }


    // 查看指定坐标是否活跃
    public bool IsCellActive(Vector2Int pos) {
        return simulationGrid.IsCellActive(pos);
    }
}
