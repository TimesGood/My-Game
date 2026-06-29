// =============================================
//  BaseTerrainPassConfig.cs — 基础地形生成配置
//  在群落生成之前执行，生成整个世界的地壳分层结构、洞穴、矿石
//  产出数据写入 GenerationContext，供后续群落访问和覆盖
// =============================================
using UnityEngine;

[CreateAssetMenu(menuName = "MapGen/Base Terrain Config")]
public class BaseTerrainPassConfig : ScriptableObject
{
    [Header("地表曲线")]
    [Tooltip("定义地表高度起伏的曲线（相对于 SurfaceY 的偏移）")]
    public CurveConfig terrainCurve;

    [Header("地壳分层")]
    [Range(0.5f, 0.95f)]
    [Tooltip("石头层边界比例：stoneHeight = SurfaceY * stoneRatio + stoneCurve")]
    public float stoneRatio = 0.8f;

    [Tooltip("瓦片映射：定义地表/泥土/石头/墙壁使用的瓦片类型")]
    public TileMapping tileMapping;

    [Tooltip("深层泥土/石头混合噪声（深层偏石头）")]
    public NoiseParams dirtMixNoise = new NoiseParams();

    [Tooltip("浅层石头/泥土混合噪声（浅层偏泥土）")]
    public NoiseParams stoneMixNoise = new NoiseParams();

    [Header("全局洞穴")]
    [Tooltip("洞穴雕刻噪声")]
    public NoiseParams caveNoise = new NoiseParams();

    [Header("全局矿石（可选）")]
    [Tooltip("在基础地形中散布的全局矿石")]
    public OreGeneration[] globalOres;

    // ========== 运行时纹理（Execute 期间临时使用） ==========
    [System.NonSerialized] private Texture2D _dirtMixTex;
    [System.NonSerialized] private Texture2D _stoneMixTex;
    [SerializeField] public Texture2D _caveTex;

    /// <summary>
    /// 执行基础地形生成
    /// </summary>
    public void Execute(GenerationContext _ctx)
    {
        if (tileMapping == null || !tileMapping.IsValid)
        {
            Debug.LogError("[BaseTerrain] TileMapping 未配置或无效");
            return;
        }

        MapConfig config = _ctx.Config;
        int width = config.Width;
        int height = config.Height;
        int surfaceY = config.SurfaceY;
        int seed = _ctx.Seed;

        // 初始化噪声纹理
        InitNoises(width, height, seed);
        Debug.Log(width+"*****************"+ height+"seed"+seed);

        // 初始化输出数组
        int[] surfaceHeightMap = new int[width];
        float[] stoneHeightMap = new float[width];
        float[] terrainCurveData = new float[width];

        WorldManager world = WorldManager.Instance;
        ChunkManager chunk = ChunkManager.Instance;
        int t = 0;
        // ---- Phase 1: 逐列生成地壳分层 ----
        for (int x = 0; x < width; x++)
        {
            // 地表高度 = SurfaceY + 曲线偏移
            float curveOffset = terrainCurve != null ? terrainCurve.GetHeight(x) : 0f;
            int surfaceHeight = surfaceY + (int)curveOffset;
            surfaceHeightMap[x] = surfaceHeight;
            terrainCurveData[x] = curveOffset;

            // 石头层边界
            float stoneCurve = GetStoneCurve(x, seed);
            float stoneHeight = surfaceY * stoneRatio + stoneCurve;
            stoneHeightMap[x] = stoneHeight;

            // 从底到地表逐格填充
            for (int y = 0; y <= surfaceHeight && y < height; y++)
            {
                // 洞穴雕刻：噪声值 <= 阈值则挖空
                if (_caveTex != null && _caveTex.GetPixel(x, y).r == 1)
                {
                    //if (t < 10000) {
                        //Debug.Log(_caveTex.GetPixel(x, y).r);
                        //t++;
                    //}
                    
                    continue;
                }

                // 根据深度选择瓦片（带噪声混合）
                TileClass tile = GetTileWithMix(y, surfaceHeight, stoneHeight, x);
                if (tile != null)
                {
                    world.SetTileClass(tile, Layers.Ground, x, y);
                }
            }
        }

        // ---- Phase 2: 全局矿石散布 ----
        if (globalOres != null)
        {
            for (int i = 0; i < globalOres.Length; i++)
            {
                OreGeneration ore = globalOres[i];
                if (ore.oreClass == null) continue;

                Texture2D oreTex = NoiseSampler.GenerateTexture(width, height, ore.noiseParams, seed + 100 + i);
                if (oreTex == null) continue;

                for (int x = 0; x < width; x++)
                {
                    int surfaceH = surfaceHeightMap[x];
                    float stoneH = stoneHeightMap[x];

                    // 矿石只在石头层以下放置
                    for (int y = 0; y < (int)stoneH && y < surfaceH; y++)
                    {
                        if (oreTex.GetPixel(x, y).r > ore.threshold)
                        {
                            world.SetTileClass(ore.oreClass, Layers.Ground, x, y);
                        }
                    }
                }
            }
        }

        // ---- Phase 3: 写入 GenerationContext ----
        _ctx.SurfaceHeightMap = surfaceHeightMap;
        _ctx.StoneHeightMap = stoneHeightMap;
        _ctx.TerrainCurveData = terrainCurveData;

        // 同步到 WorldManager（兼容旧代码，如 TerrainFeature）
        if (world.surfaceHeights == null || world.surfaceHeights.Length < width)
            world.surfaceHeights = new int[width];
        if (world.stoneCurveData == null || world.stoneCurveData.Length < width)
            world.stoneCurveData = new float[width];
        if (world.terrainCurveData == null || world.terrainCurveData.Length < width)
            world.terrainCurveData = new float[width];

        for (int x = 0; x < width; x++)
        {
            world.surfaceHeights[x] = surfaceHeightMap[x];
            world.stoneCurveData[x] = stoneHeightMap[x] - surfaceY * stoneRatio;
            world.terrainCurveData[x] = terrainCurveData[x];
        }

        // 释放临时纹理
        DestroyNoises();

        Debug.Log($"[BaseTerrain] 基础地形生成完成 SurfaceY={surfaceY} StoneRatio={stoneRatio}");
    }

