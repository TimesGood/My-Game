
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

//�ر�Ⱥ��
[CreateAssetMenu(fileName = "BiomeTest", menuName = "MyGame/new BiomeTest")]
public class BiomeTest : ScriptableObject
{
    public Vector2Int biomeWidth { get; private set; }  //Ⱥ���ȣ�����©����׼�ߣ�baseHeight���Ŀ�ȣ�
    [field: SerializeField] public BiomeTest child { get; private set; } //��Ⱥ��
    [field: SerializeField] public CurveConfig terrain { get; private set; }//�ر����
    [field: SerializeField] public ShapeGenerator outLine { get; private set; }//Ⱥ������
    [field: SerializeField] public PerlinNoise cave { get; private set; }//Ⱥ�䶴Ѩ
    [field: SerializeField] public OreClass[] ores { get; private set; }//�����ɵĿ���
    [field: SerializeField] public TileClass grassBlock { get; private set; }//�ر���Ƭ
    [field: SerializeField] public TileClass dirtBlock { get; private set; }//������Ƭ
    [field: SerializeField] public TileClass dirtWall { get; private set; }//����ǽ��
    [field: SerializeField] public TileClass stoneBlock { get; private set; }//�Ҳ���Ƭ
    [field: SerializeField] public TileClass stoneWall { get; private set; }//�Ҳ�ǽ��
    [field: SerializeField] public TileClass plants { get; private set; }//ֲ��
    [field: SerializeField] public TileClass tree { get; private set; }//��ľ
    [field: SerializeField] public TileClass leaf { get; private set; }//���

    //�洢���ɵ���ͼ
    private Dictionary<string, Texture2D> noises = new Dictionary<string, Texture2D>();

    //��ʼ��
    public void InitBiome(int startWidth, int endWidth, int seed) {

        //��������ʵ��¶������Ŀ�ȱ�ʾȺ����
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

    //��ʼ����ͼ����
    public void InitNoise(int width, int height, int seed) {
        //������ͼ����
        terrain.InitValidate(width, height, seed);
        outLine.InitValidate(width, height, seed);
        cave.InitValidate(width, height, seed);

        terrain.InitNoise();
        outLine.InitNoise();
        cave.InitNoise();

        //��ʯ��Ƭ��ͼ����
        int t = 0;
        foreach (OreClass tileClass in ores) {
            tileClass.noise.InitValidate(width, height, seed + t * 100);
            Texture2D noiseTexture = tileClass.noise.InitNoise();
            noises.Add(""+tileClass.blockId, noiseTexture);
            t++;
        }

    }

    
    //�ڷ���Ŀռ���ִ�������߼�
    public void GenerateBiome(int startWidth, int endWidth, int seed) {
        int baseHeight = WorldGeneration.Instance.baseHeight;
        InitBiome(startWidth, endWidth, seed);
        WorldGeneration world = WorldGeneration.Instance;
        for (int x = startWidth; x < endWidth; x++) {
            int noiseX = x - startWidth;
            int oldHeight = world.surfaceHeights[x];
            
            int terrainHeight = baseHeight + terrain.GetHeight(noiseX);
            //�����ɵ��θ߳��µ��ε���Ƭ
            if (oldHeight > terrainHeight && x >= biomeWidth.x && x <= biomeWidth.y) {
                for (int diffY = terrainHeight; diffY < oldHeight; diffY++) {
                    world.SetTileClass(null, Layers.Ground, x, diffY);
                }
            }
            //���θ߶ȵ���
            if (x >= biomeWidth.x && x <= biomeWidth.y) {
                world.surfaceHeights[x] = terrainHeight;
            }
            int treeHeight = 0;
            for (int y = 0; y < terrainHeight; y++) {
                TileClass tileClass = null;
                
                //Ⱥ��ر����
                if (y > baseHeight && x >= biomeWidth.x && x <= biomeWidth.y) {

                    tileClass = world.baseTerrain.dirtClass;

                }

                //Ⱥ��������
                if (outLine.noiseTexture.GetPixel(noiseX, y).r > 0.5f) {
                    
                    //��������
                    if (y < terrainHeight - 1) {
                        //�����Ҳ�
                        tileClass = dirtBlock;

                    } else {
                        //��Ƥ
                        tileClass = grassBlock;
                    }

                    //����
                    foreach (OreClass oreClass in ores) {
                        Texture2D oreNoise = null;
                        noises.TryGetValue(""+oreClass.blockId, out oreNoise);
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
                        TileClass tileBase = world.GetTileClass(Layers.Ground, x, y - treeHeight - 1);
                        if (tileBase != null && tileBase == dirtBlock) {
                            //�����������ˣ�������������
                            if (world.GetTileClass(Layers.Addons, x - 1, y) != null) {
                                treeHeight = 0;
                            } else {

                                treeHeight++;
                                //����������������
                                if (treeHeight == 10) {
                                    //��������
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
    //������,�����
    public void SpawnTree(TileClass tileClass, TileClass leafClass, int x, int y) {
        int h = Random.Range(5, 10);//����
        int maxBranches = Random.Range(3, 10);//���
        int bCounts = 0;//��込���
        for (int ny = y; ny < y + h; ny++) {
            WorldGeneration.Instance.SetTileClass(tileClass, tileClass.layer, x, ny);
            //������׮
            if (ny == y) {
                //�����׮
                if (Random.Range(0, 100) < 30) {
                    if (x > 0 && WorldGeneration.Instance.GetTileClass(Layers.Ground, x - 1, ny - 1) != null && WorldGeneration.Instance.GetTileClass(Layers.Ground, x - 1, ny) == null) {
                        WorldGeneration.Instance.SetTileClass(tileClass, tileClass.layer, x - 1, ny);
                    }
                }
                //�Ҳ���׮
                if (Random.Range(0, 100) < 30) {
                    if (WorldGeneration.Instance.GetTileClass(Layers.Ground, x + 1, ny - 1) != null && WorldGeneration.Instance.GetTileClass(Layers.Ground, x + 1, ny) == null) {
                        WorldGeneration.Instance.SetTileClass(tileClass, tileClass.layer, x + 1, ny);
                    }
                }

            }
            //�������
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
