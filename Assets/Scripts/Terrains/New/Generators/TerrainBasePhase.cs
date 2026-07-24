using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;

/// <summary>
/// 管线 1: 生成基础地形——地形层。
/// 在群落分配之前执行，为所有群落提供统一的地形基底。
/// </summary>
public class TerrainBasePhase : IGenerator {
    public int Order => 0;

    public string Name => "TerrainBase";

    public void Generate(GenerationContext context) {
        NoiseParams p = new NoiseParams() {
            type = NoiseType.MixValueWorley,
            frequency = 0.008f,
            isCurve = true,
            heightAdd = 1000,
            heightMult = 150,
            fbmParams = new FBMParams(),
            mixParams = new MIXParams() { leftFrequency = 0.005f, rightFrequency = 0.005f, weight = 0.157f}
        };

        
        // 生成高度噪声图
        SamplerResult samplerResult = NoiseSampler.GenerateTexture(context.Width, context.Height, p, context.Seed);

        float[] curveData = samplerResult.curveData;
        context.SurfaceProfile = curveData;

        // 填充地形
        ChunkManager chunk = ChunkManager.Instance;
        for (int x = 0; x < context.Width; x++) {
            int surfaceY = (int)context.SurfaceProfile[x];

            for (int y = 0; y < context.Height; y++) {

                if (y < 800) {
                    // 岩石层
                    chunk.SetBlockId(LayerType.Foreground, x, y, 2616614646469115761);
                } else if (y < surfaceY) {
                    // 泥土层
                    chunk.SetBlockId(LayerType.Foreground, x, y, 7356931947058480037);
                } else if (y == surfaceY){
                    // 地表层
                    chunk.SetBlockId(LayerType.Foreground, x, y, 2033059213628461017);
                }
                
            }
        }


    }
}
