using System.Collections.Generic;
using UnityEngine;

[System.Serializable]

// 洞穴挖掘
public class CaveFeature : BiomeFeature
{
    public NoiseParams caveNoise;

    // ========== 运行时纹理（Execute 期间临时使用） ==========
    [System.NonSerialized] private SamplerResult _caveTex;

    public override void Init(Vector2Int _biomeSize, int _seed, Dictionary<string, Texture2D> _noiseCache)
    {
        _caveTex = NoiseSampler.GenerateTexture(_biomeSize.x, _biomeSize.y, caveNoise, _seed);
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
                if (_caveTex.tex.GetPixel(x, y).r <= 0)
                    world.SetTileClass(null, Layers.Ground, wx, wy);
            }
        }
    }
}
