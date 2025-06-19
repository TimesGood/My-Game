using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

//顶点生成限制类型
public enum LimitType {
    Circle,//圆
    Ellipse,//椭圆
    Rect//矩形
}

[CreateAssetMenu(fileName = "ShapeGenerator", menuName = "NoiseConfig/new ShapeGenerator")]
public class ShapeGenerator : NoiseConfig {



    [Header("多边形设置")]
    public int vertexCount = 5;          // 多边形顶点数
    public Vector2 offset = new Vector2(0, 0);//整体图形偏移
    public bool fillPolygon = true;      // 填充多边形
    [Space]
    public LimitType limitType = LimitType.Circle;
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
    [Header("裁剪")]
    public Vector2 leftLower = new Vector2(0, 0);   //裁剪左下点位
    public Vector2 rightUpper = new Vector2(100, 100);//裁剪右上点位
    [Space]
    [Header("贝塞尔曲线控制")]
    [Range(0, 1)] public float bezierStrength = 0.3f; // 曲线弯曲强度
    [Range(3, 64)] public int bezierSegments = 16;    // 每边曲线分段数
    public bool useBezierCurves = false;   //开启贝塞尔曲线
    [Header("Perlin曲线控制")]
    public float perlinFrequency = 0.05f;  // 频率
    public float perlinAmplitude = 20f;    // 幅度
    [Min(1)]
    public int perlinSegments = 10;   //采样分段，每隔seagmentCount个点采样一次，分段越高越趋于原始图像
    public bool usePerlinCurves = false;
    [Header("曲线叠加")]
    [Range(1, 5)]
    public int octaves = 1;          // 噪声层数
    public float persistence = 0.4f; // 振幅衰减
    public float lacunarity = 2.2f;  // 频率倍增





    protected override Texture2D GenerateNoise() {
        ClearTexture(_noiseTexture, Color.clear);

        GenerateRandomPolygon(_noiseTexture);
        return _noiseTexture;
    }

    //=== 随机多边形生成 ===//
    private void GenerateRandomPolygon(Texture2D tex) {
        Random.State originalState = Random.state;
        Random.InitState((int)seed);//随机种子

        Vector2[] vertices = GenerateRandomPolygonVertex();

        // 绘制并填充
        DrawShape(tex, vertices, Color.white);

        //ApplyDistortion();
        Random.state = originalState;
    }

    //生成多边形随机顶点
    private Vector2[] GenerateRandomPolygonVertex() {
        Vector2[] vertices = new Vector2[vertexCount];//顶点集合
        
        Vector2 center = new Vector2(noiseWidth / 2, noiseHeight / 2) + offset;//中点

        //顶点生成
        switch (limitType) {
            case LimitType.Circle:
                GenerateCircleVertex(center, vertices);
                break;
            case LimitType.Ellipse:
                GenerateEllipseVertex(center, vertices);
                break;
            case LimitType.Rect:
                GenerateRectVertex(center, vertices);
                break;
            default:
                break;
        }


        // 按极角排序顶点
        System.Array.Sort(vertices, (a, b) =>
            Mathf.Atan2(a.y - center.y, a.x - center.x).CompareTo(
            Mathf.Atan2(b.y - center.y, b.x - center.x)));

        return vertices;
    }

    //生成矩阵限制顶点
    private void GenerateRectVertex(Vector2 center, Vector2[] vertices) {
        Vector2 halfValidSize = rectSize * 0.5f;//
        Vector2 halfMinSize = halfValidSize * (1 - rectRange);
        

        for (int i = 0; i < vertexCount; i++) {
            float angle = i * Mathf.PI * 2 / vertexCount;//角度
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));//中点到顶点向量
            
            float x;
            // ===== 生成x坐标 =====
            if (dir.x > 0) // 左侧区域
            {
                x = Random.Range(center.x - halfValidSize.x, center.x - halfMinSize.x);
            } else // 右侧区域
              {
                x = Random.Range(center.x + halfMinSize.x, center.x + halfValidSize.x);
            }

