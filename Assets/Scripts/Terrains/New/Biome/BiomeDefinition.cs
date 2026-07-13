// =============================================
//  BiomeDefinition.cs — 群落定义（含 Feature 组合）
// =============================================
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 群落定义 = "是什么" + "怎么生成"。
/// 通过 [SerializeReference] 内联 Feature 列表，自定义组合该群落的生成规则。
/// </summary>
public abstract class BiomeDefinition : ScriptableObject {

    [Header("基本信息")]
    public string biomeId;
    public string BiomeName = "New Biome";

    [Header("优先级（越高越先分配）")]
    public int Priority = 0;

    [Header("Feature 列表（按顺序执行）")]
    [SerializeReference]
    public List<BiomeFeature> _features = new List<BiomeFeature>();
    public List<BiomeFeature> features => _features;


    /// <summary>
    /// 对指定群落实例执行生成
    /// </summary>
    public abstract void Generate(BiomeContext _ctx);
}
