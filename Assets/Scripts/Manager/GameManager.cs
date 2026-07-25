using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;


// 场景入口控制器
public class GameManager : Singleton<GameManager> {

    // 核心管理器
    [SerializeField] private ChunkManager _chunkManager;
    [SerializeField] private WorldGenerator _worldGenerator;
    [SerializeField] private LightHandler _lightHandler;
    [SerializeField] private ChunkHandler _chunkHandler;


    // 根据参数确定是加载世界还是创建世界
    private IEnumerator Start() {
        var flowData = SceneFlowController.Instance.PendingData;
        _chunkManager = ChunkManager.Instance;
        _worldGenerator = WorldGenerator.Instance;
        _lightHandler = LightHandler.Instance;
        _chunkHandler = ChunkHandler.Instance;

        // ── 初始化各系统引用 ──
        _worldGenerator.Init(_chunkManager);

        WorldMeta meta = null;

        if (flowData == null) {
            // 构造元数据
            meta = new WorldMeta {
                worldName = "Debug",
                worldId = System.Guid.NewGuid().ToString(),
                seed = 111,
                width = 6000,
                height = 2000,
                creationTime = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                genParams = new WorldCreationParams()
            };

            _chunkManager.InitializeNewWorld(meta);

            // 执行世界生成（协程，带进度回调）
            yield return _worldGenerator.GenerateWorld(meta.genParams, (progress, msg) => {

            });
        } else if (flowData.isNewWorld) {
            // 构造元数据
            meta = new WorldMeta {
                worldName = flowData.worldName,
                worldId = System.Guid.NewGuid().ToString(),
                seed = flowData.seed,
                width = 6000,
                height = 2000,
                creationTime = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                genParams = flowData.genParams
            };

            _chunkManager.InitializeNewWorld(meta);

            // 执行世界生成（协程，带进度回调）
            yield return _worldGenerator.GenerateWorld(meta.genParams, (progress, msg) => {

            });
        } else {
            meta = WorldSaveManager.GetWorld(flowData.worldId);
            _chunkManager.LoadExistingWorld(meta);

            yield return null;
        }


        _lightHandler.Init(meta);
        _chunkHandler.Init(meta);
        PhysicsSimulationHandler.Instance.Reinitialize();
        yield return null;
    }


    private void LoadScene(string sceneName, Action onComplete = null) {
        StartCoroutine(SceneLoadCoroutine(sceneName, onComplete));
    }

    private IEnumerator SceneLoadCoroutine(string sceneName, Action onComplete) {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = false;

        while (!asyncLoad.isDone) {
            // 计算真实进度 (0.0 - 1.0)
            float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
            // TODO: 此处更新 UI 进度条 loadingBar.value = progress;

            if (asyncLoad.progress >= 0.9f) {
                // 资源加载完毕，等待特定条件（如延时）后激活
                yield return new WaitForSeconds(0.5f);
                asyncLoad.allowSceneActivation = true;
            }

            yield return null; // 挂起至下一帧
        }
    }
}