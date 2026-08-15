using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using static Unity.Burst.Intrinsics.X86;
using static UnityEditor.PlayerSettings;
using static UnityEditor.Progress;

/// <summary>
/// 液体物理模拟测试
/// </summary>
public class LiquidSimulation
{
    private readonly ChunkManager chunkManager;
    private readonly MaterialPhysicsConfig physicsConfig;
    private readonly System.Random random;
    private bool IsDiffusion = false;

    // 流速控制：记录每个格子上次更新时间
    private readonly Dictionary<Vector2Int, float> lastUpdateTime = new Dictionary<Vector2Int, float>();

    // 临时方向瓦片数据存储
    private readonly Dictionary<Vector2Int, TileData> tempPosList = new Dictionary<Vector2Int, TileData>();// ��ʱ���ݻ���

    // 全局速度倍率
    public float GlobalSpeedMultiplier { get; set; } = 1f;


    // ===== 可调参数 =====
    public float MaxVerticalFlowRate { get; set; } = 0.10f;

    // 液体更新回调
    public System.Action<long, Vector2Int, float> OnUpdateVolume;

    /// <summary>
    /// 创建液体模拟实例
    /// </summary>
    /// <param name="chunkManager">区块管理器</param>
    /// <param name="physicsConfig">物理配置</param>
    /// <param name="seed">随机种子（0表示随机）</param>>
    public LiquidSimulation(ChunkManager chunkManager, MaterialPhysicsConfig physicsConfig, int seed = 0) {
        this.chunkManager = chunkManager;
        this.physicsConfig = physicsConfig;
        this.random = seed == 0 ? new System.Random() : new System.Random(seed);
    }

    /// <summary>
    /// 清除计时器缓存（世界重置时调用）
    /// </summary>
    public void ClearTimers() {
        lastUpdateTime.Clear();
    }

    /// <summary>
    /// 检查格子是否应该更新（基于流速控制）
    /// </summary>
    private bool ShouldUpdate(Vector2Int pos, float flowSpeed) {
        float currentTime = Time.time;
        // 应用全局速度倍率
        float effectiveSpeed = flowSpeed * Mathf.Max(0.1f, GlobalSpeedMultiplier);
        float updateInterval = 1f / Mathf.Max(0.1f, effectiveSpeed); // 防止除零

        if (lastUpdateTime.TryGetValue(pos, out float lastTime)) {
            if (currentTime - lastTime < updateInterval) {
                return false; // 还没到更新时间
            }
        }

        // 更新时间戳
        lastUpdateTime[pos] = currentTime;
        return true;
    }

    /// <summary>
    /// 处理单个液体格子的物理计算
    /// </summary>
    /// <param name="x">X坐标</param>
    /// <param name="y">Y坐标</param>
    /// <param name="grid">模拟网格</param>
    /// <returns>是否发生了变化</returns>
    public bool StepCell(int x, int y, SimulationGrid grid) {
        if (!grid.InBounds(x, y)) return false;

        TileData tileData = chunkManager.GetTileData(x, y);
        if (!tileData.HasLiquid) return false;

        long liquidId = tileData.liquidId;
        float curVolume = tileData.liquidVolume;

        var materialDef = physicsConfig.GetDefinition(liquidId);
        if (materialDef == null || !materialDef.IsLiquid) return false;

        var pos = new Vector2Int(x, y);


        // 流速控制：检查是否到达更新时间
        if (!ShouldUpdate(pos, materialDef.flowSpeed)) {
            // 此帧不到更新时间，放到下一帧
            grid.KeepActive(x, y);
            return false;
        }

        // 检查最小体积阈值
        if (curVolume < materialDef.minVolume) {
            // 体积太小，移除液体
            UpdateVolume(liquidId, pos, 0f);
            return true;
        }

        // 清理:格内有实体地面阻挡
        if (chunkManager.GetTileClass(LayerType.Foreground, x, y) != null) {
            UpdateVolume(liquidId, pos, 0f);
            return true;
        }




        // 1. 尝试向下流动
        if (TryFlowDown(x, y, curVolume, liquidId, materialDef)) return true;

        // 2. 尝试斜向扩散
        if (TryDiagonalFlow(x, y, curVolume, liquidId, materialDef)) return true;

        // 3. 尝试横向扩散
        if (TrySpreadFlow(x, y, curVolume, liquidId, materialDef)) return true;

        // 4. 尝试向上溢出
        if (TryOverflow(x, y, curVolume, liquidId, materialDef)) return true;

        return false;
    }

