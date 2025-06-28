using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Tilemaps;
using static ChunkHandler;
using static UnityEditor.PlayerSettings;


//液体流动处理
public class LiquidHandler : Singleton<LiquidHandler> {

    public WorldGeneration world;
    public LiquidClass[] liquids;//注册需要处理的液体
    public bool openFlow = false;
    public float[,] liquidVolume { get; set; }//记录液体瓦片的体积数据
    public Dictionary<LiquidClass, Dictionary<Vector2Int, int>> updates = new Dictionary<LiquidClass, Dictionary<Vector2Int, int>>();//存储要计算液体的区域，由于不同液体流动速度不同，需要对不同液体单独处理
    private Dictionary<LiquidClass, Coroutine> updateRoutines = new Dictionary<LiquidClass, Coroutine>();
    private Dictionary<LiquidClass, Coroutine> backUpdateRoutines = new Dictionary<LiquidClass, Coroutine>();

    // 检查稳定的水源移除计算
    private float lastCheckUpdateTime;
    private const float checkUpdateInterval = 1f; // 更新间隔

    protected override void Awake() {
        base.Awake();
        //初始化液体存储
        liquidVolume = new float[world.worldWidth, world.worldHeight];
        //初始化注册液体字典
        foreach (var liquid in liquids) {
            updates.Add(liquid, new Dictionary<Vector2Int, int>());
        }

    }

    // Update is called once per frame
    void Update() {
        UpdateLiquid();
    }

    private void LateUpdate() {
        Bounds bounds = ChunkHandler.Instance.GetCameraBounds();
        foreach (var kvp in updates) {
            Render(bounds, kvp.Value);
        }
    }

    private void UpdateLiquid() {
        // 每帧最多处理2种液体
        if (!openFlow) return;

        // 每帧最多处理2种液体
        int processed = 0;
        foreach (var kvp in updates) {
            if (updateRoutines.ContainsKey(kvp.Key) || processed >= 2)
                continue;

            processed++;
            updateRoutines[kvp.Key] = StartCoroutine(
                HandleLiquidUpdate(kvp.Key, kvp.Value)
            );
            ScanClearSteadyLiquid(kvp.Value);
        }


    }
    private IEnumerator HandleLiquidUpdate(LiquidClass liquid, Dictionary<Vector2Int, int> updates) {
        Bounds bounds = ChunkHandler.Instance.GetCameraBounds();
        List<Vector2Int> keys = new List<Vector2Int>(updates.Keys);
        int processed = 0;

        // 处理屏幕外区域
        foreach (var item in keys) {
            if (!world.CheckWorldBound(item.x, item.y) || bounds.Contains((Vector3Int)item)) continue;
            float curVolume = liquidVolume[item.x, item.y];
            float oldVolume = curVolume;
            ProcessLiquidCell(liquid, item, ref curVolume);
            if (!updates.ContainsKey(item)) continue;
            if (curVolume == oldVolume) {
                updates[item] += 1;
            } else {
                updates[item] = 0;
            }

            if (++processed % 1000 == 0) yield return null;
        }

        // 等待流动间隔
        yield return new WaitForSeconds(liquid.flowSpeed);


        processed = 0;
        keys = new List<Vector2Int>(updates.Keys); // 获取最新集合
        //排序再计算，使窗口内的液体流动自然
        keys.Sort((a, b) => { return a.y.CompareTo(b.y); });

        // 处理屏幕内区域
        foreach (var item in keys) {
            if (!bounds.Contains(((Vector3Int)item))) continue;
            float curVolume = liquidVolume[item.x, item.y];
            float oldVolume = curVolume;
            ProcessLiquidCell(liquid, item, ref curVolume);
            //记录如果液体体积无变化，计数器+1，否则归零
            if (!updates.ContainsKey(item)) continue;
            if (curVolume == oldVolume) {
                updates[item] += 1;
            } else {
                updates[item] = 0;
            }

            if (++processed % 1000 == 0) yield return null;
        }

        updateRoutines.Remove(liquid);
    }

