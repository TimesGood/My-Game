using System;

/// <summary>
/// 世界元数据 - 存储世界的基本信息
/// </summary>
[Serializable]
public class WorldMeta
{
    /// <summary>世界唯一标识（文件夹名）</summary>
    public string worldId;

    /// <summary>玩家自定义的世界名称</summary>
    public string worldName;

    /// <summary>世界种子</summary>
    public int seed;

    /// <summary>世界宽度（瓦片数）</summary>
    public int width;

    /// <summary>世界高度（瓦片数）</summary>
    public int height;

    // 预览缩略图路径 (相对于存档目录)
    public string thumbnailPath;

    /// <summary>创建时间戳（Unix毫秒）</summary>
    public long creationTime;

    /// <summary>最后游玩时间戳（Unix毫秒）</summary>
    public long lastPlayTime;

    // 可扩展: 游戏时长、boss进度等
    public float playTimeSeconds;

    /// <summary> 世界创建参数 </summary>
    public WorldCreationParams genParams;



    /// <summary>
    /// 创建新的世界元数据
    /// </summary>
    public static WorldMeta Create(string _name, int _seed, int _width, int _height)
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        return new WorldMeta
        {
            worldId = $"world_{now}",
            worldName = _name,
            seed = _seed,
            width = _width,
            height = _height,
            creationTime = now,
            lastPlayTime = now
        };
    }


    /// <summary>格式化的创建时间</summary>
    public string CreationDate =>
        new DateTime(creationTime).ToString("yyyy-MM-dd HH:mm");

    /// <summary>格式化的最后游玩时间</summary>
    public string LastPlayDate =>
        new DateTime(lastPlayTime).ToString("yyyy-MM-dd HH:mm");


    /// <summary>格式化的游戏时长</summary>
    public string PlayTimeDisplay {
        get {
            var ts = TimeSpan.FromSeconds(playTimeSeconds);
            return ts.TotalHours >= 1
                ? $"{(int)ts.TotalHours}h {ts.Minutes}m"
                : $"{ts.Minutes}m {ts.Seconds}s";
        }
    }
}
