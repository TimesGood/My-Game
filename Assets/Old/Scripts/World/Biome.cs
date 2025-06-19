using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//生物群落
[CreateAssetMenu(fileName = "Biome", menuName = "MyGame/new Biome")]
public class Biome : ScriptableObject
{
    [field: SerializeField] public float heightAddition { get; private set; }
    [field: SerializeField] public float heightMulti { get; private set; }
    [field: SerializeField, Range(0, 1)] public float caveThreshold { get; private set; }
    [field: SerializeField, Range(0, 1)] public float caveScale { get; private set; }
    [field: SerializeField, Range(0, 1)] public float plantsThreshold { get; private set; }
    [field: SerializeField, Range(0, 1)] public float plantsFrequncy { get; private set; }
    [field: SerializeField, Range(0, 1)] public float treeThreshold { get; private set; }
    [field: SerializeField, Range(0, 1)] public float treeFrequncy { get; private set; }
    [field: SerializeField] public Vector2Int treeHeight { get; private set; }
    [field: SerializeField, Range(0, 1)] public float heightScale { get; private set; }
    [field: SerializeField] public OreClass[] ores { get; private set; }//可生成的矿物
    [field: SerializeField] public TileAtlas tileAtlas { get; private set; }//
}
