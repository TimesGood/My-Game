using UnityEngine;

//分形柏林噪声
[CreateAssetMenu(fileName = "FBMPerlinNoise", menuName = "NoiseConfig/new FBMPerlinNoise")]
public class FBMPerlinNoise : PerlinNoise {
    [SerializeField] private FBMNoiseConfig fbm = new FBMNoiseConfig();

    // 公开 FBM 参数访问器
    public int Octaves { get => fbm.octaves; set => fbm.octaves = value; }
    public float Persistence { get => fbm.persistence; set => fbm.persistence = value; }
    public float Lacunarity { get => fbm.lacunarity; set => fbm.lacunarity = value; }
    public float Scale { get => fbm.scale; set => fbm.scale = value; }

    protected override Texture2D GenerateOnCPU() {
        for (int x = 0; x < _noiseTexture.width; x++) {
            for (int y = 0; y < _noiseTexture.height; y++) {
                float noiseValue = 0;
                float freq_tmp = frequency;
                float amplitude = 1;
                float maxAmplitude = 0;

                for (int i = 0; i < fbm.octaves; i++) {
                    float sampleX = x / fbm.scale * freq_tmp + seed;
                    float sampleY = y / fbm.scale * freq_tmp + seed;
                    noiseValue += (Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1) * amplitude;
                    maxAmplitude += amplitude;
                    amplitude *= fbm.persistence;
                    freq_tmp *= fbm.lacunarity;
                }
                noiseValue /= maxAmplitude; // 归一化到[0,1]

                // 根据参数设置像素值
                if (isBinary)
                    _noiseTexture.SetPixel(x, y, noiseValue > threshold ? Color.white : Color.black);
                else
                    _noiseTexture.SetPixel(x, y, noiseValue > threshold ? new Color(noiseValue, noiseValue, noiseValue, 1) : Color.black);
            }
        }
        return _noiseTexture;
    }

    protected override void InitShader() {
        base.InitShader();
        int kernel = shader.FindKernel("CSMain");
        fbm.SetShaderParams(shader, kernel, noiseWidth, noiseHeight);
    }
}
