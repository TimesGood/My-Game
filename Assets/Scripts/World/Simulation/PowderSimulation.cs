using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 粉末/颗粒物理模拟，处理沙子、砾石等材料的下落和堆积
/// 借鉴 PixelAlchemy 的粉末移动规则设计
/// </summary>
public class PowderSimulation {
    private readonly ChunkManager chunkManager;
    private readonly MaterialPhysicsConfig physicsConfig;
    private readonly System.Random random;

    // 粉末更新回调
    public System.Action<long, Vector2Int, long, Vector2Int> OnSwapTiles;
    public System.Action<long, Vector2Int, long, Vector2Int> OnMoveTile;

    /// <summary>
    /// 创建粉末模拟实例
    /// </summary>
    /// <param name="chunkManager">区块管理器</param>
    /// <param name="physicsConfig">物理配置</param>
    /// <param name="seed">随机种子（0表示随机）</param>
    public PowderSimulation(ChunkManager chunkManager, MaterialPhysicsConfig physicsConfig, int seed = 0) {
        this.chunkManager = chunkManager;
        this.physicsConfig = physicsConfig;
        this.random = seed == 0 ? new System.Random() : new System.Random(seed);
    }

    /// <summary>
    /// 处理单个粉末格子的物理计算
    /// </summary>
    /// <param name="x">X坐标</param>
    /// <param name="y">Y坐标</param>
    /// <param name="grid">模拟网格</param>
    /// <returns>是否发生了变化</returns>
    public bool StepCell(int x, int y, SimulationGrid grid) {
        if (!grid.InBounds(x, y)) return false;

        TileData tileData = chunkManager.GetTileData(x, y);
        long groundId = tileData.groundId;

        // 检查是否为粉末材料
        var materialDef = physicsConfig.GetDefinition(groundId);
        if (materialDef == null || !materialDef.IsPowder) return false;

        // 检查移动概率
        if (random.NextDouble() > materialDef.moveProbability) return false;

        // 随机化对角线移动方向
        bool moveRightFirst = random.Next(0, 2) == 0;

        // 1. 尝试向下移动（优先级最高）
        if (TryMoveDown(x, y, groundId, materialDef)) {
            return true;
        }

        // 2. 尝试斜向下移动（粉末特性）
        if (TryMoveDiagonal(x, y, groundId, materialDef, moveRightFirst)) {
            return true;
        }

        // 3. 尝试沉入液体（密度驱动位移）
        if (TryDisplaceLiquid(x, y, groundId, materialDef)) {
            return true;
        }

        return false;
    }

