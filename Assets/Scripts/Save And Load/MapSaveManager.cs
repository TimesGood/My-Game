using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

//地图数据保存管理器
public class MapSaveManager : Singleton<MapSaveManager> {
    [SerializeField] private string fileName;
    [SerializeField] private bool encryptData;

    private MapData gameData;
    private List<ISaveManager> saveManagers;
    private FileDataHandler dataHandler;

    //删除游戏数据
    [ContextMenu("Delete save file")]//添加到组件菜单中
    public void DeleteSaveData() {
        dataHandler = new FileDataHandler(Application.persistentDataPath, fileName, encryptData);
        dataHandler.Delete();
    }

    private void Start() {
        saveManagers = FindAllSaveManagers();
        dataHandler = new FileDataHandler(Application.persistentDataPath, fileName, encryptData);
        //LoadGame();
    }

    //创建新游戏数据
    public void NewGame() {
        WorldGeneration world = WorldGeneration.Instance;
        ChunkHandler chunk = ChunkHandler.Instance;
        //分区
        int chunkXCount = chunk.chunkCount;
        int chunkYCount = chunk.chunkCount * world.worldHeight / world.worldWidth;
        int[,][,,] chunkDatas = new int[chunkXCount, chunkYCount][,,];
        //每个区的瓦片
        int chunkXSize = world.worldWidth / chunkXCount;
        int chunkYSize = world.worldHeight / chunkYCount;
        for (int x = 0; x < chunkXCount; x++) {
            for (int y = 0; y < chunkYCount; y++) {
                chunkDatas[x, y] = new int[4, chunkXSize, chunkYSize];
            }
        }


        int[,,] tileDatas = new int[4, world.worldWidth, world.worldHeight];
        gameData = new MapData(chunkDatas, tileDatas);
    }

    //加载游戏数据
    public void LoadGame() {
        gameData = dataHandler.Load();
        if (this.gameData == null) {
            NewGame();
        }

        foreach (ISaveManager saveManager in saveManagers) {
            saveManager.LoadData(gameData);
        }
    }

    //保存
    public void SaveGame() {
        foreach (ISaveManager saveManager in saveManagers) {
            saveManager.SaveData(ref gameData);
        }
        dataHandler.Save(gameData);
    }

    //退出保存
    protected override void OnApplicationQuit() {
        SaveGame();
        base.OnApplicationQuit();

    }

    //查找游戏内所有实现ISaveManager接口的对象
    private List<ISaveManager> FindAllSaveManagers() {
        IEnumerable<ISaveManager> saveManagers = FindObjectsOfType<MonoBehaviour>().OfType<ISaveManager>();
        return new List<ISaveManager>(saveManagers);
    }

    //查看是否有保存数据
    public bool HasSaveData() {
        return dataHandler.Load() != null;

    }
}
