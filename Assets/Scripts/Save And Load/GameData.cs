using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 游戏数据对象
/// </summary>
public class GameData
{
    public WorldMeta worldMeta; // 当前世界元数据
    public WorldCreationParams worldCreationParams; // 当前世界创建参数
    public List<Chunk> chunks; // 地图区块数据

    public GameData(WorldMeta worldMeta) {
        this.worldMeta = worldMeta;
    }

}
