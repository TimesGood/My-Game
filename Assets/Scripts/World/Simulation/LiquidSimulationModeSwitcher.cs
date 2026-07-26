using UnityEngine;

/// <summary>
/// 液体模拟模式切换器
/// 用于在运行时切换和对比两种液体模拟模式
/// </summary>
public class LiquidSimulationModeSwitcher : MonoBehaviour {
    [Header("切换设置")]
    public KeyCode switchModeKey = KeyCode.M; // 切换模式的快捷键
    public KeyCode showStatsKey = KeyCode.S;  // 显示/隐藏统计的快捷键

    private PhysicsSimulationHandler simulationHandler;
    private bool showStats = true;

    void Start() {
        simulationHandler = PhysicsSimulationHandler.Instance;
        if (simulationHandler == null) {
            Debug.LogError("[LiquidSimulationModeSwitcher] PhysicsSimulationHandler 未找到");
            enabled = false;
        }
    }

    void Update() {
        if (simulationHandler == null) return;

        // 按 M 键切换模式
        if (Input.GetKeyDown(switchModeKey)) {
            SwitchMode();
        }

        // 按 S 键切换统计显示
        if (Input.GetKeyDown(showStatsKey)) {
            showStats = !showStats;
            simulationHandler.enableStats = showStats;
        }
    }

    /// <summary>
    /// 切换液体模拟模式
    /// </summary>
    public void SwitchMode() {
        var currentMode = simulationHandler.GetLiquidSimulationMode();
        var newMode = currentMode == LiquidSimulationMode.Custom
            ? LiquidSimulationMode.PixelAlchemy
            : LiquidSimulationMode.Custom;

        simulationHandler.SetLiquidSimulationMode(newMode);
        Debug.Log($"[模式切换] 当前模式: {newMode}");
    }

    void OnGUI() {
        if (simulationHandler == null) return;

        GUILayout.BeginArea(new Rect(Screen.width - 310, 10, 300, 200));
        GUILayout.Box("液体模拟模式切换");

        // 显示当前模式
        var currentMode = simulationHandler.GetLiquidSimulationMode();
        GUILayout.Label($"当前模式: {currentMode}");

        // 切换按钮
        if (GUILayout.Button($"切换模式 ({switchModeKey})")) {
            SwitchMode();
        }

        // 模式说明
        GUILayout.Space(10);
        if (currentMode == LiquidSimulationMode.Custom) {
            GUILayout.Label("Custom 模式:");
            GUILayout.Label("- 带冷却时间的密度分层");
            GUILayout.Label("- 基于体积的液体流动");
            GUILayout.Label("- 防止冒泡效果");
        } else {
            GUILayout.Label("PixelAlchemy 模式:");
            GUILayout.Label("- 密度驱动的位移系统");
            GUILayout.Label("- 横向搜索多个格子");
            GUILayout.Label("- 随机化方向避免偏向");
        }

        // 快捷键说明
        GUILayout.Space(10);
        GUILayout.Label($"按 {switchModeKey} 切换模式");
        GUILayout.Label($"按 {showStatsKey} 显示/隐藏统计");

        GUILayout.EndArea();
    }
}
