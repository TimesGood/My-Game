
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

//地表群落
[CreateAssetMenu(fileName = "BiomeTest", menuName = "MyGame/new BiomeTest")]
public class BiomeTest : ScriptableObject
{
    public Vector2Int biomeWidth { get; private set; }  //群落宽度（轮廓漏出基准线（baseHeight）的宽度）
    [field: SerializeField] public BiomeTest child { get; private set; } //子群落
    [field: SerializeField] public CurveConfig terrain { get; private set; }//地表地形
    [field: SerializeField] public ShapeGenerator outLine { get; private set; }//群落轮廓
    [field: SerializeField] public PerlinNoise cave { get; private set; }//群落洞穴
    [field: SerializeField] public OreClass[] ores { get; private set; }//可生成的矿物
    [field: SerializeField] public TileClass grassBlock { get; private set; }//地表瓦片
    [field: SerializeField] public TileClass dirtBlock { get; private set; }//土层瓦片
    [field: SerializeField] public TileClass dirtWall { get; private set; }//土层墙壁
    [field: SerializeField] public TileClass stoneBlock { get; private set; }//岩层瓦片
    [field: SerializeField] public TileClass stoneWall { get; private set; }//岩层墙壁
    [field: SerializeField] public TileClass plants { get; private set; }//植物
    [field: SerializeField] public TileClass tree { get; private set; }//树木
    [field: SerializeField] public TileClass leaf { get; private set; }//树杈

    //存储生成的噪图
    private Dictionary<string, Texture2D> noises = new Dictionary<string, Texture2D>();

    //初始化
    public void InitBiome(int startWidth, int endWidth, int seed) {

        //计算轮廓实际露出地面的宽度表示群落宽度
        int baseHeight = WorldGeneration.Instance.baseHeight;
        Vector2Int _biomeWidth = new Vector2Int();
        bool isReversal = false;
        int biomeSize = endWidth - startWidth;

        for(int x = startWidth; x < endWidth; x++) {
            int noiseX = x - startWidth;
            if (!isReversal && outLine.noiseTexture.GetPixel(noiseX, baseHeight).r > 0.5) {
                _biomeWidth.x = x;
                break;
            }
        }
        for (int x = endWidth; x > startWidth; x--) {
            int noiseX = x - startWidth;
            if (outLine.noiseTexture.GetPixel(noiseX, baseHeight).r > 0.5) {
                _biomeWidth.y = x;
                break;
            }
        }

        
        biomeWidth = _biomeWidth;
    }

