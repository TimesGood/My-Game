using System;
using UnityEngine;

//分形值噪声
[CreateAssetMenu(fileName = "FBMValueNoise", menuName = "NoiseConfig/new FBMValueNoise")]
public class FBMValueNoise : ValueNoise {
    [SerializeField] private FBMNoiseConfig fbm = new FBMNoiseConfig();

    public int Octaves { get => fbm.octaves; set => fbm.octaves = value; }
    public float Persistence { get => fbm.persistence; set => fbm.persistence = value; }
    public float Lacunarity { get => fbm.lacunarity; set => fbm.lacunarity = value; }
    public float Scale { get => fbm.scale; set => fbm.scale = value; }

    protected override bool SupportsCPU => false;

    protected override Texture2D GenerateOnCPU() {
        throw new Exception("分形值噪声未实现CPU生成！");
    }

    protected override void InitShader() {
        base.InitShader();
        int kernel = shader.FindKernel("CSMain");
        fbm.SetShaderParams(shader, kernel, noiseWidth, noiseHeight);
    }
}
