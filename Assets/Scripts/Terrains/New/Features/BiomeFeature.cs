using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ======================================================================
// BiomeContext —— 运行时上下文（适配新架构 GenerationContext + BiomeInstance）
// ======================================================================

/// <summary>
/// 群落生成上下文，由 BiomeDefinition.Generate() 创建。
/// 封装了全局 GenerationContext + 当前 BiomeInstance，并提供 Feature 间共享状态。
/// </summary>
public class BiomeContext
{
    /// <summary>全局生成上下文</summary>
    public GenerationContext genContext;
    /// <summary>当前群落实例</summary>
    public BiomeInstance instance;

    /// <summary>群落最小世界坐标</summary>
    public Vector2Int minPos => new Vector2Int(instance.X, instance.Y);
    /// <summary>群落最大世界坐标</summary>
    public Vector2Int maxPos => new Vector2Int(instance.Right, instance.Top);
    /// <summary>群落大小</summary>
    public Vector2Int biomeSize => new Vector2Int(instance.Width, instance.Height);

    /// <summary>每列的地形高度（由 TerrainFeature 设置）</summary>
    public int[] terrainHeights;
    /// <summary>每列的世界 X 坐标缓存</summary>
    public int[] worldXs;
    /// <summary>群落内最大地形高度</summary>
    public int maxHeight;

    /// <summary>地表范围</summary>
    public int surfaceStart;
    public int surfaceEnd;

    /// <summary>噪声纹理缓存，Feature 间共享</summary>
    public Dictionary<string, Texture2D> noiseCache = new Dictionary<string, Texture2D>();
    /// <summary>通用共享状态</summary>
    public Dictionary<string, object> shared = new Dictionary<string, object>();

    // 便捷方法
    public int LocalToWorldX(int _x) => _x + minPos.x;
    public int LocalToWorldY(int _y) => _y + minPos.y;
    public Vector2Int LocalToWorld(int _x, int _y) => new Vector2Int(LocalToWorldX(_x), LocalToWorldY(_y));
    public bool IsSurfaceRange(int _x) => _x >= surfaceStart && _x <= surfaceEnd;
}

// ======================================================================
// BiomeFeature —— Feature 抽象基类（内联配置，非 ScriptableObject）
// ======================================================================

/// <summary>
/// 群落 Feature 抽象基类。
/// 数据通过 [SerializeReference] 内联在 BiomeDefinition 的 .asset 中。
/// 每个 Feature 做一件事，组合使用。
/// </summary>
[System.Serializable]
public abstract class BiomeFeature
{
    public string name;
    /// <summary>
    /// 初始化噪声等运行时资源（生成前调用）。
    /// </summary>
    public virtual void Init(Vector2Int _biomeSize, int _seed, Dictionary<string, Texture2D> _noiseCache) { }

    /// <summary>
    /// 执行该 Feature 的生成逻辑（非协程！新架构不使用协程）
    /// </summary>
    public abstract void Execute(BiomeContext _ctx);
}
