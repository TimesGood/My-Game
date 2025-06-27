using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 地图元数据
[System.Serializable]
public class MapMetadata {
    public int seed; // 地图种子
    public Vector2Int mapSize;  // 地图尺寸
    public int chunkSize = 32; // 区块尺寸
    public string savePath; // 存储路径
    public DateTime creationTime; //创建时间
}
