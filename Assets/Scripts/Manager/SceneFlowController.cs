using UnityEngine.SceneManagement;

using UnityEngine;

/// <summary>
/// 跨场景数据传递控制器（单例，跨场景存活）
/// 负责：主菜单 → 游戏场景 的参数传递
/// </summary>
public class SceneFlowController : Singleton<SceneFlowController> {

    /// <summary>待传递的场景切换数据</summary>
    public SceneTransitionData PendingData { get; private set; }

    /// <summary>从主菜单发起：新建世界</summary>
    public void StartNewWorld(string worldName, int seed, WorldCreationParams genParams) {
        PendingData = new SceneTransitionData {
            isNewWorld = true,
            worldName = worldName,
            seed = seed,
            genParams = genParams
        };
        SceneManager.LoadScene("Game");
    }

    /// <summary>从主菜单发起：加载已有世界</summary>
    public void LoadWorld(string worldId) {
        PendingData = new SceneTransitionData {
            isNewWorld = false,
            worldId = worldId
        };
        SceneManager.LoadScene("Game");
    }

    /// <summary>从游戏返回主菜单</summary>
    public void ReturnToMenu() {
        // 保存 & 清理
        //WorldDataCenter.Instance.Shutdown();
        SceneManager.LoadScene("MainMenu");
    }
}

[System.Serializable]
public class SceneTransitionData {
    public bool isNewWorld;
    public string worldName;
    public string worldId;
    public int seed;
    public WorldCreationParams genParams;
}