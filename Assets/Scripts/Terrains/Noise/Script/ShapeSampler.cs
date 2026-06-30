using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Color = UnityEngine.Color;

public class ShapeSampler
{

    /// <summary>
    /// 为整个区域生成噪声纹理（输出原始噪声值，不应用阈值）。
    /// 优先使用 GPU（ComputeShader），失败时自动回退到 CPU。
    /// 调用方在读取时自行判断阈值：tex.GetPixel(x,y).r > threshold
    /// </summary>
    public static Texture2D GenerateTexture(int _width, int _height, ShapeParams _p, int _seed) {
        return GenerateRandomPolygon(_width, _height, _p, _seed);

    }

    //=== 随机多边形生成 ===//
    private static Texture2D GenerateRandomPolygon(int _width, int _height, ShapeParams _p, int _seed) {
        Texture2D tex = new Texture2D(_width, _height, TextureFormat.RGBA32, false) {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Point
        };
        Color[] pixels = new Color[tex.width * tex.height];
        System.Array.Fill(pixels, Color.clear);
        tex.SetPixels(pixels);


        Random.State originalState = Random.state;
        Random.InitState((int)_seed);//随机种子

        Vector2[] vertices = GenerateRandomPolygonVertex(_width, _height, _p);

        // 绘制并填充
        DrawShape(tex, vertices, Color.white, _width, _height, _p, _seed);

        tex.Apply();
        return tex;
        //ApplyDistortion();
        Random.state = originalState;
    }

    //生成多边形随机顶点
    private static Vector2[] GenerateRandomPolygonVertex(int _width, int _height, ShapeParams _p) {
        Vector2[] vertices = new Vector2[_p.vertexCount];//顶点集合

        Vector2 center = new Vector2(_width / 2, _height / 2) + _p.offset;//中点

        //顶点生成
        switch (_p.limitType) {
            case LimitType_.Circle:
                GenerateCircleVertex(center, vertices, _p);
                break;
            case LimitType_.Ellipse:
                GenerateEllipseVertex(center, vertices, _p);
                break;
            case LimitType_.Rect:
                GenerateRectVertex(center, vertices, _p);
                break;
            default:
                break;
        }

        // 随机旋转一下
        float randomAngle = Random.Range(0f, 360f);  // 生成随机角度
        vertices = RotatePoints(vertices, center, randomAngle);

        // 按极角排序顶点
        System.Array.Sort(vertices, (a, b) =>
            Mathf.Atan2(a.y - center.y, a.x - center.x).CompareTo(
            Mathf.Atan2(b.y - center.y, b.x - center.x)));

        return vertices;
    }

    // 旋转角度
    private static Vector2[] RotatePoints(Vector2[] points, Vector2 center, float angleDeg) {
        float rad = angleDeg * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        Vector2[] result = new Vector2[points.Length];
        for (int i = 0; i < points.Length; i++) {
            Vector2 p = points[i];
            // 1. 平移到原点
            Vector2 dir = p - center;
            // 2. 旋转
            float x = dir.x * cos - dir.y * sin;
            float y = dir.x * sin + dir.y * cos;
            // 3. 平移回去
            result[i] = new Vector2(x + center.x, y + center.y);
        }
        return result;
    }

