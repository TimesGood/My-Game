// =============================================
//  MapConfig.cs — 全局地图配置
// =============================================
using UnityEngine;

[CreateAssetMenu(menuName = "MapGen/Map Config")]
public class MapConfig : ScriptableObject {
    [Header("地图尺寸")]
    public int Width = 4200;
    public int Height = 1200;

    [Header("层界")]
    public int SurfaceY = 700;     // 地表基准线

    [Header("种子")]
    public int Seed = 0;

    /// <summary>若 Seed == 0 则随机</summary>
    public int ResolveSeed()
        => Seed == 0 ? System.Environment.TickCount : Seed;
}
