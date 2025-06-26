using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using TreeEditor;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Tilemaps;
using Random = UnityEngine.Random;

//�ر�Ⱥ��
[CreateAssetMenu(fileName = "SurfaceBiome", menuName = "Biome/new SurfaceBiome")]
public class SurfaceBiome : BaseBiome {

    public int surfaceStart;//Ⱥ���ڵر��Ͽ�ʼX������
    public int surfaceEnd;//Ⱥ���ڵر����X������

    [field: SerializeField] public BaseBiome[] childBiomes { get; private set; } //��Ⱥ��
    [field: SerializeField] public CurveConfig terrain { get; private set; }//�ر��������
    [field: SerializeField] public PerlinNoise cave { get; private set; }//Ⱥ�䶴Ѩ��ͼ
    [field: SerializeField] public OreClass[] ores { get; private set; }//�����ɵĿ���
    [field: SerializeField] public TileClass grassBlock { get; private set; }//�ر���Ƭ
    [field: SerializeField] public TileClass dirtBlock { get; private set; }//������Ƭ
    [field: SerializeField] public TileClass dirtWall { get; private set; }//����ǽ��
    [field: SerializeField] public TileClass stoneBlock { get; private set; }//�Ҳ���Ƭ
    [field: SerializeField] public TileClass stoneWall { get; private set; }//�Ҳ�ǽ��
    //ֲ��
    [field: SerializeField] public TileClass plants { get; private set; }//ֲ��


    [field: SerializeField] public TreeClass[] trees { get; private set; }//�����ɵ���ľ
    [field: SerializeField] public TreeClass[] caveTrees { get; private set; }//��Ѩ��
    //�洢���ɵĿ�����ͼ
    private Dictionary<string, Texture2D> noises = new Dictionary<string, Texture2D>();//�洢���ɵ���ͼ

    //��ʼ��Ⱥ��
    public override void InitBiome(Vector2Int worldPosition, int seed) {
        base.InitBiome(worldPosition, seed);
        HandlerBiomeSurfacePos();
        
    }

    //����Ⱥ��©���ر�λ��
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

