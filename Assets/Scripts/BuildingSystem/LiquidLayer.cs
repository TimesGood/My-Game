using UnityEngine;

// 液体层 —— 持有 Tilemap 引用；液体量数据现已存入 ChunkManager（TileData.liquidVolume）
public class LiquidLayer : ConstructionLayer {
    // 液体量访问器委托给 ChunkManager
    public void SetVolume(Vector3Int pos, float volume) {
        ChunkManager.Instance.SetLiquidVolume(new Vector2Int(pos.x, pos.y), volume);
    }

    public float GetVolume(Vector3Int pos) {
        return ChunkManager.Instance.GetLiquidVolume(new Vector2Int(pos.x, pos.y));
    }

    public override void Build(Vector3 worldCoords, TileClass item) {
        base.Build(worldCoords, item);
        var coords = _tilemap.WorldToCell(worldCoords);
        float oldVolume = GetVolume(coords);
        SetVolume(coords, oldVolume + 1);
        LiquidHandler.Instance.MarkForUpdate(item as LiquidClass, new Vector2Int(coords.x, coords.y));
    }

    public override void Destory(Vector3 worldCoords) {
        base.Destory(worldCoords);
        var coords = _tilemap.WorldToCell(worldCoords);
        SetVolume(coords, 0);
    }
}
