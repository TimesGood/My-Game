using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 生成上下文 - 在管线各阶段间传递的共享数据
/// </summary>
public class GenerationContext {

    // 基础配置
    public MapConfig Config { get; }
    // 区块数据管理器
    public ChunkManager ChunkManager { get; }
    // 已进行分配的群落
    public HashSet<BiomeInstance> claimed { get; private set; }
    // 地表高度
    public int[] SurfaceHeightMap { get; private set; }

    public GenerationContext(MapConfig config, ChunkManager chunkManager) {
        Config = config;
        ChunkManager = chunkManager;
        claimed = new HashSet<BiomeInstance>();
    }

}
