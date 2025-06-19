using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//噪音叠加配置
[CreateAssetMenu(fileName = "FBMPerlinNoise", menuName = "NoiseConfig/new FBMPerlinNoise")]
public class FBMPerlinNoise : PerlinNoise {
    public int octaves = 4;              // 分形层数，层数越多，细节越丰富
    [Range(0, 1)]
    public float persistence = 0.5f;     // 振幅衰减系数（0-1），越大高层细节越强
    [Min(2)]
    public float lacunarity = 2f;        // 频率倍增系数（>1），越大高层细节越密集
    public float scale = 1f;     // 坐标扭曲强度，越大噪图越扭曲


    public override void Draw(int x, int y) {
        float noiseValue = 0;
        float freq_tmp = frequency;
        float amplitude = 1;
        float maxAmplitude = 0;

        for (int i = 0; i < octaves; i++) {
            float sampleX = (x - noiseWidth) / scale * freq_tmp + seed;
            float sampleY = (y - noiseHeight) / scale * freq_tmp + seed;
            noiseValue += (Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1) * amplitude;
            maxAmplitude += amplitude;
            amplitude *= persistence;
            freq_tmp *= lacunarity;
        }
        noiseValue /= maxAmplitude; // 归一化到[0,1]

        // 第四步：二值化
        _noiseTexture.SetPixel(x, y, noiseValue > threshold ? Color.white : Color.black);
    }

    protected override Texture2D GenerateOnGPU() {
        GenerateOnGPUBefore();
        int kernel = shader.FindKernel("CSMain");

        // 传递参数
        shader.SetInt("Octaves", octaves);
        shader.SetFloat("Persistence", persistence);
        shader.SetFloat("Lacunarity", lacunarity);
        shader.SetFloat("Scale", scale);
        shader.SetInt("Width", noiseWidth);
        shader.SetInt("Height", noiseHeight);

        // 分配线程组
        int threadGroupsX = Mathf.CeilToInt(_gpuNoiseTex.width / 8f);
        int threadGroupsY = Mathf.CeilToInt(_gpuNoiseTex.height / 8f);
        shader.Dispatch(kernel, threadGroupsX, threadGroupsY, 1);

        return ToTexture2D(_gpuNoiseTex);
    }

}