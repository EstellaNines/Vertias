using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using InventorySystem.Grid;

/// <summary>
/// 网格检测器自定义Inspector - 在Inspector面板中显示网格的详细检测信息
/// </summary>
[CustomEditor(typeof(BaseItemGrid), true)]
public class GridDetectorInspector : Editor
{
    // 折叠面板状态
    private bool showBasicInfo = true;
    private bool showItemDistribution = true;
    private bool showOccupancyInfo = true;
    private bool showSpecificAnalysis = true;
    private bool showVisualization = false;

    // 自动刷新设置
    private bool autoRefresh = false;
    private double lastRefreshTime;
    private float refreshInterval = 2.0f;

    // 缓存的检测器信息
    private InventorySystem.Grid.GridDetectorInfo cachedDetectorInfo;

    // GUI样式
    private GUIStyle headerStyle;
    private GUIStyle subHeaderStyle;
    private GUIStyle infoStyle;
    private GUIStyle warningStyle;
    private GUIStyle errorStyle;

    /// <summary>
    /// 启用时初始化
    /// </summary>
    private void OnEnable()
    {
        InitializeStyles();
        RefreshDetectorInfo();
    }

    /// <summary>
    /// 初始化GUI样式
    /// </summary>
    private void InitializeStyles()
    {
        headerStyle = new GUIStyle(EditorStyles.foldout)
        {
            fontSize = 12,
            fontStyle = FontStyle.Bold
        };

        subHeaderStyle = new GUIStyle(EditorStyles.label)
        {
            fontSize = 11,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.7f, 0.7f, 0.7f) }
        };

        infoStyle = new GUIStyle(EditorStyles.label)
        {
            fontSize = 10,
            normal = { textColor = Color.white }
        };

        warningStyle = new GUIStyle(EditorStyles.label)
        {
            fontSize = 10,
            normal = { textColor = Color.yellow }
        };

        errorStyle = new GUIStyle(EditorStyles.label)
        {
            fontSize = 10,
            normal = { textColor = Color.red }
        };
    }

    /// <summary>
    /// 刷新检测器信息
    /// </summary>
    private void RefreshDetectorInfo()
    {
        BaseItemGrid grid = target as BaseItemGrid;
        if (grid != null)
        {
            try
            {
                cachedDetectorInfo = grid.GetGridDetectorInfo();
                lastRefreshTime = EditorApplication.timeSinceStartup;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"刷新网格检测器信息时出错: {e.Message}");
                cachedDetectorInfo = null;
            }
        }
    }

    /// <summary>
    /// 绘制Inspector GUI
    /// </summary>
    public override void OnInspectorGUI()
    {
        // 绘制默认Inspector
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("网格检测器信息", EditorStyles.boldLabel);

        // 检查自动刷新
        if (autoRefresh && EditorApplication.timeSinceStartup - lastRefreshTime > refreshInterval)
        {
            RefreshDetectorInfo();
        }

        DrawDetectorControls();

        if (cachedDetectorInfo != null)
        {
            DrawDetectorInfo();
        }
        else
        {
            EditorGUILayout.HelpBox("无法获取网格检测器信息", MessageType.Warning);
        }
    }

    /// <summary>
    /// 绘制检测器控制面板
    /// </summary>
    private void DrawDetectorControls()
    {
        EditorGUILayout.BeginVertical("box");

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("刷新检测器信息", GUILayout.Height(25)))
        {
            RefreshDetectorInfo();
        }

        if (GUILayout.Button("打开监控窗口", GUILayout.Height(25)))
        {
            GridSystemMonitorWindow.ShowWindow();
        }

        if (GUILayout.Button("可视化网格", GUILayout.Height(25)))
        {
            GridVisualizationWindow window = EditorWindow.GetWindow<GridVisualizationWindow>();
            window.SetSelectedGrid(target as BaseItemGrid);
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        autoRefresh = EditorGUILayout.ToggleLeft("自动刷新", autoRefresh, GUILayout.Width(80));

        if (autoRefresh)
        {
            EditorGUILayout.LabelField("间隔:", GUILayout.Width(30));
            refreshInterval = EditorGUILayout.Slider(refreshInterval, 1.0f, 10.0f, GUILayout.Width(100));
            EditorGUILayout.LabelField("秒", GUILayout.Width(20));
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.LabelField($"最后更新: {System.DateTime.FromBinary((long)lastRefreshTime):HH:mm:ss}",
            EditorStyles.miniLabel);

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 绘制检测器信息
    /// </summary>
    private void DrawDetectorInfo()
    {
        // 基础信息
        showBasicInfo = EditorGUILayout.Foldout(showBasicInfo, "基础信息", headerStyle);
        if (showBasicInfo)
        {
            DrawBasicInfo();
        }

        // 物品分布
        if (cachedDetectorInfo.itemDistribution.Count > 0)
        {
            showItemDistribution = EditorGUILayout.Foldout(showItemDistribution, "物品分布", headerStyle);
            if (showItemDistribution)
            {
                DrawItemDistribution();
            }
        }

        // 占用信息
        showOccupancyInfo = EditorGUILayout.Foldout(showOccupancyInfo, "占用分析", headerStyle);
        if (showOccupancyInfo)
        {
            DrawOccupancyInfo();
        }

        // 特定类型分析
        showSpecificAnalysis = EditorGUILayout.Foldout(showSpecificAnalysis, "专项分析", headerStyle);
        if (showSpecificAnalysis)
        {
            DrawSpecificAnalysis();
        }

        // 可视化预览
        showVisualization = EditorGUILayout.Foldout(showVisualization, "可视化预览", headerStyle);
        if (showVisualization)
        {
            DrawVisualizationPreview();
        }
    }

    /// <summary>
    /// 绘制基础信息
    /// </summary>
    private void DrawBasicInfo()
    {
        EditorGUILayout.BeginVertical("box");

        EditorGUILayout.LabelField($"网格尺寸: {cachedDetectorInfo.gridSize.x} x {cachedDetectorInfo.gridSize.y}", infoStyle);
        EditorGUILayout.LabelField($"总容量: {cachedDetectorInfo.totalCells} 格", infoStyle);
        EditorGUILayout.LabelField($"已占用: {cachedDetectorInfo.occupiedCellsCount} 格", infoStyle);
        EditorGUILayout.LabelField($"可用空间: {cachedDetectorInfo.availableCells} 格", infoStyle);

        // 占用率进度条
        float occupancyRatio = cachedDetectorInfo.occupancyRate;
        Color barColor = occupancyRatio > 0.8f ? Color.red : (occupancyRatio > 0.6f ? Color.yellow : Color.green);

        EditorGUILayout.LabelField($"占用率: {cachedDetectorInfo.occupancyRate * 100:F1}%",
            occupancyRatio > 0.8f ? warningStyle : infoStyle);

        Rect progressRect = GUILayoutUtility.GetRect(0, 20, GUILayout.ExpandWidth(true));
        EditorGUI.ProgressBar(progressRect, occupancyRatio, $"{cachedDetectorInfo.occupancyRate * 100:F1}%");

        EditorGUILayout.LabelField($"物品数量: {cachedDetectorInfo.placedItemsCount}", infoStyle);

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 绘制物品分布
    /// </summary>
    private void DrawItemDistribution()
    {
        EditorGUILayout.BeginVertical("box");

        if (cachedDetectorInfo.itemDistribution.Count == 0)
        {
            EditorGUILayout.LabelField("暂无物品", infoStyle);
        }
        else
        {
            EditorGUILayout.LabelField($"物品种类: {cachedDetectorInfo.itemDistribution.Count}", subHeaderStyle);

            // 显示前10种物品
            var sortedItems = cachedDetectorInfo.itemDistribution
                .OrderByDescending(kvp => kvp.Value)
                .Take(10);

            foreach (var kvp in sortedItems)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(kvp.Key, infoStyle, GUILayout.ExpandWidth(true));
                EditorGUILayout.LabelField($"{kvp.Value} 个", infoStyle, GUILayout.Width(60));
                EditorGUILayout.EndHorizontal();
            }

            if (cachedDetectorInfo.itemDistribution.Count > 10)
            {
                EditorGUILayout.LabelField($"... 还有 {cachedDetectorInfo.itemDistribution.Count - 10} 种物品",
                    EditorStyles.miniLabel);
            }
        }

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 绘制占用信息
    /// </summary>
    private void DrawOccupancyInfo()
    {
        EditorGUILayout.BeginVertical("box");

        // 区域占用信息 - 功能开发中
        EditorGUILayout.LabelField("区域占用分析功能开发中...", EditorStyles.centeredGreyMiniLabel);
        
        // 可用空间信息 - 功能开发中  
        EditorGUILayout.LabelField("可用空间分析功能开发中...", EditorStyles.centeredGreyMiniLabel);

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 绘制特定类型分析
    /// </summary>
    private void DrawSpecificAnalysis()
    {
        BaseItemGrid grid = target as BaseItemGrid;

        EditorGUILayout.BeginVertical("box");

        // 仓库分析
        if (grid is ItemGrid warehouseGrid)
        {
            DrawWarehouseAnalysis(warehouseGrid);
        }
        // 背包分析
        else if (grid is BackpackItemGrid backpackGrid)
        {
            DrawBackpackAnalysis(backpackGrid);
        }
        // 战术挂具分析
        else if (grid is TactiaclRigItemGrid tacticalGrid)
        {
            DrawTacticalRigAnalysis(tacticalGrid);
        }
        else
        {
            EditorGUILayout.LabelField("此网格类型暂无专项分析", infoStyle);
        }

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 绘制仓库分析
    /// </summary>
    private void DrawWarehouseAnalysis(ItemGrid warehouseGrid)
    {
        try
        {
            // 仓库效率分析功能开发中
            EditorGUILayout.LabelField("仓库效率分析功能开发中...", EditorStyles.centeredGreyMiniLabel);
        }
        catch (System.Exception e)
        {
            EditorGUILayout.LabelField($"仓库分析错误: {e.Message}", errorStyle);
        }
    }

    /// <summary>
    /// 绘制背包分析
    /// </summary>
    private void DrawBackpackAnalysis(BackpackItemGrid backpackGrid)
    {
        try
        {
            // 背包负重分析功能开发中
            EditorGUILayout.LabelField("背包负重分析功能开发中...", EditorStyles.centeredGreyMiniLabel);
        }
        catch (System.Exception e)
        {
            EditorGUILayout.LabelField($"背包分析错误: {e.Message}", errorStyle);
        }
    }

    /// <summary>
    /// 绘制战术挂具分析
    /// </summary>
    private void DrawTacticalRigAnalysis(TactiaclRigItemGrid tacticalGrid)
    {
        try
        {
            // 战术挂具分析功能开发中
            EditorGUILayout.LabelField("战术挂具分析功能开发中...", EditorStyles.centeredGreyMiniLabel);
        }
        catch (System.Exception e)
        {
            EditorGUILayout.LabelField($"挂具分析错误: {e.Message}", errorStyle);
        }
    }

    /// <summary>
    /// 绘制可视化预览
    /// </summary>
    private void DrawVisualizationPreview()
    {
        EditorGUILayout.BeginVertical("box");

        // 简单的网格预览
        if (cachedDetectorInfo != null)
        {
            Rect previewRect = GUILayoutUtility.GetRect(200, 150);
            DrawSimpleGridPreview(previewRect);
        }

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("打开详细可视化"))
        {
            GridVisualizationWindow window = EditorWindow.GetWindow<GridVisualizationWindow>();
            window.SetSelectedGrid(target as BaseItemGrid);
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 绘制简单网格预览
    /// </summary>
    private void DrawSimpleGridPreview(Rect rect)
    {
        // 绘制背景
        EditorGUI.DrawRect(rect, new Color(0.2f, 0.2f, 0.2f));

        Vector2 gridSize = cachedDetectorInfo.gridSize;
        float cellSize = Mathf.Min(rect.width / gridSize.x, rect.height / gridSize.y) * 0.8f;

        Vector2 startPos = new Vector2(
            rect.x + (rect.width - gridSize.x * cellSize) * 0.5f,
            rect.y + (rect.height - gridSize.y * cellSize) * 0.5f
        );

        // 绘制网格
        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                Rect cellRect = new Rect(
                    startPos.x + x * cellSize,
                    startPos.y + y * cellSize,
                    cellSize - 1,
                    cellSize - 1
                );

                // 使用占用矩阵来判断格子是否被占用
                bool isOccupied = false;
                if (cachedDetectorInfo.occupancyMatrix != null && 
                    x < cachedDetectorInfo.occupancyMatrix.GetLength(0) && 
                    y < cachedDetectorInfo.occupancyMatrix.GetLength(1))
                {
                    isOccupied = cachedDetectorInfo.occupancyMatrix[x, y] > 0;
                }

                Color cellColor = isOccupied ? new Color(1f, 0.3f, 0.3f, 0.8f) : new Color(0.3f, 0.3f, 0.3f, 0.5f);
                EditorGUI.DrawRect(cellRect, cellColor);
            }
        }
    }

    /// <summary>
    /// 获取配置类型的显示名称
    /// </summary>
    private string GetLoadoutTypeDisplayName(InventorySystem.Grid.LoadoutType loadoutType)
    {
        switch (loadoutType)
        {
            case InventorySystem.Grid.LoadoutType.Assault: return "突击配置";
            case InventorySystem.Grid.LoadoutType.Support: return "支援配置";
            case InventorySystem.Grid.LoadoutType.Marksman: return "射手配置";
            case InventorySystem.Grid.LoadoutType.Utility: return "工具配置";
            case InventorySystem.Grid.LoadoutType.Balanced: return "平衡配置";
            default: return loadoutType.ToString();
        }
    }
}