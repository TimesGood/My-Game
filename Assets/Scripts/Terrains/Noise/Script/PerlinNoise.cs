using System;
using UnityEngine;
using static TreeEditor.TreeEditorHelper;

//��������
[CreateAssetMenu(fileName = "PerlinNoise", menuName = "NoiseConfig/new PerlinNoise")]
public class PerlinNoise : NoiseConfig
{
    [Range(0, 1)]
    public float frequency = 0.02f;      // Ƶ��
    [Range(0, 1)]
    public float threshold = 0.2f;       // ��ֵ
    public float offset;
    protected RenderTexture _gpuNoiseTex;
    [field: SerializeField] protected ComputeShader shader;

    public virtual void Draw(int x, int y) {
        float v = Mathf.PerlinNoise((x + seed) * frequency, (y + seed) * frequency);
        if (v > threshold)
            _noiseTexture.SetPixel(x, y, Color.white);
        else
            _noiseTexture.SetPixel(x, y, Color.black);
    }

    protected override Texture2D GenerateNoise() {

        for (int x = 0; x < _noiseTexture.width; x++) {
            for (int y = 0; y < _noiseTexture.height; y++) {
                Draw(x, y);
            }
        }
        return _noiseTexture;
    }


    //������д����
    private void InitializeTexture() {
        if (_gpuNoiseTex != null) _gpuNoiseTex.Release();

        _gpuNoiseTex = new RenderTexture(noiseWidth, noiseHeight, 0, RenderTextureFormat.ARGB32) {
            enableRandomWrite = true,
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Point
        };
        _gpuNoiseTex.Create();
    }
    private void InitShader() {
        //shader = Resources.Load<ComputeShader>("Shader/" + this.name);
        //if (shader == null) throw new Exception("�Ҳ�����ɫ����" + this.name);
        if (shader == null) throw new Exception("�����ɫ��!");
        int kernel = shader.FindKernel("CSMain");
        // ���ݲ�����������Ҫ ComputeBuffer��
        shader.SetTexture(kernel, "NoiseTexture", _gpuNoiseTex);
        shader.SetFloat("Frequency", frequency);
        shader.SetFloat("Threshold", threshold);
        shader.SetFloat("Offset", offset);
        shader.SetInt("Seed", seed);
        shader.SetBool("IsBinary", isBinary);
    }

    protected virtual void GenerateOnGPUBefore() {
        InitializeTexture();
        InitShader();
    }

    protected override Texture2D GenerateOnGPU() {
        GenerateOnGPUBefore();

        int kernel = shader.FindKernel("CSMain");

        // �����߳���
        int threadGroupsX = Mathf.CeilToInt(_gpuNoiseTex.width / 8f);
        int threadGroupsY = Mathf.CeilToInt(_gpuNoiseTex.height / 8f);
        shader.Dispatch(kernel, threadGroupsX, threadGroupsY, 1);
        return ToTexture2D(_gpuNoiseTex);
    }
}
