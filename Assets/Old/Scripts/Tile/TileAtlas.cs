using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//ÍßÆ¬ºÏ¼¯
[CreateAssetMenu(fileName = "TileAtlas", menuName = "Tile/new TileAtlas")]
public class TileAtlas : ScriptableObject
{
    //µØÐÎ»·¾³
    [field: SerializeField] public TileClass grassBlock { get; private set; }//±í²ã
    [field: SerializeField] public TileClass dirtBlock { get; private set; }//ÍÁ²ã
    [field: SerializeField] public TileClass dirtWall { get; private set; }//ÍÁ²ãÇ½±Ú
    [field: SerializeField] public TileClass stoneBlock { get; private set; }//ÑÒ²ã
    [field: SerializeField] public TileClass stoneWall { get; private set; }//ÑÒ²ãÇ½±Ú
    [field: SerializeField] public TileClass plants { get; private set; }//Ö²Îï
    [field: SerializeField] public TileClass tree { get; private set; }//Ê÷Ä¾
    [field: SerializeField] public TileClass leaf { get; private set; }//Ê÷è¾

    //¿óÊ¯
    [field: SerializeField] public OreClass coal { get; private set; }
    [field: SerializeField] public OreClass iron { get; private set; }
    [field: SerializeField] public OreClass gold { get; private set; }
    [field: SerializeField] public OreClass diamond { get; private set; }
}
