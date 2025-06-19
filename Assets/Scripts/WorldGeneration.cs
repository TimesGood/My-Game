using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
//�������ɹ�����
public class WorldGeneration : Singleton<WorldGeneration>, ISaveManager
{
    public int seed;//��������
    public int worldWidth = 200;//������
    public int worldHeight = 100;//����߶�


    public Tilemap[] tilemaps;//��Ƭ��ͼ��
    public TileClass[,,] tileDatas;//��ͼ��Ƭ����

    public int baseHeight => (int)(worldHeight * 0.7);//���λ�׼�߶�
    public int[] surfaceHeights { get; set; }//���θ߶�����

    //��������
    public BaseTerrain baseTerrain;
    public BiomeTerrain biomeTerrain;

    public List<TileClass> tileClassBases;//��Ƭע�Ἧ


    private void Start() {
        InitWorld();
        if (MapSaveManager.Instance.HasSaveData()) {
            Debug.Log("��������");
            MapSaveManager.Instance.LoadGame();
        } else {
            Debug.Log("��������");
            MapSaveManager.Instance.NewGame();
            StartCoroutine(GenerateWorld());
        }
    }

    //��ʼ����������
    public void InitWorld() {
        //seed = Random.Range(-10000, 10000);
        seed = -2366;
        InitNoiseTexture();
        //��ʼ���߶�
        surfaceHeights = new int[worldWidth];
        for (int x = 0; x < worldWidth; x++) {
            surfaceHeights[x] = baseHeight;
        }
        tileDatas = new TileClass[4, worldWidth, worldHeight];
        LiquidHandler.Instance.Init();
        ChunkHandler.Instance.InitChunk();
    }

    //У�������Ƿ������緶Χ��
    public bool CheckWorldBound(int x, int y) {
        if (x < 0 || x >= worldWidth || y < 0 || y >= worldHeight) return false;
        else return true;
    }


    //��ʼ������ͼ

    private void InitNoiseTexture() {

        baseTerrain.InitNoiseTexture();
        biomeTerrain.InitNoiseTexture();
    }


    //��������
    public IEnumerator GenerateWorld() {
        Debug.Log("�������ɻ�������...");
        yield return StartCoroutine(baseTerrain.Generation());

        Debug.Log("��������Ⱥ�����...");
        yield return StartCoroutine(biomeTerrain.Generation());
        //��ʼ������
        //Debug.Log("������Ⱦ����...");
        //LightHandler.Instance.Init();

        //������Ϸ����

    }


    //���ɴ�ֱ��������
    public void GenerateNoiseTextureVertical(float frequency, float threshold, Texture2D noiseTexture) {
        for (int x = 0; x < noiseTexture.width; x++) {
            for (int y = 0; y < noiseTexture.height; y++) {
                float p = (float)y / noiseTexture.height;//�߶�Խ�ߣ�pֵԽ��
                float v = Mathf.PerlinNoise((x + seed) * frequency, (y + seed) * frequency);
                v /= 0.5f + p;//�߶�Խ�ߣ�v��ֵ�ͻ�ԽС
                if (v > threshold)
                    noiseTexture.SetPixel(x, y, Color.white);
                else
                    noiseTexture.SetPixel(x, y, Color.black);
            }
        }

        noiseTexture.Apply();
    }

    #region ��Ƭ����
    //���÷���
    public void PlaceTile(TileClass tileClass, int x, int y) {
        SetTileData(tileClass, tileClass.layer, x, y);
        tilemaps[(int)tileClass.layer].SetTile(new Vector3Int(x, y), tileClass.tile);
    }

    //�������÷���
    public void PlaceTiles(Layers layer, List<Vector3Int> pos, List<TileClass> tileClasss) {
        TileBase[] tileBases = SetTileDatas(layer, pos, tileClasss);
        if (tileBases == null) return;
        tilemaps[(int)layer].SetTiles(pos.ToArray(), tileBases);
    }

