using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// 生成上下文 — 在各阶段间传递的共享数据
/// </summary>
public class GenerationContext {
    // 全局配置
    public readonly int Width;
    public readonly int Height;
    public readonly int Seed;
    public readonly System.Random RNG;

    // 地表高度
    public float[] SurfaceProfile;
    // 区块数据管理器
    public ChunkManager ChunkManager { get; }
    // 已分配的群落实例
    public List<BiomeInstance> Placements { get; } = new();


    // 跨群落共享浮点图
    private readonly Dictionary<string, float[,]> _floatMaps = new();

    // 跨群落共享元数据
    private readonly Dictionary<string, object> _metadata = new();
    public GenerationContext(int width, int height, int seed) {
        Width = width;
        Height = height;
        Seed = seed;
        RNG = new System.Random(seed);
    }

    // ── 共享浮点图 ──

    public void SetFloatMap(string key, float[,] map) => _floatMaps[key] = map;
    public float[,] GetFloatMap(string key) =>
        _floatMaps.TryGetValue(key, out var m) ? m : null;
    public bool HasFloatMap(string key) => _floatMaps.ContainsKey(key);

    // ── 元数据 ──

    public void SetMeta<T>(string key, T value) => _metadata[key] = value;
    public T GetMeta<T>(string key) =>
        _metadata.TryGetValue(key, out var v) && v is T t ? t : default;
    public bool HasMeta(string key) => _metadata.ContainsKey(key);

    // ── 分叉随机数（用于子系统需要独立随机序列的场景）──

    public System.Random ForkRng(int subSeed) =>
        new(Seed * 397 ^ subSeed);
}