    /// <summary>
    /// 尝试向下流动
    /// </summary>
    private bool TryFlowDown(int x, int y, float curVolume, long liquidId, SimulationMaterialDefinition materialDef) {
        if (y <= 0) return false;

        Vector2Int downPos = new Vector2Int(x, y - 1);
        TileData downData = chunkManager.GetTileData(downPos);
        if (downData.HasGround) return false;

        long downLiquidId = downData.liquidId;
        float downVolume = downData.liquidVolume;

        // 下方为空 → 转移
        if (downLiquidId == 0) {
            float move = Mathf.Min(curVolume, MaxVerticalFlowRate * materialDef.maxVolume);
            UpdateVolume(liquidId, downPos, move);
            UpdateVolume(liquidId, new Vector2Int(x, y), curVolume - move);
            if (lastUpdateTime.TryGetValue(new Vector2Int(x, y), out float lastTime)) {
                lastUpdateTime[downPos] = lastTime;
            }
            return true;
        }

        // 下方为同种液体 → 未满则转移
        if (downLiquidId == liquidId) {
            if (downVolume >= materialDef.maxVolume) return false;
            float move = Mathf.Min(curVolume, MaxVerticalFlowRate * materialDef.maxVolume);
            UpdateVolume(liquidId, downPos, downVolume + move);
            UpdateVolume(liquidId, new Vector2Int(x, y), curVolume - move);
            return true;
        }

        // 下方为异种液体 → 密度判定
        var downDef = physicsConfig.GetDefinition(downLiquidId);
        if (downDef == null) return false;

        if (materialDef.density > downDef.density) {
            // 当前密度更大 → 沉底(交换两格液体)
            UpdateVolume(downLiquidId, new Vector2Int(x, y), downVolume);
            UpdateVolume(liquidId, downPos, curVolume);
            return true;
        }

        return false; // 当前密度更小 → 浮在上面,阻挡下落
    }

    /// <summary>
    /// 尝试斜向流动
    /// </summary>
    private bool TryDiagonalFlow(int x, int y, float curVolume, long liquidId, SimulationMaterialDefinition materialDef) {
        
        var pos = new Vector2Int(x, y);
        var leftDiagonalPos = pos + Vector2Int.left + Vector2Int.down;
        var rightDiagonalPos = pos + Vector2Int.right + Vector2Int.down;
        TileData leftData = chunkManager.GetTileData(leftDiagonalPos);
        TileData rightData = chunkManager.GetTileData(rightDiagonalPos);
        if (CheckDiagonalFlow(leftDiagonalPos, curVolume, liquidId, materialDef)) tempPosList.Add(leftDiagonalPos, leftData);
        if (CheckDiagonalFlow(rightDiagonalPos, curVolume, liquidId, materialDef)) tempPosList.Add(rightDiagonalPos, rightData);

        if (tempPosList.Count == 0) return false;
        float avg = curVolume;

        avg /= (tempPosList.Count + 1);

        UpdateVolume(liquidId, pos, avg);

        // 更新目标位置
        foreach (var dir in tempPosList) {
            Vector2Int k = dir.Key;
            TileData v = dir.Value;
            UpdateVolume(liquidId, k, avg + v.liquidVolume);
        }
        tempPosList.Clear();
        return true;

    }

