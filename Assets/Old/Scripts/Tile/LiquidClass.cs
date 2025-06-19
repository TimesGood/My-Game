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
    [field: SerializeField] public float minVolume { get; private set; } = 0.0005f;//最小水位

    //计算液体流动
    public IEnumerator CalculatePhysics(int x, int y) {
        yield return new WaitForSeconds(1f / flowSpeed);

        WorldGeneration world = WorldGeneration.Instance;
        LiquidHandlerTest liquid = LiquidHandlerTest.Instance;

        //各角度液体体积情况
        float curVolume = liquid.liquidVolume[x, y];
        float downVolume = y - 1 >= 0 ? liquid.liquidVolume[x, y - 1] : 1;
        float leftVolume = x - 1 >= 0 ? liquid.liquidVolume[x - 1, y] : curVolume + 1;
        float rightVolume = x + 1 < world.worldWidth ? liquid.liquidVolume[x + 1, y] : curVolume + 1;


        //当液体大于某个体积时，才进行放置
        if (curVolume > minVolume) {
            //如果该液体在地面瓦片，消除它
            if (world.GetTileData(Layers.Ground, x, y) != null) {
                world.Erase(this.layer, x, y);
            }
            //判断液体流动方向
            //下边没有瓦片且体积小于1时，往下流动
            if (y - 1 >= 0 && world.GetTileData(Layers.Ground, x, y - 1) == null && downVolume < 1) {
                world.PlaceLiquidTile(this, x, y - 1, curVolume);
                world.Erase(this.layer, x, y);
            } else {

                bool isRight = false;
                bool isLeft = false;

                //左边
                if (x - 1 >= 0 && world.GetTileData(Layers.Ground, x - 1, y) == null && leftVolume < curVolume) {
                    isLeft = true;
                }
                //右边
                if (x + 1 < world.worldWidth && world.GetTileData(Layers.Ground, x + 1, y) == null && rightVolume < curVolume) {
                    isRight = true;
                }


                if (isRight && isLeft) {
                    float avg = (curVolume + leftVolume + rightVolume) / 3;
                    liquid.liquidVolume[x, y] = avg;
                    liquid.liquidVolume[x - 1, y] = 0;
                    liquid.liquidVolume[x + 1, y] = 0;
                    world.PlaceLiquidTile(this, x - 1, y, avg);
                    world.PlaceLiquidTile(this, x + 1, y, avg);
                } else if (isLeft) {
                    float avg = (curVolume + leftVolume) / 2;
                    liquid.liquidVolume[x, y] = avg;
                    liquid.liquidVolume[x - 1, y] = 0;
                    world.PlaceLiquidTile(this, x - 1, y, avg);
                } else if (isRight) {
                    float avg = (curVolume + rightVolume) / 2;
                    liquid.liquidVolume[x, y] = avg;
                    liquid.liquidVolume[x + 1, y] = 0;
                    world.PlaceLiquidTile(this, x + 1, y, avg);
                }
            }
            //如果液体溢出，向上流动
            float topVolume = liquid.liquidVolume[x, y + 1];
            curVolume = liquid.liquidVolume[x, y];
            if (curVolume > 0.99f && world.GetTileData(Layers.Ground, x, y) == null && topVolume < curVolume) {
                world.PlaceLiquidTile(this, x, y + 1, curVolume - 1);
                liquid.liquidVolume[x, y] = 1;
            }
        } else {
            world.Erase(Layers.Liquid, x, y);
        }


        curVolume = liquid.liquidVolume[x, y];
        //根据液体体积更换不同Tile
        if (curVolume >= 1) {
            TileBase tile = tiles[tiles.Length - 1];
            if (world.GetTileData(Layers.Liquid, x, y) == tile) yield break;
            world.tilemaps[(int)Layers.Liquid].SetTile(new Vector3Int(x, y), tile);
        } else {
            int liquidIndex = Mathf.FloorToInt(liquid.liquidVolume[x, y] * (tiles.Length - 1));
            TileBase tile = tiles[liquidIndex >= 0 ? liquidIndex : 0];
            if (world.GetTileData(Layers.Liquid, x, y) == tile) yield break;
            world.tilemaps[(int)Layers.Liquid].SetTile(new Vector3Int(x, y), tile);
        }

    }

    //根据水位获取对应体积瓦片
    public TileBase GetTile(float volume) {
        //根据液体体积更换不同Tile
        if (volume >= 1) {
            return tiles[tiles.Length - 1];
        } else {
            int liquidIndex = Mathf.FloorToInt(volume * (tiles.Length - 1));
            return tiles[liquidIndex >= 0 ? liquidIndex : 0];

        }
    }

}
