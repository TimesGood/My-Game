using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

//预览层
public class PreviewLayer : TilemapLayer
{
    [SerializeField]
    private SpriteRenderer _previewRernder;

    public void ShowPreview(TileClass item, Vector3 worldCoorlds, bool isValid) {
        var coords = _tilemap.WorldToCell(worldCoorlds);

        TileData tileData = GetSpriteWithoutPlacing(item.tile, coords);
        _previewRernder.enabled = true;
        _previewRernder.transform.position = _tilemap.CellToWorld(coords) + _tilemap.cellSize / 2;
        _previewRernder.sprite = item.previewSprite == null ? item.tile.m_DefaultSprite : item.previewSprite;
        //_previewRernder.transform.localRotation = tileData.transform.rotation;
        _previewRernder.color = isValid ? new Color(0, 1, 0, 0.5f) : new Color(1, 0, 0, 0.5f);
    }

    //获取预放置瓦片信息
    public TileData GetSpriteWithoutPlacing(CustomTile ruleTile, Vector3Int position) {
        TileData tileData = new TileData();
        //ruleTile.GetTileData(position, _tilemap, ref tileData);
        return tileData;
    }

    public void ClearPreview() {
        _previewRernder.enabled = false;
    }
}
