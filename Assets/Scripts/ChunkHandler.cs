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
    private HashSet<Vector2Int> loadedChunkIDs = new HashSet<Vector2Int>();//�����Ѽ��ص�����

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
        public ChunkState state = ChunkState.Unloaded;//�������״̬
        public Bounds bounds;//���鷶Χ��
    }

    private void Update() {
        Vector2Int currentChunk = WorldToChunkCoord(renderCamera.transform.position);

        // ������ƶ���������ʱ���¼���
        if (currentChunk != lastLoadedChunk) {
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
                    if (chunkID.x >= chunks.GetLength(0) || chunkID.x < 0 || chunkID.y >= chunks.GetLength(1) || chunkID.y < 0) continue;
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

        // �����������ȼ��������
        chunksToLoad.Sort((a, b) =>
            Vector2Int.Distance(centerChunk, a).CompareTo(Vector2Int.Distance(centerChunk, b)));
        if (loadingCoroutine != null) StopCoroutine(loadingCoroutine);
        loadingCoroutine = StartCoroutine(UpdateLoadChunks(chunksToLoad));
    }


    //������Ұ�ڵ�����
    private IEnumerator UpdateLoadChunks(List<Vector2Int> visiblePos) {
        int processed = 0;
        foreach (var chunkID in visiblePos) {
            ChunkData chunk = chunks[chunkID.x, chunkID.y];

            if (chunk.state == ChunkState.Unloaded) {
                //��ֹ���ص�����Э��̫�࣬���¿��١�������̫СҲ�ᵼ�¼����ٶ�̫��������������ٶ�
                if (processed++ % 5 == 0)
                    yield return null;
                StartCoroutine(LoadChunkAsync(chunk));
            }
        }
    }
    //ж���Ѽ��ص�����
    private void UpdateUnLoadChunks(List<Vector2Int> chunkIDs) {
        foreach (var chunkID in chunkIDs) {
            ChunkData chunk = chunks[chunkID.x, chunkID.y];
            if (chunk.state == ChunkState.Loaded)
                StartCoroutine(UnloadChunk(chunk));
        }
    }

    // �첽��������
    IEnumerator LoadChunkAsync(ChunkData chunk) {
        Layers[] layers = (Layers[])Enum.GetValues(typeof(Layers));
        // �����������ж�أ��ȴ�ж�����
        while (chunk.state == ChunkState.Unloading) {
            yield return null;
        }

        chunk.state = ChunkState.Loading;

        foreach (Layers layer in layers) {
            List<TileBase> tileBases = new List<TileBase>();

            //Һ����LiquidHandler��Ⱦ������
            if (layer == Layers.Liquid) continue;
            foreach (Vector3Int tilePos in chunk.tilePos) {
                TileClass tileClass = world.GetTileClass(layer, tilePos.x, tilePos.y);

                tileBases.Add(tileClass?.tile);
            }

            BoundsInt bound = ToBoundsInt(chunk.bounds);
            world.tilemaps[(int)layer].SetTilesBlock(bound, tileBases.ToArray());
            yield return null; // ÿ����һ����ͣһ֡
        }

        chunk.state = ChunkState.Loaded;

        loadedChunkIDs.Add(chunk.coord);
    }
    //תΪInt��Χ��
    private BoundsInt ToBoundsInt(Bounds bounds) {
        Vector3Int min = new Vector3Int(Mathf.FloorToInt(bounds.min.x), Mathf.FloorToInt(bounds.min.y), 0);
        Vector3Int size = new Vector3Int(Mathf.FloorToInt(bounds.size.x), Mathf.FloorToInt(bounds.size.y), 1);
        return new BoundsInt(min, size);
    }
    // ж������
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
            yield return null; // ÿ����һ����ͣһ֡
        }


        chunk.state = ChunkState.Unloaded;
        loadedChunkIDs.Remove(chunk.coord);
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