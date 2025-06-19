#ifndef NOISE_COMMON_INCLUDED
#define NOISE_COMMON_INCLUDED
#include "HashLib.hlsl"

float3 mod289(float3 x) { return x - floor(x * (1.0 / 289.0)) * 289.0; }
float2 mod289(float2 x) { return x - floor(x * (1.0 / 289.0)) * 289.0; }
float3 permute(float3 x) { return mod289(((x * 34.0) + 1.0) * x); }
float2 permute(float2 x) { return mod289(((x * 34.0) + 1.0) * x); }

//--------------------------
// Perlin梯度噪声
//--------------------------

float2 random(float2 st){
    st = float2( dot(st,float2(127.1,311.7)),
              dot(st,float2(269.5,183.3)) );
    return -1.0 + 2.0*frac(sin(st)*43758.5453123);
}

float noise(float2 st) {
    float2 i = floor(st);
    float2 f = frac(st);

    float2 u = f*f*(3.0-2.0*f);

    return lerp( lerp( dot( random(i + float2(0.0,0.0) ), f - float2(0.0,0.0) ),
                       dot( random(i + float2(1.0,0.0) ), f - float2(1.0,0.0) ), u.x),
                 lerp( dot( random(i + float2(0.0,1.0) ), f - float2(0.0,1.0) ),
                       dot( random(i + float2(1.0,1.0) ), f - float2(1.0,1.0) ), u.x), u.y);

}

//--------------------------
// Simplex噪声
//--------------------------
float snoise(float2 v)
{
    const float4 C = float4(0.211324865405187, 0.366025403784439, -0.577350269189626, 0.024390243902439);
    float2 i = floor(v + dot(v, C.yy));
    float2 x0 = v - i + dot(i, C.xx);
    float2 i1 = (x0.x > x0.y) ? float2(1.0, 0.0) : float2(0.0, 1.0);
    float4 x12 = x0.xyxy + C.xxzz;
    x12.xy -= i1;
    i = mod289(i);
    float3 p = permute(permute(i.y + float3(0.0, i1.y, 1.0)) + i.x + float3(0.0, i1.x, 1.0));
    float3 m = max(0.5 - float3(dot(x0, x0), dot(x12.xy, x12.xy), dot(x12.zw, x12.zw)), 0.0);
    m = m * m;
    m = m * m;
    float3 x = 2.0 * frac(p * C.www) - 1.0;
    float3 h = abs(x) - 0.5;
    float3 ox = floor(x + 0.5);
    float3 a0 = x - ox;
    m *= 1.79284291400159 - 0.85373472095314 * (a0 * a0 + h * h);
    float3 g;
    g.x = a0.x * x0.x + h.x * x0.y;
    g.yz = a0.yz * x12.xz + h.yz * x12.yw;
    //return 130.0 * dot(m, g);
    //限制范围（0-1）
    return ((130.0 * dot(m, g)) + 1) * 0.5;
}

//--------------------------
// Value噪声
//--------------------------
float valueNoise(float2 uv)
{
    float2 intPos = floor(uv); //uv晶格化, 取 uv 整数值，相当于晶格id
    float2 fracPos = frac(uv); //取 uv 小数值，相当于晶格内局部坐标，取值区间：(0,1)

    //二维插值权重，一个类似smoothStep的函数，叫Hermit插值函数，也叫S曲线：S(x) = -2 x^3 + 3 x^2
    //利用Hermit插值特性：可以在保证函数输出的基础上保证插值函数的导数在插值点上为0，这样就提供了平滑性
    float2 u = fracPos * fracPos * (3.0 - 2.0 * fracPos); 

    //四方取点，由于intPos是固定的，所以栅格化了（同一晶格内四点值相同，只是小数部分不同拿来插值）
    float va = hash2to1( intPos + float2(0.0, 0.0) );  //hash2to1 二维输入，映射到1维输出
    float vb = hash2to1( intPos + float2(1.0, 0.0) );
    float vc = hash2to1( intPos + float2(0.0, 1.0) );
    float vd = hash2to1( intPos + float2(1.0, 1.0) );

    //lerp的展开形式，完全可以用lerp(a,b,c)嵌套实现
    float k0 = va;
    float k1 = vb - va;
    float k2 = vc - va;
    float k4 = va - vb - vc + vd;
    float value = k0 + k1 * u.x + k2 * u.y + k4 * u.x * u.y;

    return value;
}

