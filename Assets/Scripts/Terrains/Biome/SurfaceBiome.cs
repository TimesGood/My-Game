using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using TreeEditor;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Tilemaps;
using Random = UnityEngine.Random;

//地表群落
[CreateAssetMenu(fileName = "SurfaceBiome", menuName = "Biome/new SurfaceBiome")]
public class SurfaceBiome : BaseBiome {

    public int surfaceStart;//群落在地表上开始X轴坐标
    public int surfaceEnd;//群落在地表结束X轴坐标

    [field: SerializeField] public BaseBiome[] childBiomes { get; private set; } //子群落
    [field: SerializeField] public CurveConfig terrain { get; private set; }//地表地形曲线
    [field: SerializeField] public PerlinNoise cave { get; private set; }//群落洞穴噪图
    [field: SerializeField] public OreClass[] ores { get; private set; }//可生成的矿物
    [field: SerializeField] public TileClass grassBlock { get; private set; }//地表瓦片
    [field: SerializeField] public TileClass dirtBlock { get; private set; }//土层瓦片
    [field: SerializeField] public TileClass dirtWall { get; private set; }//土层墙壁
    [field: SerializeField] public TileClass stoneBlock { get; private set; }//岩层瓦片
    [field: SerializeField] public TileClass stoneWall { get; private set; }//岩层墙壁
    //植物
    [field: SerializeField] public TileClass plants { get; private set; }//植物


    [field: SerializeField] public TreeClass[] trees { get; private set; }//可生成的树木
    [field: SerializeField] public TreeClass[] caveTrees { get; private set; }//洞穴树
    //存储生成的矿物噪图
    private Dictionary<string, Texture2D> noises = new Dictionary<string, Texture2D>();//存储生成的噪图

    //初始化群落
    public override void InitBiome(Vector2Int worldPosition, int seed) {
        base.InitBiome(worldPosition, seed);
        HandlerBiomeSurfacePos();
        
    }

    //处理群落漏出地表位置
    private void HandlerBiomeSurfacePos() {
        int baseHeight = WorldGeneration.Instance.baseHeight;
        bool isReversal = false;
        surfaceStart = 0;
        surfaceEnd = 0;
        for (int x = generatePos.x; x < generatePos.x + biomeWidth; x++) {
            int noiseX = GetLocalPositionX(x);
            if (!isReversal && outLine.noiseTexture.GetPixel(noiseX, baseHeight).r > 0.5) {
                surfaceStart = x;
                break;
            }
        }
        for (int x = generatePos.x + biomeWidth; x > generatePos.x; x--) {
            int noiseX = GetLocalPositionX(x);
            if (outLine.noiseTexture.GetPixel(noiseX, baseHeight).r > 0.5) {
                surfaceEnd = x;
                break;
            }
        }
    }

    //初始化噪图
    public override void InitNoise(int seed) {
        base.InitNoise(seed);
        //地形噪图生成
        terrain.InitValidate(biomeWidth,biomeHeight,seed);
        cave.InitValidate(biomeWidth, biomeHeight, seed);
        terrain.InitNoise();
        cave.InitNoise();

        //矿石瓦片噪图生成
        int t = 0;
        foreach (OreClass tileClass in ores) {
            tileClass.noise.InitValidate(biomeWidth, biomeHeight, seed + t * 100);
            Texture2D noiseTexture = tileClass.noise.InitNoise();
            noises.Add("" + tileClass.blockId, noiseTexture);
            t++;
        }

        //树木
        for (int i = 0; i < trees.Length; i++) {
            TreeClass treeClass = trees[i];
            treeClass.noise.InitValidate(biomeWidth, biomeHeight, seed);
            treeClass.noise.frequency = treeClass.frequency;//密度
            treeClass.noise.threshold = treeClass.threshold;//范围（每撮大小）
            //可能存在使用同一种树的情况
            if (!noises.ContainsKey("" + treeClass.blockId)) {
                noises.Add("" + treeClass.blockId, treeClass.noise.InitNoise());
            }
            
        }
        for (int i = 0; i < caveTrees.Length; i++) {
            TreeClass treeClass = caveTrees[i];
            treeClass.noise.InitValidate(biomeWidth, biomeHeight, seed);
            treeClass.noise.frequency = treeClass.frequency;
            treeClass.noise.threshold = treeClass.threshold;
            if (!noises.ContainsKey("" + treeClass.blockId)) {
                noises.Add("" + treeClass.blockId, treeClass.noise.InitNoise());
            }
        }

        //子噪图初始化
        foreach (BaseBiome childBiomes in childBiomes) {

            //childBiomes.InitNoise(seed);
        }
    }

