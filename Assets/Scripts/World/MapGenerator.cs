using System;
using System.Collections;
using UnityEngine;
using UnityEngineInternal;

public class MapGenerator : Singleton<MapGenerator> {
    private MapMetadata metadata;
    private WorldManager world => WorldManager.Instance;


    //世界生成
    public BaseTerrain baseTerrain;//基础地形
    public MapGridLayout[] layouts;

    [ContextMenu("GenerateWorld")]
    public void Test() {
        InitNoiseTexture();
        StartCoroutine(GenerateWorld());
    }

    //初始化噪音图

    private void InitNoiseTexture() {

        baseTerrain.InitNoiseTexture();
        foreach (var layout in layouts) {
            layout.InitLayout();
        }
    }

    //生成世界
    public IEnumerator GenerateWorld() {
        Debug.Log("正在生成基础地形...");
        yield return StartCoroutine(baseTerrain.Generation());

        Debug.Log("正在生成群落地形...");
        foreach (var layout in layouts) {
            yield return StartCoroutine(layout.Generation());
        }
        
        //Debug.Log("正在渲染光照...");
        //LightHandler.Instance.InitLight();

        Debug.Log("正在保存数据...");
        yield return StartCoroutine(MapSaveManager.Instance.SaveGame());
        //保存游戏数据

    }
}
