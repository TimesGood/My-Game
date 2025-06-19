using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

//叠加曲线
[CreateAssetMenu(fileName = "FBMPerlinCurve", menuName = "CurveConfig/new FBMPerlinCurve")]

public class FBMPerlinCurve : CurveConfig
{
    [Header("曲线叠加参数")]
    public int octaves = 5;          // 噪声层数
    public float persistence = 0.4f; // 振幅衰减
    public float lacunarity = 2.2f;  // 频率倍增
    public float warpIntensity = 2f; // 坐标扭曲强度，（使曲线呈现不规则感）
    public float peakSharpness = 3f; // 波峰锐化

    public override void Draw(int x) {
 
        // ======== 1. 分形噪声叠加 ========
        float totalHeight = 0;
        float freqTmp = frequency;
        float amplitude = 1;
        float maxHeight = 0;

        for (int i = 0; i < octaves; i++) {
            // ======== 2. 域扭曲坐标 ========
            float warpX = x + Mathf.PerlinNoise(x * 0.1f + seed, seed) * warpIntensity;

            // ======== 3. 采样噪声 ========
            float noise = Mathf.PerlinNoise(
                warpX * freqTmp + seed,  // 添加i*100避免各层重复
                seed
            );

            // ======== 4. 波峰锐化处理 ========
            if (i == octaves - 1) {
                noise = Mathf.Pow(noise, peakSharpness);
            }

            totalHeight += noise * amplitude;
            maxHeight += amplitude;
            amplitude *= persistence;
            freqTmp *= lacunarity;
        }

        // ======== 5. 归一化并计算最终高度 ========
        float normalizedHeight = totalHeight / maxHeight;
        int yPos = Mathf.FloorToInt(normalizedHeight * heightMult + heightAdd);

        // ======== 6. 存储数据 =========
        curveData[x] = yPos;


        // ======== 7. 绘制地形线，运行中就不绘制了，节省性能 =======
#if UNITY_EDITOR
        if (EditorApplication.isPlaying) return;
        for (int y = yPos - 2; y <= yPos + 2; y++) { // 绘制5像素粗的线条
            if (y >= 0 && y < noiseHeight) {
                float gradient = 1 - Mathf.Abs(y - yPos) / 2f; // 渐变透明度
                _noiseTexture.SetPixel(x, y, Color.Lerp(_noiseTexture.GetPixel(x, y), Color.white, gradient));
            }
        }
#endif
    }

    protected override Texture2D GenerateOnGPU() {

        GenerateOnGPUBefore();

        int kernel = shader.FindKernel("CSMain");

        // 传递参数
        shader.SetInt("Octaves", octaves);
        shader.SetFloat("Persistence", persistence);
        shader.SetFloat("Lacunarity", lacunarity);
        shader.SetFloat("WarpIntensity", warpIntensity);
        shader.SetFloat("PeakSharpness", peakSharpness);

        // 分配线程组
        int threadGroupsX = Mathf.CeilToInt(_gpuNoiseTex.width / 8f);
        int threadGroupsY = Mathf.CeilToInt(_gpuNoiseTex.height / 8f);
        shader.Dispatch(kernel, threadGroupsX, threadGroupsY, 1);

        curveBuffer.GetData(curveData);
        Texture2D texture2D = ToTexture2D(_gpuNoiseTex);
        DestroyResource();
        return texture2D;
    }
}
