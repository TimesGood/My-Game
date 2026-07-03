using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 结构体 Feature —— 在群落中放置预制建筑/特殊结构（当前为占位实现）。
/// </summary>
[System.Serializable]
public class StructureFeature : BiomeFeature
{
    public TileClass structureTile;
    public int count = 3;
    public int randomOffset = 10;

    public override void Init(GenerationContext _ctx, RectInt region) { }

    public override void Execute(GenerationContext _ctx, RectInt region)
    {
        if (structureTile == null) return;

        WorldManager world = WorldManager.Instance;
        for (int i = 0; i < count; i++)
        {
            int lx = Random.Range(0, region.width);
            int ly = Random.Range(0, region.height);
            int wx = region.x + lx;
            int wy = region.y + ly;
            world.SetTileClass(structureTile, structureTile.layer, wx, wy);
        }
    }
}
