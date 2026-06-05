using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;


//植物
[CreateAssetMenu(fileName = "AddonClass", menuName = "Tile/new AddonClass")]
public class AddonClass : TileClass
{
    [field: SerializeField] public TileBase[] tiles { get; private set; }//植物在不同阶段的瓦片
    [field: SerializeField] public float growthSpeed;//成长速度


    //根据水位获取对应体积瓦片
    public TileBase GetTileToGrowth(float growth) {
        //根据液体体积更换不同Tile
        if (growth >= 1) {
            return tiles[tiles.Length - 1];
        } else {
            int liquidIndex = Mathf.FloorToInt(growth * (tiles.Length - 1));
            liquidIndex = liquidIndex >= 0 ? liquidIndex : 0;
            if (liquidIndex == 0)
                return null;
            else
                return tiles[liquidIndex];

        }
    }
}
