using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 材料物理配置，集中管理所有材料的物理定义
/// 借鉴 PixelAlchemy 的 MaterialDatabase 设计
/// </summary>
[CreateAssetMenu(fileName = "MaterialPhysicsConfig", menuName = "Physics/Material Physics Config")]
public class MaterialPhysicsConfig : ScriptableObject {
    [Header("默认材料定义")]
    public MaterialDefinitionEntry[] materialEntries;

    // 缓存字典，通过 blockId 快速查询
    private Dictionary<long, SimulationMaterialDefinition> lookupCache;
    private bool isCacheBuilt = false;

    /// <summary>
    /// 材料定义条目
    /// </summary>
    [System.Serializable]
    public class MaterialDefinitionEntry {
        public string materialName;                    // 材料名称（用于调试）
        public long blockId;                           // 对应的 blockId
        public SimulationMaterialDefinition definition; // 物理定义
    }

    /// <summary>
    /// 构建查询缓存
    /// </summary>
    public void BuildCache() {
        if (isCacheBuilt && lookupCache != null) return;

        lookupCache = new Dictionary<long, SimulationMaterialDefinition>();
        if (materialEntries == null) return;

        foreach (var entry in materialEntries) {
            if (entry.blockId != 0 && entry.definition != null) {
                lookupCache[entry.blockId] = entry.definition;
            }
        }
        isCacheBuilt = true;
    }

    /// <summary>
    /// 获取材料的物理定义
    /// </summary>
    /// <param name="blockId">材料的 blockId</param>
    /// <returns>物理定义，如果不存在返回 null</returns>
    public SimulationMaterialDefinition GetDefinition(long blockId) {
        if (!isCacheBuilt) BuildCache();

        if (blockId == 0) return null;
        lookupCache.TryGetValue(blockId, out var definition);
        return definition;
    }

    /// <summary>
    /// 检查材料是否可以参与物理模拟
    /// </summary>
    public bool IsSimulatedMaterial(long blockId) {
        var def = GetDefinition(blockId);
        return def != null && def.CanMove;
    }

    /// <summary>
    /// 检查材料是否为液体
    /// </summary>
    public bool IsLiquidMaterial(long blockId) {
        var def = GetDefinition(blockId);
        return def != null && def.IsLiquid;
    }

    /// <summary>
    /// 检查材料是否为粉末
    /// </summary>
    public bool IsPowderMaterial(long blockId) {
        var def = GetDefinition(blockId);
        return def != null && def.IsPowder;
    }

    /// <summary>
    /// 获取材料密度
    /// </summary>
    public int GetDensity(long blockId) {
        var def = GetDefinition(blockId);
        return def?.density ?? 0;
    }

    /// <summary>
    /// 获取液体最小体积阈值
    /// </summary>
    public float GetMinVolume(long blockId) {
        var def = GetDefinition(blockId);
        return def?.minVolume ?? 0.005f;
    }

    /// <summary>
    /// 获取液体最大体积
    /// </summary>
    public float GetMaxVolume(long blockId) {
        var def = GetDefinition(blockId);
        return def?.maxVolume ?? 1f;
    }

    private void OnValidate() {
        // 配置变化时重建缓存
        isCacheBuilt = false;
    }
}
