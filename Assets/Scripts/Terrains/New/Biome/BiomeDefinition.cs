// =============================================
//  BiomeDefinition.cs — 群落定义（含 Feature 组合）
// =============================================
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 群落定义 = "是什么" + "怎么生成"。
/// 通过 [SerializeReference] 内联 Feature 列表，自定义该群落的生成规则。
/// </summary>
[CreateAssetMenu(menuName = "MapGen/Biome Definition")]
public class BiomeDefinition : ScriptableObject {

    [Header("基本信息")]
    public string biomeId;
    public string BiomeName = "New Biome";

    [Header("尺寸大小")]
    public Vector2Int biomeSize = new(300, 100);

    [Header("适合度范围")]
    public Suitable[] suitable;

    [Header("优先级（越高越先分配）")]
    public int Priority = 0;

    [Header("是否允许与其他群落重叠")]
    public bool AllowOverlap = false;

    [Header("生成数量")]
    public int num = 1;

    [Header("群落轮廓（可选，不规则形状）")]
    //public ShapeGenerator outLine;

    [Header("Feature 列表（按顺序执行）")]
    [SerializeReference]
    private List<BiomeFeature> _features = new List<BiomeFeature>();
    public List<BiomeFeature> features => _features;

    [System.NonSerialized] private bool _initialized;

    // -------- 生成 --------

    /// <summary>
    /// 初始化所有 Feature 的噪声（仅执行一次）
    /// </summary>
    public void InitFeatures(GenerationContext _ctx, RectInt region)
    {
        if (_initialized) return;
        _initialized = true;

        var cache = new Dictionary<string, Texture2D>();

        //outLine?.InitValidate(biomeSize.x, biomeSize.y, _seed);
        //outLine?.InitNoise();

        for (int i = 0; i < _features.Count; i++)
        {
            _features[i]?.Init(_ctx, region);
        }
    }

    /// <summary>
    /// 对指定群落实例执行生成
    /// </summary>
    public void Generate(GenerationContext _ctx, BiomeInstance _inst)
    {
        InitFeatures(_ctx, _inst.Bounds);
        //DetectSurfaceRange(biomeCtx);

        if (_features.Count == 0)
        {
            Debug.LogWarning($"[BiomeDefinition] '{BiomeName}' 没有配置任何 Feature，跳过生成");
            return;
        }

        for (int i = 0; i < _features.Count; i++)
        {
            var f = _features[i];
            if (f == null) continue;
            Debug.Log($"  → [{BiomeName}] Feature[{i}] {f.GetType().Name}");
            f.Execute(_ctx, _inst.Bounds);
        }
    }

    private BiomeContext BuildContext(GenerationContext _ctx, BiomeInstance _inst)
    {
        return new BiomeContext
        {
            genContext = _ctx,
            instance = _inst,
            noiseCache = new Dictionary<string, Texture2D>()
        };
    }

    /// <summary>
    /// 初始化实际群落在地表露出的范围（基于地平线）
    /// </summary>
    /// <param name="_ctx"></param>
    private void DetectSurfaceRange(BiomeContext _ctx)
    {
        int baseHeight = WorldManager.Instance.baseHeight;
        int start = 0, end = 0;

        //if (outLine != null)
        //{
        //    for (int x = 0; x < _ctx.biomeSize.x; x++)
        //    {
        //        if (outLine.noiseTexture.GetPixel(x, baseHeight).r > 0.5f)
        //        { start = _ctx.LocalToWorldX(x); break; }
        //    }
        //    for (int x = _ctx.biomeSize.x - 1; x >= 0; x--)
        //    {
        //        if (outLine.noiseTexture.GetPixel(x, baseHeight).r > 0.5f)
        //        { end = _ctx.LocalToWorldX(x); break; }
        //    }
        //}
        //else
        //{
        //    start = _ctx.minPos.x;
        //    end = _ctx.maxPos.x;
        //}

        _ctx.surfaceStart = start;
        _ctx.surfaceEnd = end;
    }

    // -------- 适合度 --------

    [Serializable]
    public class Suitable
    {
        public Vector2Int SuitableMin = new(0, 0);
        public Vector2Int SuitableMax = new(0, 0);
    }
}
