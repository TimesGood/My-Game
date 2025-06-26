using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

//基础地形
[CreateAssetMenu(fileName = "Terrain", menuName = "Terrain/new Terrain")]
public class BaseTerrain : ScriptableObject {

    private WorldGeneration world;

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
        world = WorldGeneration.Instance;

        terrain.InitValidate(world.worldWidth, world.worldHeight, world.seed);
        terrain.heightAdd = 0;
        dirtNoise.InitValidate(world.worldWidth, world.worldHeight, world.seed);
        stoneNoise.InitValidate(world.worldWidth, world.worldHeight, world.seed + 1);
        caveNoise.InitValidate(world.worldWidth, world.worldHeight, world.seed + 2);

        terrain.InitNoise();
        dirtNoise.InitNoise();
        stoneNoise.InitNoise();
        caveNoise.InitNoise();

        //for (int x = 0; x < world.worldWidth; x++) {
        //    for (int y = 0; y < world.worldHeight; y++) {
        //        terrain.Draw(x, y);
        //        dirtNoise.Draw(x, y);
        //        stoneNoise.Draw(x, y);
        //        caveNoise.Draw(x, y);
        //    }
        //}
    }

    public void DestroyNoiseTexture() {
        dirtNoise.DestroyNoiseTexture();
        stoneNoise.DestroyNoiseTexture();
        caveNoise.DestroyNoiseTexture();
    }

//生成
public IEnumerator Generation() {
        int processed = 0;
        for (int x = 0; x < world.worldWidth; x++) {
            int terrianHeight = world.surfaceHeights[x];
            terrianHeight += terrain.GetHeight(x);
            world.surfaceHeights[x] = terrianHeight;
            float stoneHeight = Mathf.PerlinNoise((x + world.seed) * 0.02f, world.seed * 0.02f) * 10f + (world.baseHeight * 0.8f);
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
                    world.SetTileClass(tileClass, tileClass.layer, x, y);
                    //WorldGeneration.Instance.PlaceTile(tileClass, x, y);
                }
            }
            // 每帧处理200串防止卡顿
            if (++processed % 200 == 0) {
                UnityEngine.Debug.Log(Mathf.FloorToInt((float)processed / world.worldWidth * 100) + "%");
                yield return null;
            }
        }
    }
}
