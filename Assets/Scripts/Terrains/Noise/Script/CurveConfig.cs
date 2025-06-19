using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

//曲线图
public abstract class CurveConfig : NoiseConfig
{
    [Header("曲线基础参数")]
    public float frequency;              // 频率
    public float heightMult = 50f;       // 限高
    public float heightAdd =10f;         // 补充
    public float offset;                 // 噪图偏移（避免同参数情况生成一致的噪图）
    protected int[] curveData;         // 曲线数据
    protected RenderTexture _gpuNoiseTex;
    [field: SerializeField] protected ComputeShader shader;
    protected ComputeBuffer curveBuffer;


    protected override void GenerateBefore() {
        base.GenerateBefore();
        // 设置纯黑色像素背景
        Color[] colors = new Color[noiseWidth * noiseHeight];
        for (int i = 0; i < colors.Length; i++) {
            colors[i] = Color.black;
        }
        _noiseTexture.SetPixels(colors);
        _noiseTexture.Apply();

        // 初始化曲线数组
        curveData = new int[noiseWidth];
    }

    protected override Texture2D GenerateNoise() {
        for (int x = 0; x < noiseWidth; x++) {
            Draw(x);
        }
        return _noiseTexture;
    }

    public virtual void Draw(int x) { }

    //获取指定X轴曲线数据
    public int GetHeight(int x) {

        return curveData[x];
    }


    //创建可写纹理
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
        if (shader == null) throw new Exception("请绑定着色器!");
        int kernel = shader.FindKernel("CSMain");
        // 传递参数（不再需要 ComputeBuffer）
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
