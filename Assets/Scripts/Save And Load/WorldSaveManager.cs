using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using UnityEngine;

/// <summary>
/// 世界列表管理器 - 管理所有世界的存档
/// </summary>
public static class WorldSaveManager
{
    private static readonly string WorldsRoot =
        Path.Combine(Application.persistentDataPath, "worlds");

    private const string MetaFileName = "meta.json";

    /// <summary>
    /// 获取所有世界元数据（按最后游玩时间倒序排列）
    /// </summary>
    public static List<WorldMeta> LoadWorldList()
    {
        var worlds = new List<WorldMeta>();

        if (!Directory.Exists(WorldsRoot))
        {
            Directory.CreateDirectory(WorldsRoot);
            return worlds;
        }

        string[] worldDirs = Directory.GetDirectories(WorldsRoot);
        foreach (string dir in worldDirs)
        {
            string metaPath = Path.Combine(dir, MetaFileName);
            if (File.Exists(metaPath))
            {
                try
                {
                    string json = File.ReadAllText(metaPath);
                    WorldMeta meta = JsonUtility.FromJson<WorldMeta>(json);
                    worlds.Add(meta);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"读取世界元数据失败: {dir}, 错误: {e.Message}");
                }
            }
        }

        // 按最后游玩时间倒序排列
        worlds.Sort((a, b) => b.lastPlayTime.CompareTo(a.lastPlayTime));
        return worlds;
    }

    /// <summary>
    /// 获取世界元数据
    /// </summary>
    public static WorldMeta GetWorld(string _worldId) {
        return null;
    }

    /// <summary>
    /// 创建新世界文件夹和元数据
    /// </summary>
    public static WorldMeta CreateWorld(string _name, int _seed, int _width, int _height)
    {
        WorldMeta meta = WorldMeta.Create(_name, _seed, _width, _height);

        string worldPath = GetWorldPath(meta.worldId);
        Directory.CreateDirectory(worldPath);

        SaveMeta(meta);
        return meta;
    }

    /// <summary>
    /// 删除世界及其所有数据
    /// </summary>
    public static void DeleteWorld(string _worldId)
    {
        string worldPath = GetWorldPath(_worldId);
        if (Directory.Exists(worldPath))
        {
            Directory.Delete(worldPath, true);
            Debug.Log($"已删除世界: {_worldId}");
        }
    }

    /// <summary>
    /// 获取世界存档文件夹路径
    /// </summary>
    public static string GetWorldPath(string _worldId)
    {
        return Path.Combine(WorldsRoot, _worldId);
    }

    /// <summary>
    /// 更新最后游玩时间
    /// </summary>
    public static void UpdateLastPlayed(string _worldId)
    {
        string metaPath = Path.Combine(GetWorldPath(_worldId), MetaFileName);
        if (File.Exists(metaPath))
        {
            string json = File.ReadAllText(metaPath);
            WorldMeta meta = JsonUtility.FromJson<WorldMeta>(json);
            meta.lastPlayTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            SaveMeta(meta);
        }
    }

    /// <summary>
    /// 保存元数据到文件
    /// </summary>
    public static void SaveMeta(WorldMeta _meta) {
        string worldPath = GetWorldPath(_meta.worldId);
        string metaPath = Path.Combine(worldPath, MetaFileName);
        string json = JsonUtility.ToJson(_meta, true);
        File.WriteAllText(metaPath, json);
    }


    // =================================================================
    //  区块数据读写
    // =================================================================

    public static void SaveChunk(string _worldId, List<Chunk> chunks) {
        foreach (var chunkd in chunks) {
            SaveChunk(_worldId, chunks);
        }
    }
    public static List<Chunk> LoadChunks(string _worldId, List<Vector2Int> chunkCoords) {
        List<Chunk> chunks = new List<Chunk>();
        foreach (var chunkCoord in chunkCoords) {
            chunks.Add(LoadChunk(_worldId, chunkCoord));
        }
        return chunks;
    }

    /// <summary>
    /// 保存区块数据
    /// </summary>
    /// <param name="_worldId"></param>
    /// <param name="_chunk"></param>
    public static void SaveChunk(string _worldId, Chunk _chunk) {
        string worldPath = GetWorldPath(_worldId);
        string chunkFile = Path.Combine(worldPath,
                $"chunk_{_chunk.coord.x}_{_chunk.coord.y}.bin");

        int w = _chunk.Width;
        int h = _chunk.Height;

        // TileData → byte[]
        // TileData 大小: sizeof(long)*4 + sizeof(float) + sizeof(int) = 40 bytes
        int tileSize = 40;
        byte[] raw = new byte[w * h * tileSize];

        // 逐行 BlockCopy (TileData[,] 是行主序，每行 = w 个 TileData)
        for (int y = 0; y < h; y++) {
            // 从二维数组的 [0,y] 行拷贝 w 个 TileData
            // 注意: C# 二维数组在内存中是连续的 [x + y*width]
            // 但 TileData[,] 是 [x,y]，内存布局是 row-major
            // 需要逐元素拷贝
            for (int x = 0; x < w; x++) {
                int offset = (y * w + x) * tileSize;
                TileData td = _chunk.tiles[x, y];

                // 手动写入 (避免 BlockCopy 对 struct 数组的限制)
                WriteLong(raw, offset + 0, td.groundId);
                WriteLong(raw, offset + 8, td.wallId);
                WriteLong(raw, offset + 16, td.liquidId);
                WriteFloat(raw, offset + 24, td.liquidVolume);
                WriteLong(raw, offset + 28, td.addonId);
                WriteInt(raw, offset + 36, td.growthData);
            }
        }

        // gzip 压缩
        byte[] compressed = GzipCompress(raw);

        // 写入文件: [原始大小(4)] [压缩数据]
        using var fs = File.Create(chunkFile);
        using var bw = new BinaryWriter(fs);
        bw.Write(_chunk.Width);
        bw.Write(_chunk.Height);
        bw.Write(raw.Length);
        bw.Write(compressed.Length);
        bw.Write(compressed);

    }

    /// <summary>
    /// 加载区块数据
    /// </summary>
    /// <param name="_worldId"></param>
    /// <param name="chunkCoord"></param>
    /// <returns></returns>
    public static Chunk LoadChunk(string _worldId, Vector2Int chunkCoord) {

        string worldPath = GetWorldPath(_worldId);

        string chunkFile = Path.Combine(worldPath,
                        $"chunk_{chunkCoord.x}_{chunkCoord.y}.bin");

        if (!File.Exists(chunkFile)) return null;

        using var fs = File.OpenRead(chunkFile);
        using var br = new BinaryReader(fs);

        int width = br.ReadInt32();
        int height = br.ReadInt32();
        int rawLength = br.ReadInt32();
        int compressedLength = br.ReadInt32();
        byte[] compressed = br.ReadBytes(compressedLength);
        byte[] raw = GzipDecompress(compressed, rawLength);

        Chunk chunk = new Chunk(chunkCoord, width, height, chunkCoord.x * width, chunkCoord.y * height);

        int w = chunk.Width;
        int h = chunk.Height;
        int tileSize = 40;

        for (int y = 0; y < h; y++) {
            for (int x = 0; x < w; x++) {
                int offset = (y * w + x) * tileSize;
                if (offset + tileSize > raw.Length) break;

                TileData td = new TileData();
                td.groundId = ReadLong(raw, offset + 0);
                td.wallId = ReadLong(raw, offset + 8);
                td.liquidId = ReadLong(raw, offset + 16);
                td.liquidVolume = ReadFloat(raw, offset + 24);
                td.addonId = ReadLong(raw, offset + 28);
                td.growthData = ReadInt(raw, offset + 36);

                chunk.tiles[x, y] = td;
            }
        }

        chunk.isDirty = true;
        return chunk;

    }
    // =================================================================
    //  压缩工具
    // =================================================================

    private static byte[] GzipCompress(byte[] data) {
        using var ms = new MemoryStream();
        using (var gzip = new GZipStream(ms, System.IO.Compression.CompressionLevel.Fastest))
            gzip.Write(data, 0, data.Length);
        return ms.ToArray();
    }

    private static byte[] GzipDecompress(byte[] compressed, int expectedSize) {
        using var ms = new MemoryStream(compressed);
        using var gzip = new GZipStream(ms, CompressionMode.Decompress);
        var result = new byte[expectedSize];
        int read = 0;
        while (read < expectedSize) {
            int n = gzip.Read(result, read, expectedSize - read);
            if (n == 0) break;
            read += n;
        }
        return result;
    }

    // =================================================================
    //  二进制读写小端
    // =================================================================

    private static void WriteLong(byte[] buf, int offset, long v) {
        buf[offset + 0] = (byte)v;
        buf[offset + 1] = (byte)(v >> 8);
        buf[offset + 2] = (byte)(v >> 16);
        buf[offset + 3] = (byte)(v >> 24);
        buf[offset + 4] = (byte)(v >> 32);
        buf[offset + 5] = (byte)(v >> 40);
        buf[offset + 6] = (byte)(v >> 48);
        buf[offset + 7] = (byte)(v >> 56);
    }

    private static long ReadLong(byte[] buf, int offset) {
        return (long)buf[offset]
             | ((long)buf[offset + 1] << 8)
             | ((long)buf[offset + 2] << 16)
             | ((long)buf[offset + 3] << 24)
             | ((long)buf[offset + 4] << 32)
             | ((long)buf[offset + 5] << 40)
             | ((long)buf[offset + 6] << 48)
             | ((long)buf[offset + 7] << 56);
    }


    private static void WriteFloat(byte[] buf, int offset, float v) {
        byte[] b = BitConverter.GetBytes(v);
        Buffer.BlockCopy(b, 0, buf, offset, 4);
    }

    private static float ReadFloat(byte[] buf, int offset) {
        return BitConverter.ToSingle(buf, offset);
    }

    private static void WriteInt(byte[] buf, int offset, int v) {
        buf[offset + 0] = (byte)v;
        buf[offset + 1] = (byte)(v >> 8);
        buf[offset + 2] = (byte)(v >> 16);
        buf[offset + 3] = (byte)(v >> 24);
    }

    private static int ReadInt(byte[] buf, int offset) {
        return buf[offset]
             | (buf[offset + 1] << 8)
             | (buf[offset + 2] << 16)
             | (buf[offset + 3] << 24);
    }
}
