using UnityEngine;
using static UnityEditor.PlayerSettings;
using UnityEngine.Rendering;
using UnityEngine.Tilemaps;

// 液体层 —— 持有 Tilemap 引用；液体量数据现已存入 ChunkManager（TileData.liquidVolume）
public class LiquidLayer : ConstructionLayer {

    public override void Build(Vector2Int worldCoords, TileClass item) {
        float oldVolume = chunkManager.GetLiquidVolume(worldCoords);
        this.Build(worldCoords, item, oldVolume + 1);
        
    }

    public void Build(Vector2Int worldCoords, TileClass item, float volume) {
        base.Build(worldCoords, item);
        LiquidClass liquid = item as LiquidClass;
        chunkManager.SetLiquidVolume(worldCoords, volume);
        if (volume == 0) {
            chunkManager.SetBlockId(LayerType.Liquid, worldCoords, 0);
            LiquidHandler.Instance.RemoveForUpdate(liquid, worldCoords);
        } else {
            chunkManager.SetBlockId(LayerType.Liquid, worldCoords, liquid.blockId);
            PhysicsSimulationHandler.Instance.MarkForUpdate(worldCoords);
            LiquidHandler.Instance.MarkForUpdate(liquid, worldCoords);
        }
        
        // 不同体积水体瓦片
        TileBase newTile = liquid.GetTileToVolume(volume);
        var coords = _tilemap.WorldToCell(new Vector3Int(worldCoords.x, worldCoords.y));
        _tilemap.SetTile(coords, newTile);
    }

    public override void Destory(Vector2Int worldCoords) {
        base.Destory(worldCoords);
        ChunkManager.Instance.SetLiquidVolume(worldCoords, 0);
    }
}
