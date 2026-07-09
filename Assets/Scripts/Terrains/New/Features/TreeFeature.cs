using System.Collections.Generic;
using UnityEngine;

public enum TreePlacement { Surface, CaveCeiling, Both }

/// <summary>
/// 植物散布
/// </summary>
[System.Serializable]
public class TreeFeature : BiomeFeature
{
    public TreePlacement placement = TreePlacement.Surface;
    public TreeGeneration[] trees;
    [Range(0, 100)] public int spawnChance = 50;
    [System.NonSerialized] private Dictionary<string, SamplerResult> _cache;

    public override void Execute(BiomeContext _ctx)
    {
        //if (trees == null || trees.Length == 0 || _cache == null) return;
        //if (placement == TreePlacement.Surface || placement == TreePlacement.Both) PlaceSurface(_ctx);
        //if (placement == TreePlacement.CaveCeiling || placement == TreePlacement.Both) PlaceCave(_ctx);
    }

    private void PlaceSurface(BiomeContext _ctx)
    {
        //WorldManager world = WorldManager.Instance;
        //for (int x = 0; x < _ctx.biomeSize.x; x++)
        //{
        //    int wx = _ctx.worldXs != null ? _ctx.worldXs[x] : _ctx.LocalToWorldX(x);
        //    int th = _ctx.terrainHeights != null ? _ctx.terrainHeights[x] : world.surfaceHeights[wx];
        //    if (!_ctx.IsSurfaceRange(wx)) continue;
        //    TileClass tb = world.GetTileClass(Layers.Ground, wx, th);
        //    if (tb == null) continue;
        //    int ty = th + 1;
        //    foreach (var tg in trees)
        //    {
        //        if (tg?.treeClass == null || !tg.treeClass.CheckSpawn(wx, ty)) continue;
        //        if (_cache.TryGetValue(tg.treeClass.blockId.ToString(), out var tex) && tex.tex.GetPixel(x, ty).r > 0.5f && Random.Range(0, 100) < spawnChance)
        //        { tg.treeClass.PlanceSelf(wx, ty); break; }
        //    }
        //}
    }

    private void PlaceCave(BiomeContext _ctx)
    {
        WorldManager world = WorldManager.Instance;
        //for (int y = _ctx.maxHeight; y >= 0; y--)
        //{
        //    int wy = _ctx.LocalToWorldY(y);
        //    for (int x = 0; x < _ctx.biomeSize.x; x++)
        //    {
        //        int wx = _ctx.worldXs != null ? _ctx.worldXs[x] : _ctx.LocalToWorldX(x);
        //        int th = _ctx.terrainHeights != null ? _ctx.terrainHeights[x] : _ctx.biomeSize.y;
        //        if (wy > th) continue;
        //        if (world.GetTileClass(Layers.Ground, wx, wy) != null || world.GetTileClass(Layers.Ground, wx, wy + 1) == null) continue;
        //        if (Random.Range(0, 100) > 60) continue;
        //        int idx = Random.Range(0, trees.Length);
        //        var tg = trees[idx];
        //        if (tg?.treeClass != null && tg.treeClass.CheckSpawn(wx, wy))
        //            tg.treeClass.PlanceSelf(wx, wy);
        //    }
        //}
    }
}
