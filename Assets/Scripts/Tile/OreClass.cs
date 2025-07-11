using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//矿物类瓦片
[System.Serializable]
[CreateAssetMenu(fileName = "OreClass", menuName = "Tile/new OreClass")]
public class OreClass : TileClass
{
    //public Texture2D spreadTexture;
    [field: SerializeField, Range(0, 1)] public float oreRarity { get; private set; }//稀有度
    [field: SerializeField, Range(0, 1)] public float oreRadius { get; private set; }//生成大小
    [field: SerializeField] public float minY { get; private set; }//最小生成区间
    [field: SerializeField] public float maxY { get; private set; }//最大生成区间
    [field: SerializeField] public float offset { get; private set; }//偏移（不同偏移使最终生成的方块放到不同地方）
    public PerlinNoise noise;//矿石分布形态
}
