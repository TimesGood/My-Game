using System.Collections.Generic;
using System.Linq;
using UnityEngine;


//网格布局
[CreateAssetMenu(fileName = "MapHorizontalLayout", menuName = "Terrain/new MapHorizontalLayout")]
public class MapHorizontalLayout : MapGridLayout {
    public Vector2Int cell;


    //分布
    protected override void InitDistribution() {
        result.Clear();
        List<BaseBiome> baseBiomes = biomes.ToList();
        //网格布局
        List<Vector2> points = PoissonDiscSampling.GenerateGridPoints(cell, world.worldSize, 1, true);
        //分配点位
        foreach (var point in points) {
            int index = Random.Range(0, baseBiomes.Count);
            BaseBiome biome = baseBiomes[index];
            result.Add(new Vector2Int((int)point.x, MapGenerator.Instance.baseHeight), biome);

            baseBiomes.Remove(biome);
        }
    }
}