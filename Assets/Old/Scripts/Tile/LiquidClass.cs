using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;

//液体类瓷砖
[CreateAssetMenu(fileName = "LiquidClass", menuName = "Tile/new LiquidClass")]
public class LiquidClass : TileClass {

    [field: SerializeField] public float flowSpeed { get; private set; }//流动速度
    [field: SerializeField] public TileBase[] tiles { get; private set; }//液体在不同水位时的不同瓦片
    //[field: SerializeField] public float volume { get; private set; }
    [field: SerializeField] public float minVolume { get; private set; } = 0.005f;//最小水位

    //根据水位获取对应体积瓦片
    public TileBase GetTileToVolume(float volume) {
        //根据液体体积更换不同Tile
        if (volume >= 1) {
            return tiles[tiles.Length - 1];
        } else {
            int liquidIndex = Mathf.FloorToInt(volume * (tiles.Length - 1));
            return tiles[liquidIndex >= 0 ? liquidIndex : 0];

        }
    }

}
