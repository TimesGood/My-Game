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


    public override void Init(GenerationContext _ctx, RectInt region) {

    }
    public override void Execute(GenerationContext _ctx, RectInt region) {
        Texture2D tex = ShapeSampler.GenerateTexture(region.width, region.height, outLine.shapeParams, _ctx.Seed);
        ChunkManager chunk = ChunkManager.Instance;
        for (int x = 0; x < region.width; x++) {
            for (int y = 0; y <= region.height; y++) {
                if (tex.GetPixel(x, y).r > 0) {
                    int wx = region.x + x;
                    int wy = region.y + y;
                    chunk.SetBlockId(Layers.Ground, new Vector2Int(wx, wy), outLine.tileClass.blockId);
                
                }
            }
        }
        
    }
}
