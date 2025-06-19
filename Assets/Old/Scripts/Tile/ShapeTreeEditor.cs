#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TreeClass))]
public class TreeTileDataEditor : Editor {

    private TreeClass treeClass;
    private bool[,] editMap;
    private bool isDragging = false;
    private bool dragState = false;
    //��������
    private float cellSize = 30f;//��Ԫ���С
    private const float MinCellSize = 20f;
    private const float MaxCellSize = 50f;
    private Vector2 scrollPosition;//������λ��
    private Rect gridArea;//�������
    private const float Padding = 2f;//��Ԫ��֮��ļ��

    //��ɫ
    private Color gridColor = Color.red;//������ɫ
    private Color clearColor = Color.green;//��������ɫ
    private Color originColor = new Color(0.2f, 0.2f, 1f, 0.8f);//ԭ����ɫ

    //��������
    private Texture2D spriteTexture;//�������
    private Vector2 spritePivot = Vector2.one * 0.5f;//����ԭ��
    private bool showPixelGrid = true;//�Ƿ���ʵ������ռ����
    private float pixelsPerUnit = 100f;
    private Vector2 spriteSizeInUnits;
    private bool useCustomPivot = false;//�Ƿ��Զ�������
    private Vector2 customPivot = Vector2.one * 0.5f;//�Զ�������

    private void OnEnable() {
        treeClass = (TreeClass)target;
        if (treeClass.clearMap == null || treeClass.clearMap.Length != treeClass.gridWidth * treeClass.gridHeight) {
            treeClass.InitializeGrid();
        }

        InitializeEditMap();
        LoadSpriteTexture();
        CalculateSpriteSize();
        LoadPivotData();
    }

    //��ʼ���༭����������
    private void InitializeEditMap() {
        editMap = new bool[treeClass.gridWidth, treeClass.gridHeight];
        for (int x = 0; x < treeClass.gridWidth; x++) {
            for (int y = 0; y < treeClass.gridHeight; y++) {
                int index = y * treeClass.gridWidth + x;
                editMap[x, y] = treeClass.clearMap[index];
            }
        }
    }