    /// <summary>
    /// 通过 NoiseSampler 初始化所有噪声纹理
    /// </summary>
    private void InitNoises(int _width, int _height, int _seed)
    {
        if (terrainCurve != null)
        {
            terrainCurve.InitValidate(_width, _height, _seed);
            terrainCurve.InitNoise();
        }

        _dirtMixTex = NoiseSampler.GenerateTexture(_width, _height, dirtMixNoise, _seed);
        _stoneMixTex = NoiseSampler.GenerateTexture(_width, _height, stoneMixNoise, _seed + 1);
        _caveTex = NoiseSampler.GenerateTexture(_width, _height, caveNoise, _seed + 2);
        _caveTex.Apply();
    }

    /// <summary>
    /// 释放临时噪声纹理
    /// </summary>
    private void DestroyNoises()
    {
        if (_dirtMixTex != null) { DestroyImmediate(_dirtMixTex); _dirtMixTex = null; }
        if (_stoneMixTex != null) { DestroyImmediate(_stoneMixTex); _stoneMixTex = null; }
        if (_caveTex != null) { DestroyImmediate(_caveTex); _caveTex = null; }
        if (terrainCurve != null) terrainCurve.DestroyNoiseTexture();
    }

    /// <summary>
    /// 获取石头层曲线偏移（缓慢波动的 Perlin 噪声）
    /// </summary>
    private float GetStoneCurve(int _x, int _seed)
    {
        return Mathf.PerlinNoise((_x + _seed) * 0.02f, _seed * 0.02f) * 10f;
    }

    /// <summary>
    /// 根据深度和噪声混合选择瓦片
    /// 模拟旧 BaseTerrain 的地壳分层逻辑：
    ///   - 深层区（y < stoneHeight）：dirtMixNoise 决定泥土/石头（偏石头）
    ///   - 浅层区（y < surfaceHeight）：stoneMixNoise 决定石头/泥土（偏泥土）
    ///   - 地表（y == surfaceHeight）：地表瓦片
    /// </summary>
    private TileClass GetTileWithMix(int _worldY, int _surfaceHeight, float _stoneHeight, int _x)
    {
        // 地表层
        if (_worldY >= _surfaceHeight - 1)
            return tileMapping.surfaceTile;

        // 深层区：偏石头，dirtMixNoise > 0.5 时出现泥土口袋
        if (_worldY < _stoneHeight)
        {
            if (_dirtMixTex != null && _dirtMixTex.GetPixel(_x, _worldY).r > 0.5f)
                return tileMapping.dirtTile;
            return tileMapping.stoneTile;
        }

        // 浅层区：偏泥土，stoneMixNoise > 0.5 时出现石头口袋
        if (_stoneMixTex != null && _stoneMixTex.GetPixel(_x, _worldY).r > 0.5f)
            return tileMapping.stoneTile;
        return tileMapping.dirtTile;
    }
}
