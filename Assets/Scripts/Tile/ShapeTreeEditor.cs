#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TreeClass))]
public class TreeTileDataEditor : Editor {

    private TreeClass treeClass;
    private bool[,] editMap;
    private bool isDragging = false;
    private bool dragState = false;
    //网格属性
    private float cellSize = 30f;//单元格大小
    private const float MinCellSize = 20f;
    private const float MaxCellSize = 50f;
    private Vector2 scrollPosition;//滚动条位置
    private Rect gridArea;//网格矩阵
    private const float Padding = 2f;//单元格之间的间距

    //颜色
    private Color gridColor = Color.red;//网格颜色
    private Color clearColor = Color.green;//清理区颜色
    private Color originColor = new Color(0.2f, 0.2f, 1f, 0.8f);//原点颜色

    //精灵属性
    private Texture2D spriteTexture;//精灵材质
    private Vector2 spritePivot = Vector2.one * 0.5f;//精灵原点
    private bool showPixelGrid = true;//是否现实精灵所占网格
    private float pixelsPerUnit = 100f;
    private Vector2 spriteSizeInUnits;
    //轴心自定义
    private bool useCustomPivot = false;//是否自定义轴心
    private Vector2 customPivot = Vector2.one * 0.5f;//自定义轴心

    private void OnEnable() {
        treeClass = (TreeClass)target;
        if (treeClass.footprint.clearMap == null || treeClass.footprint.clearMap.Length != treeClass.footprint.gridWidth * treeClass.footprint.gridHeight) {
            treeClass.footprint.Initialize();
        }

        InitializeEditMap();
        LoadSpriteTexture();
        CalculateSpriteSize();
        LoadPivotData();
    }

    //初始化编辑器网格数据
    private void InitializeEditMap() {
        int gridWidth = treeClass.footprint.gridWidth;
        int gridHeight = treeClass.footprint.gridHeight;
        editMap = new bool[gridWidth, gridHeight];
        for (int x = 0; x < gridWidth; x++) {
            for (int y = 0; y < gridHeight; y++) {
                int index = y * gridWidth + x;
                editMap[x, y] = treeClass.footprint.clearMap[index];
            }
        }
    }

