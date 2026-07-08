using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 生成管线：按注册顺序依次执行各阶段。
/// </summary>
public class GenerationPipeline {
    private readonly List<IGenerator> _phases = new();

    public GenerationPipeline AddPhase(IGenerator phase) {
        _phases.Add(phase);
        return this;
    }

    public void Run(GenerationContext _ctx) {
        foreach (var phase in _phases) {
            Debug.Log($"[Pipeline] Running phase: {phase.Name}");
            phase.Generate(_ctx);
        }
        Debug.Log("[Pipeline] Generation complete.");
    }
}