// =============================================
//  WorldGenerator.cs — 地图生成总控制器
//  流程：基础地形 → 分配 → 生成（BiomeDefinition 内联 Feature）→ 后处理
// =============================================
using System;
using System.Collections.Generic;
using UnityEngine;

public class WorldGenerator : MonoBehaviour
{
    [Header("配置")]
    [SerializeField] private MapConfig _config;

    [Header("区块数据管理器")]
    [SerializeField] private ChunkManager chunkManager;

    [Header("全局群落")]
    [SerializeField] private BaseTerrainPassConfig _globalBiome;

    [Header("分配器")]
    [SerializeField] private DistributorBase[] _distributors;

    [Header("后处理器（按 Order 排序）")]
    [SerializeField] private PostProcessorBase[] _postProcessors;

    public List<BiomeInstance> BiomeInstances { get; private set; }

    [ContextMenu("Generate Map")]
    public void Generate()
    {
        int seed = _config.ResolveSeed();
        Debug.Log($"[MapGen] 开始 Seed={seed} Size={_config.Width}x{_config.Height}");

        GenerationContext context = new GenerationContext(_config.Width, _config.Height, _config.Seed);

        // Phase 1: 全局地形生成
        if (_globalBiome != null)
        {
            _globalBiome.Execute(context, new RectInt(0, 0, _config.Width, _config.Height));
            Debug.Log("[MapGen] 基础地形生成完成");
        }

        // Phase 2: 分配
        BiomeInstances = DistributeAll(context);
        Debug.Log($"[MapGen] 分配完成 共 {BiomeInstances.Count} 个群落实例");

        // Phase 3: 生成（BiomeDefinition 内联 Feature，直接调用）
        GenerateAll(context);

        // Phase 4: 后处理
        PostProcessAll(context);

        Debug.Log("[MapGen] 地图生成完成");
    }

    private List<BiomeInstance> DistributeAll(GenerationContext context)
    {
        Array.Sort(_distributors, (a, b) => a.Priority.CompareTo(b.Priority));

        List<BiomeInstance> result = new List<BiomeInstance>();
        foreach (var dist in _distributors)
        {
            result.AddRange(dist.Distribute(context));
        }
        return result;
    }

    private void GenerateAll(GenerationContext context)
    {
        foreach (var inst in BiomeInstances)
        {
            // 直接调用 BiomeDefinition.Generate()，Feature 在定义中内联
            inst.Def.Generate(context, inst);
        }
    }

    private void PostProcessAll(GenerationContext context)
    {
        // TODO: 后处理器实现
    }
}
