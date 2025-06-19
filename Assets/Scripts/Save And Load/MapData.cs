using System.Collections;
using System.Collections.Generic;
using MessagePack;
using UnityEngine;

[MessagePackObject]
public class MapData {

    //瓦片地图数据索引
    [Key(0)]
    public int[,][,,] chunkDatas;

    [Key(1)]
    public int[,,] tileDatas;//瓦片坐标
    

    public MapData(int[,][,,] chunkDatas, int[,,] tileDatas) {
        this.chunkDatas = chunkDatas;
        this.tileDatas = tileDatas;
    }
}
