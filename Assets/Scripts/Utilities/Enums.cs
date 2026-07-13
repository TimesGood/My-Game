using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//方块类型枚举
public enum Layers
{
    Addons,//装饰、植株、忽略碰撞的瓦片
    Background,//背景
    Ground,//地面、需要碰撞的瓦片层
    Liquid//液体
}