    //初始化噪图数据
    public void InitNoise(int width, int height, int seed) {
        //地形噪图生成
        terrain.InitValidate(width, height, seed);
        outLine.InitValidate(width, height, seed);
        cave.InitValidate(width, height, seed);

        terrain.InitNoise();
        outLine.InitNoise();
        cave.InitNoise();

        //矿石瓦片噪图生成
        int t = 0;
        foreach (OreClass tileClass in ores) {
            tileClass.noise.InitValidate(width, height, seed + t * 100);
            Texture2D noiseTexture = tileClass.noise.InitNoise();
            noises.Add(""+tileClass.blockId, noiseTexture);
            t++;
        }

    }

    
    //在分配的空间内执行生成逻辑
    public void GenerateBiome(int startWidth, int endWidth, int seed) {
        int baseHeight = WorldGeneration.Instance.baseHeight;
        InitBiome(startWidth, endWidth, seed);
        WorldGeneration world = WorldGeneration.Instance;
        for (int x = startWidth; x < endWidth; x++) {
            int noiseX = x - startWidth;
            int oldHeight = world.surfaceHeights[x];
            
            int terrainHeight = baseHeight + terrain.GetHeight(noiseX);
            //擦除旧地形高出新地形的瓦片
            if (oldHeight > terrainHeight && x >= biomeWidth.x && x <= biomeWidth.y) {
                for (int diffY = terrainHeight; diffY < oldHeight; diffY++) {
                    world.SetTileClass(null, Layers.Ground, x, diffY);
                }
            }
            //地形高度调整
            if (x >= biomeWidth.x && x <= biomeWidth.y) {
                world.surfaceHeights[x] = terrainHeight;
            }
            int treeHeight = 0;
            for (int y = 0; y < terrainHeight; y++) {
                TileClass tileClass = null;
                
                //群落地表地形
                if (y > baseHeight && x >= biomeWidth.x && x <= biomeWidth.y) {

                    tileClass = world.baseTerrain.dirtClass;

                }

                //群落轮廓内
                if (outLine.noiseTexture.GetPixel(noiseX, y).r > 0.5f) {
                    
                    //基础地形
                    if (y < terrainHeight - 1) {
                        //补充岩层
                        tileClass = dirtBlock;

                    } else {
                        //地皮
                        tileClass = grassBlock;
                    }

                    //矿脉
                    foreach (OreClass oreClass in ores) {
                        Texture2D oreNoise = null;
                        noises.TryGetValue(""+oreClass.blockId, out oreNoise);
                        if (oreNoise.GetPixel(x, y).r > 0.5) {
                            tileClass = oreClass;
                            break;
                        }
                    }

                    //挖洞穴
                    if (cave.noiseTexture.GetPixel(noiseX, y).r <= 0) {
                        world.SetTileClass(null, Layers.Ground, x, y);
                        tileClass = null;
                        //洞穴植株
                        TileClass tileBase = world.GetTileClass(Layers.Ground, x, y - treeHeight - 1);
                        if (tileBase != null && tileBase == dirtBlock) {
                            //如果左侧有树了，树计数器归零
                            if (world.GetTileClass(Layers.Addons, x - 1, y) != null) {
                                treeHeight = 0;
                            } else {

                                treeHeight++;
                                //满足生成树的条件
                                if (treeHeight == 10) {
                                    //概率生成
                                    if (Random.Range(0, 100) > 70) {
                                        SpawnTree(tree, leaf, x, y - 9);
                                    }
                                    treeHeight = 0;
                                }
                            }
                        }
                    }

                }

                if (tileClass != null) {
                    WorldGeneration.Instance.SetTileClass(tileClass, tileClass.layer, x, y);
                    treeHeight = 0;
                }
                //else if(y < world.surfaceHeights[x]) {
                //    treeHeight++;
                //    if (treeHeight == 10) {
                //        if (Random.Range(0, 100) > 0.6) {
                //            Debug.Log(x+":"+y);
                //            SpawnTree(tree, leaf, x, y - 9);
                //        }
                //        treeHeight = 0;

                //    }
                //}
                    
            }

        }
    }
    //生成树,组合树
    public void SpawnTree(TileClass tileClass, TileClass leafClass, int x, int y) {
        int h = Random.Range(5, 10);//树高
        int maxBranches = Random.Range(3, 10);//树杈
        int bCounts = 0;//树杈计数
        for (int ny = y; ny < y + h; ny++) {
            WorldGeneration.Instance.SetTileClass(tileClass, tileClass.layer, x, ny);
            //生成树桩
            if (ny == y) {
                //左侧树桩
                if (Random.Range(0, 100) < 30) {
                    if (x > 0 && WorldGeneration.Instance.GetTileClass(Layers.Ground, x - 1, ny - 1) != null && WorldGeneration.Instance.GetTileClass(Layers.Ground, x - 1, ny) == null) {
                        WorldGeneration.Instance.SetTileClass(tileClass, tileClass.layer, x - 1, ny);
                    }
                }
                //右侧树桩
                if (Random.Range(0, 100) < 30) {
                    if (WorldGeneration.Instance.GetTileClass(Layers.Ground, x + 1, ny - 1) != null && WorldGeneration.Instance.GetTileClass(Layers.Ground, x + 1, ny) == null) {
                        WorldGeneration.Instance.SetTileClass(tileClass, tileClass.layer, x + 1, ny);
                    }
                }

            }
            //生成树杈
            else if (ny >= y + 2 && ny <= y + h - 3) {
                if (bCounts < maxBranches && Random.Range(0, 100) < 40) {
                    if (x > 0 && WorldGeneration.Instance.GetTileClass(Layers.Ground, x - 1, ny) == null && WorldGeneration.Instance.GetTileClass(Layers.Addons, x - 1, ny - 1) != tileClass) {
                        WorldGeneration.Instance.SetTileClass(leafClass, leafClass.layer, x - 1, ny);
                        bCounts++;
                    }
                }
                if (bCounts < maxBranches && Random.Range(0, 100) < 40) {
                    if (WorldGeneration.Instance.GetTileClass(Layers.Ground, x + 1, ny) == null && WorldGeneration.Instance.GetTileClass(Layers.Addons, x + 1, ny - 1) != tileClass) {
                        WorldGeneration.Instance.SetTileClass(leafClass, leafClass.layer, x + 1, ny);
                        bCounts++;
                    }
                }
            }
        }
    }

    public void DestroyNoiseTexture() {
        outLine.DestroyNoiseTexture();
        cave.DestroyNoiseTexture();
    }
}
