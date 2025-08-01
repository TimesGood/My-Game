using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using Debug = UnityEngine.Debug;


//液体流动处理
public class LiquidHandler : Singleton<LiquidHandler> {

    public WorldManager world;
    public LiquidLayer liquidLayer;
    public LiquidClass[] liquids;//注册需要处理的液体
    public TileClass test;
    public bool openFlow = false;
    private Dictionary<Vector2Int, float> volume = new Dictionary<Vector2Int, float>();//液体体积
    public Dictionary<LiquidClass, Dictionary<Vector2Int, int>> updates = new Dictionary<LiquidClass, Dictionary<Vector2Int, int>>();//存储要计算液体的区域，由于不同液体流动速度不同，需要对不同液体单独处理
    private HashSet<string> loadingRoutines = new HashSet<string>();//记录正在执行的协程
    private Dictionary<LiquidClass, Coroutine> updateRoutines = new Dictionary<LiquidClass, Coroutine>();

    // 检查稳定的水源移除计算
    private float lastClearUpdateTime;
    private const float checkClearInterval = 3f; // 更新间隔秒

    protected override void Awake() {
        base.Awake();
        //初始化注册液体字典
        foreach (var liquid in liquids) {
            updates.Add(liquid, new Dictionary<Vector2Int, int>());
        }

    }

    // Update is called once per frame
    void Update() {
        UpdateLiquid();
        ScanClearSteadyLiquid();
    }

    //private void UpdateLiquid() {
    //    // 每帧最多处理2种液体
    //    if (!openFlow) return;

    //    // 每帧最多处理2种液体
    //    int processed = 0;
    //    foreach (var kvp in updates) {
    //        if (updateRoutines.ContainsKey(kvp.Key) || processed >= 2)
    //            continue;

    //        //processed++;
    //        updateRoutines[kvp.Key] = StartCoroutine(
    //            HandleLiquidUpdate(kvp.Key, kvp.Value)
    //        );
    //        //foreach (var item in kvp.Value) {
    //        //    StartCoroutine(kvp.Key.CalculatePhysics(item.Key));
    //        //}
    //    }
    //}
    private IEnumerator HandleLiquidUpdate(LiquidClass liquid, Dictionary<Vector2Int, int> updates) {
        Bounds bounds = ChunkHandler.Instance.GetCameraBounds();
        List<Vector2Int> keys = new List<Vector2Int>(updates.Keys);
        int processed = 0;

        // 处理屏幕外区域
        foreach (var item in keys) {
            if (!world.CheckWorldBound(item.x, item.y) || bounds.Contains((Vector3Int)item)) continue;
            //float curVolume = liquidVolume[item.x, item.y];
            float curVolume = GetVolume(item);
            float oldVolume = curVolume;
            bool isChange = liquid.CalculatePhysics(item);
            if (!updates.ContainsKey(item)) continue;
            if (isChange) {
                updates[item] = 0;
            } else {
                updates[item] += 1;
            }

            if (++processed % 5000 == 0) yield return null;
        }

        // 等待流动间隔
        yield return new WaitForSeconds(1f / liquid.flowSpeed);


        processed = 0;
        keys = new List<Vector2Int>(updates.Keys); // 获取最新集合
        //排序再计算，使窗口内的液体流动自然
        keys.Sort((a, b) => { return a.y.CompareTo(b.y); });

        // 处理屏幕内区域
        foreach (var item in keys) {
            if (!bounds.Contains(((Vector3Int)item))) continue;
            //float curVolume = liquidVolume[item.x, item.y];
            float curVolume = GetVolume(item);
            float oldVolume = curVolume;
            bool isChange = liquid.CalculatePhysics(item);
            if (!updates.ContainsKey(item)) continue;
            if (isChange) {
                updates[item] = 0;
            } else {
                updates[item] += 1;
            }

            if (++processed % 5000 == 0) yield return null;
        }

        updateRoutines.Remove(liquid);
    }

    //第二个方案，支持屏幕前与屏幕后不同的液体处理速度。如果希望加快液体在屏幕后的处理速度，使用该方案
    private void UpdateLiquid() {
        Bounds bounds = ChunkHandler.Instance.GetCameraBounds();
        foreach (var kvp in updates) {
            List<Vector2Int> outUpdates = new List<Vector2Int>();
            List<Vector2Int> inUpdates = new List<Vector2Int>();
            foreach (var inKvp in kvp.Value) {

                if (bounds.Contains((Vector3Int)inKvp.Key)) {
                    inUpdates.Add(inKvp.Key);
                } else {
                    outUpdates.Add(inKvp.Key);
                }
            }

            //可见区域的液体计算
            if (!loadingRoutines.Contains(kvp.Key.name + "in")) {
                loadingRoutines.Add(kvp.Key.name + "in");
                StartCoroutine(HandlerVisibleIn(bounds, kvp.Key, inUpdates, kvp.Value));
            }

            //不可见区域的液体计算
            if (!loadingRoutines.Contains(kvp.Key.name + "out")) {
                loadingRoutines.Add(kvp.Key.name + "out");
                StartCoroutine(HandlerVisibleOut(bounds, kvp.Key, outUpdates, kvp.Value));
            }
        }

    }

