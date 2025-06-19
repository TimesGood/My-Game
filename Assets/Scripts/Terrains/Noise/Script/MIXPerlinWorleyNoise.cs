using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//����-ϸ���������
[CreateAssetMenu(fileName = "MIXPerlinWorleyNoise", menuName = "NoiseConfig/new MIXPerlinWorleyNoise")]
public class MIXPerlinWorleyNoise : PerlinNoise {

    [Range(0, 1)]
    public float perlinFrequency = 0.02f;
    [Range(0, 1)]
    public float worleyFrequency = 0.02f;
    [Range(0, 1)]
    public float weight = 0.5f;//�������ϸ��
    public float scale = 1f;     // ����Ť��ǿ�ȣ�Խ����ͼԽŤ��

    public int octaves = 4;              // ���β���������Խ�࣬ϸ��Խ�ḻ
    [Range(0, 1)]
    public float persistence = 0.5f;     // ���˥��ϵ����0-1����Խ��߲�ϸ��Խǿ
    [Min(1)]
    public float lacunarity = 2f;        // Ƶ�ʱ���ϵ����>1����Խ��߲�ϸ��Խ�ܼ�


    protected override Texture2D GenerateOnGPU() {
        GenerateOnGPUBefore();
        int kernel = shader.FindKernel("CSMain");

        // ���ݲ���
        shader.SetFloat("PerlinFrequency", perlinFrequency);
        shader.SetFloat("WorleyFrequency", worleyFrequency);
        shader.SetFloat("Weight", weight);
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
