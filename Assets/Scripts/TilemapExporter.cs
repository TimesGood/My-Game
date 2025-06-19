
using System.Collections.Generic;
using System.IO;
using UnityEngine.Tilemaps;
using UnityEngine;
using System.Linq;
using static UnityEditor.Experimental.GraphView.GraphView;
#if UNITY_EDITOR
public class TilemapExporter : MonoBehaviour {
    public int chunkSize = 16;
    public string exportFileName = "tilemap_data.bin";

    // ��Ƭע���
    public static class TileRegistry {
        private static Dictionary<int, TileBase> tileDictionary = new Dictionary<int, TileBase>();
        private static Dictionary<TileBase, int> reverseLookup = new Dictionary<TileBase, int>();
        private static int nextID = 1;

        public static int RegisterTile(TileBase tile) {
            if (tile == null) return 0;

            if (reverseLookup.TryGetValue(tile, out int id)) {
                return id;
            }

            int newID = nextID++;
            tileDictionary.Add(newID, tile);
            reverseLookup.Add(tile, newID);
            return newID;
        }

        public static TileBase GetTile(int id) {
            if (id == 0) return null;
            return tileDictionary.TryGetValue(id, out var tile) ? tile : null;
        }

        public static void ClearRegistry() {
            tileDictionary.Clear();
            reverseLookup.Clear();
            nextID = 1;
        }
    }

    [ContextMenu("save file")]
    public void ExportAllTilemaps() {
        TileRegistry.ClearRegistry();
        string path = Path.Combine(Application.streamingAssetsPath, exportFileName);

        WorldGeneration world = WorldGeneration.Instance;
        // �ռ���������
        List<ChunkHandler.ChunkData> allChunks = new List<ChunkHandler.ChunkData>();

        for (int y = 0; y < world.worldHeight; y += chunkSize) {
            for (int x = 0; x < world.worldWidth; x += chunkSize) {
                Vector3Int chunkOrigin = new Vector3Int(x, y, 0);
                ChunkHandler.ChunkData chunk = new ChunkHandler.ChunkData();
                
                TileClass[,,] datas = world.tileDatas;
                // ����ÿ��ͼ��
                for (int i = 0; i < 4; i++) {
                    

                    List<Vector3Int> positions = new List<Vector3Int>();
                    List<int> tileIDs = new List<int>();

                    // ������ǰ����
                    for (int cy = 0; cy < chunkSize; cy++) {
                        for (int cx = 0; cx < chunkSize; cx++) {
                            Vector3Int pos = chunkOrigin + new Vector3Int(cx, cy, 0);
                            TileClass data = datas[i, cx, cy];
                            if (data == null) continue;
                            TileBase tile = data.tile;

                            if (tile != null) {
                                positions.Add(new Vector3Int(cx, cy, 0));
                                tileIDs.Add(TileRegistry.RegisterTile(tile));
                            }
                        }
                    }

                    // ֻ�������Ƭ��ͼ��
                    if (positions.Count > 0) {
                        chunk.tilePos = positions.ToArray();
                        chunk.coord = new Vector2Int(chunkOrigin.x, chunkOrigin.y);
                    }
                }
                // ֻ��������ݵ�����
                if (chunk.tilePos != null && chunk.tilePos.Length > 0) {
                    allChunks.Add(chunk);
                }
            }
        }

        // ���浽�ļ�
        using (BinaryWriter writer = new BinaryWriter(File.OpenWrite(path))) {
            // д��汾��
            writer.Write(1);

            // д����������
            writer.Write(allChunks.Count);

            foreach (ChunkHandler.ChunkData chunk in allChunks) {
                WriteChunk(writer, chunk);
            }

            Debug.Log($"Exported {allChunks.Count} chunks to {path}");
        }
    }
    private BoundsInt ToBoundsInt(Bounds bounds) {
        Vector3Int min = new Vector3Int(Mathf.FloorToInt(bounds.min.x), Mathf.FloorToInt(bounds.min.y), 0);
        Vector3Int size = new Vector3Int(Mathf.FloorToInt(bounds.size.x), Mathf.FloorToInt(bounds.size.y), 1);
        return new BoundsInt(min, size);
    }

    private void WriteChunk(BinaryWriter writer, ChunkHandler.ChunkData chunk) {
        writer.Write(chunk.coord.x);
        writer.Write(chunk.coord.y);
        for (int i = 0; i < chunk.tilePos.Length; i++) {
            writer.Write(chunk.tilePos[i].x);
            writer.Write(chunk.tilePos[i].y);

        }
    }
}
#endif