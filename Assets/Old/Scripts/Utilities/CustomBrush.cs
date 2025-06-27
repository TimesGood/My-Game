using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using UnityEditor.Tilemaps;
using UnityEngine;
using UnityEngine.Tilemaps;
using static UnityEditor.Experimental.GraphView.GraphView;
using static UnityEditor.PlayerSettings;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

//自定义笔刷
namespace UnityEditor
{
    [CustomGridBrush(true, false, false,"CustonBrush")]
    [CreateAssetMenu(fileName ="CustomBrush", menuName ="World/new CustomBrush")]
    public class CustomBrush : GridBrush
    {
        public bool refresh;

        //范围绘制
        public override void BoxFill(GridLayout gridLayout, GameObject brushTarget, BoundsInt position)
        {

            if (Application.isPlaying) {
                foreach (var pos in position.allPositionsWithin) {
                    // 获取精确的世界坐标
                    Vector3 cellCenter = GetCellWorldCenter(gridLayout, pos);
                    //检查此区域是否有碰撞体,有碰撞体不能放置
                    Collider2D[] colliders = Physics2D.OverlapCircleAll(new Vector2(cellCenter.x, cellCenter.y), 0.3f);
                    if (colliders.Length > 0) return;

                    BrushCell cell = cells[GetCellIndexWrapAround(pos.x, pos.y, pos.z)];
                    if (cell.tile is CustomTile) {
                        long id = ((CustomTile)cell.tile).blockId;
                        TileClass tileClass = WorldGeneration.TileRegistry.GetTile(id);

                        if (tileClass is LiquidClass) {
                            
                            WorldGeneration.Instance.PlaceLiquidTile((LiquidClass)tileClass, pos.x, pos.y, 1);
                        } else {
                            WorldGeneration.Instance.PlaceTile(tileClass, pos.x, pos.y);
                            if (tileClass.isIlluminated) {
                                LightHandler.Instance.LightUpdate(pos.x, pos.y);
                            }
                        }
                    }
                }
            } else {
                base.BoxFill(gridLayout, brushTarget, position);
                
            }
        }
        // 获取单元格精确中心点
        private Vector3 GetCellWorldCenter(GridLayout grid, Vector3Int position) {
            Vector3 cellCenter = grid.CellToWorld(position);
            cellCenter += grid.cellSize / 2;
            cellCenter.z = 0;
            return cellCenter;
        }
        //查找瓦片对应瓦片地图
        private Layers GetLayerByName(string name) {
            switch (name) {
                case "Addons":
                    return Layers.Addons;
                case "BackGround":
                    return Layers.Background;
                case "Ground":
                    return Layers.Ground;
                case "Liquid":
                    return Layers.Liquid;
                default:
                    throw new System.Exception("异常，找不到瓦片地图");
            }
        }



        //擦除
        public override void Erase(GridLayout gridLayout, GameObject brushTarget, Vector3Int position)
        {
            if (Application.isPlaying)
            {
                Layers layer = GetLayerByName(brushTarget.name);
                WorldGeneration.Instance.Erase(layer, position.x, position.y);
            }
            else
            {
                base.Erase(gridLayout, brushTarget, position);
            }
        }

    }


    //绘制描述信息
    [CustomEditor(typeof(CustomBrush))]
    public class CustomBrushEditor : GridBrushEditor
    {
        public override void OnPaintSceneGUI(GridLayout gridLayout, GameObject brushTarget, BoundsInt position, GridBrushBase.Tool tool, bool executing)
        {
            base.OnPaintSceneGUI(gridLayout, brushTarget, position, tool, executing);

            //范围
            string labelText = "Pos:"+new Vector2Int(position.x, position.y);
            if (position.size.x > 1 || position.size.y > 1)
            {
                labelText += ",Size:" + new Vector2Int(position.size.x, position.size.y);
            }
            if (Application.isPlaying) {
                Tilemap tilemap = brushTarget.GetComponent<Tilemap>();
                foreach (var pos in position.allPositionsWithin) {
                    // 获取精确的世界坐标
                    //Vector3 cellCenter = GetCellWorldCenter(gridLayout, pos);
                    // 获取瓦片
                    TileBase tile = tilemap.GetTile(pos);
                    Layers layer = GetLayerByName(brushTarget.name);
                    TileClass tileClass = WorldGeneration.Instance.GetTileClass(layer, pos.x, pos.y);
                    if (tileClass is LiquidClass) {
                        float volume = LiquidHandler.Instance.liquidVolume[pos.x, pos.y];
                        Handles.Label(new Vector3(pos.x, pos.y + 1), volume.ToString());
                    }

                }
            }


            Handles.Label(new Vector3(position.x, position.y), labelText);

            //Vector3 cellCenter = GetCellWorldCenter(gridLayout, new Vector3Int(position.x, position.y));
            //Handles.CircleHandleCap(
            //0, // 控件 ID
            //new Vector3(cellCenter.x, cellCenter.y),
            //Quaternion.identity, // 无旋转（保持 2D 平面）
            //0.3f,
            //EventType.Repaint
            //);
        }

        //获取单元格精确中心点
        private Vector3 GetCellWorldCenter(GridLayout grid, Vector3Int position) {
            Vector3 cellCenter = grid.CellToWorld(position);
            cellCenter += grid.cellSize / 2;
            cellCenter.z = 0;
            return new Vector3Int((int)cellCenter.x, (int)cellCenter.y, 0);
        }
        //查找瓦片对应瓦片地图
        private Layers GetLayerByName(string name) {
            switch (name) {
                case "Addons":
                    return Layers.Addons;
                case "BackGround":
                    return Layers.Background;
                case "Ground":
                    return Layers.Ground;
                case "Liquid":
                    return Layers.Liquid;
                default:
                    throw new System.Exception("异常，找不到瓦片地图");
            }
        }
    }


}

