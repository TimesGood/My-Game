using System;
using UnityEngine;

//混合-细胞柏林噪声
[CreateAssetMenu(fileName = "MIXValueWorleyNoise", menuName = "NoiseConfig/new MIXValueWorleyNoise")]
public class MIXValueWorleyNoise : TextureNoiseBase {

    [SerializeField] private FBMNoiseConfig fbm = new FBMNoiseConfig();

    [Range(0, 1)]
    public float valueFrequency = 0.02f;
    [Range(0, 1)]
    public float worleyFrequency = 0.02f;
    [Range(0, 1)]
    public int worleyType = 0;
    public bool worleyFlip = false;
    [Header("混合权重 值 <-> 细胞")]
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
        shader.SetFloat("ValueFrequency", valueFrequency);
        shader.SetFloat("WorleyFrequency", worleyFrequency);
        shader.SetInt("WorleyReturnType", worleyType);
        shader.SetBool("WorleyFlip", worleyFlip);
        shader.SetFloat("Weight", weight);
        fbm.SetShaderParams(shader, kernel, noiseWidth, noiseHeight);
    }
}
