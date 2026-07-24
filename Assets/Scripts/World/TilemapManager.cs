using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 所有Tilemap管理器
/// </summary>
public class TilemapManager : Singleton<TilemapManager>
{
    private Dictionary<LayerType, TilemapLayer> _lookup;

    protected override void Awake() {
        base.Awake();
        BuildLookup();
    }
    // ══════════════════════════════════════
    //  初始化
    // ══════════════════════════════════════

    // 注册
    [RuntimeInitializeOnLoadMethod]
    private static void Initialize() {
        TileRegistry_.ClearRegistry();

        string[] assetNames = AssetDatabase.FindAssets("", new[] { "Assets/Data/Tiles" });
        int i = 0;
        foreach (string SOName in assetNames) {
            var SOpath = AssetDatabase.GUIDToAssetPath(SOName);
            var itemData = AssetDatabase.LoadAssetAtPath<TileClass>(SOpath);
            if (itemData == null) continue;
            TileRegistry_.RegisterTile(itemData);
            i++;
        }
        Debug.Log($"已注册 {i} 个图块");
    }
    private void BuildLookup() {
        _lookup = new Dictionary<LayerType, TilemapLayer>();

        TilemapLayer[] tilemapLayers = GetComponentsInChildren<TilemapLayer>();
        foreach (var tl in tilemapLayers) {
            if (_lookup.ContainsKey(tl.layer)) {
                Debug.LogError($"[TilemapLayerManager] 重复的层级类型: {tl.layer}，" +
                               $"已存在 {_lookup[tl.layer].name}，忽略 {tl.name}");
                continue;
            }
            _lookup.Add(tl.layer, tl);
        }


        // 校验完整性
        foreach (LayerType type in System.Enum.GetValues(typeof(LayerType))) {
            if (!_lookup.ContainsKey(type))
                Debug.LogWarning($"[TilemapLayerManager] 缺少层级: {type}");
        }
    }

    // ══════════════════════════════════════
    //  查询接口
    // ══════════════════════════════════════

    /// <summary>获取指定层级</summary>
    public TilemapLayer GetLayer(LayerType type) {
        return _lookup.TryGetValue(type, out var layer) ? layer : null;
    }

    /// <shortcut>直接获取指定层级的 Tilemap 组件</shortcut>
    public Tilemap GetTilemap(LayerType type) {
        return GetLayer(type)?._tilemap;
    }

    /// <summary>检查指定层级是否存在</summary>
    public bool HasLayer(LayerType type) {
        return _lookup.ContainsKey(type);
    }
}
