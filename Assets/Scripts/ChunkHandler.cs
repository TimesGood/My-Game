using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Tilemaps;

//�������ݹ���
public class ChunkHandler : Singleton<ChunkHandler> {

    public WorldGeneration world;
    public float loadRadius = 3f;//���ط�Χ

    public int chunkCount = 20; //X��Y����������
    public int chunkXCount;     //X��������
    public int chunkYCount;     //Y��������
    public int chunkXSize;      //X��ÿ�������ش�С
    public int chunkYSize;      //Y��ÿ�������ش�С
    private static TileBase[] emptyTiles;
    public int maxCachedChunks = 50; // ��󻺴�������

    public Camera renderCamera; // �����������Ŀ�꣩
    public float padding = 2f;   // �ӿ��⻺��������

    private HashSet<Vector2Int> loadedChunkIDs = new HashSet<Vector2Int>();//�����Ѽ��ص�����
    private Vector2Int lastLoadedChunk = new Vector2Int(int.MinValue, int.MinValue);
    private Dictionary<Vector2Int, ChunkData> chunkDataCache = new Dictionary<Vector2Int, ChunkData>();//�������ݻ��棬�����ظ�����


    private Coroutine loadingCoroutine;

    //��������
    public class ChunkData {
        public Vector2Int coord;//��������
        public List<TileBase>[] tileBases;//������Ƭ��
        public BoundsInt bounds;//���鷶Χ��
        public DateTime lastAccessTime;  // ������ʱ�䣨���ڻ������
    }

    protected override void Awake() {
        base.Awake();
        //����
        chunkXCount = chunkCount;
        chunkYCount = chunkCount * world.worldHeight / world.worldWidth;
        //ÿ��������Ƭ
        chunkXSize = world.worldWidth / chunkXCount;
        chunkYSize = world.worldHeight / chunkYCount;
        //����Ƭ���飬����ж��
        emptyTiles = new TileBase[chunkXSize * chunkXSize];

    }

    private void Update() {
        Vector2Int currentChunk = WorldToChunkCoord(renderCamera.transform.position);

        // ������ƶ���������ʱ���¼���
        if (currentChunk != lastLoadedChunk) {
            LoadChunksAroundCamera();
            lastLoadedChunk = currentChunk;
        }
    }

    private ChunkData GetChunkData(int chunkX, int chunkY) {
        Vector2Int coord = new Vector2Int(chunkX, chunkY);

        if (!chunkDataCache.TryGetValue(coord, out var data)) {
            data = BuildChunkData(chunkX, chunkY);
            chunkDataCache[coord] = data;
            CleanupChunkCache(); // ������
        }

        data.lastAccessTime = DateTime.Now;
        return data;
    }
    // ������ڵ����黺��
    private void CleanupChunkCache() {
        if (chunkDataCache.Count <= maxCachedChunks)
            return;

        // �Ƴ����δʹ�õ�����
        var chunksToRemove = chunkDataCache.OrderBy(x => x.Value.lastAccessTime)
                                          .Take(chunkDataCache.Count - maxCachedChunks)
                                          .ToList();

        foreach (var chunk in chunksToRemove) {
            chunkDataCache.Remove(chunk.Key);
        }
    }
    //������������
    private ChunkData BuildChunkData(int chunkX, int chunkY) {
        ChunkData chunk = new ChunkData {
            coord = new Vector2Int(chunkX, chunkY),
            bounds = new BoundsInt(
                        new Vector3Int(
                            chunkX * chunkXSize,
                            chunkY * chunkYSize, 0),
                        new Vector3Int(chunkXSize, chunkYSize, 1))
        };
        Layers[] layers = (Layers[])Enum.GetValues(typeof(Layers));
        List<TileBase>[] tileBases = new List<TileBase>[layers.Length];
        //��y��ʼ������ʹ��SetTilesBlock
        for (int y = 0; y < chunkXSize; y++) {
            for (int x = 0; x < chunkYSize; x++) {
                int worldXPos = chunk.coord.x * chunkXSize + x;
                int worldYPos = chunk.coord.y * chunkYSize + y;
                foreach (var layer in layers) {
                    TileClass tileClass = world.GetTileClass(layer, worldXPos, worldYPos);
                    if (tileBases[(int)layer] == null) tileBases[(int)layer] = new List<TileBase>();
                    tileBases[(int)layer].Add(tileClass == null ? null : tileClass.tile);
                }

            }
        }
        chunk.tileBases = tileBases;
        return chunk;
    }

    // ��ȡ���������
    public Bounds GetCameraBounds() {
        Vector3[] frustumCorners = new Vector3[4];
        renderCamera.CalculateFrustumCorners(
            new Rect(0, 0, 1, 1),
            renderCamera.farClipPlane,
            Camera.MonoOrStereoscopicEye.Mono,
            frustumCorners
        );

        Matrix4x4 camMatrix = renderCamera.transform.localToWorldMatrix;
        for (int i = 0; i < 4; i++) {
            frustumCorners[i] = camMatrix.MultiplyPoint(frustumCorners[i]);
            frustumCorners[i].z = 0;
        }

        Bounds bounds = new Bounds(frustumCorners[0], Vector3.zero);
        foreach (Vector3 corner in frustumCorners) {
            bounds.Encapsulate(corner);
        }

        // ��չ�߽緶Χ
        bounds.Expand(padding * chunkXSize);

        return bounds;
    }

