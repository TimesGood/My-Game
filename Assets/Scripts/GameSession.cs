using UnityEngine;

/// <summary>
/// 游戏会话 - 跨场景传递世界数据
/// 继承 PersistentSingleton，在场景切换时保持存活
/// </summary>
public class GameSession : PersistentSingleton<GameSession>
{
    /// <summary>当前世界元数据</summary>
    public WorldMeta CurrentWorld { get; set; }

    public WorldCreationParams CreationParams { get; set; }

    /// <summary>是否是新世界（需要执行生成）</summary>
    public bool IsNewWorld { get; set; }

    /// <summary>世界种子（新建世界时使用）</summary>
    public int Seed { get; set; }

    /// <summary>世界宽度（新建世界时使用）</summary>
    public int Width { get; set; }

    /// <summary>世界高度（新建世界时使用）</summary>
    public int Height { get; set; }

    protected override void Awake()
    {
        base.Awake();
    }

    /// <summary>
    /// 设置为新建世界模式
    /// </summary>
    public void SetupNewWorld(WorldMeta _meta, int _seed, int _width, int _height)
    {
        CurrentWorld = _meta;
        IsNewWorld = true;
        Seed = _seed;
        Width = _width;
        Height = _height;
    }

    /// <summary>
    /// 设置为加载已有世界模式
    /// </summary>
    public void SetupLoadWorld(WorldMeta _meta)
    {
        CurrentWorld = _meta;
        IsNewWorld = false;
    }
}
