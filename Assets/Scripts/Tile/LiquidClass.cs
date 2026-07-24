using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework.Internal;
using Unity.Entities;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;
using static UnityEditor.PlayerSettings;
using static UnityEditor.Progress;

//液体类瓷砖
[CreateAssetMenu(fileName = "LiquidClass", menuName = "Tile/new LiquidClass")]
public class LiquidClass : TileClass {
    private ChunkManager chunk => ChunkManager.Instance;
    private LiquidHandler liquidHandler;
    [field: SerializeField] public TileBase[] tiles { get; private set; }//液体在不同水位时的不同瓦片
    [field: SerializeField] public float flowSpeed { get; private set; }//流动速度
    [field: SerializeField] public float minVolume { get; private set; } = 0.005f;//最小水位

    [field: SerializeField] public TileClass medium;//与不同液体碰触之后生成的物块

    //根据水位获取对应体积瓦片
    public TileBase GetTileToVolume(float volume) {
        //根据液体体积更换不同Tile
        if (volume >= 1) {
            return tiles[tiles.Length - 1];
        } else {
            int liquidIndex = Mathf.FloorToInt(volume * (tiles.Length - 1));
            liquidIndex = liquidIndex >= 0 ? liquidIndex : 0;
            if (liquidIndex == 0)
                return null;
            else
                return tiles[liquidIndex];

        }
    }

    public IEnumerator CalculatePhysicsDelay(Vector2Int pos) {
        yield return new WaitForSeconds(1f / flowSpeed);
        CalculatePhysics(pos);
        
    }

    public bool CalculatePhysics(Vector2Int pos) {
        liquidHandler = LiquidHandler.Instance;

        int x = pos.x;
        int y = pos.y;
        //float curVolume = liquidHandler.liquidVolume[pos.x, pos.y];
        float curVolume = liquidHandler.GetVolume(pos);
        

        //体积太小时，擦掉该瓦片
        if (curVolume < minVolume) {
            liquidHandler.UpdateVolume(this, pos, 0);
            return false;
        }
        //液体在地面瓦片中，擦掉
        if (chunk.GetTileClass(LayerType.Foreground, x, y) != null) {
            liquidHandler.UpdateVolume(this, pos, 0);
            return false;
        }
        // 优先向下流动
        if (TryFlowDown(pos, ref curVolume)) return true;
        
        // 扩散处理
        if(TryDiffusion(pos, ref curVolume)) return true;

        //液体溢出
        if (TryOverflow(pos, curVolume)) return true;
        return false;
    }

    // 尝试向下流动（返回是否成功流动）
    private bool TryFlowDown(Vector2Int pos, ref float curVolume) {
        int x = pos.x;
        int y = pos.y;
        if (y <= 0) return false;

        Vector2Int downPos = pos + Vector2Int.down;
        // 检查下方是否可流动
        if (chunk.GetTileClass(LayerType.Foreground, downPos.x, downPos.y) != null) return false;
        //液体满了
        //float downVolume = liquidHandler.liquidVolume[downPos.x, downPos.y];
        float downVolume = liquidHandler.GetVolume(downPos);
        LiquidClass downLiquid = chunk.GetTileClass(LayerType.Liquid, downPos.x, downPos.y) as LiquidClass;

        if (downVolume >= 1f && (downLiquid == null || downLiquid == this)) return false;

        
        if (downLiquid == null || !downLiquid.TouchLiquid(pos, downPos)) {
            downVolume += curVolume;
            liquidHandler.UpdateVolume(this, downPos, downVolume);

        }

        curVolume = 0;
        liquidHandler.UpdateVolume(this, pos, curVolume);

        //可能周围有稳定状态液体，重新激活上左右液体液体
        //liquidHandler.MarkForUpdate(this, pos + Vector2Int.up);
        //liquidHandler.MarkForUpdate(this, pos + Vector2Int.left);
        //liquidHandler.MarkForUpdate(this, pos + Vector2Int.right);
        return true;
    }


