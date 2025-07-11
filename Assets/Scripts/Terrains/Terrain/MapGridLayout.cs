using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//网格布局
[CreateAssetMenu(fileName = "GridLayout", menuName = "Terrain/new GridLayout")]
public class MapGridLayout : ScriptableObject {
    protected MapGenerator map;
    public int XCell;//X坐标单元格数量
    public int YCell;//Y坐标单元格数量
    protected int cellCount;//单元格总数
    protected int XSize;//X单元格内瓦片数
    protected int YSize;//Y单元格内瓦片数
    protected int totalSize;//单元格内瓦片总数

    //储存已经分配好生成点位的群落
    protected Dictionary<Vector2Int, BaseBiome> result = new Dictionary<Vector2Int, BaseBiome>();

    [Header("群落集合")]
    public BaseBiome[] biomes;

    public virtual void InitLayout() {
        map = MapGenerator.Instance;
        XSize = map.mapSize.x / XCell;
        YSize = map.mapSize.y / YCell;
        cellCount = XCell * YCell;
        totalSize = XSize * YSize;
    }


    //初始化群落分布情况
    protected virtual void InitDistribution() {
        List<BaseBiome> conformBiomes = new List<BaseBiome>();
        //遍历所有单元格
        for (int x = 0; x < XCell; x++) {
            for (int y = 0; y < YCell; y++) {
                Vector2Int cellCenter = GetCellCenter(x, y);

                for (int i = 0; i < biomes.Length; i++) {
                    BaseBiome biome = biomes[i];
                    if (biome.isConformGenerator(cellCenter))
                        conformBiomes.Add(biome);
                }

                //符合条件的群落随机选取一个
                if (conformBiomes.Count == 0) continue;
                int randomIndex = Random.Range(0, conformBiomes.Count);
                result.Add(cellCenter, conformBiomes[randomIndex]);
                conformBiomes.Clear();
            }

        }
    }

    //生成

    public virtual IEnumerator Generation() {

        //遍历所有单元格
        for (int x = 0; x < XCell; x++) {
            for (int y = 0; y < YCell; y++) {
                Vector2Int cellCenter = GetCellCenter(x, y);

                if (result.TryGetValue(cellCenter, out BaseBiome biome)) {
                    //初始化群落
                    biome.biomeWidth = XSize;
                    biome.biomeHeight = YSize;
                    biome.InitBiome(cellCenter, map.seed);

                    yield return biome.GenerateBiome();
                }
            }
        
        }
        
    }

    //获取单元格中点
    protected Vector2Int GetCellCenter(int XCell, int YCell) {
        Vector2Int cellCenter = new Vector2Int(XCell * XSize + XSize / 2, YCell * YSize + YSize / 2);
        return cellCenter;
    }



}
