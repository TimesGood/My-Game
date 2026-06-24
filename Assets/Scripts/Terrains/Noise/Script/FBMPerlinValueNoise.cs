using UnityEngine;

//PerlinNoise ValueNoise混合噪声
[CreateAssetMenu(fileName = "FBMPerlinValueNoise", menuName = "NoiseConfig/new FBMPerlinValueNoise")]
public class FBMPerlinValueNoise : ValueNoise
{
    [SerializeField] private FBMNoiseConfig fbm = new FBMNoiseConfig();

    public int Octaves { get => fbm.octaves; set => fbm.octaves = value; }
    public float Persistence { get => fbm.persistence; set => fbm.persistence = value; }
    public float Lacunarity { get => fbm.lacunarity; set => fbm.lacunarity = value; }

    public float contrastPower = 3f;     // 对比度增强指数
    public float warpStrength = 15f;     // 扭曲强度
    public float warpFrequency = 0.02f;  // 扭曲频率
    public float perlinWeight = 0.5f;    // Perlin噪声权重
    public float valueNoiseWeight = 0.5f;// Value Noise权重
    public float blendFrequency = 0.1f;  // 混合噪声频率


    protected override Texture2D GenerateOnCPU() {
        for (int x = 0; x < _noiseTexture.width; x++) {
            for (int y = 0; y < _noiseTexture.height; y++) {
                // 第一步：计算扭曲坐标
                float warpX = Mathf.PerlinNoise((x + seed) * warpFrequency, (y + seed) * warpFrequency) * warpStrength;
                float warpY = Mathf.PerlinNoise((x + seed + 100) * warpFrequency, (y + seed + 100) * warpFrequency) * warpStrength;

                // 计算两种噪声
                float perlinNoise = 0f;
                float valueNoise = 0f;
                float freq_tmp = frequency;
                float amplitude = 1f;
                float maxAmplitude = 0f;

                for (int i = 0; i < fbm.octaves; i++) {
                    // Perlin噪声
                    float pSampleX = (x + warpX) * freq_tmp;
                    float pSampleY = (y + warpY) * freq_tmp;
                    perlinNoise += Mathf.PerlinNoise(pSampleX, pSampleY) * amplitude;

                    // Value Noise
                    float vSampleX = x * freq_tmp * blendFrequency;
                    float vSampleY = y * freq_tmp * blendFrequency;
                    valueNoise += GenerateValueNoise(vSampleX, vSampleY, valueGrid) * amplitude;

                    maxAmplitude += amplitude;
                    amplitude *= fbm.persistence;
                    freq_tmp *= fbm.lacunarity;
                }

                // 归一化和混合权重
                perlinNoise /= maxAmplitude;
                valueNoise /= maxAmplitude;
                float mixedNoise = (perlinNoise * perlinWeight) + (valueNoise * valueNoiseWeight);

                // 对比度增强和二值化
                mixedNoise = Mathf.Pow(mixedNoise, contrastPower);
                if (isBinary)
                    noiseTexture.SetPixel(x, y, mixedNoise > threshold ? Color.white : Color.black);
                else
                    noiseTexture.SetPixel(x, y, mixedNoise > threshold ? new Color(mixedNoise, mixedNoise, mixedNoise, 1) : Color.black);
            }
        }
        return noiseTexture;
    }

    protected override void InitShader() {
        base.InitShader();
        int kernel = shader.FindKernel("CSMain");
        shader.SetInt("Octaves", fbm.octaves);
        shader.SetFloat("Persistence", fbm.persistence);
        shader.SetFloat("Lacunarity", fbm.lacunarity);
        shader.SetFloat("WarpStrength", warpStrength);
        shader.SetFloat("WarpFrequency", warpFrequency);
        shader.SetFloat("PerlinWeight", perlinWeight);
        shader.SetFloat("ValueNoiseWeight", valueNoiseWeight);
        shader.SetFloat("BlendFrequency", blendFrequency);
    }
}
