
using System.Collections.Generic;
using System.IO;
using UnityEngine.Tilemaps;
using UnityEngine;
using System;
using Unity.Collections.LowLevel.Unsafe;
using System.Collections;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using Unity.Entities.UniversalDelegates;
using JetBrains.Annotations;
using System.IO.Compression;
using CompressionLevel = System.IO.Compression.CompressionLevel;
using static UnityEngine.Rendering.DebugUI;
using UnityEngine.UIElements;


//瓦片地图数据输出
public class TilemapExporter : Singleton<TilemapExporter> {
    public int chunkSize = 16;
    public string exportFileName = "tilemap_data.bin";

    public WorldGeneration world;

    [ContextMenu("save file")]
    public void ExportTest() {
        Debug.Log("执行1");
        long[,][,,] chunks = WorldGeneration.Instance.GetChunks();
        StartCoroutine(ExportAllTilemaps(chunks, process => Debug.Log("地图数据导出中, 进度：" + process)));
    }
    //public IEnumerator ExportAllTilemaps(long[,][,,] mapData, Action<float> progressCallback) {
    //    string path = Path.Combine(Application.streamingAssetsPath, exportFileName);
    //    int chunkXCount = mapData.GetLength(0);
    //    int chunkYCount = mapData.GetLength(1);
    //    int chunkTotal = chunkXCount * chunkYCount;
    //    // 保存到文件
    //    using (BinaryWriter writer = new BinaryWriter(File.OpenWrite(path))) {
    //        // 写入版本号
    //        writer.Write(1);


    //        // 写入区块数量
    //        writer.Write(chunkTotal);
    //        writer.Write(chunkXCount);
    //        writer.Write(chunkYCount);
    //        float processed = 0;
    //        for (int chunkXIndex = 0; chunkXIndex < chunkXCount; chunkXIndex++) {
    //            for (int chunkYIndex = 0; chunkYIndex < chunkYCount; chunkYIndex++) {
    //                long[,,] chunk = mapData[chunkXIndex, chunkYIndex];
    //                WriteChunk(writer, chunk);

    //                if (++processed % 100 == 0) {
    //                    yield return null;
    //                    // 计算并报告进度 (0-100%)
    //                    float progress = (float)processed / chunkTotal * 100f;
    //                    progressCallback?.Invoke(progress);
    //                }
    //            }
            
    //        }


    //        Debug.Log($"Exported {chunkTotal} chunks to {path}");
    //    }
    //}
    public IEnumerator ExportAllTilemaps(long[,][,,] mapData, Action<float> progressCallback) {
        string path = Path.Combine(Application.streamingAssetsPath, exportFileName);
        int chunkXCount = mapData.GetLength(0);
        int chunkYCount = mapData.GetLength(1);
        int chunkTotal = chunkXCount * chunkYCount;

        // 使用压缩流包裹文件流（关键优化）
        using (FileStream fs = File.Create(path))
        using (GZipStream gzip = new GZipStream(fs, CompressionLevel.Optimal))  // 使用GZIP压缩
        using (BinaryWriter writer = new BinaryWriter(gzip))  // 写入压缩流
        {
            writer.Write(2);  // 版本号升级到2（标识新格式）

            // 写入区块元数据
            writer.Write(chunkXCount);
            writer.Write(chunkYCount);

            float processed = 0;
            for (int cx = 0; cx < chunkXCount; cx++) {
                for (int cy = 0; cy < chunkYCount; cy++) {
                    long[,,] chunk = mapData[cx, cy];
                    WriteChunkOptimized(writer, chunk);  // 使用优化后的写入方法

                    // 进度更新（每区块更新一次）
                    if (++processed % 10 == 0) {
                        yield return null;
                        progressCallback?.Invoke(processed / chunkTotal * 100f);
                    }
                }
            }
            Debug.Log($"Exported {chunkTotal} chunks to {path} (Compressed)");
        }
    }


