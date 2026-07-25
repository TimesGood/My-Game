using UnityEngine;

/// <summary>
/// 材料物理定义，描述可模拟材料的物理属性
/// 借鉴 PixelAlchemy 的材料驱动设计理念
/// </summary>
[System.Serializable]
public class SimulationMaterialDefinition {
    [Header("基础属性")]
    public MaterialMovementMode movementMode = MaterialMovementMode.Static;
    public int density = 50;                           // 密度，决定位移优先级（0-100）

    [Header("移动控制")]
    [Range(0f, 1f)]
    public float moveProbability = 1f;                 // 每帧移动概率
    [Range(0f, 1f)]
    public float lateralProbability = 0.8f;            // 液体横向流动概率
    public int horizontalSearchDistance = 3;            // 液体横向搜索距离（格子数，保留兼容）

    [Header("交互属性")]
    public bool canBeDisplaced = true;                 // 是否可以被其他材料挤开
    public bool isFlammable = false;                   // 是否可燃
    public float flammability = 0f;                    // 可燃性（0-1）
    public float ignitionTemperature = 100f;           // 点燃温度

    [Header("液体特有属性")]
    public float minVolume = 0.005f;                   // 最小液体量（低于此值移除）
    public float maxVolume = 1f;                       // 最大液体量（单格子容量）
    public float flowSpeed = 10f;                      // 流动速度（每秒更新次数，值越大越快）
    public int horizontalSpreadDistance = 3;            // 水平扩散距离

    /// <summary>
    /// 检查是否为静态材料
    /// </summary>
    public bool IsStatic => movementMode == MaterialMovementMode.Static;

    /// <summary>
    /// 检查是否为粉末材料
    /// </summary>
    public bool IsPowder => movementMode == MaterialMovementMode.Powder;

    /// <summary>
    /// 检查是否为液体材料
    /// </summary>
    public bool IsLiquid => movementMode == MaterialMovementMode.Liquid;

    /// <summary>
    /// 检查是否可以移动
    /// </summary>
    public bool CanMove => movementMode != MaterialMovementMode.Static;

    /// <summary>
    /// 检查是否可以垂直移动
    /// </summary>
    public bool CanMoveVertical => movementMode == MaterialMovementMode.Powder ||
                                   movementMode == MaterialMovementMode.Liquid;

    /// <summary>
    /// 检查是否可以对角线移动（粉末特性）
    /// </summary>
    public bool CanMoveDiagonal => movementMode == MaterialMovementMode.Powder;

    /// <summary>
    /// 检查是否可以水平移动（液体特性）
    /// </summary>
    public bool CanMoveHorizontal => movementMode == MaterialMovementMode.Liquid;

    /// <summary>
    /// 检查两个材料之间是否可以发生位移
    /// </summary>
    /// <param name="source">源材料</param>
    /// <param name="target">目标材料</param>
    /// <param name="isDownward">是否向下移动</param>
    /// <returns>是否可以位移</returns>
    public static bool CanDisplace(SimulationMaterialDefinition source, SimulationMaterialDefinition target, bool isDownward) {
        if (source == null || target == null) return false;
        if (!target.canBeDisplaced) return false;

        // 向下移动时，高密度材料可以挤开低密度材料
        if (isDownward) {
            return source.density > target.density;
        }

        // 向上移动时，低密度材料可以挤开高密度材料
        return source.density < target.density;
    }
}
