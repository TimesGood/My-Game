using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// PixelAlchemy 风格的液体物理模拟
/// 基于粒子移动理念，每个格子是一个粒子
/// 特点：密度驱动位移、横向搜索、随机化方向
/// </summary>
public class LiquidSimulationPixelAlchemy {
    private readonly ChunkManager chunkManager;
    private readonly MaterialPhysicsConfig physicsConfig;
    private readonly System.Random random;

    // 帧更新标记：避免同一帧重复移动
    private readonly HashSet<Vector2Int> updatedThisFrame = new HashSet<Vector2Int>();

    // 下落帧数跟踪：用于加速下落
    private readonly Dictionary<Vector2Int, int> fallingFrames = new Dictionary<Vector2Int, int>();

    // 液体更新回调
    public System.Action<long, Vector2Int, float> OnUpdateVolume;

    /// <summary>
    /// 创建 PixelAlchemy 风格的液体模拟实例
    /// </summary>
    public LiquidSimulationPixelAlchemy(ChunkManager chunkManager, MaterialPhysicsConfig physicsConfig, int seed = 0) {
        this.chunkManager = chunkManager;
        this.physicsConfig = physicsConfig;
        this.random = seed == 0 ? new System.Random() : new System.Random(seed);
    }

    /// <summary>
    /// 清除帧标记（每帧开始时调用）
    /// </summary>
    public void ClearFrameFlags() {
        updatedThisFrame.Clear();
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

        Vector2Int pos = new Vector2Int(x, y);

        // 检查是否已在本帧更新过
        if (updatedThisFrame.Contains(pos)) {
            return false;
        }

        TileData tileData = chunkManager.GetTileData(x, y);
        if (!tileData.HasLiquid) return false;

        long liquidId = tileData.liquidId;
        float curVolume = tileData.liquidVolume;

        // 获取材料定义
        var materialDef = physicsConfig.GetDefinition(liquidId);
        if (materialDef == null || !materialDef.IsLiquid) return false;

        // 检查最小体积阈值
        if (curVolume < materialDef.minVolume) {
            UpdateVolume(liquidId, pos, 0f);
            MarkUpdated(pos);
            return true;
        }

        // 检查是否有固体方块阻挡
        TileClass foregroundTile = chunkManager.GetTileClass(LayerType.Foreground, x, y);
        if (foregroundTile != null) {
            UpdateVolume(liquidId, pos, 0f);
            MarkUpdated(pos);
            return true;
        }

        // 随机化左右优先顺序
        int firstSide = random.Next(0, 2) == 0 ? -1 : 1;

        // 1. 尝试向下流动（优先级最高）
        if (TryMoveVertical(x, y, liquidId, materialDef, -1)) {
            return true;
        }

        // 2. 尝试横向扩散（液体特性）
        if (random.NextDouble() <= materialDef.lateralProbability) {
            if (TryHorizontalSpread(x, y, liquidId, materialDef, firstSide)) {
                return true;
            }

            if (TryHorizontalSpread(x, y, liquidId, materialDef, -firstSide)) {
                return true;
            }
        }

        // 所有移动尝试失败，标记为已处理
        MarkUpdated(pos);
        return false;
    }

    /// <summary>
    /// 尝试垂直移动
    /// </summary>
    private bool TryMoveVertical(int x, int y, long liquidId, SimulationMaterialDefinition materialDef, int direction) {
        return TryMove(x, y, 0, direction, liquidId, materialDef);
    }

    /// <summary>
    /// 尝试横向扩散（支持搜索多个格子）
    /// </summary>
    private bool TryHorizontalSpread(int x, int y, long liquidId, SimulationMaterialDefinition materialDef, int direction) {
        int maxDistance = Mathf.Max(1, materialDef.horizontalSpreadDistance);

        for (int distance = 1; distance <= maxDistance; distance++) {
            if (TryMove(x, y, direction * distance, 0, liquidId, materialDef)) {
                return true;
            }

            int checkX = x + direction * distance;
            if (!chunkManager.CheckWorldBound(checkX, y)) {
                // 到达世界边界时停止搜索
                return false;
            }

            // 检查是否被不同材料阻挡
            TileData checkTile = chunkManager.GetTileData(checkX, y);
            if (checkTile.HasGround) {
                // 被固体阻挡
                return false;
            }

            if (checkTile.HasLiquid && checkTile.liquidId != liquidId) {
                // 遇到不同液体，停止搜索
                return false;
            }
        }

        return false;
    }

