// =============================================
//  MapGenerator.cs — 地图生成总控制器
// =============================================
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

public class WorldGenerator : MonoBehaviour {
    // ============ Inspector 引用 ============
    [Header("配置")]
    [SerializeField] private MapConfig _config;

    [Header("区块数据管理器")]
    [SerializeField] private ChunkManager chunkManager;

    [Header("分配器")]
    [SerializeField] private DistributorBase[] _distributors;

    [Header("群落生成器 (所有 BiomeGeneratorBase)")]
    [SerializeField] private BiomeGeneratorBase[] _generators;

    [Header("后处理器 (按 Order 排序)")]
    [SerializeField] private PostProcessorBase[] _postProcessors;

    // ============ 运行时数据 ============
    public List<BiomeInstance> BiomeInstances { get; private set; }
    private BiomeGeneratorRegistry _genRegistry;

    // ============ 公开入口 ============

    /// <summary>执行完整生成流程</summary>
    [ContextMenu("Generate Map")]
    public void Generate() {
        int seed = _config.ResolveSeed();
        var rng = new System.Random(seed);
        Debug.Log($"[MapGen] 开始生成 Seed={seed} Size={_config.Width}x{_config.Height}");
        
        GenerationContext context = new GenerationContext(_config, chunkManager);

        // ── Phase 0: 初始化 ──

        _genRegistry = new BiomeGeneratorRegistry(_generators);

        // ── Phase 1: 分配 ──
        BiomeInstances = DistributeAll(context);
        Debug.Log($"[MapGen] 分配完成 共 {BiomeInstances.Count} 个群落实例");

        // ── Phase 2: 生成 ──
        GenerateAll(context);

        // ── Phase 3: 后处理 ──
        PostProcessAll(context);

        Debug.Log("[MapGen] 地图生成完成 ✓");
    }

    // ============ 内部流程 ============

    /// <summary>Phase 1: 调用所有分配器</summary>
    private List<BiomeInstance> DistributeAll(GenerationContext context) {
    

        // 按优先级排序
        Array.Sort(_distributors, (a, b) => {
            return a.Priority.CompareTo(b.Priority);
        });


        // 执行分配
        HashSet<BiomeInstance> claimed = new HashSet<BiomeInstance>();
        List<BiomeInstance> result = new List<BiomeInstance>();
        foreach (var allocator in _distributors) {
            List<BiomeInstance> biomeInstances = allocator.Distribute(context);
            result.AddRange(biomeInstances);
        }
        return result;
    }

    /// <summary>Phase 2: 逐实例调用对应的生成器</summary>
    private void GenerateAll(GenerationContext context) {
        foreach (var inst in BiomeInstances) {
            string genId = inst.Def.GeneratorId;

            if (!_genRegistry.Has(genId)) {
                Debug.LogWarning($"[MapGen] 群落 '{inst.Def.BiomeName}' " +
                    $"引用了不存在的 GeneratorId='{genId}'，跳过");
                continue;
            }

            var gen = _genRegistry.Get(genId);
            var instRng = new System.Random(inst.Seed);
            gen.Generate(context);
        }
    }

    /// <summary>Phase 3: 按 Order 依次执行后处理器</summary>
    private void PostProcessAll(GenerationContext context) {
        //var sorted = _postProcessors.OrderBy(p => p.Order);
        //foreach (var pp in sorted)
        //    pp.Process(rng);
    }
}
