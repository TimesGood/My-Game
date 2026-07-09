using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]

// 矿石填充
public class OreFeature : BiomeFeature
{
    public OreGeneration[] ores;

    public override void Execute(BiomeContext _ctx)
    {
        if (ores == null || ores.Length == 0) return;
        RectInt region = _ctx.Bounds;
        var _cache = new Dictionary<string, Texture2D>();
        for (int i = 0; i < ores.Length; i++) {
            var ore = ores[i];
            if (ore?.oreClass == null) continue;
            string key = ore.oreClass.blockId.ToString();
            if (_cache.ContainsKey(key)) continue;

            // 用 NoiseSampler 生成纹理（替代 NoiseConfig SO）
            SamplerResult tex = NoiseSampler.GenerateTexture(
                region.width, region.height, ore.noiseParams, _ctx.Instance.Seed + i * 100);
            _cache[key] = tex.tex;
        }
        Debug.Log("矿石群落生成");
        ChunkManager chunk = ChunkManager.Instance;
        // 在轮廓内布置
        
        for (int x = 0; x < region.width; x++) {
            for (int y = 0; y < region.height; y++) {
                // 只处理轮廓内
                if (_ctx.noiseCache.TryGetValue("outLine", out Texture2D outLineTex)) {
                    if (outLineTex.GetPixel(x, y).r < 1) continue;
                }
                foreach (var ore in ores) {
                    _cache.TryGetValue(ore.oreClass.blockId.ToString(), out Texture2D tex);
                    if (tex.GetPixel(x, y).r > (ore.noiseParams.isBinary ? 0 : ore.noiseParams.threshold)) {
                        Vector2Int worldPos = _ctx.LocalToWorld(x, y);
                        chunk.SetBlockId(Layers.Ground, worldPos, ore.oreClass.blockId);

                    }
                }
            }
        }

    }
}
