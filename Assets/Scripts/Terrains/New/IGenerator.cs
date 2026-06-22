using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ===== IGenerator.cs =====
// 生成管线中每一步的抽象接口
public interface IGenerator {
    /// <summary>生成顺序权重，越小越先执行</summary>
    int Order { get; }

    /// <summary>该生成器的名称（用于调试和日志）</summary>
    string Name { get; }

    /// <summary>执行生成</summary>
    void Generate(GenerationContext context);
}
