using System.Collections;
using System.Collections.Generic;
using MessagePack;
using UnityEngine;

//��ͼ����

[MessagePackObject]
public class MapData {

    //��Ƭ��ͼ��������
    [Key(0)]
    public long[,][,,] chunkDatas;

    [Key(1)]
    public long[,,] tileDatas;//��ƬId
    

    public MapData(long[,][,,] chunkDatas, long[,,] tileDatas) {
        this.chunkDatas = chunkDatas;
        this.tileDatas = tileDatas;
    }
}
