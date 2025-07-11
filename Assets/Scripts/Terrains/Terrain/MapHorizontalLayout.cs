using System.Collections;
using System.Linq;
using NUnit.Framework;
using Unity.Entities.UniversalDelegates;
using UnityEngine;
using UnityEngine.XR;


//水平布局
[CreateAssetMenu(fileName = "MapHorizontalLayout", menuName = "Terrain/new MapHorizontalLayout")]
public class MapHorizontalLayout : MapGridLayout {

    public override void InitLayout() {
        map = MapGenerator.Instance;
        XCell = biomes.Length;
        YCell = 1;
        XSize = map.mapSize.x / XCell;
        YSize = map.mapSize.y / YCell;
        cellCount = biomes.Length;
        totalSize = XSize * YSize;
        InitDistribution();
    }
    protected override void InitDistribution() {
        System.Collections.Generic.List<BaseBiome> baseBiomes = biomes.ToList();
        Debug.Log(cellCount);
        for (int cell = 0; cell < cellCount; cell++) {
            int index = Random.Range(0, baseBiomes.Count);
            Vector2Int cellCenter = GetCellCenter(cell, 0);
            BaseBiome biome = baseBiomes[index];
            result.Add(cellCenter, biome);

            baseBiomes.Remove(biome);
        }
    }

    public void DestroyNoiseTexture() {
        //foreach (BiomeTest biome in biomes) {
        //    biome.child.DestroyNoiseTexture();
        //    biome.DestroyNoiseTexture();
        //}
    }

    public override IEnumerator Generation() {
        int i = 0;
        foreach (var item in result) {
            Vector2Int center = item.Key;
            BaseBiome biome = item.Value;
            biome.biomeWidth = XSize;
            biome.biomeHeight = YSize;
            biome.InitBiome(center, map.seed);

            Debug.Log("第" + i + "群落【" + biome.name + "】生成中...");
            i++;
            yield return map.StartCoroutine(biome.GenerateBiome());
        }

    }
}