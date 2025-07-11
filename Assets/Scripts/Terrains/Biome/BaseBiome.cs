using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEditor.Timeline;
using UnityEngine;

//群落基类
public abstract class BaseBiome : ScriptableObject
{
    protected MapGenerator map;
    public int biomeWidth;//群落宽（只代表该群落生成的最大宽高，实际以生成轮廓为准）
    public int biomeHeight;//群落高
    private Vector2Int worldPosition;//群落世界位置
    protected Vector2Int startPos;//群落开始点位（群落左下点位）
    private Vector2Int curBiomePos = new Vector2Int(0, 0);//群落本地位置
    private Vector2Int temp = new Vector2Int(0, 0);

    //群落噪图配置
    [field: SerializeField] public ShapeGenerator outLine { get; private set; }//群落轮廓
    [field: SerializeField] public PerlinNoise distributionNoise { get; private set; }//群落分布噪图

    //初始化群落
    public virtual void InitBiome(Vector2Int worldPosition, int seed) {
        map = MapGenerator.Instance;
        this.worldPosition = worldPosition;
        startPos = new Vector2Int(worldPosition.x - biomeWidth / 2, worldPosition.y - biomeHeight / 2);

        InitNoise(seed);
    }

    //初始化噪图数据
    public virtual void InitNoise(int seed) {
        //地形噪图生成
        outLine.InitValidate(biomeWidth, biomeHeight, seed);
        outLine.InitNoise();

        //群落在地图上的分布
        distributionNoise?.InitValidate(map.mapSize.x, map.mapSize.y, seed);
        distributionNoise?.InitNoise();
    }

    //生成点是否符合生成条件
    public virtual bool isConformGenerator(Vector2Int generatorDot) {
        if (distributionNoise.GetPixel(generatorDot.x, generatorDot.y).r > 0) {
            return true;
        }
        return false;
    }

    public int GetWorldPositionX(int x) {
        return x + startPos.x;
    }
    public int GetWorldPositionY(int y) {
        return y + startPos.y;
    }

    //群落坐标转世界坐标
    public Vector2Int GetWorldPosition(int x, int y) {
        temp.x = GetWorldPositionX(x);
        temp.y = GetWorldPositionY(y);
        return temp;
    }


    public abstract IEnumerator GenerateBiome();

}
