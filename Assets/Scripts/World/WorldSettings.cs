using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


[CreateAssetMenu(fileName ="GameSettins", menuName = "MyGame/new GameSettings")]
public class WorldSettings : ScriptableObject
{
    [field:SerializeField] public int seed { get; private set; }//世界种子
    [field:SerializeField] public Vector2Int chunkSize { get; private set; }//区块大小
    [field: SerializeField] public int chunkScale { get; private set; }//区块缩放
    [HideInInspector] public Vector2Int worldSize { get; private set; }//实际世界大小

    //地图高度：heightAddition + heightMulti * perlinnoise
    [field: SerializeField] public float heightAddition { get; private set; }//基准高度
    [field: SerializeField] public float heightMulti { get; private set; }//可变化高度
    [field: SerializeField, Range(0, 1)]
    public float heightScale { get; private set; }//地形高度波形阈值

    //洞穴生成设置
    [field: SerializeField, Range(0, 1)] public float caveThreshold { get; private set; }//洞穴阈值
    [field: SerializeField, Range(0, 1)] public float caveScale { get; private set; }//洞穴范围
    [field: SerializeField] public bool[,] cavePoints { get; private set; }//标记洞穴生成位置
    //植物生成
    [field: SerializeField, Range(0, 1)] public float plantsThreshold { get; private set; }//植物阈值
    [field: SerializeField, Range(0, 1)] public float plantsFrequncy { get; private set; }//植物生成频率
    //树木生成
    [field: SerializeField, Range(0, 1)] public float treeThreshold { get; private set; }//树木阈值
    [field: SerializeField, Range(0, 1)] public float treeFrequncy { get; private set; }//树木生成频率

    [field: SerializeField] public OreClass[] ores { get; private set; }//可生成的矿物
    [field: SerializeField] public Biome[] biomes { get; private set; }


    public void Init()
    {
        if (seed == 0) seed = Random.Range(-10000, 10000);
        Random.InitState(seed);
        worldSize = chunkSize * chunkScale;
        cavePoints = new bool[worldSize.x, worldSize.y];
    }

    //标记洞穴
    public void InitCaves()
    {
        for (int x = 0; x < worldSize.x; x++)
        {
            int height = GetHeight(x);
            for (int y = 0; y < height; y++)
            {
                float p = (float)y / height;//高度越高，值越大
                float v = Mathf.PerlinNoise((x + seed) * caveScale, (y + seed) * caveScale);
                v /= 0.5f + p;//高度越高，v的值就会越小
                cavePoints[x, y] = v > caveThreshold;
            }
        }
    }

    public int GetHeight(int x)
    {
        return (int)(heightAddition + heightMulti * Mathf.PerlinNoise((x + seed) * heightScale, seed));
    }


}
