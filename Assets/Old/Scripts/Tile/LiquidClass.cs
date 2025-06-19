using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;

//Һ�����ש
[CreateAssetMenu(fileName = "LiquidClass", menuName = "Tile/new LiquidClass")]
public class LiquidClass : TileClass {

    [field: SerializeField] public float flowSpeed { get; private set; }//�����ٶ�
    [field: SerializeField] public TileBase[] tiles { get; private set; }//Һ���ڲ�ͬˮλʱ�Ĳ�ͬ��Ƭ
    //[field: SerializeField] public float volume { get; private set; }
    [field: SerializeField] public float minVolume { get; private set; } = 0.0005f;//��Сˮλ

    //����Һ������
    public IEnumerator CalculatePhysics(int x, int y) {
        yield return new WaitForSeconds(1f / flowSpeed);

        WorldGeneration world = WorldGeneration.Instance;
        LiquidHandlerTest liquid = LiquidHandlerTest.Instance;

        //���Ƕ�Һ��������
        float curVolume = liquid.liquidVolume[x, y];
        float downVolume = y - 1 >= 0 ? liquid.liquidVolume[x, y - 1] : 1;
        float leftVolume = x - 1 >= 0 ? liquid.liquidVolume[x - 1, y] : curVolume + 1;
        float rightVolume = x + 1 < world.worldWidth ? liquid.liquidVolume[x + 1, y] : curVolume + 1;


        //��Һ�����ĳ�����ʱ���Ž��з���
        if (curVolume > minVolume) {
            //�����Һ���ڵ�����Ƭ��������
            if (world.GetTileData(Layers.Ground, x, y) != null) {
                world.Erase(this.layer, x, y);
            }
            //�ж�Һ����������
            //�±�û����Ƭ�����С��1ʱ����������
            if (y - 1 >= 0 && world.GetTileData(Layers.Ground, x, y - 1) == null && downVolume < 1) {
                world.PlaceLiquidTile(this, x, y - 1, curVolume);
                world.Erase(this.layer, x, y);
            } else {

                bool isRight = false;
                bool isLeft = false;

                //���
                if (x - 1 >= 0 && world.GetTileData(Layers.Ground, x - 1, y) == null && leftVolume < curVolume) {
                    isLeft = true;
                }
                //�ұ�
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
            //���Һ���������������
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
        //����Һ�����������ͬTile
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

    //����ˮλ��ȡ��Ӧ�����Ƭ
    public TileBase GetTile(float volume) {
        //����Һ�����������ͬTile
        if (volume >= 1) {
            return tiles[tiles.Length - 1];
        } else {
            int liquidIndex = Mathf.FloorToInt(volume * (tiles.Length - 1));
            return tiles[liquidIndex >= 0 ? liquidIndex : 0];

        }
    }

}
