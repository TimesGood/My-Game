// =============================================
//  BiomeGeneratorBase.cs — 群落生成器抽象基类
// =============================================
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 生成器职责：在 BiomeInstance.Bounds 范围内填充 TileMap。
/// 每个生成器通过 Id 与 BiomeDefinition.GeneratorId 对应。
/// </summary>
public abstract class BiomeGeneratorBase : ScriptableObject {
    [Header("生成器唯一 ID")]
    public string Id = "Default";

    /// <summary>入口：在 instance.Bounds 范围内填充 tileMap</summary>
    public abstract void Generate(
        GenerationContext context);
}
