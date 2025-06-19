using System.Collections;
using System.Collections.Generic;
using TreeEditor;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Tilemaps;

//区块数据管理
public class ChunkHandler : Singleton<ChunkHandler> {

    public WorldGeneration world;

    public int chunkCount = 20; //X和Y轴区块刻度
    public int chunkXCount;     //X轴区块数
    public int chunkYCount;     //Y轴区块数
    public int chunkXSize;      //X轴每区块像素大小
    public int chunkYSize;      //Y轴每区块像素大小

    public Camera renderCamera;
    public float padding = 2f;   // 视口外缓冲区块数
    private ChunkData[,] chunks;
    private List<ChunkData> activeChunks = new List<ChunkData>();


    private void Update() {
        UpdateVisibleChunks();
    }


    public class ChunkData {
        public Vector2Int coord;//区块坐标
        public Vector3Int[] tilePos;//区块瓦片坐标
        public int[] tileIDs;//瓦片Id
        public bool isLoaded;//是否已加载渲染
        public Bounds bounds;//区块范围盒
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



    // 计算摄像机边界
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

    // 更新可见分块
    private void UpdateVisibleChunks() {
        Bounds camBounds = GetCameraBounds();
        //List<ChunkData> newActiveChunks = new List<ChunkData>();

        // 遍历所有分块
        for (int x = 0; x < chunkXCount; x++) {
            for (int y = 0; y < chunkYCount; y++) {
                if (chunks[x, y].bounds.Intersects(camBounds)) {
                    if (!chunks[x, y].isLoaded) {
                        StartCoroutine(LoadChunkAsync(chunks[x, y]));
                    }
                    //newActiveChunks.Add(chunks[x, y]);
                } else {
                    if (chunks[x, y].isLoaded) {
                        StartCoroutine(UnloadChunk(chunks[x, y]));
                    }
                }
            }
        }
        // 更新活动分块列表
        //activeChunks = newActiveChunks;
    }

    
    //使用SetTilesBlock批量设置区块
    // 异步加载分块
    IEnumerator LoadChunkAsync(ChunkData chunk) {
        //List<Vector3Int>[] positionsByLayer = new List<Vector3Int>[4];//每层瓦片坐标
        List<TileBase>[] tilesByLayer = new List<TileBase>[4];//每层瓦片集
        for (int i = 0; i < 4; i++) {
            //positionsByLayer[i] = new List<Vector3Int>();
            tilesByLayer[i] = new List<TileBase>();
        }
        for (int i = 0; i < 4; i++) {
            //液体由LiquidHandler渲染，跳过
            if (i == (int)Layers.Liquid) continue;
            foreach (Vector3Int tilePos in chunk.tilePos) {
                TileClass tileClass = world.tileDatas[i, tilePos.x, tilePos.y];
                

                tilesByLayer[i].Add(tileClass?.tile);
                //positionsByLayer[i].Add(tilePos);
            }

            BoundsInt bound = ToBoundsInt(chunk.bounds);
            world.tilemaps[i].SetTilesBlock(bound, tilesByLayer[i].ToArray());

            tilesByLayer[i].Clear();
            //positionsByLayer[i].Clear();
            yield return null; // 每设置一层暂停一帧
        }
        chunk.isLoaded = true;
    }
    //转为Int包围盒
    private BoundsInt ToBoundsInt(Bounds bounds) {
        Vector3Int min = new Vector3Int(Mathf.FloorToInt(bounds.min.x), Mathf.FloorToInt(bounds.min.y), 0);
        Vector3Int size = new Vector3Int(Mathf.FloorToInt(bounds.size.x), Mathf.FloorToInt(bounds.size.y), 1);
        return new BoundsInt(min, size);
    }
    // 卸载分块
    IEnumerator UnloadChunk(ChunkData chunk) {
        for (int i = 0; i < 4; i++) {
            BoundsInt bound = ToBoundsInt(chunk.bounds);
            world.tilemaps[i].SetTilesBlock(bound, new TileBase[bound.size.x * bound.size.y]);
            yield return null; // 每设置一层暂停一帧
        }
        chunk.isLoaded = false;
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


}
