using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

//��������
[CreateAssetMenu(fileName = "FBMPerlinCurve", menuName = "CurveConfig/new FBMPerlinCurve")]

public class FBMPerlinCurve : CurveConfig
{
    [Header("���ߵ��Ӳ���")]
    public int octaves = 5;          // ��������
    public float persistence = 0.4f; // ���˥��
    public float lacunarity = 2.2f;  // Ƶ�ʱ���
    public float warpIntensity = 2f; // ����Ť��ǿ�ȣ���ʹ���߳��ֲ�����У�
    public float peakSharpness = 3f; // ������

    public override void Draw(int x) {
 
        // ======== 1. ������������ ========
        float totalHeight = 0;
        float freqTmp = frequency;
        float amplitude = 1;
        float maxHeight = 0;

        for (int i = 0; i < octaves; i++) {
            // ======== 2. ��Ť������ ========
            float warpX = x + Mathf.PerlinNoise(x * 0.1f + seed, seed) * warpIntensity;

            // ======== 3. �������� ========
            float noise = Mathf.PerlinNoise(
                warpX * freqTmp + seed,  // ���i*100��������ظ�
                seed
            );

            // ======== 4. �����񻯴��� ========
            if (i == octaves - 1) {
                noise = Mathf.Pow(noise, peakSharpness);
            }

            totalHeight += noise * amplitude;
            maxHeight += amplitude;
            amplitude *= persistence;
            freqTmp *= lacunarity;
        }

        // ======== 5. ��һ�����������ո߶� ========
        float normalizedHeight = totalHeight / maxHeight;
        int yPos = Mathf.FloorToInt(normalizedHeight * heightMult + heightAdd);

        // ======== 6. �洢���� =========
        curveData[x] = yPos;


        // ======== 7. ���Ƶ����ߣ������оͲ������ˣ���ʡ���� =======
#if UNITY_EDITOR
        if (EditorApplication.isPlaying) return;
        for (int y = yPos - 2; y <= yPos + 2; y++) { // ����5���شֵ�����
            if (y >= 0 && y < noiseHeight) {
                float gradient = 1 - Mathf.Abs(y - yPos) / 2f; // ����͸����
                _noiseTexture.SetPixel(x, y, Color.Lerp(_noiseTexture.GetPixel(x, y), Color.white, gradient));
            }
        }
#endif
    }

    protected override Texture2D GenerateOnGPU() {

        GenerateOnGPUBefore();

        int kernel = shader.FindKernel("CSMain");

        // ���ݲ���
        shader.SetInt("Octaves", octaves);
        shader.SetFloat("Persistence", persistence);
        shader.SetFloat("Lacunarity", lacunarity);
        shader.SetFloat("WarpIntensity", warpIntensity);
        shader.SetFloat("PeakSharpness", peakSharpness);

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
