using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

// 区块渲染管理器 —— 根据摄像机位置加载/卸载 Unity Tilemap 图块
public class ChunkHandler : Singleton<ChunkHandler> {
    public WorldManager world;
    public float loadRadius = 3f;

    public int chunkCount = 20;
    public int chunkXCount;
    public int chunkYCount;
    public int chunkXSize;
    public int chunkYSize;
    private static TileBase[] emptyTiles;
    public int maxCachedChunks = 50;

    public Camera renderCamera;
    public float padding = 2f;

    public HashSet<Vector2Int> loadedChunkIDs = new HashSet<Vector2Int>();
    private Vector2Int lastLoadedChunk = new Vector2Int(int.MinValue, int.MinValue);
    private Dictionary<Vector2Int, ChunkRenderData> chunkDataCache = new Dictionary<Vector2Int, ChunkRenderData>();

    private Coroutine unloadingCoroutine;
    private Coroutine loadingCoroutine;
    private bool applyAll = false;

    // 区块渲染数据（GPU 图块，非数据）
    public class ChunkRenderData {
        public Vector2Int coord; // 区块坐标
        public List<TileBase>[] tileBases; // 逐层图块数组，供 SetTilesBlock 使用
        public BoundsInt bounds; // 区块范围盒
        public DateTime lastAccessTime; // 追后访问时间（用于混村管理）
    }

    protected override void Awake() {
        base.Awake();

        chunkXCount = chunkCount;
        chunkYCount = chunkCount * world.worldSize.y / world.worldSize.x;

        chunkXSize = world.worldSize.x / chunkXCount;
        chunkYSize = world.worldSize.y / chunkYCount;
        // 空瓦片数组，用于卸载
        emptyTiles = new TileBase[chunkXSize * chunkYSize];
    }

    private void Update() {
        if (applyAll) return;
        Vector2Int currentChunk = WorldToChunkCoord(renderCamera.transform.position);

        // 当玩家移动到新区块是重新加载
        if (currentChunk != lastLoadedChunk) {
            LoadChunksAroundCamera();
            lastLoadedChunk = currentChunk;
        }
    }

    public ChunkRenderData GetChunkRenderData(int chunkX, int chunkY) {
        Vector2Int coord = new Vector2Int(chunkX, chunkY);

        // 已修复：启用缓存查找
        if (!chunkDataCache.TryGetValue(coord, out var data)) {
            data = BuildChunkRenderData(chunkX, chunkY);
            chunkDataCache[coord] = data;
            CleanupChunkCache(); // 清理缓存
        }

        data.lastAccessTime = DateTime.Now;
        return data;
    }
    // 清理过期的区块缓存
    private void CleanupChunkCache() {
        if (chunkDataCache.Count <= maxCachedChunks) return;
        // 移除最久未使用的区块
        var chunksToRemove = chunkDataCache
            .OrderBy(x => x.Value.lastAccessTime)
            .Take(chunkDataCache.Count - maxCachedChunks)
            .ToList();

        foreach (var chunk in chunksToRemove) {
            chunkDataCache.Remove(chunk.Key);
        }
    }

    // 从 ChunkManager 的统一 TileData 构建渲染数据
    private ChunkRenderData BuildChunkRenderData(int chunkX, int chunkY) {
        ChunkRenderData data = new ChunkRenderData {
            coord = new Vector2Int(chunkX, chunkY),
            bounds = new BoundsInt(
                new Vector3Int(chunkX * chunkXSize, chunkY * chunkYSize, 0),
                new Vector3Int(chunkXSize, chunkYSize, 1))
        };

        Layers[] layers = (Layers[])Enum.GetValues(typeof(Layers));
        List<TileBase>[] tileBases = new List<TileBase>[layers.Length];

        for (int i = 0; i < layers.Length; i++) {
            tileBases[i] = new List<TileBase>();
        }

        // 已修复：Y 循环使用 chunkYSize（原先错误地使用了 chunkXSize）
        for (int y = 0; y < chunkYSize; y++) {
            for (int x = 0; x < chunkXSize; x++) {
                int worldX = chunkX * chunkXSize + x;
                int worldY = chunkY * chunkYSize + y;

                // 一次调用获取全部图层数据
                TileData tileData = world.GetTileData(worldX, worldY);

                foreach (var layer in layers) {
                    long blockId = tileData.GetBlockId(layer);
                    TileBase tile = null;

                    if (blockId != 0) {
                        TileClass tileClass = WorldManager.TileRegistry.GetTile(blockId);
                        if (tileClass != null) {
                            if (layer == Layers.Liquid) {
                                tile = ((LiquidClass)tileClass)?.GetTileToVolume(tileData.liquidVolume);
                            } else {
                                tile = tileClass.tile;
                            }
                        }
                    }

                    tileBases[(int)layer].Add(tile);
                }
            }
        }

        data.tileBases = tileBases;
        return data;
    }

    // 摄像机视锥体范围，用于判断区块可见性
    public Bounds GetCameraBounds() {
        Vector3[] frustumCorners = new Vector3[4];
        renderCamera.CalculateFrustumCorners(
            new Rect(0, 0, 1, 1),
            renderCamera.farClipPlane,
            Camera.MonoOrStereoscopicEye.Mono,
            frustumCorners);

        Matrix4x4 camMatrix = renderCamera.transform.localToWorldMatrix;
        for (int i = 0; i < 4; i++) {
            frustumCorners[i] = camMatrix.MultiplyPoint(frustumCorners[i]);
            frustumCorners[i].z = 0;
        }

        Bounds bounds = new Bounds(frustumCorners[0], Vector3.zero);
        foreach (Vector3 corner in frustumCorners) {
            bounds.Encapsulate(corner);
        }

        // 扩展范围
        bounds.Expand(padding * chunkXSize);
        return bounds;
    }

