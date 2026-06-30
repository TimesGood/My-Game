using System.Collections;
using System.Collections.Generic;
using Unity.Entities.UniversalDelegates;
using Unity.VisualScripting;
using Unity.VisualScripting.Dependencies.Sqlite;
using UnityEngine;
using UnityEngine.UI;
/// <summary>
/// ÂÖÀªÌî³ä
/// </summary>
public class OutLineFeature : BiomeFeature {

    public OutLineGeneration outLine;


    public override void Init(Vector2Int _biomeSize, int _seed, Dictionary<string, Texture2D> _noiseCache) {

        Texture2D tex = ShapeSampler.GenerateTexture(_biomeSize.x, _biomeSize.y, outLine.shapeParams, _seed);
        _noiseCache["OutLineFeature"] = tex;
    }
    public override void Execute(BiomeContext _ctx) {
        ChunkManager chunk = ChunkManager.Instance;
        if (_ctx.noiseCache.TryGetValue("OutLineFeature", out Texture2D tex)) {
            for (int x = 0; x <= _ctx.biomeSize.x; x++) {
                for (int y = 0; y <= _ctx.biomeSize.y; y++) {
                    if (tex.GetPixel(x, y).r > 0) {
                        Vector2Int worldPos = _ctx.LocalToWorld(x, y);
                        chunk.SetBlockId(Layers.Ground, worldPos, outLine.tileClass.blockId);
                    
                    }
                }
            }
        }
    }
}