    //������Ƭ����
    public bool SetTileData(TileClass tileClass, Layers layer, int x, int y) {
        if (!CheckWorldBound(x, y)) return false;
        if (Layers.Ground == layer && tileDatas[(int)Layers.Addons, x, y] != null) return false;//ֲ�����鲻������õ�����Ƭ
        tileDatas[(int)layer, x, y] = tileClass;
        //Һ����Ƭ����
        if (tileClass is LiquidClass) {
            LiquidHandler.Instance.liquidVolume[x, y] = 1;
            LiquidHandler.Instance.MarkForUpdate((LiquidClass) tileClass, x, y);
        }
        
        return true;
    }
    //����������Ƭ����
    public TileBase[] SetTileDatas(Layers layer, List<Vector3Int> pos, List<TileClass> tileClasss) {
        if (pos.Count != tileClasss.Count) return null;
        List<TileBase> tileBases = new List<TileBase>();
        bool result = false;
        for (int i = 0; i < pos.Count; i++) {
            Vector3Int p = pos[i];
            TileClass tileClass = tileClasss[i];
            result = SetTileData(tileClass, layer, p.x, p.y);
            //ʧ��һ���ع�
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
    //��ȡָ��λ����Ƭ
    public TileClass GetTileData(Layers layer, int x, int y) {
        if (!CheckWorldBound(x, y)) return null;

        return tileDatas[(int)layer, x, y];
    }

    //������Ƭ
    public void Erase(Layers layer, int x, int y) {
        if (!CheckWorldBound(x, y)) return;
        TileClass targetTile = tileDatas[(int)layer, x, y];
        SetTileData(null, layer, x, y);
        tilemaps[(int)layer].SetTile(new Vector3Int(x, y), null);
        //�Է�����Ƭ����
        //if (tileDatas[layer, x, y] != null && tileDatas[layer, x, y].isIlluminated) {
        //    tileDatas[layer, x, y] = null;
        //    LightHandler.Instance.LightUpdate(x, y);
        //}
        //tileDatas[layer, x, y] = null;
        //Һ����Ƭ����
        if (layer == Layers.Liquid) {
            LiquidHandler.Instance.liquidVolume[x, y] = 0;
            //�����Χ����
            LiquidHandler.Instance.MarkForUpdate((LiquidClass)targetTile, x - 1, y);
            LiquidHandler.Instance.MarkForUpdate((LiquidClass)targetTile, x + 1, y);
            LiquidHandler.Instance.MarkForUpdate((LiquidClass)targetTile, x, y + 1);
            LiquidHandler.Instance.MarkForUpdate((LiquidClass)targetTile, x, y - 1);

            //LiquidHandler.Instance.updates.Remove(new Vector2Int(x, y));
        }
    }
    //����������Ƭ
    public void Erases(List<Vector3Int> pos, Layers layer) {
        foreach (var item in pos) {

            SetTileData(null, layer, item.x, item.y);
            //Һ����Ƭ����
            if (layer == Layers.Liquid) {
                LiquidHandler.Instance.liquidVolume[item.x, item.y] = 0;
               // LiquidHandler.Instance.updates.Remove(new Vector2Int(item.x, item.y));
            }
            //�Է�����Ƭ����
            //if (tileDatas[layer, x, y] != null && tileDatas[layer, x, y].isIlluminated) {
            //    tileDatas[layer, x, y] = null;
            //    LightHandler.Instance.LightUpdate(x, y);
            //}
            
        }
        tilemaps[(int)layer].SetTiles(pos.ToArray(), null);
    }


    //��ȡָ��λ�����ȼ���
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
    // ժҪ:
    //     ����Һ�巽��
    //
    // ����:
    //   vlaue:
    //     ���ø÷���ʱ��ˮ���
    public void PlaceLiquidTile(LiquidClass tileClass, int x, int y, float volume) {
        if (!CheckWorldBound(x, y)) return;

        if (tileClass != null) {
            Vector2Int tilePos = new Vector2Int(x, y);
            LiquidHandler.Instance.MarkForUpdate(tileClass, x, y);
            LiquidHandler.Instance.liquidVolume[x, y] += volume;
            SetTileData(tileClass, Layers.Liquid, x, y);
            //����Һ�岻ͬ������ò�ͬ��Ƭ
            TileBase tile = tileClass.GetTile(LiquidHandler.Instance.liquidVolume[x, y]);
            tilemaps[(int)Layers.Liquid].SetTile(new Vector3Int(x, y), tile);
        }
    }
    #endregion



    //���������Ƭ
#if UNITY_EDITOR

    //��ȡ�ʲ����е�����װ������
    [ContextMenu("�����Ʒ����")]
    private void FillUpTileClassBase() => tileClassBases = GetTileClassBase();

    private List<TileClass> GetTileClassBase() {
        List<TileClass> tileClassBases = new List<TileClass>();
        //����Ŀ¼�е�װ�����ݣ����ص���GUID
        string[] assetNames = AssetDatabase.FindAssets("", new[] { "Assets/Old/Tiles" });
        int id = 0;
        foreach (string SOName in assetNames) {
            var SOpath = AssetDatabase.GUIDToAssetPath(SOName);//GUIDתΪ���ʵ����Ŀ·��
            var itemData = AssetDatabase.LoadAssetAtPath<TileClass>(SOpath);//����·����ȡָ�����
            if (itemData == null) continue;
            itemData.blockId = id;
            tileClassBases.Add(itemData);
            id++;
        }

        return tileClassBases;
    }

#endif



    //��ͼ�����뱣��
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
            //�����鱣��
            for (int x = 0; x < worldWidth; x++) {
                for (int y = 0; y < worldHeight; y++) {
                    TileClass tileClass = tileDatas[i, x, y];
                    data.tileDatas[i, x, y] = tileClass == null ? -1 : tileClass.blockId;
                }
            }
        }

    }
}
