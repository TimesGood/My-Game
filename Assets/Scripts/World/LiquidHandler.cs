using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using static ChunkHandler;
using Debug = UnityEngine.Debug;


//液体流动处理
public class LiquidHandler : Singleton<LiquidHandler> {

    public WorldGeneration world;
    public LiquidClass[] liquids;//注册需要处理的液体
    public TileClass test;
    public bool openFlow = false;
    public float[,] liquidVolume { get; set; }//记录液体瓦片的体积数据
    public Dictionary<LiquidClass, Dictionary<Vector2Int, int>> updates = new Dictionary<LiquidClass, Dictionary<Vector2Int, int>>();//存储要计算液体的区域，由于不同液体流动速度不同，需要对不同液体单独处理
    private Dictionary<LiquidClass, Coroutine> updateRoutines = new Dictionary<LiquidClass, Coroutine>();
    private Dictionary<LiquidClass, Coroutine> backUpdateRoutines = new Dictionary<LiquidClass, Coroutine>();

    // 检查稳定的水源移除计算
    private float lastClearUpdateTime;
    private const float checkClearInterval = 3f; // 更新间隔秒

    // 检查错误清理的稳定水源
    private float lastErrClearUpdateTime;
    private const float checkErrClearInterval = 1f;

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
        ScanClearSteadyLiquid();
        //ScanErrSteadyLiquid();
    }

    private void LateUpdate() {
        //Bounds bounds = ChunkHandler.Instance.GetCameraBounds();
        //foreach (var kvp in updates) {
        //    StartCoroutine(Render(bounds, kvp.Value));
        //}
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
            float curVolume = liquidVolume[item.x, item.y];
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
            float curVolume = liquidVolume[item.x, item.y];
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
            if (!updateRoutines.ContainsKey(kvp.Key)) {
                updateRoutines[kvp.Key] = StartCoroutine(HandlerVisibleIn(bounds, kvp.Key, inUpdates, kvp.Value));
            }

            //不可见区域的液体计算
            if (!backUpdateRoutines.ContainsKey(kvp.Key)) {
                backUpdateRoutines[kvp.Key] = StartCoroutine(HandlerVisibleOut(bounds, kvp.Key, outUpdates, kvp.Value));
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
            float curVolume = liquidVolume[item.x, item.y];
            float oldVolume = curVolume;
            bool isChange = liquid.CalculatePhysics(item);
            if (!updates.ContainsKey(item)) continue;
            if (isChange) {
                updates[item] = 0;
            } else {
                updates[item] += 1;
            }
        }
        updateRoutines.Remove(liquid);
    }


    //计算可视范围外的液体
    private IEnumerator HandlerVisibleOut(Bounds bounds, LiquidClass liquid, List<Vector2Int> outUpdates, Dictionary<Vector2Int, int> updates) {
        yield return null;
        int processed = 0;
        //可视范围外的液体体积计算
        foreach (var item in outUpdates) {
            if (!world.CheckWorldBound(item.x, item.y)) continue;
            if (bounds.Contains((Vector3Int)item)) continue;//只处理屏幕外的
            float curVolume = liquidVolume[item.x, item.y];
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
        backUpdateRoutines.Remove(liquid);
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
    //扫描错误清理的稳定液体，重新进入计算
    private void ScanErrSteadyLiquid() {
        // 使用固定间隔检查，避免每帧都检查
        if (Time.time - lastErrClearUpdateTime > checkErrClearInterval) {
            ChunkHandler chunkHandler = ChunkHandler.Instance;
            List<Vector2Int> chunkToLoad = chunkHandler.GetCenterLoadChunk();
            foreach (var chunkID in chunkToLoad) {
                ChunkData chunkData = chunkHandler.GetChunkData(chunkID.x, chunkID.y);
                List<Vector2Int> tilePos = chunkData.tilePos;
            
                for (int i = 0; i < tilePos.Count; i++) {
                    Vector2Int pos = tilePos[i];
                    LiquidClass liquidClass = (LiquidClass)world.GetTileClass(Layers.Liquid, pos.x, pos.y);
                    if (liquidClass != null) {

                        float volume = liquidVolume[pos.x, pos.y];
                        //有时候由于大量液体在空中导致个别液体无法做液体运动，这里渲染时检查一下是否有异常空中液体，重新激活该液体
                        Vector2Int downPos = pos + Vector2Int.down;
                        TileClass downGroundClass = world.GetTileClass(Layers.Ground, downPos.x, downPos.y);
                        TileClass downLiquidClass = world.GetTileClass(Layers.Liquid, downPos.x, downPos.y);
                        updates.TryGetValue(liquidClass, out Dictionary<Vector2Int, int> liquidUpdates);
                        if (!liquidUpdates.ContainsKey(pos) && downGroundClass == null && downLiquidClass == null)
                            MarkForUpdate(liquidClass, pos);

                    }

                }
            }

            lastErrClearUpdateTime = Time.time;

        }
    }

    //渲染区域
    private IEnumerator Render(Bounds bounds, Dictionary<Vector2Int, int> updates) {
        //List<Vector2Int> toRemove = new List<Vector2Int>();
        //Tilemap liquidMap = world.tilemaps[(int)Layers.Liquid];

        int processed = 0;
        ////渲染可视范围的液体瓦片
        //for (int y = (int)bounds.min.y; y < bounds.max.y; y++) {
        //    for (int x = (int)bounds.min.x; x < bounds.max.x; x++) {
        //        Vector3Int worldPos = new Vector3Int(x, y);

        //        LiquidClass liquidClass = (LiquidClass)world.GetTileClass(Layers.Liquid, x, y);
        //        TileBase oldTile = world.tilemaps[(int)Layers.Liquid].GetTile(worldPos);
        //        if (liquidClass != null) {

        //            float volume = liquidVolume[x, y];
        //            //有时候由于大量液体在空中导致个别液体无法做液体运动，这里渲染时检查一下是否有异常空中液体，重新激活该液体
        //            Vector3Int downPos = worldPos + Vector3Int.down;
        //            TileClass downGroundClass = world.GetTileClass(Layers.Ground, downPos.x, downPos.y);
        //            TileClass downLiquidClass = world.GetTileClass(Layers.Liquid, downPos.x, downPos.y);
        //            if (!updates.ContainsKey((Vector2Int)worldPos) && downGroundClass == null && downLiquidClass == null)
        //                MarkForUpdate(liquidClass, (Vector2Int)worldPos);

        //            //如果渲染前新旧液体瓦片一致，不需要再次渲染跳过
        //            TileBase newTile = liquidClass.GetTileToVolume(volume);
        //            if (newTile == oldTile) continue;
        //            world.tilemaps[(int)Layers.Liquid].SetTile(worldPos, newTile);
        //        } else {
        //            if (oldTile == null) continue;
        //            world.tilemaps[(int)Layers.Liquid].SetTile(worldPos, null);
        //        }

        //        if (++processed % 100 == 0)
        //            yield return null;
        //    }
        //}
        ChunkHandler chunkHandler = ChunkHandler.Instance;
        List<Vector2Int> chunkToLoad = chunkHandler.GetCenterLoadChunk();
        foreach (var chunkID in chunkToLoad) {
            ChunkData chunkData = chunkHandler.GetChunkData(chunkID.x, chunkID.y);
            List<Vector2Int> tilePos = chunkData.tilePos;
            List<TileBase> tileBases = chunkData.tileBases[(int)Layers.Liquid];

            for (int i = 0; i < tilePos.Count; i++) {
                Vector2Int pos = tilePos[i];

                LiquidClass liquidClass = (LiquidClass)world.GetTileClass(Layers.Liquid, pos.x, pos.y);
                TileBase oldTile = world.tilemaps[(int)Layers.Liquid].GetTile((Vector3Int)pos);
                if (liquidClass != null) {

                    float volume = liquidVolume[pos.x, pos.y];
                    //有时候由于大量液体在空中导致个别液体无法做液体运动，这里渲染时检查一下是否有异常空中液体，重新激活该液体
                    Vector2Int downPos = pos + Vector2Int.down;
                    TileClass downGroundClass = world.GetTileClass(Layers.Ground, downPos.x, downPos.y);
                    TileClass downLiquidClass = world.GetTileClass(Layers.Liquid, downPos.x, downPos.y);
                    if (!updates.ContainsKey(pos) && downGroundClass == null && downLiquidClass == null)
                        MarkForUpdate(liquidClass, pos);

                    //如果渲染前新旧液体瓦片一致，不需要再次渲染跳过
                    TileBase newTile = liquidClass.GetTileToVolume(volume);
                    if (newTile == oldTile) continue;
                    world.tilemaps[(int)Layers.Liquid].SetTile((Vector3Int)pos, newTile);
                } else {
                    if (oldTile == null) continue;
                    world.tilemaps[(int)Layers.Liquid].SetTile((Vector3Int)pos, null);
                }

                if (++processed % 100 == 0)
                    yield return null;
            }
        }

    }


    //更新液体体积
    public void UpdateVolume(LiquidClass liquid, Vector2Int pos, float volume) {
        liquidVolume[pos.x, pos.y] = volume;
        world.SetTileClass(liquid, Layers.Liquid, pos.x, pos.y);
    }

    //更新瓦片
    public void UpdateTile(LiquidClass liquid, Vector2Int pos, float volume) {
        Vector2Int ChunkID = ChunkHandler.Instance.WorldToChunkCoord(pos);
        if (!ChunkHandler.Instance.loadedChunkIDs.Contains(ChunkID)) return;
        TileBase newTile = liquid.GetTileToVolume(volume);
        world.tilemaps[(int)Layers.Liquid].SetTile((Vector3Int)pos, newTile);
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

    //校验标记是否已存在
    public bool CheckMarkForUpdate(LiquidClass liquid, Vector2Int pos) {
        if (!world.CheckWorldBound(pos.x, pos.y)) return false;

        if (!updates.TryGetValue(liquid, out var set)) {
            throw new Exception("液体" + liquid.name + "未进行注册！");
        }
        return set.ContainsKey(pos);
    }
}
