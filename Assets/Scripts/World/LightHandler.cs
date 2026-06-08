using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using static WorldManager;
using Debug = UnityEngine.Debug;

//光源处理
public class LightHandler : Singleton<LightHandler>
{
    public WorldManager world;
    public float[,] lightValues;//记录发光瓦片瓦片的亮度数据
    public readonly float sunlight = 15f;//太阳光照
    public Texture2D lightTex;//光照材质
    public Material lightMap;
    public bool updating;//是否正在更新光源
    public Queue<Vector2Int> updates = new Queue<Vector2Int>();//存储要更新的光源的区域
    public SpriteRenderer spriteRenderer;
    private Color defaultColor;

    protected override void Awake() {
        base.Awake();
        lightValues = new float[world.worldSize.x, world.worldSize.y];

        defaultColor = spriteRenderer.color;
        spriteRenderer.color = new Color(255, 255, 255, 0);

        lightTex = new Texture2D(world.worldSize.x, world.worldSize.y);
        lightTex.filterMode = FilterMode.Point;

        //放大到覆盖地图大小
        transform.localScale = new Vector3(world.worldSize.x, world.worldSize.y, 1);
        //位置
        transform.localPosition = new Vector3(world.worldSize.x / 2f, world.worldSize.y / 2f, 0);

    }
    private void Update() {
        if (!updating && updates.Count > 0) {
            updating = true;
            StartCoroutine(LightUpdate(updates.Dequeue()));
        }
    }

    public void InitLight()
    {
        StartCoroutine(UpdateScopeLight(0, world.worldSize.x - 1, 0, world.worldSize.y - 1));
    }

    IEnumerator LightUpdate(Vector2Int pos) {
        //更新该光源一定范围的光亮
        int px1 = Mathf.Clamp(pos.x - (int)sunlight, 0, world.worldSize.x - 1);//最左
        int px2 = Mathf.Clamp(pos.x + (int)sunlight, 0, world.worldSize.x - 1);//最右
        int py1 = Mathf.Clamp(pos.y - (int)sunlight, 0, world.worldSize.y - 1);//最下
        int py2 = Mathf.Clamp(pos.y + (int)sunlight, 0, world.worldSize.y - 1);//最上

        for (int x = px1; x <= px2; x++) {
            for (int y = py1; y <= py2; y++) {
                lightValues[x, y] = 0;
            }
        }

        UpdateScopeLight(px1, px2, py1, py2);
        yield return null;
        updating = false;
    }

    //更新指定范围内的光源
    private IEnumerator UpdateScopeLight(int minX, int maxX, int minY, int maxY)
    {
        Debug.Log("渲染中1...");
        int processed = 0;
        //左下角开始渲染光照
        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                float lightValue = 0f;
                lightValue = GetLightValue(x, y);
                else if (world.GetTileClass(Layers.Background, x, y) == null && world.GetTileClass(Layers.Ground, x, y) == null)
                {
                    lightValue = sunlight;
                }
                else
                {
                    //获取四周光亮值
                    int nx1 = Mathf.Clamp(x - 1, 0, world.worldSize.x - 1);
                    int nx2 = Mathf.Clamp(x + 1, 0, world.worldSize.x - 1);
                    int ny1 = Mathf.Clamp(y - 1, 0, world.worldSize.y - 1);
                    int ny2 = Mathf.Clamp(y + 1, 0, world.worldSize.y - 1);
                    //取最亮的一边
                    lightValue = Mathf.Max(lightValues[x, ny1], lightValues[x, ny2], lightValues[nx1, y], lightValues[nx2, y]);

                    //光照锐减
                    if (world.GetTileClass(Layers.Ground, x, y) == null)
                    {
                        lightValue -= 1f;
                    }
                    else
                    {
                        lightValue -= 2.5f;
                    }
                }
                lightValue = Mathf.Clamp(lightValue, 0, sunlight);
                lightValues[x, y] = lightValue;

                //每帧处理【】个，防止卡顿
                if (++processed % 10000 == 0) {
                    Debug.Log("1");
                    yield return null;
                }
            }
        }
        Debug.Log("渲染中2...");
        //右上角开始渲染光照
        for (int x = maxX; x >= minX; x--)
        {
            for (int y = maxY; y >= minY; y--)
            {
                float lightValue = 0f;
                if (GetLightValue(x, y) != 0)
                {
                    lightValue = GetLightValue(x, y);
                }
                else if (world.GetTileClass(Layers.Background, x, y) == null && world.GetTileClass(Layers.Ground, x, y) == null)
                {
                    lightValue = sunlight;
                }
                else
                {
                    //获取四周光亮值
                    int nx1 = Mathf.Clamp(x - 1, 0, world.worldSize.x - 1);
                    int nx2 = Mathf.Clamp(x + 1, 0, world.worldSize.x - 1);
                    int ny1 = Mathf.Clamp(y - 1, 0, world.worldSize.y - 1);
                    int ny2 = Mathf.Clamp(y + 1, 0, world.worldSize.y - 1);
                    //取最亮的一边
                    lightValue = Mathf.Max(lightValues[x, ny1], lightValues[x, ny2], lightValues[nx1, y], lightValues[nx2, y]);

                    //光照锐减
                    if (world.GetTileClass(Layers.Ground, x, y) == null)
                    {
                        lightValue -= 1f;
                    }
                    else
                    {
                        lightValue -= 2.5f;
                    }
                }
                lightValue = Mathf.Clamp(lightValue, 0, sunlight);
                lightValues[x, y] = lightValue;

                //每帧处理【】个，防止卡顿
                if (++processed % 10000 == 0) {
                    yield return null;
                }
            }
        }

        Debug.Log("应用光照");

        //应用亮度
        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                lightTex.SetPixel(x, y, new Color(0, 0, 0, 1f - lightValues[x, y] / sunlight));
                //每帧处理【】个，防止卡顿
                if (++processed % 5000 == 0) {
                    yield return null;
                }
            }
        }

        spriteRenderer.color = defaultColor;
        lightTex.Apply();
        lightMap.SetTexture("_LightMap", lightTex);
    }


    //标记需要更新光源的地方
    public void MarForUpdate(int x, int y)
    {
        updates.Enqueue(new Vector2Int(x, y));
    }


    public float GetLightValue(int x, int y) {
        float lightValue = 0;
        TileData tile = ChunkManager.Instance.GetTileData(x, y);

        // 检查每个图层的图块是否发光
        Layers[] layers = (Layers[])Enum.GetValues(typeof(Layers));
        foreach (var layer in layers) {
            long blockId = tile.GetBlockId(layer);
            TileClass tileClass = TileRegistry.GetTile(blockId);
            if (tileClass == null) continue;
            if (tileClass.lightLevel > lightValue)
                lightValue = tileClass.lightLevel;
        }
        return lightValue;
    }
}
