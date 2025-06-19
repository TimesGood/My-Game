
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

//PerlinNoise ValueNoise混合噪音
[CreateAssetMenu(fileName = "FBMPerlinValueNoise", menuName = "NoiseConfig/new FBMPerlinValueNoise")]
public class FBMPerlinValueNoise : ValueNoise
{
    public int octaves = 4;              // 分形层数，层数越多，细节越丰富
    public float persistence = 0.5f;     // 振幅衰减系数（0-1），越大高层细节越强
    public float lacunarity = 2f;        // 频率倍增系数（>1），越大高层细节越密集
    public float contrastPower = 3f;     // 对比度增强指数（>1），越大黑白过渡越尖锐
    public float warpStrength = 15f;     // 坐标扭曲强度，越大噪图越扭曲
    public float warpFrequency = 0.02f;  // 坐标扭曲频率，越大扭曲细节越小
    public float perlinWeight = 0.5f;    // Perlin噪声权重（0-1）
    public float valueNoiseWeight = 0.5f;// Value Noise权重（0-1）
    public float blendFrequency = 0.1f;  // 混合噪声的频率

    public override void Draw(int x, int y) {
        // 第一步：域扭曲坐标
        float warpX = Mathf.PerlinNoise((x + seed) * warpFrequency, (y + seed) * warpFrequency) * warpStrength;
        float warpY = Mathf.PerlinNoise((x + seed + 100) * warpFrequency, (y + seed + 100) * warpFrequency) * warpStrength;

        // 分形噪声叠加
        float perlinNoise = 0f;
        float valueNoise = 0f;
        float freq_tmp = frequency;
        float amplitude = 1f;
        float maxAmplitude = 0f;

        for (int i = 0; i < octaves; i++) {
            // Perlin噪声
            float pSampleX = (x + warpX) * freq_tmp;
            float pSampleY = (y + warpY) * freq_tmp;
            perlinNoise += Mathf.PerlinNoise(pSampleX, pSampleY) * amplitude;

            // Value Noise
            float vSampleX = x * freq_tmp * blendFrequency;
            float vSampleY = y * freq_tmp * blendFrequency;
            valueNoise += GenerateValueNoise(vSampleX, vSampleY, valueGrid) * amplitude;

            maxAmplitude += amplitude;
            amplitude *= persistence;
            freq_tmp *= lacunarity;
        }

        // 归一化并混合两种噪声
        perlinNoise /= maxAmplitude;
        valueNoise /= maxAmplitude;
        float mixedNoise = (perlinNoise * perlinWeight) + (valueNoise * valueNoiseWeight);

        // 对比度增强和二值化
        mixedNoise = Mathf.Pow(mixedNoise, contrastPower);
        noiseTexture.SetPixel(x, y, mixedNoise > threshold ? Color.white : Color.black);
    }
    protected override Texture2D GenerateOnGPU() {
        GenerateOnGPUBefore();
        int kernel = shader.FindKernel("CSMain");

        // 传递参数
        shader.SetInt("Octaves", octaves);
        shader.SetFloat("Persistence", persistence);
        shader.SetFloat("Lacunarity", lacunarity);
        shader.SetFloat("WarpStrength", warpStrength);
        shader.SetFloat("WarpFrequency", warpFrequency);
        shader.SetFloat("PerlinWeight", perlinWeight);
        shader.SetFloat("ValueNoiseWeight", valueNoiseWeight);
        shader.SetFloat("BlendFrequency", blendFrequency);

        // 分配线程组
        int threadGroupsX = Mathf.CeilToInt(_gpuNoiseTex.width / 8f);
        int threadGroupsY = Mathf.CeilToInt(_gpuNoiseTex.height / 8f);
        shader.Dispatch(kernel, threadGroupsX, threadGroupsY, 1);

        return ToTexture2D(_gpuNoiseTex);
    }
}
