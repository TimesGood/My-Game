using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//顶点生成限制类型
public enum LimitType_ {
    Circle,//圆
    Ellipse,//椭圆
    Rect//矩形
}
public enum CurvesType {
    Empty,
    Bezier,
    Perlin
}

[System.Serializable]
public class ShapeParams
{
    [Header("多边形设置")]
    public int vertexCount = 5;          // 多边形顶点数
    public Vector2 offset = new Vector2(0, 0);//整体图形偏移
    public bool fillPolygon = true;      // 填充多边形
    [Header("裁剪")]
    public Vector2 leftLower = new Vector2(0, 0);   //裁剪左下点位
    public Vector2 rightUpper = new Vector2(100, 100);//裁剪右上点位
    [Space]
    [Header("顶点生成限制")]
    [Header("限制类型")]
    public LimitType_ limitType = LimitType_.Circle;
    [Header("圆形顶点范围限制")]
    public float circleRadius = 100;      // 顶点随机生成外圆半径
    [Range(0.001f, 1)]
    public float circleRange = 0.8f;     // 半径范围
    [Header("椭圆顶点范围限制")]
    public float ellipseRadius = 150f;     // 椭圆基础半径
    [Range(0.001f, 1)]
    public float ellipseRange = 0.8f;
    public Vector2 ellipseScale = new Vector2(1, 1);  // 椭圆XY轴缩放
    [Range(0, 1)] public float edgeBias = 0.5f; // 分布偏向（0=靠近内圈，1=靠近外圈）
    [Header("方形顶点范围限制")]
    public Vector2 rectSize = new Vector2(100, 100);
    [Range(0.001f, 1)]
    public float rectRange = 0.8f;
    [Space]
    [Header("曲线采样")]
    [Header("曲线类型")]
    public CurvesType curvesType = CurvesType.Empty;
    [Range(3, 64)]
    public int segments;// 每段采样点
    [Header("贝塞尔曲线控制")]
    [Range(0, 1)] public float bezierStrength = 0.3f; // 曲线弯曲强度
    [Header("Perlin曲线控制")]
    public float perlinFrequency = 0.05f;  // 频率
    public float perlinAmplitude = 20f;    // 幅度
    [Header("曲线叠加")]
    [Range(1, 5)]
    public int octaves = 1;          // 噪声层数
    public float persistence = 0.4f; // 振幅衰减
    public float lacunarity = 2.2f;  // 频率倍增
}
