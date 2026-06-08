using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

// 植物生长模拟
public class GrowthHandler : Singleton<GrowthHandler> {
    private WorldManager world => WorldManager.Instance;
    private ChunkManager chunkManager;
    public Dictionary<Vector3Int, float> growthTime = new Dictionary<Vector3Int, float>();
    private float interval = 10f;

    protected override void Awake() {
        base.Awake();
        chunkManager = ChunkManager.Instance;
    }

    private void Update() {
        if (Time.time % interval >= Time.deltaTime) return;

        foreach (var kvp in growthTime.ToList()) {
            if (Time.time > kvp.Value) {
                Vector2Int wpos = new Vector2Int(kvp.Key.x, kvp.Key.y);
                TileClass tileClass = world.GetTileClass(Layers.Addons, kvp.Key.x, kvp.Key.y);
                int growthData = chunkManager.GetGrowthData(wpos);

                if (tileClass == null) {
                    chunkManager.SetGrowthData(wpos, 0);
                    growthTime.Remove(kvp.Key);
                    continue;
                }

                if (growthData != 0) {
                    growthData += 1;
                    chunkManager.SetGrowthData(wpos, growthData);

                    Tilemap tilemap = world.GetTileLayer(Layers.Addons)._tilemap;
                    if (tilemap == null) continue;

                    GameObject tileObj = tilemap.GetInstantiatedObject(kvp.Key);
                    if (tileObj != null) {
                        Rope rope = tileObj.GetComponent<Rope>();
                        if (rope != null)
                            rope.AddLink();
                    } else {
                        Vector2Int chunkID = ChunkHandler.Instance.WorldToChunkCoord(wpos);
                        if (!ChunkHandler.Instance.loadedChunkIDs.Contains(chunkID)) continue;

                        if (tileClass is AddonClass addon) {
                            TileBase tile = addon.GetTileToGrowth(growthData);
                            tilemap.SetTile(kvp.Key, tile);
                        }
                    }
                } else {
                    growthTime.Remove(kvp.Key);
                }
            }
        }
    }

    public void MarkForUpdate(Vector2Int pos, int growthData) {
        if (!world.CheckWorldBound(pos.x, pos.y)) return;
        Vector3Int p = (Vector3Int)pos;
        if (growthTime.TryGetValue(p, out float value)) return;
        growthTime.Add(p, Time.time);
        chunkManager.SetGrowthData(pos, growthData);
    }

    public void RemoveForUpdate(Vector2Int pos) {
        growthTime.Remove((Vector3Int)pos);
    }
}
