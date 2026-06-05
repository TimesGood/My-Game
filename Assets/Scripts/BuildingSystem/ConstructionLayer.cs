using System.Collections.Generic;
using UnityEngine;

// 图层包装 —— 持有 Unity Tilemap 引用，数据委托给 ChunkManager
public class ConstructionLayer : TilemapLayer {
    // 区块尺寸（由 WorldSetting 计算得出）
    public Vector2Int ChunkCount { get; private set; }
    public Vector2Int ChunkSize { get; private set; }

    private void Start() {
        InitLayer();
    }

    public virtual void InitLayer() {
        ChunkCount = WorldSetting.chunkCount;
        ChunkSize = new Vector2Int(
            WorldSetting.worldSize.x / ChunkCount.x,
            WorldSetting.worldSize.y / ChunkCount.y);
    }

    // 放置图块（当 ChunkManager 管理数据时的直接 Tilemap 操作）
    public virtual void Build(Vector3 worldCoords, TileClass item) {
        var coords = _tilemap.WorldToCell(worldCoords);

        if (item.tile != null)
            _tilemap.SetTile(coords, item.tile);

        if (item.gameObject != null)
            Instantiate(item.gameObject, _tilemap.CellToWorld(coords) + _tilemap.cellSize / 2, Quaternion.identity);

        // 数据通过 WorldManager.PlaceTile 存入 ChunkManager
    }

    // 检查图块位置是否为空（同时检查数据和视觉）
    public bool IsEmpty(Vector3 worldCoords) {
        var coords = _tilemap.WorldToCell(worldCoords);
        TileData tileData = WorldManager.Instance.GetTileData(coords.x, coords.y);
        return tileData.IsEmpty && _tilemap.GetTile(coords) == null;
    }

    // 销毁图块（仅视觉——数据由 WorldManager.Erase 处理）
    public virtual void Destory(Vector3 worldCoords) {
        var coords = _tilemap.WorldToCell(worldCoords);
        _tilemap.SetTile(coords, null);
    }
}
