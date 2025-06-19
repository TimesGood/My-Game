using System.Collections;
using System.Collections.Generic;
using MessagePack;
using UnityEngine;

[MessagePackObject]
public class MapData {

    //��Ƭ��ͼ��������
    [Key(0)]
    public int[,][,,] chunkDatas;

    [Key(1)]
    public int[,,] tileDatas;//��Ƭ����
    

    public MapData(int[,][,,] chunkDatas, int[,,] tileDatas) {
        this.chunkDatas = chunkDatas;
        this.tileDatas = tileDatas;
    }
}
