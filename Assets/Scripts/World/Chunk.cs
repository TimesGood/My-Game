using System;
using UnityEngine;

/// <summary>
/// 世界空间区块，持有统一的 TileData 二维数组。
/// 图块数据的唯一权威来源——不再使用逐层字典分散存储。
/// </summary>
[Serializable]
public class Chunk {
    public Vector2Int coord;       // 区块网格坐标
    public TileData[,] tiles;      // [x, y] 图块权威数据
    public BoundsInt bounds;       // 世界空间包围盒
    public bool isDirty;           // 数据变更时置 true，用于触发渲染刷新

    public Chunk(Vector2Int coord, int width, int height, int worldXOffset, int worldYOffset) {
        this.coord = coord;
        tiles = new TileData[width, height];
        bounds = new BoundsInt(
            new Vector3Int(worldXOffset, worldYOffset, 0),
            new Vector3Int(width, height, 1));
        isDirty = true; // 新区块需要渲染
    }

    // 局部坐标转世界坐标
    public Vector2Int LocalToWorld(int localX, int localY) {
        return new Vector2Int(bounds.xMin + localX, bounds.yMin + localY);
    }

    // 世界坐标转局部索引
    public bool WorldToLocal(Vector2Int worldPos, out int localX, out int localY) {
        localX = worldPos.x - bounds.xMin;
        localY = worldPos.y - bounds.yMin;
        return localX >= 0 && localX < tiles.GetLength(0)
            && localY >= 0 && localY < tiles.GetLength(1);
    }

    // 通过世界坐标获取图块数据
    public bool TryGetTile(Vector2Int worldPos, out TileData tile) {
        if (WorldToLocal(worldPos, out int lx, out int ly)) {
            tile = tiles[lx, ly];
            return true;
        }
        tile = default;
        return false;
    }

    // 通过世界坐标设置图块数据
    public bool TrySetTile(Vector2Int worldPos, TileData tile) {
        if (WorldToLocal(worldPos, out int lx, out int ly)) {
            tiles[lx, ly] = tile;
            isDirty = true;
            return true;
        }
        return false;
    }

    public int Width => tiles.GetLength(0);
    public int Height => tiles.GetLength(1);
}
