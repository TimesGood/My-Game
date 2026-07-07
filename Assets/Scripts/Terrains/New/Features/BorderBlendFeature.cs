using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BorderBlendFeature : BiomeFeature
{
    public BorderBlendMode mode = BorderBlendMode.HeightBlend;
    public int blendDistance = 50;
    public bool blendStoneCurve;
    public int heightAdd;
    public NoiseParams terrainNoise;


    // ========== 运行时纹理（Execute 期间临时使用） ==========
    [System.NonSerialized] private SamplerResult _terrainTex;


    public override void Init(BiomeContext _ctx)
    {
        //if (mode == BorderBlendMode.CurveBlend)
        //{
        //    _terrainTex = NoiseSampler.GenerateTexture(_biomeSize.x, _biomeSize.y, terrainNoise, _seed);
        //}
    }

    public override void Execute(BiomeContext _ctx) {
        //if (_ctx.surfaceStart == 0 || _ctx.surfaceEnd == 0) return;
        //WorldManager w = WorldManager.Instance;

        //if (mode == BorderBlendMode.HeightBlend) BlendByHeight(w, _ctx);
        //else if (mode == BorderBlendMode.CurveBlend && _terrainTex != null) BlendByCurve(w, _ctx);
    }

    private void BlendByHeight(WorldManager w, BiomeContext ctx)
    {
        //int lsY = w.surfaceHeights[ctx.surfaceStart], rsY = w.surfaceHeights[ctx.surfaceEnd];
        //int lbX = Mathf.Max(ctx.surfaceStart - blendDistance, 0);
        //int reX = Mathf.Min(ctx.surfaceEnd + blendDistance, w.worldSize.x - 1);
        //int lbY = w.surfaceHeights[lbX], reY = w.surfaceHeights[reX];

        //for (int x = 0; x < blendDistance; x++)
        //{
        //    float t = (float)x / (blendDistance - 1);
        //    float n = Mathf.PerlinNoise(x * 0.05f, 0) * 2 - 1;
        //    FillErase(w, x + lbX, (int)(Mathf.Lerp(lbY, lsY, t) + n * 3f));
        //    FillErase(w, x + reX - blendDistance, (int)(Mathf.Lerp(rsY, reY, t) + n * 3f));
        //}
    }

    private void BlendByCurve(WorldManager w, BiomeContext ctx)
    {
        int start = ctx.LocalToWorldX(0);
        BlendData(w.terrainCurveData, _terrainTex.curveData, start);
        if (blendStoneCurve) BlendData(w.stoneCurveData, _terrainTex.curveData, start, 100);
    }

    private void BlendData(float[] m, float[] s, int start, int add = 0)
    {
        for (int i = 0; i < s.Length; i++)
        {
            float f = 1f;
            if (i < blendDistance) f = Smooth(0, 1, i / (float)blendDistance);
            else if (i > s.Length - 1 - blendDistance) f = Smooth(1, 0, (i - (s.Length - 1 - blendDistance)) / (float)blendDistance);
            int idx = start + i;
            if (idx < m.Length) m[idx] = Mathf.Lerp(m[idx], s[i] + add, f);
        }
    }

    private void FillErase(WorldManager w, int x, int y)
    {
        int d = y; while (w.GetTileClass(Layers.Ground, x, d) == null && d >= 0) d--;
        int u = y + 1, oh = w.surfaceHeights[x];
        while (u < oh) { w.SetTileClass(null, Layers.Ground, x, u); u++; }
        w.surfaceHeights[x] = y;
    }

    private float Smooth(float a, float b, float t) { t = Mathf.Clamp01(t); t = -2f * t * t * t + 3f * t * t; return Mathf.Lerp(a, b, t); }
}
