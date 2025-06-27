using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;

//Һ�����ש
[CreateAssetMenu(fileName = "LiquidClass", menuName = "Tile/new LiquidClass")]
public class LiquidClass : TileClass {
    [field: SerializeField] public TileBase[] tiles { get; private set; }//Һ���ڲ�ͬˮλʱ�Ĳ�ͬ��Ƭ
    [field: SerializeField] public float flowSpeed { get; private set; }//�����ٶ�
    [field: SerializeField] public float minVolume { get; private set; } = 0.005f;//��Сˮλ

    //����ˮλ��ȡ��Ӧ�����Ƭ
    public TileBase GetTileToVolume(float volume) {
        //����Һ�����������ͬTile
        if (volume >= 1) {
            return tiles[tiles.Length - 1];
        } else {
            int liquidIndex = Mathf.FloorToInt(volume * (tiles.Length - 1));
            liquidIndex = liquidIndex >= 0 ? liquidIndex : 0;
            if (liquidIndex == 0)
                return null;
            else
                return tiles[liquidIndex];

        }
    }

}