    //第二个方案，支持屏幕前与屏幕后不同的液体处理速度。如果希望加快液体在屏幕后的处理速度，使用该方案
    //private void UpdateLiquid() {
    //    Bounds bounds = ChunkHandler.Instance.GetCameraBounds();
    //    foreach (var kvp in updates) {
    //        List<Vector2Int> outUpdates = new List<Vector2Int>();
    //        List<Vector2Int> inUpdates = new List<Vector2Int>();
    //        foreach (var inKvp in kvp.Value) {

    //            if (bounds.Contains((Vector3Int)inKvp.Key)) {
    //                inUpdates.Add(inKvp.Key);
    //            } else {
    //                outUpdates.Add(inKvp.Key);
    //            }
    //        }
    //        //可见区域的液体计算
    //        Coroutine updateRoutine;
    //        updateRoutines.TryGetValue(kvp.Key, out updateRoutine);
    //        if (updateRoutine == null)
    //            updateRoutines[kvp.Key] = StartCoroutine(HandlerVisibleIn(bounds, kvp.Key, inUpdates, kvp.Value));

    //        //不可见区域的液体计算
    //        Coroutine backUpdateRoutine;
    //        backUpdateRoutines.TryGetValue(kvp.Key, out backUpdateRoutine);
    //        if (backUpdateRoutine == null) {
    //            backUpdateRoutines[kvp.Key] = StartCoroutine(HandlerVisibleOut(bounds, kvp.Key, outUpdates, kvp.Value));

    //        }

    //        ScanClearSteadyLiquid(kvp.Value);
    //    }

    //}

    //private IEnumerator HandlerVisibleIn(Bounds bounds, LiquidClass liquid, List<Vector2Int> inUpdate, Dictionary<Vector2Int, int> updates) {
    //    yield return new WaitForSeconds(liquid.flowSpeed);
    //    
    //    int processed = 0;
    //    //排序在计算，这样水流自然一点
    //    inUpdate.Sort((a, b) => {
    //        return a.y.CompareTo(b.y);
    //    });

    //    foreach (var item in inUpdate) {
    //        float curVolume = liquidVolume[item.x, item.y];
    //        float oldVolume = curVolume;
    //        ProcessLiquidCell(liquid, item, ref curVolume);
    //        if (!updates.ContainsKey(item)) continue;
    //        if (curVolume == oldVolume) {
    //            updates[item] += 1;
    //        } else {
    //            updates[item] = 0;
    //        }
    //        // 每帧处理1000个瓦片防止卡顿
    //        if (++processed % 1000 == 0) {
    //            yield return null;
    //        }
    //    }
    //    updateRoutines.Remove(liquid);
    //}


    ////计算可视范围外的液体
    //private IEnumerator HandlerVisibleOut(Bounds bounds, LiquidClass liquid, List<Vector2Int> outUpdates, Dictionary<Vector2Int, int> updates) {


    //    int processed = 0;
    //    //可视范围外的液体体积计算
    //    foreach (var item in outUpdates) {
    //        if (!world.CheckWorldBound(item.x, item.y)) continue;
    //        if (bounds.Contains((Vector3Int)item)) continue;//只处理屏幕外的
    //        float curVolume = liquidVolume[item.x, item.y];
    //        float oldVolume = curVolume;
    //        ProcessLiquidCell(liquid, item, ref curVolume);
    //        if (!updates.ContainsKey(item)) continue;
    //        if (curVolume == oldVolume) {
    //            updates[item] += 1;
    //        } else {
    //            updates[item] = 0;
    //        }

    //        // 每帧处理1000个瓦片防止卡顿
    //        if (++processed % 1000 == 0) {
    //            yield return null;
    //        }

    //    }

    //    backUpdateRoutines.Remove(liquid);
    //}