    // RLE编码写入器
    private void WriteRLE(BinaryWriter writer, int length, long value) {
        // 4. 值优化：高频值用字节标记
        const long EMPTY = 0;
        if (value == EMPTY) {
            writer.Write((byte)0x00);  // 特殊标记：空瓦片
            writer.Write((ushort)length);  // 连续长度
        }
        // 5. 值范围压缩：根据实际值域选择存储类型
        else if (value <= ushort.MaxValue) {
            writer.Write((byte)0x01);  // 标记：16位值
            writer.Write((ushort)value);
            writer.Write((ushort)length);
        } else {
            writer.Write((byte)0x02);  // 标记：完整long
            writer.Write(value);
            writer.Write((ushort)length);
        }
    }

    [ContextMenu("load file")]
    public void LoadTest() {
        long[,][,,] chunkList = null;
        
        StartCoroutine(ImportAllTilemaps(
            value => world.SetChunks(value),
            process => Debug.Log("地图加载中, 进度：" + process)));
        //Debug.Log(chunkList.Count);
    }

    public IEnumerator LoadAllTilemaps(Action<long[,][,,]> onComplete, Action<float> progressCallback) {
        string path = Path.Combine(Application.streamingAssetsPath, exportFileName);
        using (BinaryReader reader = new BinaryReader(File.OpenRead(path))) {
            // 读取版本号
            int version = reader.ReadInt32();
            // 写入区块数量
            int chunkTotal = reader.ReadInt32();
            int chunkXCount = reader.ReadInt32();
            int chunkYCount = reader.ReadInt32();
            long[,][,,] chunks = new long[chunkXCount,chunkYCount][,,];
            int processed = 0;
            for (int chunkXIndex = 0; chunkXIndex < chunkXCount; chunkXIndex++) {
                for (int chunkYIndex = 0; chunkYIndex < chunkYCount; chunkYIndex++) {
                    long[,,] chunk = ReadChunk(reader);
                    chunks[chunkXIndex, chunkYIndex] = chunk;
                    if (++processed % 100 == 0) {
                        yield return null;
                        // 计算并报告进度 (0-100%)
                        float progress = (float)processed / chunkTotal * 100f;
                        progressCallback?.Invoke(progress);
                    }
                }

            }
            onComplete(chunks);
        }

    }
    public IEnumerator ImportAllTilemaps(Action<long[,][,,]> onComplete, Action<float> progressCallback) {
        string path = Path.Combine(Application.streamingAssetsPath, exportFileName);
        if (!File.Exists(path)) {
            Debug.LogError($"File not found: {path}");
            yield break;
        }

        using (FileStream fs = File.OpenRead(path))
        using (GZipStream gzip = new GZipStream(fs, CompressionMode.Decompress))
        using (BinaryReader reader = new BinaryReader(gzip)) {
            // 读取版本号
            int version = reader.ReadInt32();
            if (version != 2) {
                Debug.LogError($"Unsupported file version: {version}. Expected version 2.");
                yield break;
            }

            // 读取区块元数据
            int chunkXCount = reader.ReadInt32();
            int chunkYCount = reader.ReadInt32();
            int chunkTotal = chunkXCount * chunkYCount;

            // 初始化地图数据结构
            long[,][,,] mapData = new long[chunkXCount, chunkYCount][,,];
            float processed = 0;

            for (int cx = 0; cx < chunkXCount; cx++) {
                for (int cy = 0; cy < chunkYCount; cy++) {
                    mapData[cx, cy] = ReadChunkOptimized(reader);

                    // 进度更新
                    if (++processed % 10 == 0) {
                        yield return null;
                        progressCallback?.Invoke(processed / chunkTotal * 100f);
                    }
                }
            }

            Debug.Log($"Imported {chunkTotal} chunks from {path}");
            onComplete?.Invoke(mapData);
        }

    }

