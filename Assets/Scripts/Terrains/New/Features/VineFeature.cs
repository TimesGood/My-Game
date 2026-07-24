using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class VineFeature : BiomeFeature
{
    public TileClass vineTile;
    [Range(0, 100)] public int spawnChance = 30;
    public int minLength = 3;
    public int maxLength = 10;

    public override void Execute(BiomeContext _ctx)
    {
        if (vineTile == null) return;

        //for (int y = _ctx.maxHeight; y >= 0; y--)
        //{
        //    int wy = _ctx.LocalToWorldY(y);
        //    for (int x = 0; x < _ctx.biomeSize.x; x++)
        //    {
        //        int wx = _ctx.worldXs != null ? _ctx.worldXs[x] : _ctx.LocalToWorldX(x);
        //        int th = _ctx.terrainHeights != null ? _ctx.terrainHeights[x] : _ctx.biomeSize.y;
        //        if (wy > th) continue;

        //        if (world.GetTileClass(Layers.Ground, wx, wy) != null || world.GetTileClass(Layers.Ground, wx, wy + 1) == null) continue;
        //        if (Random.Range(0, 100) > spawnChance) continue;

        //        int len = Random.Range(minLength, maxLength + 1);
        //        bool ok = true;
        //        for (int i = 1; i <= len; i++) { if (world.GetTileClass(Layers.Ground, wx, wy - i) != null) { ok = false; break; } }
        //        if (ok)
        //        {
        //            world.SetTileClass(vineTile, Layers.Addons, wx, wy);
        //            GrowthHandler.Instance?.MarkForUpdate(new Vector2Int(wx, wy), len);
        //        }
        //    }
        //}
    }
}
