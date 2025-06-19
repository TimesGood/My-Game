using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

//����������������
public enum LimitType {
    Circle,//Բ
    Ellipse,//��Բ
    Rect//����
}

[CreateAssetMenu(fileName = "ShapeGenerator", menuName = "NoiseConfig/new ShapeGenerator")]
public class ShapeGenerator : NoiseConfig {



    [Header("���������")]
    public int vertexCount = 5;          // ����ζ�����
    public Vector2 offset = new Vector2(0, 0);//����ͼ��ƫ��
    public bool fillPolygon = true;      // �������
    [Space]
    public LimitType limitType = LimitType.Circle;
    [Header("Բ�ζ��㷶Χ����")]
    public float circleRadius = 100;      // �������������Բ�뾶
    [Range(0.001f, 1)]
    public float circleRange = 0.8f;     // �뾶��Χ
    [Header("��Բ���㷶Χ����")]
    public float ellipseRadius = 150f;     // ��Բ�����뾶
    [Range(0.001f, 1)]
    public float ellipseRange = 0.8f;  
    public Vector2 ellipseScale = new Vector2(1, 1);  // ��ԲXY������
    [Range(0, 1)] public float edgeBias = 0.5f; // �ֲ�ƫ��0=������Ȧ��1=������Ȧ��
    [Header("���ζ��㷶Χ����")]
    public Vector2 rectSize = new Vector2(100, 100);
    [Range(0.001f, 1)]
    public float rectRange = 0.8f;
    [Header("�ü�")]
    public Vector2 leftLower = new Vector2(0, 0);   //�ü����µ�λ
    public Vector2 rightUpper = new Vector2(100, 100);//�ü����ϵ�λ
    [Space]
    [Header("���������߿���")]
    [Range(0, 1)] public float bezierStrength = 0.3f; // ��������ǿ��
    [Range(3, 64)] public int bezierSegments = 16;    // ÿ�����߷ֶ���
    public bool useBezierCurves = false;   //��������������
    [Header("Perlin���߿���")]
    public float perlinFrequency = 0.05f;  // Ƶ��
    public float perlinAmplitude = 20f;    // ����
    [Min(1)]
    public int perlinSegments = 10;   //�����ֶΣ�ÿ��seagmentCount�������һ�Σ��ֶ�Խ��Խ����ԭʼͼ��
    public bool usePerlinCurves = false;
    [Header("���ߵ���")]
    [Range(1, 5)]
    public int octaves = 1;          // ��������
    public float persistence = 0.4f; // ���˥��
    public float lacunarity = 2.2f;  // Ƶ�ʱ���





    protected override Texture2D GenerateNoise() {
        ClearTexture(_noiseTexture, Color.clear);

        GenerateRandomPolygon(_noiseTexture);
        return _noiseTexture;
    }

    //=== ������������ ===//
    private void GenerateRandomPolygon(Texture2D tex) {
        Random.State originalState = Random.state;
        Random.InitState((int)seed);//�������

        Vector2[] vertices = GenerateRandomPolygonVertex();

        // ���Ʋ����
        DrawShape(tex, vertices, Color.white);

        //ApplyDistortion();
        Random.state = originalState;
    }

    //���ɶ�����������
    private Vector2[] GenerateRandomPolygonVertex() {
        Vector2[] vertices = new Vector2[vertexCount];//���㼯��
        
        Vector2 center = new Vector2(noiseWidth / 2, noiseHeight / 2) + offset;//�е�

        //��������
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


        // ���������򶥵�
        System.Array.Sort(vertices, (a, b) =>
            Mathf.Atan2(a.y - center.y, a.x - center.x).CompareTo(
            Mathf.Atan2(b.y - center.y, b.x - center.x)));

        return vertices;
    }

    //���ɾ������ƶ���
    private void GenerateRectVertex(Vector2 center, Vector2[] vertices) {
        Vector2 halfValidSize = rectSize * 0.5f;//
        Vector2 halfMinSize = halfValidSize * (1 - rectRange);
        

        for (int i = 0; i < vertexCount; i++) {
            float angle = i * Mathf.PI * 2 / vertexCount;//�Ƕ�
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));//�е㵽��������
            
            float x;
            // ===== ����x���� =====
            if (dir.x > 0) // �������
            {
                x = Random.Range(center.x - halfValidSize.x, center.x - halfMinSize.x);
            } else // �Ҳ�����
              {
                x = Random.Range(center.x + halfMinSize.x, center.x + halfValidSize.x);
            }

            // ===== ����y���� =====
            float y;
            if (dir.y < 0) // �²�����
            {
                y = Random.Range(center.y - halfValidSize.y, center.y - halfMinSize.y);
            } else // �ϲ�����
              {
                y = Random.Range(center.y + halfMinSize.y, center.y + halfValidSize.y);
            }

