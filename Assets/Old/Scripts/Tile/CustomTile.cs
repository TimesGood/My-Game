using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;


//自定义Tile规则
[CreateAssetMenu(fileName ="CustomTile",menuName ="Tile/new CustomTile")]
public class CustomTile : RuleTile<CustomTile.Neighbor>
{
    public TileBase[] specifiedBlocks;
    public int blockId;

    //规则拓展
    public class Neighbor : RuleTile.TilingRule.Neighbor
    {
        //规则
        public const int Any = 3;//任何方块
        public const int Specified = 4;//指定方块
        public const int notSpecified = 5;
        public const int Air = 6;//空气
    }

    public override bool RuleMatch(int neighbor, TileBase other)
    {
        if (neighbor == 3)
        {
            return CheckAny(other);
        }
        else if (neighbor == 4)
        {

            return CheckSpecified(other);
        }
        else if (neighbor == 5)
        {
            return CheckNotSpecified(other);
        }
        else if (neighbor == 6)
        {
            return CheckAir(other);
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
}
