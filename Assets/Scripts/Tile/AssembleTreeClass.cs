using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//组装树
[CreateAssetMenu(fileName = "AssembleTreeClass", menuName = "Tile/new AssembleTreeClass")]
public class AssembleTreeClass : TreeClass
{
    public int maxHeight;//最大树高
    public int minHeight;//最小树高
    public TileClass leaf;//树叉
    private Vector3Int[] directions = {
        Vector3Int.up,
        Vector3Int.down,
        Vector3Int.left,
        Vector3Int.right
    };
    public override void PlanceSelf(int x, int y) {
        int h = Random.Range(minHeight, maxHeight);//树高
        int maxBranches = Random.Range(3, 10);//树杈
        int bCounts = 0;//树杈计数
        //组合树
        for (int ny = y; ny < y + h; ny++) {
            chunk.SetTileClass(this.layer, x, ny, this);
            //生成树桩
            if (ny == y) {
                //左侧树桩
                if (Random.Range(0, 100) < 30) {
                    if (x > 0 && chunk.GetTileClass(LayerType.Foreground, x - 1, ny - 1) != null && chunk.GetTileClass(LayerType.Foreground, x - 1, ny) == null) {
                        chunk.SetTileClass(this.layer, x - 1, ny, this);
                    }
                }
                //右侧树桩
                if (Random.Range(0, 100) < 30) {
                    if (chunk.GetTileClass(LayerType.Foreground, x + 1, ny - 1) != null && chunk.GetTileClass(LayerType.Foreground, x + 1, ny) == null) {
                        chunk.SetTileClass(this.layer, x + 1, ny, this);
                    }
                }

            }
            //生成树杈
            else if (ny >= y + 2 && ny <= y + h - 3) {
                if (bCounts < maxBranches && Random.Range(0, 100) < 40) {
                    if (x > 0 && chunk.GetTileClass(LayerType.Foreground, x - 1, ny) == null && chunk.GetTileClass(LayerType.Addons, x - 1, ny - 1) != this) {
                        chunk.SetTileClass(leaf.layer, x - 1, ny, leaf);
                        bCounts++;
                    }
                }
                if (bCounts < maxBranches && Random.Range(0, 100) < 40) {
                    if (chunk.GetTileClass(LayerType.Foreground, x + 1, ny) == null && chunk.GetTileClass(LayerType.Addons, x + 1, ny - 1) != this) {
                        chunk.SetTileClass(leaf.layer, x + 1, ny, leaf);
                        bCounts++;
                    }
                }
            }
        }
    }

    //清理自己
    public override void ClearSelf(int x, int y) {
        List<Vector2Int> treePos = FindConnectedTiles(new Vector2Int(x, y));
        foreach (var pos in treePos) {
            chunk.SetTileClass(layer, pos.x, pos.y, null);
        }
    }

    //检索连续的树木瓦片坐标
    private List<Vector2Int> FindConnectedTiles(Vector2Int startPosition) {
        // 初始化数据结构
        var connectedTiles = new List<Vector2Int>();//结果
        var visited = new HashSet<Vector2Int>();//记录走过的路径
        var queue = new Queue<Vector2Int>();

        // 获取起始点瓦片
        TileClass startTile = chunk.GetTileClass(layer, startPosition.x, startPosition.y);
        if (startTile == null) return connectedTiles; // 如果起始点无瓦片，返回空列表

        // 开始BFS
        queue.Enqueue(startPosition);
        visited.Add(startPosition);

        while (queue.Count > 0) {
            Vector2Int current = queue.Dequeue();
            connectedTiles.Add(current); // 添加到结果

            // 检查所有相邻方向
            foreach (Vector2Int dir in directions) {
                Vector2Int neighbor = current + dir;

                // 跳过已访问坐标
                if (visited.Contains(neighbor)) continue;

                // 检查瓦片是否存在且类型相同
                TileClass neighborTile = chunk.GetTileClass(layer, neighbor.x, neighbor.y);
                if (neighborTile != null && (neighborTile.Equals(startTile) || neighborTile.Equals(leaf))) {
                    queue.Enqueue(neighbor);
                    visited.Add(neighbor);
                }
            }
        }

        return connectedTiles;
    }

    public override bool CheckSpawn(int x, int y) {
        for (int extY = y; extY < y + maxHeight; extY++) {
            //查看左右侧树两格情况，如果存在树木，则不能生成
            for (int i = 1; i <= 2; i++) {
                TileClass leftAddons = chunk.GetTileClass(LayerType.Addons, x - i, extY);
                TileClass rightAddons = chunk.GetTileClass(LayerType.Addons, x + i, extY);
                if ((leftAddons != null && leftAddons as TreeClass) || (rightAddons != null && rightAddons as TreeClass)) {
                    return false;
                }
            }

        }
        return true;
    }
}
