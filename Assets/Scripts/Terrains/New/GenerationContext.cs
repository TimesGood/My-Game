using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 生成上下文 — 在各阶段间传递的共享数据
/// </summary>
public class GenerationContext
{
    // 全局配置
    public MapConfig Config { get; }
    // 区块数据管理器
    public ChunkManager ChunkManager { get; }
    // 已分配的群落实例
    public HashSet<BiomeInstance> claimed { get; private set; }

    /// <summary>生成使用的种子</summary>
    public int Seed { get; }

    // ========== 基础地形数据（由 BaseTerrainPassConfig 填充） ==========

    /// <summary>每列地表高度 [0..Width)</summary>
    public int[] SurfaceHeightMap { get; set; }

    /// <summary>每列石头层边界高度 [0..Width)</summary>
    public float[] StoneHeightMap { get; set; }

    /// <summary>每列地表曲线原始偏移 [0..Width)</summary>
    public float[] TerrainCurveData { get; set; }

    public GenerationContext(MapConfig _config, ChunkManager _chunkManager)
    {
        Config = _config;
        ChunkManager = _chunkManager;
        Seed = _config.ResolveSeed();
        claimed = new HashSet<BiomeInstance>();
    }
}
