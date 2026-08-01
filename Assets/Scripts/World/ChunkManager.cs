using System.Collections.Generic;
using System.Drawing;
using Unity.Entities;
using UnityEngine;
/// <summary>
/// 区块数据管理中心——世界图块数据的唯一权威来源。
/// 替代原先分散在各 ConstructionLayer/LiquidLayer/AddonLayer 中的多个字典。
/// </summary>
public class ChunkManager : Singleton<ChunkManager>, IMapSaveManager {

    public int Width { get; private set; }
    public int Height { get; private set; }

    // 世界中全部区块（仅数据，不负责渲染）
    private Dictionary<Vector2Int, Chunk> allChunks = new Dictionary<Vector2Int, Chunk>();

    // 区块尺寸
    //public Vector2Int chunkCount = new Vector2Int(20, 20);
    public Vector2Int chunkSize;

    public WorldMeta worldMeta { get; private set; }
    public bool IsReady { get; private set; }

    protected override void Awake() {
        base.Awake();
    }

    public void InitializeNewWorld(WorldMeta meta) {
        worldMeta = meta;
        Width = meta.width;
        Height = meta.height;
        allChunks.Clear();
        Vector2Int chunkCount = new Vector2Int(
            Mathf.CeilToInt(Width / chunkSize.x),
            Mathf.CeilToInt(Height / chunkSize.y)
        );
        //chunkSize = new Vector2Int(
        //    width / chunkCount.x,
        //    height / chunkCount.y);

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
        IsReady = true;
    }

    /// <summary>加载已有世界元数据</summary>
    public void LoadExistingWorld(WorldMeta meta) {
        worldMeta = meta;
        Width = meta.width;
        Height = meta.height;

        allChunks.Clear();




        IsReady = true;
        Debug.Log($"[WDC] 存档世界已就绪: {meta.worldName}");
    }

    // ===== 图块数据访问 =====
    public bool CheckWorldBound(Vector2Int worldPos) {
        return CheckWorldBound(worldPos.x, worldPos.y);
    }
    public bool CheckWorldBound(int x, int y) {
        return x >= 0 && x < Width && y >= 0 && y < Height;
    }

    /// <summary>
    /// 获取世界坐标处的完整 TileData。越界返回默认空值。
    /// </summary>
    public TileData GetTileData(Vector2Int worldPos) {
        if (!CheckWorldBound(worldPos.x, worldPos.y)) return default;

        if (TryGetChunk(worldPos, out Chunk chunk)) {
            if (chunk.TryGetTile(worldPos, out TileData tile))
                return tile;
        }
        return default;
    }

    public TileData GetTileData(int x, int y) {
        return GetTileData(new Vector2Int(x, y));
    }

    public TileClass GetTileClass(LayerType layer, Vector2Int worldPos) {
        TileData tile = GetTileData(worldPos);
        return TileRegistry_.GetTile(tile.GetBlockId(layer));
    }

    public TileClass GetTileClass(LayerType layer, int x, int y) {
        return GetTileClass(layer, new Vector2Int(x, y));
    }

    public bool SetTileClass(LayerType layer, Vector2Int worldPos, TileClass tile) {
        long blockId = tile == null ? 0 : tile.blockId;
        return SetBlockId(layer, worldPos, blockId);
    }

    public bool SetTileClass(LayerType layer, int x, int y, TileClass tile) {
        return SetTileClass(layer, new Vector2Int(x, y), tile);
    }

    /// <summary>
    /// 获取指定图层在世界坐标处的 blockId。
    /// </summary>
    public long GetBlockId(LayerType layer, Vector2Int worldPos) {
        TileData tile = GetTileData(worldPos);
        return tile.GetBlockId(layer);
    }

    public long GetBlockId(LayerType layer, int x, int y) {
        return GetBlockId(layer, new Vector2Int(x, y));
    }

    public bool SetBlockId(LayerType layer, int x, int y, long blockId) {
        Vector2Int worldPos = new Vector2Int(x, y);
        return SetBlockId(layer, worldPos, blockId);
    }

    /// <summary>
    /// 设置指定图层在世界坐标处的 blockId。越界返回 false。
    /// </summary>
    public bool SetBlockId(LayerType layer, Vector2Int worldPos, long blockId) {
        if (!CheckWorldBound(worldPos.x, worldPos.y)) return false;

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

    public void LoadData(GameData data) {
        List<Chunk> chunks = data.chunks;
        if (chunks == null) return;
        foreach (var chunk in chunks) {
            allChunks.Add(chunk.coord, chunk);
        }
    }

    public void SaveData(ref GameData data) {
        List<Chunk> chunks = new List<Chunk>();
        foreach (var chunk in allChunks.Values) {
            chunks.Add(chunk);
        }
        data.chunks = chunks;

    }
}
