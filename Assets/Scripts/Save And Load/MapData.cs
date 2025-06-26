using System.Collections;
using System.Collections.Generic;
using MessagePack;
using UnityEngine;

//��ͼ����

[MessagePackObject]
public class MapData {

    //��Ƭ��ͼ��������
    [Key(0)]
    public int[,][,,] chunkDatas;

    [Key(1)]
    public long[,,] tileDatas;//��ƬId
    

    public MapData(int[,][,,] chunkDatas, long[,,] tileDatas) {
        this.chunkDatas = chunkDatas;
        this.tileDatas = tileDatas;
    }
}
