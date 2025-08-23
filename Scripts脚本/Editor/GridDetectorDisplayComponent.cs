using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System;
using InventorySystem.Grid;

/// <summary>
/// 网格检测器显示组件 - 负责在Editor窗口中显示网格检测器的详细信息
/// </summary>
public class GridDetectorDisplayComponent
{
    // GUI样式
    private GUIStyle headerStyle;
    private GUIStyle subHeaderStyle;
    private GUIStyle infoStyle;
    private GUIStyle warningStyle;
    private GUIStyle errorStyle;
    private GUIStyle successStyle;

    // 显示设置
    private GridMonitorSettings settings;

    // 折叠状态
    private Dictionary<string, bool> foldoutStates = new Dictionary<string, bool>();

    /// <summary>
    /// 构造函数
    /// </summary>
    public GridDetectorDisplayComponent(GridMonitorSettings settings = null)
    {
        this.settings = settings ?? new GridMonitorSettings();
        InitializeStyles();
        InitializeFoldoutStates();
    }

    /// <summary>
    /// 初始化GUI样式
    /// </summary>
    private void InitializeStyles()
    {
        headerStyle = new GUIStyle(EditorStyles.foldout)
        {
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.9f, 0.9f, 0.9f) }
        };

        subHeaderStyle = new GUIStyle(EditorStyles.label)
        {
            fontSize = 11,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.8f, 0.8f, 0.8f) }
        };

        infoStyle = new GUIStyle(EditorStyles.label)
        {
            fontSize = 10,
            normal = { textColor = new Color(0.9f, 0.9f, 0.9f) }
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

        successStyle = new GUIStyle(EditorStyles.label)
        {
            fontSize = 10,
            normal = { textColor = Color.green }
        };
    }

    /// <summary>
    /// 初始化折叠状态
    /// </summary>
    private void InitializeFoldoutStates()
    {
        foldoutStates["basicInfo"] = true;
        foldoutStates["itemDistribution"] = true;
        foldoutStates["occupancyInfo"] = true;
        foldoutStates["specificAnalysis"] = true;
        foldoutStates["performanceMetrics"] = false;
        foldoutStates["warnings"] = true;
    }

    /// <summary>
    /// 绘制网格检测器信息
    /// </summary>
    public void DrawGridDetectorInfo(BaseItemGrid grid, GridDetectorInfo info)
    {
        if (grid == null || info == null) return;

        EditorGUILayout.BeginVertical("box");

        // 网格标题和控制按钮
        DrawGridHeader(grid, info);

        // 基础信息
        if (settings.ShowBasicInfo)
        {
            DrawBasicInfo(grid, info);
        }

        // 物品分布
        if (settings.ShowItemDistribution && info.itemDistribution.Count > 0)
        {
            DrawItemDistribution(grid, info);
        }

        // 占用信息
        if (settings.ShowOccupancyInfo)
        {
            DrawOccupancyInfo(grid, info);
        }

        // 特定类型分析
        if (settings.ShowSpecificAnalysis)
        {
            DrawSpecificAnalysis(grid);
        }

        // 性能指标
        DrawPerformanceMetrics(grid, info);

        // 警告和建议
        DrawWarningsAndSuggestions(grid, info);

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 绘制网格监控信息（用于监控窗口）
    /// </summary>
    public void DrawGridMonitorInfo(BaseItemGrid grid, InventorySystem.Grid.GridDetectorInfo info, GridMonitorSettings monitorSettings)
    {
        if (grid == null || info == null) return;

        // 临时更新设置
        var originalSettings = this.settings;
        this.settings = monitorSettings;

        try
        {
            DrawGridDetectorInfo(grid, info);
        }
        finally
        {
            // 恢复原始设置
            this.settings = originalSettings;
        }
    }

    /// <summary>
    /// 绘制网格标题
    /// </summary>
    private void DrawGridHeader(BaseItemGrid grid, InventorySystem.Grid.GridDetectorInfo info)
    {
        EditorGUILayout.BeginHorizontal();

        // 网格名称和类型
        string gridTypeName = GetGridTypeDisplayName(grid.GetType().Name);
        EditorGUILayout.LabelField($"{grid.name} ({gridTypeName})", headerStyle);

        // 状态指示器
        DrawStatusIndicator(info);

        GUILayout.FlexibleSpace();

        // 控制按钮
        if (GUILayout.Button("选择", GUILayout.Width(50)))
        {
            Selection.activeGameObject = grid.gameObject;
        }

        if (GUILayout.Button("可视化", GUILayout.Width(60)))
        {
            GridVisualizationWindow window = EditorWindow.GetWindow<GridVisualizationWindow>();
            window.SetSelectedGrid(grid);
        }

        if (GUILayout.Button("刷新", GUILayout.Width(50)))
        {
            GridMonitorDataManager.Instance.ForceRefreshGrid(grid);
        }

        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// 绘制状态指示器
    /// </summary>
    private void DrawStatusIndicator(InventorySystem.Grid.GridDetectorInfo info)
    {
        Color indicatorColor;
        string statusText;

        if (info.occupancyRate * 100 >= settings.HighOccupancyThreshold)
        {
            indicatorColor = Color.red;
            statusText = "高占用";
        }
        else if (info.occupancyRate * 100 >= 50f)
        {
            indicatorColor = Color.yellow;
            statusText = "中占用";
        }
        else
        {
            indicatorColor = Color.green;
            statusText = "低占用";
        }

        GUI.color = indicatorColor;
        GUILayout.Label("●", GUILayout.Width(15));
        GUI.color = Color.white;

        GUILayout.Label(statusText, GUILayout.Width(50));
    }

    /// <summary>
    /// 绘制基础信息
    /// </summary>
    private void DrawBasicInfo(BaseItemGrid grid, GridDetectorInfo info)
    {
        string foldoutKey = $"{grid.GetInstanceID()}_basicInfo";
        if (!foldoutStates.ContainsKey(foldoutKey))
            foldoutStates[foldoutKey] = true;

        foldoutStates[foldoutKey] = EditorGUILayout.Foldout(foldoutStates[foldoutKey], "基础信息", headerStyle);

        if (foldoutStates[foldoutKey])
        {
            EditorGUILayout.BeginVertical("box");

            // 网格尺寸和容量
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"网格尺寸: {info.gridSize.x} × {info.gridSize.y}", infoStyle, GUILayout.Width(150));
            EditorGUILayout.LabelField($"总容量: {info.totalCells} 格", infoStyle);
            EditorGUILayout.EndHorizontal();

            // 占用情况
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"已占用: {info.occupiedCellsCount} 格", infoStyle, GUILayout.Width(150));
            EditorGUILayout.LabelField($"可用: {info.availableCells} 格", infoStyle);
            EditorGUILayout.EndHorizontal();

            // 占用率进度条
            float occupancyRatio = info.occupancyRate;
            Rect progressRect = GUILayoutUtility.GetRect(0, 20, GUILayout.ExpandWidth(true));

            Color progressColor = occupancyRatio > 0.8f ? Color.red :
                                 (occupancyRatio > 0.6f ? Color.yellow : Color.green);

            EditorGUI.ProgressBar(progressRect, occupancyRatio, $"占用率: {info.occupancyRate * 100:F1}%");

            // 物品统计
            EditorGUILayout.LabelField($"物品数量: {info.placedItemsCount}", infoStyle);

            if (info.itemDistribution.Count > 0)
        {
            EditorGUILayout.LabelField($"物品种类: {info.itemDistribution.Count}", infoStyle);
        }

            EditorGUILayout.EndVertical();
        }
    }

    /// <summary>
    /// 绘制物品分布
    /// </summary>
    private void DrawItemDistribution(BaseItemGrid grid, GridDetectorInfo info)
    {
        string foldoutKey = $"{grid.GetInstanceID()}_itemDistribution";
        if (!foldoutStates.ContainsKey(foldoutKey))
            foldoutStates[foldoutKey] = true;

        foldoutStates[foldoutKey] = EditorGUILayout.Foldout(foldoutStates[foldoutKey],
            $"物品分布 ({info.itemDistribution.Count} 种)", headerStyle);

        if (foldoutStates[foldoutKey])
        {
            EditorGUILayout.BeginVertical("box");

            var sortedItems = info.itemDistribution
                .OrderByDescending(kvp => kvp.Value)
                .Take(settings.MaxItemsToDisplay);

            foreach (var kvp in sortedItems)
            {
                EditorGUILayout.BeginHorizontal();

                // 物品名称
                EditorGUILayout.LabelField(kvp.Key, infoStyle, GUILayout.ExpandWidth(true));

                // 物品信息
                EditorGUILayout.LabelField($"{kvp.Value.itemName}", infoStyle, GUILayout.Width(120));
                
                // 位置信息
                EditorGUILayout.LabelField($"({kvp.Value.gridPosition.x},{kvp.Value.gridPosition.y})", infoStyle, GUILayout.Width(60));

                // 占比（基于物品数量计算）
                float percentage = 1.0f / info.placedItemsCount * 100f;
                EditorGUILayout.LabelField($"({percentage:F1}%)", infoStyle, GUILayout.Width(50));

                EditorGUILayout.EndHorizontal();

                // 小型进度条
                Rect itemProgressRect = GUILayoutUtility.GetRect(0, 8, GUILayout.ExpandWidth(true));
                EditorGUI.ProgressBar(itemProgressRect, percentage / 100f, "");
            }

            if (info.itemDistribution.Count > settings.MaxItemsToDisplay)
            {
                EditorGUILayout.LabelField(
                    $"... 还有 {info.itemDistribution.Count - settings.MaxItemsToDisplay} 种物品",
                    EditorStyles.miniLabel);
            }

            EditorGUILayout.EndVertical();
        }
    }

    /// <summary>
    /// 绘制占用信息
    /// </summary>
    private void DrawOccupancyInfo(BaseItemGrid grid, GridDetectorInfo info)
    {
        string foldoutKey = $"{grid.GetInstanceID()}_occupancyInfo";
        if (!foldoutStates.ContainsKey(foldoutKey))
            foldoutStates[foldoutKey] = true;

        foldoutStates[foldoutKey] = EditorGUILayout.Foldout(foldoutStates[foldoutKey], "占用分析", headerStyle);

        if (foldoutStates[foldoutKey])
        {
            EditorGUILayout.BeginVertical("box");

            // 注释：区域占用信息和可用空间信息功能暂时不可用
            // 因为GridDetectorInfo中没有AreaOccupancy和AvailableSpaces属性
            EditorGUILayout.LabelField("高级占用分析功能开发中...", EditorStyles.miniLabel);

            EditorGUILayout.EndVertical();
        }
    }

    /// <summary>
    /// 绘制特定类型分析
    /// </summary>
    private void DrawSpecificAnalysis(BaseItemGrid grid)
    {
        string foldoutKey = $"{grid.GetInstanceID()}_specificAnalysis";
        if (!foldoutStates.ContainsKey(foldoutKey))
            foldoutStates[foldoutKey] = true;

        foldoutStates[foldoutKey] = EditorGUILayout.Foldout(foldoutStates[foldoutKey], "专项分析", headerStyle);

        if (foldoutStates[foldoutKey])
        {
            EditorGUILayout.BeginVertical("box");

            try
            {
                // 仓库分析
                if (grid is ItemGrid warehouseGrid)
                {
                    DrawWarehouseAnalysisDetailed(warehouseGrid);
                }
                // 背包分析
                else if (grid is BackpackItemGrid backpackGrid)
                {
                    DrawBackpackAnalysisDetailed(backpackGrid);
                }
                // 战术挂具分析
                else if (grid is TactiaclRigItemGrid tacticalGrid)
                {
                    EditorGUILayout.LabelField("战术挂具分析功能开发中", infoStyle);
                }
                else
                {
                    EditorGUILayout.LabelField("此网格类型暂无专项分析", infoStyle);
                }
            }
            catch (Exception e)
            {
                EditorGUILayout.LabelField($"专项分析错误: {e.Message}", errorStyle);
            }

            EditorGUILayout.EndVertical();
        }
    }

    /// <summary>
    /// 绘制详细仓库分析
    /// </summary>
    private void DrawWarehouseAnalysisDetailed(ItemGrid warehouseGrid)
    {
        EditorGUILayout.LabelField("仓库效率分析功能开发中", infoStyle);
    }

    /// <summary>
    /// 绘制详细背包分析
    /// </summary>
    private void DrawBackpackAnalysisDetailed(BackpackItemGrid backpackGrid)
    {
        EditorGUILayout.LabelField("背包负重分析功能开发中", infoStyle);
    }

    /// <summary>
    /// 绘制详细战术挂具分析
    /// </summary>
    private void DrawTacticalRigAnalysisDetailed(TactiaclRigItemGrid tacticalGrid)
    {
        EditorGUILayout.LabelField("战术挂具分析功能开发中", infoStyle);
    }

    /// <summary>
    /// 绘制性能指标
    /// </summary>
    private void DrawPerformanceMetrics(BaseItemGrid grid, GridDetectorInfo info)
    {
        string foldoutKey = $"{grid.GetInstanceID()}_performanceMetrics";
        if (!foldoutStates.ContainsKey(foldoutKey))
            foldoutStates[foldoutKey] = false;

        foldoutStates[foldoutKey] = EditorGUILayout.Foldout(foldoutStates[foldoutKey], "性能指标", headerStyle);

        if (foldoutStates[foldoutKey])
        {
            EditorGUILayout.BeginVertical("box");

            // 计算一些性能指标
            float densityRatio = info.totalCells > 0 ? (float)info.placedItemsCount / info.totalCells : 0f;
        float spaceUtilization = info.totalCells > 0 ? (float)info.occupiedCellsCount / info.totalCells : 0f;

            EditorGUILayout.LabelField($"物品密度: {densityRatio:F2}", infoStyle);
            EditorGUILayout.LabelField($"空间利用率: {spaceUtilization:F2}", infoStyle);
            EditorGUILayout.LabelField($"可用格子数: {info.availableCells}", infoStyle);

            EditorGUILayout.EndVertical();
        }
    }

    /// <summary>
    /// 绘制警告和建议
    /// </summary>
    private void DrawWarningsAndSuggestions(BaseItemGrid grid, GridDetectorInfo info)
    {
        List<string> warnings = new List<string>();
        List<string> suggestions = new List<string>();

        // 检查各种警告条件
        if (info.occupancyRate * 100 >= settings.HighOccupancyThreshold)
        {
            warnings.Add($"占用率过高 ({info.occupancyRate * 100:F1}%)");
            suggestions.Add("建议清理不必要的物品或扩展存储空间");
        }

        if (info.availableCells == 0)
        {
            warnings.Add("没有可用空间");
            suggestions.Add("建议整理物品以创建更多空间");
        }

        if (info.itemDistribution.Count > info.totalCells * 0.8f)
        {
            warnings.Add("物品种类过多，可能影响管理效率");
            suggestions.Add("建议合并相似物品或分类存储");
        }

        // 显示警告和建议
        if (warnings.Count > 0 || suggestions.Count > 0)
        {
            string foldoutKey = $"{grid.GetInstanceID()}_warnings";
            if (!foldoutStates.ContainsKey(foldoutKey))
                foldoutStates[foldoutKey] = true;

            foldoutStates[foldoutKey] = EditorGUILayout.Foldout(foldoutStates[foldoutKey],
                $"警告和建议 ({warnings.Count + suggestions.Count})", headerStyle);

            if (foldoutStates[foldoutKey])
            {
                EditorGUILayout.BeginVertical("box");

                // 显示警告
                foreach (string warning in warnings)
                {
                    EditorGUILayout.LabelField($"?? {warning}", warningStyle);
                }

                // 显示建议
                foreach (string suggestion in suggestions)
                {
                    EditorGUILayout.LabelField($"? {suggestion}", infoStyle);
                }

                EditorGUILayout.EndVertical();
            }
        }
    }

    /// <summary>
    /// 获取网格类型的显示名称
    /// </summary>
    private string GetGridTypeDisplayName(string typeName)
    {
        switch (typeName)
        {
            case "ItemGrid": return "仓库网格";
            case "BackpackItemGrid": return "背包网格";
            case "TactiaclRigItemGrid": return "战术挂具";
            case "BaseItemGrid": return "基础网格";
            default: return typeName;
        }
    }

    /// <summary>
    /// 获取配置类型的显示名称
    /// </summary>
    private string GetLoadoutTypeDisplayName(LoadoutType loadoutType)
    {
        switch (loadoutType)
        {
            case LoadoutType.Assault: return "突击配置";
            case LoadoutType.Support: return "支援配置";
            case LoadoutType.Marksman: return "射手配置";
            case LoadoutType.Utility: return "工具配置";
            case LoadoutType.Balanced: return "平衡配置";
            default: return loadoutType.ToString();
        }
    }
}