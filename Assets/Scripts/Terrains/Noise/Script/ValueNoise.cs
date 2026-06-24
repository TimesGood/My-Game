using UnityEngine;

//值噪声
[CreateAssetMenu(fileName = "ValueNoise", menuName = "NoiseConfig/new ValueNoise")]
public class ValueNoise : TextureNoiseBase
{
    protected float[,] valueGrid;

    protected override void GenerateBefore() {
        base.GenerateBefore();
        // 预生成Value Noise网格（优化性能）
        int gridSizeX = Mathf.CeilToInt(noiseWidth * frequency) + 1;
        int gridSizeY = Mathf.CeilToInt(noiseHeight * frequency) + 1;
        valueGrid = GenerateValueNoiseGrid(gridSizeX, gridSizeY, 1f);
    }

    protected override Texture2D GenerateOnCPU() {
        for (int x = 0; x < _noiseTexture.width; x++) {
            for (int y = 0; y < _noiseTexture.height; y++) {
                float vSampleX = x * frequency;
                float vSampleY = y * frequency;
                float valueNoise = GenerateValueNoise(vSampleX, vSampleY, valueGrid);
                if (isBinary)
                    _noiseTexture.SetPixel(x, y, valueNoise > threshold ? Color.white : Color.black);
                else
                    _noiseTexture.SetPixel(x, y, valueNoise > threshold ? new Color(valueNoise, valueNoise, valueNoise, valueNoise) : Color.black);
            }
        }
        return _noiseTexture;
    }

    // 生成Value Noise的基础随机网格
    protected float[,] GenerateValueNoiseGrid(int sizeX, int sizeY, float scale) {
        UnityEngine.Random.State originalState = UnityEngine.Random.state;
        UnityEngine.Random.InitState(seed); //设置种子
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
}