    /// <summary>
    /// 尝试横向流动
    /// </summary>
    private bool TrySpreadFlow(int x, int y, float curVolume, long liquidId, SimulationMaterialDefinition materialDef) {
        
        var pos = new Vector2Int(x, y);
        // 检测可用流动方向
        Vector2Int leftDir = pos + Vector2Int.left;
        Vector2Int rightDir = pos + Vector2Int.right;
        TileData leftData = chunkManager.GetTileData(leftDir);
        TileData rightData = chunkManager.GetTileData(rightDir);
        if (CheckSpreadFlow(rightDir, curVolume, liquidId, materialDef)) tempPosList.Add(rightDir, rightData);
        if (CheckSpreadFlow(leftDir, curVolume, liquidId, materialDef)) tempPosList.Add(leftDir, leftData);
        if (tempPosList.Count == 0) return false;

        // 计算每个方向的分配量
        float avg = curVolume;
        foreach (var item in tempPosList) {
            Vector2Int k = item.Key;
            TileData v = item.Value;
            if (v.liquidId != liquidId) continue;
            avg += v.liquidVolume;
        }
        avg /= (tempPosList.Count + 1);

        UpdateVolume(liquidId, pos, avg);

        // 更新目标位置
        foreach (var dir in tempPosList) {
            // 同种液体或空位置，直接更新
            Vector2Int k = dir.Key;
            TileData v = dir.Value;

            // 不同液体
            if (v.liquidId != 0 && v.liquidId != liquidId) {

                Vector2Int upPos = k + Vector2Int.up;
                
                TileData upData = chunkManager.GetTileData(upPos);

                float upVolume = v.liquidVolume;
                if (upData.HasLiquid && upData.liquidId == v.liquidId) {
                    upVolume += upData.liquidVolume;
                }

                UpdateVolume(v.liquidId, upPos, upVolume);

                UpdateVolume(liquidId, k, avg);
                continue;
            }

            UpdateVolume(liquidId, k, avg);
            
        }
        tempPosList.Clear();
        return true;
    }

    private bool CheckSpreadFlow(Vector2Int dir, float curVolume, long liquidId, SimulationMaterialDefinition materialDef) {
        if (!chunkManager.CheckWorldBound(dir)) return false;

        TileData targetData = chunkManager.GetTileData(dir);
        // 检查是否符合条件
        if (!targetData.HasGround && !targetData.HasLiquid) return true;
        if (targetData.HasGround) return false;

        // 相同液体
        if (targetData.HasLiquid && targetData.liquidId == liquidId) {
            if (curVolume > targetData.liquidVolume && curVolume - targetData.liquidVolume > materialDef.minVolume) return true;
        }

        // 不同液体
        if (targetData.HasLiquid && targetData.liquidId != liquidId) {

            var targetDef = physicsConfig.GetDefinition(targetData.liquidId);
            if (targetDef == null) return false;
            if (targetDef.density < materialDef.density) return true;

            // return false;
        }
        return false;

    }

    private bool CheckDiagonalFlow(Vector2Int dir, float curVolume, long liquidId, SimulationMaterialDefinition materialDef) {

        if (!chunkManager.CheckWorldBound(dir)) return false;

        TileData targetData = chunkManager.GetTileData(dir);
        // 检查是否符合条件
        if (targetData.HasGround) return false;

        // 相同液体
        if (targetData.HasLiquid && targetData.liquidId == liquidId) {
            if (targetData.liquidVolume < materialDef.maxVolume) return true;
        }
        return false;
    }

    /// <summary>
    /// 尝试向上溢出
    /// </summary>
    private bool TryOverflow(int x, int y, float curVolume, long liquidId, SimulationMaterialDefinition materialDef) {
        if (curVolume <= materialDef.maxVolume) return false;
        var pos = new Vector2Int(x, y);
        //液体溢出
        Vector2Int upPos = pos + Vector2Int.up;
        LiquidClass targetLiquid = chunkManager.GetTileClass(LayerType.Liquid, upPos.x, upPos.y) as LiquidClass;

        // 如果溢出目标不是相同液体
        if (targetLiquid != null && targetLiquid.blockId != liquidId) {
            return false;
        }
        //float upVolume = liquidHandler.liquidVolume[upPos.x, upPos.y];
        float upVolume = chunkManager.GetLiquidVolume(upPos);
        upVolume += curVolume - materialDef.maxVolume;
        UpdateVolume(liquidId, upPos, upVolume);

        curVolume = materialDef.maxVolume;
        UpdateVolume(liquidId, pos, curVolume);
        return true;
    }

    /// <summary>
    /// 更新液体体积
    /// </summary>
    private void UpdateVolume(long liquidId, Vector2Int pos, float volume) {
        OnUpdateVolume?.Invoke(liquidId, pos, volume);
    }
}
