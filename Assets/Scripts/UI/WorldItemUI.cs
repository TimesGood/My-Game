using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 世界列表项 UI 组件
/// 显示单个世界的信息和操作按钮
/// </summary>
public class WorldItemUI : MonoBehaviour
{
    [Header("UI 引用")]
    [SerializeField] private Text worldNameText;
    [SerializeField] private Text worldInfoText;
    [SerializeField] private Button selectButton;
    [SerializeField] private Button deleteButton;

    private WorldMeta meta;
    private Action<WorldMeta> onSelect;
    private Action<WorldMeta> onDelete;

    /// <summary>
    /// 初始化世界列表项
    /// </summary>
    public void Setup(WorldMeta _meta, Action<WorldMeta> _onSelect, Action<WorldMeta> _onDelete)
    {
        meta = _meta;
        onSelect = _onSelect;
        onDelete = _onDelete;

        // 显示世界名称
        if (worldNameText)
        {
            worldNameText.text = _meta.worldName;
        }

        // 显示世界信息
        if (worldInfoText)
        {
            string size = GetSizeName(_meta.width, _meta.height);
            string lastPlayed = "从未游玩";
            if (_meta.lastPlayTime > 0)
            {
                DateTimeOffset dt = DateTimeOffset.FromUnixTimeMilliseconds(_meta.lastPlayTime);
                lastPlayed = dt.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
            }
            worldInfoText.text = $"种子: {_meta.seed}  大小: {size}\n最后游玩: {lastPlayed}";
        }

        // 绑定按钮事件
        if (selectButton)
        {
            selectButton.onClick.AddListener(() => onSelect?.Invoke(meta));
        }

        if (deleteButton)
        {
            deleteButton.onClick.AddListener(() => onDelete?.Invoke(meta));
        }
    }

    /// <summary>
    /// 根据尺寸获取大小名称
    /// </summary>
    private string GetSizeName(int _width, int _height)
    {
        if (_width <= 2400) return "小";
        if (_width <= 4200) return "中";
        return "大";
    }
}
