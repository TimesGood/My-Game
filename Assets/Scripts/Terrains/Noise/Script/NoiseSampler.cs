using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 噪声采样工具类，支持 CPU 和 GPU 两种路径。
/// 根据 NoiseParams 内联参数在指定坐标采样噪声值，无需 SO 资产。
/// </summary>
public static class NoiseSampler
{
    // GPU shader 缓存，避免重复 Resources.Load
    private static readonly Dictionary<string, ComputeShader> _shaderCache = new Dictionary<string, ComputeShader>();

    // ==================== 公共 API ====================

    /// <summary>
    /// 在指定坐标采样噪声值 [0,1]。
    /// 自动根据 NoiseParams.UseFBM 决定是否叠加分形。
    /// 仅适用于CPU采样
    /// </summary>
    public static float Sample(int _x, int _y, NoiseParams _p, int _seed) {
        if (_p.UseFBM)
            return SampleFBM(_x, _y, _p, _seed);
        return SampleRaw(_x, _y, _p, _seed);
    }

    /// <summary>
    /// 采样并判断是否超过阈值
    /// 仅适用于CPU采样
    /// </summary>
    public static bool SampleThreshold(int _x, int _y, NoiseParams _p, int _seed) {
        return Sample(_x, _y, _p, _seed) > _p.threshold;
    }

    /// <summary>
    /// 为整个区域生成噪声纹理（输出原始噪声值，不应用阈值）。
    /// 优先使用 GPU（ComputeShader），失败时自动回退到 CPU。
    /// 调用方在读取时自行判断阈值：tex.GetPixel(x,y).r > threshold
    /// </summary>
    public static Texture2D GenerateTexture(int _width, int _height, NoiseParams _p, int _seed) {
        // 尝试 GPU 路径
        if (_p.useGPU) {
            Texture2D gpuResult = GenerateTextureGPU(_width, _height, _p, _seed);
            if (gpuResult != null) return gpuResult;
        }
        // 回退到 CPU 路径
        return GenerateTextureCPU(_width, _height, _p, _seed);
    }

    // ==================== GPU 纹理生成 ====================

    /// <summary>
    /// 通过 ComputeShader 生成噪声纹理。如果 shader 不可用返回 null。
    /// </summary>
    private static Texture2D GenerateTextureGPU(int _width, int _height, NoiseParams _p, int _seed) {
        ComputeShader shader = LoadShader(_p.type, _p.UseFBM);
        if (shader == null) return null;

        // 创建 RenderTexture
        RenderTexture rt = new RenderTexture(_width, _height, 0, RenderTextureFormat.ARGB32) {
            enableRandomWrite = true,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Point
        };
        rt.Create();

        int kernel = shader.FindKernel("CSMain");

        // 设置公共参数
        shader.SetTexture(kernel, "NoiseTexture", rt);
        shader.SetFloat("Frequency", _p.frequency);
        shader.SetFloat("Threshold", _p.threshold); // shader 内部不用阈值（IsBinary=false），但保留绑定
        shader.SetFloat("Offset", _p.offset);
        shader.SetInt("Seed", _seed);
        shader.SetBool("IsBinary", false); // 始终输出原始值

        // FBM 参数
        if (_p.UseFBM) {
            shader.SetInt("Octaves", _p.octaves);
            shader.SetFloat("Persistence", _p.persistence);
            shader.SetFloat("Lacunarity", _p.lacunarity);
            shader.SetFloat("Scale", _p.scale);
            shader.SetInt("Width", _width);
            shader.SetInt("Height", _height);
        }

        // Worley 特有参数
        if (_p.type == NoiseType.Worley) {
            shader.SetInt("ReturnType", _p.worleyType);
            shader.SetBool("IsFlip", _p.worleyFlip);
        }

        // 调度
        int groupsX = Mathf.CeilToInt(_width / 8f);
        int groupsY = Mathf.CeilToInt(_height / 8f);
        shader.Dispatch(kernel, groupsX, groupsY, 1);

        // 读回
        Texture2D tex = new Texture2D(_width, _height, TextureFormat.RGBA32, false) {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Point
        };
        RenderTexture.active = rt;
        tex.ReadPixels(new Rect(0, 0, _width, _height), 0, 0);
        tex.Apply();
        RenderTexture.active = null;

        rt.Release();
        Object.DestroyImmediate(rt);
        return tex;
    }

    // ==================== Shader 加载 ====================

    private static ComputeShader LoadShader(NoiseType _type, bool _useFBM) {
        string name = GetShaderName(_type, _useFBM);
        if (_shaderCache.TryGetValue(name, out var cached)) return cached;

        ComputeShader shader = Resources.Load<ComputeShader>("Shader/" + name);
        _shaderCache[name] = shader; // null 也缓存，避免重复加载失败
        return shader;
    }

    private static string GetShaderName(NoiseType _type, bool _useFBM) {
        switch (_type) {
            case NoiseType.Perlin:
                return _useFBM ? "FBMPerlinNoise" : "PerlinNoise";
            case NoiseType.Value:
                return _useFBM ? "FBMValueNoise" : "ValueNoise";
            case NoiseType.Worley:
                return _useFBM ? "FBMWorleyNoise" : "WorleyNoise";
            default:
                return _useFBM ? "FBMPerlinNoise" : "PerlinNoise";
        }
    }

    // ==================== CPU 纹理生成 ====================

    private static Texture2D GenerateTextureCPU(int _width, int _height, NoiseParams _p, int _seed) {
        Texture2D tex = new Texture2D(_width, _height, TextureFormat.RGBA32, false) {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Point
        };

        for (int x = 0; x < _width; x++) {
            for (int y = 0; y < _height; y++) {
                float v = Sample(x, y, _p, _seed);
                v = v > _p.threshold ? v : 0;
                tex.SetPixel(x, y, new Color(v, v, v, 1));
            }
        }
        tex.Apply();
        return tex;
    }


