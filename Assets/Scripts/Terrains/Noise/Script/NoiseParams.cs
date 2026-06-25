using UnityEngine;

/// <summary>
/// 噪声类型枚举
/// </summary>
public enum NoiseType
{
    Perlin,  // 柏林噪声
    Value,   // 值噪声
    Worley   // 细胞噪声
}

/// <summary>
/// 可序列化的噪声参数。
/// 作为内联字段组合到矿石/树木等配置中，替代 NoiseConfig SO 引用。
/// </summary>
[System.Serializable]
public class NoiseParams
{
    [Header("噪声类型")]
    public NoiseType type = NoiseType.Perlin;

    [Header("基础参数")]
    [Range(0, 1)]
    public float frequency = 0.02f;      // 频率
    [Range(0, 1)]
    public float threshold = 0.5f;       // 阈值（用于二值化判断）
    public float offset;                 // 偏移

    [Header("FBM 分形参数")]
    public int octaves = 4;              // 叠加层数（1 = 无分形）
    [Range(0, 1)]
    public float persistence = 0.5f;     // 振幅衰减
    [Min(1)]
    public float lacunarity = 2f;        // 频率倍增
    public float scale = 1f;             // 缩放

    public bool useGPU = true;           // 生成方式

    /// <summary>
    /// 判断是否使用 FBM（octaves > 1 时启用分形）
    /// </summary>
    public bool UseFBM => octaves > 1;
}
