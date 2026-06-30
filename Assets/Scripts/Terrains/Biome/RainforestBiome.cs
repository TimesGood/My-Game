using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using Unity.Entities.UniversalDelegates;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Tilemaps;
using static UnityEngine.Rendering.HableCurve;

//雨林群落
[CreateAssetMenu(fileName = "RainforestBiome", menuName = "Biome/new RainforestBiome")]
public class RainforestBiome : BaseBiome {
    public int surfaceStart;//群落出现在地表上的开始坐标
    public int surfaceEnd;//群落出现在地表上结束坐标

    //[field: SerializeField] public CurveConfig terrain { get; private set; }//地表地形曲线
    //[field: SerializeField] public PerlinNoise cave { get; private set; }//群落洞穴噪图
    [field: SerializeField] public OreClass[] ores { get; private set; }//群落中可生成的矿物
    [field: SerializeField] public TileClass grassBlock { get; private set; }//群落地表瓦片
    [field: SerializeField] public TileClass dirtBlock { get; private set; }//土层瓦片
    [field: SerializeField] public TileClass dirtWall { get; private set; }//土层墙壁
    [field: SerializeField] public TileClass stoneBlock { get; private set; }//岩层瓦片
    [field: SerializeField] public TileClass stoneWall { get; private set; }//岩层墙壁
    //植物
    [field: SerializeField] public TileClass[] plants { get; private set; }//植物

    [field: SerializeField] public TreeClass[] trees { get; private set; }//地表树
    [field: SerializeField] public TreeClass[] caveTrees { get; private set; }//地底树

    [field: SerializeField] public TileClass vine { get; private set;  }//藤蔓
    //存储生成的矿物噪图
    protected Dictionary<string, Texture2D> noises = new Dictionary<string, Texture2D>();//存储生成的噪图

    //初始化群落
    public override void InitBiome(Vector2Int worldPosition, int seed) {
        base.InitBiome(worldPosition, seed);
        HandlerBiomeSurfacePos();

    }

    //获取群落露出地面的点位
    private void HandlerBiomeSurfacePos() {
        int baseHeight = WorldManager.Instance.baseHeight;
        bool isReversal = false;
        surfaceStart = 0;
        surfaceEnd = 0;
        //for (int x = 0; x < biomeSize.x; x++) {
        //    if (!isReversal && outLine.noiseTexture.GetPixel(x, baseHeight).r > 0.5) {
        //        int worldX = LocalToWorldPosX(x);
        //        surfaceStart = worldX;
        //        break;
        //    }
        //}
        //for (int x = biomeSize.x; x > 0; x--) {
        //    if (outLine.noiseTexture.GetPixel(x, baseHeight).r > 0.5) {
        //        int worldX = LocalToWorldPosX(x);
        //        surfaceEnd = worldX;
        //        break;
        //    }
        //}
    }

    //初始化噪图
    protected override void InitNoise(int seed) {
        base.InitNoise(seed);
        //地形噪图生成
        //terrain.InitValidate(biomeSize.x, biomeSize.y, seed);
        //cave.InitValidate(biomeSize.x, biomeSize.y, seed);
        //terrain.InitNoise();
        //cave.InitNoise();

        ////矿石瓦片噪图生成
        //int t = 0;
        //foreach (OreClass tileClass in ores) {
        //    tileClass.noise.InitValidate(biomeSize.x, biomeSize.y, seed + t * 100);
        //    Texture2D noiseTexture = tileClass.noise.InitNoise();
        //    noises.Add(tileClass.blockId.ToString(), noiseTexture);
        //    t++;
        //}

        ////树木
        //for (int i = 0; i < trees.Length; i++) {
        //    TreeClass treeClass = trees[i];
        //    treeClass.noise.InitValidate(biomeSize.x, biomeSize.y, seed);
        //    treeClass.noise.frequency = treeClass.frequency;//密度
        //    treeClass.noise.threshold = treeClass.threshold;//范围（每撮大小）
        //    //可能存在使用同一种树的情况
        //    if (!noises.ContainsKey(treeClass.blockId.ToString())) {
        //        noises.Add(treeClass.blockId.ToString(), treeClass.noise.InitNoise());
        //    }
        //}

        //for (int i = 0; i < caveTrees.Length; i++) {
        //    TreeClass treeClass = caveTrees[i];
        //    treeClass.noise.InitValidate(biomeSize.x, biomeSize.y, seed);
        //    treeClass.noise.frequency = treeClass.frequency;
        //    treeClass.noise.threshold = treeClass.threshold;
        //    if (!noises.ContainsKey(treeClass.blockId.ToString())) {
        //        noises.Add(treeClass.blockId.ToString(), treeClass.noise.InitNoise());
        //    }
        //}
    }

