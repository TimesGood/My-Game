using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

// 世界管理器 —— 图块查询与世界状态的中心枢纽
public class WorldManager : Singleton<WorldManager>, IMapSaveManager {
    public int seed;
    public Vector2Int worldSize = new Vector2Int(6000, 2000);
    public Vector2Int chunkCount = new Vector2Int(20, 20);

    // 图层 Tilemap 注册表（用于渲染——每个图层仍有一个 Unity Tilemap GameObject）
    private Dictionary<Layers, TilemapLayer> tileLayers = new Dictionary<Layers, TilemapLayer>();
    private ChunkManager chunkManager;

    public int baseHeight => (int)(worldSize.y * 0.7f);
    public int[] surfaceHeights { get; set; }
    public float[] terrainCurveData;
    public float[] stoneCurveData;

    public static Vector3Int[] directions = {
        Vector3Int.up,
        Vector3Int.down,
        Vector3Int.left,
        Vector3Int.right
    };

    private static bool _isInitialized;

    // ===== 图块注册表 =====

    public static class TileRegistry {
        private static Dictionary<long, TileClass> tileDictionary = new Dictionary<long, TileClass>();
        private static Dictionary<TileClass, long> reverseLookup = new Dictionary<TileClass, long>();

        public static long RegisterTile(TileClass tile) {
            if (tile == null) return 0;

            if (reverseLookup.TryGetValue(tile, out long id))
                return id;

            tileDictionary.Add(tile.blockId, tile);
            reverseLookup.Add(tile, tile.blockId);
            return tile.blockId;
        }

        public static TileClass GetTile(long id) {
            if (id == 0) return null;
            return tileDictionary.TryGetValue(id, out var tile) ? tile : null;
        }

        public static void ClearRegistry() {
            tileDictionary.Clear();
            reverseLookup.Clear();
        }
    }

    [RuntimeInitializeOnLoadMethod]
    private static void Initialize() {
        if (_isInitialized) return;
        TileRegistry.ClearRegistry();

        string[] assetNames = AssetDatabase.FindAssets("", new[] { "Assets/Data/Tiles" });
        int i = 0;
        foreach (string SOName in assetNames) {
            var SOpath = AssetDatabase.GUIDToAssetPath(SOName);
            var itemData = AssetDatabase.LoadAssetAtPath<TileClass>(SOpath);
            if (itemData == null) continue;
            TileRegistry.RegisterTile(itemData);
            i++;
        }

        _isInitialized = true;
        Debug.Log($"已注册 {i} 个图块");
    }

    protected override void Awake() {
        base.Awake();
        seed = -8211;
        WorldSetting.seed = seed;
        WorldSetting.worldSize = worldSize;
        WorldSetting.chunkCount = chunkCount;
    }

    private void Start() {
        InitWorld();
    }

    public void InitWorld() {
        // 查找 ChunkManager
        chunkManager = GetComponentInChildren<ChunkManager>();
        if (chunkManager == null) {
            Debug.LogError("未在 WorldManager 子对象中找到 ChunkManager！");
            return;
        }
        chunkManager.world = this;
        chunkManager.InitChunks();

        // 注册各图层的 Unity Tilemap（从 TilemapLayer 子对象中获取）
        TilemapLayer[] tilemapLayers = GetComponentsInChildren<TilemapLayer>();
        foreach (var tml in tilemapLayers) {
            if (!tileLayers.ContainsKey(tml.layer))
                tileLayers.Add(tml.layer, tml);
        }

        surfaceHeights = new int[worldSize.x];
        for (int x = 0; x < worldSize.x; x++) {
            surfaceHeights[x] = baseHeight;
        }
        stoneCurveData = new float[worldSize.x];
    }

    public bool CheckWorldBound(int x, int y) {
        return x >= 0 && x < worldSize.x && y >= 0 && y < worldSize.y;
    }


    // ===== 图块查询 =====

