using UnityEngine;

/// <summary>
/// 噪声类型枚举
/// </summary>
public enum NoiseType
{
    Perlin,  // 柏林噪声
    Value,   // 值噪声
    Worley,  // 细胞噪声
    MixPerlinValue,       // 混合噪声（Perlin + Value）
    MixPerlinWorley,      // 混合噪声（Perlin + Worley）
    MixValueWorley        // 混合噪声（Value + Worley）
}

[System.Serializable]
public class FBMParams {
    [Min(1)]
    public int octaves = 1;              // 叠加层数（1 = 无分形）
    [Range(0, 1)]
    public float persistence = 0.5f;     // 振幅衰减
    [Min(1)]
    public float lacunarity = 2f;        // 频率倍增
}

[System.Serializable]
public class MIXParams {
    [Range(0, 1)]
    public float leftFrequency = 0.02f;  // 左侧混合频率
    [Range(0, 1)]
    public float rightFrequency = 0.02f; // 右侧混合频率
    [Range(0, 1)]
    public float weight = 0.02f;         // 权重
}

/// <summary>
/// 可序列化的噪声参数。
/// </summary>
[System.Serializable]
public class NoiseParams
{
    [Header("噪声类型")]
    public NoiseType type = NoiseType.Perlin;

    [Header("基础参数")]
    [Range(0, 1)]
    public float frequency = 0.02f;      // 频率
    public bool isBinary = false;        // 是否二值化
    [Range(0, 1)]
    public float threshold = 0.5f;       // 阈值（用于二值化判断）
    public float offset;                 // 偏移
    public float scale = 1f;             // 缩放

    [Header("曲线参数")]
    public bool isCurve;
    public int heightMult;               // 
    public int heightAdd;                // 

    [Header("worley噪声专属")]
    public int worleyType;               // worley类型 0 细胞 1 蜂窝
    public bool worleyFlip;              // worley反转

    [Header("FBM 分形参数")]
    public FBMParams fbmParams;          // 分形参数


    [Header("MIX 混合参数")]
    public MIXParams mixParams;          // 混合参数

    [Header("域扭曲参数")]
    public float warpFrequency;
    public float warpStrength;

    public bool useGPU = true;           // 生成方式

    /// <summary>
    /// 判断是否使用 FBM（octaves > 1 时启用分形）
    /// </summary>
    public bool UseFBM => fbmParams != null && fbmParams.octaves > 1;
}
