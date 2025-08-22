using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using InventorySystem.Grid;

/// <summary>
/// 网格可视化窗口 - 提供网格的可视化显示和交互功能
/// </summary>
public class GridVisualizationWindow : EditorWindow
{
    // 当前选中的网格
    private BaseItemGrid selectedGrid;

    // 可视化设置
    private bool showGridLines = true;
    private bool showOccupiedCells = true;
    private bool showItemIcons = false;
    private bool showHeatmap = false;

    // 颜色设置
    private Color gridLineColor = Color.gray;
    private Color occupiedCellColor = new Color(1f, 0f, 0f, 0.3f);
    private Color emptyCellColor = new Color(0f, 1f, 0f, 0.1f);
    private Color heatmapHighColor = Color.red;
    private Color heatmapLowColor = Color.blue;

    // 缩放和平移
    private float zoomLevel = 1.0f;
    private Vector2 panOffset = Vector2.zero;
    private Vector2 lastMousePosition;
    private bool isPanning = false;

    // 网格数据缓存
    private GridDetectorInfo cachedGridInfo;
    private double lastUpdateTime;
    private float updateInterval = 0.5f;

    /// <summary>
    /// 打开网格可视化窗口
    /// </summary>
    [MenuItem("Tools/Inventory System/Grid Visualization")]
    public static void ShowWindow()
    {
        GridVisualizationWindow window = GetWindow<GridVisualizationWindow>("网格可视化");
        window.minSize = new Vector2(400, 300);
        window.Show();
    }

    /// <summary>
    /// 设置要显示的网格
    /// </summary>
    public void SetSelectedGrid(BaseItemGrid grid)
    {
        selectedGrid = grid;
        RefreshGridData();
        Repaint();
    }

