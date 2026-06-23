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

    public override void Init(Vector2Int _biomeSize, int _seed, Dictionary<string, Texture2D> _noiseCache) { }

    public override void Execute(BiomeContext _ctx)
    {
        if (structureTile == null) return;

        WorldManager world = WorldManager.Instance;
        for (int i = 0; i < count; i++)
        {
            int lx = Random.Range(0, _ctx.biomeSize.x);
            int ly = Random.Range(0, _ctx.biomeSize.y);
            int wx = _ctx.LocalToWorldX(lx) + Random.Range(-randomOffset, randomOffset + 1);
            int wy = _ctx.LocalToWorldY(ly) + Random.Range(-randomOffset, randomOffset + 1);
            world.SetTileClass(structureTile, structureTile.layer, wx, wy);
        }
    }
}
