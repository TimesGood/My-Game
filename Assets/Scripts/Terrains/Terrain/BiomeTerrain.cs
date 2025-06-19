using System.Collections;
using UnityEngine;
using UnityEngine.XR;


//��ֱ����
[CreateAssetMenu(fileName = "BiomeTerrain", menuName = "Terrain/new BiomeTerrain")]
public class BiomeTerrain : ScriptableObject {

    private WorldGeneration world;

    [Header("Ⱥ�伯��")]
    public BaseBiome[] biomes;


    public int biomeSize => world.worldWidth  / biomes.Length;//Ⱥ���ȷ�Χ

    public void InitNoiseTexture() {
        world = WorldGeneration.Instance;

        for (int i = 0; i < biomes.Length; i++) {
            BaseBiome biome = biomes[i];
            //Ⱥ�����
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
        //����ִ��Ⱥ��
        for (int i = 0; i < biomes.Length; i++) {
            int biomeStart = i * biomeSize;
            int biomeEnd = biomeStart + biomeSize;
            BaseBiome biome = biomes[i];
            Debug.Log("��"+ i +"��Ⱥ�䡾" + biome.name + "��������...");
            yield return WorldGeneration.Instance.StartCoroutine(biome.GenerateBiome());
        }
    }
}