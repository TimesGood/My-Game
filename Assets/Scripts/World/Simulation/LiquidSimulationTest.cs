using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using static UnityEditor.Progress;

/// <summary>
/// 液体物理模拟测试
/// </summary>
public class LiquidSimulationTest
{
    private readonly ChunkManager chunkManager;
    private readonly MaterialPhysicsConfig physicsConfig;
    private readonly System.Random random;
    private bool IsDiffusion = false;

    // 流速控制：记录每个格子上次更新时间
    private readonly Dictionary<Vector2Int, float> lastUpdateTime = new Dictionary<Vector2Int, float>();

    // 全局速度倍率
    public float GlobalSpeedMultiplier { get; set; } = 1f;

    // 液体更新回调
    public System.Action<long, Vector2Int, float> OnUpdateVolume;

    /// <summary>
    /// 创建液体模拟实例
    /// </summary>
    /// <param name="chunkManager">区块管理器</param>
    /// <param name="physicsConfig">物理配置</param>
    /// <param name="seed">随机种子（0表示随机）</param>
    public LiquidSimulationTest(ChunkManager chunkManager, MaterialPhysicsConfig physicsConfig, int seed = 0) {
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

        // 获取材料定义
        var materialDef = physicsConfig.GetDefinition(liquidId);
        if (materialDef == null || !materialDef.IsLiquid) return false;

        // 流速控制：检查是否到达更新时间
        Vector2Int pos = new Vector2Int(x, y);
        if (!ShouldUpdate(pos, materialDef.flowSpeed)) {
            // 此帧不到更新时间，放到下一帧
            grid.MarkChanged(x, y);
            return false;
        }

        // 检查最小体积阈值
        if (curVolume < materialDef.minVolume) {
            // 体积太小，移除液体
            UpdateVolume(liquidId, pos, 0f);
            return true;
        }

        // 检查是否有固体方块阻挡
        TileClass foregroundTile = chunkManager.GetTileClass(LayerType.Foreground, x, y);
        if (foregroundTile != null) {
            // 被固体阻挡，移除液体
            UpdateVolume(liquidId, pos, 0f);
            return true;
        }



        // 1. 尝试向下流动
        if (TryFlowDown(x, y, ref curVolume, liquidId, materialDef)) {
            return true;
        }

        // 2. 尝试横向扩散
        if (TryDiffusion(x, y, ref curVolume, liquidId, materialDef)) {
            return true;
        }

        // 3. 尝试向上溢出（体积>1时）
        if (TryOverflow(x, y, curVolume, liquidId, materialDef)) {
            return true;
        }

        return false;
    }

    /// <summary>
    /// 尝试向下流动
    /// </summary>
    private bool TryFlowDown(int x, int y, ref float curVolume, long liquidId, SimulationMaterialDefinition materialDef) {
        var pos = new Vector2Int(x, y);
        if (y <= 0) return false;

        Vector2Int downPos = pos + Vector2Int.down;

        // 检查下方是否可流动（有固体阻挡）
        if (chunkManager.GetTileClass(LayerType.Foreground, downPos.x, downPos.y) != null) return false;

        // 获取下方瓦片信息
        TileData downData = chunkManager.GetTileData(downPos);
        if (downData.HasGround || downData.liquidVolume >= 1f) return false;

        // 下方是空的或同种液体，执行普通流动
        float downVolume = curVolume + downData.liquidVolume;
        UpdateVolume(liquidId, downPos, downVolume);

        // 清空当前位置
        UpdateVolume(liquidId, pos, 0);

        return true;
    }

    /// <summary>
    /// 尝试横向扩散
    /// </summary>
    private bool TryDiffusion(int x, int y, ref float curVolume, long liquidId, SimulationMaterialDefinition materialDef) {
        var pos = new Vector2Int(x, y);

        // 评估左右两侧流动目标（若目标下方为空则重定向到下方以加速下落）
        var leftTarget = pos + Vector2Int.left;
        var rightTarget = pos + Vector2Int.right;

        bool canFlowLeft = CheckFlowDirection(ref leftTarget, curVolume, materialDef);
        bool canFlowRight = CheckFlowDirection(ref rightTarget, curVolume, materialDef);

        if (!canFlowLeft && !canFlowRight) return false;

        // 区分「平流」（水平均分）与「重定向」（向下追加）目标
        bool leftFell = canFlowLeft && leftTarget.y != y;
        bool rightFell = canFlowRight && rightTarget.y != y;
        bool hasFalling = leftFell || rightFell;

        // ---------- 计算均分体积 ----------
        // 仅平流目标参与均分；有重定向目标时基础分母 +1（当前位置 + 下落份额）
        float avg = curVolume;
        int divisor = hasFalling ? 2 : 1;

        if (canFlowLeft && !leftFell) {
            avg += chunkManager.GetLiquidVolume(leftTarget);
            divisor++;
        }
        if (canFlowRight && !rightFell) {
            avg += chunkManager.GetLiquidVolume(rightTarget);
            divisor++;
        }
        avg /= divisor;

        // ---------- 更新当前位置 ----------
        curVolume = avg;
        UpdateVolume(liquidId, pos, avg);

        // ---------- 更新平流目标 ----------
        if (canFlowLeft && !leftFell) {
            UpdateVolume(liquidId, leftTarget, avg);
        }
        if (canFlowRight && !rightFell) {
            UpdateVolume(liquidId, rightTarget, avg);
        }

        // ---------- 更新重定向下落目标（在已有体积上追加） ----------
        if (leftFell) {
            float existing = chunkManager.GetLiquidVolume(leftTarget);
            UpdateVolume(liquidId, leftTarget, existing + avg);
        }
        if (rightFell) {
            float existing = chunkManager.GetLiquidVolume(rightTarget);
            UpdateVolume(liquidId, rightTarget, existing + avg);
        }

        return true;
    }

    /// <summary>
    /// 检查是否可以向指定方向流动
    /// </summary>
    private bool CheckFlowDirection(ref Vector2Int dir, float curVolume, SimulationMaterialDefinition materialDef) {
      
        if (!chunkManager.CheckWorldBound(dir)) return false;

        TileData targetData = chunkManager.GetTileData(dir);
        // 检查是否有固体阻挡
        if (targetData.HasGround) return false;

        // 水平扩散时，如果扩散目标下方是空的，不需要水平扩散，直接扩散到下级
        var downDir = dir + Vector2Int.down;
        TileData downData = chunkManager.GetTileData(downDir);
        if (downData.IsEmpty || (downData.HasLiquid && downData.liquidVolume < materialDef.maxVolume)) {
            dir = downDir;
            return true;
        }

        // 横向扩散则需要比较体积
        if (curVolume > targetData.liquidVolume) {
            return true;
        }

        return false;
    }

    /// <summary>
    /// 尝试向上溢出
    /// </summary>
    private bool TryOverflow(int x, int y, float curVolume, long liquidId, SimulationMaterialDefinition materialDef) {
        if (curVolume <= 1f) return false;
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
        upVolume += curVolume - 1f;
        UpdateVolume(liquidId, upPos, upVolume);

        curVolume = 1f;
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