    /// <summary>
    /// 刷新网格数据
    /// </summary>
    private void RefreshGridData()
    {
        if (selectedGrid != null)
        {
            try
            {
                cachedGridInfo = selectedGrid.GetGridDetectorInfo();
                lastUpdateTime = EditorApplication.timeSinceStartup;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"刷新网格数据时出错: {e.Message}");
                cachedGridInfo = null;
            }
        }
    }

    /// <summary>
    /// 绘制GUI
    /// </summary>
    private void OnGUI()
    {
        // 检查是否需要更新数据
        if (selectedGrid != null && EditorApplication.timeSinceStartup - lastUpdateTime > updateInterval)
        {
            RefreshGridData();
        }

        DrawToolbar();
        DrawVisualizationSettings();

        if (selectedGrid == null)
        {
            DrawGridSelection();
        }
        else
        {
            DrawGridVisualization();
        }

        HandleInput();
    }

    /// <summary>
    /// 绘制工具栏
    /// </summary>
    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        if (selectedGrid != null)
        {
            EditorGUILayout.LabelField($"当前网格: {selectedGrid.name}", EditorStyles.toolbarButton);

            if (GUILayout.Button("刷新", EditorStyles.toolbarButton, GUILayout.Width(50)))
            {
                RefreshGridData();
            }

            if (GUILayout.Button("重置视图", EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                ResetView();
            }

            if (GUILayout.Button("清除选择", EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                selectedGrid = null;
                cachedGridInfo = null;
            }
        }
        else
        {
            EditorGUILayout.LabelField("未选择网格", EditorStyles.toolbarButton);
        }

        GUILayout.FlexibleSpace();

        if (selectedGrid != null)
        {
            EditorGUILayout.LabelField($"缩放: {zoomLevel:F1}x", EditorStyles.toolbarButton);
        }

        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// 绘制可视化设置
    /// </summary>
    private void DrawVisualizationSettings()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("可视化设置", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        showGridLines = EditorGUILayout.ToggleLeft("网格线", showGridLines, GUILayout.Width(80));
        showOccupiedCells = EditorGUILayout.ToggleLeft("占用格", showOccupiedCells, GUILayout.Width(80));
        showItemIcons = EditorGUILayout.ToggleLeft("物品图标", showItemIcons, GUILayout.Width(80));
        showHeatmap = EditorGUILayout.ToggleLeft("热力图", showHeatmap, GUILayout.Width(80));
        EditorGUILayout.EndHorizontal();

        // 颜色设置
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("颜色:", GUILayout.Width(40));
        gridLineColor = EditorGUILayout.ColorField("网格", gridLineColor, GUILayout.Width(120));
        occupiedCellColor = EditorGUILayout.ColorField("占用", occupiedCellColor, GUILayout.Width(120));
        emptyCellColor = EditorGUILayout.ColorField("空闲", emptyCellColor, GUILayout.Width(120));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 绘制网格选择界面
    /// </summary>
    private void DrawGridSelection()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("选择要可视化的网格", EditorStyles.boldLabel);

        BaseItemGrid[] allGrids = FindObjectsOfType<BaseItemGrid>();

        if (allGrids.Length == 0)
        {
            EditorGUILayout.HelpBox("场景中未找到任何网格系统", MessageType.Info);
        }
        else
        {
            foreach (BaseItemGrid grid in allGrids)
            {
                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.LabelField($"{grid.name} ({grid.GetType().Name})", GUILayout.ExpandWidth(true));

                if (GUILayout.Button("选择", GUILayout.Width(60)))
                {
                    SetSelectedGrid(grid);
                }

                if (GUILayout.Button("定位", GUILayout.Width(60)))
                {
                    Selection.activeGameObject = grid.gameObject;
                    SceneView.FrameLastActiveSceneView();
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 绘制网格可视化
    /// </summary>
    private void DrawGridVisualization()
    {
        if (cachedGridInfo == null)
        {
            EditorGUILayout.HelpBox("无法获取网格信息", MessageType.Warning);
            return;
        }

        Rect visualizationRect = GUILayoutUtility.GetRect(position.width - 20, position.height - 150);

        // 绘制背景
        EditorGUI.DrawRect(visualizationRect, new Color(0.2f, 0.2f, 0.2f));

        // 计算网格绘制参数
        Vector2 gridSize = new Vector2(cachedGridInfo.gridSize.x, cachedGridInfo.gridSize.y);
        float cellSize = Mathf.Min(
            (visualizationRect.width - 40) / gridSize.x,
            (visualizationRect.height - 40) / gridSize.y
        ) * zoomLevel;

        Vector2 gridStartPos = new Vector2(
            visualizationRect.x + (visualizationRect.width - gridSize.x * cellSize) * 0.5f + panOffset.x,
            visualizationRect.y + (visualizationRect.height - gridSize.y * cellSize) * 0.5f + panOffset.y
        );

        // 绘制网格
        DrawGrid(gridStartPos, gridSize, cellSize);

        // 绘制网格信息
        DrawGridInfo(visualizationRect);
    }

    /// <summary>
    /// 绘制网格
    /// </summary>
    private void DrawGrid(Vector2 startPos, Vector2 gridSize, float cellSize)
    {
        // 绘制网格线
        if (showGridLines)
        {
            Handles.color = gridLineColor;

            // 垂直线
            for (int x = 0; x <= gridSize.x; x++)
            {
                Vector2 start = new Vector2(startPos.x + x * cellSize, startPos.y);
                Vector2 end = new Vector2(startPos.x + x * cellSize, startPos.y + gridSize.y * cellSize);
                Handles.DrawLine(start, end);
            }

            // 水平线
            for (int y = 0; y <= gridSize.y; y++)
            {
                Vector2 start = new Vector2(startPos.x, startPos.y + y * cellSize);
                Vector2 end = new Vector2(startPos.x + gridSize.x * cellSize, startPos.y + y * cellSize);
                Handles.DrawLine(start, end);
            }
        }

        // 绘制单元格状态
        if (showOccupiedCells || showHeatmap)
        {
            DrawCellStates(startPos, gridSize, cellSize);
        }
    }

    /// <summary>
    /// 绘制单元格状态
    /// </summary>
    private void DrawCellStates(Vector2 startPos, Vector2 gridSize, float cellSize)
    {
        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                Rect cellRect = new Rect(
                    startPos.x + x * cellSize,
                    startPos.y + y * cellSize,
                    cellSize,
                    cellSize
                );

                // 检查单元格是否被占用
                bool isOccupied = IsCellOccupied(x, y);

                Color cellColor;
                if (showHeatmap)
                {
                    // 热力图模式
                    float heatValue = GetCellHeatValue(x, y);
                    cellColor = Color.Lerp(heatmapLowColor, heatmapHighColor, heatValue);
                    cellColor.a = 0.5f;
                }
                else if (showOccupiedCells)
                {
                    // 占用状态模式
                    cellColor = isOccupied ? occupiedCellColor : emptyCellColor;
                }
                else
                {
                    continue;
                }

                EditorGUI.DrawRect(cellRect, cellColor);

                // 绘制物品图标（如果启用）
                if (showItemIcons && isOccupied)
                {
                    DrawItemIcon(cellRect, x, y);
                }
            }
        }
    }

    /// <summary>
    /// 检查单元格是否被占用
    /// </summary>
    private bool IsCellOccupied(int x, int y)
    {
        if (cachedGridInfo?.occupancyMatrix == null) return false;
        
        // 检查坐标是否在有效范围内
        if (x < 0 || y < 0 || x >= cachedGridInfo.gridSize.x || y >= cachedGridInfo.gridSize.y)
            return false;
            
        return cachedGridInfo.occupancyMatrix[x, y] == 1;
    }

    /// <summary>
    /// 获取单元格的热力值
    /// </summary>
    private float GetCellHeatValue(int x, int y)
    {
        // 这里可以根据访问频率、物品价值等计算热力值
        // 目前简单地基于是否占用来计算
        return IsCellOccupied(x, y) ? 1.0f : 0.0f;
    }

    /// <summary>
    /// 绘制物品图标
    /// </summary>
    private void DrawItemIcon(Rect cellRect, int x, int y)
    {
        // 这里可以绘制物品的图标或标识
        // 目前简单地绘制一个小圆点
        Vector2 center = cellRect.center;
        float radius = Mathf.Min(cellRect.width, cellRect.height) * 0.2f;

        Handles.color = Color.white;
        Handles.DrawSolidDisc(center, Vector3.forward, radius);
    }

    /// <summary>
    /// 绘制网格信息
    /// </summary>
    private void DrawGridInfo(Rect visualizationRect)
    {
        Rect infoRect = new Rect(visualizationRect.x + 10, visualizationRect.y + 10, 200, 100);

        GUI.Box(infoRect, "");

        GUILayout.BeginArea(infoRect);
        GUILayout.Label("网格信息", EditorStyles.boldLabel);
        GUILayout.Label($"尺寸: {cachedGridInfo.gridSize.x}x{cachedGridInfo.gridSize.y}");
        GUILayout.Label($"占用率: {(cachedGridInfo.occupancyRate * 100):F1}%");
        GUILayout.Label($"物品数: {cachedGridInfo.placedItemsCount}");
        GUILayout.EndArea();
    }

    /// <summary>
    /// 处理输入
    /// </summary>
    private void HandleInput()
    {
        Event e = Event.current;

        if (e.type == EventType.ScrollWheel)
        {
            // 缩放
            float zoomDelta = -e.delta.y * 0.1f;
            zoomLevel = Mathf.Clamp(zoomLevel + zoomDelta, 0.1f, 5.0f);
            e.Use();
            Repaint();
        }
        else if (e.type == EventType.MouseDown && e.button == 0)
        {
            // 开始平移
            isPanning = true;
            lastMousePosition = e.mousePosition;
            e.Use();
        }
        else if (e.type == EventType.MouseDrag && isPanning)
        {
            // 平移
            Vector2 delta = e.mousePosition - lastMousePosition;
            panOffset += delta;
            lastMousePosition = e.mousePosition;
            e.Use();
            Repaint();
        }
        else if (e.type == EventType.MouseUp)
        {
            // 结束平移
            isPanning = false;
            e.Use();
        }
    }

    /// <summary>
    /// 重置视图
    /// </summary>
    private void ResetView()
    {
        zoomLevel = 1.0f;
        panOffset = Vector2.zero;
        Repaint();
    }
}