    /// <summary>
    /// 尝试向下移动
    /// </summary>
    private bool TryMoveDown(int x, int y, long groundId, SimulationMaterialDefinition materialDef) {
        if (y <= 0) return false;

        Vector2Int downPos = new Vector2Int(x, y - 1);
        if (!chunkManager.CheckWorldBound(downPos.x, downPos.y)) return false;

        // 获取下方位置信息
        TileData downTile = chunkManager.GetTileData(downPos);
        long downGroundId = downTile.groundId;

        // 如果下方是空的，直接下落
        if (downGroundId == 0) {
            MoveTile(groundId, new Vector2Int(x, y), 0, downPos);
            return true;
        }

        // 如果下方是液体，检查是否可以沉入
        if (downTile.HasLiquid) {
            var downLiquidDef = physicsConfig.GetDefinition(downTile.liquidId);
            if (downLiquidDef != null && materialDef.density > downLiquidDef.density) {
                // 粉末沉入液体（交换位置）
                SwapWithLiquid(x, y, groundId, downPos, downTile.liquidId, downTile.liquidVolume);
                return true;
            }
        }

        // 如果下方是粉末材料，检查是否可以交换（高密度下沉）
        var downGroundDef = physicsConfig.GetDefinition(downGroundId);
        if (downGroundDef != null && downGroundDef.IsPowder) {
            if (materialDef.density > downGroundDef.density) {
                // 高密度粉末下沉
                SwapTiles(groundId, new Vector2Int(x, y), downGroundId, downPos);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 尝试斜向下移动
    /// </summary>
    private bool TryMoveDiagonal(int x, int y, long groundId, SimulationMaterialDefinition materialDef, bool moveRightFirst) {
        if (!materialDef.CanMoveDiagonal) return false;
        if (y <= 0) return false;

        int firstDirection = moveRightFirst ? 1 : -1;

        // 尝试向第一个方向斜向下移动
        if (TryMoveDiagonalDirection(x, y, groundId, materialDef, firstDirection)) {
            return true;
        }

        // 尝试向相反方向斜向下移动
        if (TryMoveDiagonalDirection(x, y, groundId, materialDef, -firstDirection)) {
            return true;
        }

        return false;
    }

    /// <summary>
    /// 尝试向指定方向斜向下移动
    /// </summary>
    private bool TryMoveDiagonalDirection(int x, int y, long groundId, SimulationMaterialDefinition materialDef, int direction) {
        int targetX = x + direction;
        int targetY = y - 1;

        if (!chunkManager.CheckWorldBound(targetX, targetY)) return false;

        // 获取目标位置信息
        TileData targetTile = chunkManager.GetTileData(targetX, targetY);
        long targetGroundId = targetTile.groundId;

        // 如果目标位置是空的，移动
        if (targetGroundId == 0) {
            MoveTile(groundId, new Vector2Int(x, y), 0, new Vector2Int(targetX, targetY));
            return true;
        }

        // 如果目标位置是液体，检查是否可以沉入
        if (targetTile.HasLiquid) {
            var targetLiquidDef = physicsConfig.GetDefinition(targetTile.liquidId);
            if (targetLiquidDef != null && materialDef.density > targetLiquidDef.density) {
                // 粉末沉入液体
                SwapWithLiquid(x, y, groundId, new Vector2Int(targetX, targetY), targetTile.liquidId, targetTile.liquidVolume);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 尝试沉入液体
    /// </summary>
    private bool TryDisplaceLiquid(int x, int y, long groundId, SimulationMaterialDefinition materialDef) {
        // 检查下方是否有液体
        if (y <= 0) return false;

        Vector2Int downPos = new Vector2Int(x, y - 1);
        if (!chunkManager.CheckWorldBound(downPos.x, downPos.y)) return false;

        TileData downTile = chunkManager.GetTileData(downPos);
        if (!downTile.HasLiquid) return false;

        var downLiquidDef = physicsConfig.GetDefinition(downTile.liquidId);
        if (downLiquidDef == null) return false;

        // 密度比较：粉末密度必须大于液体密度
        if (materialDef.density <= downLiquidDef.density) return false;

        // 粉末沉入液体（交换位置）
        SwapWithLiquid(x, y, groundId, downPos, downTile.liquidId, downTile.liquidVolume);
        return true;
    }

    /// <summary>
    /// 移动粉末到新位置
    /// </summary>
    private void MoveTile(long sourceId, Vector2Int sourcePos, long targetId, Vector2Int targetPos) {
        OnMoveTile?.Invoke(sourceId, sourcePos, targetId, targetPos);
    }

    /// <summary>
    /// 交换两个粉末位置
    /// </summary>
    private void SwapTiles(long sourceId, Vector2Int sourcePos, long targetId, Vector2Int targetPos) {
        OnSwapTiles?.Invoke(sourceId, sourcePos, targetId, targetPos);
    }

    /// <summary>
    /// 粉末与液体交换位置
    /// </summary>
    private void SwapWithLiquid(int x, int y, long groundId, Vector2Int liquidPos, long liquidId, float liquidVolume) {
        // 移除原位置的粉末
        chunkManager.SetBlockId(LayerType.Foreground, x, y, 0);

        // 在液体位置放置粉末
        chunkManager.SetBlockId(LayerType.Foreground, liquidPos.x, liquidPos.y, groundId);

        // 在原位置放置液体
        chunkManager.SetBlockId(LayerType.Liquid, x, y, liquidId);
        chunkManager.SetLiquidVolume(new Vector2Int(x, y), liquidVolume);
    }
}
