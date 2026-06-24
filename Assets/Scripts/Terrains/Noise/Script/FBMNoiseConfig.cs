using UnityEngine;

/// <summary>
/// FBM（分形布朗运动）公共参数。
/// 作为可序列化的内联字段组合到各 FBM 噪声类中，消除字段重复。
/// </summary>
[System.Serializable]
public class FBMNoiseConfig
{
    [Header("FBM 分形参数")]
    public int octaves = 4;              // 叠加层数
    [Range(0, 1)]
    public float persistence = 0.5f;     // 振幅衰减系数
    [Min(1)]
    public float lacunarity = 2f;        // 频率倍增系数
    public float scale = 1f;             // 缩放系数

    /// <summary>
    /// 将 FBM 参数绑定到 ComputeShader
    /// </summary>
    public void SetShaderParams(ComputeShader _shader, int _kernel, int _width, int _height) {
        _shader.SetInt("Octaves", octaves);
        _shader.SetFloat("Persistence", persistence);
        _shader.SetFloat("Lacunarity", lacunarity);
        _shader.SetFloat("Scale", scale);
        _shader.SetInt("Width", _width);
        _shader.SetInt("Height", _height);
    }
}