            // ===== 生成y坐标 =====
            float y;
            if (dir.y < 0) // 下部区域
            {
                y = Random.Range(center.y - halfValidSize.y, center.y - halfMinSize.y);
            } else // 上部区域
              {
                y = Random.Range(center.y + halfMinSize.y, center.y + halfValidSize.y);
            }

            vertices[i] = new Vector2(x, y);
        }
    }
    //生成圆形限制顶点
    private void GenerateCircleVertex(Vector2 center, Vector2[] vertices) {
        for (int i = 0; i < vertexCount; i++) {

            float angle = i * Mathf.PI * 2 / vertexCount;//平均角度
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));//中点到顶点向量
            float radius = Random.Range(circleRadius * (1 - circleRange), circleRadius);//下一个点范围
            Vector2 dot = center + dir * radius;
            vertices[i] = dot;
        }
    }
    //生成椭圆限制顶点
    private void GenerateEllipseVertex(Vector2 center, Vector2[] vertices) {
        //椭圆限制内圆半径
        float innerRadius = ellipseRadius * (1 - ellipseRange);

        for (int i = 0; i < vertexCount; i++) {
            
            //float angle = Random.Range(0f, Mathf.PI * 2f);// 随机角度
            float angle = i * Mathf.PI * 2 / vertexCount;// 平均角度

            // 根据edgeBias计算半径插值
            float t = Mathf.Pow(Random.value, 1 - edgeBias);

            float radius = Mathf.Lerp(innerRadius, ellipseRadius, t);

            // 计算内外椭圆实际半径
            Vector2 outerRadii = new Vector2(
                ellipseRadius * ellipseScale.x,
                ellipseRadius * ellipseScale.y
            );
            Vector2 innerRadii = new Vector2(
                innerRadius * ellipseScale.x,
                innerRadius * ellipseScale.y
            );

            // 椭圆坐标计算（基于当前半径的比例插值）
            float currentScaleX = Mathf.Lerp(ellipseScale.x, ellipseScale.x, t);
            float currentScaleY = Mathf.Lerp(ellipseScale.y, ellipseScale.y, t);

            float x = center.x + Mathf.Cos(angle) * radius * currentScaleX;
            float y = center.y + Mathf.Sin(angle) * radius * currentScaleY;


            vertices[i] = new Vector2(x, y);
        }
    }




    //=== 基础绘图方法 ===//
    //=== 通用绘制方法 ===//
        /// <summary>
        /// 
        /// </summary>
        /// <param name="tex">画布</param>
        /// <param name="vertices">多边形随机顶点集合</param>
        /// <param name="color">绘制颜色</param>
    private void DrawShape(Texture2D tex, Vector2[] vertices, Color color) {
        Vector2[] path = vertices;
        //扭曲曲线
        if (usePerlinCurves)
            path = GeneratePerlinPath(vertices);
        else if (useBezierCurves)
            path = GenerateCurvedEdges(vertices);


        // 填充图形
        if (fillPolygon) {
            //ApplyDistortion(path);
            FillPolygon(tex, path, color);
        } else {
            // 绘制边界
            for (int i = 0; i < path.Length - 1; i++)
                DrawLine(tex, path[i], path[i + 1], color);
        }

    }

    //清除绘制
    private void ClearTexture(Texture2D tex, Color bgColor) {
        Color[] pixels = new Color[tex.width * tex.height];
        System.Array.Fill(pixels, bgColor);
        tex.SetPixels(pixels);
    }

    //绘制线
    private void DrawLine(Texture2D tex, Vector2 start, Vector2 end, Color color) {
        // Bresenham算法实现
        int x0 = (int)start.x, y0 = (int)start.y;
        int x1 = (int)end.x, y1 = (int)end.y;

        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        while (true) {
            if (IsInTexture(x0, y0))
                tex.SetPixel(x0, y0, color);

            if (x0 == x1 && y0 == y1) break;

            int e2 = 2 * err;
            if (e2 > -dy) { err -= dy; x0 += sx; }
            if (e2 < dx) { err += dx; y0 += sy; }
        }
    }
    
    //顶点是否在画布内

    private bool IsInTexture(int x, int y) {
        return (x > 0 && x < noiseWidth && y > 0 && y < noiseHeight) && IsInScope(x, y);
    }

    //顶点是否在限制范围内
    private bool IsInScope(int x, int y) {
        return x > leftLower.x && x < rightUpper.x && y > leftLower.y && y < rightUpper.y;
    }


    //填充
    private void FillPolygon(Texture2D tex, Vector2[] vertices, Color color) {
        // 获取多边形包围盒
        float minY = vertices.Min(v => v.y);
        float maxY = vertices.Max(v => v.y);
        // 扫描线遍历
        for (int y = (int)minY; y <= maxY; y++) {
            List<float> intersections = new List<float>();
            for (int i = 0; i < vertices.Length; i++) {
                Vector2 p1 = vertices[i];
                Vector2 p2 = vertices[(i + 1) % vertices.Length];
                
                if ((p1.y > y && p2.y <= y) || (p2.y > y && p1.y <= y)) {
                    float x = p1.x + (y - p1.y) / (p2.y - p1.y) * (p2.x - p1.x);
                    intersections.Add(x);
                }
            }

            intersections.Sort();
            for (int i = 0; i < intersections.Count; i += 2) {
                int startX = Mathf.Clamp((int)intersections[i], 0, tex.width - 1);
                int endX = Mathf.Clamp((int)intersections[i + 1], 0, tex.width - 1);
                for (int x = startX; x <= endX; x++) {
                    if (IsInTexture(x, y)) 
                        tex.SetPixel(x, y, color);  
                }

            }
            
        }
    }

    #region 曲线路径生成

    // 生成贝塞尔曲线边
    private Vector2[] GenerateCurvedEdges(Vector2[] vertices) {
        List<Vector2> path = new List<Vector2>();
        for (int i = 0; i < vertices.Length; i++) {
            Vector2 p0 = vertices[i];
            Vector2 p3 = vertices[(i + 1) % vertexCount];

            // 计算控制点（垂直于边中点）
            Vector2 mid = (p0 + p3) * 0.5f;//中点
            Vector2 dir = (p3 - p0).normalized;//向量
            Vector2 normal = new Vector2(-dir.y, dir.x);
            Vector2 p1 = mid + normal * bezierStrength * noiseWidth;
            Vector2 p2 = mid - normal * bezierStrength * noiseWidth;


            // 三次贝塞尔曲线采样
            for (int t = 0; t <= bezierSegments; t++) {
                float u = t / (float)bezierSegments;
                Vector2 point = CalculateCubicBezier(p0, p1, p2, p3, u);
                path.Add(point);
            }
        }
        return path.ToArray();
    }
    // 三次贝塞尔曲线计算
    private Vector2 CalculateCubicBezier(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t) {
        float u = 1 - t;
        return
            u * u * u * p0 +
            3 * u * u * t * p1 +
            3 * u * t * t * p2 +
            t * t * t * p3;
    }
    // 柏林曲线
    private Vector2[] GeneratePerlinPath(Vector2[] vertices) {
        List<Vector2> path = new List<Vector2>();

        for (int i = 0; i < vertices.Length; i++) {
            Vector2 start = vertices[i];
            Vector2 end = vertices[(i + 1) % vertexCount];

            // 沿直线路径采样，
            int segments = Mathf.CeilToInt(Vector2.Distance(start, end) / perlinSegments);
            for (int t = 0; t <= segments; t++) {
                float lerp = t / (float)segments;
                Vector2 basePoint = Vector2.Lerp(start, end, lerp);

                // ===== 分形噪声计算 =====
                float noiseX = 0f;
                float noiseY = 0f;
                float frequency = perlinFrequency;
                float amplitude = 1f;
                float maxAmplitude = 0f;
                //叠加曲线
                for (int oct = 0; oct < octaves; oct++) {
                    // 每层使用不同种子偏移
                    float octaveSeed = seed + oct * 1000;

                    // X轴噪声
                    float nx = (basePoint.x + octaveSeed) * frequency;
                    float ny = (basePoint.y + octaveSeed) * frequency;
                    noiseX += Mathf.PerlinNoise(nx, ny) * amplitude;

                    // Y轴噪声（使用不同采样坐标）
                    float nx2 = (basePoint.x + octaveSeed + 1000) * frequency;
                    float ny2 = (basePoint.y + octaveSeed + 1000) * frequency;
                    noiseY += Mathf.PerlinNoise(nx2, ny2) * amplitude;

                    maxAmplitude += amplitude;
                    amplitude *= persistence;
                    frequency *= lacunarity;
                }

                //// 归一化到[-1,1]范围，防止整体图形发生偏移
                noiseX = (noiseX / maxAmplitude) * 2 - 1;
                noiseY = (noiseY / maxAmplitude) * 2 - 1;

                // 应用偏移
                Vector2 offset = new Vector2(
                    noiseX * perlinAmplitude,
                    noiseY * perlinAmplitude
                );

                path.Add(basePoint + offset);
            }
        }
        return path.ToArray();
    }
    #endregion

    //GPU填充
    public void ApplyDistortion(Vector2[] paths) {
        // 创建可写纹理
        RenderTextureFormat format = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.R8) ? RenderTextureFormat.ARGB32 : RenderTextureFormat.ARGB32;
        RenderTexture resultTexture = new RenderTexture(noiseWidth, noiseHeight, 0, format) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Point };
        resultTexture.enableRandomWrite = true; // 关键参数
        resultTexture.Create();


        ComputeShader shader = Resources.Load<ComputeShader>("Shader/PolygonGenerator");
        int kernel = shader.FindKernel("RasterizePolygon");

        int pathPointsIndex = Shader.PropertyToID("PathPoints");
        ComputeBuffer pathPoints = new ComputeBuffer(paths.Length, sizeof(float) * 2);
        pathPoints.SetData(paths);


        // 设置Shader参数
        shader.SetTexture(kernel, "Result", resultTexture);
        shader.SetInt("VertexCount", pathPoints.count);
        shader.SetBuffer(kernel, pathPointsIndex, pathPoints);

        // 分派线程
        shader.Dispatch(kernel,
            Mathf.CeilToInt(noiseWidth / 8.0f),
            Mathf.CeilToInt(noiseHeight / 8.0f),
            1);
        _noiseTexture = ToTexture2D(resultTexture);
        pathPoints.Release();
        resultTexture.Release();
        
    }

    //旋转
    public Texture2D Rotate(Texture2D tex, float degress) {

        // 创建可写纹理
        RenderTextureFormat format = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.R8) ? RenderTextureFormat.ARGB32 : RenderTextureFormat.ARGB32;
        RenderTexture resultTexture = new RenderTexture(noiseWidth, noiseHeight, 0, format) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Point };
        resultTexture.enableRandomWrite = true; // 关键参数
        resultTexture.Create();

        ComputeShader shader = Resources.Load<ComputeShader>("Shader/PolygonGenerator");
        int kernel = shader.FindKernel("Rotate");

        shader.SetTexture(kernel, "SourceTexture", tex);
        shader.SetTexture(kernel, "Result", resultTexture);
        shader.SetVector("TextureSize", new Vector2(tex.width, tex.height));

        // 设置变换参数
        shader.SetVector("Offset", offset);
        shader.SetFloat("RotationAngle", degress * Mathf.Deg2Rad);
        shader.SetVector("Pivot", new Vector2(0.5f, 0.5f));

        // 分派线程
        shader.Dispatch(kernel,
            Mathf.CeilToInt(tex.width / 8.0f),
            Mathf.CeilToInt(tex.height / 8.0f),
            1);
  
        Texture2D tex_ =  ToTexture2D(resultTexture);
        
        return tex_;
    }
}