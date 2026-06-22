// =============================================
//  BiomeInstance.cs — 分配后的群落运行时实例
// =============================================
using UnityEngine;

public class BiomeInstance {
    public BiomeDefinition Def;
    public RectInt Bounds;   // 分配器产出的实际矩形
    public int Seed;     // 该实例专属种子

    // 便捷访问
    public int X => Bounds.xMin;
    public int Y => Bounds.yMin;
    public int Right => Bounds.xMax;
    public int Top => Bounds.yMax;
    public int Width => Bounds.width;
    public int Height => Bounds.height;
}
