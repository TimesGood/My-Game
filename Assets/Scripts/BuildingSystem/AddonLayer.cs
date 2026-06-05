using UnityEngine;

// 附加物层 —— 持有 Tilemap 引用；生长数据现已存入 ChunkManager（TileData.growthData）
public class AddonLayer : ConstructionLayer {
    public void SetGrowthData(Vector3Int pos, int data) {
        ChunkManager.Instance.SetGrowthData(new Vector2Int(pos.x, pos.y), data);
    }

    public int GetGrowthData(Vector3Int pos) {
        return ChunkManager.Instance.GetGrowthData(new Vector2Int(pos.x, pos.y));
    }
}
