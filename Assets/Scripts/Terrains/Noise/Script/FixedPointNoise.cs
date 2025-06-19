using UnityEngine;

//生成固定图

[CreateAssetMenu(fileName = "FixedPointNoise", menuName = "NoiseConfig/FixedPoint")]
public class FixedPointNoise : PerlinNoise {
    [Header("Fixed Points")]
    public int fixedPointX1 = 50;    // 第一个固定点X坐标
    public int fixedPointX2 = 150;   // 第二个固定点X坐标
    public float connectionSlope = 0.3f; // 连接坡度控制

    protected override Texture2D GenerateNoise() {

        // ==== 步骤2: 生成基础分形噪声 ====
        float[] baseNoise = GenerateFractalNoise();

        // ==== 步骤3: 强制设置固定点 ====
        EnforceFixedPoints(baseNoise);

        // ==== 步骤4: 应用连接曲线 ====
        ApplyConnectionCurve(baseNoise);

        // ==== 步骤5: 渲染到纹理 ====
        RenderToTexture(baseNoise);
        return _noiseTexture;
    }

    // 生成分形噪声（带高度约束）
    private float[] GenerateFractalNoise() {
        float[] noise = new float[noiseWidth];
        for (int x = 0; x < noiseWidth; x++) {
            // 分形噪声叠加
            float total = 0;
            float freq = frequency;
            float amp = 1;
            float maxAmp = 0;

            for (int i = 0; i < 4; i++) {
                float nx = (x + seed) * freq;
                float ny = seed * freq;
                total += Mathf.PerlinNoise(nx, ny) * amp;
                maxAmp += amp;
                freq *= 2;
                amp *= 0.5f;
            }

            // 归一化并约束高度
            noise[x] = Mathf.Clamp01(total / maxAmp) * (noiseHeight - 10);
        }
        return noise;
    }

    // 强制设置固定顶点
    private void EnforceFixedPoints(float[] baseNoise) {
        // 设置固定点高度为最大值
        baseNoise[fixedPointX1] = noiseHeight - 1;
        baseNoise[fixedPointX2] = noiseHeight - 1;

        // 平滑过渡区域
        SmoothTransition(fixedPointX1 - 10, fixedPointX1 + 10, baseNoise);
        SmoothTransition(fixedPointX2 - 10, fixedPointX2 + 10, baseNoise);
    }

    // 平滑过渡处理
    private void SmoothTransition(int startX, int endX, float[] noise) {
        startX = Mathf.Clamp(startX, 0, noiseWidth - 1);
        endX = Mathf.Clamp(endX, 0, noiseWidth - 1);

        for (int x = startX; x <= endX; x++) {
            float t = (x - startX) / (float)(endX - startX);
            noise[x] = Mathf.Lerp(noise[startX], noise[endX], t);
        }
    }

    // 应用连接曲线（二次贝塞尔）
    private void ApplyConnectionCurve(float[] noise) {
        Vector2 p0 = new Vector2(fixedPointX1, noiseHeight - 1);
        Vector2 p2 = new Vector2(fixedPointX2, noiseHeight - 1);
        Vector2 p1 = new Vector2((p0.x + p2.x) / 2, p0.y - connectionSlope * (p2.x - p0.x));

        for (int x = fixedPointX1; x <= fixedPointX2; x++) {
            float t = (x - p0.x) / (p2.x - p0.x);
            float y = QuadraticBezier(p0.y, p1.y, p2.y, t);
            noise[x] = Mathf.Max(noise[x], y); // 确保连接线高于其他区域
        }
    }

    private float QuadraticBezier(float a, float b, float c, float t) {
        return Mathf.Pow(1 - t, 2) * a + 2 * (1 - t) * t * b + Mathf.Pow(t, 2) * c;
    }

    // 渲染高度数据到纹理
    private void RenderToTexture(float[] heights) {
        for (int x = 0; x < noiseWidth; x++) {
            int yPos = Mathf.FloorToInt(heights[x]);
            for (int y = 0; y < noiseHeight; y++) {
                Color color = (y <= yPos) ? Color.white : Color.black;
                _noiseTexture.SetPixel(x, y, color);
            }
        }
        _noiseTexture.Apply();
    }
}