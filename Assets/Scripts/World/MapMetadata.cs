using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 地图元数据
[System.Serializable]
public class MapMetadata {
    public int seed; // 地图种子
    public Vector2Int mapSize;  // 地图尺寸
    public Vector2Int chunkCount; //区块数量
    public DateTime creationTime; //创建时间


    public Vector2Int GetChunkSize() {
        return new Vector2Int(mapSize.x / chunkCount.x, mapSize.y / chunkCount.y);
    }
}
