using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

//树
[CreateAssetMenu(fileName = "TreeClass", menuName = "Tile/new TreeClass")]
public class TreeClass : TileClass
{
    public float frequency = 0.04f;//控制树的密度
    public float threshold = 0.6f;//控制树的稀有度
    public float prob = 0.7f;//生成概率
    public bool isSurface = false;//生成于地表

    protected ChunkManager chunk => ChunkManager.Instance;

    #region 编辑器

    [Header("占位定义")]
    public TileFootprint footprint = new TileFootprint();

    protected override void OnEnable() {
        base.OnEnable();
#if UNITY_EDITOR
        if (footprint.clearMap == null || footprint.clearMap.Length == 0) {
            footprint.Initialize();
        }
#endif
    }

    #endregion

    //放置自己
    public virtual void PlanceSelf(int x, int y) {

        //清理周围瓦片
        List<Vector2Int> occupys = footprint.GetWorldClearCells(x, y);
        foreach (var wordPos in occupys) {

            TileClass groundTile = chunk.GetTileClass(LayerType.Foreground, wordPos);
            if (groundTile != null) {
                chunk.SetBlockId(LayerType.Foreground, wordPos, 0);
                //查看清除的土块上方是否有树木，清除掉
                TileClass addonsTile = chunk.GetTileClass(LayerType.Addons, wordPos + Vector2Int.up);
                if (addonsTile == null) continue;
                if (addonsTile is TreeClass) ((TreeClass)addonsTile).ClearSelf(wordPos.x, wordPos.y + 1);
            }
            // 占位
            chunk.SetBlockId(layer, wordPos, 1);
        }
        chunk.SetBlockId(layer, x, y, this.blockId);

    }


    //清理自己
    public virtual void ClearSelf(int x, int y) {
        chunk.SetBlockId(layer, x, y, 0);
    }

    //校验能否放置
    public virtual bool CheckSpawn(int x, int y) {
        if (!CheckGroundUnderRoots(x, y))
            return false;

        if (CheckOccupancy(x, y))
            return false;

        return true;
    }


    /// <summary>
    /// 检查根部下方是否有足够的地面支撑。
    /// 沿 originPoint 行向两侧扩展，遇到 clearMap 边界为止。
    /// </summary>
    protected bool CheckGroundUnderRoots(int x, int y) {
        var fp = footprint;
        int originGx = fp.originPoint.x;
        int originGy = fp.originPoint.y;

        // 向右检查
        for (int gx = originGx; gx < fp.gridWidth; gx++) {
            if (!fp.ShouldClear(gx, originGy)) break;
            int worldX = gx - originGx + x;
            int worldYBelow = y - 1;
            if (chunk.GetTileClass(LayerType.Foreground, worldX, worldYBelow) == null)
                return false;
        }

        // 向左检查
        for (int gx = originGx - 1; gx >= 0; gx--) {
            if (!fp.ShouldClear(gx, originGy)) break;
            int worldX = gx - originGx + x;
            int worldYBelow = y - 1;
            if (chunk.GetTileClass(LayerType.Foreground, worldX, worldYBelow) == null)
                return false;
        }

        return true;
    }


    /// <summary>
    /// 检查自身占用是否与其他植株占用冲突
    /// </summary>
    protected bool CheckOccupancy(int x, int y) {
        List<Vector2Int> occupys = footprint.GetWorldClearCells(x, y);

        foreach (var wordPos in occupys) {
            long blockId = chunk.GetBlockId(layer, wordPos);
            if (blockId == 1) return true;
        }
        return false;
    }

}
