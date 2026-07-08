using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

/// <summary>
/// Phase 2: 全局群落生成。
/// 全局群落作用于全地图。
/// </summary>
public class GlobalBiomePhase : IGenerator {
    public int Order => 0;

    public string Name => "GlobalBiomePhase";

    private readonly List<GobalDefinition> _globalBiomes;

    public GlobalBiomePhase(List<GobalDefinition> globalBiomes) {
        _globalBiomes = globalBiomes;
    }

    public void Generate(GenerationContext context) {
        if (_globalBiomes == null) return;
        foreach (var def in _globalBiomes) {
            // 全局群落不需要 bounds，它作用于整个世界
            var biomeCtx = new BiomeContext(context, def);
            def.Generate(biomeCtx);
        }
    }
}
