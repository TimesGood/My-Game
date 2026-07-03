using System.Collections.Generic;
using UnityEngine;

[System.Serializable]

// 洞穴挖掘
public class CaveFeature : BiomeFeature
{
    public NoiseParams caveNoise;

    // ========== 运行时纹理（Execute 期间临时使用） ==========
    [System.NonSerialized] private SamplerResult _caveTex;

    public override void Init(GenerationContext _ctx, RectInt region)
    {
        
    }

    public override void Execute(GenerationContext _ctx, RectInt region)
    {
        _caveTex = NoiseSampler.GenerateTexture(region.width, region.height, caveNoise, _ctx.Seed);
        if (caveNoise == null) return;

        WorldManager world = WorldManager.Instance;
        for (int y = region.height; y >= 0; y--)
        {
            for (int x = 0; x < region.width; x++)
            {
                int wx = region.x + x;
                int wy = region.y + y;
                if (_caveTex.tex.GetPixel(x, y).r <= 0)
                    world.SetTileClass(null, Layers.Ground, wx, wy);
            }
        }
    }
}