//--------------------------
// Worley噪声（细胞噪声）
// returnType：返回类型
//--------------------------
float worleyNoise(float2 uv, int returnType)
{

    float F1 = 1e5;
    float F2 = 1e5;
    float2 intPos = floor(uv);
    float2 fracPos = frac(uv);

    for(int x = -1; x <= 1; x++) //3x3九宫格采样
    {
        for(int y = -1; y <= 1 ; y++)
        {
            //hash22(intPos + float2(x,y)) 相当于offset，定义为在周围9个格子中的某一个特征点
            //float2(x,y) 相当于周围九格子root
            //如没有 offset，那么格子是规整的距离场
            //如果没有 root，相当于在自己的晶格范围内生成特征点，一个格子就有九个“球球”
            float d = distance(hash22(intPos + float2(x,y)) + float2(x,y), fracPos); //fracPos作为采样点，hash22(intPos)作为生成点，来计算dist
            
            if(d < F1) {
                F2 = F1;
                F1 = d;
            } else if(d < F2) {
                F2 = d;
            }

        }
    
    }
    switch (returnType) {
        case 0:
            return F1;
        case 1:
            return F2 - F1;
        default:
            return F1;
    }


}

//=======================================分形噪音========================================//
//--------------------------
// 分形Perlin噪声
//--------------------------
float FBMPerlinNoise(
    float2 uv,
    float baseFrequency,
    uint octaves,//叠加层数
    float persistence,//持续度
    float lacunarity,//粗糙度
    float scale,
    uint width,
    uint height,
    int seed
) {
    //uv缩放
    float2 scaleUv = float2((uv.x - width / 2) / scale, (uv.y - height / 2) / scale);
    
    // 分形叠加 (FBM)
    float noiseValue = 0.0;
    float frequency = baseFrequency;
    float amplitude = 1.0;
    
    float maxAmplitude = 0.0;
    
    [unroll(8)] // 显式展开循环提升性能
    for (uint i = 0; i < octaves; i++) {
        float2 samplePos = scaleUv * frequency + float2(seed, seed);
        float noise = snoise(samplePos);
        noiseValue += noise * amplitude;
        maxAmplitude += amplitude;
        amplitude *= persistence;
        frequency *= lacunarity;
    }
    //归一化（0-1）
    return saturate(noiseValue / maxAmplitude);
}
//--------------------------
// 分形Value噪声
//--------------------------
float FBMValueNoise(
    float2 uv,
    float baseFrequency,
    int octaves,
    float persistence,
    float lacunarity,
    float scale,
    uint width,
    uint height,
    int seed
) {
    //uv缩放
    float2 scaleUv = float2((uv.x - width / 2) / scale, (uv.y - height / 2) / scale);

    // 分形叠加 (FBM)
    float noiseValue = 0.0;
    float frequency = baseFrequency;
    float amplitude = 1.0;

    float maxAmplitude = 0.0;

    [unroll(8)] // 显式展开循环提升性能
    for (int i = 0; i < octaves; i++) {
        float2 samplePos = scaleUv * frequency + float2(seed, seed);
        float noise = valueNoise(samplePos);
        noiseValue += noise * amplitude;
        maxAmplitude += amplitude;
        amplitude *= persistence;
        frequency *= lacunarity;
    }
    
    return saturate(noiseValue / maxAmplitude);
}

