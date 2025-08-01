using System.Collections;
using System.Collections.Generic;
using MessagePack;
using UnityEngine;

//地图数据

[MessagePackObject]
public class MapData {

    //瓦片地图数据索引
    [Key(0)]
    public long[,][,,] chunkDatas;
}
