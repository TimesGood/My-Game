using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CaveFeature : BiomeFeature
{
    public NoiseConfig caveNoise;

    public override void Init(Vector2Int _biomeSize, int _seed, Dictionary<string, Texture2D> _noiseCache)
    {
        if (caveNoise != null)
        {
            caveNoise.InitValidate(_biomeSize.x, _biomeSize.y, _seed);
            caveNoise.InitNoise();
        }
    }

    public override void Execute(BiomeContext _ctx)
    {
        if (caveNoise == null) return;

        WorldManager world = WorldManager.Instance;
        for (int y = _ctx.maxHeight; y >= 0; y--)
        {
            int wy = _ctx.LocalToWorldY(y);
            for (int x = 0; x < _ctx.biomeSize.x; x++)
            {
                int th = _ctx.terrainHeights != null ? _ctx.terrainHeights[x] : _ctx.biomeSize.y;
                int wx = _ctx.worldXs != null ? _ctx.worldXs[x] : _ctx.LocalToWorldX(x);
                if (wy > th) continue;
                if (caveNoise.noiseTexture.GetPixel(x, y).r <= 0)
                    world.SetTileClass(null, Layers.Ground, wx, wy);
            }
        }
    }
}
