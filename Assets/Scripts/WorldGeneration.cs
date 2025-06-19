using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
//世界生成管理器
public class WorldGeneration : Singleton<WorldGeneration>, ISaveManager
{
    public int seed;//世界种子
    public int worldWidth = 200;//世界宽度
    public int worldHeight = 100;//世界高度


    public Tilemap[] tilemaps;//瓦片地图集
    public TileClass[,,] tileDatas;//地图瓦片数据

    public int baseHeight => (int)(worldHeight * 0.7);//地形基准高度
    public int[] surfaceHeights { get; set; }//地形高度数据

    //世界生成
    public BaseTerrain baseTerrain;
    public BiomeTerrain biomeTerrain;

    public List<TileClass> tileClassBases;//瓦片注册集


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
        tileDatas = new TileClass[4, worldWidth, worldHeight];
        LiquidHandler.Instance.Init();
        ChunkHandler.Instance.InitChunk();
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
        //初始化光照
        //Debug.Log("正在渲染光照...");
        //LightHandler.Instance.Init();

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
        SetTileData(tileClass, tileClass.layer, x, y);
        tilemaps[(int)tileClass.layer].SetTile(new Vector3Int(x, y), tileClass.tile);
    }

    //批量放置方块
    public void PlaceTiles(Layers layer, List<Vector3Int> pos, List<TileClass> tileClasss) {
        TileBase[] tileBases = SetTileDatas(layer, pos, tileClasss);
        if (tileBases == null) return;
        tilemaps[(int)layer].SetTiles(pos.ToArray(), tileBases);
    }

    //设置瓦片数据
    public bool SetTileData(TileClass tileClass, Layers layer, int x, int y) {
        if (!CheckWorldBound(x, y)) return false;
        if (Layers.Ground == layer && tileDatas[(int)Layers.Addons, x, y] != null) return false;//植物区块不允许放置地面瓦片
        tileDatas[(int)layer, x, y] = tileClass;
        //液体瓦片处理
        if (tileClass is LiquidClass) {
            LiquidHandler.Instance.liquidVolume[x, y] = 1;
            LiquidHandler.Instance.MarkForUpdate((LiquidClass) tileClass, x, y);
        }
        
        return true;
    }
    //批量设置瓦片数据
    public TileBase[] SetTileDatas(Layers layer, List<Vector3Int> pos, List<TileClass> tileClasss) {
        if (pos.Count != tileClasss.Count) return null;
        List<TileBase> tileBases = new List<TileBase>();
        bool result = false;
        for (int i = 0; i < pos.Count; i++) {
            Vector3Int p = pos[i];
            TileClass tileClass = tileClasss[i];
            result = SetTileData(tileClass, layer, p.x, p.y);
            //失败一个回滚
            if (result) {
                for (int j = i; j <= i; j--) {
                    SetTileData(null, layer, p.x, p.y);
                }
                return null;
            }

            tileBases.Add(tileClass.tile);
        }
        return tileBases.ToArray();
    }
    //获取指定位置瓦片
    public TileClass GetTileData(Layers layer, int x, int y) {
        if (!CheckWorldBound(x, y)) return null;

        return tileDatas[(int)layer, x, y];
    }

    //消除瓦片
    public void Erase(Layers layer, int x, int y) {
        if (!CheckWorldBound(x, y)) return;
        TileClass targetTile = tileDatas[(int)layer, x, y];
        SetTileData(null, layer, x, y);
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
            //标记周围更新
            LiquidHandler.Instance.MarkForUpdate((LiquidClass)targetTile, x - 1, y);
            LiquidHandler.Instance.MarkForUpdate((LiquidClass)targetTile, x + 1, y);
            LiquidHandler.Instance.MarkForUpdate((LiquidClass)targetTile, x, y + 1);
            LiquidHandler.Instance.MarkForUpdate((LiquidClass)targetTile, x, y - 1);

            //LiquidHandler.Instance.updates.Remove(new Vector2Int(x, y));
        }
    }
    //批量消除瓦片
    public void Erases(List<Vector3Int> pos, Layers layer) {
        foreach (var item in pos) {

            SetTileData(null, layer, item.x, item.y);
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


    //获取指定位置亮度级别
    public float GetLightValue(int x, int y) {
        float lightValue = 0;
        for (int i = 0; i < tileDatas.GetLength(0); i++) {
            if (tileDatas[i, x, y] == null) continue;
            if (tileDatas[i, x, y].lightLevel > lightValue)
                lightValue = tileDatas[i, x, y].lightLevel;
        }
        return lightValue;
    }

    //
    // 摘要:
    //     放置液体方块
    //
    // 参数:
    //   vlaue:
    //     放置该方块时的水体积
    public void PlaceLiquidTile(LiquidClass tileClass, int x, int y, float volume) {
        if (!CheckWorldBound(x, y)) return;

        if (tileClass != null) {
            Vector2Int tilePos = new Vector2Int(x, y);
            LiquidHandler.Instance.MarkForUpdate(tileClass, x, y);
            LiquidHandler.Instance.liquidVolume[x, y] += volume;
            SetTileData(tileClass, Layers.Liquid, x, y);
            //根据液体不同体积设置不同瓦片
            TileBase tile = tileClass.GetTile(LiquidHandler.Instance.liquidVolume[x, y]);
            tilemaps[(int)Layers.Liquid].SetTile(new Vector3Int(x, y), tile);
        }
    }
    #endregion



    //填充所有瓦片
#if UNITY_EDITOR

    //获取资产库中的所有装备数据
    [ContextMenu("填充物品数据")]
    private void FillUpTileClassBase() => tileClassBases = GetTileClassBase();

    private List<TileClass> GetTileClassBase() {
        List<TileClass> tileClassBases = new List<TileClass>();
        //查找目录中的装备数据，返回的是GUID
        string[] assetNames = AssetDatabase.FindAssets("", new[] { "Assets/Old/Tiles" });
        int id = 0;
        foreach (string SOName in assetNames) {
            var SOpath = AssetDatabase.GUIDToAssetPath(SOName);//GUID转为物件实际项目路径
            var itemData = AssetDatabase.LoadAssetAtPath<TileClass>(SOpath);//根据路径读取指定物件
            if (itemData == null) continue;
            itemData.blockId = id;
            tileClassBases.Add(itemData);
            id++;
        }

        return tileClassBases;
    }

#endif



    //地图加载与保存
    public void LoadData(MapData data) {
        for (int i = 0; i < 4; i++) {
            for (int x = 0; x < worldWidth; x++) {
                for (int y = 0; y < worldHeight; y++) {
                    int tileBlockId = data.tileDatas[i, x, y];
                    if (tileBlockId == -1) continue;
                    TileClass tileClass = tileClassBases[tileBlockId];
                    tileDatas[i, x, y] = tileClass;
                }
            }
        }


    }

    public void SaveData(ref MapData data) {
        for (int i = 0; i < 4; i++) {
            //分区块保存
            for (int x = 0; x < worldWidth; x++) {
                for (int y = 0; y < worldHeight; y++) {
                    TileClass tileClass = tileDatas[i, x, y];
                    data.tileDatas[i, x, y] = tileClass == null ? -1 : tileClass.blockId;
                }
            }
        }

    }
}
