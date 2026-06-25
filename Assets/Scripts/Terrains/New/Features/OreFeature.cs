using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class OreFeature : BiomeFeature
{
    public OreGeneration[] ores;
    [System.NonSerialized] private Dictionary<string, Texture2D> _cache;

    public override void Init(Vector2Int _biomeSize, int _seed, Dictionary<string, Texture2D> _noiseCache)
    {
        _cache = new Dictionary<string, Texture2D>();
        if (ores == null) return;
        for (int i = 0; i < ores.Length; i++)
        {
            var ore = ores[i];
            if (ore?.oreClass == null) continue;
            string key = ore.oreClass.blockId.ToString();
            if (_cache.ContainsKey(key)) continue;

            // 用 NoiseSampler 生成纹理（替代 NoiseConfig SO）
            Texture2D tex = NoiseSampler.GenerateTexture(
                _biomeSize.x, _biomeSize.y, ore.noiseParams, _seed + i * 100);
            _cache[key] = tex;
        }
    }

    public override void Execute(BiomeContext _ctx)
    {
        if (ores == null || ores.Length == 0 || _cache == null) return;

        WorldManager world = WorldManager.Instance;
        for (int y = _ctx.maxHeight; y >= 0; y--)
        {
            int wy = _ctx.LocalToWorldY(y);
            for (int x = 0; x < _ctx.biomeSize.x; x++)
            {
                int th = _ctx.terrainHeights != null ? _ctx.terrainHeights[x] : _ctx.biomeSize.y;
                int wx = _ctx.worldXs != null ? _ctx.worldXs[x] : _ctx.LocalToWorldX(x);
                if (wy > th) continue;

                foreach (var ore in ores)
                {
                    if (ore?.oreClass == null) continue;
                    if (_cache.TryGetValue(ore.oreClass.blockId.ToString(), out var tex) && tex.GetPixel(x, y).r > ore.threshold)
                    {
                        world.SetTileClass(ore.oreClass, Layers.Ground, wx, wy);
                        break;
                    }
                }
            }
        }
    }
}
