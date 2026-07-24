using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//方块类型枚举
public enum LayerType
{
    Addons,//装饰层。放置植株等忽略碰撞体的瓦片
    Background,//背景层。放置背景墙
    Foreground,//前景层。放置需要碰撞体的瓦片
    Liquid//液体层/覆盖层。可叠加于其他层级
}