//--------------------------
// 分形Worley噪声
//--------------------------
float FBMWorleyNoise(
    float2 uv,
    float baseFrequency,
    int octaves,
    float persistence,
    float lacunarity,
    float scale,
    uint width,
    uint height,
    int returnType,
    int seed
) {
    //uv缩放
    float2 scaleUv = float2((uv.x - width / 2) / scale, (uv.y - height / 2) / scale);

    // 分形叠加 (FBM)
    float noiseValue = 0.0;
    float frequency = baseFrequency;
    float amplitude = 1.0;

    float maxAmplitude = 0.0;

    [unroll(8)] // 显式展开循环提升性能
    for (int i = 0; i < octaves; i++) {
        float2 samplePos = scaleUv * frequency + float2(seed, seed);
        float noise = worleyNoise(samplePos, returnType);
        noiseValue += noise * amplitude;
        maxAmplitude += amplitude;
        amplitude *= persistence;
        frequency *= lacunarity;
    }
    
    return saturate(noiseValue / maxAmplitude);
}

//--------------------------
// Perlin-Value噪声混合
//--------------------------
float FBMPerlinValueNoise(
    float2 uv,
    float baseFrequency,//振幅
    int octaves,
    float persistence,//振幅衰减系数（0-1）
    float lacunarity,//频率倍增系数（>1）
    float warpStrength,//坐标扭曲强度
    float warpFrequency,//坐标扭曲频率
    float perlinWeight,//Perlin噪声权重（0-1）
    float valueNoiseWeight,//Value噪声权重（0-1）
    float blendFrequency,//混合噪声频率
    int seed
) {
    // 域扭曲（使用不同种子偏移）
    float2 warp = float2(
        snoise(uv * warpFrequency + float2(seed,seed)),
        snoise((uv + float2(100,100)) * warpFrequency + float2(seed,seed))
    ) * warpStrength;

    // 分形叠加 (FBM)
    float pNoise = 0, vNoise = 0;
    float frequency = baseFrequency;
    float amplitude = 1.0;
    float maxAmplitude = 0.0;


    [unroll(8)] // 显式展开循环提升性能
    for (int i = 0; i < octaves; i++) {
        float2 samplePos = (uv + warp + float2(seed, seed)) * frequency;
        //float2 pPos = uv * frequency * blendFrequency;
        pNoise += snoise(samplePos) * amplitude;

        float2 vPos = (uv + float2(seed, seed)) * frequency * blendFrequency;
        vNoise += valueNoise(vPos) * amplitude;


        maxAmplitude += amplitude;
        amplitude *= persistence;
        frequency *= lacunarity;
    }
    // 归一化
    pNoise = saturate(pNoise / maxAmplitude);
    vNoise = saturate(vNoise / maxAmplitude);
    

    //return lerp(pNoise, vNoise, blendFrequency);
    float mixedNoise = (pNoise * perlinWeight) + (vNoise * valueNoiseWeight);
    return mixedNoise;
}

//--------------------------
// Perlin-Worley噪声混合
//--------------------------
float MIXPerlinWorleyNoise(
    float2 uv,
    float perlinFrequency,//柏林频率
    float worleyFrequency,//细胞频率
    float weight,//噪声权重（0-1）
    int octaves,
    float persistence,
    float lacunarity,
    float scale,
    uint width,
    uint height,
    int seed
) {
    
    // Worley噪声生成洞穴轮廓
    float worley = 1 - FBMWorleyNoise(
       uv.xy, worleyFrequency, octaves, persistence, lacunarity, scale, width, height, 0, seed
    );
    
    // Perlin噪声添加细节
    //float perlin = snoise(
    //   uv.xy * perlinFrequency
    //);
    float perlin = FBMPerlinNoise(
       uv.xy, perlinFrequency, octaves, persistence, lacunarity, scale, width, height, seed
    );
    float worleyWeight = 1 - weight;

    return worley * worleyWeight + perlin * weight;
    
    
}

#endif