using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ItemGrid))]
public class ItemGridEditor : Editor
{
    private ItemGrid itemGrid;
    private bool showGridVisualization = true;
    private bool showCoordinates = true;
    private Color gridLineColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);
    private Color gridBackgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.1f);

    private void OnEnable()
    {
        itemGrid = (ItemGrid)target;
        // 确保Scene视图在编辑器启用时刷新
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    public override void OnInspectorGUI()
    {
        // 检测参数变化
        EditorGUI.BeginChangeCheck();

        // 绘制默认的Inspector
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("编辑器可视化设置", EditorStyles.boldLabel);

        // 网格可视化开关
        showGridVisualization = EditorGUILayout.Toggle("显示网格线", showGridVisualization);

        if (showGridVisualization)
        {
            EditorGUI.indentLevel++;
            gridLineColor = EditorGUILayout.ColorField("网格线颜色", gridLineColor);
            gridBackgroundColor = EditorGUILayout.ColorField("网格背景颜色", gridBackgroundColor);
            showCoordinates = EditorGUILayout.Toggle("显示坐标标签", showCoordinates);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space();

        // 显示网格信息
        EditorGUILayout.LabelField("网格信息", EditorStyles.boldLabel);
        Vector2Int gridSize = itemGrid.GetGridSize();
        float cellSize = itemGrid.GetCellSize();

        EditorGUILayout.LabelField($"网格尺寸: {gridSize.x} x {gridSize.y}");
        EditorGUILayout.LabelField($"总格子数: {gridSize.x * gridSize.y}");
        EditorGUILayout.LabelField($"单元格大小: {cellSize}");
        EditorGUILayout.LabelField($"UI尺寸: {gridSize.x * cellSize} x {gridSize.y * cellSize}");

        // 显示GridConfig信息
        GridConfig config = itemGrid.GetGridConfig();
        if (config != null)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("GridConfig 信息", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"配置文件: {config.name}");
            EditorGUILayout.LabelField($"单元格大小: {config.cellSize}");
            EditorGUILayout.LabelField($"背包宽度: {config.inventoryWidth}");
            EditorGUILayout.LabelField($"背包高度: {config.inventoryHeight}");
        }
        else
        {
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("未分配 GridConfig，请在 Inspector 中分配一个 GridConfig 资源。", MessageType.Warning);
        }

        // 如果参数发生变化，重新绘制Scene视图
        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(itemGrid);
            // 延迟刷新Scene视图，确保参数更新完成后再刷新
            EditorApplication.delayCall += () =>
            {
                if (itemGrid != null)
                {
                    SceneView.RepaintAll();
                }
            };
        }
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (!showGridVisualization || itemGrid == null) return;

        RectTransform rectTransform = itemGrid.GetComponent<RectTransform>();
        if (rectTransform == null) return;

        // 获取网格的世界坐标
        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);

        Vector3 bottomLeft = corners[0];
        Vector3 topRight = corners[2];
        Vector3 topLeft = corners[1];
        Vector3 bottomRight = corners[3];

        Vector2Int gridSize = itemGrid.GetGridSize();
        float cellSize = itemGrid.GetCellSize();

        // 计算实际的网格尺寸（考虑Canvas的缩放）
        Canvas canvas = itemGrid.GetComponentInParent<Canvas>();
        float scaleFactor = canvas != null ? canvas.scaleFactor : 1f;
        float actualCellSize = cellSize * scaleFactor;

        // 绘制网格背景
        Handles.color = gridBackgroundColor;
        Vector3[] backgroundVerts = new Vector3[4]
        {
            bottomLeft,
            bottomRight,
            topRight,
            topLeft
        };
        Handles.DrawSolidRectangleWithOutline(backgroundVerts, gridBackgroundColor, Color.clear);

        // 绘制网格线
        Handles.color = gridLineColor;

        // 绘制垂直线
        for (int x = 0; x <= gridSize.x; x++)
        {
            float xPos = bottomLeft.x + (x * actualCellSize);
            Vector3 start = new Vector3(xPos, bottomLeft.y, bottomLeft.z);
            Vector3 end = new Vector3(xPos, topRight.y, bottomLeft.z);
            Handles.DrawLine(start, end);
        }

        // 绘制水平线
        for (int y = 0; y <= gridSize.y; y++)
        {
            float yPos = bottomLeft.y + (y * actualCellSize);
            Vector3 start = new Vector3(bottomLeft.x, yPos, bottomLeft.z);
            Vector3 end = new Vector3(topRight.x, yPos, bottomLeft.z);
            Handles.DrawLine(start, end);
        }

        // 绘制网格坐标标签（修正坐标显示逻辑）
        if (showCoordinates && gridSize.x <= 20 && gridSize.y <= 20)
        {
            Handles.color = Color.white;
            GUIStyle labelStyle = new GUIStyle();
            labelStyle.normal.textColor = Color.white;
            labelStyle.fontSize = 10;
            labelStyle.alignment = TextAnchor.MiddleCenter;

            for (int x = 0; x < gridSize.x; x++)
            {
                for (int y = 0; y < gridSize.y; y++)
                {
                    // 修正坐标计算：左上角为(0,0)
                    int displayY = gridSize.y - 1 - y;

                    Vector3 cellCenter = new Vector3(
                        bottomLeft.x + (x + 0.5f) * actualCellSize,
                        bottomLeft.y + (y + 0.5f) * actualCellSize,
                        bottomLeft.z
                    );

                    Handles.Label(cellCenter, $"{x},{displayY}", labelStyle);
                }
            }
        }
    }
}
