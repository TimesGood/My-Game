using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//ϸ��״����ͼ
[CreateAssetMenu(fileName = "WorleyNoise", menuName = "NoiseConfig/new WorleyNoise")]
public class WorleyNoise : PerlinNoise
{

    public int returnType = 0;

    protected override Texture2D GenerateOnGPU() {
        GenerateOnGPUBefore();
        int kernel = shader.FindKernel("CSMain");

        shader.SetInt("ReturnType", returnType);

        // �����߳���
        int threadGroupsX = Mathf.CeilToInt(_gpuNoiseTex.width / 8f);
        int threadGroupsY = Mathf.CeilToInt(_gpuNoiseTex.height / 8f);
        shader.Dispatch(kernel, threadGroupsX, threadGroupsY, 1);

        return ToTexture2D(_gpuNoiseTex);
    }
}
