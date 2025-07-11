using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//生物群落
[System.Serializable]
public class Biome_New
{
    public Color biomeColor;

    //地形
    public float terrainFreq = 0.05f;//地形频率，山峰频率（值越高越密集）
    public float heightMulti = 40f;//世界高度
    public int heightAddition = 25;//抬高一点
    public float dirtFreq = 0.2f;
    public float dirtMulti = 20f;
    public int dirtAddition = 10;//泥土抬升

    //瓦片合辑
    public TileAtlas tileAtlas;

    //洞穴
    public Texture2D caveNoiseTexture;//洞穴噪声预览
    public float caveFreq = 0.05f;//噪声频率（值越高越密集）
    public float caveThreshold = 0.25f;//洞穴阈值
    public bool isGenerateCaves = false;//是否生成洞穴

    //树木
    public float treeFreq;//树木生成频率
    public float treeThreshold;//树木阈值
    public int minTreeHeight;//最小树
    public int maxTreeHeight;//最大树

    //植株
    public float plantsFreq;
    public float plantsThreshold;

    //矿石
    public OreClass_New[] ores;
}
