using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Text.RegularExpressions;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using static BiomeDefinition;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;
using Random = UnityEngine.Random;

// 泊松圆环分配器
[CreateAssetMenu(menuName = "MapGen/Distributors/PoissonDisc Tile")]
public class PoissonDiscDistributor : DistributorBase {

    [Header("群落之间最小间距")]
    public int MinSpacing = 30;

    [Header("最大尝试次数")]
    public int MaxAttempts = 200;

    public override List<BiomeInstance> Distribute(GenerationContext context) {
        var results = new List<BiomeInstance>();
        Debug.Log("采样中-------------");
        
        if (biomeDefinitions == null || biomeDefinitions.Count == 0) return results;
        // 已放置的矩形列表
        var placed = new List<RectInt>();
        foreach (var item in context.claimed) {
            placed.Add(item.Bounds);
        }
        Debug.Log("采样1");
        foreach (var biome in biomeDefinitions) {

            if (biome.suitable == null || biome.suitable.Length == 0) continue;

            // 根据群落最大宽度进行采样
            int maxSize = Mathf.Max(biome.biomeSize.x, biome.biomeSize.y);
            var sizeX = biome.biomeSize.x / 2;
            var sizeY = biome.biomeSize.y / 2;
            List<Vector2> points = new List<Vector2>();
            foreach (var suitable in biome.suitable) {
                // 查找群落适宜区内已放置的群落
                RectInt suitableRect = new RectInt(suitable.SuitableMin, suitable.SuitableMax);
                List<BiomeInstance> clashBiomes = GetBiomeOverlaps(context.claimed, suitableRect);

                // 群落适宜区内点位采样
                List<Vector2> pointList = PoissonDiscSampling.GeneratePoints(maxSize, suitable.SuitableMin, suitable.SuitableMax, null, 50);
                Debug.Log("采样点数：" + pointList.Count);
                if (pointList.Count == 0) continue;
                points.AddRange(pointList);
                
            }
            //RectInt suitableRect = new RectInt(biome.SuitableMin, biome.SuitableMax);
            //List<BiomeInstance> clashBiomes = GetBiomeOverlaps(claimed, suitableRect);
            
            
            if (points.Count == 0) continue;



            // 点位分配
            // 随机点位
            int curRejection = 0;
            int biomeNum = biome.num;
            Debug.Log("分配");
            while (biomeNum > 0 && points.Count > 0) {
                curRejection++;
                // 随机一个点位
                int pointIndex = Random.Range(0, points.Count);
                var point = points[pointIndex];
                Debug.Log(point);
                points.Remove(point);
                Vector2Int bottomLeft = new Vector2Int((int)point.x - sizeX, (int)point.y - sizeY);
                Vector2Int topRight = new Vector2Int((int)point.x + sizeX, (int)point.y + sizeY);
                // 实际矩阵
                RectInt bounds = new RectInt(bottomLeft, topRight);

                // 查看是否符合群落之间的最小间距
                if (!CheckSpacing(bounds, placed)) continue;

                // 查看群落是否与其他群落产生交集
                if (biome.AllowOverlap && CheckIntersect(bounds, placed)) continue;
                Debug.Log("群落: "+biome.BiomeName + "分配到点位: "+ point);

                Commit(biome, bounds, results, context);
                placed.Add(bounds);
                biomeNum--;
            }

        }

        return results;
    }

    /// <summary>检查新矩形与已放置矩形是否满足最小间距</summary>
    private bool CheckSpacing(RectInt candidate, List<RectInt> placed) {
        // 扩展 candidate 的边界做碰撞检测（等效于间距检测）
        var expanded = new RectInt(
            candidate.xMin - MinSpacing,
            candidate.yMin - MinSpacing,
            candidate.width + MinSpacing * 2,
            candidate.height + MinSpacing * 2);

        foreach (var p in placed) {
            if (expanded.Overlaps(p)) return false;
        }
        return true;
    }

    /// <summary>检查新矩形与已放置矩形是否存在交集</summary>
    private bool CheckIntersect(RectInt candidate, List<RectInt> placed) {
        foreach (var p in placed) {
            if (candidate.Overlaps(p)) return true;
        }
        return false;
    }
}
