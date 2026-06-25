using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "TestNewNoise", menuName = "NoiseConfig/new TestNewNoise")]
public class TestNewNoise : TextureNoiseBase {

    [Header("工具参数")]
    public NoiseParams noiseParams = new NoiseParams();


    protected override Texture2D GenerateOnGPU() {

        return NoiseSampler.GenerateTexture(noiseWidth, noiseHeight, noiseParams, seed);
    }

}
