using NUnit.Framework;
using Unity.Entities.UniversalDelegates;
using UnityEngine;

//树
[CreateAssetMenu(fileName = "TreeClass", menuName = "Tile/new TreeClass")]
public class TreeClass : TileClass
{
    protected MapGenerator map => MapGenerator.Instance;
    protected WorldManager world => WorldManager.Instance;
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
    public bool[] clearMap; // 清除区域映射，二维数组不会保留，很奇怪

    public void InitializeGrid() {
        clearMap = new bool[gridWidth * gridHeight];
        originPoint = new Vector2Int(gridWidth / 2, gridHeight / 2);
    }

    //查看此区域是否已设置为清理区
    public bool ShouldClear(int gridX, int gridY) {
        int index = gridY * gridWidth + gridX;
        if (index >= 0 && index < clearMap.Length) {
            return clearMap[index];
        }
        return false;
    }

    #endregion

    //放置自己
    public virtual void PlanceSelf(int x, int y) {

        //清理周围瓦片
        for (int gridX = 0; gridX < gridWidth; gridX++) {
            for (int gridY = 0; gridY < gridHeight; gridY++) {
                if (ShouldClear(gridX, gridY)) {
                    //转世界空间
                    int worldX = gridX - originPoint.x + x;
                    int worldY = gridY - originPoint.y + y;
                    TileClass groundTile = world.GetTileClass(Layers.Ground, worldX, worldY);
                    if (groundTile != null) {
                        world.SetTileClass(null, Layers.Ground, worldX, worldY);
                        //查看土块上方是否有树木，清除
                        TileClass addonsTile = world.GetTileClass(Layers.Addons, worldX, worldY + 1);
                        if (addonsTile == null) continue;
                        if (addonsTile is TreeClass) ((TreeClass)addonsTile).ClearSelf(worldX, worldY + 1);
                        else world.SetTileClass(null, Layers.Addons, worldX, worldY + 1);
                    }
                    
                    
                }
            }
        }
        world.SetTileClass(this, layer, x, y);

    }


    //清理自己
    public virtual void ClearSelf(int x, int y) {
        world.SetTileClass(null, layer, x, y);
    }

    //校验生成条件
    public virtual bool CheckSpawn(int x, int y) {
        //查看树桩所占地面是否镂空，镂空不能生成
        int pointer = 1;
        while (ShouldClear(originPoint.x + pointer, originPoint.y) || ShouldClear(originPoint.x - pointer, originPoint.y)) {
            if (world.GetTileClass(Layers.Ground, x + pointer, y - 1) == null || world.GetTileClass(Layers.Ground, x - pointer, y - 1) == null) return false;
            pointer++;
        }

        //查看所占空间有物块所占比例超过一定比例，不允许生成
        int nullNum = 0;
        for (int gridX = 0; gridX < gridWidth; gridX++) {
            for (int gridY = 0; gridY < gridHeight; gridY++) {
                if (ShouldClear(gridX, gridY)) {
                    //转世界空间
                    int worldX = gridX - originPoint.x + x;
                    int worldY = gridY - originPoint.y + y;
                    if (world.GetTileClass(Layers.Ground, worldX, worldY) == null) nullNum++;
                }
            }
        }
        if ((float)nullNum / clearMap.Length < 0.5) return false;


        //查看左右侧两格情况，如果存在植物，则不能生成
        for (int i = 1; i <= 2; i++) {
            
            TileClass leftAddons = world.GetTileClass(Layers.Addons, x - i, y);
            TileClass rightAddons = world.GetTileClass(Layers.Addons, x + i, y);
            if ((leftAddons != null && leftAddons as TreeClass) || (rightAddons != null && rightAddons as TreeClass)) {
                return false;
            }
        }
        return true;
    }

}
