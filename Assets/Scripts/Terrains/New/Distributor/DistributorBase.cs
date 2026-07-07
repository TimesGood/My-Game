// =============================================
//  DistributorBase.cs — 分配器抽象基类
// =============================================
using System.Collections.Generic;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;

/// <summary>
/// 分配器职责：
/// 对拥有的群落进行随机点位分配
/// </summary>
public abstract class DistributorBase : ScriptableObject {

    [Header("优先级")]
    public int Priority = 0;

    [Header("群落")]
    public List<LocalDefinition> biomeDefinitions;


    /// <summary>入口：执行分配</summary>
    public abstract List<BiomeInstance> Distribute(
        GenerationContext context);

    // ---------- 通用辅助 ----------

    /// <summary>把实例加入结果集并加入</summary>
    protected void Commit(
        BiomeDefinition def, RectInt bounds,
        List<BiomeInstance> results, GenerationContext context) {
        BiomeInstance instance = new BiomeInstance {
            Def = def,
            Bounds = bounds,
            Seed = context.Seed
        };
        results.Add(instance);
        context.Placements.Add(instance);
    }

    // 矩阵交集
    public static RectInt Intersect(RectInt a, RectInt b) {
        // 各自的边界
        int xMin = Mathf.Max(a.xMin, b.xMin);
        int yMin = Mathf.Max(a.yMin, b.yMin);
        int xMax = Mathf.Min(a.xMax, b.xMax);
        int yMax = Mathf.Min(a.yMax, b.yMax);

        if (xMax <= xMin || yMax <= yMin)
            return new RectInt(0, 0, 0, 0);

        return new RectInt(xMin, yMin, xMax - xMin, yMax - yMin);
    }

    /// <summary>
    /// 判断两个 RectInt 是否有重叠。
    /// </summary>
    public static bool Overlaps(RectInt a, RectInt b) {
        return a.xMin < b.xMax && a.xMax > b.xMin &&
               a.yMin < b.yMax && a.yMax > b.yMin;
    }

    // 检索已放置的群落中与某矩阵产生交集的群落
    public List<BiomeInstance> GetBiomeOverlaps(List<BiomeInstance> allBiomeInstance, RectInt rect) {
        RectInt s = new RectInt();
        List<BiomeInstance> result = new List<BiomeInstance>();
        if (allBiomeInstance == null || allBiomeInstance.Count == 0) return result;

        foreach (var item in allBiomeInstance) {
            if (rect.Overlaps(item.Bounds)) result.Add(item);
        }
        return result;
    }
}