    public override IEnumerator GenerateBiome() {
        int baseHeight = WorldGeneration.Instance.baseHeight;
        int[] terrainHeights = new int[biomeWidth];
        int[] noiseXs = new int[biomeWidth];
        int maxHeight = 0;
        //上往下由左往右生成（生成树的时候方便）
        for (int x = generatePos.x; x < generatePos.x + biomeWidth; x++) {
            int noiseX = GetLocalPositionX(x);
            int terrainHeight = baseHeight + terrain.GetHeight(noiseX);
            int startIndex = x - generatePos.x;
            terrainHeights[startIndex] = terrainHeight;
            
            noiseXs[startIndex] = noiseX;
            if (terrainHeight > maxHeight) maxHeight = terrainHeight;
            //群落地形调整
            EraseTopTile(x, terrainHeight);
            //地形高度调整
            if (IsSurfaceRange(x)) {
                world.surfaceHeights[x] = terrainHeight;
            }
        }
        int processed = 0;
        int totalCell = maxHeight * biomeWidth;
        for (int y = maxHeight; y >= 0; y--) {
            for (int x = generatePos.x; x < generatePos.x + biomeWidth; x++) {
                int startIndex = x - generatePos.x;
                int terrainHeight = terrainHeights[startIndex];
                int noiseX = noiseXs[startIndex];
                if (y > terrainHeight) continue;
                int noiseY = GetLocalPositionY(y);
                TileClass tileClass = world.GetTileClass(Layers.Ground, x, y);

                //群落地表地形
                if (y > baseHeight && IsSurfaceRange(x)) {
                    tileClass = world.baseTerrain.dirtClass;
                }
                //群落轮廓内
                if (outLine.noiseTexture.GetPixel(noiseX, noiseY).r > 0.5f) {

                    //基础地形
                    if (y < terrainHeight - 1) {
                        //岩层
                        tileClass = dirtBlock;

                    } else {
                        //地皮
                        tileClass = grassBlock;
                    }

                    //矿脉
                    foreach (OreClass oreClass in ores) {
                        Texture2D oreNoise = null;
                        noises.TryGetValue("" + oreClass.blockId, out oreNoise);
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


                        //洞穴树
                        if (!(cave.noiseTexture.GetPixel(noiseX, y - 1).r <= 0) && world.GetTileClass(Layers.Ground, x, y - 1) != null) {
                            for (int i = 0; i < caveTrees.Length; i++) {
                                TreeClass tree = caveTrees[i];
                                if (CheckSpawnTree(tree, x, y)) {
                                    //概率生成
                                    Texture2D treeNoise;
                                    noises.TryGetValue("" + tree.blockId, out treeNoise);
                                    if (treeNoise.GetPixel(noiseX, noiseY).r > 0.5) {
                                        tree.PlanceSelf(x, y);
                                        break;
                                    }

                                }
                            }
                        } 
                    }

                }

                if (tileClass != null) {
                    WorldGeneration.Instance.SetTileClass(tileClass, tileClass.layer, x, y);
                }

                //TODO: 这里可能还要检查一下挖洞会不会把树基底给挖掉了
                //地表植物
                if (y == terrainHeight && IsSurfaceRange(x) && !(cave.noiseTexture.GetPixel(noiseX, y - 1).r <= 0)) {

                    for (int i = 0; i < trees.Length; i++) {
                        TreeClass tree = trees[i];
                        if (CheckSpawnTree(tree, x, y + 1)) {
                            //概率生成
                            Texture2D treeNoise;
                            noises.TryGetValue("" + tree.blockId, out treeNoise);
                            noiseY = GetLocalPositionY(y + 1);
                            if (treeNoise.GetPixel(noiseX, noiseY).r > 0.5) {
                                tree.PlanceSelf(x, y + 1);
                                break;
                            }
                        }
                    }

                }
                // 每帧处理5000个防止卡顿
                if (++processed % 5000 == 0) {
                    UnityEngine.Debug.Log(Mathf.FloorToInt((float)processed / totalCell * 100) + "%");
                    yield return null;
                }

            }
        }

        BroundTransition();
    }

