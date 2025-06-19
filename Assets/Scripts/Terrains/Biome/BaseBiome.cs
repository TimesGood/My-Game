using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

//群落基类
public abstract class BaseBiome : ScriptableObject
{
    protected WorldGeneration world => WorldGeneration.Instance;
    public int biomeWidth;//群落宽（只代表该群落生成的最大宽高，实际以生成轮廓为准）
    public int biomeHeight;//群落高
    private Vector2Int worldPosition;//群落世界位置
    protected Vector2Int generatePos;//世界生成起始点
    private Vector2Int curBiomePos = new Vector2Int(0, 0);//群落本地位置

    //群落噪图配置
    [field: SerializeField] public ShapeGenerator outLine { get; private set; }//群落轮廓

    //初始化群落
    public virtual void InitBiome(Vector2Int worldPosition, int seed) {
        this.worldPosition = worldPosition;
        generatePos = new Vector2Int(worldPosition.x - biomeWidth / 2, worldPosition.y - biomeHeight / 2);

        InitNoise(seed);
    }

    //初始化噪图数据
    public virtual void InitNoise(int seed) {
        //地形噪图生成
        outLine.InitValidate(biomeWidth, biomeHeight, seed);
        outLine.InitNoise();

    }

    //基于世界位置获取对应噪图位置
    public int GetLocalPositionX(int x) {
        int biomeX = x - (worldPosition.x - biomeWidth / 2);
        return biomeX;

    }
    //基于世界位置获取对应噪图位置
    public int GetLocalPositionY(int y) {
        int biomeY = y - (worldPosition.y - biomeHeight / 2);
        return biomeY;

    }

    //基于世界位置获取对应噪图位置
    public Vector2Int GetLocalPosition(int x, int y) {
        curBiomePos.x = GetLocalPositionX(x);
        curBiomePos.y = GetLocalPositionY(y);
        return curBiomePos;

    }


    public abstract IEnumerator GenerateBiome();

}
