using System.Collections.Generic;
using UnityEngine;

[System.Serializable]

// ¶´Ñ¨ÍÚ¾ò
public class CaveFeature : BiomeFeature
{
    public NoiseParams saveParams;

    public override void Execute(BiomeContext _ctx)
    {
        RectInt region = _ctx.Bounds;
        if (saveParams == null) return;
        SamplerResult sampleResult = NoiseSampler.GenerateTexture(region.width, region.height, saveParams, _ctx.Instance.Seed);

        ChunkManager chunk = ChunkManager.Instance;
        for (int y = region.height; y >= 0; y--)
        {
            for (int x = 0; x < region.width; x++)
            {
                if (sampleResult.tex.GetPixel(x, y).r > saveParams.threshold) {
                    Vector2Int worldPos = _ctx.LocalToWorld(x, y);
                    chunk.SetBlockId(Layers.Ground, worldPos, 0);
                }
                    
            }
        }
    }
}
