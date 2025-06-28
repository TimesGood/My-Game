using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Tilemaps;

//区块数据管理
public class ChunkHandler : Singleton<ChunkHandler> {

    public WorldGeneration world;
    public float loadRadius = 3f;//加载范围

    public int chunkCount = 20; //X和Y轴区块数量
    public int chunkXCount;     //X轴区块数
    public int chunkYCount;     //Y轴区块数
    public int chunkXSize;      //X轴每区块像素大小
    public int chunkYSize;      //Y轴每区块像素大小
    private static TileBase[] emptyTiles;
    public int maxCachedChunks = 50; // 最大缓存区块数

    public Camera renderCamera; // 摄像机（跟踪目标）
    public float padding = 2f;   // 视口外缓冲区块数

    private HashSet<Vector2Int> loadedChunkIDs = new HashSet<Vector2Int>();//跟踪已加载的区块
    private Vector2Int lastLoadedChunk = new Vector2Int(int.MinValue, int.MinValue);
    private Dictionary<Vector2Int, ChunkData> chunkDataCache = new Dictionary<Vector2Int, ChunkData>();//区块数据缓存，避免重复创建


    private Coroutine loadingCoroutine;

    //区块数据
    public class ChunkData {
        public Vector2Int coord;//区块坐标
        public List<TileBase>[] tileBases;//区块瓦片集
        public BoundsInt bounds;//区块范围盒
        public DateTime lastAccessTime;  // 最后访问时间（用于缓存管理）
    }

    protected override void Awake() {
        base.Awake();
        //分区
        chunkXCount = chunkCount;
        chunkYCount = chunkCount * world.worldHeight / world.worldWidth;
        //每个区的瓦片
        chunkXSize = world.worldWidth / chunkXCount;
        chunkYSize = world.worldHeight / chunkYCount;
        //空瓦片数组，用于卸载
        emptyTiles = new TileBase[chunkXSize * chunkXSize];

    }

    private void Update() {
        Vector2Int currentChunk = WorldToChunkCoord(renderCamera.transform.position);

        // 当玩家移动到新区块时重新加载
        if (currentChunk != lastLoadedChunk) {
            LoadChunksAroundCamera();
            lastLoadedChunk = currentChunk;
        }
    }

    private ChunkData GetChunkData(int chunkX, int chunkY) {
        Vector2Int coord = new Vector2Int(chunkX, chunkY);

        if (!chunkDataCache.TryGetValue(coord, out var data)) {
            data = BuildChunkData(chunkX, chunkY);
            chunkDataCache[coord] = data;
            CleanupChunkCache(); // 清理缓存
        }

        data.lastAccessTime = DateTime.Now;
        return data;
    }
    // 清理过期的区块缓存
    private void CleanupChunkCache() {
        if (chunkDataCache.Count <= maxCachedChunks)
            return;

        // 移除最久未使用的区块
        var chunksToRemove = chunkDataCache.OrderBy(x => x.Value.lastAccessTime)
                                          .Take(chunkDataCache.Count - maxCachedChunks)
                                          .ToList();

        foreach (var chunk in chunksToRemove) {
            chunkDataCache.Remove(chunk.Key);
        }
    }
    //构建区块数据
    private ChunkData BuildChunkData(int chunkX, int chunkY) {
        ChunkData chunk = new ChunkData {
            coord = new Vector2Int(chunkX, chunkY),
            bounds = new BoundsInt(
                        new Vector3Int(
                            chunkX * chunkXSize,
                            chunkY * chunkYSize, 0),
                        new Vector3Int(chunkXSize, chunkYSize, 1))
        };
        Layers[] layers = (Layers[])Enum.GetValues(typeof(Layers));
        List<TileBase>[] tileBases = new List<TileBase>[layers.Length];
        //从y开始，方便使用SetTilesBlock
        for (int y = 0; y < chunkXSize; y++) {
            for (int x = 0; x < chunkYSize; x++) {
                int worldXPos = chunk.coord.x * chunkXSize + x;
                int worldYPos = chunk.coord.y * chunkYSize + y;
                foreach (var layer in layers) {
                    TileClass tileClass = world.GetTileClass(layer, worldXPos, worldYPos);
                    if (tileBases[(int)layer] == null) tileBases[(int)layer] = new List<TileBase>();
                    tileBases[(int)layer].Add(tileClass == null ? null : tileClass.tile);
                }

            }
        }
        chunk.tileBases = tileBases;
        return chunk;
    }

    // 获取摄像机矩阵
    public Bounds GetCameraBounds() {
        Vector3[] frustumCorners = new Vector3[4];
        renderCamera.CalculateFrustumCorners(
            new Rect(0, 0, 1, 1),
            renderCamera.farClipPlane,
            Camera.MonoOrStereoscopicEye.Mono,
            frustumCorners
        );

        Matrix4x4 camMatrix = renderCamera.transform.localToWorldMatrix;
        for (int i = 0; i < 4; i++) {
            frustumCorners[i] = camMatrix.MultiplyPoint(frustumCorners[i]);
            frustumCorners[i].z = 0;
        }

        Bounds bounds = new Bounds(frustumCorners[0], Vector3.zero);
        foreach (Vector3 corner in frustumCorners) {
            bounds.Encapsulate(corner);
        }

        // 扩展边界范围
        bounds.Expand(padding * chunkXSize);

        return bounds;
    }

