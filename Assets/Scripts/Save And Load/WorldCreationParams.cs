using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 创建世界的参数
/// </summary>

public class WorldCreationParams
{
    public int seed;                    // 0 = 随机
    public WorldSizePreset sizePreset = WorldSizePreset.Medium;

    // 种群/生物群落开关（可扩展）
    public bool enableCaves = true;
    public bool enableOres = true;

    /// <summary>根据预设获取实际尺寸</summary>
    public (int width, int height) GetWorldSize() {
        return sizePreset switch {
            WorldSizePreset.Small => (3000, 1000),
            WorldSizePreset.Medium => (6000, 2000),
            WorldSizePreset.Large => (9000, 3000),
            _ => (6000, 2000)
        };
    }

    public int GetSeed() {
        return seed == 0 ? Random.Range(int.MinValue, int.MaxValue) : seed;
    }
}
public enum WorldSizePreset {
    Small,   // 3000×1000
    Medium,  // 6000×2000
    Large    // 9000×3000
}