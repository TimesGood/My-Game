using System.Collections;
using System.Collections.Generic;
using TreeEditor;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Tilemaps;

//�������ݹ���
public class ChunkHandler : Singleton<ChunkHandler> {

    public WorldGeneration world;

    public int chunkCount = 20; //X��Y������̶�
    public int chunkXCount;     //X��������
    public int chunkYCount;     //Y��������
    public int chunkXSize;      //X��ÿ�������ش�С
    public int chunkYSize;      //Y��ÿ�������ش�С

    public Camera renderCamera;
    public float padding = 2f;   // �ӿ��⻺��������
    private ChunkData[,] chunks;
    private List<ChunkData> activeChunks = new List<ChunkData>();


    private void Update() {
        UpdateVisibleChunks();
    }


    public class ChunkData {
        public Vector2Int coord;//��������
        public Vector3Int[] tilePos;//������Ƭ����
        public int[] tileIDs;//��ƬId
        public bool isLoaded;//�Ƿ��Ѽ�����Ⱦ
        public Bounds bounds;//���鷶Χ��
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



    // ����������߽�
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

    // ���¿ɼ��ֿ�
    private void UpdateVisibleChunks() {
        Bounds camBounds = GetCameraBounds();
        //List<ChunkData> newActiveChunks = new List<ChunkData>();

        // �������зֿ�
        for (int x = 0; x < chunkXCount; x++) {
            for (int y = 0; y < chunkYCount; y++) {
                if (chunks[x, y].bounds.Intersects(camBounds)) {
                    if (!chunks[x, y].isLoaded) {
                        StartCoroutine(LoadChunkAsync(chunks[x, y]));
                    }
                    //newActiveChunks.Add(chunks[x, y]);
                } else {
                    if (chunks[x, y].isLoaded) {
                        StartCoroutine(UnloadChunk(chunks[x, y]));
                    }
                }
            }
        }
        // ���»�ֿ��б�
        //activeChunks = newActiveChunks;
    }

    
    //ʹ��SetTilesBlock������������
    // �첽���طֿ�
    IEnumerator LoadChunkAsync(ChunkData chunk) {
        //List<Vector3Int>[] positionsByLayer = new List<Vector3Int>[4];//ÿ����Ƭ����
        List<TileBase>[] tilesByLayer = new List<TileBase>[4];//ÿ����Ƭ��
        for (int i = 0; i < 4; i++) {
            //positionsByLayer[i] = new List<Vector3Int>();
            tilesByLayer[i] = new List<TileBase>();
        }
        for (int i = 0; i < 4; i++) {
            //Һ����LiquidHandler��Ⱦ������
            if (i == (int)Layers.Liquid) continue;
            foreach (Vector3Int tilePos in chunk.tilePos) {
                TileClass tileClass = world.tileDatas[i, tilePos.x, tilePos.y];
                

                tilesByLayer[i].Add(tileClass?.tile);
                //positionsByLayer[i].Add(tilePos);
            }

            BoundsInt bound = ToBoundsInt(chunk.bounds);
            world.tilemaps[i].SetTilesBlock(bound, tilesByLayer[i].ToArray());

            tilesByLayer[i].Clear();
            //positionsByLayer[i].Clear();
            yield return null; // ÿ����һ����ͣһ֡
        }
        chunk.isLoaded = true;
    }
    //תΪInt��Χ��
    private BoundsInt ToBoundsInt(Bounds bounds) {
        Vector3Int min = new Vector3Int(Mathf.FloorToInt(bounds.min.x), Mathf.FloorToInt(bounds.min.y), 0);
        Vector3Int size = new Vector3Int(Mathf.FloorToInt(bounds.size.x), Mathf.FloorToInt(bounds.size.y), 1);
        return new BoundsInt(min, size);
    }
    // ж�طֿ�
    IEnumerator UnloadChunk(ChunkData chunk) {
        for (int i = 0; i < 4; i++) {
            BoundsInt bound = ToBoundsInt(chunk.bounds);
            world.tilemaps[i].SetTilesBlock(bound, new TileBase[bound.size.x * bound.size.y]);
            yield return null; // ÿ����һ����ͣһ֡
        }
        chunk.isLoaded = false;
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


}
