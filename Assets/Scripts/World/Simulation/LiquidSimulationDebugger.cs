using UnityEngine;

/// <summary>
/// 液体模拟调试器，用于监控液体流动状态
/// </summary>
public class LiquidSimulationDebugger : MonoBehaviour {
    [Header("调试设置")]
    public bool enableDebugLogs = false;
    public bool showDebugGUI = true;

    private PhysicsSimulationHandler simulationHandler;

    void Start() {
        simulationHandler = PhysicsSimulationHandler.Instance;
    }

    void Update() {
        if (!enableDebugLogs) return;

        // 按 D 键切换调试日志
        if (Input.GetKeyDown(KeyCode.D)) {
            showDebugGUI = !showDebugGUI;
        }
    }

    void OnGUI() {
        if (!showDebugGUI || simulationHandler == null) return;

        GUILayout.BeginArea(new Rect(10, 280, 300, 250));
        GUILayout.Box("液体模拟调试");

        // 显示 FPS
        float fps = simulationHandler.GetCurrentFPS();
        GUIStyle fpsStyle = new GUIStyle(GUI.skin.label);
        if (fps >= 50) {
            fpsStyle.normal.textColor = Color.green;
        } else if (fps >= 30) {
            fpsStyle.normal.textColor = Color.yellow;
        } else {
            fpsStyle.normal.textColor = Color.red;
        }
        GUILayout.Label($"FPS: {fps:F1}", fpsStyle);

        if (GUILayout.Button("打印鼠标位置液体信息")) {
            PrintLiquidInfoAtMouse();
        }

        if (GUILayout.Button("测试液体流动")) {
            TestLiquidFlow();
        }

        GUILayout.EndArea();
    }

    /// <summary>
    /// 打印鼠标位置的液体信息
    /// </summary>
    private void PrintLiquidInfoAtMouse() {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2Int tilePos = new Vector2Int(
            Mathf.FloorToInt(mousePos.x),
            Mathf.FloorToInt(mousePos.y)
        );

        ChunkManager chunkManager = ChunkManager.Instance;
        if (chunkManager == null) return;

        TileData tileData = chunkManager.GetTileData(tilePos);
        Debug.Log($"[液体调试] 位置: {tilePos}");
        Debug.Log($"  液体ID: {tileData.liquidId}");
        Debug.Log($"  液体体积: {tileData.liquidVolume:F4}");
        Debug.Log($"  有液体: {tileData.HasLiquid}");

        // 检查周围格子
        Debug.Log($"[液体调试] 周围格子:");
        Vector2Int[] neighbors = new Vector2Int[] {
            tilePos + Vector2Int.up,
            tilePos + Vector2Int.down,
            tilePos + Vector2Int.left,
            tilePos + Vector2Int.right
        };

        foreach (var neighbor in neighbors) {
            TileData neighborData = chunkManager.GetTileData(neighbor);
            if (neighborData.HasLiquid) {
                Debug.Log($"  {neighbor}: 体积={neighborData.liquidVolume:F4}");
            }
        }
    }

    /// <summary>
    /// 测试液体流动
    /// </summary>
    private void TestLiquidFlow() {
        Debug.Log("[液体调试] 测试液体流动功能已禁用");
    }
}