    //加载精灵材质
    private void LoadSpriteTexture() {
        Sprite previewSprite = ((CustomTile)treeClass.tile)?.m_DefaultSprite;
        if (previewSprite != null) {
            spriteTexture = AssetPreview.GetAssetPreview(previewSprite);
            if (spriteTexture == null) {
                spriteTexture = AssetPreview.GetMiniThumbnail(previewSprite);
            }

            // 获取精灵的Pixels Per Unit
            string assetPath = AssetDatabase.GetAssetPath(previewSprite);
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null) {
                pixelsPerUnit = importer.spritePixelsPerUnit;
            }
        } else {
            spriteTexture = null;
        }
    }

    //计算精灵实际在Tilemap中所占单元格大小
    private void CalculateSpriteSize() {
        Sprite previewSprite = ((CustomTile)treeClass.tile)?.m_DefaultSprite;
        if (previewSprite != null) {
            // 计算精灵在Tilemap中的实际尺寸（以网格单元为单位）
            float widthInUnits = previewSprite.rect.width / pixelsPerUnit;
            float heightInUnits = previewSprite.rect.height / pixelsPerUnit;
            spriteSizeInUnits = new Vector2(widthInUnits, heightInUnits);
        } else {
            spriteSizeInUnits = Vector2.one;
        }
    }

    //加载精灵轴心坐标
    private void LoadPivotData() {
        Sprite previewSprite = ((CustomTile)treeClass.tile)?.m_DefaultSprite;
        if (previewSprite != null) {
            // 获取精灵的实际轴心点
            Rect rect = previewSprite.rect;
            spritePivot = previewSprite.pivot;
            spritePivot.x /= rect.width;
            spritePivot.y /= rect.height;;
        } else {
            spritePivot = Vector2.one * 0.5f;
        }
    }

    public override void OnInspectorGUI() {
        serializedObject.Update();

        // 绘制默认属性
        DrawDefaultInspector();

        EditorGUILayout.Space(20);
        EditorGUILayout.LabelField("Tree Shape Editor", EditorStyles.boldLabel);

        // 网格尺寸控制
        EditorGUI.BeginChangeCheck();
        int newWidth = Mathf.Clamp(EditorGUILayout.IntField("Grid Width", treeClass.footprint.gridWidth), 3, 15);
        int newHeight = Mathf.Clamp(EditorGUILayout.IntField("Grid Height", treeClass.footprint.gridHeight), 3, 15);

        if (EditorGUI.EndChangeCheck()) {
            treeClass.footprint.gridWidth = newWidth;
            treeClass.footprint.gridHeight = newHeight;
            treeClass.footprint.Initialize();
            InitializeEditMap();
        }

        // 原点位置控制
        Vector2Int newOrigin = EditorGUILayout.Vector2IntField("Origin Point", treeClass.footprint.originPoint);
        if (newOrigin.x < 0) newOrigin.x = 0;
        if (newOrigin.y < 0) newOrigin.y = 0;
        if (newOrigin.x >= treeClass.footprint.gridWidth) newOrigin.x = treeClass.footprint.gridWidth - 1;
        if (newOrigin.y >= treeClass.footprint.gridHeight) newOrigin.y = treeClass.footprint.gridHeight - 1;

        if (newOrigin != treeClass.footprint.originPoint) {
            treeClass.footprint.originPoint = newOrigin;
            EditorUtility.SetDirty(treeClass);
        }

        // 显示精灵尺寸信息
        EditorGUILayout.LabelField($"Sprite Size in Units: {spriteSizeInUnits.x:F2} x {spriteSizeInUnits.y:F2}");
        EditorGUILayout.LabelField($"Pixels Per Unit: {pixelsPerUnit}");

        // 显示轴心信息
        EditorGUILayout.LabelField($"Sprite Pivot: ({spritePivot.x:F2}, {spritePivot.y:F2})");

        // 自定义轴心选项
        EditorGUI.BeginChangeCheck();
        useCustomPivot = EditorGUILayout.Toggle("Use Custom Pivot", useCustomPivot);
        if (useCustomPivot) {
            customPivot = EditorGUILayout.Vector2Field("Custom Pivot", customPivot);
        }

        // 单元大小控制
        cellSize = EditorGUILayout.Slider("Cell Size", cellSize, MinCellSize, MaxCellSize);

        // 显示选项
        showPixelGrid = EditorGUILayout.Toggle("Show Pixel Grid", showPixelGrid);

        EditorGUILayout.Space(10);

        // 绘制网格编辑器
        DrawGridEditor();

        EditorGUILayout.Space(15);

        // 工具按钮
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("清空")) ClearAllCells();
        if (GUILayout.Button("反选")) InvertCells();
        if (GUILayout.Button("保存更改")) SaveChanges();
        if (GUILayout.Button("重置轴心")) ResetPivot();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.HelpBox("Click to toggle cells. Drag to paint multiple cells.", MessageType.Info);

        serializedObject.ApplyModifiedProperties();

        // 如果精灵发生变化，重新加载纹理
        Sprite previewSprite = ((CustomTile)treeClass.tile)?.m_DefaultSprite;
        if (GUI.changed && previewSprite != null) {
            LoadSpriteTexture();
            CalculateSpriteSize();
            LoadPivotData();
        }
    }

    private void DrawGridEditor() {
        // 计算网格区域大小
        float gridWidth = treeClass.footprint.gridWidth * (cellSize + Padding);
        float gridHeight = treeClass.footprint.gridHeight * (cellSize + Padding);

        // 开始滚动视图
        scrollPosition = EditorGUILayout.BeginScrollView(
            scrollPosition,
            GUILayout.Height(Mathf.Min(gridHeight + 50, 500))
        );

        // 开始网格区域
        gridArea = GUILayoutUtility.GetRect(gridWidth, gridHeight);

        // 绘制背景
        GUI.Box(gridArea, "", GUI.skin.box);

        // 绘制Sprite
        if (spriteTexture != null) {
            // 计算精灵显示尺寸（以网格单元为单位）
            float spriteDisplayWidth = spriteSizeInUnits.x * cellSize;
            float spriteDisplayHeight = spriteSizeInUnits.y * cellSize;


            // 计算精灵位置（原点为轴心点）
            Vector2 effectivePivot = useCustomPivot ? customPivot : spritePivot;

            // 计算轴心偏移（从中心点偏移）
            Vector2 pivotOffset = new Vector2(
                0.5f - effectivePivot.x,
                0.5f - effectivePivot.y
            );

            // 计算精灵位置（网格原点对应精灵轴心点）
            float spriteX = gridArea.x + treeClass.footprint.originPoint.x * (cellSize + Padding) - spriteDisplayWidth * effectivePivot.x + cellSize / 2;
            float spriteY = gridArea.y + (treeClass.footprint.gridHeight - 1 - treeClass.footprint.originPoint.y) * (cellSize + Padding) + spriteDisplayHeight * (effectivePivot.y - 1) + cellSize / 2;
            Rect spriteRect = new Rect(spriteX, spriteY, spriteDisplayWidth, spriteDisplayHeight);

            // 绘制精灵
            GUI.DrawTexture(spriteRect, spriteTexture, ScaleMode.ScaleToFit);

            // 绘制精灵边界
            Handles.BeginGUI();
            Handles.color = new Color(0, 1, 1, 0.5f);
            Handles.DrawSolidRectangleWithOutline(spriteRect, Color.clear, Color.cyan);

            // 绘制轴心点标记
            Vector2 pivotPoint = new Vector2(
                spriteRect.x + spriteRect.width * effectivePivot.x,
                spriteRect.y - spriteRect.height * (effectivePivot.y - 1)
            );

            float pivotMarkerSize = 8f;
            Handles.color = Color.yellow;
            Handles.DrawLine(
                new Vector3(pivotPoint.x - pivotMarkerSize, pivotPoint.y, 0),
                new Vector3(pivotPoint.x + pivotMarkerSize, pivotPoint.y, 0)
            );
            Handles.DrawLine(
                new Vector3(pivotPoint.x, pivotPoint.y - pivotMarkerSize, 0),
                new Vector3(pivotPoint.x, pivotPoint.y + pivotMarkerSize, 0)
            );

            Handles.EndGUI();

            // 绘制像素网格（如果需要）
            if (showPixelGrid && cellSize > 20f) {
                DrawPixelGrid(spriteRect);
            }
        }

        // 绘制网格单元
        
        for (int y = 0; y < treeClass.footprint.gridHeight; y++) {
            for (int x = 0; x < treeClass.footprint.gridWidth; x++) {
                Rect cellRect = new Rect(
                    gridArea.x + x * (cellSize + Padding),
                    gridArea.y + (treeClass.footprint.gridHeight - 1 - y) * (cellSize + Padding),
                    cellSize,
                    cellSize
                );

                // 绘制单元格
                DrawCell(cellRect, x, y);
            }
        }

        // 绘制原点标记
        Rect originRect = new Rect(
           gridArea.x + treeClass.footprint.originPoint.x * (cellSize + Padding),
           gridArea.y + (treeClass.footprint.gridHeight - 1 - treeClass.footprint.originPoint.y) * (cellSize + Padding),
           cellSize,
           cellSize
       );
        GUI.Box(originRect, "", new GUIStyle(GUI.skin.box) { normal = { background = MakeTex(2, 2, originColor) } });

        // 结束滚动视图
        EditorGUILayout.EndScrollView();
    }

    // 绘制sprite背景网格
    private void DrawPixelGrid(Rect spriteRect) {
        Sprite previewSprite = ((CustomTile)treeClass.tile)?.m_DefaultSprite;
        if (previewSprite == null) return;

        Handles.BeginGUI();
        Handles.color = new Color(1, 1, 1, 0.1f);

        // 计算每个像素的大小
        float pixelWidth = spriteRect.width / previewSprite.rect.width;
        float pixelHeight = spriteRect.height / previewSprite.rect.height;

        // 绘制水平线
        for (int y = 0; y <= previewSprite.rect.height; y++) {
            float yPos = spriteRect.y + y * pixelHeight;
            Handles.DrawLine(
                new Vector3(spriteRect.x, yPos, 0),
                new Vector3(spriteRect.x + spriteRect.width, yPos, 0)
            );
        }

        // 绘制垂直线
        for (int x = 0; x <= previewSprite.rect.width; x++) {
            float xPos = spriteRect.x + x * pixelWidth;
            Handles.DrawLine(
                new Vector3(xPos, spriteRect.y, 0),
                new Vector3(xPos, spriteRect.y + spriteRect.height, 0)
            );
        }

        Handles.EndGUI();
    }

    // 绘制单元格
    private void DrawCell(Rect cellRect, int x, int y) {
        // 绘制单元格背景
        if (editMap[x, y]) {
            // 清除区域 - 半透明红色
            GUI.color = clearColor;
            GUI.Box(cellRect, "", new GUIStyle(GUI.skin.box) { normal = { background = MakeTex(2, 2, clearColor) } });
        } else {
            // 非清除区域 - 网格线
            DrawGridCell(cellRect);
        }

        // 恢复颜色
        GUI.color = Color.white;

        // 处理鼠标事件
        Event evt = Event.current;
        if (cellRect.Contains(evt.mousePosition)) {
            // 鼠标按下
            if (evt.type == EventType.MouseDown) {
                isDragging = true;
                dragState = !editMap[x, y];
                editMap[x, y] = dragState;
                evt.Use();
                GUI.changed = true;
            }
            // 鼠标拖动
            else if (evt.type == EventType.MouseDrag && isDragging) {
                editMap[x, y] = dragState;
                evt.Use();
                GUI.changed = true;
            }
            // 鼠标释放
            else if (evt.type == EventType.MouseUp) {
                isDragging = false;
                evt.Use();
            }
        }
    }

    // 绘制网格
    private void DrawGridCell(Rect rect) {
        // 绘制单元格边框
        Handles.BeginGUI();
        Handles.color = gridColor;

        // 绘制网格线
        Handles.DrawLine(
            new Vector3(rect.x, rect.y, 0),
            new Vector3(rect.x + rect.width, rect.y, 0)
        );

        Handles.DrawLine(
            new Vector3(rect.x, rect.y, 0),
            new Vector3(rect.x, rect.y + rect.height, 0)
        );

        Handles.DrawLine(
            new Vector3(rect.x + rect.width, rect.y, 0),
            new Vector3(rect.x + rect.width, rect.y + rect.height, 0)
        );

        Handles.DrawLine(
            new Vector3(rect.x, rect.y + rect.height, 0),
            new Vector3(rect.x + rect.width, rect.y + rect.height, 0)
        );

        Handles.EndGUI();
    }

    // 清理所有
    private void ClearAllCells() {
        for (int x = 0; x < treeClass.footprint.gridWidth; x++) {
            for (int y = 0; y < treeClass.footprint.gridHeight; y++) {
                editMap[x, y] = false;
            }
        }
        SaveChanges();
    }

    // 反选
    private void InvertCells() {
        for (int x = 0; x < treeClass.footprint.gridWidth; x++) {
            for (int y = 0; y < treeClass.footprint.gridHeight; y++) {
                editMap[x, y] = !editMap[x, y];
            }
        }
        SaveChanges();
    }

    // 重置
    private void ResetPivot() {
        useCustomPivot = false;
        customPivot = Vector2.one * 0.5f;
        GUI.changed = true;
    }


    // 保存改动
    private new void SaveChanges() {
        for (int x = 0; x < treeClass.footprint.gridWidth; x++) {
            for (int y = 0; y < treeClass.footprint.gridHeight; y++) {
                int index = y * treeClass.footprint.gridWidth + x;
                treeClass.footprint.clearMap[index] = editMap[x, y];
            }
        }
        EditorUtility.SetDirty(treeClass);
    }

    // 创建纯色纹理
    private Texture2D MakeTex(int width, int height, Color col) {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; i++) {
            pix[i] = col;
        }
        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }
}
#endif