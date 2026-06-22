using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

//基础地形
[CreateAssetMenu(fileName = "Terrain", menuName = "Terrain/new Terrain")]
public class BaseTerrain : ScriptableObject {

    private MapGenerator map;
    private WorldManager world => WorldManager.Instance;

    [Header("地形")]
    public CurveConfig terrain;

    [Header("洞穴")]
    public FBMPerlinNoise caveNoise;
    [Header("泥土")]
    public FBMPerlinNoise dirtNoise;
    public TileClass dirtClass;
    [Header("石头")]
    public FBMPerlinNoise stoneNoise;
    public TileClass stoneClass;
    [Header("地皮")]
    public TileClass grassTile;


    //初始化噪声纹理
    public void InitNoiseTexture() {

        FBMPerlinCurve s = new FBMPerlinCurve();
        terrain.InitValidate(world.worldSize.x, world.worldSize.y, world.seed);
        //terrain.heightMult = 1;
        terrain.heightAdd = 0;
        dirtNoise.InitValidate(world.worldSize.x, world.worldSize.y, world.seed);
        stoneNoise.InitValidate(world.worldSize.x, world.worldSize.y, world.seed + 1);
        caveNoise.InitValidate(world.worldSize.x, world.worldSize.y, world.seed + 2);

        terrain.InitNoise();
        dirtNoise.InitNoise();
        stoneNoise.InitNoise();
        caveNoise.InitNoise();
    }

    public void DestroyNoiseTexture() {
        dirtNoise.DestroyNoiseTexture();
        stoneNoise.DestroyNoiseTexture();
        caveNoise.DestroyNoiseTexture();
    }

//生成
public IEnumerator Generation() {
        world.terrainCurveData = terrain.GetCurveData();
        int processed = 0;
        for (int x = 0; x < this.world.worldSize.x; x++) {
            int terrianHeight = this.world.surfaceHeights[x];
            terrianHeight += (int)(terrain.GetHeight(x));
            this.world.surfaceHeights[x] = terrianHeight;
            float stoneCurve = Mathf.PerlinNoise((x + this.world.seed) * 0.02f, this.world.seed * 0.02f) * 10f;
            this.world.stoneCurveData[x] = stoneCurve;
            float stoneHeight = (this.world.baseHeight * 0.8f) + stoneCurve;
            for (int y = 0; y < terrianHeight; y++) {
                TileClass tileClass = null;

                //地质层
                if (y < stoneHeight) {
                    //补充土层
                    if (dirtNoise.GetPixel(x, y).r > 0.5f) {
                        tileClass = dirtClass;
                    } else {
                        tileClass = stoneClass;
                    }
                } else if (y < terrianHeight - 1) {
                    //补充岩层
                    if (stoneNoise.GetPixel(x, y).r > 0.5f) {
                        tileClass = stoneClass;
                    } else {
                        tileClass = dirtClass;
                    }

                } else {
                    //地皮
                    tileClass = grassTile;
                }


                //洞穴
                if (caveNoise.GetPixel(x, y).r > 0.5f) {
                    this.world.SetTileClass(tileClass, tileClass.layer, x, y);
                    //WorldGeneration.Instance.PlaceTile(tileClass, x, y);
                }
            }
            // 每帧处理200串防止卡顿
            if (++processed % 200 == 0) {
                UnityEngine.Debug.Log(Mathf.FloorToInt((float)processed / this.world.worldSize.x * 100) + "%");
                yield return null;
            }
        }
    }
}
