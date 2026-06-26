using System;
using UnityEngine;

/// <summary>
/// 矿石生成配置
/// </summary>
[System.Serializable]
public class OreGeneration
{
    [field: SerializeField] public OreClass oreClass { get; private set; }
    public NoiseParams noiseParams = new NoiseParams();
    [Range(0, 1)] public float threshold = 0.5f;
}

/// <summary>
/// 树木生成配置
/// </summary>
[System.Serializable]
public class TreeGeneration
{
    [field: SerializeField] public TreeClass treeClass { get; private set; }
    public NoiseParams noiseParams = new NoiseParams();
    [Range(0, 100)] public int spawnChance = 50;
}

/// <summary>
/// 轮廓生成定义
/// </summary>
[System.Serializable]
public class OutLineGeneration {
    [field: SerializeField] public TileClass tileClass { get; private set; }
    public ShapeParams shapeParams = new ShapeParams();
}


/// <summary>
/// 瓦片映射 —— 定义群落地形使用的瓦片类型
/// </summary>
[System.Serializable]
public class TileMapping
{
    public TileClass surfaceTile;
    public TileClass dirtTile;
    public TileClass stoneTile;
    public TileClass dirtWall;
    public TileClass stoneWall;

    public TileClass GetTileByDepth(int _worldY, int _terrainHeight, float _stoneHeight)
    {
        if (_worldY < _stoneHeight) return stoneTile;
        if (_worldY < _terrainHeight - 1) return dirtTile;
        return surfaceTile;
    }

    public bool IsValid => surfaceTile != null && dirtTile != null;
}
