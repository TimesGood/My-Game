using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 主界面 UI 控制器
/// 管理世界列表显示、新建世界、删除世界等功能
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    [Header("面板引用")]
    [SerializeField] private GameObject worldListPanel;
    [SerializeField] private GameObject newWorldPanel;

    [Header("世界列表")]
    [SerializeField] private Transform worldListContent;
    [SerializeField] private GameObject worldItemPrefab;

    [Header("新建世界表单")]
    [SerializeField] private InputField worldNameInput;
    [SerializeField] private InputField seedInput;
    [SerializeField] private Dropdown worldSizeDropdown;

    [Header("世界大小预设")]
    [SerializeField] private int smallWidth = 2400;
    [SerializeField] private int smallHeight = 800;
    [SerializeField] private int mediumWidth = 4200;
    [SerializeField] private int mediumHeight = 1200;
    [SerializeField] private int largeWidth = 6000;
    [SerializeField] private int largeHeight = 2000;

    [Header("确认对话框")]
    [SerializeField] private GameObject confirmDialog;
    [SerializeField] private Text confirmText;

    private List<WorldMeta> worldList;
    private string pendingDeleteWorldId;

    private void Start()
    {
        ShowWorldList();
    }

    /// <summary>
    /// 显示世界列表面板
    /// </summary>
    public void ShowWorldList()
    {
        worldListPanel.SetActive(true);
        newWorldPanel.SetActive(false);
        if (confirmDialog) confirmDialog.SetActive(false);

        RefreshWorldList();
    }

    /// <summary>
    /// 刷新世界列表显示
    /// </summary>
    private void RefreshWorldList()
    {
        // 清空现有列表
        foreach (Transform child in worldListContent)
        {
            Destroy(child.gameObject);
        }

        // 加载世界列表
        worldList = WorldSaveManager.LoadWorldList();

        foreach (WorldMeta meta in worldList)
        {
            GameObject item = Instantiate(worldItemPrefab, worldListContent);
            WorldItemUI itemUI = item.GetComponent<WorldItemUI>();
            if (itemUI != null)
            {
                itemUI.Setup(meta, OnWorldSelected, OnDeleteRequested);
            }
        }
    }

    /// <summary>
    /// 点击已有世界 - 加载该世界
    /// </summary>
    private void OnWorldSelected(WorldMeta _meta)
    {
        // 设置 GameSession
        GameSession.Instance.SetupLoadWorld(_meta);

        // 更新最后游玩时间
        WorldSaveManager.UpdateLastPlayed(_meta.worldId);

        // 加载游戏场景
        SceneManager.LoadScene("SampleScene");
    }

    /// <summary>
    /// 点击删除按钮 - 显示确认对话框
    /// </summary>
    private void OnDeleteRequested(WorldMeta _meta)
    {
        pendingDeleteWorldId = _meta.worldId;
        if (confirmDialog)
        {
            confirmDialog.SetActive(true);
            if (confirmText)
            {
                confirmText.text = $"确定要删除世界 \"{_meta.worldName}\" 吗？\n此操作不可撤销。";
            }
        }
    }

    /// <summary>
    /// 确认删除
    /// </summary>
    public void ConfirmDelete()
    {
        if (!string.IsNullOrEmpty(pendingDeleteWorldId))
        {
            WorldSaveManager.DeleteWorld(pendingDeleteWorldId);
            pendingDeleteWorldId = null;
        }

        if (confirmDialog) confirmDialog.SetActive(false);
        RefreshWorldList();
    }

    /// <summary>
    /// 取消删除
    /// </summary>
    public void CancelDelete()
    {
        pendingDeleteWorldId = null;
        if (confirmDialog) confirmDialog.SetActive(false);
    }

    /// <summary>
    /// 显示新建世界面板
    /// </summary>
    public void ShowNewWorldPanel()
    {
        worldListPanel.SetActive(false);
        newWorldPanel.SetActive(true);

        // 重置表单
        worldNameInput.text = "";
        seedInput.text = "";
        worldSizeDropdown.value = 1; // 默认中等大小
    }

    /// <summary>
    /// 创建并开始新世界
    /// </summary>
    public void CreateAndStart()
    {
        // 获取世界名称
        string worldName = worldNameInput.text.Trim();
        if (string.IsNullOrEmpty(worldName))
        {
            worldName = "新世界";
        }

        // 获取种子（留空则随机）
        int seed = 0;
        if (!string.IsNullOrEmpty(seedInput.text))
        {
            int.TryParse(seedInput.text, out seed);
        }

        // 获取世界大小
        int width, height;
        GetWorldSize(out width, out height);

        // 创建世界元数据
        WorldMeta meta = WorldSaveManager.CreateWorld(worldName, seed, width, height);

        // 设置 GameSession
        GameSession.Instance.SetupNewWorld(meta, seed, width, height);

        // 加载游戏场景
        SceneManager.LoadScene("SampleScene");
    }

    /// <summary>
    /// 根据下拉框选择获取世界大小
    /// </summary>
    private void GetWorldSize(out int _width, out int _height)
    {
        switch (worldSizeDropdown.value)
        {
            case 0: // 小
                _width = smallWidth;
                _height = smallHeight;
                break;
            case 1: // 中
                _width = mediumWidth;
                _height = mediumHeight;
                break;
            case 2: // 大
                _width = largeWidth;
                _height = largeHeight;
                break;
            default:
                _width = mediumWidth;
                _height = mediumHeight;
                break;
        }
    }
}
