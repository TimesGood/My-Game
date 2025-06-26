
using System.Collections.Generic;
using System.IO;
using UnityEngine.Tilemaps;
using UnityEngine;
using System;
using Unity.Collections.LowLevel.Unsafe;
using System.Collections;
using System.Diagnostics;
using Debug = UnityEngine.Debug;


//��Ƭ��ͼ�������
public class TilemapExporter : MonoBehaviour {
    public int chunkSize = 16;
    public string exportFileName = "tilemap_data.bin";

    public WorldGeneration world;

    [ContextMenu("save file")]
    public void ExportTest() {
        StartCoroutine(ExportAllTilemaps());
    }
    public IEnumerator ExportAllTilemaps() {
        string path = Path.Combine(Application.streamingAssetsPath, exportFileName);

        ChunkHandler chunkHandler = ChunkHandler.Instance;
        ChunkHandler.ChunkData[,] chunkDatas = chunkHandler.GetChunkDatas();
        int chunkCount = chunkDatas.GetLength(0) * chunkDatas.GetLength(1);
        // ���浽�ļ�
        using (BinaryWriter writer = new BinaryWriter(File.OpenWrite(path))) {
            // д��汾��
            writer.Write(1);

            // д����������
            writer.Write(chunkCount);
            float processed = 0;
            foreach (ChunkHandler.ChunkData chunk in chunkDatas) {
                WriteChunk(writer, chunk);
                
                if (++processed % 100 == 0) {
                    Debug.Log("��ͼ���ݵ�����, ���ȣ�" + (int)(++processed / chunkCount * 100));
                    yield return null;
                }
                
            }

            Debug.Log($"Exported {chunkCount} chunks to {path}");
        }
    }

    [ContextMenu("load file")]
    public void LoadTest() {
        List<ChunkHandler.ChunkData> chunkList = new List<ChunkHandler.ChunkData>();
        StartCoroutine(LoadAllTilemaps());
    }

    public IEnumerator LoadAllTilemaps() {
        string path = Path.Combine(Application.streamingAssetsPath, exportFileName);
        using (BinaryReader reader = new BinaryReader(File.OpenRead(path))) {
            // ��ȡ�汾��
            int version = reader.ReadInt32();

            // ��ȡ��������
            float processed = 0;
            int chunkCount = reader.ReadInt32();
            for (int i = 0; i < chunkCount; i++) {
                ChunkHandler.ChunkData chunk = ReadChunk(reader);
                if (++processed % 100 == 0) {
                    Debug.Log("��ͼ������, ���ȣ�" + (int)(++processed / chunkCount * 100));
                    yield return null;
                }
            }
        }

    }


    //д����������
    private void WriteChunk(BinaryWriter writer, ChunkHandler.ChunkData chunk) {
        writer.Write(chunk.coord.x);
        writer.Write(chunk.coord.y);
        writer.Write(chunk.tilePos.Length);
        for (int i = 0; i < chunk.tilePos.Length; i++) {
            int x = chunk.tilePos[i].x;
            int y = chunk.tilePos[i].y;
            writer.Write(x);
            writer.Write(y);
            Layers[] layers = (Layers[])Enum.GetValues(typeof(Layers));
            foreach (var layer in layers) {
                TileClass tileClass = world.GetTileClass(layer, x, y);
                writer.Write(tileClass == null ? 0 : tileClass.blockId);
            }
        }
    }
    //������������
    private ChunkHandler.ChunkData ReadChunk(BinaryReader reader) {
        ChunkHandler.ChunkData chunk = new ChunkHandler.ChunkData();
        chunk.coord = new Vector2Int(reader.ReadInt32(), reader.ReadInt32());

        int tileCount = reader.ReadInt32();
        //chunk.tilePos = new Vector3Int[tileCount];

        for (int t = 0; t < tileCount; t++) {
            int x = reader.ReadInt32();
            int y = reader.ReadInt32();
            //chunk.tilePos[t] = new Vector3Int(x, y, 0);
            Layers[] layers = (Layers[])Enum.GetValues(typeof(Layers));
            foreach (var layer in layers) {
                long tileID = reader.ReadInt64();
        
                TileClass tileClass = WorldGeneration.TileRegistry.GetTile(tileID);
                world.SetTileClass(tileClass, layer, x, y);
            }
        }
        return chunk;
    }

}