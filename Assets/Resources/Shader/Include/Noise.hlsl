#ifndef NOISE_COMMON_INCLUDED
#define NOISE_COMMON_INCLUDED
#include "HashLib.hlsl"

float3 mod289(float3 x) { return x - floor(x * (1.0 / 289.0)) * 289.0; }
float2 mod289(float2 x) { return x - floor(x * (1.0 / 289.0)) * 289.0; }
float3 permute(float3 x) { return mod289(((x * 34.0) + 1.0) * x); }
float2 permute(float2 x) { return mod289(((x * 34.0) + 1.0) * x); }

//--------------------------
// Perlin�ݶ�����
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
// Simplex����
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
    //���Ʒ�Χ��0-1��
    return ((130.0 * dot(m, g)) + 1) * 0.5;
}

//--------------------------
// Value����
//--------------------------
float valueNoise(float2 uv)
{
    float2 intPos = floor(uv); //uv����, ȡ uv ����ֵ���൱�ھ���id
    float2 fracPos = frac(uv); //ȡ uv С��ֵ���൱�ھ����ھֲ����꣬ȡֵ���䣺(0,1)

    //��ά��ֵȨ�أ�һ������smoothStep�ĺ�������Hermit��ֵ������Ҳ��S���ߣ�S(x) = -2 x^3 + 3 x^2
    //����Hermit��ֵ���ԣ������ڱ�֤��������Ļ����ϱ�֤��ֵ�����ĵ����ڲ�ֵ����Ϊ0���������ṩ��ƽ����
    float2 u = fracPos * fracPos * (3.0 - 2.0 * fracPos); 

    //�ķ�ȡ�㣬����intPos�ǹ̶��ģ�����դ���ˣ�ͬһ�������ĵ�ֵ��ͬ��ֻ��С�����ֲ�ͬ������ֵ��
    float va = hash2to1( intPos + float2(0.0, 0.0) );  //hash2to1 ��ά���룬ӳ�䵽1ά���
    float vb = hash2to1( intPos + float2(1.0, 0.0) );
    float vc = hash2to1( intPos + float2(0.0, 1.0) );
    float vd = hash2to1( intPos + float2(1.0, 1.0) );

    //lerp��չ����ʽ����ȫ������lerp(a,b,c)Ƕ��ʵ��
    float k0 = va;
    float k1 = vb - va;
    float k2 = vc - va;
    float k4 = va - vb - vc + vd;
    float value = k0 + k1 * u.x + k2 * u.y + k4 * u.x * u.y;

    return value;
}

//--------------------------
// Worley������ϸ��������
// returnType����������
//--------------------------
float worleyNoise(float2 uv, int returnType)
{

    float F1 = 1e5;
    float F2 = 1e5;
    float2 intPos = floor(uv);
    float2 fracPos = frac(uv);

    for(int x = -1; x <= 1; x++) //3x3�Ź������
    {
        for(int y = -1; y <= 1 ; y++)
        {
            //hash22(intPos + float2(x,y)) �൱��offset������Ϊ����Χ9�������е�ĳһ��������
            //float2(x,y) �൱����Χ�Ÿ���root
            //��û�� offset����ô�����ǹ����ľ��볡
            //���û�� root���൱�����Լ��ľ���Χ�����������㣬һ�����Ӿ��оŸ�������
            float d = distance(hash22(intPos + float2(x,y)) + float2(x,y), fracPos); //fracPos��Ϊ�����㣬hash22(intPos)��Ϊ���ɵ㣬������dist
            
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

//=======================================��������========================================//
//--------------------------
// ����Perlin����
//--------------------------
float FBMPerlinNoise(
    float2 uv,
    float baseFrequency,
    uint octaves,//���Ӳ���
    float persistence,//������
    float lacunarity,//�ֲڶ�
    float scale,
    uint width,
    uint height,
    int seed
) {
    //uv����
    float2 scaleUv = float2((uv.x - width / 2) / scale, (uv.y - height / 2) / scale);
    
    // ���ε��� (FBM)
    float noiseValue = 0.0;
    float frequency = baseFrequency;
    float amplitude = 1.0;
    
    float maxAmplitude = 0.0;
    
    [unroll(8)] // ��ʽչ��ѭ����������
    for (uint i = 0; i < octaves; i++) {
        float2 samplePos = scaleUv * frequency + float2(seed, seed);
        float noise = snoise(samplePos);
        noiseValue += noise * amplitude;
        maxAmplitude += amplitude;
        amplitude *= persistence;
        frequency *= lacunarity;
    }
    //��һ����0-1��
    return saturate(noiseValue / maxAmplitude);
}
//--------------------------
// ����Value����
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
    //uv����
    float2 scaleUv = float2((uv.x - width / 2) / scale, (uv.y - height / 2) / scale);

    // ���ε��� (FBM)
    float noiseValue = 0.0;
    float frequency = baseFrequency;
    float amplitude = 1.0;

    float maxAmplitude = 0.0;

    [unroll(8)] // ��ʽչ��ѭ����������
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
// ����Worley����
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
    //uv����
    float2 scaleUv = float2((uv.x - width / 2) / scale, (uv.y - height / 2) / scale);

    // ���ε��� (FBM)
    float noiseValue = 0.0;
    float frequency = baseFrequency;
    float amplitude = 1.0;

    float maxAmplitude = 0.0;

    [unroll(8)] // ��ʽչ��ѭ����������
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
// Perlin-Value�������
//--------------------------
float FBMPerlinValueNoise(
    float2 uv,
    float baseFrequency,//���
    int octaves,
    float persistence,//���˥��ϵ����0-1��
    float lacunarity,//Ƶ�ʱ���ϵ����>1��
    float warpStrength,//����Ť��ǿ��
    float warpFrequency,//����Ť��Ƶ��
    float perlinWeight,//Perlin����Ȩ�أ�0-1��
    float valueNoiseWeight,//Value����Ȩ�أ�0-1��
    float blendFrequency,//�������Ƶ��
    int seed
) {
    // ��Ť����ʹ�ò�ͬ����ƫ�ƣ�
    float2 warp = float2(
        snoise(uv * warpFrequency + float2(seed,seed)),
        snoise((uv + float2(100,100)) * warpFrequency + float2(seed,seed))
    ) * warpStrength;

    // ���ε��� (FBM)
    float pNoise = 0, vNoise = 0;
    float frequency = baseFrequency;
    float amplitude = 1.0;
    float maxAmplitude = 0.0;


    [unroll(8)] // ��ʽչ��ѭ����������
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
    // ��һ��
    pNoise = saturate(pNoise / maxAmplitude);
    vNoise = saturate(vNoise / maxAmplitude);
    

    //return lerp(pNoise, vNoise, blendFrequency);
    float mixedNoise = (pNoise * perlinWeight) + (vNoise * valueNoiseWeight);
    return mixedNoise;
}

//--------------------------
// Perlin-Worley�������
//--------------------------
float MIXPerlinWorleyNoise(
    float2 uv,
    float perlinFrequency,//����Ƶ��
    float worleyFrequency,//ϸ��Ƶ��
    float weight,//����Ȩ�أ�0-1��
    int octaves,
    float persistence,
    float lacunarity,
    float scale,
    uint width,
    uint height,
    int seed
) {
    
    // Worley�������ɶ�Ѩ����
    float worley = 1 - FBMWorleyNoise(
       uv.xy, worleyFrequency, octaves, persistence, lacunarity, scale, width, height, 0, seed
    );
    
    // Perlin�������ϸ��
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