    // ==================== FBM 分形采样（CPU） ====================

    private static float SampleFBM(int _x, int _y, NoiseParams _p, int _seed) {
        float total = 0;
        float amplitude = 1;
        float maxAmplitude = 0;
        float freq = _p.frequency;

        for (int i = 0; i < _p.octaves; i++) {
            total += SampleRaw(_x, _y, _p, _seed, freq) * amplitude;
            maxAmplitude += amplitude;
            amplitude *= _p.persistence;
            freq *= _p.lacunarity;
        }

        return total / maxAmplitude;
    }

    // ==================== 底层噪声采样（CPU） ====================

    /// <summary>
    /// 单次原始采样（不分形），返回 [0,1]
    /// </summary>
    private static float SampleRaw(int _x, int _y, NoiseParams _p, int _seed, float _freqOverride = -1) {
        float freq = _freqOverride > 0 ? _freqOverride : _p.frequency;
        float sampleX = _x / _p.scale * freq + _seed + _p.offset;
        float sampleY = _y / _p.scale * freq + _seed + _p.offset;

        switch (_p.type) {
            case NoiseType.Perlin:
                return Mathf.PerlinNoise(sampleX, sampleY);
            case NoiseType.Value:
                return ValueNoise(sampleX, sampleY, _seed);
            case NoiseType.Worley:
                return WorleyNoise(sampleX, sampleY);
            default:
                return Mathf.PerlinNoise(sampleX, sampleY);
        }
    }

    // ==================== Value Noise（CPU） ====================

    private static float ValueNoise(float _x, float _y, int _seed) {
        int x0 = Mathf.FloorToInt(_x);
        int y0 = Mathf.FloorToInt(_y);
        int x1 = x0 + 1;
        int y1 = y0 + 1;

        float fracX = _x - x0;
        float fracY = _y - y0;

        fracX = fracX * fracX * (3f - 2f * fracX);
        fracY = fracY * fracY * (3f - 2f * fracY);

        float v00 = Hash2D(x0, y0, _seed);
        float v10 = Hash2D(x1, y0, _seed);
        float v01 = Hash2D(x0, y1, _seed);
        float v11 = Hash2D(x1, y1, _seed);

        float a = Mathf.Lerp(v00, v10, fracX);
        float b = Mathf.Lerp(v01, v11, fracX);
        return Mathf.Lerp(a, b, fracY);
    }

    // ==================== Worley Noise（CPU） ====================

    private static float WorleyNoise(float _x, float _y) {
        int ix = Mathf.FloorToInt(_x);
        int iy = Mathf.FloorToInt(_y);
        float fx = _x - ix;
        float fy = _y - iy;

        float minDist = 10f;

        for (int dy = -1; dy <= 1; dy++) {
            for (int dx = -1; dx <= 1; dx++) {
                Vector2 featurePoint = GetWorleyPoint(ix + dx, iy + dy);
                float dist = Vector2.Distance(new Vector2(fx, fy), featurePoint + new Vector2(dx, dy));
                minDist = Mathf.Min(minDist, dist);
            }
        }

        return Mathf.Clamp01(minDist / 1.5f);
    }

    // ==================== Mix Noise（CPU） ====================

    /// <summary>
    /// 混合噪声：FBM Perlin + FBM Worley，按 weight 加权混合
    /// </summary>
    //private static float MixNoise(int _x, int _y, NoiseParams _p, int _seed) {
    //    // FBM Perlin
    //    float perlinVal = 0;
    //    float amp = 1;
    //    float maxAmp = 0;
    //    float freq = _p.perlinFrequency;
    //    for (int i = 0; i < _p.octaves; i++) {
    //        float sx = _x / _p.scale * freq + _seed + _p.offset;
    //        float sy = _y / _p.scale * freq + _seed + _p.offset;
    //        perlinVal += Mathf.PerlinNoise(sx, sy) * amp;
    //        maxAmp += amp;
    //        amp *= _p.persistence;
    //        freq *= _p.lacunarity;
    //    }
    //    perlinVal /= maxAmp;

    //    // FBM Worley
    //    float worleyVal = 0;
    //    amp = 1;
    //    maxAmp = 0;
    //    freq = _p.worleyFrequency;
    //    for (int i = 0; i < _p.octaves; i++) {
    //        float sx = _x / _p.scale * freq + _seed + _p.offset;
    //        float sy = _y / _p.scale * freq + _seed + _p.offset;
    //        worleyVal += WorleyNoise(sx, sy) * amp;
    //        maxAmp += amp;
    //        amp *= _p.persistence;
    //        freq *= _p.lacunarity;
    //    }
    //    worleyVal /= maxAmp;

    //    // 加权混合
    //    return Mathf.Lerp(perlinVal, worleyVal, _p.mixWeight);
    //}

    // ==================== 哈希函数 ====================

    private static float Hash2D(int _x, int _y, int _seed) {
        int h = _x * 374761393 + _y * 668265263 + _seed * 1274126177;
        h = (h ^ (h >> 13)) * 1274126177;
        h = h ^ (h >> 16);
        return (h & 0x7fffffff) / (float)0x7fffffff;
    }

    private static Vector2 GetWorleyPoint(int _cellX, int _cellY) {
        float px = Hash2D(_cellX, _cellY, 42);
        float py = Hash2D(_cellX, _cellY, 137);
        return new Vector2(px, py);
    }
}
