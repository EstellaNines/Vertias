using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System;
using InventorySystem.Grid;
using GridDetectorInfo = InventorySystem.Grid.GridDetectorInfo;

/// <summary>
/// 网格系统监控窗口 - 实时监控所有网格系统的状态
/// </summary>
public class GridSystemMonitorWindow : EditorWindow
{
    // 核心组件
    private GridDetectorDisplayComponent displayComponent;
    private GridMonitorSettings settings;
    private GridMonitorDataManager dataManager;
    private GridRealTimeRefreshSystem refreshSystem;

    // 窗口设置
    private Vector2 scrollPosition;
    private Vector2 settingsScrollPosition;
    private Vector2 saveSystemScrollPosition;
    private Vector2 crossSceneScrollPosition;
    private double lastRefreshTime;

    // 显示状态
    private bool showSettings = false;
    private bool showStatistics = false;
    private bool showRefreshSettings = false;
    private bool showSaveSystemMonitor = false;
    private bool showCrossSceneIDSync = false;
    private bool isInitialized = false;

    // 缓存数据
    private List<BaseItemGrid> cachedGrids = new List<BaseItemGrid>();
    private Dictionary<BaseItemGrid, GridDetectorInfo> cachedDetectorInfo = new Dictionary<BaseItemGrid, GridDetectorInfo>();
    private Dictionary<BaseItemGrid, bool> gridFoldoutStates = new Dictionary<BaseItemGrid, bool>();

    // 统计信息
    private int totalGrids;
    private int activeGrids;
    private int totalItems;
    private float averageOccupancy;
    private GridSystemStatistics currentStatistics;
    private RefreshStatistics refreshStatistics;

    // GUI样式
    private GUIStyle headerStyle;
    private GUIStyle subHeaderStyle;
    private GUIStyle infoStyle;
    private GUIStyle warningStyle;
    private GUIStyle errorStyle;
    private GUIStyle toolbarStyle;

    /// <summary>
    /// 打开监控窗口
    /// </summary>
    [MenuItem("工具/网格系统监控")]
    public static void ShowWindow()
    {
        GridSystemMonitorWindow window = GetWindow<GridSystemMonitorWindow>("网格系统监控");
        window.minSize = new Vector2(500, 400);
        window.Show();
    }

    /// <summary>
    /// 窗口启用时初始化
    /// </summary>
    private void OnEnable()
    {
        InitializeComponents();
        InitializeStyles();

        // 初始化实时刷新系统
        refreshSystem = GridRealTimeRefreshSystem.Instance;

        // 注册数据管理器事件
        dataManager.OnGridDataUpdated += OnGridDataUpdated;
        dataManager.OnAllGridsRefreshed += OnAllGridsRefreshed;

        // 注册刷新系统事件
        refreshSystem.OnRefreshStarted += OnRefreshStarted;
        refreshSystem.OnRefreshCompleted += OnRefreshCompleted;
        refreshSystem.OnGridRefreshed += OnGridRefreshed;
        refreshSystem.OnRefreshError += OnRefreshError;

        // 开始监控
        dataManager.StartMonitoring();

        RefreshGridData();
        lastRefreshTime = EditorApplication.timeSinceStartup;
        isInitialized = true;
    }

    /// <summary>
    /// 初始化组件
    /// </summary>
    private void InitializeComponents()
    {
        if (settings == null)
            settings = new GridMonitorSettings();

        if (dataManager == null)
            dataManager = GridMonitorDataManager.Instance;

        if (displayComponent == null)
            displayComponent = new GridDetectorDisplayComponent();
    }

    /// <summary>
    /// 初始化GUI样式
    /// </summary>
    private void InitializeStyles()
    {
        headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 14,
            normal = { textColor = Color.white }
        };

        subHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 12,
            normal = { textColor = new Color(0.8f, 0.8f, 0.8f) }
        };

        infoStyle = new GUIStyle(EditorStyles.label)
        {
            fontSize = 10,
            normal = { textColor = Color.green }
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

        toolbarStyle = new GUIStyle(EditorStyles.toolbar)
        {
            fixedHeight = 25
        };
    }

    /// <summary>
    /// 刷新网格数据
    /// </summary>
    private void RefreshGridData()
    {
        if (dataManager == null) return;

        cachedGrids.Clear();
        cachedDetectorInfo.Clear();

        // 使用数据管理器获取网格数据
        var gridData = dataManager.GetAllGridData();

        foreach (var kvp in gridData)
        {
            cachedGrids.Add(kvp.Key);
            cachedDetectorInfo[kvp.Key] = kvp.Value;
        }

        // 应用过滤和排序
        ApplyFiltersAndSorting();

        // 更新统计信息
        UpdateStatistics();
    }

    /// <summary>
    /// 应用过滤和排序
    /// </summary>
    private void ApplyFiltersAndSorting()
    {
        if (settings == null) return;

        var filteredGrids = cachedGrids.AsEnumerable();

        // 应用过滤器
        if (settings.ShowOnlyNonEmptyGrids)
        {
            filteredGrids = filteredGrids.Where(g =>
                cachedDetectorInfo.ContainsKey(g) &&
                cachedDetectorInfo[g].placedItemsCount > 0);
        }

        // 应用排序
        if (settings.SortByOccupancy)
        {
            filteredGrids = filteredGrids.OrderByDescending(g =>
                cachedDetectorInfo.ContainsKey(g) ?
                cachedDetectorInfo[g].occupancyRate * 100 : 0);
        }
        else
        {
            filteredGrids = filteredGrids.OrderBy(g => g.GetType().Name);
        }

        cachedGrids = filteredGrids.ToList();
    }

    /// <summary>
    /// 更新统计信息
    /// </summary>
    private void UpdateStatistics()
    {
        // 更新数据管理器统计
        if (dataManager != null)
        {
            currentStatistics = dataManager.GetSystemStatistics();
        }

        // 更新刷新系统统计
        if (refreshSystem != null)
        {
            refreshStatistics = refreshSystem.GetRefreshStatistics();
        }

        // 计算基础统计
        totalGrids = cachedGrids.Count;
        activeGrids = cachedDetectorInfo.Count(kvp => kvp.Value.placedItemsCount > 0);
        totalItems = cachedDetectorInfo.Values.Sum(info => info.placedItemsCount);
        averageOccupancy = cachedDetectorInfo.Values.Any() ?
            cachedDetectorInfo.Values.Average(info => info.occupancyRate * 100) : 0f;
    }

    /// <summary>
    /// 绘制GUI界面
    /// </summary>
    private void OnGUI()
    {
        if (!isInitialized)
        {
            EditorGUILayout.HelpBox("正在初始化监控窗口...", MessageType.Info);
            return;
        }

        // 检查自动刷新
        if (settings.EnableAutoRefresh &&
            EditorApplication.timeSinceStartup - lastRefreshTime > settings.AutoRefreshInterval)
        {
            RefreshGridData();
            lastRefreshTime = EditorApplication.timeSinceStartup;
            Repaint();
        }

        DrawMainToolbar();

        if (showSettings)
        {
            DrawSettingsPanel();
        }

        if (showStatistics)
        {
            DrawStatisticsPanel();
        }

        if (showRefreshSettings)
        {
            DrawRefreshSettingsPanel();
        }

        if (showSaveSystemMonitor)
        {
            DrawSaveSystemMonitorPanel();
        }

        if (showCrossSceneIDSync)
        {
            DrawCrossSceneIDSyncPanel();
        }

        DrawMainContent();
    }

    /// <summary>
    /// 绘制主工具栏
    /// </summary>
    private void DrawMainToolbar()
    {
        EditorGUILayout.BeginHorizontal(toolbarStyle);

        // 刷新按钮
        if (GUILayout.Button("? 刷新", EditorStyles.toolbarButton, GUILayout.Width(60)))
        {
            refreshSystem.RefreshAllGrids();
        }

        // 刷新选中按钮
        if (GUILayout.Button("? 刷新选中", EditorStyles.toolbarButton, GUILayout.Width(80)))
        {
            refreshSystem.RefreshSelectedGrids();
        }

        // 自动刷新切换
        bool newAutoRefresh = GUILayout.Toggle(refreshSystem.IsAutoRefreshEnabled, "自动刷新", EditorStyles.toolbarButton);
        if (newAutoRefresh != refreshSystem.IsAutoRefreshEnabled)
        {
            if (newAutoRefresh)
            {
                refreshSystem.EnableAutoRefresh();
            }
            else
            {
                refreshSystem.DisableAutoRefresh();
            }
        }

        // 显示刷新状态
        if (refreshSystem.IsRefreshing)
        {
            GUI.color = Color.yellow;
            GUILayout.Label($"? 刷新中... ({refreshSystem.QueuedGridsCount})", EditorStyles.toolbarButton);
            GUI.color = Color.white;
        }

        GUILayout.FlexibleSpace();

        // 刷新设置按钮
        showRefreshSettings = GUILayout.Toggle(showRefreshSettings, "? 刷新设置", EditorStyles.toolbarButton);

        showSettings = GUILayout.Toggle(showSettings, "?? 设置", EditorStyles.toolbarButton, GUILayout.Width(60));
        showStatistics = GUILayout.Toggle(showStatistics, "? 统计", EditorStyles.toolbarButton, GUILayout.Width(60));
        showSaveSystemMonitor = GUILayout.Toggle(showSaveSystemMonitor, "? 保存系统", EditorStyles.toolbarButton, GUILayout.Width(80));
        showCrossSceneIDSync = GUILayout.Toggle(showCrossSceneIDSync, "? 跨场景同步", EditorStyles.toolbarButton, GUILayout.Width(100));

        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// 绘制设置面板
    /// </summary>
    private void DrawSettingsPanel()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("监控设置", headerStyle);

        settingsScrollPosition = EditorGUILayout.BeginScrollView(settingsScrollPosition, GUILayout.Height(120));

        // 显示设置
        EditorGUILayout.LabelField("显示选项", subHeaderStyle);
        settings.ShowBasicInfo = EditorGUILayout.Toggle("基础信息", settings.ShowBasicInfo);
        settings.ShowItemDistribution = EditorGUILayout.Toggle("物品分布", settings.ShowItemDistribution);
        settings.ShowOccupancyInfo = EditorGUILayout.Toggle("占用信息", settings.ShowOccupancyInfo);
        settings.ShowSpecificAnalysis = EditorGUILayout.Toggle("特定分析", settings.ShowSpecificAnalysis);
        settings.ShowPerformanceMetrics = EditorGUILayout.Toggle("性能指标", settings.ShowPerformanceMetrics);

        EditorGUILayout.Space();

        // 过滤设置
        EditorGUILayout.LabelField("过滤选项", subHeaderStyle);
        bool oldShowOnlyNonEmpty = settings.ShowOnlyNonEmptyGrids;
        bool oldSortByOccupancy = settings.SortByOccupancy;

        settings.ShowOnlyNonEmptyGrids = EditorGUILayout.Toggle("仅显示非空网格", settings.ShowOnlyNonEmptyGrids);
        settings.SortByOccupancy = EditorGUILayout.Toggle("按占用率排序", settings.SortByOccupancy);

        // 如果过滤设置改变，重新应用过滤
        if (oldShowOnlyNonEmpty != settings.ShowOnlyNonEmptyGrids ||
            oldSortByOccupancy != settings.SortByOccupancy)
        {
            ApplyFiltersAndSorting();
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 绘制统计面板
    /// </summary>
    private void DrawStatisticsPanel()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("系统统计", headerStyle);

        // 基础统计信息
        EditorGUILayout.LabelField("基础信息", subHeaderStyle);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"总网格数: {totalGrids}", infoStyle, GUILayout.Width(100));
        EditorGUILayout.LabelField($"活跃网格: {activeGrids}", infoStyle, GUILayout.Width(100));
        EditorGUILayout.LabelField($"总物品数: {totalItems}", infoStyle, GUILayout.Width(100));
        EditorGUILayout.LabelField($"平均占用率: {averageOccupancy:F1}%",
            averageOccupancy > 80 ? warningStyle : infoStyle, GUILayout.Width(120));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // 数据管理器统计
        if (currentStatistics != null)
        {
            EditorGUILayout.LabelField("数据管理统计", subHeaderStyle);
            EditorGUILayout.LabelField($"总网格数: {currentStatistics.TotalGrids}", infoStyle);
            EditorGUILayout.LabelField($"活跃网格数: {currentStatistics.ActiveGrids}", infoStyle);
            EditorGUILayout.LabelField($"总物品数: {currentStatistics.TotalItems}", infoStyle);
            var lastUpdateDateTime = new DateTime(1970, 1, 1).AddSeconds(currentStatistics.LastUpdateTime);
            EditorGUILayout.LabelField($"上次更新时间: {lastUpdateDateTime:yyyy-MM-dd HH:mm:ss}", infoStyle);

            EditorGUILayout.Space();
        }

        // 刷新系统统计
        if (refreshStatistics != null)
        {
            EditorGUILayout.LabelField("刷新系统统计", subHeaderStyle);
            EditorGUILayout.LabelField($"总刷新次数: {refreshStatistics.TotalRefreshCount}", infoStyle);
            EditorGUILayout.LabelField($"平均刷新时间: {refreshStatistics.AverageRefreshTime:F2} ms", infoStyle);
            EditorGUILayout.LabelField($"自动刷新启用: {(refreshStatistics.IsAutoRefreshEnabled ? "是" : "否")}", infoStyle);
            EditorGUILayout.LabelField($"刷新间隔: {refreshStatistics.RefreshInterval:F1} 秒", infoStyle);
            EditorGUILayout.LabelField($"当前刷新状态: {(refreshStatistics.IsCurrentlyRefreshing ? "刷新中" : "空闲")}", infoStyle);
            EditorGUILayout.LabelField($"队列中网格数: {refreshSystem.QueuedGridsCount}", infoStyle);

            // 刷新状态指示
            if (refreshSystem.IsRefreshing)
            {
                GUI.color = Color.yellow;
                EditorGUILayout.LabelField("? 正在刷新...", infoStyle);
                GUI.color = Color.white;
            }
            else if (refreshSystem.IsAutoRefreshEnabled)
            {
                GUI.color = Color.green;
                EditorGUILayout.LabelField("? 自动刷新已启用", infoStyle);
                GUI.color = Color.white;
            }
            else
            {
                GUI.color = Color.gray;
                EditorGUILayout.LabelField("?? 自动刷新已暂停", infoStyle);
                GUI.color = Color.white;
            }

            EditorGUILayout.Space();
        }

        // 网格类型统计
        var gridTypeCounts = cachedGrids.GroupBy(g => g.GetType().Name)
            .ToDictionary(g => g.Key, g => g.Count());

        if (gridTypeCounts.Count > 0)
        {
            EditorGUILayout.LabelField("网格类型分布", subHeaderStyle);
            foreach (var kvp in gridTypeCounts)
            {
                EditorGUILayout.LabelField($"  {GetGridTypeDisplayName(kvp.Key)}: {kvp.Value}", infoStyle);
            }
        }

        EditorGUILayout.Space();

        // 控制按钮
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("刷新统计", GUILayout.Width(80)))
        {
            UpdateStatistics();
            if (dataManager != null)
            {
                currentStatistics = dataManager.GetSystemStatistics();
            }
            if (refreshSystem != null)
            {
                refreshStatistics = refreshSystem.GetRefreshStatistics();
            }
        }

        if (GUILayout.Button("重置数据统计", GUILayout.Width(100)))
        {
            if (dataManager != null)
            {
                dataManager.ResetStatistics();
                currentStatistics = dataManager.GetSystemStatistics();
            }
        }

        if (GUILayout.Button("重置刷新统计", GUILayout.Width(100)))
        {
            if (refreshSystem != null)
            {
                refreshSystem.ResetStatistics();
                refreshStatistics = null;
            }
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("性能报告", GUILayout.Width(80)))
        {
            if (dataManager != null)
            {
                var report = dataManager.GetPerformanceReport();
                Debug.Log($"网格系统性能报告:\n{report}");
                EditorUtility.DisplayDialog("性能报告", "性能报告已输出到控制台", "确定");
            }
        }

        if (GUILayout.Button("导出统计", GUILayout.Width(80)))
        {
            ExportStatistics();
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 导出统计信息到文件
    /// </summary>
    private void ExportStatistics()
    {
        try
        {
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string fileName = $"GridSystemStatistics_{timestamp}.txt";
            string filePath = EditorUtility.SaveFilePanel("导出统计信息", "", fileName, "txt");

            if (!string.IsNullOrEmpty(filePath))
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("=== 网格系统统计报告 ===");
                sb.AppendLine($"导出时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine();

                // 基础统计
                sb.AppendLine("--- 基础统计 ---");
                sb.AppendLine($"总网格数: {totalGrids}");
                sb.AppendLine($"活跃网格: {activeGrids}");
                sb.AppendLine($"总物品数: {totalItems}");
                sb.AppendLine($"平均占用率: {averageOccupancy:F1}%");
                sb.AppendLine();

                if (currentStatistics != null)
                {
                    sb.AppendLine("--- 数据管理器统计 ---");
                    sb.AppendLine($"总网格数: {currentStatistics.TotalGrids}");
                    sb.AppendLine($"活跃网格数: {currentStatistics.ActiveGrids}");
                    sb.AppendLine($"总物品数: {currentStatistics.TotalItems}");
                    var lastUpdateDateTime = new DateTime(1970, 1, 1).AddSeconds(currentStatistics.LastUpdateTime);
                    sb.AppendLine($"上次更新时间: {lastUpdateDateTime:yyyy-MM-dd HH:mm:ss}");
                    sb.AppendLine();
                }

                if (refreshStatistics != null)
                {
                    sb.AppendLine("--- 刷新系统统计 ---");
                    sb.AppendLine($"总刷新次数: {refreshStatistics.TotalRefreshCount}");
                    sb.AppendLine($"平均刷新时间: {refreshStatistics.AverageRefreshTime:F2} ms");
                    sb.AppendLine($"自动刷新启用: {(refreshStatistics.IsAutoRefreshEnabled ? "是" : "否")}");
                    sb.AppendLine($"刷新间隔: {refreshStatistics.RefreshInterval:F1} 秒");
                    sb.AppendLine($"当前刷新状态: {(refreshStatistics.IsCurrentlyRefreshing ? "刷新中" : "空闲")}");
                    sb.AppendLine();
                }

                if (dataManager != null)
                {
                    sb.AppendLine("--- 性能报告 ---");
                    sb.AppendLine(dataManager.GetPerformanceReport());
                }

                System.IO.File.WriteAllText(filePath, sb.ToString());
                EditorUtility.DisplayDialog("导出成功", $"统计信息已导出到:\n{filePath}", "确定");
            }
        }
        catch (System.Exception ex)
        {
            EditorUtility.DisplayDialog("导出失败", $"导出统计信息时发生错误:\n{ex.Message}", "确定");
        }
    }

    /// <summary>
    /// 绘制刷新设置面板
    /// </summary>
    private void DrawRefreshSettingsPanel()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("刷新系统设置", headerStyle);

        // 自动刷新设置
        EditorGUILayout.LabelField("自动刷新配置", subHeaderStyle);

        bool autoRefreshEnabled = refreshSystem.IsAutoRefreshEnabled;
        bool newAutoRefreshEnabled = EditorGUILayout.Toggle("启用自动刷新", autoRefreshEnabled);
        if (newAutoRefreshEnabled != autoRefreshEnabled)
        {
            if (newAutoRefreshEnabled)
                refreshSystem.EnableAutoRefresh();
            else
                refreshSystem.DisableAutoRefresh();
        }

        if (refreshSystem.IsAutoRefreshEnabled)
        {
            float currentInterval = refreshStatistics != null ? refreshStatistics.RefreshInterval : 1.0f;
            float newInterval = EditorGUILayout.Slider("刷新间隔(秒)", currentInterval, 0.5f, 10f);
            if (Math.Abs(newInterval - currentInterval) > 0.01f)
            {
                // 注意：实际的刷新间隔设置需要通过其他方式实现
                EditorGUILayout.HelpBox("刷新间隔设置功能需要进一步实现", MessageType.Info);
            }
        }

        EditorGUILayout.Space();

        // 刷新优先级设置（暂时移除，因为RefreshPriority类型未定义）
        // EditorGUILayout.LabelField("刷新优先级", subHeaderStyle);

        EditorGUILayout.Space();

        // 批处理设置（暂时移除，因为相关属性和方法未定义）
        // EditorGUILayout.LabelField("批处理配置", subHeaderStyle);

        EditorGUILayout.Space();

        // 刷新统计信息
        if (refreshStatistics != null)
        {
            EditorGUILayout.LabelField("刷新统计", subHeaderStyle);
            EditorGUILayout.LabelField($"总刷新次数: {refreshStatistics.TotalRefreshCount}", infoStyle);
            EditorGUILayout.LabelField($"平均刷新时间: {refreshStatistics.AverageRefreshTime:F2}ms", infoStyle);
            EditorGUILayout.LabelField($"自动刷新启用: {(refreshStatistics.IsAutoRefreshEnabled ? "是" : "否")}", infoStyle);
            EditorGUILayout.LabelField($"当前刷新状态: {(refreshStatistics.IsCurrentlyRefreshing ? "刷新中" : "空闲")}", infoStyle);
        }

        EditorGUILayout.Space();

        // 控制按钮
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("立即刷新所有", GUILayout.Width(100)))
        {
            refreshSystem.RefreshAllGrids();
        }

        if (GUILayout.Button("清除队列", GUILayout.Width(80)))
        {
            // 注意：ClearRefreshQueue方法不可访问，需要其他方式实现
            EditorGUILayout.HelpBox("清除队列功能需要进一步实现", MessageType.Info);
        }

        if (GUILayout.Button("重置统计", GUILayout.Width(80)))
        {
            refreshSystem.ResetStatistics();
            refreshStatistics = null;
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 绘制保存系统监控面板
    /// </summary>
    private void DrawSaveSystemMonitorPanel()
    {
        saveSystemScrollPosition = EditorGUILayout.BeginScrollView(saveSystemScrollPosition, GUILayout.Height(300));

        // 集成保存系统监控显示组件
        SaveSystemMonitorDisplay.DrawSaveSystemMonitor();

        EditorGUILayout.EndScrollView();
    }

    /// <summary>
    /// 绘制跨场景ID同步面板
    /// </summary>
    private void DrawCrossSceneIDSyncPanel()
    {
        EditorGUILayout.LabelField("跨场景ID同步监控", headerStyle);

        crossSceneScrollPosition = EditorGUILayout.BeginScrollView(crossSceneScrollPosition, GUILayout.Height(450));

        // 调用跨场景ID同步显示组件
        CrossSceneIDSyncDisplay.DrawCrossSceneIDSync();

        EditorGUILayout.EndScrollView();
    }

    /// <summary>
    /// 绘制主内容区域
    /// </summary>
    private void DrawMainContent()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        if (cachedGrids.Count == 0)
        {
            EditorGUILayout.HelpBox("场景中未找到任何网格系统", MessageType.Info);
        }
        else
        {
            foreach (BaseItemGrid grid in cachedGrids)
            {
                if (displayComponent != null && cachedDetectorInfo.ContainsKey(grid))
                {
                    displayComponent.DrawGridMonitorInfo(grid, cachedDetectorInfo[grid], settings);
                }
            }
        }

        EditorGUILayout.EndScrollView();
    }

    /// <summary>
    /// 窗口禁用时清理
    /// </summary>
    private void OnDisable()
    {
        // 注销数据管理器事件
        if (dataManager != null)
        {
            dataManager.OnGridDataUpdated -= OnGridDataUpdated;
            dataManager.OnAllGridsRefreshed -= OnAllGridsRefreshed;
            dataManager.StopMonitoring();
        }

        // 注销刷新系统事件
        if (refreshSystem != null)
        {
            refreshSystem.OnRefreshStarted -= OnRefreshStarted;
            refreshSystem.OnRefreshCompleted -= OnRefreshCompleted;
            refreshSystem.OnGridRefreshed -= OnGridRefreshed;
            refreshSystem.OnRefreshError -= OnRefreshError;
        }
    }

    /// <summary>
    /// 数据管理器网格数据更新事件处理
    /// </summary>
    private void OnGridDataUpdated(BaseItemGrid grid, InventorySystem.Grid.GridDetectorInfo info)
    {
        if (cachedDetectorInfo.ContainsKey(grid))
        {
            cachedDetectorInfo[grid] = info;
            UpdateStatistics();
            Repaint();
        }
    }

    /// <summary>
    /// 数据管理器所有网格刷新事件处理
    /// </summary>
    private void OnAllGridsRefreshed()
    {
        RefreshGridData();
        Repaint();
    }

    /// <summary>
    /// 刷新系统开始刷新事件处理
    /// </summary>
    private void OnRefreshStarted()
    {
        // 可以在这里显示刷新状态
        Repaint();
    }

    /// <summary>
    /// 刷新系统完成刷新事件处理
    /// </summary>
    private void OnRefreshCompleted()
    {
        refreshStatistics = refreshSystem.GetRefreshStatistics();
        Repaint();
    }

    /// <summary>
    /// 刷新系统单个网格刷新事件处理
    /// </summary>
    private void OnGridRefreshed(BaseItemGrid grid)
    {
        // 更新单个网格的缓存数据
        if (grid != null)
        {
            var info = dataManager.GetGridData(grid);
            if (info != null)
            {
                cachedDetectorInfo[grid] = info;
                if (!cachedGrids.Contains(grid))
                {
                    cachedGrids.Add(grid);
                }
            }
        }
        Repaint();
    }

    /// <summary>
    /// 刷新系统错误事件处理
    /// </summary>
    private void OnRefreshError(string error)
    {
        Debug.LogError($"网格监控刷新错误: {error}");
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