using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.SocialPlatforms;

// ======================================================================
// BiomeContext —— 运行时上下文
// ======================================================================

/// <summary>
/// 群落生成上下文，由 BiomeDefinition.Generate() 创建。
/// 封装了全局 GenerationContext + 当前 BiomeInstance，并提供 Feature 间共享状态。
/// </summary>
public class BiomeContext
{
    /// <summary>全局生成上下文</summary>
    public GenerationContext genContext;
    /// <summary> 当前群落定义 </summary>
    public BiomeDefinition Definition;
    /// <summary>当前群落实例</summary>
    public BiomeInstance Instance;
    /// <summary>当前执行区域（全局群落 = 整张地图，分配群落 = 其 Bounds）</summary>
    public RectInt Bounds { get; }
    public int Seed { get; }

    /// <summary>群落最小世界坐标</summary>
    public Vector2Int minPos => new Vector2Int(Bounds.xMin, Bounds.yMin);
    /// <summary>群落最大世界坐标</summary>
    public Vector2Int maxPos => new Vector2Int(Bounds.xMax, Bounds.yMax);
    /// <summary>群落大小</summary>
    public Vector2Int biomeSize => new Vector2Int(Bounds.width, Bounds.height);

    /// <summary>噪声纹理缓存，Feature 间共享</summary>
    public Dictionary<string, Texture2D> noiseCache = new Dictionary<string, Texture2D>();
    /// <summary>通用共享状态</summary>
    public Dictionary<string, object> shared = new Dictionary<string, object>();

    /// <summary>用于分配群落</summary>
    public BiomeContext(GenerationContext global, BiomeInstance instance) {
        genContext = global;
        Instance = instance;
        Definition = instance.Def;
        Bounds = instance.Bounds;
        Seed = global.Seed;
    }

    /// <summary>用于全局群落（没有 Placement）</summary>
    public BiomeContext(GenerationContext global, BiomeDefinition globalBiome) {
        genContext = global;
        Instance = null;
        Definition = globalBiome;
        Bounds = new RectInt(0, 0, global.Width, global.Height);
        Seed = global.Seed;
    }

    // 便捷方法
    public int LocalToWorldX(int _x) => _x + minPos.x;
    public int LocalToWorldY(int _y) => _y + minPos.y;
    public Vector2Int LocalToWorld(int _x, int _y) => new Vector2Int(LocalToWorldX(_x), LocalToWorldY(_y));
}

// ======================================================================
// BiomeFeature —— Feature 抽象基类
// ======================================================================

/// <summary>
/// 群落 Feature 抽象基类。
/// 数据通过 [SerializeReference] 内联在 BiomeDefinition 的 .asset 中。
/// 每个 Feature 做一件事，组合使用。
/// </summary>
[System.Serializable]
public abstract class BiomeFeature
{
    /// <summary>
    /// 执行该 Feature 的生成逻辑
    /// </summary>
    public abstract void Execute(BiomeContext _ctx);
}