    /// <summary>
    /// 尝试移动到指定位置
    /// </summary>
    private bool TryMove(int fromX, int fromY, int offsetX, int offsetY, long liquidId, SimulationMaterialDefinition materialDef) {
        int toX = fromX + offsetX;
        int toY = fromY + offsetY;

        Vector2Int fromPos = new Vector2Int(fromX, fromY);
        Vector2Int toPos = new Vector2Int(toX, toY);

        // 检查目标位置是否在范围内
        if (!chunkManager.CheckWorldBound(toX, toY)) {
            // 越界时清空原位置（模拟流出世界）
            if (offsetY < 0) { // 只有向下流出才清除
                UpdateVolume(liquidId, fromPos, 0f);
                MarkUpdated(fromPos);
                return true;
            }
            return false;
        }

        // 检查目标位置是否有固体阻挡
        if (chunkManager.GetTileClass(LayerType.Foreground, toX, toY) != null) {
            return false;
        }

        TileData targetTile = chunkManager.GetTileData(toX, toY);
        float targetVolume = targetTile.liquidVolume;
        long targetLiquidId = targetTile.liquidId;

        // 情况1：目标为空气（无液体）
        if (!targetTile.HasLiquid) {
            // 直接移动液体
            float sourceVolume = chunkManager.GetLiquidVolume(fromPos);
            UpdateVolume(liquidId, toPos, sourceVolume);
            UpdateVolume(liquidId, fromPos, 0f);

            // 更新下落帧数
            UpdateFallingFrames(fromPos, toPos, offsetY);

            MarkUpdated(fromPos);
            MarkUpdated(toPos);
            return true;
        }

        // 情况2：目标是同种液体
        if (targetLiquidId == liquidId) {
            // 如果目标未满，可以流动
            if (targetVolume < materialDef.maxVolume) {
                float sourceVolume = chunkManager.GetLiquidVolume(fromPos);
                float totalVolume = sourceVolume + targetVolume;

                if (totalVolume <= materialDef.maxVolume) {
                    // 全部流过去
                    UpdateVolume(liquidId, toPos, totalVolume);
                    UpdateVolume(liquidId, fromPos, 0f);
                } else {
                    // 只流一部分
                    float flowAmount = materialDef.maxVolume - targetVolume;
                    UpdateVolume(liquidId, toPos, materialDef.maxVolume);
                    UpdateVolume(liquidId, fromPos, sourceVolume - flowAmount);
                }

                MarkUpdated(fromPos);
                MarkUpdated(toPos);
                return true;
            }
            return false; // 目标已满
        }

        // 情况3：目标是不同液体，检查密度位移
        var targetMaterialDef = physicsConfig.GetDefinition(targetLiquidId);
        if (targetMaterialDef != null && CanDisplace(materialDef, targetMaterialDef, offsetY)) {
            // 密度驱动位移：交换两种液体
            float sourceVolume = chunkManager.GetLiquidVolume(fromPos);

            // 简化处理：直接交换位置
            UpdateVolume(liquidId, toPos, sourceVolume);
            UpdateVolume(targetLiquidId, fromPos, targetVolume);

            // 更新下落帧数
            UpdateFallingFrames(fromPos, toPos, offsetY);

            MarkUpdated(fromPos);
            MarkUpdated(toPos);
            return true;
        }

        return false;
    }

    /// <summary>
    /// 检查是否可以位移（密度比较）
    /// </summary>
    private bool CanDisplace(SimulationMaterialDefinition source, SimulationMaterialDefinition target, int offsetY) {
        if (offsetY < 0) {
            // 向下移动时，高密度材料可以挤开低密度材料
            return source.density > target.density;
        }

        if (offsetY > 0) {
            // 向上移动时，低密度材料可以挤开高密度材料
            return source.density < target.density;
        }

        // 水平位移需要明显密度差
        return Math.Abs(source.density - target.density) >= 8;
    }

    /// <summary>
    /// 更新下落帧数
    /// </summary>
    private void UpdateFallingFrames(Vector2Int fromPos, Vector2Int toPos, int offsetY) {
        if (offsetY < 0) {
            // 向下移动，增加下落帧数
            int frames = 0;
            fallingFrames.TryGetValue(fromPos, out frames);
            fallingFrames[toPos] = Math.Min(frames + 1, 64);
        } else {
            // 非向下移动，重置下落帧数
            fallingFrames[toPos] = 0;
        }
        fallingFrames.Remove(fromPos);
    }

    /// <summary>
    /// 标记位置已更新
    /// </summary>
    private void MarkUpdated(Vector2Int pos) {
        updatedThisFrame.Add(pos);
    }

    /// <summary>
    /// 更新液体体积
    /// </summary>
    private void UpdateVolume(long liquidId, Vector2Int pos, float volume) {
        OnUpdateVolume?.Invoke(liquidId, pos, volume);
    }

    /// <summary>
    /// 获取下落帧数（用于调试）
    /// </summary>
    public int GetFallingFrames(Vector2Int pos) {
        fallingFrames.TryGetValue(pos, out int frames);
        return frames;
    }

    /// <summary>
    /// 检查是否已更新（用于调试）
    /// </summary>
    public bool IsUpdatedThisFrame(Vector2Int pos) {
        return updatedThisFrame.Contains(pos);
    }
}
