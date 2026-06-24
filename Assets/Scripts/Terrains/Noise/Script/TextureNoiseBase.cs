using UnityEngine;

/// <summary>
/// 支持 GPU 纹理生成的噪声基类。
/// 提供 ComputeShader 调度、RenderTexture 管理等公共基础设施。
/// </summary>
public abstract class TextureNoiseBase : NoiseConfig
{
    [Header("基础噪声参数")]
    [Range(0, 1)]
    public float frequency = 0.02f;      // 频率
    [Range(0, 1)]
    public float threshold = 0.2f;       // 阈值
    public float offset;                 // 偏移

    protected RenderTexture _gpuNoiseTex;
    [field: SerializeField] public ComputeShader shader { get; set; }

    /// <summary>
    /// 子类是否支持 CPU 生成（默认支持）
    /// </summary>
    protected virtual bool SupportsCPU => true;

    /// <summary>
    /// 子类是否支持 GPU 生成（默认支持）
    /// </summary>
    protected virtual bool SupportsGPU => true;

    public override Texture2D InitNoise() {
        if (noiseWidth < 1 || noiseHeight < 1) return null;

        if (openGPU && !SupportsGPU) {
            Debug.LogWarning($"[{GetType().Name}] 不支持 GPU 生成，自动切换到 CPU 模式");
            openGPU = false;
        }
        if (!openGPU && !SupportsCPU) {
            Debug.LogWarning($"[{GetType().Name}] 不支持 CPU 生成，自动切换到 GPU 模式");
            openGPU = true;
        }

        GenerateBefore();
        if (openGPU)
            _noiseTexture = GenerateOnGPU();
        else
            _noiseTexture = GenerateOnCPU();

        _noiseTexture?.Apply();
        GenerateAfter();
        return _noiseTexture;
    }

    /// <summary>
    /// 初始化 RenderTexture（如果尺寸变化则重新创建）
    /// </summary>
    protected void InitializeTexture() {
        if (_gpuNoiseTex != null &&
            _gpuNoiseTex.width == noiseWidth &&
            _gpuNoiseTex.height == noiseHeight)
            return;

        ReleaseGPUResources();

        _gpuNoiseTex = new RenderTexture(noiseWidth, noiseHeight, 0, RenderTextureFormat.ARGB32) {
            enableRandomWrite = true,
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Point
        };
        _gpuNoiseTex.Create();
    }

    /// <summary>
    /// 初始化 Shader 并绑定公共参数。子类可 override 以添加额外参数。
    /// </summary>
    protected virtual void InitShader() {
        if (shader == null)
            shader = Resources.Load<ComputeShader>("Shader/" + this.name);
        if (shader == null)
            throw new System.Exception($"找不到着色器: {this.name}");

        int kernel = shader.FindKernel("CSMain");
        shader.SetTexture(kernel, "NoiseTexture", _gpuNoiseTex);
        shader.SetFloat("Frequency", frequency);
        shader.SetFloat("Threshold", threshold);
        shader.SetFloat("Offset", offset);
        shader.SetInt("Seed", seed);
        shader.SetBool("IsBinary", isBinary);
    }

    /// <summary>
    /// GPU 生成前置流程：初始化纹理 + Shader
    /// </summary>
    protected virtual void GenerateOnGPUBefore() {
        InitializeTexture();
        InitShader();
    }

    protected override Texture2D GenerateOnGPU() {
        GenerateOnGPUBefore();

        int kernel = shader.FindKernel("CSMain");
        int threadGroupsX = Mathf.CeilToInt(_gpuNoiseTex.width / 8f);
        int threadGroupsY = Mathf.CeilToInt(_gpuNoiseTex.height / 8f);
        shader.Dispatch(kernel, threadGroupsX, threadGroupsY, 1);

        return ToTexture2D(_gpuNoiseTex);
    }

    /// <summary>
    /// 确定性释放 GPU 资源
    /// </summary>
    protected void ReleaseGPUResources() {
        if (_gpuNoiseTex != null) {
            _gpuNoiseTex.Release();
            DestroyImmediate(_gpuNoiseTex);
            _gpuNoiseTex = null;
        }
    }

    protected virtual void OnDestroy() {
        ReleaseGPUResources();
    }
}
