using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

//地图生成器
public class WorldGeneration : Singleton<WorldGeneration>, ISaveManager
{
    public int seed;//世界种子
    public int worldWidth = 2000;//世界宽度
    public int worldHeight = 1000;//世界高度

    
    public Tilemap[] tilemaps;//瓦片地图集
    //private long[,,] tileIds;//地图瓦片Id
    //private long[,][,,] chunkDatas { get; set; }//瓦片分区数据集
    private Dictionary<Vector2Int, long[,,]> chunkDatas = new Dictionary<Vector2Int, long[,,]>(); 
    public bool openChunk = false;

    public int baseHeight => (int)(worldHeight * 0.7);//地形基准高度
    public int[] surfaceHeights { get; set; }//地形高度数据

    //世界生成
    public BaseTerrain baseTerrain;
    public BiomeTerrain biomeTerrain;

    //初始化方法
    private static bool _isInitialized;

    // 瓦片注册表
    public static class TileRegistry {
        private static Dictionary<long, TileClass> tileDictionary = new Dictionary<long, TileClass>();
        private static Dictionary<TileClass, long> reverseLookup = new Dictionary<TileClass, long>();

        public static long RegisterTile(TileClass tile) {
            if (tile == null) return 0;

            if (reverseLookup.TryGetValue(tile, out long id)) {
                return id;
            }

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


    //初始化时执行，获取瓦片资产数据
    [RuntimeInitializeOnLoadMethod]
    private static void Initialize() {
        if (_isInitialized) return;
        TileRegistry.ClearRegistry();

        //TileClass[] allTiles = Resources.LoadAll<TileClass>("Tiles");
        //注册瓦片资产
        //查找目录中的装备数据，返回的是GUID
        string[] assetNames = AssetDatabase.FindAssets("", new[] { "Assets/Old/Tiles" });
        int i = 0;
        foreach (string SOName in assetNames) {
            var SOpath = AssetDatabase.GUIDToAssetPath(SOName);//GUID转为物件实际项目路径
            var itemData = AssetDatabase.LoadAssetAtPath<TileClass>(SOpath);//根据路径读取指定物件
            if (itemData == null) continue;
            TileRegistry.RegisterTile(itemData);
            i++;

        }

        _isInitialized = true;
        Debug.Log($"已注册 {i} 个图块");
    }

    private void Start() {
        InitWorld();
        if (MapSaveManager.Instance.HasSaveData()) {
            Debug.Log("加载世界");
            MapSaveManager.Instance.LoadGame();
        } else {
            Debug.Log("生成世界");
            MapSaveManager.Instance.NewGame();
            StartCoroutine(GenerateWorld());
        }
    }

    //初始化世界属性
    public void InitWorld() {
        //seed = Random.Range(-10000, 10000);
        seed = -2366;
        InitNoiseTexture();
        //初始化高度
        surfaceHeights = new int[worldWidth];
        for (int x = 0; x < worldWidth; x++) {
            surfaceHeights[x] = baseHeight;
        }
        //tileDatas = new TileClass[4, worldWidth, worldHeight];
        //tileIds = new long[Enum.GetValues(typeof(Layers)).Length, worldWidth, worldHeight];
        InitChunk();
    }

    //校验坐标是否在世界范围内
    public bool CheckWorldBound(int x, int y) {
        if (x < 0 || x >= worldWidth || y < 0 || y >= worldHeight) return false;
        else return true;
    }


    //初始化噪音图

    private void InitNoiseTexture() {

        baseTerrain.InitNoiseTexture();
        biomeTerrain.InitNoiseTexture();
    }


    //生成世界
    public IEnumerator GenerateWorld() {
        Debug.Log("正在生成基础地形...");
        yield return StartCoroutine(baseTerrain.Generation());

        Debug.Log("正在生成群落地形...");
        yield return StartCoroutine(biomeTerrain.Generation());

        Debug.Log("正在渲染光照...");
        LightHandler.Instance.InitLight();

        //保存游戏数据

    }


    //生成垂直规则纹理
    public void GenerateNoiseTextureVertical(float frequency, float threshold, Texture2D noiseTexture) {
        for (int x = 0; x < noiseTexture.width; x++) {
            for (int y = 0; y < noiseTexture.height; y++) {
                float p = (float)y / noiseTexture.height;//高度越高，p值越大
                float v = Mathf.PerlinNoise((x + seed) * frequency, (y + seed) * frequency);
                v /= 0.5f + p;//高度越高，v的值就会越小
                if (v > threshold)
                    noiseTexture.SetPixel(x, y, Color.white);
                else
                    noiseTexture.SetPixel(x, y, Color.black);
            }
        }

        noiseTexture.Apply();
    }

    #region 瓦片处理
    //放置方块
    public void PlaceTile(TileClass tileClass, int x, int y) {
        if (SetTileClass(tileClass, tileClass.layer, x, y))
            tilemaps[(int)tileClass.layer].SetTile(new Vector3Int(x, y), tileClass.tile);

    }

    //批量放置方块
    public void PlaceTiles(Layers layer, List<Vector3Int> pos, List<TileClass> tileClasss) {
        TileBase[] tileBases = SetTileClasses(layer, pos, tileClasss);
        if (tileBases == null) return;
        tilemaps[(int)layer].SetTiles(pos.ToArray(), tileBases);
    }
    public void PlaceLiquidTile(LiquidClass tileClass, int x, int y, float volume) {
        if (!CheckWorldBound(x, y)) return;

        if (tileClass != null) {
            SetLiquidTileClass(tileClass, Layers.Liquid, x, y, volume);
            //根据液体不同体积设置不同瓦片
            TileBase tile = tileClass.GetTileToVolume(LiquidHandler.Instance.liquidVolume[x, y]);
            tilemaps[(int)Layers.Liquid].SetTile(new Vector3Int(x, y), tile);

        }
    }
    //设置瓦片数据
    public bool SetTileClass(TileClass tileClass, Layers layer, int x, int y) {
        if (!CheckWorldBound(x, y)) return false;
        long tileId = tileClass == null ? 0 : tileClass.blockId;
        if (openChunk) {
            SetChunkTile(tileId, layer, x, y);
            return true;
        }
        //tileIds[(int)layer, x, y] = tileId;
        return true;
    }

    //设置液体瓦片数据
    public bool SetLiquidTileClass(LiquidClass liquidClass, Layers layer, int x, int y, float volume) {
        if (!SetTileClass(liquidClass, layer, x, y)) return false;
        float curVolume = liquidClass == null ? 0 : volume;
        LiquidHandler.Instance.liquidVolume[x, y] += curVolume;
        LiquidHandler.Instance.MarkForUpdate(liquidClass, new Vector2Int(x, y));
        return true;

    }
    //批量设置瓦片数据
    public TileBase[] SetTileClasses(Layers layer, List<Vector3Int> pos, List<TileClass> tileClasss) {
        if (pos.Count != tileClasss.Count) return null;
        List<TileBase> tileBases = new List<TileBase>();
        bool result = false;
        for (int i = 0; i < pos.Count; i++) {
            Vector3Int p = pos[i];
            TileClass tileClass = tileClasss[i];
            result = SetTileClass(tileClass, layer, p.x, p.y);
            //失败一个回滚
            if (result) {
                for (int j = i; j <= i; j--) {
                    SetTileClass(null, layer, p.x, p.y);
                }
                return null;
            }

            tileBases.Add(tileClass.tile);
        }
        return tileBases.ToArray();
    }
    //获取指定位置瓦片
    public TileClass GetTileClass(Layers layer, int x, int y) {
        if (!CheckWorldBound(x, y)) return null;
        //if(openChunk)
            return GetChunkTile(layer, x, y); ;
        //long tileId = tileIds[(int)layer, x, y];
        //TileClass tileClass = TileRegistry.GetTile(tileId);
        //return tileClass;
    }

    //消除瓦片
    public void Erase(Layers layer, int x, int y) {
        TileClass targetTile = GetTileClass(layer, x, y);
        SetTileClass(null, layer, x, y);
        tilemaps[(int)layer].SetTile(new Vector3Int(x, y), null);
        //自发光瓦片处理
        //if (tileDatas[layer, x, y] != null && tileDatas[layer, x, y].isIlluminated) {
        //    tileDatas[layer, x, y] = null;
        //    LightHandler.Instance.LightUpdate(x, y);
        //}
        //tileDatas[layer, x, y] = null;
        //液体瓦片处理
        if (layer == Layers.Liquid) {
            LiquidHandler.Instance.liquidVolume[x, y] = 0;
            Vector2Int pos = new Vector2Int(x, y);
            //标记更新
            LiquidHandler.Instance.MarkForUpdate((LiquidClass)targetTile, pos + Vector2Int.up);
        }
    }
    //批量消除瓦片
    public void Erases(List<Vector3Int> pos, Layers layer) {
        foreach (var item in pos) {

            SetTileClass(null, layer, item.x, item.y);
            //液体瓦片处理
            if (layer == Layers.Liquid) {
                LiquidHandler.Instance.liquidVolume[item.x, item.y] = 0;
               // LiquidHandler.Instance.updates.Remove(new Vector2Int(item.x, item.y));
            }
            //自发光瓦片处理
            //if (tileDatas[layer, x, y] != null && tileDatas[layer, x, y].isIlluminated) {
            //    tileDatas[layer, x, y] = null;
            //    LightHandler.Instance.LightUpdate(x, y);
            //}
            
        }
        tilemaps[(int)layer].SetTiles(pos.ToArray(), null);
    }


    //获取指定位置发光体发光强度最高的那个图层亮度
    public float GetLightValue(int x, int y) {
        float lightValue = 0;
        Layers[] layers = (Layers[])Enum.GetValues(typeof(Layers));
        for (int i = 0; i < layers.Length; i++) {
            Layers layer = (Layers)Enum.ToObject(typeof(Layers), i);
            TileClass tileClass = GetTileClass(layer, x, y);
            if (tileClass == null) continue;
            if (tileClass.lightLevel > lightValue)
                lightValue = tileClass.lightLevel;
        }
        return lightValue;
    }

    #endregion


    //地图加载与保存
    public void LoadData(MapData data) {
        //不使用区块
        //for (int i = 0; i < tileIds.GetLength(0); i++) {
        //    for (int x = 0; x < worldWidth; x++) {
        //        for (int y = 0; y < worldHeight; y++) {
        //            long tileBlockId = data.tileDatas[i, x, y];
        //            tileIds[i, x, y] = tileBlockId;
        //        }
        //    }
        //}

        //使用区块
        for (int chunkXIndex = 0; chunkXIndex < chunkXCount; chunkXIndex++) {
            for (int chunkYIndex = 0; chunkYIndex < chunkYCount; chunkYIndex++) {
                long[,,] tileIds = data.chunkDatas[chunkXIndex, chunkYIndex];
                Layers[] layers = (Layers[])Enum.GetValues(typeof(Layers));
                for (int i = 0; i < layers.Length; i++) {
                    //分区块保存
                    for (int x = 0; x < chunkXSize; x++) {
                        for (int y = 0; y < chunkYSize; y++) {
                            long tileId = tileIds[i, x, y];
                            //chunkDatas[chunkXIndex, chunkYIndex][i, x, y] = tileId;
                            chunkDatas.TryGetValue(new Vector2Int(chunkXIndex, chunkYIndex), out long[,,] tileClasses);
                            tileClasses[i, x, y] = tileId;
                        }
                    }
                }
            }
        }
    }

    public void SaveData(ref MapData data) {

        //for (int i = 0; i < tileIds.GetLength(0); i++) {
        //    for (int x = 0; x < worldWidth; x++) {
        //        for (int y = 0; y < worldHeight; y++) {
        //            long tileId = tileIds[i, x, y];
        //            data.tileDatas[i, x, y] = tileId;
        //        }
        //    }
        //}


        for (int chunkXIndex = 0; chunkXIndex < chunkXCount; chunkXIndex++) {
            for (int chunkYIndex = 0; chunkYIndex < chunkYCount; chunkYIndex++) {
                //long[,,] tileClasses = chunkDatas[chunkXIndex, chunkYIndex];
                chunkDatas.TryGetValue(new Vector2Int(chunkXIndex, chunkYIndex), out long[,,] tileClasses);
                Layers[] layers = (Layers[])Enum.GetValues(typeof(Layers));
                for (int i = 0; i < layers.Length; i++) {
                    //分区块保存
                    for (int x = 0; x < chunkXSize; x++) {
                        for (int y = 0; y < chunkYSize; y++) {
                            long tileId = tileClasses[i, x, y];
                            data.chunkDatas[chunkXIndex, chunkYIndex][i, x, y] = tileId;
                        }
                    }
                }
            }
        }
    }


    #region 分区块


    public int chunkXCount;     //X轴区块数
    public int chunkYCount;     //Y轴区块数
    private int chunkXSize;      //X轴每区块像素大小
    private int chunkYSize;      //Y轴每区块像素大小
    public int chunkSize { get; private set; }//总区块大小
    //初始化分区
    private void InitChunk() {
        //chunkDatas = new long[chunkXCount, chunkYCount][,,];
        
        //每个区的瓦片
        chunkXSize = worldWidth / chunkXCount;
        chunkYSize = worldHeight / chunkYCount;
        chunkSize = chunkXSize * chunkYSize;
        for (int chunkXIndex = 0; chunkXIndex < chunkXCount; chunkXIndex++) {
            for (int chunkYIndex = 0; chunkYIndex < chunkYCount; chunkYIndex++) {
                //chunkDatas[chunkXIndex, chunkYIndex] = new long[Enum.GetValues(typeof(Layers)).Length, chunkXSize, chunkYSize];
                chunkDatas.Add(new Vector2Int(chunkXIndex, chunkYIndex), new long[Enum.GetValues(typeof(Layers)).Length, chunkXSize, chunkYSize]);
            }
        }
    }

    //设置区块瓦片
    public void SetChunkTile(long tileId, Layers layer, int x, int y) {
        int chunkXIndex = x / chunkXSize;
        int chunkYIndex = y / chunkYSize;
        int tileXIndex = x - (chunkXIndex * chunkXSize);
        int tileYIndex = y - (chunkYIndex * chunkYSize);
        //chunkDatas[chunkXIndex, chunkYIndex][(int)layer, tileXIndex, tileYIndex] = tileId;
        chunkDatas.TryGetValue(new Vector2Int(chunkXIndex, chunkYIndex), out long[,,] tiles);
        tiles[(int)layer, tileXIndex, tileYIndex] = tileId;
    }

    //获取区块瓦片
    public TileClass GetChunkTile(Layers layer, int x, int y) {
        int chunkXIndex = x / chunkXSize;
        int chunkYIndex = y / chunkYSize;
        int tileXIndex = x - (chunkXIndex * chunkXSize);
        int tileYIndex = y - (chunkYIndex * chunkYSize);
        //long tileId = chunkDatas[chunkXIndex, chunkYIndex][(int)layer, tileXIndex, tileYIndex];
        chunkDatas.TryGetValue(new Vector2Int(chunkXIndex, chunkYIndex), out long[,,] tiles);
        long tileId = tiles[(int)layer, tileXIndex, tileYIndex];
        return TileRegistry.GetTile(tileId);
    }
    #endregion

}
