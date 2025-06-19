using System.Collections;
using UnityEngine;
using UnityEngine.XR;


//垂直分配
[CreateAssetMenu(fileName = "BiomeTerrain", menuName = "Terrain/new BiomeTerrain")]
public class BiomeTerrain : ScriptableObject {

    private WorldGeneration world;

    [Header("群落集合")]
    public BaseBiome[] biomes;


    public int biomeSize => world.worldWidth  / biomes.Length;//群落宽度范围

    public void InitNoiseTexture() {
        world = WorldGeneration.Instance;

        for (int i = 0; i < biomes.Length; i++) {
            BaseBiome biome = biomes[i];
            //群落落点
            Vector2Int biomePos = new Vector2Int(biomeSize / 2 + i * biomeSize, world.worldHeight / 2);
            biome.biomeWidth = biomeSize;
            biome.biomeHeight = world.worldHeight;
            biome.InitBiome(biomePos, world.seed);

        }
    }

    public void DestroyNoiseTexture() {
        //foreach (BiomeTest biome in biomes) {
        //    biome.child.DestroyNoiseTexture();
        //    biome.DestroyNoiseTexture();
        //}
    }

    public IEnumerator Generation() {
        //遍历执行群落
        for (int i = 0; i < biomes.Length; i++) {
            int biomeStart = i * biomeSize;
            int biomeEnd = biomeStart + biomeSize;
            BaseBiome biome = biomes[i];
            Debug.Log("第"+ i +"个群落【" + biome.name + "】生成中...");
            yield return WorldGeneration.Instance.StartCoroutine(biome.GenerateBiome());
        }
    }
}