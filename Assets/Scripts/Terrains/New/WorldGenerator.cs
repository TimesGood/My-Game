// =============================================
//  WorldGenerator.cs — 地图生成总控制器
//  流程：基础地形 → 分配 → 生成（BiomeDefinition 内联 Feature）→ 后处理
// =============================================
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class WorldGenerator : Singleton<WorldGenerator> {
    [Header("配置")]
    [SerializeField] private MapConfig _config;

    [Header("区块数据管理器")]
    [SerializeField] private ChunkManager chunkManager;

    [Header("全局群落")]
    [SerializeField] private List<GobalDefinition> _globalBiome;

    [Header("局部群落")]
    [SerializeField] private List<LocalDefinition> _localBiome;

    [Header("分配器")]
    [SerializeField] private DistributorBase[] _distributors;

    [Header("后处理器（按 Order 排序）")]
    [SerializeField] private PostProcessorBase[] _postProcessors;

    public List<BiomeInstance> BiomeInstances { get; private set; }

    public void Init(ChunkManager chunkManager) {
        this.chunkManager = chunkManager;
    }


    [ContextMenu("Generate Map")]
    public void Generate()
    {
        var genParams = new WorldCreationParams();
        StartCoroutine(GenerateWorld(genParams, (p, s) => {
            Debug.Log("");
        }));
    }

    public IEnumerator GenerateWorld(WorldCreationParams genParams,
                                     System.Action<float, string> onProgress) {

        GenerationContext context = new GenerationContext(chunkManager.Width, chunkManager.Height, _config.Seed, chunkManager);


        var pipeline = new GenerationPipeline();

        // Phase 1: 地形基底
        pipeline.AddPhase(new TerrainBasePhase());

        // Phase 2: 全局群落
        pipeline.AddPhase(new GlobalBiomePhase(_globalBiome));

        // Phase 3: 局部群落分配
        pipeline.AddPhase(new BiomeAllocationPhase(_localBiome));

        // Phase 4: Feature生成
        pipeline.AddPhase(new FeatureGenerationPhase());

        // Phase 5: 后处理

        // ---- 6. 执行 Pipeline ----
        pipeline.Run(context);


        onProgress?.Invoke(1f, "世界生成完成!");

        yield return null;
    }


}
