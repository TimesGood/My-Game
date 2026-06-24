using UnityEngine;

//多段的地形曲线
[CreateAssetMenu(fileName = "FBMPerlinCurve", menuName = "CurveConfig/new FBMPerlinCurve")]
public class FBMPerlinCurve : CurveConfig
{
    [SerializeField] private FBMNoiseConfig fbm = new FBMNoiseConfig();

    public int Octaves { get => fbm.octaves; set => fbm.octaves = value; }
    public float Persistence { get => fbm.persistence; set => fbm.persistence = value; }
    public float Lacunarity { get => fbm.lacunarity; set => fbm.lacunarity = value; }

    public float warpIntensity = 2f; // 扭曲强度
    public float peakSharpness = 3f; // 峰值锐度


    protected override Texture2D GenerateOnCPU() {
        for (int x = 0; x < noiseWidth; x++) {
            // ======== 1. 计算分形噪声 ========
            float totalHeight = 0;
            float freqTmp = frequency;
            float amplitude = 1;
            float maxHeight = 0;

            for (int i = 0; i < fbm.octaves; i++) {
                // ======== 2. 扭曲坐标 ========
                float warpX = x + Mathf.PerlinNoise(x * 0.1f + seed, seed) * warpIntensity;

                // ======== 3. 分形叠加 ========
                float noise = Mathf.PerlinNoise(
                    warpX * freqTmp + seed,
                    seed
                );

                // ======== 4. 峰值锐化处理 ========
                if (i == fbm.octaves - 1) {
                    noise = Mathf.Pow(noise, peakSharpness);
                }

                totalHeight += noise * amplitude;
                maxHeight += amplitude;
                amplitude *= fbm.persistence;
                freqTmp *= fbm.lacunarity;
            }

            // ======== 5. 归一化并计算最终高度 ========
            float normalizedHeight = totalHeight / maxHeight;
            int yPos = Mathf.FloorToInt(normalizedHeight * heightMult + heightAdd);

            // ======== 6. 存储数据 ========
            curveData[x] = yPos;

            // ======== 7. 绘制曲线（可选） =======
#if UNITY_EDITOR
            if (UnityEditor.EditorApplication.isPlaying) return noiseTexture;
            for (int y = yPos - 2; y <= yPos + 2; y++) {
                if (y >= 0 && y < noiseHeight) {
                    float gradient = 1 - Mathf.Abs(y - yPos) / 2f;
                    noiseTexture.SetPixel(x, y, Color.Lerp(noiseTexture.GetPixel(x, y), Color.white, gradient));
                }
            }
#endif
        }
        return noiseTexture;
    }

    protected override void InitShader() {
        base.InitShader();
        int kernel = shader.FindKernel("CSMain");
        shader.SetInt("Octaves", fbm.octaves);
        shader.SetFloat("Persistence", fbm.persistence);
        shader.SetFloat("Lacunarity", fbm.lacunarity);
        shader.SetFloat("WarpIntensity", warpIntensity);
        shader.SetFloat("PeakSharpness", peakSharpness);
    }

    protected override Texture2D GenerateOnGPU() {

        GenerateOnGPUBefore();
        int kernel = shader.FindKernel("CSMain");
        // 分配线程组
        int threadGroupsX = Mathf.CeilToInt(_gpuNoiseTex.width / 8f);
        int threadGroupsY = Mathf.CeilToInt(_gpuNoiseTex.height / 8f);
        shader.Dispatch(kernel, threadGroupsX, threadGroupsY, 1);

        curveBuffer.GetData(curveData);
        Texture2D texture2D = ToTexture2D(_gpuNoiseTex);
        DestroyResource();
        return texture2D;
    }
}