    //扫描清理稳定状态液体
    private void ScanClearSteadyLiquid(Dictionary<Vector2Int, int> updates) {
        // 使用固定间隔更新，避免每帧都检查
        if (Time.time - lastCheckUpdateTime > checkUpdateInterval) {
            //检查，如果液体不变次数超过一定次数，判定此液体处于稳定状态
            foreach (var key in updates.ToList()) {
                updates.TryGetValue(key.Key, out int num);
                if (num > 5) {
                    updates.Remove(key.Key);
                }
            }
            Debug.Log(updates.Count);
            lastCheckUpdateTime = Time.time;
        }

    }

    //渲染区域
    private void Render(Bounds bounds, Dictionary<Vector2Int, int> updates) {
        List<Vector2Int> toRemove = new List<Vector2Int>();
        Tilemap liquidMap = world.tilemaps[(int)Layers.Liquid];

        //渲染可视范围的液体瓦片
        for (int y = (int)bounds.min.y; y < bounds.max.y; y++) {
            for (int x = (int)bounds.min.x; x < bounds.max.x; x++) {
                Vector3Int worldPos = new Vector3Int(x, y);

                LiquidClass liquidClass = (LiquidClass)world.GetTileClass(Layers.Liquid, x, y);
                TileBase oldTile = world.tilemaps[(int)Layers.Liquid].GetTile(worldPos);
                if (liquidClass != null) {

                    float volume = liquidVolume[x, y];
                    //有时候由于大量液体在空中导致个别液体无法做液体运动，这里渲染时检查一下是否有异常空中液体，重新激活该液体
                    Vector3Int downPos = worldPos + Vector3Int.down;
                    TileClass downGroundClass = world.GetTileClass(Layers.Ground, downPos.x, downPos.y);
                    TileClass downLiquidClass = world.GetTileClass(Layers.Liquid, downPos.x, downPos.y);
                    if (!updates.ContainsKey((Vector2Int)worldPos) && downGroundClass == null && downLiquidClass == null)
                        MarkForUpdate(liquidClass, (Vector2Int)worldPos);

                    //如果渲染前新旧液体瓦片一致，不需要再次渲染跳过
                    TileBase newTile = liquidClass.GetTileToVolume(volume);
                    if (newTile == oldTile) continue;
                    world.tilemaps[(int)Layers.Liquid].SetTile(worldPos, newTile);
                } else {
                    if (oldTile == null) continue;
                    world.tilemaps[(int)Layers.Liquid].SetTile(worldPos, null);
                }
            }
        }
    }

    // 核心处理逻辑
    private void ProcessLiquidCell(LiquidClass liquid, Vector2Int pos, ref float curVolume) {
        int x = pos.x;
        int y = pos.y;

        //体积太小时，擦掉该瓦片
        if (curVolume < liquid.minVolume) {
            UpdateVolume(null, pos, 0);
            MarkForUpdate(liquid, pos);
            return;
        }
        //液体在地面瓦片中，擦掉
        if (world.GetTileClass(Layers.Ground, x, y) != null) {
            UpdateVolume(null, pos, 0);
            MarkForUpdate(liquid, pos);
            return;
        }
        // 优先向下流动
        if (TryFlowDown(liquid, pos, ref curVolume)) return;
        // 扩散处理
        if (HandlerDiffusion(liquid, pos, ref curVolume)) return;


        //液体溢出
        if (HandlerOverflow(liquid, pos, curVolume)) curVolume = 1f; ;


    }

    // 尝试向下流动（返回是否成功流动）
    private bool TryFlowDown(LiquidClass liquid, Vector2Int pos, ref float curVolume) {
        int x = pos.x;
        int y = pos.y;
        if (y <= 0) return false;

        Vector2Int downPos = pos + Vector2Int.down;
        // 检查下方是否可流动
        if (world.GetTileClass(Layers.Ground, downPos.x, downPos.y) != null) return false;
        //液体满了
        float downVolume = liquidVolume[downPos.x, downPos.y];
        if (downVolume >= 1f) return false;
        //液体不一致
        LiquidClass downLiquid = world.GetTileClass(Layers.Liquid, downPos.x, downPos.y) as LiquidClass;
        if (downLiquid != null && downLiquid != liquid)
            return false;
        downVolume += curVolume;
        curVolume = 0;

        UpdateVolume(liquid, pos, curVolume);
        MarkForUpdate(liquid, pos);

        UpdateVolume(liquid, downPos, downVolume);
        MarkForUpdate(liquid, downPos);

        //可能周围有稳定状态液体，重新激活上左右液体液体
        MarkForUpdate(liquid, pos + Vector2Int.up);
        //MarkForUpdate(liquid, pos + Vector2Int.left);
        //MarkForUpdate(liquid, pos + Vector2Int.right);
        return true;
    }


