using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static BiomeDefinition;

[CreateAssetMenu(menuName = "MapGen/Local Biome Definition")]
public class LocalDefinition : BiomeDefinition
{
    [Header("尺寸大小")]
    public Vector2Int biomeSize = new(300, 100);

    [Header("适合度范围")]
    public Suitable[] suitable;

    [Header("是否允许与其他群落重叠")]
    public bool AllowOverlap = false;

    [Header("生成数量")]
    public int num = 1;

    //[Header("群落轮廓（可选，不规则形状）")]
    //public ShapeGenerator outLine;

    // -------- 生成 --------

    /// <summary>
    /// 对指定群落实例执行生成
    /// </summary>
    public override void Generate(BiomeContext _ctx) {

        if (features.Count == 0) {
            Debug.LogWarning($"[BiomeDefinition] '{BiomeName}' 没有配置任何 Feature，跳过生成");
            return;
        }

        for (int i = 0; i < features.Count; i++) {
            var f = features[i];
            if (f == null) continue;
            Debug.Log($"  → [{BiomeName}] Feature[{i}] {f.GetType().Name}");

            f.Execute(_ctx);
        }
    }

    [Serializable]
    public class Suitable {
        public Vector2Int SuitableMin = new(0, 0);
        public Vector2Int SuitableMax = new(0, 0);
    }
}
