using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

//地图数据保存管理器
public class MapSaveManager : Singleton<MapSaveManager> {
    [SerializeField] private string fileName;
    [SerializeField] private bool encryptData;

    private GameData gameData;
    private List<IMapSaveManager> saveManagers;
    private TilemapExporter dataHandler;

    //删除游戏数据
    [ContextMenu("Delete save file")]//添加到组件菜单中
    public void DeleteSaveData() {

    }

    private void Start() {
        saveManagers = FindAllSaveManagers();
        dataHandler = GetComponent<TilemapExporter>();

        // 根据 GameSession 设置当前世界的存档路径
        //var session = GameSession.Instance;
        //if (session?.CurrentWorld != null)
        //{
        //    string worldPath = WorldSaveManager.GetWorldPath(session.CurrentWorld.worldId);
        //    dataHandler.SetCustomSavePath(worldPath);
        //    Debug.Log($"[MapSaveManager] 当前世界路径: {worldPath}");
        //}
    }

    //创建新游戏数据
    public void NewGame(WorldMeta worldMeta) {
        gameData = new GameData(worldMeta);
        
    }


    [ContextMenu("load Game")]
    public void LoadTest() {
        StartCoroutine(LoadGame());
    }
    [ContextMenu("save Game")]
    public void SaveTest() {
        StartCoroutine(SaveGame());
    }

    //加载游戏数据
    public IEnumerator LoadGame() {

        // 数据加载



        // 数据注入
        foreach (IMapSaveManager saveManager in saveManagers) {
            saveManager.LoadData(gameData);
        }

        yield return null;

    }

    //保存游戏数据
    public IEnumerator SaveGame() {

        // 数据收集
        foreach (IMapSaveManager saveManager in saveManagers) {
            saveManager.SaveData(ref gameData);
        }

        // 数据序列化到本地
        
        yield return null;
    }

    //退出保存
    protected override void OnApplicationQuit() {
        //SaveGame();
        base.OnApplicationQuit();

    }

    //查找游戏内所有实现ISaveManager接口的对象
    private List<IMapSaveManager> FindAllSaveManagers() {
        IEnumerable<IMapSaveManager> saveManagers = FindObjectsOfType<MonoBehaviour>().OfType<IMapSaveManager>();
        return new List<IMapSaveManager>(saveManagers);
    }

    //查看是否有保存数据
    public bool HasSaveData() {
        return dataHandler.isExists();
    }
}
