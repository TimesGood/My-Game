using System;
using UnityEngine;

[CreateAssetMenu(fileName = "FBMWorleyNoise", menuName = "NoiseConfig/new FBMWorleyNoise")]
public class FBMWorleyNoise : TextureNoiseBase {
    [SerializeField] private FBMNoiseConfig fbm = new FBMNoiseConfig();
    [Range(0, 1)]
    public int returnType;
    public bool isFlip;

    public int Octaves { get => fbm.octaves; set => fbm.octaves = value; }
    public float Persistence { get => fbm.persistence; set => fbm.persistence = value; }
    public float Lacunarity { get => fbm.lacunarity; set => fbm.lacunarity = value; }
    public float Scale { get => fbm.scale; set => fbm.scale = value; }

    protected override bool SupportsCPU => false;

    protected override Texture2D GenerateOnCPU() {
        throw new Exception("分形细胞噪声未实现CPU生成！");
    }

    protected override void InitShader() {
        base.InitShader();
        int kernel = shader.FindKernel("CSMain");
        fbm.SetShaderParams(shader, kernel, noiseWidth, noiseHeight);
        shader.SetInt("ReturnType", returnType);
        shader.SetBool("IsFlip", isFlip);
    }
}