            vertices[i] = new Vector2(x, y);
        }
    }
    //����Բ�����ƶ���
    private void GenerateCircleVertex(Vector2 center, Vector2[] vertices) {
        for (int i = 0; i < vertexCount; i++) {

            float angle = i * Mathf.PI * 2 / vertexCount;//ƽ���Ƕ�
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));//�е㵽��������
            float radius = Random.Range(circleRadius * (1 - circleRange), circleRadius);//��һ���㷶Χ
            Vector2 dot = center + dir * radius;
            vertices[i] = dot;
        }
    }
    //������Բ���ƶ���
    private void GenerateEllipseVertex(Vector2 center, Vector2[] vertices) {
        //��Բ������Բ�뾶
        float innerRadius = ellipseRadius * (1 - ellipseRange);

        for (int i = 0; i < vertexCount; i++) {
            
            //float angle = Random.Range(0f, Mathf.PI * 2f);// ����Ƕ�
            float angle = i * Mathf.PI * 2 / vertexCount;// ƽ���Ƕ�

            // ����edgeBias����뾶��ֵ
            float t = Mathf.Pow(Random.value, 1 - edgeBias);

            float radius = Mathf.Lerp(innerRadius, ellipseRadius, t);

            // ����������Բʵ�ʰ뾶
            Vector2 outerRadii = new Vector2(
                ellipseRadius * ellipseScale.x,
                ellipseRadius * ellipseScale.y
            );
            Vector2 innerRadii = new Vector2(
                innerRadius * ellipseScale.x,
                innerRadius * ellipseScale.y
            );

            // ��Բ������㣨���ڵ�ǰ�뾶�ı�����ֵ��
            float currentScaleX = Mathf.Lerp(ellipseScale.x, ellipseScale.x, t);
            float currentScaleY = Mathf.Lerp(ellipseScale.y, ellipseScale.y, t);

            float x = center.x + Mathf.Cos(angle) * radius * currentScaleX;
            float y = center.y + Mathf.Sin(angle) * radius * currentScaleY;


            vertices[i] = new Vector2(x, y);
        }
    }




    //=== ������ͼ���� ===//
    //=== ͨ�û��Ʒ��� ===//
        /// <summary>
        /// 
        /// </summary>
        /// <param name="tex">����</param>
        /// <param name="vertices">�����������㼯��</param>
        /// <param name="color">������ɫ</param>
    private void DrawShape(Texture2D tex, Vector2[] vertices, Color color) {
        Vector2[] path = vertices;
        //Ť������
        if (usePerlinCurves)
            path = GeneratePerlinPath(vertices);
        else if (useBezierCurves)
            path = GenerateCurvedEdges(vertices);


        // ���ͼ��
        if (fillPolygon) {
            //ApplyDistortion(path);
            FillPolygon(tex, path, color);
        } else {
            // ���Ʊ߽�
            for (int i = 0; i < path.Length - 1; i++)
                DrawLine(tex, path[i], path[i + 1], color);
        }

    }

    //�������
    private void ClearTexture(Texture2D tex, Color bgColor) {
        Color[] pixels = new Color[tex.width * tex.height];
        System.Array.Fill(pixels, bgColor);
        tex.SetPixels(pixels);
    }

    //������
    private void DrawLine(Texture2D tex, Vector2 start, Vector2 end, Color color) {
        // Bresenham�㷨ʵ��
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
    
    //�����Ƿ��ڻ�����

    private bool IsInTexture(int x, int y) {
        return (x > 0 && x < noiseWidth && y > 0 && y < noiseHeight) && IsInScope(x, y);
    }

    //�����Ƿ������Ʒ�Χ��
    private bool IsInScope(int x, int y) {
        return x > leftLower.x && x < rightUpper.x && y > leftLower.y && y < rightUpper.y;
    }


    //���
    private void FillPolygon(Texture2D tex, Vector2[] vertices, Color color) {
        // ��ȡ����ΰ�Χ��
        float minY = vertices.Min(v => v.y);
        float maxY = vertices.Max(v => v.y);
        // ɨ���߱���
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

    #region ����·������

    // ���ɱ��������߱�
    private Vector2[] GenerateCurvedEdges(Vector2[] vertices) {
        List<Vector2> path = new List<Vector2>();
        for (int i = 0; i < vertices.Length; i++) {
            Vector2 p0 = vertices[i];
            Vector2 p3 = vertices[(i + 1) % vertexCount];

            // ������Ƶ㣨��ֱ�ڱ��е㣩
            Vector2 mid = (p0 + p3) * 0.5f;//�е�
            Vector2 dir = (p3 - p0).normalized;//����
            Vector2 normal = new Vector2(-dir.y, dir.x);
            Vector2 p1 = mid + normal * bezierStrength * noiseWidth;
            Vector2 p2 = mid - normal * bezierStrength * noiseWidth;


            // ���α��������߲���
            for (int t = 0; t <= bezierSegments; t++) {
                float u = t / (float)bezierSegments;
                Vector2 point = CalculateCubicBezier(p0, p1, p2, p3, u);
                path.Add(point);
            }
        }
        return path.ToArray();
    }
    // ���α��������߼���
    private Vector2 CalculateCubicBezier(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t) {
        float u = 1 - t;
        return
            u * u * u * p0 +
            3 * u * u * t * p1 +
            3 * u * t * t * p2 +
            t * t * t * p3;
    }
    // ��������
    private Vector2[] GeneratePerlinPath(Vector2[] vertices) {
        List<Vector2> path = new List<Vector2>();

        for (int i = 0; i < vertices.Length; i++) {
            Vector2 start = vertices[i];
            Vector2 end = vertices[(i + 1) % vertexCount];

            // ��ֱ��·��������
            int segments = Mathf.CeilToInt(Vector2.Distance(start, end) / perlinSegments);
            for (int t = 0; t <= segments; t++) {
                float lerp = t / (float)segments;
                Vector2 basePoint = Vector2.Lerp(start, end, lerp);

                // ===== ������������ =====
                float noiseX = 0f;
                float noiseY = 0f;
                float frequency = perlinFrequency;
                float amplitude = 1f;
                float maxAmplitude = 0f;
                //��������
                for (int oct = 0; oct < octaves; oct++) {
                    // ÿ��ʹ�ò�ͬ����ƫ��
                    float octaveSeed = seed + oct * 1000;

                    // X������
                    float nx = (basePoint.x + octaveSeed) * frequency;
                    float ny = (basePoint.y + octaveSeed) * frequency;
                    noiseX += Mathf.PerlinNoise(nx, ny) * amplitude;

                    // Y��������ʹ�ò�ͬ�������꣩
                    float nx2 = (basePoint.x + octaveSeed + 1000) * frequency;
                    float ny2 = (basePoint.y + octaveSeed + 1000) * frequency;
                    noiseY += Mathf.PerlinNoise(nx2, ny2) * amplitude;

                    maxAmplitude += amplitude;
                    amplitude *= persistence;
                    frequency *= lacunarity;
                }

                //// ��һ����[-1,1]��Χ����ֹ����ͼ�η���ƫ��
                noiseX = (noiseX / maxAmplitude) * 2 - 1;
                noiseY = (noiseY / maxAmplitude) * 2 - 1;

                // Ӧ��ƫ��
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

    //GPU���
    public void ApplyDistortion(Vector2[] paths) {
        // ������д����
        RenderTextureFormat format = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.R8) ? RenderTextureFormat.ARGB32 : RenderTextureFormat.ARGB32;
        RenderTexture resultTexture = new RenderTexture(noiseWidth, noiseHeight, 0, format) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Point };
        resultTexture.enableRandomWrite = true; // �ؼ�����
        resultTexture.Create();


        ComputeShader shader = Resources.Load<ComputeShader>("Shader/PolygonGenerator");
        int kernel = shader.FindKernel("RasterizePolygon");

        int pathPointsIndex = Shader.PropertyToID("PathPoints");
        ComputeBuffer pathPoints = new ComputeBuffer(paths.Length, sizeof(float) * 2);
        pathPoints.SetData(paths);


        // ����Shader����
        shader.SetTexture(kernel, "Result", resultTexture);
        shader.SetInt("VertexCount", pathPoints.count);
        shader.SetBuffer(kernel, pathPointsIndex, pathPoints);

        // �����߳�
        shader.Dispatch(kernel,
            Mathf.CeilToInt(noiseWidth / 8.0f),
            Mathf.CeilToInt(noiseHeight / 8.0f),
            1);
        _noiseTexture = ToTexture2D(resultTexture);
        pathPoints.Release();
        resultTexture.Release();
        
    }

    //��ת
    public Texture2D Rotate(Texture2D tex, float degress) {

        // ������д����
        RenderTextureFormat format = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.R8) ? RenderTextureFormat.ARGB32 : RenderTextureFormat.ARGB32;
        RenderTexture resultTexture = new RenderTexture(noiseWidth, noiseHeight, 0, format) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Point };
        resultTexture.enableRandomWrite = true; // �ؼ�����
        resultTexture.Create();

        ComputeShader shader = Resources.Load<ComputeShader>("Shader/PolygonGenerator");
        int kernel = shader.FindKernel("Rotate");

        shader.SetTexture(kernel, "SourceTexture", tex);
        shader.SetTexture(kernel, "Result", resultTexture);
        shader.SetVector("TextureSize", new Vector2(tex.width, tex.height));

        // ���ñ任����
        shader.SetVector("Offset", offset);
        shader.SetFloat("RotationAngle", degress * Mathf.Deg2Rad);
        shader.SetVector("Pivot", new Vector2(0.5f, 0.5f));

        // �����߳�
        shader.Dispatch(kernel,
            Mathf.CeilToInt(tex.width / 8.0f),
            Mathf.CeilToInt(tex.height / 8.0f),
            1);
  
        Texture2D tex_ =  ToTexture2D(resultTexture);
        
        return tex_;
    }
}