    //校验是否满足生成条件
    private bool CheckSpawnTree(TreeClass tree, int x, int y) {
        //如果目标位置下面不是泥土，不能生成
        TileClass tileBase = world.GetTileClass(Layers.Ground, x, y - 1);
        if (tileBase == null || (tileBase != dirtBlock && tileBase != grassBlock)) return false;

        for (int extY = y; extY < y + tree.maxHeight; extY++) {
            //查看左右侧树情况，如果存在植物，则不能生成
            if (world.GetTileClass(Layers.Addons, x - 1, extY) != null || world.GetTileClass(Layers.Addons, x + 1, extY) != null) {
                return false;
            }
        }
        return true;
    }

    //群落边界过渡
    private void BroundTransition() {
        if (surfaceStart == 0 || surfaceEnd == 0) return;
        //群落边界地形平滑过渡调整
        int leftBiomeHeight = world.surfaceHeights[surfaceStart];
        int rightBiomeHeight = world.surfaceHeights[surfaceEnd];
        int blendDistance = 50;//过渡距离
        int leftHeightX = surfaceStart - blendDistance > 0 ? surfaceStart - blendDistance : 0;
        int rightHeightX = surfaceEnd + blendDistance > world.worldWidth ? world.worldWidth - 1 : surfaceEnd + blendDistance;
        int leftWorldHeight = world.surfaceHeights[leftHeightX];
        int rightWorldHeight = world.surfaceHeights[rightHeightX];
        //群落左侧过渡
        for (int x = 0; x < blendDistance; x++) {
            float t = (float)x / (blendDistance - 1);
            float noise = Mathf.PerlinNoise(x * 0.05f, 0) * 2 - 1; // -1~1范围


            //群落左侧过渡
            float leftLerpHeight = Mathf.Lerp(leftWorldHeight, leftBiomeHeight, t);

            int leftHeight = (int)(leftLerpHeight + noise * 3f);

            int leftBlendX = x + leftHeightX;

            FillEraseTile(leftHeight, leftBlendX);

            //群落右侧过渡
            float rightLerpHeight = Mathf.Lerp(rightBiomeHeight, rightWorldHeight, t);
            int height = (int)(rightLerpHeight + noise * 3f);

            int rightBlendX = x + rightHeightX - blendDistance;
            FillEraseTile(height, rightBlendX);
        }
    }


    //向下填充或向上擦除
    private void FillEraseTile(int height, int blendX) {
        //向下填充
        int downHeight = height;
        while (world.GetTileClass(Layers.Ground, blendX, downHeight) == null) {
            world.SetTileClass(world.baseTerrain.stoneClass, Layers.Ground, blendX, downHeight);
            downHeight--;
        }

        //向上消除
        int upHeigth = height + 1;
        int oldHeight = world.surfaceHeights[blendX];
        while (upHeigth < oldHeight) {
            world.SetTileClass(null, Layers.Ground, blendX, upHeigth);
            upHeigth++;

        }
        //更新地形高度
        world.surfaceHeights[blendX] = height;
    }


    //擦除旧地形高出新地形的瓦片
    private void EraseTopTile(int x, int newTerrainHeight) {
        int oldHeight = world.surfaceHeights[x];
        if (oldHeight > newTerrainHeight && IsSurfaceRange(x)) {
            for (int diffY = newTerrainHeight; diffY < oldHeight; diffY++) {
                world.SetTileClass(null, Layers.Ground, x, diffY);
            }
        }
    }

 

    //判断x轴是否在地表范围内
    private bool IsSurfaceRange(int x) {
        return x >= surfaceStart && x <= surfaceEnd;
    }

}
    