    private void LoadChunksAroundCamera() {

        Vector2Int centerChunk = WorldToChunkCoord(renderCamera.transform.position);
        // 查找范围内区块
        List<Vector2Int> chunksToLoad = new List<Vector2Int>();
        int radius = Mathf.CeilToInt(loadRadius);
        for (int y = -radius; y <= radius; y++) {
            for (int x = -radius; x <= radius; x++) {
                Vector2Int chunkID = centerChunk + new Vector2Int(x, y);

                // 只加载圆形区域内的区块
                if (Vector2Int.Distance(centerChunk, chunkID) <= loadRadius) {
                    //越界
                    if (chunkID.x >= chunkXCount || chunkID.x < 0 || chunkID.y >= chunkYCount || chunkID.y < 0) continue;
                    chunksToLoad.Add(chunkID);
                }
            }
        }


        List<Vector2Int> toUnload = new List<Vector2Int>();
        foreach (var chunkID in loadedChunkIDs) {
            if (!chunksToLoad.Contains(chunkID)) {
                toUnload.Add(chunkID);
            }
        }


        UpdateUnLoadChunks(toUnload);
        // 按优先级排序 (中心优先)
        chunksToLoad.Sort((a, b) =>
            Vector2Int.Distance(centerChunk, a).CompareTo(Vector2Int.Distance(centerChunk, b)));

        if (loadingCoroutine != null) StopCoroutine(loadingCoroutine);
        loadingCoroutine = StartCoroutine(UpdateLoadChunks(chunksToLoad));

    }

    //加载视野内的区块
    private IEnumerator UpdateLoadChunks(List<Vector2Int> visiblePos) {
        
        int processed = 0;
        foreach (var chunkID in visiblePos) {
            if (loadedChunkIDs.Contains(chunkID)) continue;
            
            //防止加载的区块协程太多，导致卡顿。不过调太小也会导致加载速度太慢，跟不上玩家速度
            if (processed++ % 5 == 0)
                yield return null;
            StartCoroutine(LoadChunk(chunkID));

        }
    }
    //卸载已加载的区块
    private void UpdateUnLoadChunks(List<Vector2Int> chunkIDs) {
        foreach (var chunkID in chunkIDs) {
            
            StartCoroutine(UnloadChunk(chunkID));
        }
    }

    // 加载区块
    IEnumerator LoadChunk(Vector2Int chunkID) {
        Layers[] layers = (Layers[])Enum.GetValues(typeof(Layers));
        ChunkData chunkData = GetChunkData(chunkID.x, chunkID.y);
        foreach (Layers layer in layers) {

            BoundsInt bound = chunkData.bounds;
            world.tilemaps[(int)layer].SetTilesBlock(bound, chunkData.tileBases[(int)layer].ToArray());
            yield return null; // 每设置一层暂停一帧
        }

        loadedChunkIDs.Add(chunkID);
    }

    // 卸载区块
    private IEnumerator UnloadChunk(Vector2Int chunkID) {
        ChunkData chunkData = GetChunkData(chunkID.x, chunkID.y);
        Layers[] layers = (Layers[])Enum.GetValues(typeof(Layers));

        foreach (Layers layer in layers) {
            BoundsInt bound = chunkData.bounds;
            world.tilemaps[(int)layer].SetTilesBlock(chunkData.bounds, emptyTiles);
            world.tilemaps[(int)layer].CompressBounds();
            yield return null; // 每设置一层暂停一帧
        }

        loadedChunkIDs.Remove(chunkID);
    }


    //渲染整个世界
    private IEnumerator LoadAllChunkAsync() {

        //==============================按区块渲染=============================

        for (int chunkXIndex = 0; chunkXIndex < chunkXCount; chunkXIndex++) {
            for (int chunkYIndex = 0; chunkYIndex < chunkYCount; chunkYIndex++) {

                StartCoroutine(LoadChunk(new Vector2Int(chunkXIndex, chunkYIndex)));
                yield return null; // 每设置一区块暂停一帧 
            }
        }
    }

    //世界坐标转区块索引
    private Vector2Int WorldToChunkCoord(Vector3 worldPos) {
        return new Vector2Int(
            Mathf.FloorToInt(worldPos.x / chunkXSize),
            Mathf.FloorToInt(worldPos.y / chunkYSize)
        );
    }

    //获取指定区块实际瓦片坐标
    public Vector3Int GetActualIndex(int chunkXIndex, int chunkYIndex, int chunkTileXIndex, int chunkTileYIndex) {
        int x = chunkTileXIndex + (chunkXIndex * chunkXSize);
        int y = chunkTileYIndex + (chunkYIndex * chunkXSize);
        return new Vector3Int(x, y);
    }

    //获取区块数据待定
    public ChunkData[,] GetChunkDatas() {
        return null;
    }

}