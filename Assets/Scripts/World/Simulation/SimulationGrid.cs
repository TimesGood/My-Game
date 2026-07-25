using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 模拟网格，负责追踪活跃区域和脏区块
/// 借鉴 PixelAlchemy 的活跃区域优化设计
/// </summary>
public class SimulationGrid {
    // 世界尺寸
    public int Width { get; }
    public int Height { get; }

    // 区块配置
    public int ChunkSize { get; private set; } = 16;
    public int ChunkColumns { get; private set; }
    public int ChunkRows { get; private set; }

    // 活跃像素追踪（双缓冲）
    private readonly HashSet<Vector2Int> activeCells = new HashSet<Vector2Int>();
    private readonly HashSet<Vector2Int> nextActiveCells = new HashSet<Vector2Int>();

    // 区块脏区域追踪
    private readonly HashSet<Vector2Int> activeChunks = new HashSet<Vector2Int>();
    private readonly HashSet<Vector2Int> nextActiveChunks = new HashSet<Vector2Int>();
    private readonly HashSet<Vector2Int> changedChunks = new HashSet<Vector2Int>();

    // 睡眠计数器（区块休眠延迟）
    private readonly Dictionary<Vector2Int, int> chunkSleepFrames = new Dictionary<Vector2Int, int>();
    public int ChunkSleepDelay { get; private set; } = 3;

    // 统计信息
    public int ActiveCellCount => activeCells.Count;
    public int ActiveChunkCount => activeChunks.Count;

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
        ChunkSize = Mathf.Max(4, chunkSize);
        ChunkSleepDelay = Mathf.Max(0, chunkSleepDelay);

        ChunkColumns = Mathf.CeilToInt((float)Width / ChunkSize);
        ChunkRows = Mathf.CeilToInt((float)Height / ChunkSize);
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

    /// <summary>
    /// 开始新的模拟步骤
    /// </summary>
    public void BeginSimulationStep() {
        // 清空下一帧缓冲
        nextActiveCells.Clear();
        nextActiveChunks.Clear();
        changedChunks.Clear();
    }

    /// <summary>
    /// 结束模拟步骤，提交下一帧的活跃集合
    /// </summary>
    public void EndSimulationStep() {
        // 构建下一帧区块（基于脏区块和睡眠计数）
        BuildNextChunkFrame();

        // 交换活跃像素缓冲
        SwapActiveCellBuffers();

        // 交换活跃区块缓冲
        SwapActiveChunkBuffers();
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

        // 标记所在区块和相邻区块为活跃
        MarkChunkAndNeighborsActive(x, y);

        // 标记区块为已变化
        MarkChunkChanged(x, y);
    }

