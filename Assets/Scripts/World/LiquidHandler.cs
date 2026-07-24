using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

// 液体物理模拟
public class LiquidHandler : Singleton<LiquidHandler> {
    public LiquidClass[] liquids; // 已注册的液体类型
    public bool openFlow = false;
    public Dictionary<LiquidClass, Dictionary<Vector2Int, int>> updates = new Dictionary<LiquidClass, Dictionary<Vector2Int, int>>();
    private HashSet<string> loadingRoutines = new HashSet<string>();
    private Dictionary<LiquidClass, Coroutine> updateRoutines = new Dictionary<LiquidClass, Coroutine>();

    private float lastClearUpdateTime;
    private const float checkClearInterval = 3f;

    private ChunkManager chunkManager => ChunkManager.Instance;

    protected override void Awake() {
        base.Awake();
        foreach (var liquid in liquids) {
            updates.Add(liquid, new Dictionary<Vector2Int, int>());
        }
    }

    void Update() {
        UpdateLiquid();
        ScanClearSteadyLiquid();
    }

    private IEnumerator HandleLiquidUpdate(LiquidClass liquid, Dictionary<Vector2Int, int> updates) {
        Bounds bounds = ChunkHandler.Instance.GetCameraBounds();
        List<Vector2Int> keys = new List<Vector2Int>(updates.Keys);
        int processed = 0;

        // 屏幕外：快速批量处理
        foreach (var item in keys) {
            if (!chunkManager.CheckWorldBound(item.x, item.y) || bounds.Contains((Vector3Int)item)) continue;
            float curVolume = GetVolume(item);
            bool isChange = liquid.CalculatePhysics(item);
            if (!updates.ContainsKey(item)) continue;
            if (isChange)
                updates[item] = 0;
            else
                updates[item] += 1;

            if (++processed % 5000 == 0) yield return null;
        }

        yield return new WaitForSeconds(1f / liquid.flowSpeed);

        processed = 0;
        keys = new List<Vector2Int>(updates.Keys);
        // 按 Y 排序以实现自然向下流动
        keys.Sort((a, b) => a.y.CompareTo(b.y));

        // 屏幕内：优先处理可见区域
        foreach (var item in keys) {
            if (!bounds.Contains((Vector3Int)item)) continue;
            float curVolume = GetVolume(item);
            bool isChange = liquid.CalculatePhysics(item);
            if (!updates.ContainsKey(item)) continue;
            if (isChange)
                updates[item] = 0;
            else
                updates[item] += 1;

            if (++processed % 5000 == 0) yield return null;
        }

        updateRoutines.Remove(liquid);
    }

    private void UpdateLiquid() {
        Bounds bounds = ChunkHandler.Instance.GetCameraBounds();
        foreach (var kvp in updates) {
            List<Vector2Int> outUpdates = new List<Vector2Int>();
            List<Vector2Int> inUpdates = new List<Vector2Int>();
            foreach (var inKvp in kvp.Value) {
                if (bounds.Contains((Vector3Int)inKvp.Key))
                    inUpdates.Add(inKvp.Key);
                else
                    outUpdates.Add(inKvp.Key);
            }

            if (!loadingRoutines.Contains(kvp.Key.name + "in")) {
                loadingRoutines.Add(kvp.Key.name + "in");
                StartCoroutine(HandlerVisibleIn(bounds, kvp.Key, inUpdates, kvp.Value));
            }

            if (!loadingRoutines.Contains(kvp.Key.name + "out")) {
                loadingRoutines.Add(kvp.Key.name + "out");
                StartCoroutine(HandlerVisibleOut(bounds, kvp.Key, outUpdates, kvp.Value));
            }
        }
    }

    private IEnumerator HandlerVisibleIn(Bounds bounds, LiquidClass liquid, List<Vector2Int> inUpdate, Dictionary<Vector2Int, int> updates) {
        yield return new WaitForSeconds(1f / liquid.flowSpeed);
        inUpdate.Sort((a, b) => a.y.CompareTo(b.y));

        foreach (var item in inUpdate) {
            float curVolume = GetVolume(item);
            bool isChange = liquid.CalculatePhysics(item);
            if (!updates.ContainsKey(item)) continue;
            if (isChange)
                updates[item] = 0;
            else
                updates[item] += 1;
        }
        loadingRoutines.Remove(liquid.name + "in");
    }

    private IEnumerator HandlerVisibleOut(Bounds bounds, LiquidClass liquid, List<Vector2Int> outUpdates, Dictionary<Vector2Int, int> updates) {
        int processed = 0;
        foreach (var item in outUpdates) {
            if (!chunkManager.CheckWorldBound(item.x, item.y)) continue;
            if (bounds.Contains((Vector3Int)item)) continue;
            float curVolume = GetVolume(item);
            bool isChange = liquid.CalculatePhysics(item);
            if (!updates.ContainsKey(item)) continue;
            if (isChange)
                updates[item] = 0;
            else
                updates[item] += 1;

            if (++processed % 1000 == 0)
                yield return null;
        }
        loadingRoutines.Remove(liquid.name + "out");
    }

    private void ScanClearSteadyLiquid() {
        if (Time.time - lastClearUpdateTime > checkClearInterval) {
            foreach (var kvp in updates) {
                foreach (var key in kvp.Value.ToList()) {
                    kvp.Value.TryGetValue(key.Key, out int num);
                    if (num > 50)
                        kvp.Value.Remove(key.Key);
                }
                Debug.Log(kvp.Key.name + ": " + kvp.Value.Count);
            }
            lastClearUpdateTime = Time.time;
        }
    }

    // 更新液体
    public void UpdateVolume(LiquidClass liquid, Vector2Int pos, float volume) {
        LiquidLayer tilemapLayer = TilemapManager.Instance.GetLayer(LayerType.Liquid) as LiquidLayer;
        tilemapLayer.Build(pos, liquid, volume);
    }

    // 标记
    public void MarkForUpdate(LiquidClass liquid, Vector2Int pos) {
        if (!chunkManager.CheckWorldBound(pos.x, pos.y)) return;

        if (!updates.TryGetValue(liquid, out var set))
            throw new Exception("Liquid " + liquid.name + " 未注册！");

        if (!set.ContainsKey(pos))
            set.Add(pos, 0);
        else
            set[pos] = 0;
    }

    // 删除标记
    public void RemoveForUpdate(LiquidClass liquid, Vector2Int pos) {
        if (!chunkManager.CheckWorldBound(pos.x, pos.y)) return;

        if (!updates.TryGetValue(liquid, out var set))
            throw new Exception("Liquid " + liquid.name + " 未注册！");
        set.Remove(pos);
    }

    // 校验此处有没有活跃水标记
    public bool CheckMarkForUpdate(LiquidClass liquid, Vector2Int pos) {
        if (!updates.TryGetValue(liquid, out var set))
            throw new Exception("Liquid " + liquid.name + " 未注册！");
        return set.ContainsKey(pos);
    }

    // 将液体量存储委托给 ChunkManager
    public void SetVolume(Vector2Int pos, float volume) {
        chunkManager.SetLiquidVolume(pos, volume);
    }

    public float GetVolume(Vector2Int pos) {
        return chunkManager.GetLiquidVolume(pos);
    }
}
