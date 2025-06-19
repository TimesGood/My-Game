using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

//����ͼ
public abstract class CurveConfig : NoiseConfig
{
    [Header("���߻�������")]
    public float frequency;              // Ƶ��
    public float heightMult = 50f;       // �޸�
    public float heightAdd =10f;         // ����
    public float offset;                 // ��ͼƫ�ƣ�����ͬ�����������һ�µ���ͼ��
    protected int[] curveData;         // ��������
    protected RenderTexture _gpuNoiseTex;
    [field: SerializeField] protected ComputeShader shader;
    protected ComputeBuffer curveBuffer;


    protected override void GenerateBefore() {
        base.GenerateBefore();
        // ���ô���ɫ���ر���
        Color[] colors = new Color[noiseWidth * noiseHeight];
        for (int i = 0; i < colors.Length; i++) {
            colors[i] = Color.black;
        }
        _noiseTexture.SetPixels(colors);
        _noiseTexture.Apply();

        // ��ʼ����������
        curveData = new int[noiseWidth];
    }

    protected override Texture2D GenerateNoise() {
        for (int x = 0; x < noiseWidth; x++) {
            Draw(x);
        }
        return _noiseTexture;
    }

    public virtual void Draw(int x) { }

    //��ȡָ��X����������
    public int GetHeight(int x) {

        return curveData[x];
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
        if (shader == null) throw new Exception("�����ɫ��!");
        int kernel = shader.FindKernel("CSMain");
        // ���ݲ�����������Ҫ ComputeBuffer��
        shader.SetTexture(kernel, "NoiseTexture", _gpuNoiseTex);
        shader.SetFloat("Frequency", frequency);
        shader.SetFloat("HeightMult", heightMult);
        shader.SetFloat("HeightAdd", heightAdd);
        shader.SetFloat("Offset", offset);
        shader.SetInt("Seed", seed);
        shader.SetBool("IsBinary", isBinary);
        curveBuffer = new ComputeBuffer(noiseWidth, sizeof(int));
        shader.SetBuffer(kernel, "CurveData", curveBuffer);
    }

    protected virtual void GenerateOnGPUBefore() {
        InitializeTexture();
        InitShader();
    }
    protected void DestroyResource() {
        curveBuffer?.Release();
        if (_gpuNoiseTex != null) {
            _gpuNoiseTex.Release();
        }
    }
}
