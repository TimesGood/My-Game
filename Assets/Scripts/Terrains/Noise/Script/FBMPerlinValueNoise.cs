
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

//PerlinNoise ValueNoise�������
[CreateAssetMenu(fileName = "FBMPerlinValueNoise", menuName = "NoiseConfig/new FBMPerlinValueNoise")]
public class FBMPerlinValueNoise : ValueNoise
{
    public int octaves = 4;              // ���β���������Խ�࣬ϸ��Խ�ḻ
    public float persistence = 0.5f;     // ���˥��ϵ����0-1����Խ��߲�ϸ��Խǿ
    public float lacunarity = 2f;        // Ƶ�ʱ���ϵ����>1����Խ��߲�ϸ��Խ�ܼ�
    public float contrastPower = 3f;     // �Աȶ���ǿָ����>1����Խ��ڰ׹���Խ����
    public float warpStrength = 15f;     // ����Ť��ǿ�ȣ�Խ����ͼԽŤ��
    public float warpFrequency = 0.02f;  // ����Ť��Ƶ�ʣ�Խ��Ť��ϸ��ԽС
    public float perlinWeight = 0.5f;    // Perlin����Ȩ�أ�0-1��
    public float valueNoiseWeight = 0.5f;// Value NoiseȨ�أ�0-1��
    public float blendFrequency = 0.1f;  // ���������Ƶ��

    public override void Draw(int x, int y) {
        // ��һ������Ť������
        float warpX = Mathf.PerlinNoise((x + seed) * warpFrequency, (y + seed) * warpFrequency) * warpStrength;
        float warpY = Mathf.PerlinNoise((x + seed + 100) * warpFrequency, (y + seed + 100) * warpFrequency) * warpStrength;

        // ������������
        float perlinNoise = 0f;
        float valueNoise = 0f;
        float freq_tmp = frequency;
        float amplitude = 1f;
        float maxAmplitude = 0f;

        for (int i = 0; i < octaves; i++) {
            // Perlin����
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

        // ��һ���������������
        perlinNoise /= maxAmplitude;
        valueNoise /= maxAmplitude;
        float mixedNoise = (perlinNoise * perlinWeight) + (valueNoise * valueNoiseWeight);

        // �Աȶ���ǿ�Ͷ�ֵ��
        mixedNoise = Mathf.Pow(mixedNoise, contrastPower);
        noiseTexture.SetPixel(x, y, mixedNoise > threshold ? Color.white : Color.black);
    }
    protected override Texture2D GenerateOnGPU() {
        GenerateOnGPUBefore();
        int kernel = shader.FindKernel("CSMain");

        // ���ݲ���
        shader.SetInt("Octaves", octaves);
        shader.SetFloat("Persistence", persistence);
        shader.SetFloat("Lacunarity", lacunarity);
        shader.SetFloat("WarpStrength", warpStrength);
        shader.SetFloat("WarpFrequency", warpFrequency);
        shader.SetFloat("PerlinWeight", perlinWeight);
        shader.SetFloat("ValueNoiseWeight", valueNoiseWeight);
        shader.SetFloat("BlendFrequency", blendFrequency);

        // �����߳���
        int threadGroupsX = Mathf.CeilToInt(_gpuNoiseTex.width / 8f);
        int threadGroupsY = Mathf.CeilToInt(_gpuNoiseTex.height / 8f);
        shader.Dispatch(kernel, threadGroupsX, threadGroupsY, 1);

        return ToTexture2D(_gpuNoiseTex);
    }
}
