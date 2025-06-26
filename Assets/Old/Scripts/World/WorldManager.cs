//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.Tilemaps;

//public class WorldManager : Singleton<WorldManager>
//{
//    public WorldSettings worldSettings;//��������
//    public TileClass[,,] tileDatas;//��ͼͼ������
//    public float[,] liquidVolume;//��¼Һ����Ƭ���������
//    public Tilemap[] tilemaps;
//    //public TileAtlas tileAtlas;//ͼ��ϼ�

//    private void Start()
//    {
//        Init();
//    }


//    private void Update()
//    {
//        //ʵʱˮЧ��
//        for (int x = 0; x < worldSettings.worldSize.x; x++)
//        {
//            for (int y = 0; y < worldSettings.worldSize.y; y++)
//            {

//                if (tileDatas[(int)Layers.Liquid, x, y] != null)
//                {
//                    StartCoroutine(((LiquidClass)tileDatas[(int)Layers.Liquid, x, y]).CalculatePhysics(x, y));
//                }
                
//            }
//        }
//    }


//    public void Init()
//    {
//        worldSettings.Init();
//        worldSettings.InitCaves();
//        tileDatas = new TileClass[4, worldSettings.worldSize.x, worldSettings.worldSize.y];
//        liquidVolume = new float[worldSettings.worldSize.x, worldSettings.worldSize.y];
//        Generate();
//    }
    
//    //��ͼ����
//    public void Generate()
//    {
//        List<Biome> biomes = new List<Biome>();
//        for (int i = 0; i < worldSettings.biomes.Length; i++) {
//            biomes.Insert(Random.Range(0, biomes.Count), worldSettings.biomes[i]);
//        }
//        for (int i = 0; i < biomes.Count; i++) {
//            if (biomes[i] == worldSettings.biomes[0]) {
//                Biome _biome = biomes[i];
//                biomes.RemoveAt(i);
//                biomes.Insert((biomes.Count + 1) / 2, _biome);
//                break;
//            }
//        }
//        int[] biomeLengths = new int[worldSettings.biomes.Length];
//        Biome[] chunkBiomes = new Biome[worldSettings.chunkSize.x];
//        int start = 0;
//        for (int i = 0; i < biomeLengths.Length; i++) {
//            biomeLengths[i] = worldSettings.chunkSize.x / biomeLengths.Length;
//            if (biomes[i] == worldSettings.biomes[0]) {
//                biomeLengths[i] += worldSettings.chunkSize.x % biomeLengths.Length;
//            }
//            for (int j = 0; j < biomeLengths[i]; j++) {
//                chunkBiomes[start + j] = biomes[i];
//            }
//            start += biomeLengths[i];
//        }

//        //��������
//        for (int x = 0; x < worldSettings.worldSize.x; x++)
//        {
//            Biome _biome = chunkBiomes[x / worldSettings.chunkScale];
//            int height = worldSettings.GetHeight(x);
//            for (int y = 0; y < height; y++)
//            {
//                //������������
//                TileClass tileToPlace;
//                if (y > height - Random.Range(3, 5))
//                {
//                    tileToPlace = _biome.tileAtlas.grassBlock;
//                }
//                else if (y > height - 30)
//                {
//                    tileToPlace = _biome.tileAtlas.dirtBlock;
//                }
//                else
//                {
//                    tileToPlace = _biome.tileAtlas.stoneBlock;
//                }

//                //��������
//                foreach (var ore in _biome.ores)
//                {
//                    if (Mathf.PerlinNoise((x + ore.offset) * ore.oreRarity, (y + ore.offset) * ore.oreRarity) < ore.oreRadius)
//                    {
//                        tileToPlace = ore;
//                        break;
//                    }
//                }

//                if (!worldSettings.cavePoints[x, y])
//                {
//                    PlaceTile(tileToPlace, x, y);
//                }
//                //ǽ������
//                if (y > height - 30)
//                {
//                    PlaceTile(_biome.tileAtlas.dirtWall, x, y);
//                }
//                else
//                {
//                    PlaceTile(_biome.tileAtlas.stoneWall, x, y);
//                }
//            }
//        }
//        //����ֲ��
//        for (int x = 0; x < worldSettings.worldSize.x; x++)
//        {
//            Biome _biome = chunkBiomes[x / 16];
//            int height = worldSettings.GetHeight(x);
//            for (int y = 0; y < height; y++)
//            {
//                if (y == height - 1 && tileDatas[(int)Layers.Ground, x, y] == _biome.tileAtlas.grassBlock)
//                {
//                    //����ֲ��
//                    if (_biome.tileAtlas.plants != null && Mathf.PerlinNoise((x + worldSettings.seed) * _biome.plantsFrequncy, (y + worldSettings.seed) * _biome.plantsFrequncy) > _biome.plantsThreshold) {
//                        PlaceTile(_biome.tileAtlas.plants, x, y + 1);
//                    }
//                    //������
//                    else if (_biome.tileAtlas.tree != null && Mathf.PerlinNoise((x + worldSettings.seed) * _biome.treeFrequncy, (y + worldSettings.seed) * _biome.treeFrequncy) > _biome.treeThreshold) {
//                        if (x > 0 && tileDatas[(int)Layers.Addons, x - 1, worldSettings.GetHeight(x - 1)] != _biome.tileAtlas.tree) {
//                            SpawnTree(x, y + 1, _biome);
//                        }
//                    }


//                }
//            }
//        }
//        //LightHandler.Instance.Init();

//    }