    //生成矩阵限制顶点
    private static void GenerateRectVertex(Vector2 center, Vector2[] vertices, ShapeParams _p) {
        Vector2 halfValidSize = _p.rectSize * 0.5f;//
        Vector2 halfMinSize = halfValidSize * (1 - _p.rectRange);


        for (int i = 0; i < _p.vertexCount; i++) {
            float angle = i * Mathf.PI * 2 / _p.vertexCount;//角度
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
    private static void GenerateCircleVertex(Vector2 center, Vector2[] vertices, ShapeParams _p) {
        for (int i = 0; i < _p.vertexCount; i++) {

            float angle = i * Mathf.PI * 2 / _p.vertexCount;//平均角度
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));//中点到顶点向量
            float radius = Random.Range(_p.circleRadius * (1 - _p.circleRange), _p.circleRadius);//下一个点范围
            Vector2 dot = center + dir * radius;
            vertices[i] = dot;
        }
    }
    //生成椭圆限制顶点
    private static void GenerateEllipseVertex(Vector2 center, Vector2[] vertices, ShapeParams _p) {
        //椭圆限制内圆半径
        float innerRadius = _p.ellipseRadius * (1 - _p.ellipseRange);

        for (int i = 0; i < _p.vertexCount; i++) {

            //float angle = Random.Range(0f, Mathf.PI * 2f);// 随机角度
            float angle = i * Mathf.PI * 2 / _p.vertexCount;// 平均角度

            // 根据edgeBias计算半径插值
            float t = Mathf.Pow(Random.value, 1 - _p.edgeBias);

            float radius = Mathf.Lerp(innerRadius, _p.ellipseRadius, t);

            // 计算内外椭圆实际半径
            Vector2 outerRadii = new Vector2(
                _p.ellipseRadius * _p.ellipseScale.x,
                _p.ellipseRadius * _p.ellipseScale.y
            );
            Vector2 innerRadii = new Vector2(
                innerRadius * _p.ellipseScale.x,
                innerRadius * _p.ellipseScale.y
            );

            // 椭圆坐标计算（基于当前半径的比例插值）
            float currentScaleX = Mathf.Lerp(_p.ellipseScale.x, _p.ellipseScale.x, t);
            float currentScaleY = Mathf.Lerp(_p.ellipseScale.y, _p.ellipseScale.y, t);

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
    private static void DrawShape(Texture2D tex, Vector2[] vertices, Color color, int _width, int _height, ShapeParams _p, int _seed) {
        Vector2[] path = vertices;
        //扭曲曲线
        switch (_p.curvesType) {
            case CurvesType.Bezier:
                path = GenerateCurvedEdges(vertices, _seed, _p);
                break;
            case CurvesType.Perlin:
                path = GeneratePerlinPath(vertices, _seed, _p);
                break;
            default:
                break;

        }
        // 填充图形
        if (_p.fillPolygon) {
            //ApplyDistortion(path);
            FillPolygon(tex, path, color, _width, _height, _p);
        } else {
            // 绘制边界
            for (int i = 0; i < path.Length - 1; i++)
                DrawLine(tex, path[i], path[i + 1], color, _width, _height, _p);
        }

    }

    //清除绘制
    private void ClearTexture(Texture2D tex, Color bgColor) {
        Color[] pixels = new Color[tex.width * tex.height];
        System.Array.Fill(pixels, bgColor);
        tex.SetPixels(pixels);
    }

    //绘制线
    private static void DrawLine(Texture2D tex, Vector2 start, Vector2 end, Color color, int _width, int _height, ShapeParams _p) {
        // Bresenham算法实现
        int x0 = (int)start.x, y0 = (int)start.y;
        int x1 = (int)end.x, y1 = (int)end.y;

        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        while (true) {
            if (IsInTexture(x0, y0,_width, _height, _p))
                tex.SetPixel(x0, y0, color);

            if (x0 == x1 && y0 == y1) break;

            int e2 = 2 * err;
            if (e2 > -dy) { err -= dy; x0 += sx; }
            if (e2 < dx) { err += dx; y0 += sy; }
        }
    }

    //顶点是否在画布内

    private static bool IsInTexture(int x, int y, int _width, int _height, ShapeParams _p) {
        return (x > 0 && x < _width && y > 0 && y < _height) && IsInScope(x, y, _p);
    }

    //顶点是否在限制范围内
    private static bool IsInScope(int x, int y, ShapeParams _p) {
        return x > _p.leftLower.x && x < _p.rightUpper.x && y > _p.leftLower.y && y < _p.rightUpper.y;
    }


    //填充
    private static void FillPolygon(Texture2D tex, Vector2[] vertices, Color color, int _width, int _height, ShapeParams _p) {
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
                    if (IsInTexture(x, y, _width, _height, _p))
                        tex.SetPixel(x, y, color);
                }

            }

        }
    }

    #region 曲线路径生成

    // 生成贝塞尔曲线边
    private static Vector2[] GenerateCurvedEdges(Vector2[] vertices, int _width, ShapeParams _p) {
        List<Vector2> path = new List<Vector2>();
        for (int i = 0; i < vertices.Length; i++) {
            Vector2 p0 = vertices[i];
            Vector2 p3 = vertices[(i + 1) % _p.vertexCount];

            // 计算控制点（垂直于边中点）
            Vector2 mid = (p0 + p3) * 0.5f;//中点
            Vector2 dir = (p3 - p0).normalized;//向量
            Vector2 normal = new Vector2(-dir.y, dir.x);
            Vector2 p1 = mid + normal * _p.bezierStrength * _width;
            Vector2 p2 = mid - normal * _p.bezierStrength * _width;


            // 三次贝塞尔曲线采样
            for (int t = 0; t <= _p.segments; t++) {
                float u = t / (float)_p.segments;
                Vector2 point = CalculateCubicBezier(p0, p1, p2, p3, u);
                path.Add(point);
            }
        }
        return path.ToArray();
    }
    // 三次贝塞尔曲线计算
    private static Vector2 CalculateCubicBezier(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t) {
        float u = 1 - t;
        return
            u * u * u * p0 +
            3 * u * u * t * p1 +
            3 * u * t * t * p2 +
            t * t * t * p3;
    }
    // 柏林曲线
    private static Vector2[] GeneratePerlinPath(Vector2[] vertices, int _seed, ShapeParams _p) {
        List<Vector2> path = new List<Vector2>();

        for (int i = 0; i < vertices.Length; i++) {
            Vector2 start = vertices[i];
            Vector2 end = vertices[(i + 1) % _p.vertexCount];

            // 沿直线路径采样，
            int segments = Mathf.CeilToInt(Vector2.Distance(start, end) / _p.segments);
            for (int t = 0; t <= segments; t++) {
                float lerp = t / (float)segments;
                Vector2 basePoint = Vector2.Lerp(start, end, lerp);

                // ===== 分形噪声计算 =====
                float noiseX = 0f;
                float noiseY = 0f;
                float frequency = _p.perlinFrequency;
                float amplitude = 1f;
                float maxAmplitude = 0f;
                //叠加曲线
                for (int oct = 0; oct < _p.octaves; oct++) {
                    // 每层使用不同种子偏移
                    float octaveSeed = _seed + oct * 1000;

                    // X轴噪声
                    float nx = (basePoint.x + octaveSeed) * frequency;
                    float ny = (basePoint.y + octaveSeed) * frequency;
                    noiseX += Mathf.PerlinNoise(nx, ny) * amplitude;

                    // Y轴噪声（使用不同采样坐标）
                    float nx2 = (basePoint.x + octaveSeed + 1000) * frequency;
                    float ny2 = (basePoint.y + octaveSeed + 1000) * frequency;
                    noiseY += Mathf.PerlinNoise(nx2, ny2) * amplitude;

                    maxAmplitude += amplitude;
                    amplitude *= _p.persistence;
                    frequency *= _p.lacunarity;
                }

                //// 归一化到[-1,1]范围，防止整体图形发生偏移
                noiseX = (noiseX / maxAmplitude) * 2 - 1;
                noiseY = (noiseY / maxAmplitude) * 2 - 1;

                // 应用偏移
                Vector2 offset = new Vector2(
                    noiseX * _p.perlinAmplitude,
                    noiseY * _p.perlinAmplitude
                );

                path.Add(basePoint + offset);
            }
        }
        return path.ToArray();
    }
    #endregion

    //GPU填充
    //public void ApplyDistortion(Vector2[] paths) {
    //    // 创建可写纹理
    //    RenderTextureFormat format = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.R8) ? RenderTextureFormat.ARGB32 : RenderTextureFormat.ARGB32;
    //    RenderTexture resultTexture = new RenderTexture(noiseWidth, noiseHeight, 0, format) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Point };
    //    resultTexture.enableRandomWrite = true; // 关键参数
    //    resultTexture.Create();


    //    ComputeShader shader = Resources.Load<ComputeShader>("Shader/PolygonGenerator");
    //    int kernel = shader.FindKernel("RasterizePolygon");

    //    int pathPointsIndex = Shader.PropertyToID("PathPoints");
    //    ComputeBuffer pathPoints = new ComputeBuffer(paths.Length, sizeof(float) * 2);
    //    pathPoints.SetData(paths);


    //    // 设置Shader参数
    //    shader.SetTexture(kernel, "Result", resultTexture);
    //    shader.SetInt("VertexCount", pathPoints.count);
    //    shader.SetBuffer(kernel, pathPointsIndex, pathPoints);

    //    // 分派线程
    //    shader.Dispatch(kernel,
    //        Mathf.CeilToInt(noiseWidth / 8.0f),
    //        Mathf.CeilToInt(noiseHeight / 8.0f),
    //        1);
    //    _noiseTexture = ToTexture2D(resultTexture);
    //    pathPoints.Release();
    //    resultTexture.Release();

    //}

    ////旋转
    //public Texture2D Rotate(Texture2D tex, float degress) {

    //    // 创建可写纹理
    //    RenderTextureFormat format = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.R8) ? RenderTextureFormat.ARGB32 : RenderTextureFormat.ARGB32;
    //    RenderTexture resultTexture = new RenderTexture(noiseWidth, noiseHeight, 0, format) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Point };
    //    resultTexture.enableRandomWrite = true; // �ؼ�����
    //    resultTexture.Create();

    //    ComputeShader shader = Resources.Load<ComputeShader>("Shader/PolygonGenerator");
    //    int kernel = shader.FindKernel("Rotate");

    //    shader.SetTexture(kernel, "SourceTexture", tex);
    //    shader.SetTexture(kernel, "Result", resultTexture);
    //    shader.SetVector("TextureSize", new Vector2(tex.width, tex.height));

    //    // 设置变换参数
    //    shader.SetVector("Offset", offset);
    //    shader.SetFloat("RotationAngle", degress * Mathf.Deg2Rad);
    //    shader.SetVector("Pivot", new Vector2(0.5f, 0.5f));

    //    // 分派线程
    //    shader.Dispatch(kernel,
    //        Mathf.CeilToInt(tex.width / 8.0f),
    //        Mathf.CeilToInt(tex.height / 8.0f),
    //        1);

    //    Texture2D tex_ = ToTexture2D(resultTexture);

    //    return tex_;
    //}
}
