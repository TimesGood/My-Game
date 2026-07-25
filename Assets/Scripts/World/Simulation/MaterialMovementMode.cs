/// <summary>
/// 材料运动模式，决定材料在物理模拟中的行为
/// </summary>
public enum MaterialMovementMode {
    /// <summary>静态材料，不会移动（石头、墙壁等）</summary>
    Static,
    /// <summary>粉末/颗粒材料，受重力影响下落（沙子、砾石等）</summary>
    Powder,
    /// <summary>液体材料，受重力影响并可横向流动（水、岩浆等）</summary>
    Liquid
}
