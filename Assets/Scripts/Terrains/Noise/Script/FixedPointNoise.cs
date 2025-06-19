using UnityEngine;

//���ɹ̶�ͼ

[CreateAssetMenu(fileName = "FixedPointNoise", menuName = "NoiseConfig/FixedPoint")]
public class FixedPointNoise : PerlinNoise {
    [Header("Fixed Points")]
    public int fixedPointX1 = 50;    // ��һ���̶���X����
    public int fixedPointX2 = 150;   // �ڶ����̶���X����
    public float connectionSlope = 0.3f; // �����¶ȿ���

    protected override Texture2D GenerateNoise() {

        // ==== ����2: ���ɻ����������� ====
        float[] baseNoise = GenerateFractalNoise();

        // ==== ����3: ǿ�����ù̶��� ====
        EnforceFixedPoints(baseNoise);

        // ==== ����4: Ӧ���������� ====
        ApplyConnectionCurve(baseNoise);

        // ==== ����5: ��Ⱦ������ ====
        RenderToTexture(baseNoise);
        return _noiseTexture;
    }

    // ���ɷ������������߶�Լ����
    private float[] GenerateFractalNoise() {
        float[] noise = new float[noiseWidth];
        for (int x = 0; x < noiseWidth; x++) {
            // ������������
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

            // ��һ����Լ���߶�
            noise[x] = Mathf.Clamp01(total / maxAmp) * (noiseHeight - 10);
        }
        return noise;
    }

    // ǿ�����ù̶�����
    private void EnforceFixedPoints(float[] baseNoise) {
        // ���ù̶���߶�Ϊ���ֵ
        baseNoise[fixedPointX1] = noiseHeight - 1;
        baseNoise[fixedPointX2] = noiseHeight - 1;

        // ƽ����������
        SmoothTransition(fixedPointX1 - 10, fixedPointX1 + 10, baseNoise);
        SmoothTransition(fixedPointX2 - 10, fixedPointX2 + 10, baseNoise);
    }

    // ƽ�����ɴ���
    private void SmoothTransition(int startX, int endX, float[] noise) {
        startX = Mathf.Clamp(startX, 0, noiseWidth - 1);
        endX = Mathf.Clamp(endX, 0, noiseWidth - 1);

        for (int x = startX; x <= endX; x++) {
            float t = (x - startX) / (float)(endX - startX);
            noise[x] = Mathf.Lerp(noise[startX], noise[endX], t);
        }
    }

    // Ӧ���������ߣ����α�������
    private void ApplyConnectionCurve(float[] noise) {
        Vector2 p0 = new Vector2(fixedPointX1, noiseHeight - 1);
        Vector2 p2 = new Vector2(fixedPointX2, noiseHeight - 1);
        Vector2 p1 = new Vector2((p0.x + p2.x) / 2, p0.y - connectionSlope * (p2.x - p0.x));

        for (int x = fixedPointX1; x <= fixedPointX2; x++) {
            float t = (x - p0.x) / (p2.x - p0.x);
            float y = QuadraticBezier(p0.y, p1.y, p2.y, t);
            noise[x] = Mathf.Max(noise[x], y); // ȷ�������߸�����������
        }
    }

    private float QuadraticBezier(float a, float b, float c, float t) {
        return Mathf.Pow(1 - t, 2) * a + 2 * (1 - t) * t * b + Mathf.Pow(t, 2) * c;
    }

    // ��Ⱦ�߶����ݵ�����
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