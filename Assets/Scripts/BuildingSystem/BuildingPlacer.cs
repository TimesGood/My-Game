using System;
using System.Collections;
using System.Collections.Generic;
using GameInput;
using UnityEngine;

namespace BuildingSystem {
    // 建造系统 —— 图块放置与销毁
    public class BuildingPlacer : MonoBehaviour {
        [field: SerializeField]
        public TileClass ActionBuildable { get; private set; }

        [SerializeField] private float _maxBuildingDistance = 3f;
        [SerializeField] private MouseUser _mouseUser;
        [SerializeField] private ConstructionLayer _constructionLayer; // 用于 Tilemap 引用和兼容性
        [SerializeField] private PreviewLayer _previewLayer;

        public event Action ActiveBuildableChanged;

        private Vector2Int lastMousePos;

        private void Update() {
            var mousePos = _mouseUser.MouseInWorldPosition;

            if (!IsMouseWithinBuildableRange() || _constructionLayer == null) {
                _previewLayer.ClearPreview();
                return;
            }

            // 右键：销毁
            if (_mouseUser.IsMouseButtonPressed(MouseButton.Right)) {
                DestroyTile(mousePos);
            }

            if (ActionBuildable == null) return;

            // 鼠标移动时更新预览
            if (_mouseUser.MousePosition != lastMousePos) {
                _previewLayer.ShowPreview(ActionBuildable, mousePos, IsEmpty(mousePos));
                lastMousePos = new Vector2Int((int)mousePos.x, (int)mousePos.y);
            }

            // 左键：建造
            if (_mouseUser.IsMouseButtonPressed(MouseButton.Left) && IsEmpty(mousePos)) {
                BuildTile(mousePos, ActionBuildable);
            }
        }

        private void BuildTile(Vector3 worldCoords, TileClass item) {
            var cellPos = _constructionLayer._tilemap.WorldToCell(worldCoords);
            WorldManager.Instance.PlaceTile(item, cellPos);
        }

        private void DestroyTile(Vector3 worldCoords) {
            var cellPos = _constructionLayer._tilemap.WorldToCell(worldCoords);

            // 检查所有图层，销毁找到的第一个非空图块
            Layers[] layers = (Layers[])Enum.GetValues(typeof(Layers));
            foreach (var layer in layers) {
                TileClass tile = WorldManager.Instance.GetTileClass(layer, cellPos.x, cellPos.y);
                if (tile != null) {
                    WorldManager.Instance.Erase(layer, cellPos);
                    break;
                }
            }
        }

        private bool IsEmpty(Vector3 worldCoords) {
            var cellPos = _constructionLayer._tilemap.WorldToCell(worldCoords);
            TileData tileData = WorldManager.Instance.GetTileData(cellPos.x, cellPos.y);
            return tileData.IsEmpty && _constructionLayer._tilemap.GetTile(cellPos) == null;
        }

        private bool IsMouseWithinBuildableRange() {
            return Vector3.Distance(_mouseUser.MouseInWorldPosition, transform.position) <= _maxBuildingDistance;
        }

        public void SetActionBuildable(TileClass item) {
            ActionBuildable = item;
            ActiveBuildableChanged?.Invoke();
        }
    }
}
