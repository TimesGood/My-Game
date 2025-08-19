using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Random = UnityEngine.Random;

//地表群落
[CreateAssetMenu(fileName = "SurfaceBiome", menuName = "Biome/new SurfaceBiome")]
public class SurfaceBiome : BaseBiome {

    public int surfaceStart;//群落出现在地表上的开始坐标
    public int surfaceEnd;//群落出现在地表上结束坐标

    [field: SerializeField] public CurveConfig terrain { get; private set; }//地表地形曲线
    [field: SerializeField] public PerlinNoise cave { get; private set; }//群落洞穴噪图
    [field: SerializeField] public OreClass[] ores { get; private set; }//群落中可生成的矿物
    [field: SerializeField] public TileClass grassBlock { get; private set; }//群落地表瓦片
    [field: SerializeField] public TileClass dirtBlock { get; private set; }//土层瓦片
    [field: SerializeField] public TileClass dirtWall { get; private set; }//土层墙壁
    [field: SerializeField] public TileClass stoneBlock { get; private set; }//岩层瓦片
    [field: SerializeField] public TileClass stoneWall { get; private set; }//岩层墙壁
    //植物
    [field: SerializeField] public TileClass plants { get; private set; }//植物

    [field: SerializeField] public TreeClass[] trees { get; private set; }//地表树
    [field: SerializeField] public TreeClass[] caveTrees { get; private set; }//地底树
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
        for (int x = 0; x < biomeSize.x; x++) {
            if (!isReversal && outLine.noiseTexture.GetPixel(x, baseHeight).r > 0.5) {
                int worldX = LocalToWorldPosX(x);
                surfaceStart = worldX;
                break;
            }
        }
        for (int x = biomeSize.x; x > 0; x--) {
            if (outLine.noiseTexture.GetPixel(x, baseHeight).r > 0.5) {
                int worldX = LocalToWorldPosX(x);
                surfaceEnd = worldX;
                break;
            }
        }
    }

    //初始化噪图
    protected override void InitNoise(int seed) {
        base.InitNoise(seed);
        //地形噪图生成
        terrain.InitValidate(biomeSize.x,biomeSize.y,seed);
        cave.InitValidate(biomeSize.x, biomeSize.y, seed);
        terrain.InitNoise();
        cave.InitNoise();

        //矿石瓦片噪图生成
        int t = 0;
        foreach (OreClass tileClass in ores) {
            tileClass.noise.InitValidate(biomeSize.x, biomeSize.y, seed + t * 100);
            Texture2D noiseTexture = tileClass.noise.InitNoise();
            noises.Add(tileClass.blockId.ToString(), noiseTexture);
            t++;
        }

        //树木
        for (int i = 0; i < trees.Length; i++) {
            TreeClass treeClass = trees[i];
            treeClass.noise.InitValidate(biomeSize.x, biomeSize.y, seed);
            treeClass.noise.frequency = treeClass.frequency;//密度
            treeClass.noise.threshold = treeClass.threshold;//范围（每撮大小）
            //可能存在使用同一种树的情况
            if (!noises.ContainsKey(treeClass.blockId.ToString())) {
                noises.Add(treeClass.blockId.ToString(), treeClass.noise.InitNoise());
            }
        }

        for (int i = 0; i < caveTrees.Length; i++) {
            TreeClass treeClass = caveTrees[i];
            treeClass.noise.InitValidate(biomeSize.x, biomeSize.y, seed);
            treeClass.noise.frequency = treeClass.frequency;
            treeClass.noise.threshold = treeClass.threshold;
            if (!noises.ContainsKey(treeClass.blockId.ToString())) {
                noises.Add(treeClass.blockId.ToString(), treeClass.noise.InitNoise());
            }
        }
    }

    //执行生成
    public override IEnumerator GenerateBiome() {
        int baseHeight = WorldManager.Instance.baseHeight;
        int[] terrainHeights = new int[biomeSize.x];//存储地形高度
        int[] worldXs = new int[biomeSize.x];//存储x轴世界坐标
        int maxHeight = 0;
        //从上往下-左往右生成（生成树的时候方便）
        for (int x = 0; x < biomeSize.x; x++) {
            
            int terrainHeight = baseHeight + (int)terrain.GetHeight(x);

            terrainHeights[x] = terrainHeight;
            int worldX = LocalToWorldPosX(x);
            worldXs[x] = worldX;
            if (terrainHeight > maxHeight) maxHeight = terrainHeight;
            //群落地形调整
            EraseTopTile(worldX, terrainHeight);
        }


        int processed = 0;
        int totalCell = maxHeight * biomeSize.x;

        //地形生成
        for (int y = maxHeight; y >= 0; y--) {
            int worldY = LocalToWorldPosY(y);
            for (int x = 0; x < biomeSize.x; x++) {
                int terrainHeight = terrainHeights[x];
                int worldX = worldXs[x];
                if (worldY > terrainHeight) continue;
                TileClass tileClass = world.GetTileClass(Layers.Ground, worldX, worldY);

                //群落地表地形
                if (worldY > baseHeight && IsSurfaceRange(worldX)) {
                    tileClass = dirtBlock;
                }

                //群落轮廓内
                if (isOutLine(x, y)) {
                    if (worldY == terrainHeight && IsSurfaceRange(worldX)) {
                        tileClass = grassBlock;
                    } else {
                        tileClass = dirtBlock;
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
                    if (cave.noiseTexture.GetPixel(x, y).r <= 0) {
                        world.SetTileClass(null, Layers.Ground, worldX, worldY);
                        tileClass = null;
                    }

                }

                if (tileClass != null) {
                    world.SetTileClass(tileClass, tileClass.layer, worldX, worldY);
                }
                //地表
                if (worldY == terrainHeight && IsSurfaceRange(worldX) && !(cave.noiseTexture.GetPixel(x, y - 1).r <= 0)) {
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

                // 每帧处理5000个防止卡顿
                if (++processed % 5000 == 0) {
                    Debug.Log(Mathf.FloorToInt((float)processed / totalCell * 100) + "%");
                    yield return null;
                }

            }
        }

        //植物生成
        for (int y = maxHeight; y >= 0; y--) {
            int worldY = LocalToWorldPosY(y);
            for (int x = 0; x < biomeSize.x; x++) {
                int terrainHeight = terrainHeights[x];
                int worldX = worldXs[x];
                if (worldY > terrainHeight) continue;

                //地底洞穴
                if (isOutLine(x, y)) {
                    //挖洞穴
                    if (cave.noiseTexture.GetPixel(x, y).r <= 0) {
                        //洞穴树
                        if (!(cave.noiseTexture.GetPixel(x, y - 1).r <= 0) && world.GetTileClass(Layers.Ground, worldX, worldY - 1) != null) {
                            if (caveTrees.Length != 0 && Random.Range(0, 100) > 60) {
                                int caveIndex = Random.Range(0, caveTrees.Length);
                                TreeClass tree = caveTrees[caveIndex];
                                if (tree.CheckSpawn(worldX, worldY)) {
                                    tree.PlanceSelf(worldX, worldY);
                                }
                            }
                        }

                    }
                }
                //地表
                if (worldY == terrainHeight && IsSurfaceRange(worldX) && !(cave.noiseTexture.GetPixel(x, y - 1).r <= 0)) {
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
            }
        }


        //群落地表地形过渡
        BroundTransition();
        
    }

    //群落边界过渡
    private void BroundTransition() {
        if (surfaceStart == 0 || surfaceEnd == 0) return;
        //群落边界地形平滑过渡调整
        int leftSurfaceY = world.surfaceHeights[surfaceStart];
        int rightSurfaceY = world.surfaceHeights[surfaceEnd];
        int blendDistance = 50;//过渡距离

        //过渡点位
        int leftBlendStartX = surfaceStart - blendDistance > 0 ? surfaceStart - blendDistance : 0;
        int rightBlendEndX = surfaceEnd + blendDistance > world.worldSize.x ? world.worldSize.x - 1 : surfaceEnd + blendDistance;

        int leftBlendStartY = world.surfaceHeights[leftBlendStartX];
        int rightBlendEndY = world.surfaceHeights[rightBlendEndX];


        //群落左侧过渡
        for (int x = 0; x < blendDistance; x++) {
            float t = (float)x / (blendDistance - 1);
            float noise = Mathf.PerlinNoise(x * 0.05f, 0) * 2 - 1; // -1~1范围


            //群落左侧过渡
            float leftLerpHeight = Mathf.Lerp(leftBlendStartY, leftSurfaceY, t);

            int leftBlendY = (int)(leftLerpHeight + noise * 3f);

            int leftBlendX = x + leftBlendStartX;

            FillEraseTile(leftBlendX, leftBlendY);

            //群落右侧过渡
            float rightLerpHeight = Mathf.Lerp(rightSurfaceY, rightBlendEndY, t);
            int rightBlendY = (int)(rightLerpHeight + noise * 3f);

            int rightBlendX = x + rightBlendEndX - blendDistance;
            FillEraseTile(rightBlendX, rightBlendY);
        }
    }


    //基于某点向下填充-向上擦除
    private void FillEraseTile(int x, int y) {
        //向下填充
        int downHeight = y;
        while (world.GetTileClass(Layers.Ground, x, downHeight) == null) {
            world.SetTileClass(dirtBlock, Layers.Ground, x, downHeight);
            downHeight--;
        }

        //向上消除
        int upHeigth = y + 1;
        int oldHeight = world.surfaceHeights[x];
        while (upHeigth < oldHeight) {
            world.SetTileClass(null, Layers.Ground, x, upHeigth);
            upHeigth++;

        }
        //更新地形高度
        world.surfaceHeights[x] = y;
    }


    //擦除某点位高出旧地形的瓦片
    private void EraseTopTile(int x, int y) {
        int oldHeight = world.surfaceHeights[x];
        if (oldHeight > y && IsSurfaceRange(x)) {
            for (int diffY = y; diffY < oldHeight; diffY++) {
                world.SetTileClass(null, Layers.Ground, x, diffY);
                world.surfaceHeights[x] = y;
            }
        }
    }

 

    //判断x轴是否在地表范围内
    private bool IsSurfaceRange(int x) {
        return x >= surfaceStart && x <= surfaceEnd;
    }

}
    
