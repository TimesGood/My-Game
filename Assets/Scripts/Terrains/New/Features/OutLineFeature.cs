using System.Collections;
using System.Collections.Generic;
using Unity.Entities.UniversalDelegates;
using Unity.VisualScripting;
using Unity.VisualScripting.Dependencies.Sqlite;
using UnityEngine;
/// <summary>
/// 轮廓填充
/// </summary>
public class OutLineFeature : BiomeFeature {

    public TileClass fillTile;
    public ShapeParams shapeParams = new ShapeParams();
    [System.NonSerialized] private Dictionary<string, Texture2D> _cache;


    public override void Init(Vector2Int _biomeSize, int _seed, Dictionary<string, Texture2D> _noiseCache) {
        Texture2D outLine = ShapeSample.GenerateTexture(_biomeSize.x, _biomeSize.y, shapeParams, _seed);
        _noiseCache["OutLineFeature"] = outLine;
    }
    public override void Execute(BiomeContext _ctx) {
        ChunkManager chunk = ChunkManager.Instance;
        Debug.Log("轮廓地形生成");
        if (_ctx.noiseCache.TryGetValue("OutLineFeature", out Texture2D tex)) {
            Debug.Log("轮廓地形生成中");
            for (int x = 0; x <= _ctx.biomeSize.x; x++) {
                for (int y = 0; y <= _ctx.biomeSize.y; y++) {
                    if (tex.GetPixel(x, y).r > 0) {
                        Vector2Int worldPos = _ctx.LocalToWorld(x, y);
                        chunk.SetBlockId(Layers.Ground, worldPos, fillTile.blockId);
                    
                    }
                
                }
            
            }
            
        }
    }
}
