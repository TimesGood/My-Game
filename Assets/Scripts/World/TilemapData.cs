using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static ChunkHandler;

//瓦片数据管理
public class TilemapData : MonoBehaviour
{
    public Vector2Int chunkCount; //x,y区块数量
    public Vector2Int chunkSize; //x,y区块大小
    private long[,][,,] chunkDatas;//地图瓦片Id

    private void Start() {
        
    }

    //#region 区块
    //private void InitChunk() {
    //    chunkDatas = new long[metadata.chunkCount.x, metadata.chunkCount.y][,,];

    //    for (int chunkXIndex = 0; chunkXIndex < chunkCount.x; chunkXIndex++) {
    //        for (int chunkYIndex = 0; chunkYIndex < chunkCount.y; chunkYIndex++) {
    //            chunkDatas[chunkXIndex, chunkYIndex] = new long[Enum.GetValues(typeof(Layers)).Length, chunkSize.x, chunkSize.y];
    //            //chunkDatas.Add(new Vector2Int(chunkXIndex, chunkYIndex), new long[Enum.GetValues(typeof(Layers)).Length, chunkXSize, chunkYSize]);
    //        }
    //    }
    //}

    ////设置区块瓦片
    //public void SetChunkTile(long tileId, Layers layer, int x, int y) {
    //    int chunkXIndex = x / chunkSize.x;
    //    int chunkYIndex = y / chunkSize.y;
    //    int tileXIndex = x - (chunkXIndex * chunkSize.x);
    //    int tileYIndex = y - (chunkYIndex * chunkSize.y);
    //    chunkDatas[chunkXIndex, chunkYIndex][(int)layer, tileXIndex, tileYIndex] = tileId;
    //}

    ////获取区块瓦片
    //public long GetChunkTile(Layers layer, int x, int y) {
    //    int chunkXIndex = x / chunkSize.x;
    //    int chunkYIndex = y / chunkSize.y;
    //    int tileXIndex = x - (chunkXIndex * chunkSize.x);
    //    int tileYIndex = y - (chunkYIndex * chunkSize.y);
    //    long tileId = chunkDatas[chunkXIndex, chunkYIndex][(int)layer, tileXIndex, tileYIndex];
    //    return tileId;
    //}
    //#endregion
}
