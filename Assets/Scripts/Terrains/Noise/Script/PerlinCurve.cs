using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//����ͼ
[CreateAssetMenu(fileName = "PerlinCurve", menuName = "CurveConfig/new PerlinCurve")]
public class PerlinCurve : CurveConfig {
    public override void Draw(int x) {
        float y = Mathf.PerlinNoise((x + seed) * frequency, seed * frequency) * heightMult + heightAdd;
        _noiseTexture.SetPixel(x, (int)y, Color.white);
        curveData[x] = Mathf.FloorToInt(y);
    }


    protected override Texture2D GenerateOnGPU() {
        GenerateOnGPUBefore();

        int kernel = shader.FindKernel("CSMain");

        // �����߳���
        int threadGroupsX = Mathf.CeilToInt(_gpuNoiseTex.width / 8f);
        int threadGroupsY = Mathf.CeilToInt(_gpuNoiseTex.height / 8f);
        shader.Dispatch(kernel, threadGroupsX, threadGroupsY, 1);

        curveBuffer.GetData(curveData);
        Texture2D texture2D = ToTexture2D(_gpuNoiseTex);
        DestroyResource();
        return texture2D;
    }
}
    
