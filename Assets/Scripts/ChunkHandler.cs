using System;
using System.Collections;
using System.Collections.Generic;
using TreeEditor;
using Unity.VisualScripting;
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

    public Camera renderCamera;
    public float padding = 2f;   // �ӿ��⻺��������
    private ChunkData[,] chunks;
    private Dictionary<Vector2Int, ChunkData> loadedChunks = new Dictionary<Vector2Int, ChunkData>();//�����Ѽ��ص�����
    private HashSet<Vector2Int> unloadingChunks = new HashSet<Vector2Int>(); // ��������ж�ص�����
    private Vector2Int lastLoadedChunk = new Vector2Int(int.MinValue, int.MinValue);

    private Coroutine loadingCoroutine;

    // �������״̬ö��
    public enum ChunkState {
        Unloaded,
        Loading,
        Loaded,
        Unloading
    }


    public class ChunkData {
        public Vector2Int coord;//��������
        public Vector3Int[] tilePos;//������Ƭ����
        public int[] tileIDs;//��ƬId
        public ChunkState state = ChunkState.Unloaded;
        public Bounds bounds;//���鷶Χ��
    }

    private void Update() {
        Vector2Int currentChunk = WorldToChunkCoord(renderCamera.transform.position);

        if (currentChunk != lastLoadedChunk || Time.frameCount % 10 == 0) //ÿ10֡ǿ�Ƽ��
        {
            LoadChunksAroundCamera();
            lastLoadedChunk = currentChunk;
        }
    }


    //��ʼ������
    public void InitChunk() {
        //����
        chunkXCount = chunkCount;
        chunkYCount = chunkCount * world.worldHeight / world.worldWidth;
        chunks = new ChunkData[chunkXCount, chunkYCount];
        //ÿ��������Ƭ
        chunkXSize = world.worldWidth / chunkXCount;
        chunkYSize = world.worldHeight / chunkYCount;
        for (int x = 0; x < chunkXCount; x++) {
            for (int y = 0; y < chunkYCount; y++) {
                chunks[x, y] = new ChunkData {
                    coord = new Vector2Int(x, y),
                    bounds = new Bounds(
                        new Vector3(
                            x * chunkXSize + chunkXSize / 2f,
                            y * chunkYSize + chunkYSize / 2f, 0),
                        new Vector3(chunkXSize, chunkYSize, 0))
                };
                GenerateChunkData(chunks[x, y]);
            }
        }
    }

    //����������Ƭ����
    private void GenerateChunkData(ChunkData chunk) {
        List<Vector3Int> tilePos = new List<Vector3Int>();
        List<int> tileIds = new List<int>();
        //��y��ʼ������ʹ��SetTilesBlock
        for (int y = 0; y < chunkXSize; y++) {
            for (int x = 0; x < chunkYSize; x++) {
                Vector3Int tilemapPos = new Vector3Int(
                    chunk.coord.x * chunkXSize + x,
                    chunk.coord.y * chunkYSize + y,
                    0
                    );
                tilePos.Add(tilemapPos);
            }
        }
        chunk.tilePos = tilePos.ToArray();
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

        if (loadingCoroutine != null) {
            StopCoroutine(loadingCoroutine);
        }

        loadingCoroutine = StartCoroutine(UpdateVisibleChunks());
    }

    // ���¿ɼ��ֿ�
    private IEnumerator UpdateVisibleChunks() {

        Vector2Int centerChunk = WorldToChunkCoord(renderCamera.transform.position);
        List<Vector2Int> chunksToLoad = new List<Vector2Int>();

        //��Բ�ν�����Ⱦ
        int radius = Mathf.CeilToInt(loadRadius);
        for (int y = -radius; y <= radius; y++) {
            for (int x = -radius; x <= radius; x++) {
                Vector2Int chunkID = centerChunk + new Vector2Int(x, y);

                if (Vector2Int.Distance(centerChunk, chunkID) > loadRadius)
                    continue;

                if (chunkID.x < 0 || chunkID.x >= chunkXCount ||
                    chunkID.y < 0 || chunkID.y >= chunkYCount)
                    continue;

                chunksToLoad.Add(chunkID);
            }
        }

        // �����������ȼ��������
        chunksToLoad.Sort((a, b) =>
            Vector2Int.Distance(centerChunk, a).CompareTo(Vector2Int.Distance(centerChunk, b)));

        //����������ν�����Ⱦ
        //Bounds bounds = GetCameraBounds();
        //for (int y = (int)bounds.min.y; y <= bounds.max.y; y++) {
        //    for (int x = (int)bounds.min.x; x <= bounds.max.x; x++) {
        //        Vector2Int chunkID = GetChunkIndex(x, y);

        //        if (chunkID.x < 0 || chunkID.x >= chunkXCount ||
        //            chunkID.y < 0 || chunkID.y >= chunkYCount)
        //            continue;

        //        chunksToLoad.Add(chunkID);

        //    }
        //}

        // ж�ز�����Ұ�ڵ�����
        List<Vector2Int> toUnload = new List<Vector2Int>();
        foreach (var kvp in loadedChunks) {
            if (!chunksToLoad.Contains(kvp.Key) &&
                kvp.Value.state == ChunkState.Loaded) {
                toUnload.Add(kvp.Key);
            }
        }
        
        // ��ʼж��
        foreach (var chunkID in toUnload) {
            if (unloadingChunks.Contains(chunkID)) continue;

            StartCoroutine(UnloadChunk(loadedChunks[chunkID]));
            unloadingChunks.Add(chunkID);
        }
        //int processed = 0;
        // ����������
        foreach (var chunkID in chunksToLoad) {
            ChunkData chunk = chunks[chunkID.x, chunkID.y];

            if (chunk.state == ChunkState.Unloaded &&
                !unloadingChunks.Contains(chunkID)) {
                ////��ֹ����Э��̫����ɿ���
                //if (processed++ % 5 == 0)
                //    yield return null;
                StartCoroutine(LoadChunkAsync(chunk));
            }
        }
        yield return null;
    }

    //ʹ��SetTilesBlock������������
    // �첽���طֿ�
    IEnumerator LoadChunkAsync(ChunkData chunk) {
        // �����������ж�أ��ȴ�ж�����
        while (unloadingChunks.Contains(chunk.coord)) {
            yield return null;
        }

        chunk.state = ChunkState.Loading;

        Layers[] layers = (Layers[])Enum.GetValues(typeof(Layers));
        foreach (Layers layer in layers) {
            if (layer == Layers.Liquid) continue;

            List<TileBase> tileBases = new List<TileBase>();
            foreach (Vector3Int tilePos in chunk.tilePos) {
                TileClass tileClass = world.GetTileClass(layer, tilePos.x, tilePos.y);
                tileBases.Add(tileClass?.tile);
            }

            BoundsInt bound = ToBoundsInt(chunk.bounds);
            world.tilemaps[(int)layer].SetTilesBlock(bound, tileBases.ToArray());
            yield return null;
        }

        chunk.state = ChunkState.Loaded;

        // ȷ��ֻ���һ��
        if (!loadedChunks.ContainsKey(chunk.coord)) {
            loadedChunks.Add(chunk.coord, chunk);
        }
    }
    //תΪInt��Χ��
    private BoundsInt ToBoundsInt(Bounds bounds) {
        Vector3Int min = new Vector3Int(Mathf.FloorToInt(bounds.min.x), Mathf.FloorToInt(bounds.min.y), 0);
        Vector3Int size = new Vector3Int(Mathf.FloorToInt(bounds.size.x), Mathf.FloorToInt(bounds.size.y), 1);
        return new BoundsInt(min, size);
    }
    // ж�طֿ�
    private IEnumerator UnloadChunk(ChunkData chunk) {
        // ����������ڼ��أ��ȴ��������
        while (chunk.state == ChunkState.Loading) {
            yield return null;
        }

        chunk.state = ChunkState.Unloading;

        Layers[] layers = (Layers[])Enum.GetValues(typeof(Layers));
        foreach (Layers layer in layers) {
            BoundsInt bound = ToBoundsInt(chunk.bounds);
            world.tilemaps[(int)layer].SetTilesBlock(bound, new TileBase[bound.size.x * bound.size.y]);
            yield return null;
        }

        chunk.state = ChunkState.Unloaded;
        loadedChunks.Remove(chunk.coord);
        unloadingChunks.Remove(chunk.coord);
    }


    //��Ⱦ��������
    private IEnumerator LoadAllChunkAsync() {

        //==============================��������Ⱦ=============================

        for (int chunkXIndex = 0; chunkXIndex < chunkXCount; chunkXIndex++) {
            for (int chunkYIndex = 0; chunkYIndex < chunkYCount; chunkYIndex++) {

                StartCoroutine(LoadChunkAsync(chunks[chunkXIndex, chunkYIndex]));
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
    //��ȡʵ�������������������
    public Vector2Int GetChunkIndex(int x, int y) {
        //��������ȡ����ȡ��������
        int chunkXIndex = x / chunkXSize;
        int chunkYIndex = y / chunkYSize;
        return new Vector2Int(chunkXIndex, chunkYIndex);
    }

    //��ȡָ������ʵ����Ƭ����
    public Vector3Int GetActualIndex(int chunkXIndex, int chunkYIndex, int chunkTileXIndex, int chunkTileYIndex) {
        int x = chunkTileXIndex + (chunkXIndex * chunkXSize);
        int y = chunkTileYIndex + (chunkYIndex * chunkXSize);
        return new Vector3Int(x, y);
    }

    public ChunkData[,] GetChunkDatas() {
        return chunks;
    }

}