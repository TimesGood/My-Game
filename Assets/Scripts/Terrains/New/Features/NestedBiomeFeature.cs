using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 嵌套群落 Feature —— 在父群落内再次分布子群落。
/// 注意：子群落的 Feature 内联在其 BiomeDefinition 中，直接调用 Generate 即可。
/// </summary>
[System.Serializable]
public class NestedBiomeFeature : BiomeFeature
{
    public BiomeDefinition[] childBiomes;
    public float sampleRadius = 30f;
    public int maxCount = 10;

    public override void Init(Vector2Int _biomeSize, int _seed, Dictionary<string, Texture2D> _noiseCache) { }

    public override void Execute(BiomeContext _ctx)
    {
        if (childBiomes == null || childBiomes.Length == 0) return;

        WorldManager world = WorldManager.Instance;
        Vector2 regionSize = new Vector2(_ctx.biomeSize.x, _ctx.biomeSize.y);

        Vector2 parentLocalMin = new Vector2(0, 0);
        Vector2 parentLocalMax = new Vector2(_ctx.biomeSize.x, _ctx.biomeSize.y);
        List<Vector2> points = PoissonDiscSampling.GeneratePoints(sampleRadius, parentLocalMin, parentLocalMax, null, 30);

        int placed = 0;
        foreach (var pt in points)
        {
            if (placed >= maxCount) break;

            int wx = _ctx.LocalToWorldX((int)pt.x);
            int wy = _ctx.LocalToWorldY((int)pt.y);

            int idx = Random.Range(0, childBiomes.Length);
            BiomeDefinition childDef = childBiomes[idx];

            // 创建子群落实例
            int sx = wx - childDef.biomeSize.x / 2;
            int sy = wy - childDef.biomeSize.y / 2;
            RectInt bounds = new RectInt(sx, sy, childDef.biomeSize.x, childDef.biomeSize.y);

            BiomeInstance childInst = new BiomeInstance
            {
                Def = childDef,
                Bounds = bounds,
                Seed = world.seed + placed * 100
            };

            // 通过 WorldManager 找到 WorldGenerator 来调用生成器
            // 此处需要 WorldGenerator 的引用——可通过 GenerationContext 传递
            // 当前为简化实现，标记待完善
            Debug.Log($"[NestedBiome] 子群落 {childDef.BiomeName} 位置=({sx},{sy})");

            placed++;
        }
    }
}
