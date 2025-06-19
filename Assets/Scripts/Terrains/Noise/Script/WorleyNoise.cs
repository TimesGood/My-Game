using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//细胞状噪音图
[CreateAssetMenu(fileName = "WorleyNoise", menuName = "NoiseConfig/new WorleyNoise")]
public class WorleyNoise : PerlinNoise
{

    public int returnType = 0;

    protected override Texture2D GenerateOnGPU() {
        GenerateOnGPUBefore();
        int kernel = shader.FindKernel("CSMain");

        shader.SetInt("ReturnType", returnType);

        // 分配线程组
        int threadGroupsX = Mathf.CeilToInt(_gpuNoiseTex.width / 8f);
        int threadGroupsY = Mathf.CeilToInt(_gpuNoiseTex.height / 8f);
        shader.Dispatch(kernel, threadGroupsX, threadGroupsY, 1);

        return ToTexture2D(_gpuNoiseTex);
    }
}
