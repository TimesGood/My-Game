using System;
using System.Collections;
using System.Collections.Generic;
using TreeEditor;
using Unity.VisualScripting;
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

    public Camera renderCamera;
    public float padding = 2f;   // 视口外缓冲区块数
    private ChunkData[,] chunks;
    private Dictionary<Vector2Int, ChunkData> loadedChunks = new Dictionary<Vector2Int, ChunkData>();//储存已加载的区块
    private HashSet<Vector2Int> unloadingChunks = new HashSet<Vector2Int>(); // 跟踪正在卸载的区块
    private Vector2Int lastLoadedChunk = new Vector2Int(int.MinValue, int.MinValue);

    private Coroutine loadingCoroutine;

    // 添加区块状态枚举
    public enum ChunkState {
        Unloaded,
        Loading,
        Loaded,
        Unloading
    }


    public class ChunkData {
        public Vector2Int coord;//区块坐标
        public Vector3Int[] tilePos;//区块瓦片坐标
        public int[] tileIDs;//瓦片Id
        public ChunkState state = ChunkState.Unloaded;
        public Bounds bounds;//区块范围盒
    }

    private void Update() {
        Vector2Int currentChunk = WorldToChunkCoord(renderCamera.transform.position);

        if (currentChunk != lastLoadedChunk || Time.frameCount % 10 == 0) //每10帧强制检查
        {
            LoadChunksAroundCamera();
            lastLoadedChunk = currentChunk;
        }
    }


    //初始化分区
    public void InitChunk() {
        //分区
        chunkXCount = chunkCount;
        chunkYCount = chunkCount * world.worldHeight / world.worldWidth;
        chunks = new ChunkData[chunkXCount, chunkYCount];
        //每个区的瓦片
        chunkXSize = world.worldWidth / chunkXCount;
        chunkYSize = world.worldHeight / chunkYCount;
        for (int x = 0; x < chunkXCount; x++) {
            for (int y = 0; y < chunkYCount; y++) {
                chunks[x, y] = new ChunkData {
                    coord = new Vector2Int(x, y),
                    bounds = new Bounds(
                        new Vector3(
                            x * chunkXSize + chunkXSize / 2f,
                            y * chunkYSize + chunkYSize / 2f, 0),
                        new Vector3(chunkXSize, chunkYSize, 0))
                };
                GenerateChunkData(chunks[x, y]);
            }
        }
    }

    //生成区块瓦片坐标
    private void GenerateChunkData(ChunkData chunk) {
        List<Vector3Int> tilePos = new List<Vector3Int>();
        List<int> tileIds = new List<int>();
        //从y开始，方便使用SetTilesBlock
        for (int y = 0; y < chunkXSize; y++) {
            for (int x = 0; x < chunkYSize; x++) {
                Vector3Int tilemapPos = new Vector3Int(
                    chunk.coord.x * chunkXSize + x,
                    chunk.coord.y * chunkYSize + y,
                    0
                    );
                tilePos.Add(tilemapPos);
            }
        }
        chunk.tilePos = tilePos.ToArray();
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

        if (loadingCoroutine != null) {
            StopCoroutine(loadingCoroutine);
        }

        loadingCoroutine = StartCoroutine(UpdateVisibleChunks());
    }

    // 更新可见分块
    private IEnumerator UpdateVisibleChunks() {

        Vector2Int centerChunk = WorldToChunkCoord(renderCamera.transform.position);
        List<Vector2Int> chunksToLoad = new List<Vector2Int>();

        //以圆形进行渲染
        int radius = Mathf.CeilToInt(loadRadius);
        for (int y = -radius; y <= radius; y++) {
            for (int x = -radius; x <= radius; x++) {
                Vector2Int chunkID = centerChunk + new Vector2Int(x, y);

                if (Vector2Int.Distance(centerChunk, chunkID) > loadRadius)
                    continue;

                if (chunkID.x < 0 || chunkID.x >= chunkXCount ||
                    chunkID.y < 0 || chunkID.y >= chunkYCount)
                    continue;

                chunksToLoad.Add(chunkID);
            }
        }

        // 按距离排序，先加载最近的
        chunksToLoad.Sort((a, b) =>
            Vector2Int.Distance(centerChunk, a).CompareTo(Vector2Int.Distance(centerChunk, b)));

        //以摄像机矩形进行渲染
        //Bounds bounds = GetCameraBounds();
        //for (int y = (int)bounds.min.y; y <= bounds.max.y; y++) {
        //    for (int x = (int)bounds.min.x; x <= bounds.max.x; x++) {
        //        Vector2Int chunkID = GetChunkIndex(x, y);

        //        if (chunkID.x < 0 || chunkID.x >= chunkXCount ||
        //            chunkID.y < 0 || chunkID.y >= chunkYCount)
        //            continue;

        //        chunksToLoad.Add(chunkID);

        //    }
        //}

        // 卸载不在视野内的区块
        List<Vector2Int> toUnload = new List<Vector2Int>();
        foreach (var kvp in loadedChunks) {
            if (!chunksToLoad.Contains(kvp.Key) &&
                kvp.Value.state == ChunkState.Loaded) {
                toUnload.Add(kvp.Key);
            }
        }
        
        // 开始卸载
        foreach (var chunkID in toUnload) {
            if (unloadingChunks.Contains(chunkID)) continue;

            StartCoroutine(UnloadChunk(loadedChunks[chunkID]));
            unloadingChunks.Add(chunkID);
        }
        //int processed = 0;
        // 加载新区块
        foreach (var chunkID in chunksToLoad) {
            ChunkData chunk = chunks[chunkID.x, chunkID.y];

            if (chunk.state == ChunkState.Unloaded &&
                !unloadingChunks.Contains(chunkID)) {
                ////防止加载协程太多造成卡顿
                //if (processed++ % 5 == 0)
                //    yield return null;
                StartCoroutine(LoadChunkAsync(chunk));
            }
        }
        yield return null;
    }

    //使用SetTilesBlock批量设置区块
    // 异步加载分块
    IEnumerator LoadChunkAsync(ChunkData chunk) {
        // 如果区块正在卸载，等待卸载完成
        while (unloadingChunks.Contains(chunk.coord)) {
            yield return null;
        }

        chunk.state = ChunkState.Loading;

        Layers[] layers = (Layers[])Enum.GetValues(typeof(Layers));
        foreach (Layers layer in layers) {
            if (layer == Layers.Liquid) continue;

            List<TileBase> tileBases = new List<TileBase>();
            foreach (Vector3Int tilePos in chunk.tilePos) {
                TileClass tileClass = world.GetTileClass(layer, tilePos.x, tilePos.y);
                tileBases.Add(tileClass?.tile);
            }

            BoundsInt bound = ToBoundsInt(chunk.bounds);
            world.tilemaps[(int)layer].SetTilesBlock(bound, tileBases.ToArray());
            yield return null;
        }

        chunk.state = ChunkState.Loaded;

        // 确保只添加一次
        if (!loadedChunks.ContainsKey(chunk.coord)) {
            loadedChunks.Add(chunk.coord, chunk);
        }
    }
    //转为Int包围盒
    private BoundsInt ToBoundsInt(Bounds bounds) {
        Vector3Int min = new Vector3Int(Mathf.FloorToInt(bounds.min.x), Mathf.FloorToInt(bounds.min.y), 0);
        Vector3Int size = new Vector3Int(Mathf.FloorToInt(bounds.size.x), Mathf.FloorToInt(bounds.size.y), 1);
        return new BoundsInt(min, size);
    }
    // 卸载分块
    private IEnumerator UnloadChunk(ChunkData chunk) {
        // 如果区块正在加载，等待加载完成
        while (chunk.state == ChunkState.Loading) {
            yield return null;
        }

        chunk.state = ChunkState.Unloading;

        Layers[] layers = (Layers[])Enum.GetValues(typeof(Layers));
        foreach (Layers layer in layers) {
            BoundsInt bound = ToBoundsInt(chunk.bounds);
            world.tilemaps[(int)layer].SetTilesBlock(bound, new TileBase[bound.size.x * bound.size.y]);
            yield return null;
        }

        chunk.state = ChunkState.Unloaded;
        loadedChunks.Remove(chunk.coord);
        unloadingChunks.Remove(chunk.coord);
    }


    //渲染整个世界
    private IEnumerator LoadAllChunkAsync() {

        //==============================按区块渲染=============================

        for (int chunkXIndex = 0; chunkXIndex < chunkXCount; chunkXIndex++) {
            for (int chunkYIndex = 0; chunkYIndex < chunkYCount; chunkYIndex++) {

                StartCoroutine(LoadChunkAsync(chunks[chunkXIndex, chunkYIndex]));
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
    //获取实际坐标的所在区块索引
    public Vector2Int GetChunkIndex(int x, int y) {
        //利用向下取整获取区块索引
        int chunkXIndex = x / chunkXSize;
        int chunkYIndex = y / chunkYSize;
        return new Vector2Int(chunkXIndex, chunkYIndex);
    }

    //获取指定区块实际瓦片坐标
    public Vector3Int GetActualIndex(int chunkXIndex, int chunkYIndex, int chunkTileXIndex, int chunkTileYIndex) {
        int x = chunkTileXIndex + (chunkXIndex * chunkXSize);
        int y = chunkTileYIndex + (chunkYIndex * chunkXSize);
        return new Vector3Int(x, y);
    }

    public ChunkData[,] GetChunkDatas() {
        return chunks;
    }

}