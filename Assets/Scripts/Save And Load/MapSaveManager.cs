using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

//地图数据保存管理器
public class MapSaveManager : Singleton<MapSaveManager> {
    [SerializeField] private string fileName;
    [SerializeField] private bool encryptData;

    private MapData gameData;
    private List<IMapSaveManager> saveManagers;
    private TilemapExporter dataHandler;

    //删除游戏数据
    [ContextMenu("Delete save file")]//添加到组件菜单中
    public void DeleteSaveData() {

    }

    private void Start() {
        saveManagers = FindAllSaveManagers();
        dataHandler = GetComponent<TilemapExporter>();
        
    }

    //创建新游戏数据
    public void NewGame() {
        WorldManager world = WorldManager.Instance;
        ChunkHandler chunk = ChunkHandler.Instance;

        gameData = new MapData();
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

        yield return StartCoroutine(dataHandler.LoadAllTilemaps(
            value => this.gameData = value,
            process => Debug.Log("地图加载中, 进度：" + process)));

        if (this.gameData == null) {
            NewGame();
        }

        foreach (IMapSaveManager saveManager in saveManagers) {
            saveManager.LoadData(gameData);
        }

    }

    //保存游戏数据
    public IEnumerator SaveGame() {
        if (this.gameData == null) {
            gameData = new MapData();
        }

        foreach (IMapSaveManager saveManager in saveManagers) {
            saveManager.SaveData(ref gameData);
        }

        yield return StartCoroutine(dataHandler.ExportAllTilemaps(
            gameData,
            process => Debug.Log("地图保存中, 进度：" + process)));

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
