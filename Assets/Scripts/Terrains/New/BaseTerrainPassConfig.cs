// =============================================
//  BaseTerrainPassConfig.cs — 基础地形生成配置
//  在群落生成之前执行，生成整个世界的地壳分层结构、洞穴、矿石
//  产出数据写入 GenerationContext，供后续群落访问和覆盖
// =============================================
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "MapGen/Base Terrain Config")]
public class BaseTerrainPassConfig : ScriptableObject
{
    [Header("基本信息")]
    public string biomeId;
    public string BiomeName = "New Biome";

    [Header("优先级（越高越先分配）")]
    public int Priority = 0;

    [Header("Feature 列表（按顺序执行）")]
    [SerializeReference]
    private List<BiomeFeature> _features = new List<BiomeFeature>();
    public List<BiomeFeature> features => _features;

    /// <summary>
    /// 执行基础地形生成
    /// </summary>
    public void Execute(GenerationContext _ctx, RectInt region)
    {
        for (int i = 0; i < _features.Count; i++) {
            var f = _features[i];
            if (f == null) continue;
            Debug.Log($"  → [{BiomeName}] Feature[{i}] {f.GetType().Name}");
            f.Execute(_ctx, region);
        }
    }

}
