using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
public class WorldGeneration : Singleton<WorldGeneration>, ISaveManager
{
    public int seed;//��������
    public int worldWidth = 200;//������
    public int worldHeight = 100;//����߶�

    
    public Tilemap[] tilemaps;//��Ƭ��ͼ��
    private long[,,] tileIds;//��Ƭ��Ӧλ��Id

    public int baseHeight => (int)(worldHeight * 0.7);//���λ�׼�߶�
    public int[] surfaceHeights { get; set; }//���θ߶�����

    //��������
    public BaseTerrain baseTerrain;
    public BiomeTerrain biomeTerrain;

    //��ʼ������
    private static bool _isInitialized;

    // ��Ƭע���
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


    //��ʼ��ʱִ�У���ȡ��Ƭ�ʲ�����
    [RuntimeInitializeOnLoadMethod]
    private static void Initialize() {
        if (_isInitialized) return;
        TileRegistry.ClearRegistry();

        //TileClass[] allTiles = Resources.LoadAll<TileClass>("Tiles");
        //ע����Ƭ�ʲ�
        //����Ŀ¼�е�װ�����ݣ����ص���GUID
        string[] assetNames = AssetDatabase.FindAssets("", new[] { "Assets/Old/Tiles" });
        int i = 0;
        foreach (string SOName in assetNames) {
            var SOpath = AssetDatabase.GUIDToAssetPath(SOName);//GUIDתΪ���ʵ����Ŀ·��
            var itemData = AssetDatabase.LoadAssetAtPath<TileClass>(SOpath);//����·����ȡָ�����
            if (itemData == null) continue;
            TileRegistry.RegisterTile(itemData);
            i++;

        }

        _isInitialized = true;
        Debug.Log($"��ע�� {i} ��ͼ��");
    }

    //������ƬId��ȡ��Ƭ�ʲ�
    public TileClass GetTileClass(long id) {

        TileClass tileClass = TileRegistry.GetTile(id);
        if (tileClass != null) return tileClass;

        Debug.LogWarning($"�Ҳ��� ID: {id} ��Ӧ��ͼ��");
        return null;
    }

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
        //tileDatas = new TileClass[4, worldWidth, worldHeight];
        tileIds = new long[Enum.GetValues(typeof(Layers)).Length, worldWidth, worldHeight];
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
        if (SetTileClass(tileClass, tileClass.layer, x, y))
            tilemaps[(int)tileClass.layer].SetTile(new Vector3Int(x, y), tileClass.tile);

    }

    //�������÷���
    public void PlaceTiles(Layers layer, List<Vector3Int> pos, List<TileClass> tileClasss) {
        TileBase[] tileBases = SetTileClasses(layer, pos, tileClasss);
        if (tileBases == null) return;
        tilemaps[(int)layer].SetTiles(pos.ToArray(), tileBases);
    }
    public void PlaceLiquidTile(LiquidClass tileClass, int x, int y, float volume) {
        if (!CheckWorldBound(x, y)) return;

        if (tileClass != null) {
            SetLiquidTileClass(tileClass, Layers.Liquid, x, y, volume);
            //����Һ�岻ͬ������ò�ͬ��Ƭ
            TileBase tile = tileClass.GetTileToVolume(LiquidHandler.Instance.liquidVolume[x, y]);
            tilemaps[(int)Layers.Liquid].SetTile(new Vector3Int(x, y), tile);
        }
    }
    //������Ƭ����
    public bool SetTileClass(TileClass tileClass, Layers layer, int x, int y) {
        if (!CheckWorldBound(x, y)) return false;
        tileIds[(int)layer, x, y] = tileClass == null ? 0 : tileClass.blockId;
        return true;
    }

    //����Һ����Ƭ����
    public bool SetLiquidTileClass(LiquidClass liquidClass, Layers layer, int x, int y, float volume) {
        if (!SetTileClass(liquidClass, layer, x, y)) return false;
        float curVolume = liquidClass == null ? 0 : volume;
        LiquidHandler.Instance.liquidVolume[x, y] += curVolume;
        LiquidHandler.Instance.MarkForUpdate(liquidClass, new Vector2Int(x, y));
        return true;

    }
    //����������Ƭ����
    public TileBase[] SetTileClasses(Layers layer, List<Vector3Int> pos, List<TileClass> tileClasss) {
        if (pos.Count != tileClasss.Count) return null;
        List<TileBase> tileBases = new List<TileBase>();
        bool result = false;
        for (int i = 0; i < pos.Count; i++) {
            Vector3Int p = pos[i];
            TileClass tileClass = tileClasss[i];
            result = SetTileClass(tileClass, layer, p.x, p.y);
            //ʧ��һ���ع�
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
    //��ȡָ��λ����Ƭ
    public TileClass GetTileClass(Layers layer, int x, int y) {
        if (!CheckWorldBound(x, y)) return null;
        long tileId = tileIds[(int)layer, x, y];
        TileClass tileClass = TileRegistry.GetTile(tileId);
        return tileClass;
    }

    //������Ƭ
    public void Erase(Layers layer, int x, int y) {
        TileClass targetTile = GetTileClass(layer, x, y);
        SetTileClass(null, layer, x, y);
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
            Vector2Int pos = new Vector2Int(x, y);
            //��Ǹ���
            LiquidHandler.Instance.MarkForUpdate((LiquidClass)targetTile, pos + Vector2Int.up);
        }
    }
    //����������Ƭ
    public void Erases(List<Vector3Int> pos, Layers layer) {
        foreach (var item in pos) {

            SetTileClass(null, layer, item.x, item.y);
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
        for (int i = 0; i < tileIds.GetLength(0); i++) {
            //TODO������ǿת��֪����û������
            Layers layer = (Layers)Enum.ToObject(typeof(Layers), i);
            TileClass tileClass = GetTileClass(layer, x, y);
            if (tileClass.lightLevel > lightValue)
                lightValue = tileClass.lightLevel;
        }
        return lightValue;
    }

    #endregion



    //���������Ƭ
#if UNITY_EDITOR

    //��ȡ�ʲ����е�����װ������
    [ContextMenu("�����Ʒ����")]
    private void GetTileClassBase() {

        //����Ŀ¼�е�װ�����ݣ����ص���GUID
        string[] assetNames = AssetDatabase.FindAssets("", new[] { "Assets/Old/Tiles" });
        foreach (string SOName in assetNames) {
            var SOpath = AssetDatabase.GUIDToAssetPath(SOName);//GUIDתΪ���ʵ����Ŀ·��
            var itemData = AssetDatabase.LoadAssetAtPath<TileClass>(SOpath);//����·����ȡָ�����
            if (itemData == null) continue;
            //if (tileDictionary.ContainsKey(itemData.blockId)) continue;
        }
    }

#endif



    //��ͼ�����뱣��
    public void LoadData(MapData data) {
        for (int i = 0; i < tileIds.GetLength(0); i++) {
            for (int x = 0; x < worldWidth; x++) {
                for (int y = 0; y < worldHeight; y++) {
                    long tileBlockId = data.tileDatas[i, x, y];
                    tileIds[i, x, y] = tileBlockId;
                }
            }
        }
    }

    public void SaveData(ref MapData data) {
        for (int i = 0; i < tileIds.GetLength(0); i++) {
            //�����鱣��
            for (int x = 0; x < worldWidth; x++) {
                for (int y = 0; y < worldHeight; y++) {
                    long tileId = tileIds[i, x, y];
                    data.tileDatas[i, x, y] = tileId;
                }
            }
        }

    }
}
