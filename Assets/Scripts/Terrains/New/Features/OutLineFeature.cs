using System.Collections;
using System.Collections.Generic;
using Unity.Entities.UniversalDelegates;
using Unity.VisualScripting;
using Unity.VisualScripting.Dependencies.Sqlite;
using UnityEngine;
using UnityEngine.UI;
/// <summary>
/// 轮廓填充
/// </summary>
public class OutLineFeature : BiomeFeature {

    public OutLineGeneration outLine;


    public override void Init(BiomeContext _ctx) {

    }
    public override void Execute(BiomeContext _ctx) {
        RectInt region = _ctx.Bounds;
        Debug.Log("OutLineFeature执行生成" + region.width +":" + region.height);
        Texture2D tex = ShapeSampler.GenerateTexture(region.width, region.height, outLine.shapeParams, _ctx.Instance.Seed);
        _ctx.noiseCache.Add("outLine", tex);// 给其他Feature使用
        ChunkManager chunk = ChunkManager.Instance;
        for (int x = 0; x < region.width; x++) {
            for (int y = 0; y < region.height; y++) {
                if (tex.GetPixel(x, y).r > 0) {
                    int wx = region.x + x;
                    int wy = region.y + y;
                    chunk.SetBlockId(Layers.Ground, new Vector2Int(wx, wy), outLine.tileClass.blockId);
                
                }
            }
        }
        
    }
}
