using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

//��ͼ���ݱ��������
public class MapSaveManager : Singleton<MapSaveManager> {
    [SerializeField] private string fileName;
    [SerializeField] private bool encryptData;

    private MapData gameData;
    private List<ISaveManager> saveManagers;
    private FileDataHandler dataHandler;

    //ɾ����Ϸ����
    [ContextMenu("Delete save file")]//��ӵ�����˵���
    public void DeleteSaveData() {
        dataHandler = new FileDataHandler(Application.persistentDataPath, fileName, encryptData);
        dataHandler.Delete();
    }

    private void Start() {
        saveManagers = FindAllSaveManagers();
        dataHandler = new FileDataHandler(Application.persistentDataPath, fileName, encryptData);
        //LoadGame();
    }

    //��������Ϸ����
    public void NewGame() {
        WorldGeneration world = WorldGeneration.Instance;
        ChunkHandler chunk = ChunkHandler.Instance;
        //����
        int chunkXCount = chunk.chunkCount;
        int chunkYCount = chunk.chunkCount * world.worldHeight / world.worldWidth;
        int[,][,,] chunkDatas = new int[chunkXCount, chunkYCount][,,];
        //ÿ��������Ƭ
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

    //������Ϸ����
    public void LoadGame() {
        gameData = dataHandler.Load();
        if (this.gameData == null) {
            NewGame();
        }

        foreach (ISaveManager saveManager in saveManagers) {
            saveManager.LoadData(gameData);
        }
    }

    //����
    public void SaveGame() {
        foreach (ISaveManager saveManager in saveManagers) {
            saveManager.SaveData(ref gameData);
        }
        dataHandler.Save(gameData);
    }

    //�˳�����
    protected override void OnApplicationQuit() {
        SaveGame();
        base.OnApplicationQuit();

    }

    //������Ϸ������ʵ��ISaveManager�ӿڵĶ���
    private List<ISaveManager> FindAllSaveManagers() {
        IEnumerable<ISaveManager> saveManagers = FindObjectsOfType<MonoBehaviour>().OfType<ISaveManager>();
        return new List<ISaveManager>(saveManagers);
    }

    //�鿴�Ƿ��б�������
    public bool HasSaveData() {
        return dataHandler.Load() != null;

    }
}
