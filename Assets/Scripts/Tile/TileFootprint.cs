using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
///  瓦片精灵实际占位结构体
/// </summary>
[System.Serializable]
public class TileFootprint
{
    [Header("网格尺寸")]
    public int gridWidth = 7;
    public int gridHeight = 7;

    [Header("原点 (树桩/植株根部在网格内的坐标)")]
    public Vector2Int originPoint = new Vector2Int(3, 0);

    [HideInInspector]
    public bool[] clearMap;    // 长度 = gridWidth * gridHeight

    // =====================================================================
    //  初始化
    // =====================================================================

    public void Initialize() {
        clearMap = new bool[gridWidth * gridHeight];
        originPoint = new Vector2Int(gridWidth / 2, gridHeight / 2);
    }

    // =====================================================================
    //  查询（网格坐标）
    // =====================================================================

    public bool ShouldClear(int gridX, int gridY) {
        if (gridX < 0 || gridX >= gridWidth || gridY < 0 || gridY >= gridHeight)
            return false;
        return clearMap[gridY * gridWidth + gridX];
    }

    // =====================================================================
    //  世界坐标转换
    // =====================================================================

    /// <summary>
    /// 获取需要清除的地面格子在世界坐标中的列表。
    /// </summary>
    public List<Vector2Int> GetWorldClearCells(int anchorX, int anchorY) {
        var cells = new List<Vector2Int>();
        for (int gx = 0; gx < gridWidth; gx++) {
            for (int gy = 0; gy < gridHeight; gy++) {
                if (ShouldClear(gx, gy)) {
                    cells.Add(new Vector2Int(
                        gx - originPoint.x + anchorX,
                        gy - originPoint.y + anchorY));
                }
            }
        }
        return cells;
    }

    /// <summary>
    /// 获取占据区域的世界包围盒。
    /// </summary>
    public RectInt GetWorldBounds(int anchorX, int anchorY) {
        int xMin = int.MaxValue, xMax = int.MinValue;
        int yMin = int.MaxValue, yMax = int.MinValue;

        for (int gx = 0; gx < gridWidth; gx++) {
            for (int gy = 0; gy < gridHeight; gy++) {
                if (!ShouldClear(gx, gy)) continue;

                int wx = gx - originPoint.x + anchorX;
                int wy = gy - originPoint.y + anchorY;
                if (wx < xMin) xMin = wx;
                if (wx > xMax) xMax = wx;
                if (wy < yMin) yMin = wy;
                if (wy > yMax) yMax = wy;
            }
        }

        return new RectInt(xMin, yMin, xMax - xMin + 1, yMax - yMin + 1);
    }

    /// <summary>
    /// 统计 clearMap 中被标记的格子总数。
    /// </summary>
    public int CountClearCells() {
        int count = 0;
        for (int i = 0; i < clearMap.Length; i++)
            if (clearMap[i]) count++;
        return count;
    }

    /// <summary>
    /// 统计 occupyMap 中被标记的格子总数。
    /// </summary>
    public int CountOccupyCells() {
        int count = 0;
        for (int i = 0; i < clearMap.Length; i++)
            if (clearMap[i]) count++;
        return count;
    }
}
