using System;
using UnityEngine;

//细胞噪声
[CreateAssetMenu(fileName = "WorleyNoise", menuName = "NoiseConfig/new WorleyNoise")]
public class WorleyNoise : TextureNoiseBase
{
    public int returnType = 0;
    public bool isFlip;

    protected override bool SupportsCPU => false;

    protected override Texture2D GenerateOnCPU() {
        throw new Exception("Worley噪声未实现CPU生成！");
    }

    protected override void InitShader() {
        base.InitShader();
        shader.SetInt("ReturnType", returnType);
        shader.SetBool("IsFlip", isFlip);
    }

    public enum WorleyType {
        CELL,
        ROCK
    }
}