    // 获取摄像机加载半径内的区块
    public List<Vector2Int> GetCenterLoadChunk() {
        Vector2Int centerChunk = WorldToChunkCoord(renderCamera.transform.position);
        // 获取范围内需要加载的区块
        List<Vector2Int> chunksToLoad = new List<Vector2Int>();
        int radius = Mathf.CeilToInt(loadRadius);

        for (int y = -radius; y <= radius; y++) {
            for (int x = -radius; x <= radius; x++) {
                Vector2Int chunkID = centerChunk + new Vector2Int(x, y);
                // 加载圆形区域内的区块
                if (Vector2Int.Distance(centerChunk, chunkID) <= loadRadius) {
                    // 越界
                    if (chunkID.x >= chunkXCount || chunkID.x < 0 ||
                        chunkID.y >= chunkYCount || chunkID.y < 0) continue;
                    chunksToLoad.Add(chunkID);
                }
            }
        }

        // 按距离排序（近者优先）
        chunksToLoad.Sort((a, b) =>
            Vector2Int.Distance(centerChunk, a).CompareTo(Vector2Int.Distance(centerChunk, b)));
        return chunksToLoad;
    }

    private void LoadChunksAroundCamera() {
        List<Vector2Int> chunksToLoad = GetCenterLoadChunk();

        List<Vector2Int> toUnload = new List<Vector2Int>();
        foreach (var chunkID in loadedChunkIDs) {
            if (!chunksToLoad.Contains(chunkID))
                toUnload.Add(chunkID);
        }

        if (unloadingCoroutine != null) StopCoroutine(unloadingCoroutine);
        unloadingCoroutine = StartCoroutine(UpdateUnLoadChunks(toUnload));

        if (loadingCoroutine != null) StopCoroutine(loadingCoroutine);
        loadingCoroutine = StartCoroutine(UpdateLoadChunks(chunksToLoad));
    }

    // 加载视野内的区块
    private IEnumerator UpdateLoadChunks(List<Vector2Int> visiblePos) {
        int processed = 0;
        foreach (var chunkID in visiblePos) {
            if (loadedChunkIDs.Contains(chunkID)) continue;
            //每加载5个区块停一帧
            if (processed++ % 5 == 0)
                yield return null;
            StartCoroutine(LoadChunk(chunkID));
        }
    }

    // 卸载已加载的区块
    private IEnumerator UpdateUnLoadChunks(List<Vector2Int> chunkIDs) {
        int processed = 0;
        foreach (var chunkID in chunkIDs) {
            if (processed++ % 5 == 0)
                yield return null;
            StartCoroutine(UnloadChunk(chunkID));
        }
    }

    // 加载区块
    IEnumerator LoadChunk(Vector2Int chunkID) {
        Layers[] layers = (Layers[])Enum.GetValues(typeof(Layers));
        ChunkRenderData data = GetChunkRenderData(chunkID.x, chunkID.y);

        foreach (Layers layer in layers) {
            world.GetTilemap(layer).SetTilesBlock(data.bounds, data.tileBases[(int)layer].ToArray());
            yield return null;
        }

        loadedChunkIDs.Add(chunkID);
    }

    // 卸载区块
    private IEnumerator UnloadChunk(Vector2Int chunkID) {
        ChunkRenderData data = GetChunkRenderData(chunkID.x, chunkID.y);
        Layers[] layers = (Layers[])Enum.GetValues(typeof(Layers));

        foreach (Layers layer in layers) {
            world.GetTilemap(layer).SetTilesBlock(data.bounds, emptyTiles);
            world.GetTilemap(layer).CompressBounds();
            yield return null;
        }
        loadedChunkIDs.Remove(chunkID);
    }


    //渲染整个世界
    [ContextMenu("ApplyAll")]
    private void ApplyAll() {
        applyAll = true;
        StartCoroutine(LoadAllChunkAsync());
    }

    private IEnumerator LoadAllChunkAsync() {
        for (int cx = 0; cx < chunkXCount; cx++) {
            for (int cy = 0; cy < chunkYCount; cy++) {
                StartCoroutine(LoadChunk(new Vector2Int(cx, cy)));
                yield return null;
            }
        }
    }

    // 世界坐标转区块坐标
    private Vector2Int WorldToChunkCoord(Vector3 worldPos) {
        return new Vector2Int(
            Mathf.FloorToInt(worldPos.x / chunkXSize),
            Mathf.FloorToInt(worldPos.y / chunkYSize));
    }

    public Vector2Int WorldToChunkCoord(Vector2Int worldPos) {
        return new Vector2Int(
            Mathf.FloorToInt(worldPos.x / chunkXSize),
            Mathf.FloorToInt(worldPos.y / chunkYSize));
    }
    // 获取指定区块实际瓦片坐标
    public Vector3Int GetActualIndex(int chunkXIndex, int chunkYIndex, int chunkTileXIndex, int chunkTileYIndex) {
        return new Vector3Int(
            chunkTileXIndex + (chunkXIndex * chunkXSize),
            chunkTileYIndex + (chunkYIndex * chunkYSize));
    }
}
