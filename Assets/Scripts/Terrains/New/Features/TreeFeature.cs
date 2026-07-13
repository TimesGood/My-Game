using System.Collections.Generic;
using UnityEngine;

public enum TreePlacement { Surface, CaveCeiling, Both }

/// <summary>
/// 植物放置
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
        if (trees == null || trees.Length == 0) return;
        // 噪图生成
        _cache = new Dictionary<string, SamplerResult>();
        foreach (var tg in trees) {
            SamplerResult result = NoiseSampler.GenerateTexture(_ctx.biomeSize.x, _ctx.biomeSize.y, tg.noiseParams, _ctx.Seed);
            _cache.Add(tg.treeClass.blockId.ToString(), result);
        }
        if (placement == TreePlacement.Surface || placement == TreePlacement.Both) PlaceSurface(_ctx);
        if (placement == TreePlacement.CaveCeiling || placement == TreePlacement.Both) PlaceCave(_ctx);
    }

    // 放置地表植株
    private void PlaceSurface(BiomeContext _ctx)
    {


        ChunkManager chunk = ChunkManager.Instance;

        float[] surface = _ctx.genContext.SurfaceProfile;
        
        for (int x = 0; x < _ctx.biomeSize.x; x++) {
            var wx = _ctx.LocalToWorldX(x);
            int th = (int)surface[wx];
            TileClass tileClass = chunk.GetTileClass(Layers.Ground, wx, th);
            if (tileClass == null) continue;
            int ty = th + 1;
            foreach (var tg in trees) {
                if (tg?.treeClass == null || !tg.treeClass.CheckSpawn(wx, ty)) continue;
                if (_cache.TryGetValue(tg.treeClass.blockId.ToString(), out var tex)) {
                    float noiseValue = tex.tex.GetPixel(x, 0).r;

                    if (noiseValue > tg.noiseParams.threshold) {
                        
                        // 重新返回到0到1的范围
                        float v = (noiseValue - tg.noiseParams.threshold) / (1f - tg.noiseParams.threshold);
                        // 越接近1生成概率越高
                        if (Random.value < v) {
                            tg.treeClass.PlanceSelf(wx, ty);
                            break;
                        }
                    }
                }
            }
        }
    }

    // 放置洞穴植株
    private void PlaceCave(BiomeContext _ctx)
    {
        ChunkManager chunk = ChunkManager.Instance;
        for (int x = 0; x < _ctx.biomeSize.x; x++) {
            for (int y = 0; y < _ctx.biomeSize.y; y++) {
                Vector2Int worldPos = _ctx.LocalToWorld(x, y);
                TileClass tile = chunk.GetTileClass(Layers.Ground, worldPos);
                if (tile != null) continue;

                TileClass downTile = chunk.GetTileClass(Layers.Ground, worldPos - Vector2Int.down);
                if (downTile != null) {
                    foreach (var tg in trees) {
                        if (tg?.treeClass == null || !tg.treeClass.CheckSpawn(worldPos.x, worldPos.y)) continue;
                        if (_cache.TryGetValue(tg.treeClass.blockId.ToString(), out var tex)) {
                            float noiseValue = tex.tex.GetPixel(x, y).r;

                            if (noiseValue > tg.noiseParams.threshold) {

                                // 重新返回到0到1的范围
                                float v = (noiseValue - tg.noiseParams.threshold) / (1f - tg.noiseParams.threshold);
                                // 越接近1生成概率越高
                                if (Random.value < v) {
                                    tg.treeClass.PlanceSelf(worldPos.x, worldPos.y);
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
