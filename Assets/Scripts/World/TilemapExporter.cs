
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


//瓦片地图导出器
public class TilemapExporter : Singleton<TilemapExporter> {
    public int chunkSize = 16;
    public string exportFileName = "tilemap_data.bin";

    /// <summary>自定义存档目录路径（为空时使用 streamingAssetsPath）</summary>
    private string customSavePath;

    /// <summary>
    /// 设置自定义存档目录路径
    /// </summary>
    public void SetCustomSavePath(string _path)
    {
        customSavePath = _path;
    }

    /// <summary>
    /// 获取当前存档文件完整路径
    /// </summary>
    private string GetSaveFilePath()
    {
        if (!string.IsNullOrEmpty(customSavePath))
        {
            return Path.Combine(customSavePath, exportFileName);
        }
        return Path.Combine(Application.streamingAssetsPath, exportFileName);
    }

    public IEnumerator ExportAllTilemaps(MapData mapData, Action<float> progressCallback) {
        string path = GetSaveFilePath();
        long[,][,,] chunkData = mapData.chunkDatas;
        int chunkXCount = chunkData.GetLength(0);
        int chunkYCount = chunkData.GetLength(1);
        int chunkTotal = chunkXCount * chunkYCount;

        // 使用压缩流包裹文件流
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
                    long[,,] chunk = chunkData[cx, cy];
                    WriteChunkOptimized(writer, chunk);

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

    public IEnumerator LoadAllTilemaps(Action<MapData> onComplete, Action<float> progressCallback) {
        string path = GetSaveFilePath();
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
            MapData result = new MapData();
            result.chunkDatas = mapData;
            onComplete?.Invoke(result);
        }

    }

    // 写入区块数据
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
    // 读取区块数据
    private long[,,] ReadChunkOptimized(BinaryReader reader) {
        // 读取区块尺寸

        ushort chunkXCount = reader.ReadUInt16();
        ushort chunkYCount = reader.ReadUInt16();

        // 获取层数信息（需要与导出时的层定义一致）
        LayerType[] layers = (LayerType[])Enum.GetValues(typeof(LayerType));
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


    public bool isExists() {
        string path = GetSaveFilePath();
        if (!File.Exists(path)) {
            return false;
        }
        return true;
    }

}