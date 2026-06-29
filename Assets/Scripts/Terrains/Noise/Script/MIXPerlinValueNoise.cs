using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//混合-柏林值噪声
[CreateAssetMenu(fileName = "MIXPerlinValueNoise", menuName = "NoiseConfig/new MIXPerlinValueNoise")]
public class MIXPerlinValueNoise : TextureNoiseBase {
    [SerializeField] private FBMNoiseConfig fbm = new FBMNoiseConfig();

    [Range(0, 1)]
    public float perlinFrequency = 0.02f;
    [Range(0, 1)]
    public float valueFrequency = 0.02f;

    [Header("混合权重 柏林 <-> 值")]
    [Range(0, 1)]
    public float weight = 0.5f; //混合权重

    public int Octaves { get => fbm.octaves; set => fbm.octaves = value; }
    public float Persistence { get => fbm.persistence; set => fbm.persistence = value; }
    public float Lacunarity { get => fbm.lacunarity; set => fbm.lacunarity = value; }
    public float Scale { get => fbm.scale; set => fbm.scale = value; }

    protected override bool SupportsCPU => false;

    protected override Texture2D GenerateOnCPU() {
        throw new Exception("混合噪声未实现CPU生成！");
    }

    protected override void InitShader() {
        base.InitShader();
        int kernel = shader.FindKernel("CSMain");
        shader.SetFloat("LeftFrequency", perlinFrequency);
        shader.SetFloat("RightFrequency", valueFrequency);
        shader.SetFloat("Weight", weight);
        fbm.SetShaderParams(shader, kernel, noiseWidth, noiseHeight);
    }
}
