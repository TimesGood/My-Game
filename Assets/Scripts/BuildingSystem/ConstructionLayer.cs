using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.PlayerSettings;
using static WorldManager;

// 图层包装 —— 持有 Unity Tilemap 引用，数据委托给 ChunkManager
public class ConstructionLayer : TilemapLayer {
    // 区块尺寸（由 WorldSetting 计算得出）
    public Vector2Int ChunkCount { get; private set; }
    public Vector2Int ChunkSize { get; private set; }

    public static Vector2Int[] directions = {
        Vector2Int.up,
        Vector2Int.down,
        Vector2Int.left,
        Vector2Int.right
    };

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
    public virtual void Build(Vector2Int worldCoords, TileClass item) {
        var coords = _tilemap.WorldToCell(new Vector3Int(worldCoords.x, worldCoords.y));

        if (item.tile != null)
            _tilemap.SetTile(coords, item.tile);

        if (item.gameObject != null)
            Instantiate(item.gameObject, _tilemap.CellToWorld(coords) + _tilemap.cellSize / 2, Quaternion.identity);

        // 数据通过 WorldManager.PlaceTile 存入 ChunkManager
        if (item is TreeClass) ((TreeClass)item).PlanceSelf(worldCoords.x, worldCoords.y);
        else {
            chunkManager.SetBlockId(layer, worldCoords, item.blockId);
        }
        
    }

    // 检查图块位置是否为空（同时检查数据和视觉）
    public bool IsEmpty(Vector2Int worldCoords) {
        var coords = _tilemap.WorldToCell(new Vector3Int(worldCoords.x, worldCoords.y));
        TileData tileData = WorldManager.Instance.GetTileData(coords.x, coords.y);
        return tileData.IsEmpty && _tilemap.GetTile(coords) == null;
    }

    // 销毁图块（仅视觉——数据由 WorldManager.Erase 处理）
    public virtual void Destory(Vector2Int worldCoords) {
        var coords = _tilemap.WorldToCell(new Vector3Int(worldCoords.x, worldCoords.y));
        _tilemap.SetTile(coords, null);
        chunkManager.SetBlockId(layer, worldCoords, 0);

        // 销毁图块时，如果周围有水体，激活模拟
        if (Layers.Ground == layer) {
            foreach (var dir in directions) {
                Vector2Int target = worldCoords + dir;
                long liquidId = chunkManager.GetLiquidId(target);
                if (liquidId == 0) continue;
                TileClass liquidTile = TileRegistry.GetTile(liquidId);
                if (liquidTile is LiquidClass liquidClass) {
                    if (LiquidHandler.Instance.CheckMarkForUpdate(liquidClass, target)) continue;
                    List<Vector2Int> tiles = FindConnectedTiles(Layers.Liquid, target);
                    foreach (var item in tiles) {
                        LiquidHandler.Instance.MarkForUpdate(liquidClass, item);
                    }
                }
            }
        }
    }

    // BFS 查找相连的同类型图块
    private List<Vector2Int> FindConnectedTiles(Layers layer, Vector2Int startPosition) {
        var connectedTiles = new List<Vector2Int>();
        var visited = new HashSet<Vector2Int>();
        var queue = new Queue<Vector2Int>();

        long startBlockId = chunkManager.GetBlockId(layer, startPosition);

        if (startBlockId == 0) return connectedTiles;

        queue.Enqueue(startPosition);
        visited.Add(startPosition);

        while (queue.Count > 0) {
            Vector2Int current = queue.Dequeue();
            connectedTiles.Add(current);

            foreach (var dir in directions) {
                Vector2Int neighbor = current + dir;
                if (visited.Contains(neighbor)) continue;
                long neighborBlockId = chunkManager.GetBlockId(layer, neighbor);
                if (startBlockId == neighborBlockId) {
                    queue.Enqueue(neighbor);
                    visited.Add(neighbor);
                }
            }
        }

        return connectedTiles;
    }
}
