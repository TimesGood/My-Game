using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;


//自定义Tile规则
[CreateAssetMenu(fileName ="CustomTile",menuName ="Tile/new CustomTile")]
public class CustomTile : RuleTile<CustomTile.Neighbor>
{
    public TileBase[] specifiedBlocks;
    public long blockId;

    //规则拓展
    public class Neighbor : RuleTile.TilingRule.Neighbor
    {
        //规则
        public const int Any = 3;//任何方块（自己或指定方块）
        public const int Specified = 4;//指定方块
        public const int notSpecified = 5;//非指定方块
        public const int Air = 6;//空气
        public const int notAir = 7;//非空
    }

    public override bool RuleMatch(int neighbor, TileBase other)
    {
        if (neighbor == Neighbor.Any) {
            return CheckAny(other);
        } else if (neighbor == Neighbor.Specified) {

            return CheckSpecified(other);
        } else if (neighbor == Neighbor.notSpecified) {
            return CheckNotSpecified(other);
        } else if (neighbor == Neighbor.Air) {
            return CheckAir(other);
        } else if (neighbor == Neighbor.notAir) {
            return CheckNotAir(other);
        }
        return base.RuleMatch(neighbor, other);
    }

    private bool CheckSpecified(TileBase other)
    {
        if (specifiedBlocks.Contains(other))
        {
            return true;
        }
        return false;
    }
    private bool CheckNotSpecified(TileBase other)
    {
        if (specifiedBlocks.Contains(other) || other == this)
        {
            return false;
        }
        return true;
    }

    private bool CheckAny(TileBase other)
    {
        if (specifiedBlocks.Contains(other) || other == this)
        {
            return true;
        }
        return false;
    }

    private bool CheckAir(TileBase other)
    {
        if (other == null) return true;
        return false;
    }

    private bool CheckNotAir(TileBase other) {
        if (other != null) return true;
        return false;
    }
}
