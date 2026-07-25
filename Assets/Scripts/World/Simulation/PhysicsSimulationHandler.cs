using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
    public int maxProcessedCellsPerFrame = 10000;      // 每帧最大处理格子数
    [Range(0.1f, 10f)]
    public float globalSpeedMultiplier = 1f;           // 全局速度倍率（影响所有材料流速）

    [Header("性能统计")]
    public bool enableStats = true;                    // 是否启用统计
    public bool openSimulation = true;

    // 模拟组件
    private SimulationGrid simulationGrid;
    private LiquidSimulation liquidSimulation;
    private PowderSimulation powderSimulation;

    // 区块管理器引用
    private ChunkManager chunkManager;

    // 统计信息
    private Stopwatch simulationStopwatch = new Stopwatch();
    private float lastSimulationTime;
    private int lastProcessedCells;
    private int lastActiveCells;

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

        // 创建液体模拟
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
        UnityEngine.Debug.Log("[PhysicsSimulationHandler] 物理模拟系统初始化完成");
    }

    /// <summary>
    /// 执行单个模拟步骤
    /// </summary>
    private void SimulationStep() {
        simulationStopwatch.Restart();

        // 开始模拟步骤
        simulationGrid.BeginSimulationStep();

        int processedCells = 0;

        //logFrameCounter++;
        //if (logFrameCounter >= logInterval) {
        //    UnityEngine.Debug.Log("目前活动中: " + simulationGrid.GetActiveCellCount());
        //    logFrameCounter = 0;
        //}

        // 创建活跃格子的副本，避免在遍历过程中修改集合
        List<Vector2Int> activeCellsCopy = new List<Vector2Int>(simulationGrid.GetActiveCells());
        activeCellsCopy.Sort((a, b) => b.y.CompareTo(a.y));
        // 遍历副本
        foreach (var pos in activeCellsCopy) {
            // 检查处理预算
            if (maxProcessedCellsPerFrame > 0 && processedCells >= maxProcessedCellsPerFrame) {
                break;
            }
            //logFrameCounter++;
            //if (logFrameCounter >= logInterval) {
            //    UnityEngine.Debug.Log("处理活动瓦片：");
            //    logFrameCounter = 0;
            //}
            // 处理液体
            if (chunkManager.GetTileData(pos).HasLiquid) {

                //logFrameCounter++;
                //if (logFrameCounter >= logInterval) {
                //    UnityEngine.Debug.Log("液体");
                //    logFrameCounter = 0;
                //}
                if (liquidSimulation.StepCell(pos.x, pos.y, simulationGrid)) {
                    processedCells++;
                }
            }

            // 处理粉末
            TileData tileData = chunkManager.GetTileData(pos);
            if (tileData.HasGround && physicsConfig.IsPowderMaterial(tileData.groundId)) {
                if (powderSimulation.StepCell(pos.x, pos.y, simulationGrid)) {
                    processedCells++;
                }
            }
        }

        // 结束模拟步骤
        simulationGrid.EndSimulationStep();

        // 更新统计信息
        simulationStopwatch.Stop();
        if (enableStats) {
            lastSimulationTime = (float)simulationStopwatch.Elapsed.TotalMilliseconds;
            lastProcessedCells = processedCells;
            lastActiveCells = simulationGrid.ActiveCellCount;
        }
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

        // 更新液体体积
        chunkManager.SetLiquidVolume(pos, volume);

        // 如果体积为0，清除液体ID
        if (volume <= 0) {
            chunkManager.SetLiquidId(pos, 0);
        } else {
            // 确保液体ID被正确设置
            if (chunkManager.GetLiquidId(pos) != liquidId) {
                chunkManager.SetLiquidId(pos, liquidId);
            }
        }

        // 更新 Tilemap 渲染
        UpdateLiquidTilemap(pos, liquidId, volume);

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
    /// 更新液体图层的 Tilemap
    /// </summary>
    private void UpdateLiquidTilemap(Vector2Int pos, long liquidId, float volume) {
        TilemapManager tilemapManager = TilemapManager.Instance;
        if (tilemapManager == null) return;

        Tilemap liquidTilemap = tilemapManager.GetTilemap(LayerType.Liquid);
        if (liquidTilemap == null) return;

        TileBase tile = null;
        if (liquidId != 0 && volume > 0) {
            TileClass tileClass = TileRegistry_.GetTile(liquidId);
            if (tileClass != null && tileClass is LiquidClass liquidClass) {
                tile = liquidClass.GetTileToVolume(volume);
            }
        }

        liquidTilemap.SetTile(new Vector3Int(pos.x, pos.y, 0), tile);
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

        return $"FPS: {currentFPS:F1}\n" +
               $"模拟时间: {lastSimulationTime:F2}ms\n" +
               $"处理格子: {lastProcessedCells}\n" +
               $"活跃格子: {lastActiveCells}\n" +
               $"活跃区块: {simulationGrid?.ActiveChunkCount ?? 0}";
    }

    /// <summary>
    /// 获取模拟统计信息
    /// </summary>
    public void GetStats(out float fps, out float simulationTime, out int processedCells, out int activeCells, out int activeChunks) {
        fps = currentFPS;
        simulationTime = lastSimulationTime;
        processedCells = lastProcessedCells;
        activeCells = lastActiveCells;
        activeChunks = simulationGrid?.ActiveChunkCount ?? 0;
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
        GUILayout.Label($"活跃区块: {simulationGrid?.ActiveChunkCount ?? 0}");

        GUILayout.EndArea();
    }
}
