using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//������������
[CreateAssetMenu(fileName = "FBMPerlinNoise", menuName = "NoiseConfig/new FBMPerlinNoise")]
public class FBMPerlinNoise : PerlinNoise {
    public int octaves = 4;              // ���β���������Խ�࣬ϸ��Խ�ḻ
    [Range(0, 1)]
    public float persistence = 0.5f;     // ���˥��ϵ����0-1����Խ��߲�ϸ��Խǿ
    [Min(2)]
    public float lacunarity = 2f;        // Ƶ�ʱ���ϵ����>1����Խ��߲�ϸ��Խ�ܼ�
    public float scale = 1f;     // ����Ť��ǿ�ȣ�Խ����ͼԽŤ��


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
        noiseValue /= maxAmplitude; // ��һ����[0,1]

        // ���Ĳ�����ֵ��
        _noiseTexture.SetPixel(x, y, noiseValue > threshold ? Color.white : Color.black);
    }

    protected override Texture2D GenerateOnGPU() {
        GenerateOnGPUBefore();
        int kernel = shader.FindKernel("CSMain");

        // ���ݲ���
        shader.SetInt("Octaves", octaves);
        shader.SetFloat("Persistence", persistence);
        shader.SetFloat("Lacunarity", lacunarity);
        shader.SetFloat("Scale", scale);
        shader.SetInt("Width", noiseWidth);
        shader.SetInt("Height", noiseHeight);

        // �����߳���
        int threadGroupsX = Mathf.CeilToInt(_gpuNoiseTex.width / 8f);
        int threadGroupsY = Mathf.CeilToInt(_gpuNoiseTex.height / 8f);
        shader.Dispatch(kernel, threadGroupsX, threadGroupsY, 1);

        return ToTexture2D(_gpuNoiseTex);
    }

}