    //���ؾ������
    private void LoadSpriteTexture() {
        Sprite previewSprite = ((CustomTile)treeClass.tile)?.m_DefaultSprite;
        if (previewSprite != null) {
            spriteTexture = AssetPreview.GetAssetPreview(previewSprite);
            if (spriteTexture == null) {
                spriteTexture = AssetPreview.GetMiniThumbnail(previewSprite);
            }

            // ��ȡ�����Pixels Per Unit
            string assetPath = AssetDatabase.GetAssetPath(previewSprite);
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null) {
                pixelsPerUnit = importer.spritePixelsPerUnit;
            }
        } else {
            spriteTexture = null;
        }
    }

    //���㾫��ʵ����Tilemap����ռ��Ԫ���С
    private void CalculateSpriteSize() {
        Sprite previewSprite = ((CustomTile)treeClass.tile)?.m_DefaultSprite;
        if (previewSprite != null) {
            // ���㾫����Tilemap�е�ʵ�ʳߴ磨������ԪΪ��λ��
            float widthInUnits = previewSprite.rect.width / pixelsPerUnit;
            float heightInUnits = previewSprite.rect.height / pixelsPerUnit;
            spriteSizeInUnits = new Vector2(widthInUnits, heightInUnits);
        } else {
            spriteSizeInUnits = Vector2.one;
        }
    }

    //���ؾ�����������
    private void LoadPivotData() {
        Sprite previewSprite = ((CustomTile)treeClass.tile)?.m_DefaultSprite;
        if (previewSprite != null) {
            // ��ȡ�����ʵ�����ĵ�
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

        // ����Ĭ������
        DrawDefaultInspector();

        EditorGUILayout.Space(20);
        EditorGUILayout.LabelField("Tree Shape Editor", EditorStyles.boldLabel);

        // ����ߴ����
        EditorGUI.BeginChangeCheck();
        int newWidth = Mathf.Clamp(EditorGUILayout.IntField("Grid Width", treeClass.gridWidth), 3, 15);
        int newHeight = Mathf.Clamp(EditorGUILayout.IntField("Grid Height", treeClass.gridHeight), 3, 15);

        if (EditorGUI.EndChangeCheck()) {
            treeClass.gridWidth = newWidth;
            treeClass.gridHeight = newHeight;
            treeClass.InitializeGrid();
            InitializeEditMap();
        }

        // ԭ��λ�ÿ���
        Vector2Int newOrigin = EditorGUILayout.Vector2IntField("Origin Point", treeClass.originPoint);
        if (newOrigin.x < 0) newOrigin.x = 0;
        if (newOrigin.y < 0) newOrigin.y = 0;
        if (newOrigin.x >= treeClass.gridWidth) newOrigin.x = treeClass.gridWidth - 1;
        if (newOrigin.y >= treeClass.gridHeight) newOrigin.y = treeClass.gridHeight - 1;

        if (newOrigin != treeClass.originPoint) {
            treeClass.originPoint = newOrigin;
            EditorUtility.SetDirty(treeClass);
        }

        // ��ʾ����ߴ���Ϣ
        EditorGUILayout.LabelField($"Sprite Size in Units: {spriteSizeInUnits.x:F2} x {spriteSizeInUnits.y:F2}");
        EditorGUILayout.LabelField($"Pixels Per Unit: {pixelsPerUnit}");

        // ��ʾ������Ϣ
        EditorGUILayout.LabelField($"Sprite Pivot: ({spritePivot.x:F2}, {spritePivot.y:F2})");

        // �Զ�������ѡ��
        EditorGUI.BeginChangeCheck();
        useCustomPivot = EditorGUILayout.Toggle("Use Custom Pivot", useCustomPivot);
        if (useCustomPivot) {
            customPivot = EditorGUILayout.Vector2Field("Custom Pivot", customPivot);
        }

        // ��Ԫ��С����
        cellSize = EditorGUILayout.Slider("Cell Size", cellSize, MinCellSize, MaxCellSize);

        // ��ʾѡ��
        showPixelGrid = EditorGUILayout.Toggle("Show Pixel Grid", showPixelGrid);

        EditorGUILayout.Space(10);

        // ��������༭��
        DrawGridEditor();

        EditorGUILayout.Space(15);

        // ���߰�ť
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Clear All")) ClearAllCells();
        if (GUILayout.Button("Fill All")) FillAllCells();
        if (GUILayout.Button("Invert")) InvertCells();
        if (GUILayout.Button("Save Changes")) SaveChanges();
        if (GUILayout.Button("Reset Pivot")) ResetPivot();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.HelpBox("Click to toggle cells. Drag to paint multiple cells.", MessageType.Info);

        serializedObject.ApplyModifiedProperties();

        // ������鷢���仯�����¼�������
        Sprite previewSprite = ((CustomTile)treeClass.tile)?.m_DefaultSprite;
        if (GUI.changed && previewSprite != null) {
            LoadSpriteTexture();
            CalculateSpriteSize();
            LoadPivotData();
        }
    }

    private void DrawGridEditor() {
        // �������������С
        float gridWidth = treeClass.gridWidth * (cellSize + Padding);
        float gridHeight = treeClass.gridHeight * (cellSize + Padding);

        // ��ʼ������ͼ
        scrollPosition = EditorGUILayout.BeginScrollView(
            scrollPosition,
            GUILayout.Height(Mathf.Min(gridHeight + 50, 500))
        );

        // ��ʼ��������
        gridArea = GUILayoutUtility.GetRect(gridWidth, gridHeight);

        // ���Ʊ���
        GUI.Box(gridArea, "", GUI.skin.box);

        // ����Sprite
        if (spriteTexture != null) {
            // ���㾫����ʾ�ߴ磨������ԪΪ��λ��
            float spriteDisplayWidth = spriteSizeInUnits.x * cellSize;
            float spriteDisplayHeight = spriteSizeInUnits.y * cellSize;


            // ���㾫��λ�ã�ԭ��Ϊ���ĵ㣩
            Vector2 effectivePivot = useCustomPivot ? customPivot : spritePivot;

            // ��������ƫ�ƣ������ĵ�ƫ�ƣ�
            Vector2 pivotOffset = new Vector2(
                0.5f - effectivePivot.x,
                0.5f - effectivePivot.y
            );

            // ���㾫��λ�ã�����ԭ���Ӧ�������ĵ㣩
            float spriteX = gridArea.x + treeClass.originPoint.x * (cellSize + Padding) - spriteDisplayWidth * effectivePivot.x + cellSize / 2;
            float spriteY = gridArea.y + (treeClass.gridHeight - 1 - treeClass.originPoint.y) * (cellSize + Padding) + spriteDisplayHeight * (effectivePivot.y - 1) + cellSize / 2;
            Rect spriteRect = new Rect(spriteX, spriteY, spriteDisplayWidth, spriteDisplayHeight);

            // ���ƾ���
            GUI.DrawTexture(spriteRect, spriteTexture, ScaleMode.ScaleToFit);

            // ���ƾ���߽�
            Handles.BeginGUI();
            Handles.color = new Color(0, 1, 1, 0.5f);
            Handles.DrawSolidRectangleWithOutline(spriteRect, Color.clear, Color.cyan);

            // �������ĵ���
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

            // �����������������Ҫ��
            if (showPixelGrid && cellSize > 20f) {
                DrawPixelGrid(spriteRect);
            }
        }

        // ��������Ԫ
        
        for (int y = 0; y < treeClass.gridHeight; y++) {
            for (int x = 0; x < treeClass.gridWidth; x++) {
                Rect cellRect = new Rect(
                    gridArea.x + x * (cellSize + Padding),
                    gridArea.y + (treeClass.gridHeight - 1 - y) * (cellSize + Padding),
                    cellSize,
                    cellSize
                );

                // ���Ƶ�Ԫ��
                DrawCell(cellRect, x, y);
            }
        }

        // ����ԭ����
        Rect originRect = new Rect(
           gridArea.x + treeClass.originPoint.x * (cellSize + Padding),
           gridArea.y + (treeClass.gridHeight - 1 - treeClass.originPoint.y) * (cellSize + Padding),
           cellSize,
           cellSize
       );
        GUI.Box(originRect, "", new GUIStyle(GUI.skin.box) { normal = { background = MakeTex(2, 2, originColor) } });

        // ����������ͼ
        EditorGUILayout.EndScrollView();
    }

    private void DrawPixelGrid(Rect spriteRect) {
        Sprite previewSprite = ((CustomTile)treeClass.tile)?.m_DefaultSprite;
        if (previewSprite == null) return;

        Handles.BeginGUI();
        Handles.color = new Color(1, 1, 1, 0.1f);

        // ����ÿ�����صĴ�С
        float pixelWidth = spriteRect.width / previewSprite.rect.width;
        float pixelHeight = spriteRect.height / previewSprite.rect.height;

        // ����ˮƽ��
        for (int y = 0; y <= previewSprite.rect.height; y++) {
            float yPos = spriteRect.y + y * pixelHeight;
            Handles.DrawLine(
                new Vector3(spriteRect.x, yPos, 0),
                new Vector3(spriteRect.x + spriteRect.width, yPos, 0)
            );
        }

        // ���ƴ�ֱ��
        for (int x = 0; x <= previewSprite.rect.width; x++) {
            float xPos = spriteRect.x + x * pixelWidth;
            Handles.DrawLine(
                new Vector3(xPos, spriteRect.y, 0),
                new Vector3(xPos, spriteRect.y + spriteRect.height, 0)
            );
        }

        Handles.EndGUI();
    }

    private void DrawCell(Rect cellRect, int x, int y) {
        // ���Ƶ�Ԫ�񱳾�
        if (editMap[x, y]) {
            // ������� - ��͸����ɫ
            GUI.color = clearColor;
            GUI.Box(cellRect, "", new GUIStyle(GUI.skin.box) { normal = { background = MakeTex(2, 2, clearColor) } });
        } else {
            // ��������� - ������
            DrawGridCell(cellRect);
        }

        // �ָ���ɫ
        GUI.color = Color.white;

        // ��������¼�
        Event evt = Event.current;
        if (cellRect.Contains(evt.mousePosition)) {
            // ��갴��
            if (evt.type == EventType.MouseDown) {
                isDragging = true;
                dragState = !editMap[x, y];
                editMap[x, y] = dragState;
                evt.Use();
                GUI.changed = true;
            }
            // ����϶�
            else if (evt.type == EventType.MouseDrag && isDragging) {
                editMap[x, y] = dragState;
                evt.Use();
                GUI.changed = true;
            }
            // ����ͷ�
            else if (evt.type == EventType.MouseUp) {
                isDragging = false;
                evt.Use();
            }
        }
    }

    private void DrawGridCell(Rect rect) {
        // ���Ƶ�Ԫ��߿�
        Handles.BeginGUI();
        Handles.color = gridColor;

        // ����������
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

    private void ClearAllCells() {
        for (int x = 0; x < treeClass.gridWidth; x++) {
            for (int y = 0; y < treeClass.gridHeight; y++) {
                editMap[x, y] = false;
            }
        }
        SaveChanges();
    }

    private void FillAllCells() {
        for (int x = 0; x < treeClass.gridWidth; x++) {
            for (int y = 0; y < treeClass.gridHeight; y++) {
                editMap[x, y] = true;
            }
        }
        SaveChanges();
    }

    private void InvertCells() {
        for (int x = 0; x < treeClass.gridWidth; x++) {
            for (int y = 0; y < treeClass.gridHeight; y++) {
                editMap[x, y] = !editMap[x, y];
            }
        }
        SaveChanges();
    }

    private void ResetPivot() {
        useCustomPivot = false;
        customPivot = Vector2.one * 0.5f;
        GUI.changed = true;
    }

    private new void SaveChanges() {
        for (int x = 0; x < treeClass.gridWidth; x++) {
            for (int y = 0; y < treeClass.gridHeight; y++) {
                int index = y * treeClass.gridWidth + x;
                treeClass.clearMap[index] = editMap[x, y];
            }
        }
        EditorUtility.SetDirty(treeClass);
    }

    // ������ɫ����
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