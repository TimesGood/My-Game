using System.Collections;
using System.Collections.Generic;
using TreeEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;
using static UnityEditor.PlayerSettings;

//树
[CreateAssetMenu(fileName = "TreeClass", menuName = "Tile/new TreeClass")]
public class TreeClass : TileClass
{
    public int maxHeight;//最大树高
    public int minHeight;//最小树高
    public int treeWidth;//树宽
    public TileClass leaf;//树叉
    public float frequency = 0.04f;//控制树的密度
    public float threshold = 0.6f;//控制树的稀有度
    public float prob = 0.7f;//生成概率
    public bool isSurface = false;//生成于地表
    public PerlinNoise noise;//噪图

    #region 编辑器
    [Header("Grid Settings")]
    public int gridWidth = 15; // 网格宽度（奇数）
    public int gridHeight = 15; // 网格高度（奇数）
    [Header("Tree Settings")]
    public Vector2Int originPoint = new Vector2Int(2, 2); // 原点位置（树根位置）

    [HideInInspector]
    public bool[] clearMap; // 清除区域映射

    public void InitializeGrid() {
        clearMap = new bool[gridWidth * gridHeight];
        originPoint = new Vector2Int(gridWidth / 2, gridHeight / 2);
    }

    public bool ShouldClear(int x, int y) {
        int index = y * gridWidth + x;
        if (index >= 0 && index < clearMap.Length) {
            return clearMap[index];
        }
        return false;
    }

    #endregion

    //世界生成时，放置自己的时候需要清空周围瓦片

    public void PlanceSelf(int x, int y) {

        int h = Random.Range(minHeight, maxHeight);//树高
        int maxBranches = Random.Range(3, 10);//树杈
        int bCounts = 0;//树杈计数
        //完整的树
        if (leaf == null) {
            //清理周围瓦片
            for (int gridX = 0; gridX < gridWidth; gridX++) {
                for (int gridY = 0; gridY < gridHeight; gridY++) {
                    if (ShouldClear(gridX, gridY)) {
                        //转世界空间
                        int worldX = gridX - originPoint.x + x;
                        int worldY = gridY - originPoint.y + y;
                        TileClass isAddon = WorldGeneration.Instance.GetTileData(Layers.Addons, worldX, worldY + 1);
                        if (isAddon != null) WorldGeneration.Instance.SetTileData(null, Layers.Addons, worldX, worldY + 1);
                        WorldGeneration.Instance.SetTileData(null, Layers.Ground, worldX, worldY);
                    }
                }
            }

            WorldGeneration.Instance.SetTileData(this, this.layer, x, y);
            return;
        }
        //组合树
        for (int ny = y; ny < y + h; ny++) {
            WorldGeneration.Instance.SetTileData(this, this.layer, x, ny);
            //生成树桩
            if (ny == y) {
                //左侧树桩
                if (Random.Range(0, 100) < 30) {
                    if (x > 0 && WorldGeneration.Instance.GetTileData(Layers.Ground, x - 1, ny - 1) != null && WorldGeneration.Instance.GetTileData(Layers.Ground, x - 1, ny) == null) {
                        WorldGeneration.Instance.SetTileData(this, this.layer, x - 1, ny);
                    }
                }
                //右侧树桩
                if (Random.Range(0, 100) < 30) {
                    if (WorldGeneration.Instance.GetTileData(Layers.Ground, x + 1, ny - 1) != null && WorldGeneration.Instance.GetTileData(Layers.Ground, x + 1, ny) == null) {
                        WorldGeneration.Instance.SetTileData(this, this.layer, x + 1, ny);
                    }
                }

            }
            //生成树杈
            else if (ny >= y + 2 && ny <= y + h - 3) {
                if (bCounts < maxBranches && Random.Range(0, 100) < 40) {
                    if (x > 0 && WorldGeneration.Instance.GetTileData(Layers.Ground, x - 1, ny) == null && WorldGeneration.Instance.GetTileData(Layers.Addons, x - 1, ny - 1) != this) {
                        WorldGeneration.Instance.SetTileData(leaf, leaf.layer, x - 1, ny);
                        bCounts++;
                    }
                }
                if (bCounts < maxBranches && Random.Range(0, 100) < 40) {
                    if (WorldGeneration.Instance.GetTileData(Layers.Ground, x + 1, ny) == null && WorldGeneration.Instance.GetTileData(Layers.Addons, x + 1, ny - 1) != this) {
                        WorldGeneration.Instance.SetTileData(leaf, leaf.layer, x + 1, ny);
                        bCounts++;
                    }
                }
            }
        }
    }


}
