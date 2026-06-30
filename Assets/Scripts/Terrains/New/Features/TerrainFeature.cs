using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 地形填充 Feature —— 在群落范围内按曲线高度和瓦片映射填充地面。
/// </summary>
[System.Serializable]
public class TerrainFeature : BiomeFeature
{
    public TerrainBlendMode blendMode = TerrainBlendMode.EraseTop;
    public NoiseParams terrainNoise;
    public TileMapping tileMapping;

    // ========== 运行时纹理（Execute 期间临时使用） ==========
    [System.NonSerialized] private SamplerResult _terrainTex;

    public bool IsValid => tileMapping != null && tileMapping.IsValid;

    public override void Init(Vector2Int _biomeSize, int _seed, Dictionary<string, Texture2D> _noiseCache)
    {
        _terrainTex = NoiseSampler.GenerateTexture(_biomeSize.x, _biomeSize.y, terrainNoise, _seed);
    }

    public override void Execute(BiomeContext _ctx)
    {
        if (!IsValid) return;

        WorldManager world = WorldManager.Instance;
        int baseHeight = world.baseHeight;
        int biomeSizeX = _ctx.biomeSize.x;

        _ctx.terrainHeights = new int[biomeSizeX];
        _ctx.worldXs = new int[biomeSizeX];
        _ctx.maxHeight = 0;

        for (int x = 0; x < biomeSizeX; x++)
        {
            int worldX = _ctx.LocalToWorldX(x);
            _ctx.worldXs[x] = worldX;

            _ctx.terrainHeights[x] = _terrainTex != null
                ? baseHeight + (int)_terrainTex.curveData[x]
                : world.surfaceHeights[worldX];

            if (_ctx.terrainHeights[x] > _ctx.maxHeight)
                _ctx.maxHeight = _ctx.terrainHeights[x];

            switch (blendMode)
            {
                case TerrainBlendMode.EraseTop:
                    EraseTop(world, _ctx, worldX, _ctx.terrainHeights[x]);
                    break;
                case TerrainBlendMode.FullBlend:
                    AdjustHeight(world, _ctx, worldX, _ctx.terrainHeights[x]);
                    break;
            }
        }

        for (int y = _ctx.maxHeight; y >= 0; y--)
        {
            int worldY = _ctx.LocalToWorldY(y);
            for (int x = 0; x < biomeSizeX; x++)
            {
                int th = _ctx.terrainHeights[x];
                int wx = _ctx.worldXs[x];
                if (worldY > th) continue;

                float stoneH = baseHeight * 0.8f + world.stoneCurveData[wx];
                TileClass tc = tileMapping.GetTileByDepth(worldY, th, stoneH);
                if (worldY > baseHeight && _ctx.IsSurfaceRange(wx)) tc = tileMapping.dirtTile;
                if (worldY == th && _ctx.IsSurfaceRange(wx)) tc = tileMapping.surfaceTile;

                world.SetTileClass(tc, Layers.Ground, wx, worldY);
            }
        }
    }

    private void EraseTop(WorldManager w, BiomeContext ctx, int x, int h)
    {
        int old = w.surfaceHeights[x];
        if (old > h && ctx.IsSurfaceRange(x))
        {
            for (int y = h; y < old; y++) w.SetTileClass(null, Layers.Ground, x, y);
            w.surfaceHeights[x] = h;
        }
    }

    private void AdjustHeight(WorldManager w, BiomeContext ctx, int x, int h)
    {
        int old = w.surfaceHeights[x];
        if (old > h) { for (int y = h; y < old; y++) w.SetTileClass(null, Layers.Ground, x, y); }
        else { for (int y = old; y < h; y++) if (tileMapping?.dirtTile != null) w.SetTileClass(tileMapping.dirtTile, Layers.Ground, x, y); }
        w.surfaceHeights[x] = h;
    }
}