    private void LoadChunksAroundCamera() {

        Vector2Int centerChunk = WorldToChunkCoord(renderCamera.transform.position);
        // ���ҷ�Χ������
        List<Vector2Int> chunksToLoad = new List<Vector2Int>();
        int radius = Mathf.CeilToInt(loadRadius);
        for (int y = -radius; y <= radius; y++) {
            for (int x = -radius; x <= radius; x++) {
                Vector2Int chunkID = centerChunk + new Vector2Int(x, y);

                // ֻ����Բ�������ڵ�����
                if (Vector2Int.Distance(centerChunk, chunkID) <= loadRadius) {
                    //Խ��
                    if (chunkID.x >= chunkXCount || chunkID.x < 0 || chunkID.y >= chunkYCount || chunkID.y < 0) continue;
                    chunksToLoad.Add(chunkID);
                }
            }
        }


        List<Vector2Int> toUnload = new List<Vector2Int>();
        foreach (var chunkID in loadedChunkIDs) {
            if (!chunksToLoad.Contains(chunkID)) {
                toUnload.Add(chunkID);
            }
        }


        UpdateUnLoadChunks(toUnload);
        // �����ȼ����� (��������)
        chunksToLoad.Sort((a, b) =>
            Vector2Int.Distance(centerChunk, a).CompareTo(Vector2Int.Distance(centerChunk, b)));

        if (loadingCoroutine != null) StopCoroutine(loadingCoroutine);
        loadingCoroutine = StartCoroutine(UpdateLoadChunks(chunksToLoad));

    }

    //������Ұ�ڵ�����
    private IEnumerator UpdateLoadChunks(List<Vector2Int> visiblePos) {
        
        int processed = 0;
        foreach (var chunkID in visiblePos) {
            if (loadedChunkIDs.Contains(chunkID)) continue;
            
            //��ֹ���ص�����Э��̫�࣬���¿��١�������̫СҲ�ᵼ�¼����ٶ�̫��������������ٶ�
            if (processed++ % 5 == 0)
                yield return null;
            StartCoroutine(LoadChunk(chunkID));

        }
    }
    //ж���Ѽ��ص�����
    private void UpdateUnLoadChunks(List<Vector2Int> chunkIDs) {
        foreach (var chunkID in chunkIDs) {
            
            StartCoroutine(UnloadChunk(chunkID));
        }
    }

    // ��������
    IEnumerator LoadChunk(Vector2Int chunkID) {
        Layers[] layers = (Layers[])Enum.GetValues(typeof(Layers));
        ChunkData chunkData = GetChunkData(chunkID.x, chunkID.y);
        foreach (Layers layer in layers) {

            BoundsInt bound = chunkData.bounds;
            world.tilemaps[(int)layer].SetTilesBlock(bound, chunkData.tileBases[(int)layer].ToArray());
            yield return null; // ÿ����һ����ͣһ֡
        }

        loadedChunkIDs.Add(chunkID);
    }

    // ж������
    private IEnumerator UnloadChunk(Vector2Int chunkID) {
        ChunkData chunkData = GetChunkData(chunkID.x, chunkID.y);
        Layers[] layers = (Layers[])Enum.GetValues(typeof(Layers));

        foreach (Layers layer in layers) {
            BoundsInt bound = chunkData.bounds;
            world.tilemaps[(int)layer].SetTilesBlock(chunkData.bounds, emptyTiles);
            world.tilemaps[(int)layer].CompressBounds();
            yield return null; // ÿ����һ����ͣһ֡
        }

        loadedChunkIDs.Remove(chunkID);
    }


    //��Ⱦ��������
    private IEnumerator LoadAllChunkAsync() {

        //==============================��������Ⱦ=============================

        for (int chunkXIndex = 0; chunkXIndex < chunkXCount; chunkXIndex++) {
            for (int chunkYIndex = 0; chunkYIndex < chunkYCount; chunkYIndex++) {

                StartCoroutine(LoadChunk(new Vector2Int(chunkXIndex, chunkYIndex)));
                yield return null; // ÿ����һ������ͣһ֡ 
            }
        }
    }

    //��������ת��������
    private Vector2Int WorldToChunkCoord(Vector3 worldPos) {
        return new Vector2Int(
            Mathf.FloorToInt(worldPos.x / chunkXSize),
            Mathf.FloorToInt(worldPos.y / chunkYSize)
        );
    }

    //��ȡָ������ʵ����Ƭ����
    public Vector3Int GetActualIndex(int chunkXIndex, int chunkYIndex, int chunkTileXIndex, int chunkTileYIndex) {
        int x = chunkTileXIndex + (chunkXIndex * chunkXSize);
        int y = chunkTileYIndex + (chunkYIndex * chunkXSize);
        return new Vector3Int(x, y);
    }

    //��ȡ�������ݴ���
    public ChunkData[,] GetChunkDatas() {
        return null;
    }

}