    // 优化后的区块写入方法
    private void WriteChunkOptimized(BinaryWriter writer, long[,,] chunk) {
        int layers = chunk.GetLength(0);
        int width = chunk.GetLength(1);
        int height = chunk.GetLength(2);

        // 1. 写入区块维度（各层尺寸相同）
        writer.Write((ushort)width);
        writer.Write((ushort)height);

        // 2. 按层处理数据
        for (int l = 0; l < layers; l++) {
            for (int y = 0; y < height; y++) {
                for (int x = 0; x < width; x++) {
                    long tileId = chunk[l, x, y];
                    const long EMPTY = 0;
                    if (tileId == EMPTY) {
                        writer.Write((byte)0x00);  // 特殊标记：空瓦片
                    }
                    // 5. 值范围压缩：根据实际值域选择存储类型
                    else if (tileId <= ushort.MaxValue) {
                        writer.Write((byte)0x01);  // 标记：16位值
                        writer.Write((ushort)tileId);
                    } else {
                        writer.Write((byte)0x02);  // 标记：完整long
                        writer.Write(tileId);
                    }
                }
            }
        }
    }
    // 读取优化后的区块数据
    private long[,,] ReadChunkOptimized(BinaryReader reader) {
        // 读取区块尺寸

        ushort chunkXCount = reader.ReadUInt16();
        ushort chunkYCount = reader.ReadUInt16();

        // 获取层数信息（需要与导出时的层定义一致）
        Layers[] layers = (Layers[])Enum.GetValues(typeof(Layers));
        int layerCount = layers.Length;

        // 初始化三维数组 [层, 宽, 高]
        long[,,] chunk = new long[layerCount, chunkXCount, chunkYCount];

        // 逐层解码RLE数据
        for (int l = 0; l < layerCount; l++) {
            for (int y = 0; y < chunkYCount; y++) {
                for (int x = 0; x < chunkXCount; x++) {
                    // 读取RLE标记和长度
                    byte flag = reader.ReadByte();
                    long tileId = 0;
                    switch (flag) {
                        case 0x00: // 空瓦片
                            tileId = 0;
                            break;
                        case 0x01: // 16位值
                            tileId = reader.ReadUInt16();
                            break;
                        case 0x02: // 完整64位值
                            tileId = reader.ReadInt64();
                            break;
                        default:
                            throw new FormatException($"Invalid RLE flag: {flag}");
                    }
                    chunk[l, x, y] = tileId;
                }
            }
        }

        return chunk;
    }

    //写入区块数据
    private void WriteChunk(BinaryWriter writer, long[,,] chunk) {
        int chunkXCount = chunk.GetLength(1);
        int chunkYCount = chunk.GetLength(2);
        writer.Write((ushort)chunkXCount);
        writer.Write((ushort)chunkYCount);
        Layers[] layers = (Layers[])Enum.GetValues(typeof(Layers));
        int layerCount = layers.Length;
        for (int l = 0; l < layerCount; l++) {
            for (int x = 0; x < chunkXCount; x++) {
                for (int y = 0; y < chunkYCount; y++) {
                    long tileId = chunk[l, x, y];
                    const long EMPTY = 0;
                    if (tileId == EMPTY) {
                        writer.Write((byte)0x00);  // 特殊标记：空瓦片
                    }
                    // 5. 值范围压缩：根据实际值域选择存储类型
                    else if (tileId <= ushort.MaxValue) {
                        writer.Write((byte)0x01);  // 标记：16位值
                        writer.Write((ushort)tileId);
                    } else {
                        writer.Write((byte)0x02);  // 标记：完整long
                        writer.Write(tileId);
                    }
                }
            }
        }
    }
    //加载区块数据
    private long[,,] ReadChunk(BinaryReader reader) {;
        ushort chunkXCount = reader.ReadUInt16();
        ushort chunkYCount = reader.ReadUInt16();
        Layers[] layers = (Layers[])Enum.GetValues(typeof(Layers));
        int layerCount = layers.Length;
        long[,,] chunk = new long[layers.Length, chunkXCount, chunkYCount];
        for (int l = 0; l < layerCount; l++) {
            for (int x = 0; x < chunkXCount; x++) {
                for (int y = 0; y < chunkYCount; y++) {
                    byte flag = reader.ReadByte();
                    long tileId = 0;
                    switch (flag) {
                        case 0x00: // 空瓦片
                            tileId = 0;
                            break;
                        case 0x01: // 16位值
                            tileId = reader.ReadUInt16();
                            break;
                        case 0x02: // 完整64位值
                            tileId = reader.ReadInt64();
                            break;
                        default:
                            throw new FormatException($"Invalid RLE flag: {flag}");
                    }
                    chunk[l, x, y] = tileId;
                }
            }
  
        }

        return chunk;
    }

}