    // 扩散处理
    private bool TryDiffusion(Vector2Int pos, ref float curVolume) {
        int x = pos.x;
        int y = pos.y;
        List<Vector2Int> flowDirs = new List<Vector2Int>();
        // 检测可用流动方向
        Vector2Int leftDir = pos + Vector2Int.left;
        Vector2Int rightDir = pos + Vector2Int.right;
        if (CheckFlowDirection(leftDir, curVolume)) flowDirs.Add(leftDir);
        if (CheckFlowDirection(rightDir, curVolume)) flowDirs.Add(rightDir);
        if (flowDirs.Count == 0) return false;
        // 计算每个方向的分配量
        float avg = curVolume;
        foreach (var item in flowDirs) {
            //avg += liquidHandler.liquidVolume[item.x, item.y];
            avg += liquidHandler.GetVolume(item);
        }
        avg /= (flowDirs.Count + 1);

        //avg = Mathf.Round(avg * 10000f) / 10000f;
        curVolume = avg;
        liquidHandler.UpdateVolume(this, pos, curVolume);
        foreach (var dir in flowDirs) {
            LiquidClass targetLiquid = chunk.GetTileClass(LayerType.Liquid, dir.x, dir.y) as LiquidClass;
            if (targetLiquid != null && targetLiquid.TouchLiquid(pos, dir)) continue;
            
            liquidHandler.UpdateVolume(this, dir, avg);
        }

        //可能周围有稳定状态液体，重新激活上左右液体液体
        //liquidHandler.MarkForUpdate(this, pos + Vector2Int.up);
        //liquidHandler.MarkForUpdate(this, pos + Vector2Int.left);
        //liquidHandler.MarkForUpdate(this, pos + Vector2Int.right);
        //liquidHandler.MarkRoundForUpdate(this, pos);
        return true;
    }

    //溢出处理
    private bool TryOverflow(Vector2Int pos, float curVolume) {
        if (curVolume <= 1f) return false;
        //液体溢出
        Vector2Int upPos = pos + Vector2Int.up;
        LiquidClass targetLiquid = chunk.GetTileClass(LayerType.Liquid, upPos.x, upPos.y) as LiquidClass;
        if (targetLiquid != null && targetLiquid != this) {
            return false;
        }
        //float upVolume = liquidHandler.liquidVolume[upPos.x, upPos.y];
        float upVolume = liquidHandler.GetVolume(upPos);
        upVolume += curVolume - 1f;
        liquidHandler.UpdateVolume(this, upPos, upVolume);

        curVolume = 1f;
        liquidHandler.UpdateVolume(this, pos, curVolume);
        return true;

    }
    private bool CheckFlowDirection(Vector2Int dir, float curVolume) {
        int x = dir.x;
        int y = dir.y;
        if (!chunk.CheckWorldBound(x, y)) return false;

        bool flag = false;
        //如果液体不相同，可流动
        TileClass targetLiquid = chunk.GetTileClass(LayerType.Liquid, x, y);
        //float targetVolume = liquidHandler.liquidVolume[x, y];
        float targetVolume = liquidHandler.GetVolume(dir);
        if (targetLiquid != null && targetLiquid != this) flag = true;

        if (chunk.GetTileClass(LayerType.Foreground, x, y) == null && curVolume > targetVolume && curVolume - targetVolume > 0.0001f) flag = true;
        return flag;
    }
    //检查水平流动方向是否可流动
    private void CheckFlowDirection(int x, int y, float curVolume, ref List<Vector2Int> dirs) {
        if (!chunk.CheckWorldBound(x, y)) return;
        TileClass targetLiquid = chunk.GetTileClass(LayerType.Liquid, x, y);

        //float targetVolume = liquidHandler.liquidVolume[x, y];
        float targetVolume = liquidHandler.GetVolume(new Vector2Int(x, y));
        if (chunk.GetTileClass(LayerType.Foreground, x, y) != null || (targetVolume >= curVolume && (targetLiquid == null || targetLiquid == this))) return;
        //两边液体体积相差无几，不扩散，避免水体表面一直在计算
        if (curVolume - targetVolume < 0.0001f) return;
        dirs.Add(new Vector2Int(x, y));
    }


    // 与其他液体接触时触发一些事件（比如生成新物质）
    // origin：接触者， target：被接触者
    private bool TouchLiquid(Vector2Int origin, Vector2Int target) {
        LiquidClass originLiquid = chunk.GetTileClass(LayerType.Liquid, origin.x, origin.y) as LiquidClass;
        //如果接触目标不是相同液体，进行处理
        if (originLiquid != this) {

            liquidHandler.UpdateVolume(this, target, 0);
            ChunkManager.Instance.SetBlockId(LayerType.Foreground, target, medium.blockId);
            //world.SetTileClass(liquidHandler.test, Layers.Ground, target.x, target.y);
            //world.tilemaps[(int)Layers.Ground].SetTile((Vector3Int)target, liquidHandler.test.tile);
            return true;
        }
        return false;
    }
}
