using System.Collections;
using System.Collections.Generic;
using MessagePack;
using UnityEngine;

//地图数据

[MessagePackObject]
public class MapData {

    //瓦片地图数据索引
    [Key(0)]
    public int[,][,,] chunkDatas;

    [Key(1)]
    public long[,,] tileDatas;//瓦片Id
    

    public MapData(int[,][,,] chunkDatas, long[,,] tileDatas) {
        this.chunkDatas = chunkDatas;
        this.tileDatas = tileDatas;
    }
}
