using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "MapGen/Gobal Biome Definition")]
public class GobalDefinition : BiomeDefinition {


    public override void Generate(BiomeContext _ctx) {

        for (int i = 0; i < features.Count; i++) {
            var f = features[i];
            if (f == null) continue;
            Debug.Log($"  ¡ú [{BiomeName}] Feature[{i}] {f.GetType().Name}");
            f.Execute(_ctx);
        }
    }
}
