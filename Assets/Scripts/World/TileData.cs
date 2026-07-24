using System;
using UnityEngine;

/// <summary>
/// 统一图块数据，一个结构体包含单个世界位置的全部图层信息，一次查询即可获取所有数据。
/// </summary>
[Serializable]
public struct TileData {
    public long groundId;       // 地面方块（石头/泥土/矿石等）
    public long wallId;         // 背景墙
    public long liquidId;       // 液体类型（0 = 无）
    public float liquidVolume;  // 液体量 0~1+
    public long addonId;        // 附加物（植物/藤蔓/树等）
    public int growthData;      // 生长阶段

    public bool HasGround => groundId != 0;
    public bool HasWall => wallId != 0;
    public bool HasLiquid => liquidId != 0 && liquidVolume > 0;
    public bool HasAddon => addonId != 0;

    // 获取指定图层的 blockId
    public long GetBlockId(LayerType layer) {
        switch (layer) {
            case LayerType.Foreground: return groundId;
            case LayerType.Background: return wallId;
            case LayerType.Liquid: return liquidId;
            case LayerType.Addons: return addonId;
            default: return 0;
        }
    }

    // 设置指定图层的 blockId
    public void SetBlockId(LayerType layer, long id) {
        switch (layer) {
            case LayerType.Foreground: groundId = id; break;
            case LayerType.Background: wallId = id; break;
            case LayerType.Liquid: liquidId = id; break;
            case LayerType.Addons: addonId = id; break;
        }
    }

    // 该位置是否没有任何图块
    public bool IsEmpty => groundId == 0 && wallId == 0 && liquidId == 0 && addonId == 0;

}
