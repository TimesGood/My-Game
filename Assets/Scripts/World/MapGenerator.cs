using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using static ChunkHandler;
using static WorldGeneration;
using Random = UnityEngine.Random;

public class MapGenerator : Singleton<MapGenerator> {
    private MapMetadata metadata;
    public int seed;
    public Vector2Int mapSize = new Vector2Int(6000, 2000);
    public Vector2Int chunkCount; //x,y区块数量
    public Vector2Int chunkSize; //x,y区块大小
    private long[,][,,] chunkDatas;//地图瓦片Id

    public int baseHeight;//地形基准高度
    public int[] surfaceHeights { get; set; }//地形高度数据

    //世界生成
    public BaseTerrain baseTerrain;//基础地形
    public MapHorizontalLayout biomeTerrain;//地表群落 

    [ContextMenu("GenerateWorld")]
    public void Test() {
        Init();
        InitNoiseTexture();

        StartCoroutine(GenerateWorld());
    }

    //初始化世界属性
    public void Init() {
        seed = Random.Range(-10000, 10000);
        metadata = new MapMetadata {
            seed = seed,
            mapSize = mapSize,
            chunkCount = chunkCount,
            creationTime = DateTime.Now
        };
        chunkSize = metadata.GetChunkSize();
        baseHeight = (int)(mapSize.y * 0.7);
        //初始化高度
        surfaceHeights = new int[mapSize.x];
        for (int x = 0; x < mapSize.x; x++) {
            surfaceHeights[x] = baseHeight;
        }
        InitChunk();
    }

    #region 区块
    private void InitChunk() {
        chunkDatas = new long[metadata.chunkCount.x, metadata.chunkCount.y][,,];

        for (int chunkXIndex = 0; chunkXIndex < chunkCount.x; chunkXIndex++) {
            for (int chunkYIndex = 0; chunkYIndex < chunkCount.y; chunkYIndex++) {
                chunkDatas[chunkXIndex, chunkYIndex] = new long[Enum.GetValues(typeof(Layers)).Length, chunkSize.x, chunkSize.y];
                //chunkDatas.Add(new Vector2Int(chunkXIndex, chunkYIndex), new long[Enum.GetValues(typeof(Layers)).Length, chunkXSize, chunkYSize]);
            }
        }
    }

    //设置区块瓦片
    public void SetChunkTile(long tileId, Layers layer, int x, int y) {
        int chunkXIndex = x / chunkSize.x;
        int chunkYIndex = y / chunkSize.y;
        int tileXIndex = x - (chunkXIndex * chunkSize.x);
        int tileYIndex = y - (chunkYIndex * chunkSize.y);
        chunkDatas[chunkXIndex, chunkYIndex][(int)layer, tileXIndex, tileYIndex] = tileId;
    }

    //获取区块瓦片
    public long GetChunkTile(Layers layer, int x, int y) {
        int chunkXIndex = x / chunkSize.x;
        int chunkYIndex = y / chunkSize.y;
        int tileXIndex = x - (chunkXIndex * chunkSize.x);
        int tileYIndex = y - (chunkYIndex * chunkSize.y);
        long tileId = chunkDatas[chunkXIndex, chunkYIndex][(int)layer, tileXIndex, tileYIndex];
        return tileId;
    }
    #endregion

    #region 瓦片数据
    //设置瓦片数据
    public bool SetTileClass(TileClass tileClass, Layers layer, int x, int y) {
        if (!CheckWorldBound(x, y)) return false;
        long tileId = tileClass == null ? 0 : tileClass.blockId;
        SetChunkTile(tileId, layer, x, y);
        return true;
    }

    //获取指定位置瓦片
    public TileClass GetTileClass(Layers layer, int x, int y) {
        if (!CheckWorldBound(x, y)) return null;
        long tileId = GetChunkTile(layer, x, y);
        return TileRegistry.GetTile(tileId);
    }
    #endregion

    //校验坐标是否在世界范围内
    public bool CheckWorldBound(int x, int y) {
        if (x < 0 || x >= mapSize.x || y < 0 || y >= mapSize.y) return false;
        else return true;
    }


    //初始化噪音图

    private void InitNoiseTexture() {

        baseTerrain.InitNoiseTexture();
        biomeTerrain.InitLayout();
    }

    //生成世界
    public IEnumerator GenerateWorld() {
        Debug.Log("正在生成基础地形...");
        yield return StartCoroutine(baseTerrain.Generation());

        Debug.Log("正在生成群落地形...");
        yield return StartCoroutine(biomeTerrain.Generation());

        //Debug.Log("正在渲染光照...");
        //LightHandler.Instance.InitLight();

        Debug.Log("正在保存数据...");
        yield return StartCoroutine(TilemapExporter.Instance.ExportAllTilemaps(chunkDatas, process => { Debug.Log("保存进度:" + process); }));
        //保存游戏数据

    }
}
