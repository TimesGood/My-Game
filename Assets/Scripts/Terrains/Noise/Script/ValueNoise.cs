using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

//值噪音
[CreateAssetMenu(fileName = "ValueNoise", menuName = "NoiseConfig/new ValueNoise")]
public class ValueNoise : PerlinNoise
{
    protected float[,] valueGrid;

    protected override void GenerateBefore() {
        base.GenerateBefore();
        // 预生成Value Noise格点（优化性能）
        int gridSizeX = Mathf.CeilToInt(noiseWidth * frequency) + 1;
        int gridSizeY = Mathf.CeilToInt(noiseHeight * frequency) + 1;
        valueGrid = GenerateValueNoiseGrid(gridSizeX, gridSizeY, 1f);
    }

    public override void Draw(int x, int y) {
        float vSampleX = x * frequency;
        float vSampleY = y * frequency;
        float valueNoise = GenerateValueNoise(vSampleX, vSampleY, valueGrid);


        _noiseTexture.SetPixel(x, y, valueNoise > threshold ? Color.white : Color.black);
    }

    // 生成Value Noise的基础随机格点
    protected float[,] GenerateValueNoiseGrid(int sizeX, int sizeY, float scale) {
        UnityEngine.Random.State originalState = UnityEngine.Random.state;
        UnityEngine.Random.InitState(seed);//随机种子
        float[,] grid = new float[sizeX, sizeY];
        for (int x = 0; x < sizeX; x++) {
            for (int y = 0; y < sizeY; y++) {
                grid[x, y] = UnityEngine.Random.value * scale; // 0-1随机值
            }
        }
        UnityEngine.Random.state = originalState;
        return grid;
    }

    // 双线性插值Value Noise
    protected float GenerateValueNoise(float x, float y, float[,] grid) {
        int x0 = Mathf.FloorToInt(x);
        int y0 = Mathf.FloorToInt(y);
        int x1 = x0 + 1;
        int y1 = y0 + 1;

        // 边界处理
        x0 = Mathf.Clamp(x0, 0, grid.GetLength(0) - 1);
        y0 = Mathf.Clamp(y0, 0, grid.GetLength(1) - 1);
        x1 = Mathf.Clamp(x1, 0, grid.GetLength(0) - 1);
        y1 = Mathf.Clamp(y1, 0, grid.GetLength(1) - 1);

        // 插值
        float fracX = x - x0;
        float fracY = y - y0;

        float v00 = grid[x0, y0];
        float v10 = grid[x1, y0];
        float v01 = grid[x0, y1];
        float v11 = grid[x1, y1];

        // 双线性插值
        float a = Mathf.Lerp(v00, v10, fracX);
        float b = Mathf.Lerp(v01, v11, fracX);
        return Mathf.Lerp(a, b, fracY);
    }


    protected override Texture2D GenerateOnGPU() {
        GenerateOnGPUBefore();
        int kernel = shader.FindKernel("CSMain");

        // 分配线程组
        int threadGroupsX = Mathf.CeilToInt(_gpuNoiseTex.width / 8f);
        int threadGroupsY = Mathf.CeilToInt(_gpuNoiseTex.height / 8f);
        shader.Dispatch(kernel, threadGroupsX, threadGroupsY, 1);

        return ToTexture2D(_gpuNoiseTex);
    }

}
