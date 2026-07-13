using System.Collections.Generic;
using UnityEngine;
using static WorldManager;

/// <summary>
/// 区块数据管理中心——世界图块数据的唯一权威来源。
/// 替代原先分散在各 ConstructionLayer/LiquidLayer/AddonLayer 中的多个字典。
/// </summary>
public class ChunkManager : Singleton<ChunkManager> {
    [Header("世界配置")]
    public WorldManager world;

    // 世界中全部区块（仅数据，不负责渲染）
    private Dictionary<Vector2Int, Chunk> allChunks = new Dictionary<Vector2Int, Chunk>();

    // 区块尺寸（由 WorldSetting 派生）
    public Vector2Int chunkCount { get; private set; }
    public Vector2Int chunkSize { get; private set; }

    protected override void Awake() {
        base.Awake();
    }

    /// <summary>
    /// 根据世界设置初始化全部区块。在世界创建/加载时调用一次。
    /// </summary>
    public void InitChunks() {
        allChunks.Clear();

        chunkCount = WorldSetting.chunkCount;
        chunkSize = new Vector2Int(
            WorldSetting.worldSize.x / chunkCount.x,
            WorldSetting.worldSize.y / chunkCount.y);

        for (int cx = 0; cx < chunkCount.x; cx++) {
            for (int cy = 0; cy < chunkCount.y; cy++) {
                var coord = new Vector2Int(cx, cy);
                var chunk = new Chunk(
                    coord,
                    chunkSize.x,
                    chunkSize.y,
                    cx * chunkSize.x,
                    cy * chunkSize.y);
                allChunks[coord] = chunk;
            }
        }
    }

    // ===== 图块数据访问 =====

    /// <summary>
    /// 获取世界坐标处的完整 TileData。越界返回默认空值。
    /// </summary>
    public TileData GetTileData(Vector2Int worldPos) {
        if (!world.CheckWorldBound(worldPos.x, worldPos.y)) return default;

        if (TryGetChunk(worldPos, out Chunk chunk)) {
            if (chunk.TryGetTile(worldPos, out TileData tile))
                return tile;
        }
        return default;
    }

    public TileData GetTileData(int x, int y) {
        return GetTileData(new Vector2Int(x, y));
    }

    public TileClass GetTileClass(Layers layer, Vector2Int worldPos) {
        TileData tile = GetTileData(worldPos);
        return TileRegistry.GetTile(tile.GetBlockId(layer));
    }

    public TileClass GetTileClass(Layers layer, int x, int y) {
        return GetTileClass(layer, new Vector2Int(x, y));
    }

    public bool SetTileClass(Layers layer, Vector2Int worldPos, TileClass tile) {
        long blockId = tile == null ? 0 : tile.blockId;
        return SetBlockId(layer, worldPos, blockId);
    }

    public bool SetTileClass(Layers layer, int x, int y, TileClass tile) {
        return SetTileClass(layer, new Vector2Int(x, y), tile);
    }

    /// <summary>
    /// 获取指定图层在世界坐标处的 blockId。
    /// </summary>
    public long GetBlockId(Layers layer, Vector2Int worldPos) {
        TileData tile = GetTileData(worldPos);
        return tile.GetBlockId(layer);
    }

    public long GetBlockId(Layers layer, int x, int y) {
        return GetBlockId(layer, new Vector2Int(x, y));
    }

    public bool SetBlockId(Layers layer, int x, int y, long blockId) {
        Vector2Int worldPos = new Vector2Int(x, y);
        return SetBlockId(layer, worldPos, blockId);
    }

    /// <summary>
    /// 设置指定图层在世界坐标处的 blockId。越界返回 false。
    /// </summary>
    public bool SetBlockId(Layers layer, Vector2Int worldPos, long blockId) {
        if (!world.CheckWorldBound(worldPos.x, worldPos.y)) return false;

        if (TryGetChunk(worldPos, out Chunk chunk)) {
            if (chunk.TryGetTile(worldPos, out TileData tile)) {
                tile.SetBlockId(layer, blockId);
                chunk.TrySetTile(worldPos, tile);
                return true;
            }
        }
        return false;
    }

    // ===== 液体便捷访问 =====

    public float GetLiquidVolume(Vector2Int worldPos) {
        TileData tile = GetTileData(worldPos);
        return tile.liquidVolume;
    }

    public void SetLiquidVolume(Vector2Int worldPos, float volume) {
        if (TryGetChunk(worldPos, out Chunk chunk)) {
            if (chunk.TryGetTile(worldPos, out TileData tile)) {
                tile.liquidVolume = volume;
                if (volume == 0)
                    tile.liquidId = 0;
                chunk.TrySetTile(worldPos, tile);
            }
        }
    }

    public long GetLiquidId(Vector2Int worldPos) {
        return GetTileData(worldPos).liquidId;
    }

    public void SetLiquidId(Vector2Int worldPos, long liquidId) {
        if (TryGetChunk(worldPos, out Chunk chunk)) {
            if (chunk.TryGetTile(worldPos, out TileData tile)) {
                tile.liquidId = liquidId;
                if (liquidId == 0)
                    tile.liquidVolume = 0;
                chunk.TrySetTile(worldPos, tile);
            }
        }
    }

    // ===== 生长数据便捷访问 =====

    public int GetGrowthData(Vector2Int worldPos) {
        return GetTileData(worldPos).growthData;
    }

    public void SetGrowthData(Vector2Int worldPos, int data) {
        if (TryGetChunk(worldPos, out Chunk chunk)) {
            if (chunk.TryGetTile(worldPos, out TileData tile)) {
                tile.growthData = data;
                if (data == 0)
                    tile.addonId = 0;
                chunk.TrySetTile(worldPos, tile);
            }
        }
    }

    // ===== 区块级操作 =====

    /// <summary>
    /// 通过区块网格坐标获取区块。
    /// </summary>
    public Chunk GetChunk(Vector2Int chunkCoord) {
        allChunks.TryGetValue(chunkCoord, out Chunk chunk);
        return chunk;
    }

    /// <summary>
    /// 查找包含指定世界坐标的区块。
    /// </summary>
    public bool TryGetChunk(Vector2Int worldPos, out Chunk chunk) {
        Vector2Int chunkCoord = WorldToChunkCoord(worldPos);
        return allChunks.TryGetValue(chunkCoord, out chunk);
    }

    /// <summary>
    /// 世界坐标转区块网格坐标。
    /// </summary>
    public Vector2Int WorldToChunkCoord(Vector2Int worldPos) {
        return new Vector2Int(
            Mathf.FloorToInt((float)worldPos.x / chunkSize.x),
            Mathf.FloorToInt((float)worldPos.y / chunkSize.y));
    }

    public Vector2Int WorldToChunkCoord(Vector3 worldPos) {
        return new Vector2Int(
            Mathf.FloorToInt(worldPos.x / chunkSize.x),
            Mathf.FloorToInt(worldPos.y / chunkSize.y));
    }

    /// <summary>
    /// 获取全部区块（用于存档遍历）。
    /// </summary>
    public IEnumerable<Chunk> GetAllChunks() {
        return allChunks.Values;
    }

    // ===== 批量操作（供存档用） =====

    /// <summary>
    /// 一次性设置整个区块的图块数据（加载存档时使用）。
    /// </summary>
    public void SetChunkTiles(Vector2Int chunkCoord, TileData[,] data) {
        if (allChunks.TryGetValue(chunkCoord, out Chunk chunk)) {
            chunk.tiles = data;
            chunk.isDirty = true;
        }
    }
}
