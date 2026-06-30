using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEditor.Timeline;
using UnityEngine;
using static UnityEditor.PlayerSettings;

//群落基类
public abstract class BaseBiome : ScriptableObject
{
    protected WorldManager world => WorldManager.Instance;
    [field: SerializeField, Range(0, 1)]
    public float heightScale { get; private set; }//群落高度范围
    public Vector2Int biomeSize;//群落大小
    private Vector2Int worldPos;//群落世界位置
    private Vector2Int localPos;//群落本地位置
    protected Vector2Int minPos;//群落最小点位
    protected Vector2Int maxPos;//群落最大点位

    //群落噪图配置
    //[field: SerializeField] public ShapeGenerator outLine { get; private set; }//群落轮廓
    //[field: SerializeField] public PerlinNoise distributionNoise { get; private set; }//群落分布噪图

    //初始化群落
    public virtual void InitBiome(Vector2Int worldPosition, int seed) {
        this.worldPos = worldPosition;
        this.localPos = new Vector2Int(biomeSize.x / 2, biomeSize.y / 2);
        minPos = new Vector2Int(worldPosition.x - localPos.x, worldPosition.y - localPos.y);
        maxPos = new Vector2Int(worldPosition.x + localPos.x, worldPosition.y + localPos.y);

        InitNoise(seed);
    }

    //初始化噪图数据
    protected virtual void InitNoise(int seed) {
        //群落轮廓
        //outLine?.InitValidate(biomeSize.x, biomeSize.y, seed);
        //outLine?.InitNoise();
    }

    //初始化群落分布噪图
    public void InitDistributionNoise() {
        //distributionNoise?.InitValidate(world.worldSize.x, world.worldSize.y, world.seed);
        //distributionNoise?.InitNoise();
    }

    //点位是否在轮廓内
    public virtual bool isOutLine(int localX, int localY) {
        //if (outLine == null) return true;
        //return outLine.noiseTexture.GetPixel(localX, localY).r > 0.5f;
        return false;
    }

    //目标点是否符合生成条件
    public virtual bool isConformGenerator(Vector2Int targetDot) {
        //if (distributionNoise == null) return true;
        //if (distributionNoise.GetPixel(targetDot.x, targetDot.y).r > 0) {
        //    return true;
        //}
        return false;
    }

    #region 坐标转换
    public int LocalToWorldPosX(int x) {
        return x + minPos.x;
    }
    public int LocalToWorldPosY(int y) {
        return y + minPos.y;
    }

    public Vector2Int LocalToWorldPos(int x, int y) {
        return new Vector2Int(LocalToWorldPosX(x), LocalToWorldPosY(y));
    }
    #endregion
    //执行生成
    public abstract IEnumerator GenerateBiome();

}