    // 扩散处理
    private bool HandlerDiffusion(LiquidClass liquid, Vector2Int pos, ref float curVolume) {
        int x = pos.x;
        int y = pos.y;
        List<Vector2Int> flowDirs = new List<Vector2Int>();

        // 检测可用流动方向
        CheckFlowDirection(x - 1, y, liquid, curVolume, ref flowDirs); // 左
        CheckFlowDirection(x + 1, y, liquid, curVolume, ref flowDirs); // 右
        if (flowDirs.Count == 0) return false;
        // 计算每个方向的分配量
        float avg = curVolume;
        foreach (var item in flowDirs) {
            avg += liquidVolume[item.x, item.y];
        }
        avg /= (flowDirs.Count + 1);

        //avg = Mathf.Round(avg * 10000f) / 10000f;
        curVolume = avg;
        UpdateVolume(liquid, pos, curVolume);
        MarkForUpdate(liquid, pos);
        if(HandlerOverflow(liquid, pos, curVolume)) curVolume = 1f;
        foreach (var dir in flowDirs) {
            UpdateVolume(liquid, dir, avg);
            MarkForUpdate(liquid, dir);
            HandlerOverflow(liquid, dir, avg);
        }

        //可能周围有稳定状态液体，重新激活上左右液体液体
        MarkForUpdate(liquid, pos + Vector2Int.up);
        MarkForUpdate(liquid, pos + Vector2Int.left);
        MarkForUpdate(liquid, pos + Vector2Int.right);
        return true;
    }

    //溢出处理
    private bool HandlerOverflow(LiquidClass liquid, Vector2Int pos, float curVolume) {
        if (curVolume <= 1f) return false;
        //液体溢出
        Vector2Int upPos = pos + Vector2Int.up;
        float upVolume = liquidVolume[upPos.x, upPos.y];
        upVolume += curVolume - 1f;
        UpdateVolume(liquid, upPos, upVolume);
        MarkForUpdate(liquid, upPos);

        curVolume = 1f;
        UpdateVolume(liquid, pos, curVolume);
        MarkForUpdate(liquid, pos);
        return true;
        
    }

    // 检查流动方向是否有效
    private void CheckFlowDirection(int x, int y, LiquidClass liquid, float curVolume, ref List<Vector2Int> dirs) {
        if (!world.CheckWorldBound(x, y)) return;
        // 在CheckFlowDirection中添加：
        TileClass targetLiquid = world.GetTileClass(Layers.Liquid, x, y);
        if (targetLiquid != null && targetLiquid != liquid)
            return;
        float targetVolume = liquidVolume[x, y];
        if (world.GetTileClass(Layers.Ground, x, y) != null || targetVolume >= curVolume) return;
        //两边液体体积相差无几，不扩散，避免水体表面一直在计算
        if (curVolume - targetVolume < 0.0001f) return;
        dirs.Add(new Vector2Int(x, y));
    }


    //更新液体体积
    private void UpdateVolume(LiquidClass liquid, Vector2Int pos, float volume) {
        liquidVolume[pos.x, pos.y] = volume;
        world.SetTileClass(liquid, Layers.Liquid, pos.x, pos.y);
    }

    //标记要计算的区域
    public void MarkForUpdate(LiquidClass liquid, Vector2Int pos) {
        if (!world.CheckWorldBound(pos.x, pos.y)) return;

        if (!updates.TryGetValue(liquid, out var set)) {
            throw new Exception("液体" + liquid.name + "未进行注册！");
        }

        if (!set.ContainsKey(pos)) {
            set.Add(pos, 0);
        }
    }
}
