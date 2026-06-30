using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using static PoissonDiscSampling;

//泊松圆盘算法，随机分布点位
public class PoissonDiscSampling {

    //采样
    //radius：相邻点距离 sampleRegionSize：采样地图大小 occupiedRegions: 已占点位 numSamplesBeforeRejection：候选点尝试生成次数
    public static List<Vector2> GeneratePoints(float radius, Vector2 regionMin, Vector2 regionMax, List<OccupiedRegion> occupiedRegions = null, int numSamplesBeforeRejection = 30) {
        // 区域尺寸
        Vector2 regionSize = regionMax - regionMin;

        //网格单元边长（直角三角斜边公式）
        float cellSize = radius / Mathf.Sqrt(2);

        // 创建网格来加速邻近点搜索;
        // 创建网格
        int gridWidth = Mathf.CeilToInt(regionSize.x / cellSize);
        int gridHeight = Mathf.CeilToInt(regionSize.y / cellSize);
        int[,] grid = new int[gridWidth, gridHeight];
        
        // 存储生成点
        List<Vector2> points = new List<Vector2>();
        // 存储活跃点
        List<Vector2> spawnPoints = new List<Vector2>();

        // 初始点：随机选择起始点，确保不在已占区域内
        if (occupiedRegions != null) {
            FindSpawnPoints(regionMin, regionMax, occupiedRegions, spawnPoints);
            if (spawnPoints.Count == 0) {
                Debug.LogWarning("无法找到有效的起始点");
                return points;
            }
        } else {
            spawnPoints.Add((regionMin + regionMax) / 2f); // 区域中心
        }


        while (spawnPoints.Count > 0) {
            //随机选择活跃点
            int spawnIndex = Random.Range(0, spawnPoints.Count);
            Vector2 spawnCentre = spawnPoints[spawnIndex];
            bool candidateAccepted = false;

            //尝试生成候选点
            for (int i = 0; i < numSamplesBeforeRejection; i++) {
                float angle = Random.value * Mathf.PI * 2;//随机角度
                Vector2 dir = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));//随机方向
                Vector2 candidate = spawnCentre + dir * Random.Range(radius, 2 * radius);//在半径到两倍半径范围内生成随机候选点

                //检车候选点是否有效
                if (IsValid(candidate, regionMin, regionMax, cellSize, radius, points, grid, occupiedRegions)) {
                    points.Add(candidate);
                    spawnPoints.Add(candidate);
                    //grid[(int)(candidate.x / cellSize), (int)(candidate.y / cellSize)] = points.Count;
                    int cx = Mathf.Min((int)((candidate.x - regionMin.x) / cellSize), gridWidth - 1);
                    int cy = Mathf.Min((int)((candidate.y - regionMin.y) / cellSize), gridHeight - 1);
                    grid[cx, cy] = points.Count;
                    candidateAccepted = true;
                    break;
                }
            }
            if (!candidateAccepted) {
                spawnPoints.RemoveAt(spawnIndex);
            }
        }
        return points;
    }

    private static void FindSpawnPoints(Vector2 regionMin, Vector2 regionMax, List<OccupiedRegion> occupiedRegions, List<Vector2> spawnPoints, int maxStartPoints = 5) {
        int attempts = 0;
        int maxAttempts = 100;

        while (spawnPoints.Count < maxStartPoints && attempts < maxAttempts) {
            Vector2 startPoint = new Vector2(
                Random.Range(regionMin.x, regionMax.x),
                Random.Range(regionMin.y, regionMax.y));

            if (!IsInOccupiedRegion(startPoint, occupiedRegions)) {
                spawnPoints.Add(startPoint);
                break;
            }
            attempts++;
        }
    }

    //点位是否有效
    private static bool IsValid(Vector2 candidate, Vector2 regionMin, Vector2 regionMax, float cellSize, float radius, List<Vector2> points, int[,] grid, List<OccupiedRegion> occupiedRegions) {
        // 边界检查：使用 regionMin / regionMax
        if (candidate.x < regionMin.x || candidate.x >= regionMax.x ||
            candidate.y < regionMin.y || candidate.y >= regionMax.y) {
            return false;
        }
        if (occupiedRegions != null && IsInOccupiedRegion(candidate, occupiedRegions))
            return false;


        //计算候选点所在网格单元
        //int cellX = (int)(candidate.x / cellSize);
        //int cellY = (int)(candidate.y / cellSize);
        int cellX = (int)((candidate.x - regionMin.x) / cellSize);
        int cellY = (int)((candidate.y - regionMin.y) / cellSize);

        //搜索周围5x5的网格区域
        int searchStartX = Mathf.Max(0, cellX - 2);
        int searchEndX = Mathf.Min(cellX + 2, grid.GetLength(0) - 1);
        int searchStartY = Mathf.Max(0, cellY - 2);
        int searchEndY = Mathf.Min(cellY + 2, grid.GetLength(1) - 1);

        //检车临近点是否太接近候选点
        for (int x = searchStartX; x <= searchEndX; x++) {
            for (int y = searchStartY; y <= searchEndY; y++) {
                int pointIndex = grid[x, y] - 1;
                if (pointIndex != -1) {

                    float sqrDst = (candidate - points[pointIndex]).sqrMagnitude;
                    if (sqrDst < radius * radius) {
                        return false;
                    }
                }
            }
        }
        return true;
    }

    /// <summary>
    /// 检查点是否在已占区域内
    /// </summary>
    private static bool IsInOccupiedRegion(Vector2 point, List<OccupiedRegion> occupiedRegions) {
        foreach (var region in occupiedRegions) {
            // 圆形区域检测
            if (region.isCircular) {
                float sqrDst = (point - region.position).sqrMagnitude;
                if (sqrDst < region.radius * region.radius) {
                    return true;
                }
            }
            // 矩形区域检测
            else {
                if (point.x >= region.position.x - region.size.x / 2 &&
                    point.x <= region.position.x + region.size.x / 2 &&
                    point.y >= region.position.y - region.size.y / 2 &&
                    point.y <= region.position.y + region.size.y / 2) {
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// 已占区域数据结构
    /// </summary>
    public class OccupiedRegion {
        public Vector2 position;    // 区域中心位置
        public bool isCircular;     // 是否为圆形区域
        public float radius;       // 圆形区域半径
        public Vector2 size;       // 矩形区域尺寸
    
        // 圆形区域构造函数
        public OccupiedRegion(Vector2 pos, float rad) {
            position = pos;
            isCircular = true;
            radius = rad;
        }
    
        // 矩形区域构造函数
        public OccupiedRegion(Vector2 pos, Vector2 sz) {
            position = pos;
            isCircular = false;
            size = sz;
        }
    }





//##############################################其他################################################

//网格随机分布
//cell：单元格大小 world：世界大小 density：生成几率 isCenter：生成的点位是否是单元格中点
public static List<Vector2> GenerateGridPoints(Vector2Int cell, Vector2Int world, float density, bool isCenter) {
        List<Vector2> points = new List<Vector2>();
        // 计算可能的群落数量
        int cols = Mathf.FloorToInt(world.x / cell.x);
        int rows = Mathf.FloorToInt(world.y / cell.y);

        for (int x = 0; x < cols; x++) {
            for (int y = 0; y < rows; y++) {
                if (Random.value < density) {
                    Vector2Int spawnPos = new Vector2Int(
                        x * cell.x + (isCenter ? cell.x / 2 : Random.Range(0, cell.x)),
                        y * cell.y + (isCenter ? cell.y / 2 : Random.Range(0, cell.y))
                    );

                    points.Add(spawnPos);
                }
            }
        }
        return points;
    }

    //噪图随机算法
    public static List<Vector2> GenerateNoisePoints(Vector2Int biome, Vector2Int world, NoiseParams noiseParams, int seed) {
        List<Vector2> points = new List<Vector2>();
        // 计算可能的群落数量
        int cols = Mathf.FloorToInt(biome.x / world.x);
        int rows = Mathf.FloorToInt(biome.y / world.y);
        SamplerResult result = NoiseSampler.GenerateTexture(world.x, world.y, noiseParams, seed);
        for (int x = 0; x < cols; x++) {
            for (int y = 0; y < rows; y++) {
                if (result.tex.GetPixel(x, y).r > 0.5) {
                    Vector2Int spawnPos = new Vector2Int(
                        x * biome.x + Random.Range(0, biome.x),
                        y * biome.y + Random.Range(0, biome.y)
                    );
                    points.Add(spawnPos);
                }
            }
        }

        return points;
    }

}