//    //���÷���
//    public void PlaceTile(TileClass tileClass, int x, int y)
//    {
//        if (x < 0 || x >= worldSettings.worldSize.x || y < 0 || y >= worldSettings.worldSize.y) return;

//        tilemaps[(int)tileClass.layer].SetTile(new Vector3Int(x, y), tileClass.tile);
//        tileDatas[(int)tileClass.layer, x, y] = tileClass;
//    }


//    //
//    // ժҪ:
//    //     ����Һ�巽�飬���ﲻʵ��ִ�з��ö�����ֻ����Һ�����ֵ
//    //
//    // ����:
//    //   vlaue:
//    //     ���ø÷���ʱ��ˮ���
//    public void PlaceLiquidTile(LiquidClass tileClass, int x, int y, float volume)
//    {
//        if (x < 0 || x >= worldSettings.worldSize.x || y < 0 || y >= worldSettings.worldSize.y) return;

//        if (tileClass != null)
//        {
//            //tilemaps[(int)Layers.Liquid].SetTile(new Vector3Int(x, y), tileClass.tile);
//            liquidVolume[x, y] += volume;
//            tileDatas[(int)Layers.Liquid, x, y] = tileClass;
//            if (liquidVolume[x, y] >= 1)
//            {
//                tilemaps[(int)Layers.Liquid].SetTile(new Vector3Int(x, y), tileClass.tiles[tileClass.tiles.Length - 1]);
//            }
//            else
//            {
//                tilemaps[(int)Layers.Liquid].SetTile(new Vector3Int(x, y), tileClass.tiles[Mathf.FloorToInt(liquidVolume[x, y] * (tileClass.tiles.Length - 1))]);
//            }
//        }
//    }

//    //��������
//    public void Erase(int layer, int x, int y)
//    {
//        if (x < 0 || x >= worldSettings.worldSize.x || y < 0 || y >= worldSettings.worldSize.y) return;

//        tilemaps[layer].SetTile(new Vector3Int(x, y), null);
//        if (tileDatas[layer, x, y] != null && tileDatas[layer, x, y].isIlluminated)
//        {
//            tileDatas[layer, x, y] = null;
//            LightHandler.Instance.LightUpdate(x, y);
//        }
//        tileDatas[layer, x, y] = null;
//        if (layer == (int) Layers.Liquid)
//        {
//            liquidVolume[x, y] = 0;
//        }
//    }


//    //������
//    public void SpawnTree(int x, int y, Biome biome)
//    {
//        if (x < 0 || x >= worldSettings.worldSize.x || y < 0 || y >= worldSettings.worldSize.y) return;
//        int h = Random.Range(biome.treeHeight.x, biome.treeHeight.y);//����
//        int maxBranches = Random.Range(3, 10);//���
//        int bCounts = 0;//��込���
//        for (int ny = y; ny < y + h; ny++)
//        {
//            PlaceTile(biome.tileAtlas.tree, x, ny);
//            //������׮
//            if (ny == y)
//            {
//                if (Random.Range(0, 100) < 30)
//                {
//                    if (x > 0 && tileDatas[(int)Layers.Ground, x - 1, ny - 1] != null && tileDatas[(int)Layers.Ground, x - 1, ny] == null)
//                    {
//                        PlaceTile(biome.tileAtlas.tree, x - 1, ny);
//                    }
//                }
//                if (Random.Range(0, 100) < 30)
//                {
//                    if (x < worldSettings.worldSize.x - 1 && tileDatas[(int)Layers.Ground, x + 1, ny - 1] != null && tileDatas[(int)Layers.Ground, x + 1, ny] == null)
//                    {
//                        PlaceTile(biome.tileAtlas.tree, x + 1, ny);
//                    }
//                }

//            }
//            //�������
//            else if (ny >= y + 2 && ny <= y + h - 3)
//            {
//                if (bCounts < maxBranches && Random.Range(0, 100) < 40)
//                {
//                    if (x > 0 && tileDatas[(int)Layers.Ground, x - 1, ny] == null && tileDatas[(int)Layers.Addons, x - 1, ny - 1] != biome.tileAtlas.tree)
//                    {
//                        PlaceTile(biome.tileAtlas.tree, x - 1, ny);
//                        bCounts++;
//                    }
//                }
//                if (bCounts < maxBranches && Random.Range(0, 100) < 40)
//                {
//                    if (x < worldSettings.worldSize.x - 1 && tileDatas[(int)Layers.Ground, x + 1, ny] == null && tileDatas[(int)Layers.Addons, x + 1, ny + 1] != biome.tileAtlas.tree)
//                    {
//                        PlaceTile(biome.tileAtlas.tree, x + 1, ny);
//                        bCounts++;
//                    }
//                }
//            }
//        }
//    }
//    //��ȡָ��λ�ô��ڵķ���
//    public TileClass GetTile(int layer, int x, int y)
//    {
//        if (x < 0 || x >= worldSettings.worldSize.x) return null;
//        if (y < 0 || y >= worldSettings.worldSize.y) return null;
//        return tileDatas[layer, x, y];
//    }

//    public float GetLightValue(int x, int y)
//    {
//        float lightValue = 0;
//        for (int i = 0; i < tileDatas.GetLength(0); i++)
//        {
//            if (tileDatas[i, x, y] == null) continue;
//            if (tileDatas[i, x, y].lightLevel > lightValue)
//                lightValue = tileDatas[i, x, y].lightLevel;
//        }
//        return lightValue;
//    }
//}