    //执行生成
    public override IEnumerator GenerateBiome() {

        Vector2Int startPos = Vector2Int.zero;
        Vector2Int endPos = Vector2Int.zero;

        int blendStart = LocalToWorldPosX(0);
        int blendEnd = LocalToWorldPosX(biomeSize.x - 1);
        //地形曲线混合
        //BlendCurves(world.terrainCurveData, terrain.GetCurveData(), blendStart, 50);
        //BlendCurves(world.stoneCurveData, terrain.GetCurveData(), blendStart, 50, 100);
        //地形生成
        for (int x = 0; x < biomeSize.x; x++) {
            int worldX = LocalToWorldPosX(x);
            float stoneHeight = (world.baseHeight * 0.8f) + world.stoneCurveData[worldX];
            int terrainHeight = world.baseHeight + (int)(world.GetTerrain(worldX));
            EraseAndPlaceTile(worldX, terrainHeight);

            for (int y = 0; y < biomeSize.y; y++) {
                int worldY = LocalToWorldPosY(y);
                if (worldY > terrainHeight) continue;

                //轮廓内
                if (isOutLine(x, y)) {
                    TileClass tileClass;
                    //地质层
                    if (worldY < stoneHeight) {

                        tileClass = stoneBlock;
                    } else if (worldY < terrainHeight - 1) {
                        tileClass = dirtBlock;

                    } else {
                        tileClass = grassBlock;
                    }
                    //矿脉
                    foreach (OreClass oreClass in ores) {
                        Texture2D oreNoise = null;
                        noises.TryGetValue(oreClass.blockId.ToString(), out oreNoise);
                        if (oreNoise.GetPixel(x, y).r > 0.5) {
                            tileClass = oreClass;
                            break;
                        }
                    }
                    //挖洞穴
                    //if (cave.noiseTexture.GetPixel(x, y).r <= 0) {
                    //    tileClass = null;
                    //}
                    world.SetTileClass(tileClass, Layers.Ground, worldX, worldY);
                }
            }
        }

        yield return null;

        //植株
        for (int x = 0; x < biomeSize.x; x++) {
            int worldX = LocalToWorldPosX(x);
            int terrainHeight = world.baseHeight + (int)(world.GetTerrain(x));

            for (int y = 0; y < biomeSize.y; y++) {
                int worldY = LocalToWorldPosY(y);
                if (worldY > terrainHeight) continue;

                //轮廓内
                if (isOutLine(x, y)) {
                    //地表植株
                    if (worldY == terrainHeight) {
                        TileClass tileBase = world.GetTileClass(Layers.Ground, worldX, worldY);
                        if (tileBase != null && (tileBase == dirtBlock || tileBase == grassBlock)) {
                            for (int i = 0; i < trees.Length; i++) {
                                TreeClass tree = trees[i];

                                if (tree.CheckSpawn(worldX, worldY + 1)) {
                                    //概率生成
                                    Texture2D treeNoise;
                                    noises.TryGetValue(tree.blockId.ToString(), out treeNoise);
                                    if (treeNoise.GetPixel(x, y + 1).r > 0.5) {
                                        tree.PlanceSelf(worldX, worldY + 1);
                                        break;
                                    }
                                }
                            }
                        }
                    }

                    //地底
                    //if (cave.noiseTexture.GetPixel(x, y).r <= 0) {
                    //    //洞穴树
                    //    if (!(cave.noiseTexture.GetPixel(x, y - 1).r <= 0) && world.GetTileClass(Layers.Ground, worldX, worldY - 1) != null) {
                    //        if (caveTrees.Length != 0 && Random.Range(0, 100) > 60) {
                    //            int caveIndex = Random.Range(0, caveTrees.Length);
                    //            TreeClass tree = caveTrees[caveIndex];
                    //            if (tree.CheckSpawn(worldX, worldY)) {
                    //                tree.PlanceSelf(worldX, worldY);
                    //            }
                    //        }
                    //    } else if(!(cave.noiseTexture.GetPixel(x, y + 1).r <= 0) && world.GetTileClass(Layers.Ground, worldX, worldY + 1) != null) {
                    //        //生成藤蔓
                    //        if (vine != null && Random.Range(0, 100) > 70) {
                    //            int l = Random.Range(3, 10);
                    //            bool flag = true;
                    //            for (int i = 1; i <= l; i++) {
                    //                if (world.GetTileClass(Layers.Ground, worldX, worldY - i) != null) {
                    //                    flag = false;
                    //                    break;
                    //                }
                    //            }
                    //            if (flag) {
                    //                world.SetTileClass(vine, Layers.Addons, worldX, worldY);
                    //                //TODO:临时设置数据
                    //                GrowthHandler.Instance.MarkForUpdate(new Vector2Int(worldX, worldY), l);
                    //            }
                                
                    //        }
                    //    }

                    //}

                } else {
                
                }
            }
        }

    }

    //曲线混合
    void BlendCurves(float[] main, float[] sub, int startIndex, int blendRange, int heightAdd = 0) {
  
        // 使用平滑函数过渡
        for (int i = 0; i < sub.Length; i++) {
            //只处理两端
            //if (i >= blendRange && i <= sub.Length - 1 - blendRange) continue;

            float t = i / (float)(sub.Length - 1);
            float blendFactor = 1f;

            // 处理两端过渡
            if (i < blendRange) blendFactor = SmoothStep(0, 1, i / (float)blendRange);
            else if (i > sub.Length - 1 - blendRange)
                blendFactor = SmoothStep(1, 0, (i - (sub.Length - 1 - blendRange)) / (float)blendRange);

            int mainIdx = startIndex + i;
            if (mainIdx < main.Length) {
                main[mainIdx] = Mathf.Lerp(main[mainIdx],
                                             sub[i] + heightAdd,
                                             blendFactor);
            }
        }
    }

    float SmoothStep(float from, float to, float t) {
        t = Mathf.Clamp01(t);
        t = -2f * t * t * t + 3f * t * t; // 三次平滑
        return Mathf.Lerp(from, to, t);
    }


    //地形瓦片调整，高出旧地形的填充瓦片，低于旧地形的擦除
    private void EraseAndPlaceTile(int x, int y) {
        int oldHeight = world.surfaceHeights[x];
        if (oldHeight > y) {
            for (int diffY = y; diffY < oldHeight; diffY++) {
                world.SetTileClass(null, Layers.Ground, x, diffY);
            }
        } else {
            for (int diffY = y; diffY >= oldHeight; diffY--) {
                world.SetTileClass(dirtBlock, Layers.Ground, x, diffY);
            }
        }
        world.surfaceHeights[x] = y;
    }
}
