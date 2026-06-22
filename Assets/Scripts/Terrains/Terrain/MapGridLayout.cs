using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//群落合集
[CreateAssetMenu(fileName = "GridLayout", menuName = "Terrain/new GridLayout")]
public class MapGridLayout : ScriptableObject {
    protected WorldManager world => WorldManager.Instance;

    //储存已经分配好生成点位的群落
    protected Dictionary<Vector2Int, BaseBiome> result = new Dictionary<Vector2Int, BaseBiome>();

    [Header("群落集合")]
    public BaseBiome[] biomes;

    public void InitLayout() {
        InitDistribution();
    }


    //初始化群落分布
    protected virtual void InitDistribution() {
        if (biomes == null || biomes.Length == 0) return;
        //泊松圆盘随机
        //List<Vector2> points = PoissonDiscSampling.GeneratePoints(100f, new Vector2(0, 0), world.worldSize);
        //foreach (var point in points) {
        //    int biomeIndex = Random.Range(0, biomes.Length);
        //    BaseBiome biome = biomes[biomeIndex];
        //    result.Add(new Vector2Int((int)point.x, (int)point.y), biome);
        //}

    }

    //生成
    public virtual IEnumerator Generation() {
        int i = 0;
        foreach (var kvp in result) {
            Vector2Int center = kvp.Key;
            BaseBiome biome = kvp.Value;
            biome.InitBiome(center, world.seed);

            Debug.Log("第" + i + "群落【" + biome.name + "】生成中...");
            i++;
            yield return world.StartCoroutine(biome.GenerateBiome());
        }

    }

}
