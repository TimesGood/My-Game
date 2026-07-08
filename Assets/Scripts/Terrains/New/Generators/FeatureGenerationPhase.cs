using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class FeatureGenerationPhase : IGenerator {
    public int Order => 0;

    public string Name => "FeatureGenerationPhase";

    public void Generate(GenerationContext context) {
        foreach (var placement in context.Placements) {
            var def = placement.Def;
            Debug.Log($"[FeatureGen] 处理群落 '{def.name}', " +
                      $"Feature 数量: {def.features.Count}");

            var ctx = new BiomeContext(context, placement);

            // 按列表顺序依次执行 Feature
            foreach (var feature in def.features) {
                if (feature == null) continue;
                Debug.Log($"[FeatureGen]   执行: xxx");
                feature.Execute(ctx);
            }
        }
    }
}
