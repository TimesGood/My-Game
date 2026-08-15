using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// 模拟网格，负责追踪活跃区域和脏区块
/// 借鉴 PixelAlchemy 的活跃区域优化设计
/// </summary>
public class SimulationGrid {
    // 世界尺寸
    public int Width { get; }
    public int Height { get; }

    // 活跃像素追踪（双缓冲）
    private readonly HashSet<Vector2Int> activeCells = new HashSet<Vector2Int>();
    private readonly HashSet<Vector2Int> nextActiveCells = new HashSet<Vector2Int>();

    // 统计信息
    public int ActiveCellCount => activeCells.Count;

    /// <summary>
    /// 创建模拟网格
    /// </summary>
    /// <param name="width">世界宽度</param>
    /// <param name="height">世界高度</param>
    /// <param name="chunkSize">区块大小</param>
    /// <param name="chunkSleepDelay">区块休眠延迟帧数</param>
    public SimulationGrid(int width, int height, int chunkSize = 16, int chunkSleepDelay = 3) {
        Width = Mathf.Max(1, width);
        Height = Mathf.Max(1, height);
    }

    /// <summary>
    /// 检查坐标是否在世界范围内
    /// </summary>
    public bool InBounds(int x, int y) {
        return x >= 0 && x < Width && y >= 0 && y < Height;
    }

    /// <summary>
    /// 检查坐标是否在世界范围内
    /// </summary>
    public bool InBounds(Vector2Int pos) {
        return InBounds(pos.x, pos.y);
    }

    ///// <summary>
    ///// 开始新的模拟步骤
    ///// </summary>
    //public void BeginSimulationStep() {
    //    // 清空下一帧缓冲
    //    nextActiveCells.Clear();
    //}

    ///// <summary>
    ///// 结束模拟步骤，提交下一帧的活跃集合
    ///// </summary>
    //public void EndSimulationStep() {

    //    // 交换活跃像素缓冲
    //    SwapActiveCellBuffers();
    //}

    public HashSet<Vector2Int> Next() {
        SwapActiveCellBuffers();
        return activeCells;
    }

    /// <summary>
    /// 标记格子变化，唤醒周围区域
    /// </summary>
    /// <param name="x">X坐标</param>
    /// <param name="y">Y坐标</param>
    public void MarkChanged(int x, int y) {
        if (!InBounds(x, y)) return;

        // 唤醒当前格子和周围格子（3x3区域）
        MarkActiveArea(x, y, 1);
    }

    /// <summary>
    /// 标记格子变化，唤醒周围区域
    /// </summary>
    public void MarkChanged(Vector2Int pos) {
        MarkChanged(pos.x, pos.y);
    }

    /// <summary>
    /// 仅保活当前格子（不唤醒 3×3 邻居、不唤醒区块、不重置区块睡眠计数）。
    /// 用于"尚未到更新时机"的格子：让它下帧继续待处理，但不扩散活跃区域，
    /// 使静止区域能被区块休眠机制正常收回，活跃集合得以收缩。
    /// </summary>
    public void KeepActive(int x, int y) {
        if (!InBounds(x, y)) return;

        var pos = new Vector2Int(x, y);
        //activeCells.Add(pos);
        nextActiveCells.Add(pos);
    }

    /// <summary>
    /// 标记区域为活跃
    /// </summary>
    /// <param name="centerX">中心X坐标</param>
    /// <param name="centerY">中心Y坐标</param>
    /// <param name="radius">半径</param>
    public void MarkActiveArea(int centerX, int centerY, int radius) {
        int safeRadius = Mathf.Max(0, radius);
        for (int y = centerY - safeRadius; y <= centerY + safeRadius; y++) {
            for (int x = centerX - safeRadius; x <= centerX + safeRadius; x++) {
                if (InBounds(x, y)) {
                    // 同时写入当前和下一帧缓冲
                    //activeCells.Add(new Vector2Int(x, y));
                    nextActiveCells.Add(new Vector2Int(x, y));
                }
            }
        }
    }

    /// <summary>
    /// 检查格子是否为活跃状态
    /// </summary>
    public bool IsCellActive(int x, int y) {
        return InBounds(x, y) && activeCells.Contains(new Vector2Int(x, y));
    }

    /// <summary>
    /// 检查格子是否为活跃状态
    /// </summary>
    public bool IsCellActive(Vector2Int pos) {
        return IsCellActive(pos.x, pos.y);
    }

    /// <summary>
    /// 获取所有活跃格子（返回副本，安全遍历）
    /// </summary>
    public IEnumerable<Vector2Int> GetActiveCells() {
        return new List<Vector2Int>(activeCells);
    }

    /// <summary>
    /// 获取活跃格子数量
    /// </summary>
    public int GetActiveCellCount() {
        return activeCells.Count;
    }


    /// <summary>
    /// 清空所有活跃状态
    /// </summary>
    public void ClearAll() {
        activeCells.Clear();
        nextActiveCells.Clear();
    }


    /// <summary>
    /// 交换活跃像素缓冲
    /// </summary>
    private void SwapActiveCellBuffers() {
        activeCells.Clear();
        activeCells.AddRange(nextActiveCells);
        nextActiveCells.Clear();
    }
}
