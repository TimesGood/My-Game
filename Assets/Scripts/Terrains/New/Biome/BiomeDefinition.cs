// =============================================
//  BiomeDefinition.cs — 群落定义
// =============================================
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "MapGen/Biome Definition")]
public class BiomeDefinition : ScriptableObject {

    [Header("身份")]
    public string biomeId;
    public string BiomeName = "New Biome";

    [Header("尺寸大小")]
    public Vector2Int biomeSize = new(0, 300);

    [Header("适宜生成区间")]
    public Suitable[] suitable;

    [Header("优先级 (高优先级先分配)")]
    public int Priority = 0;

    [Header("是否允许与其他群落重叠")]
    public bool AllowOverlap = false;

    [Header("生成器 ID (对应 BiomeGeneratorBase.Id)")]
    public string GeneratorId = "Default";

    public int num;

    // -------- 辅助方法 --------

    /// <summary>归一化 X → 实际像素 X 区间</summary>
    //public Vector2Int GetActualXRange(int mapWidth) {
    //    int min = Mathf.FloorToInt(SuitableXNormalized.x * mapWidth);
    //    int max = Mathf.CeilToInt(SuitableXNormalized.y * mapWidth);
    //    return new Vector2Int(min, max);
    //}

    /// <summary>归一化 X → 实际像素 X 区间</summary>
    //public Vector2Int GetActualYRange(int mapHeight) {
    //    int min = Mathf.FloorToInt(SuitableYNormalized.x * mapHeight);
    //    int max = Mathf.CeilToInt(SuitableYNormalized.y * mapHeight);
    //    return new Vector2Int(min, max);
    //}

    /// <summary>检查给定 X 区间是否与本群落适宜区间重叠</summary>
    //public bool OverlapsX(Vector2Int range)
    //    => SuitableX.x >= range.x && SuitableX.y <= range.y;

    ///// <summary>检查给定 Y 区间是否与本群落适宜区间重叠</summary>
    //public bool OverlapsY(Vector2Int range)
    //    => SuitableY.x >= range.x && SuitableY.y <= range.y;

    ///// <summary>总体面积</summary>
    //public int GetArea() {
    //    return biomeSize.x * biomeSize.y;
    //}

    [Serializable]
    public class Suitable {
        public Vector2Int SuitableMin = new(0, 0);
        public Vector2Int SuitableMax = new(0, 0);
    }
}