    /// <summary>
    /// 标记格子变化，唤醒周围区域
    /// </summary>
    public void MarkChanged(Vector2Int pos) {
        MarkChanged(pos.x, pos.y);
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
                    activeCells.Add(new Vector2Int(x, y));
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
    /// 检查区块是否为活跃状态
    /// </summary>
    public bool IsChunkActive(int chunkX, int chunkY) {
        return chunkX >= 0 && chunkX < ChunkColumns &&
               chunkY >= 0 && chunkY < ChunkRows &&
               activeChunks.Contains(new Vector2Int(chunkX, chunkY));
    }

    /// <summary>
    /// 获取所有活跃区块（返回副本，安全遍历）
    /// </summary>
    public IEnumerable<Vector2Int> GetActiveChunks() {
        return new List<Vector2Int>(activeChunks);
    }

    /// <summary>
    /// 获取活跃区块数量
    /// </summary>
    public int GetActiveChunkCount() {
        return activeChunks.Count;
    }

    /// <summary>
    /// 世界坐标转区块坐标
    /// </summary>
    public Vector2Int WorldToChunkCoord(int x, int y) {
        return new Vector2Int(
            Mathf.Clamp(x / ChunkSize, 0, ChunkColumns - 1),
            Mathf.Clamp(y / ChunkSize, 0, ChunkRows - 1)
        );
    }

    /// <summary>
    /// 世界坐标转区块坐标
    /// </summary>
    public Vector2Int WorldToChunkCoord(Vector2Int worldPos) {
        return WorldToChunkCoord(worldPos.x, worldPos.y);
    }

    /// <summary>
    /// 激活所有区域（用于初始化或重建）
    /// </summary>
    public void ActivateAll() {
        activeCells.Clear();
        nextActiveCells.Clear();
        activeChunks.Clear();
        nextActiveChunks.Clear();
        chunkSleepFrames.Clear();

        // 激活所有格子
        for (int y = 0; y < Height; y++) {
            for (int x = 0; x < Width; x++) {
                var pos = new Vector2Int(x, y);
                activeCells.Add(pos);
                nextActiveCells.Add(pos);
            }
        }

        // 激活所有区块
        for (int cy = 0; cy < ChunkRows; cy++) {
            for (int cx = 0; cx < ChunkColumns; cx++) {
                var chunk = new Vector2Int(cx, cy);
                activeChunks.Add(chunk);
                nextActiveChunks.Add(chunk);
            }
        }
    }

    /// <summary>
    /// 清空所有活跃状态
    /// </summary>
    public void ClearAll() {
        activeCells.Clear();
        nextActiveCells.Clear();
        activeChunks.Clear();
        nextActiveChunks.Clear();
        changedChunks.Clear();
        chunkSleepFrames.Clear();
    }

    // ===== 私有方法 =====

    /// <summary>
    /// 标记区块及其邻居为活跃
    /// </summary>
    private void MarkChunkAndNeighborsActive(int x, int y) {
        Vector2Int chunkCoord = WorldToChunkCoord(x, y);

        // 唤醒3x3区块邻域
        for (int cy = chunkCoord.y - 1; cy <= chunkCoord.y + 1; cy++) {
            for (int cx = chunkCoord.x - 1; cx <= chunkCoord.x + 1; cx++) {
                if (cx >= 0 && cx < ChunkColumns && cy >= 0 && cy < ChunkRows) {
                    var chunk = new Vector2Int(cx, cy);
                    activeChunks.Add(chunk);
                    nextActiveChunks.Add(chunk);
                    // 重置睡眠计数
                    chunkSleepFrames[chunk] = 0;
                }
            }
        }
    }

    /// <summary>
    /// 标记区块为已变化
    /// </summary>
    private void MarkChunkChanged(int x, int y) {
        Vector2Int chunkCoord = WorldToChunkCoord(x, y);
        changedChunks.Add(chunkCoord);
    }

    /// <summary>
    /// 构建下一帧区块（基于脏区块和睡眠计数）
    /// </summary>
    private void BuildNextChunkFrame() {
        foreach (var chunk in activeChunks) {
            if (changedChunks.Contains(chunk)) {
                // 有变化的区块重置睡眠计数，继续活跃
                chunkSleepFrames[chunk] = 0;
                nextActiveChunks.Add(chunk);
            } else {
                // 未变化的区块增加睡眠计数
                int sleepFrames = 0;
                chunkSleepFrames.TryGetValue(chunk, out sleepFrames);
                sleepFrames++;
                chunkSleepFrames[chunk] = sleepFrames;

                // 未超过休眠延迟的区块继续保持活跃
                if (sleepFrames <= ChunkSleepDelay) {
                    nextActiveChunks.Add(chunk);
                }
            }
        }
    }

    /// <summary>
    /// 交换活跃像素缓冲
    /// </summary>
    private void SwapActiveCellBuffers() {
        activeCells.Clear();
        foreach (var cell in nextActiveCells) {
            activeCells.Add(cell);
        }
    }

    /// <summary>
    /// 交换活跃区块缓冲
    /// </summary>
    private void SwapActiveChunkBuffers() {
        activeChunks.Clear();
        foreach (var chunk in nextActiveChunks) {
            activeChunks.Add(chunk);
        }
    }
}