    private IEnumerator HandlerVisibleIn(Bounds bounds, LiquidClass liquid, List<Vector2Int> inUpdate, Dictionary<Vector2Int, int> updates) {
        yield return new WaitForSeconds(1f / liquid.flowSpeed);
        //排序在计算，这样水流自然一点
        inUpdate.Sort((a, b) => {
            return a.y.CompareTo(b.y);
        });

        foreach (var item in inUpdate) {
            //float curVolume = liquidVolume[item.x, item.y];
            float curVolume = GetVolume(item);
            float oldVolume = curVolume;
            bool isChange = liquid.CalculatePhysics(item);
            if (!updates.ContainsKey(item)) continue;
            if (isChange) {
                updates[item] = 0;
            } else {
                updates[item] += 1;
            }
        }
        loadingRoutines.Remove(liquid.name + "in");
    }


    //计算可视范围外的液体
    private IEnumerator HandlerVisibleOut(Bounds bounds, LiquidClass liquid, List<Vector2Int> outUpdates, Dictionary<Vector2Int, int> updates) {

        int processed = 0;
        //可视范围外的液体体积计算
        foreach (var item in outUpdates) {
            if (!world.CheckWorldBound(item.x, item.y)) continue;
            if (bounds.Contains((Vector3Int)item)) continue;//只处理屏幕外的
            //float curVolume = liquidVolume[item.x, item.y];
            float curVolume = GetVolume(item);
            float oldVolume = curVolume;
            bool isChange = liquid.CalculatePhysics(item);
            if (!updates.ContainsKey(item)) continue;
            if (isChange) {
                updates[item] = 0;
            } else {
                updates[item] += 1;
            }

            // 每帧处理1000个瓦片防止卡顿
            if (++processed % 1000 == 0) {
                yield return null;
            }

        }
        loadingRoutines.Remove(liquid.name + "out");
    }

    //扫描清理稳定状态液体
    private void ScanClearSteadyLiquid() {
        // 使用固定间隔检查，避免每帧都检查
        if (Time.time - lastClearUpdateTime > checkClearInterval) {
            
            //检查，如果液体不变次数超过一定次数，判定此液体处于稳定状态
            foreach (var kvp in updates) {
                foreach (var key in kvp.Value.ToList()) {
                    kvp.Value.TryGetValue(key.Key, out int num);
                    if (num > 50) {
                        kvp.Value.Remove(key.Key);
                    }
                }
                Debug.Log(kvp.Key.name + "：" + kvp.Value.Count);
            }
            
            
            lastClearUpdateTime = Time.time;
        }
    }

    //更新液体体积
    public void UpdateVolume(LiquidClass liquid, Vector2Int pos, float volume) {
        //liquidVolume[pos.x, pos.y] = volume;
        SetVolume(pos, volume);

        if (volume == 0) {
            world.SetTileClass(null, Layers.Liquid, pos.x, pos.y);
            RemoveForUpdate(liquid, pos);
            
        } else {
            world.SetTileClass(liquid, Layers.Liquid, pos.x, pos.y);
            MarkForUpdate(liquid, pos);
        }
        UpdateTile(liquid, pos, volume);
    }

    //更新瓦片
    private void UpdateTile(LiquidClass liquid, Vector2Int pos, float volume) {
        Vector2Int ChunkID = ChunkHandler.Instance.WorldToChunkCoord(pos);
        //不在区块内，不进行渲染
        if (!ChunkHandler.Instance.loadedChunkIDs.Contains(ChunkID)) return;
        TileBase newTile = liquid.GetTileToVolume(volume);
        liquidLayer._tilemap.SetTile((Vector3Int)pos, newTile);
    }

    //标记指定位置
    public void MarkForUpdate(LiquidClass liquid, Vector2Int pos) {
        if (!world.CheckWorldBound(pos.x, pos.y)) return;

        if (!updates.TryGetValue(liquid, out var set)) {
            throw new Exception("液体" + liquid.name + "未进行注册！");
        }

        if (!set.ContainsKey(pos)) {
            set.Add(pos, 0);
        } else {
            set[pos] = 0;
        }
    }
    //删除标记
    public void RemoveForUpdate(LiquidClass liquid, Vector2Int pos) {
        if (!world.CheckWorldBound(pos.x, pos.y)) return;

        if (!updates.TryGetValue(liquid, out var set)) {
            throw new Exception("液体" + liquid.name + "未进行注册！");
        }
        set.Remove(pos);
    }

    //校验某液体指定标记是否已存在
    public bool CheckMarkForUpdate(LiquidClass liquid, Vector2Int pos) {
        if (!updates.TryGetValue(liquid, out var set)) {
            throw new Exception("液体" + liquid.name + "未进行注册！");
        }
        return set.ContainsKey(pos);
    }


    public void SetVolume(Vector2Int pos, float volume) {
        liquidLayer.SetVolume((Vector3Int) pos, volume);
    }

    public float GetVolume(Vector2Int pos) {

        return liquidLayer.GetVolume((Vector3Int)pos); ;

    }
}