    //��ʼ����ͼ
    public override void InitNoise(int seed) {
        base.InitNoise(seed);
        //������ͼ����
        terrain.InitValidate(biomeWidth,biomeHeight,seed);
        cave.InitValidate(biomeWidth, biomeHeight, seed);
        terrain.InitNoise();
        cave.InitNoise();

        //��ʯ��Ƭ��ͼ����
        int t = 0;
        foreach (OreClass tileClass in ores) {
            tileClass.noise.InitValidate(biomeWidth, biomeHeight, seed + t * 100);
            Texture2D noiseTexture = tileClass.noise.InitNoise();
            noises.Add("" + tileClass.blockId, noiseTexture);
            t++;
        }

        //��ľ
        for (int i = 0; i < trees.Length; i++) {
            TreeClass treeClass = trees[i];
            treeClass.noise.InitValidate(biomeWidth, biomeHeight, seed);
            treeClass.noise.frequency = treeClass.frequency;//�ܶ�
            treeClass.noise.threshold = treeClass.threshold;//��Χ��ÿ���С��
            //���ܴ���ʹ��ͬһ���������
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

        //����ͼ��ʼ��
        foreach (BaseBiome childBiomes in childBiomes) {

            //childBiomes.InitNoise(seed);
        }
    }

    public override IEnumerator GenerateBiome() {
        int baseHeight = WorldGeneration.Instance.baseHeight;
        int[] terrainHeights = new int[biomeWidth];
        int[] noiseXs = new int[biomeWidth];
        int maxHeight = 0;
        //�����������������ɣ���������ʱ�򷽱㣩
        for (int x = generatePos.x; x < generatePos.x + biomeWidth; x++) {
            int noiseX = GetLocalPositionX(x);
            int terrainHeight = baseHeight + terrain.GetHeight(noiseX);
            int startIndex = x - generatePos.x;
            terrainHeights[startIndex] = terrainHeight;
            
            noiseXs[startIndex] = noiseX;
            if (terrainHeight > maxHeight) maxHeight = terrainHeight;
            //Ⱥ����ε���
            EraseTopTile(x, terrainHeight);
            //���θ߶ȵ���
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

                //Ⱥ��ر����
                if (y > baseHeight && IsSurfaceRange(x)) {
                    tileClass = world.baseTerrain.dirtClass;
                }
                //Ⱥ��������
                if (outLine.noiseTexture.GetPixel(noiseX, noiseY).r > 0.5f) {

                    //��������
                    if (y < terrainHeight - 1) {
                        //�Ҳ�
                        tileClass = dirtBlock;

                    } else {
                        //��Ƥ
                        tileClass = grassBlock;
                    }

                    //����
                    foreach (OreClass oreClass in ores) {
                        Texture2D oreNoise = null;
                        noises.TryGetValue("" + oreClass.blockId, out oreNoise);
                        if (oreNoise.GetPixel(x, y).r > 0.5) {
                            tileClass = oreClass;
                            break;
                        }
                    }

                    //�ڶ�Ѩ
                    if (cave.noiseTexture.GetPixel(noiseX, y).r <= 0) {
                        world.SetTileClass(null, Layers.Ground, x, y);
                        tileClass = null;

                        //��Ѩֲ��


                        //��Ѩ��
                        if (!(cave.noiseTexture.GetPixel(noiseX, y - 1).r <= 0) && world.GetTileClass(Layers.Ground, x, y - 1) != null) {
                            for (int i = 0; i < caveTrees.Length; i++) {
                                TreeClass tree = caveTrees[i];
                                if (CheckSpawnTree(tree, x, y)) {
                                    //��������
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

                //TODO: ������ܻ�Ҫ���һ���ڶ��᲻��������׸��ڵ���
                //�ر�ֲ��
                if (y == terrainHeight && IsSurfaceRange(x) && !(cave.noiseTexture.GetPixel(noiseX, y - 1).r <= 0)) {

                    for (int i = 0; i < trees.Length; i++) {
                        TreeClass tree = trees[i];
                        if (CheckSpawnTree(tree, x, y + 1)) {
                            //��������
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
                // ÿ֡����5000����ֹ����
                if (++processed % 5000 == 0) {
                    UnityEngine.Debug.Log(Mathf.FloorToInt((float)processed / totalCell * 100) + "%");
                    yield return null;
                }

            }
        }

        BroundTransition();
    }

    //У���Ƿ�������������
    private bool CheckSpawnTree(TreeClass tree, int x, int y) {
        //���Ŀ��λ�����治����������������
        TileClass tileBase = world.GetTileClass(Layers.Ground, x, y - 1);
        if (tileBase == null || (tileBase != dirtBlock && tileBase != grassBlock)) return false;

        for (int extY = y; extY < y + tree.maxHeight; extY++) {
            //�鿴���Ҳ���������������ֲ���������
            if (world.GetTileClass(Layers.Addons, x - 1, extY) != null || world.GetTileClass(Layers.Addons, x + 1, extY) != null) {
                return false;
            }
        }
        return true;
    }

    //Ⱥ��߽����
    private void BroundTransition() {
        if (surfaceStart == 0 || surfaceEnd == 0) return;
        //Ⱥ��߽����ƽ�����ɵ���
        int leftBiomeHeight = world.surfaceHeights[surfaceStart];
        int rightBiomeHeight = world.surfaceHeights[surfaceEnd];
        int blendDistance = 50;//���ɾ���
        int leftHeightX = surfaceStart - blendDistance > 0 ? surfaceStart - blendDistance : 0;
        int rightHeightX = surfaceEnd + blendDistance > world.worldWidth ? world.worldWidth - 1 : surfaceEnd + blendDistance;
        int leftWorldHeight = world.surfaceHeights[leftHeightX];
        int rightWorldHeight = world.surfaceHeights[rightHeightX];
        //Ⱥ��������
        for (int x = 0; x < blendDistance; x++) {
            float t = (float)x / (blendDistance - 1);
            float noise = Mathf.PerlinNoise(x * 0.05f, 0) * 2 - 1; // -1~1��Χ


            //Ⱥ��������
            float leftLerpHeight = Mathf.Lerp(leftWorldHeight, leftBiomeHeight, t);

            int leftHeight = (int)(leftLerpHeight + noise * 3f);

            int leftBlendX = x + leftHeightX;

            FillEraseTile(leftHeight, leftBlendX);

            //Ⱥ���Ҳ����
            float rightLerpHeight = Mathf.Lerp(rightBiomeHeight, rightWorldHeight, t);
            int height = (int)(rightLerpHeight + noise * 3f);

            int rightBlendX = x + rightHeightX - blendDistance;
            FillEraseTile(height, rightBlendX);
        }
    }


    //�����������ϲ���
    private void FillEraseTile(int height, int blendX) {
        //�������
        int downHeight = height;
        while (world.GetTileClass(Layers.Ground, blendX, downHeight) == null) {
            world.SetTileClass(world.baseTerrain.stoneClass, Layers.Ground, blendX, downHeight);
            downHeight--;
        }

        //��������
        int upHeigth = height + 1;
        int oldHeight = world.surfaceHeights[blendX];
        while (upHeigth < oldHeight) {
            world.SetTileClass(null, Layers.Ground, blendX, upHeigth);
            upHeigth++;

        }
        //���µ��θ߶�
        world.surfaceHeights[blendX] = height;
    }


    //�����ɵ��θ߳��µ��ε���Ƭ
    private void EraseTopTile(int x, int newTerrainHeight) {
        int oldHeight = world.surfaceHeights[x];
        if (oldHeight > newTerrainHeight && IsSurfaceRange(x)) {
            for (int diffY = newTerrainHeight; diffY < oldHeight; diffY++) {
                world.SetTileClass(null, Layers.Ground, x, diffY);
            }
        }
    }

 

    //�ж�x���Ƿ��ڵر�Χ��
    private bool IsSurfaceRange(int x) {
        return x >= surfaceStart && x <= surfaceEnd;
    }

}
    