    public TilemapLayer GetTileLayer(Layers layer) {
        tileLayers.TryGetValue(layer, out TilemapLayer tileLayer);
        return tileLayer;
    }

    public TileClass GetTileClass(Layers layer, int x, int y) {
        if (!CheckWorldBound(x, y)) return null;
        long blockId = chunkManager.GetBlockId(layer, new Vector2Int(x, y));
        return TileRegistry.GetTile(blockId);
    }

    public bool SetTileClass(TileClass tileClass, Layers layer, int x, int y) {
        if (!CheckWorldBound(x, y)) return false;
        long tileId = tileClass == null ? 0 : tileClass.blockId;
        return chunkManager.SetBlockId(layer, new Vector2Int(x, y), tileId);
    }

    /// <summary>
    /// 获取世界坐标处的统一 TileData（新 API，一次调用获取全部图层）。
    /// </summary>
    public TileData GetTileData(int x, int y) {
        return chunkManager.GetTileData(new Vector2Int(x, y));
    }

    // ===== 地形 =====

    public float GetTerrain(int x) {
        return terrainCurveData[x];
    }
    public void SetTerrain(int x, float value) {
        terrainCurveData[x] = value;
    }


    // ===== 存档/读档 =====

    public void LoadData(MapData data) {
        Layers[] layers = (Layers[])Enum.GetValues(typeof(Layers));

        for (int cx = 0; cx < data.chunkDatas.GetLength(0); cx++) {
            for (int cy = 0; cy < data.chunkDatas.GetLength(1); cy++) {
                Vector2Int chunkCoord = new Vector2Int(cx, cy);
                long[,,] tileDatas = data.chunkDatas[cx, cy];
                int w = tileDatas.GetLength(1);
                int h = tileDatas.GetLength(2);

                TileData[,] tiles = new TileData[w, h];
                for (int x = 0; x < w; x++) {
                    for (int y = 0; y < h; y++) {
                        tiles[x, y] = new TileData
                        {
                            addonId = tileDatas[(int)Layers.Addons, x, y],
                            wallId = tileDatas[(int)Layers.Background, x, y],
                            groundId = tileDatas[(int)Layers.Ground, x, y],
                            liquidId = tileDatas[(int)Layers.Liquid, x, y],
                            // liquidVolume 和 growthData 在旧存档格式中丢失，
                            // 运行时由模拟系统重新计算
                        };
                    }
                }
                chunkManager.SetChunkTiles(chunkCoord, tiles);
            }
        }
        Debug.Log("Data loaded: " + data.chunkDatas.GetLength(1));
    }

    public void SaveData(ref MapData data) {
        Vector2Int cSize = new Vector2Int(WorldSetting.worldSize.x / chunkCount.x, WorldSetting.worldSize.y / chunkCount.y);
        Layers[] layers = (Layers[])Enum.GetValues(typeof(Layers));
        long[,][,,] result = new long[chunkCount.x, chunkCount.y][,,];

        for (int cx = 0; cx < chunkCount.x; cx++) {
            for (int cy = 0; cy < chunkCount.y; cy++) {
                Vector2Int chunkCoord = new Vector2Int(cx, cy);
                Chunk chunk = chunkManager.GetChunk(chunkCoord);
                long[,,] chunkResult = new long[layers.Length, cSize.x, cSize.y];

                if (chunk != null) {
                    for (int x = 0; x < cSize.x; x++) {
                        for (int y = 0; y < cSize.y; y++) {
                            TileData tile = chunk.tiles[x, y];
                            chunkResult[(int)Layers.Addons, x, y] = tile.addonId;
                            chunkResult[(int)Layers.Background, x, y] = tile.wallId;
                            chunkResult[(int)Layers.Ground, x, y] = tile.groundId;
                            chunkResult[(int)Layers.Liquid, x, y] = tile.liquidId;
                        }
                    }
                }
                result[cx, cy] = chunkResult;
            }
        }
        data.chunkDatas = result;
    }
}
