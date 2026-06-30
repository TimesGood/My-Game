using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ‘Î…˘‘§¿¿
/// </summary>
[CreateAssetMenu(fileName = "New Noise", menuName = "Generator Texture/new Noise")]
public class NoisePreview : TextureBase {
    public NoiseParams noiseParames;


    public override Texture2D Generator() {
        SamplerResult result = NoiseSampler.GenerateTexture(width, height, noiseParames, seed);
        return result.tex;
    }
    
}
