using UnityEngine;

//柏林噪声
[CreateAssetMenu(fileName = "PerlinNoise", menuName = "NoiseConfig/new PerlinNoise")]
public class PerlinNoise : TextureNoiseBase
{
    protected override Texture2D GenerateOnCPU() {
        for (int x = 0; x < _noiseTexture.width; x++) {
            for (int y = 0; y < _noiseTexture.height; y++) {
                float v = Mathf.PerlinNoise((x + seed) * frequency, (y + seed) * frequency);
                if (isBinary)
                    _noiseTexture.SetPixel(x, y, v > threshold ? Color.white : Color.black);
                else
                    _noiseTexture.SetPixel(x, y, v > threshold ? new Color(v, v, v, v) : Color.black);
            }
        }
        return _noiseTexture;
    }
}
