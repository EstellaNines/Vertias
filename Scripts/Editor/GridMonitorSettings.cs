using UnityEngine;
using System;

/// <summary>
/// 导出格式枚举
/// </summary>
public enum ExportFormat
{
    JSON,
    CSV,
    XML
}

/// <summary>
/// 网格监控设置 - 配置监控窗口的显示选项和阈值参数
/// </summary>
[Serializable]
public class GridMonitorSettings
{
    [Header("显示设置")]
    [Tooltip("显示基础信息")]
    public bool ShowBasicInfo = true;

    [Tooltip("显示物品分布")]
    public bool ShowItemDistribution = true;

    [Tooltip("显示占用信息")]
    public bool ShowOccupancyInfo = true;

    [Tooltip("显示专项分析")]
    public bool ShowSpecificAnalysis = true;

    [Tooltip("显示性能指标")]
    public bool ShowPerformanceMetrics = false;

    [Tooltip("显示警告和建议")]
    public bool ShowWarningsAndSuggestions = true;

    [Header("刷新设置")]
    [Tooltip("启用自动刷新")]
    public bool EnableAutoRefresh = true;

    [Tooltip("自动刷新间隔（秒）")]
    [Range(0.5f, 10f)]
    public float AutoRefreshInterval = 2f;

    [Tooltip("仅在播放模式下自动刷新")]
    public bool RefreshOnlyInPlayMode = true;

    [Header("显示限制")]
    [Tooltip("最大显示物品数量")]
    [Range(5, 50)]
    public int MaxItemsToDisplay = 10;

    [Tooltip("最大显示区域数量")]
    [Range(3, 20)]
    public int MaxAreasToDisplay = 8;

    [Tooltip("最大显示网格数量")]
    [Range(5, 100)]
    public int MaxGridsToDisplay = 20;

    [Header("阈值设置")]
    [Tooltip("高占用率阈值（%）")]
    [Range(50f, 95f)]
    public float HighOccupancyThreshold = 80f;

    [Tooltip("低效率阈值（%）")]
    [Range(30f, 70f)]
    public float LowEfficiencyThreshold = 50f;

    [Tooltip("高碎片化阈值（%）")]
    [Range(20f, 80f)]
    public float HighFragmentationThreshold = 60f;

    [Tooltip("容量警告阈值（%）")]
    [Range(70f, 95f)]
    public float CapacityWarningThreshold = 85f;

    [Header("过滤设置")]
    [Tooltip("仅显示非空物品网格")]
    public bool ShowOnlyNonEmptyGrids = false;

    [Tooltip("仅显示有警告的网格")]
    public bool ShowOnlyGridsWithWarnings = false;

    [Tooltip("按占用率排序")]
    public bool SortByOccupancy = true;

    [Tooltip("按名称排序")]
    public bool SortByName = false;

    [Header("可视化设置")]
    [Tooltip("网格可视化缩放")]
    [Range(0.5f, 3f)]
    public float GridVisualizationScale = 1f;

    [Tooltip("显示网格线")]
    public bool ShowGridLines = true;

    [Tooltip("显示物品图标")]
    public bool ShowItemIcons = true;

    [Tooltip("显示热力图")]
    public bool ShowHeatmap = false;

    [Tooltip("热力图透明度")]
    [Range(0.1f, 1f)]
    public float HeatmapAlpha = 0.6f;

    [Header("颜色设置")]
    [Tooltip("空闲格子颜色")]
    public Color EmptySlotColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);

    [Tooltip("占用格子颜色")]
    public Color OccupiedSlotColor = new Color(0.2f, 0.6f, 0.2f, 0.8f);

    [Tooltip("警告颜色")]
    public Color WarningColor = Color.yellow;

    [Tooltip("错误颜色")]
    public Color ErrorColor = Color.red;

    [Tooltip("成功颜色")]
    public Color SuccessColor = Color.green;

    [Tooltip("网格线颜色")]
    public Color GridLineColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);

    [Header("高级设置")]
    [Tooltip("启用详细日志")]
    public bool EnableDetailedLogging = false;

    [Tooltip("启用性能监控")]
    public bool EnablePerformanceMonitoring = true;

    [Tooltip("缓存检测器数据")]
    public bool CacheDetectorData = true;

    [Tooltip("缓存有效期（秒）")]
    [Range(1f, 30f)]
    public float CacheValidityDuration = 5f;

    [Tooltip("启用多线程处理")]
    public bool EnableMultithreading = false;

    [Header("导出设置")]
    [Tooltip("启用数据导出")]
    public bool EnableDataExport = true;

    [Tooltip("导出格式")]
    public ExportFormat ExportFormat = ExportFormat.JSON;

    [Tooltip("包含时间戳")]
    public bool IncludeTimestamp = true;

    [Tooltip("包含详细信息")]
    public bool IncludeDetailedInfo = true;



    /// <summary>
    /// 构造函数 - 设置默认值
    /// </summary>
    public GridMonitorSettings()
    {
        // 默认值已在字段声明中设置
    }

    /// <summary>
    /// 重置为默认设置
    /// </summary>
    public void ResetToDefaults()
    {
        ShowBasicInfo = true;
        ShowItemDistribution = true;
        ShowOccupancyInfo = true;
        ShowSpecificAnalysis = true;
        ShowPerformanceMetrics = false;
        ShowWarningsAndSuggestions = true;

        EnableAutoRefresh = true;
        AutoRefreshInterval = 2f;
        RefreshOnlyInPlayMode = true;

        MaxItemsToDisplay = 10;
        MaxAreasToDisplay = 8;
        MaxGridsToDisplay = 20;

        HighOccupancyThreshold = 80f;
        LowEfficiencyThreshold = 50f;
        HighFragmentationThreshold = 60f;
        CapacityWarningThreshold = 85f;

        ShowOnlyNonEmptyGrids = false;
        ShowOnlyGridsWithWarnings = false;
        SortByOccupancy = true;
        SortByName = false;

        GridVisualizationScale = 1f;
        ShowGridLines = true;
        ShowItemIcons = true;
        ShowHeatmap = false;
        HeatmapAlpha = 0.6f;

        EmptySlotColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        OccupiedSlotColor = new Color(0.2f, 0.6f, 0.2f, 0.8f);
        WarningColor = Color.yellow;
        ErrorColor = Color.red;
        SuccessColor = Color.green;
        GridLineColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);

        EnableDetailedLogging = false;
        EnablePerformanceMonitoring = true;
        CacheDetectorData = true;
        CacheValidityDuration = 5f;
        EnableMultithreading = false;

        EnableDataExport = true;
        ExportFormat = ExportFormat.JSON;
        IncludeTimestamp = true;
        IncludeDetailedInfo = true;
    }

    /// <summary>
    /// 保存设置到EditorPrefs
    /// </summary>
    public void SaveToEditorPrefs()
    {
        UnityEditor.EditorPrefs.SetBool("GridMonitor_ShowBasicInfo", ShowBasicInfo);
        UnityEditor.EditorPrefs.SetBool("GridMonitor_ShowItemDistribution", ShowItemDistribution);
        UnityEditor.EditorPrefs.SetBool("GridMonitor_ShowOccupancyInfo", ShowOccupancyInfo);
        UnityEditor.EditorPrefs.SetBool("GridMonitor_ShowSpecificAnalysis", ShowSpecificAnalysis);
        UnityEditor.EditorPrefs.SetBool("GridMonitor_ShowPerformanceMetrics", ShowPerformanceMetrics);
        UnityEditor.EditorPrefs.SetBool("GridMonitor_ShowWarningsAndSuggestions", ShowWarningsAndSuggestions);
        
        UnityEditor.EditorPrefs.SetBool("GridMonitor_EnableAutoRefresh", EnableAutoRefresh);
        UnityEditor.EditorPrefs.SetFloat("GridMonitor_AutoRefreshInterval", AutoRefreshInterval);
        UnityEditor.EditorPrefs.SetBool("GridMonitor_RefreshOnlyInPlayMode", RefreshOnlyInPlayMode);
        
        UnityEditor.EditorPrefs.SetInt("GridMonitor_MaxItemsToDisplay", MaxItemsToDisplay);
        UnityEditor.EditorPrefs.SetInt("GridMonitor_MaxAreasToDisplay", MaxAreasToDisplay);
        UnityEditor.EditorPrefs.SetInt("GridMonitor_MaxGridsToDisplay", MaxGridsToDisplay);
        
        UnityEditor.EditorPrefs.SetFloat("GridMonitor_HighOccupancyThreshold", HighOccupancyThreshold);
        UnityEditor.EditorPrefs.SetFloat("GridMonitor_LowEfficiencyThreshold", LowEfficiencyThreshold);
        UnityEditor.EditorPrefs.SetFloat("GridMonitor_HighFragmentationThreshold", HighFragmentationThreshold);
        UnityEditor.EditorPrefs.SetFloat("GridMonitor_CapacityWarningThreshold", CapacityWarningThreshold);
        
        UnityEditor.EditorPrefs.SetBool("GridMonitor_ShowOnlyNonEmptyGrids", ShowOnlyNonEmptyGrids);
        UnityEditor.EditorPrefs.SetBool("GridMonitor_ShowOnlyGridsWithWarnings", ShowOnlyGridsWithWarnings);
        UnityEditor.EditorPrefs.SetBool("GridMonitor_SortByOccupancy", SortByOccupancy);
        UnityEditor.EditorPrefs.SetBool("GridMonitor_SortByName", SortByName);
        
        UnityEditor.EditorPrefs.SetFloat("GridMonitor_GridVisualizationScale", GridVisualizationScale);
        UnityEditor.EditorPrefs.SetBool("GridMonitor_ShowGridLines", ShowGridLines);
        UnityEditor.EditorPrefs.SetBool("GridMonitor_ShowItemIcons", ShowItemIcons);
        UnityEditor.EditorPrefs.SetBool("GridMonitor_ShowHeatmap", ShowHeatmap);
        UnityEditor.EditorPrefs.SetFloat("GridMonitor_HeatmapAlpha", HeatmapAlpha);
        
        UnityEditor.EditorPrefs.SetBool("GridMonitor_EnableDetailedLogging", EnableDetailedLogging);
        UnityEditor.EditorPrefs.SetBool("GridMonitor_EnablePerformanceMonitoring", EnablePerformanceMonitoring);
        UnityEditor.EditorPrefs.SetBool("GridMonitor_CacheDetectorData", CacheDetectorData);
        UnityEditor.EditorPrefs.SetFloat("GridMonitor_CacheValidityDuration", CacheValidityDuration);
        UnityEditor.EditorPrefs.SetBool("GridMonitor_EnableMultithreading", EnableMultithreading);
        
        UnityEditor.EditorPrefs.SetBool("GridMonitor_EnableDataExport", EnableDataExport);
        UnityEditor.EditorPrefs.SetInt("GridMonitor_ExportFormat", (int)ExportFormat);
        UnityEditor.EditorPrefs.SetBool("GridMonitor_IncludeTimestamp", IncludeTimestamp);
        UnityEditor.EditorPrefs.SetBool("GridMonitor_IncludeDetailedInfo", IncludeDetailedInfo);
    }

    /// <summary>
    /// 从EditorPrefs加载设置
    /// </summary>
    public void LoadFromEditorPrefs()
    {
        ShowBasicInfo = UnityEditor.EditorPrefs.GetBool("GridMonitor_ShowBasicInfo", true);
        ShowItemDistribution = UnityEditor.EditorPrefs.GetBool("GridMonitor_ShowItemDistribution", true);
        ShowOccupancyInfo = UnityEditor.EditorPrefs.GetBool("GridMonitor_ShowOccupancyInfo", true);
        ShowSpecificAnalysis = UnityEditor.EditorPrefs.GetBool("GridMonitor_ShowSpecificAnalysis", true);
        ShowPerformanceMetrics = UnityEditor.EditorPrefs.GetBool("GridMonitor_ShowPerformanceMetrics", false);
        ShowWarningsAndSuggestions = UnityEditor.EditorPrefs.GetBool("GridMonitor_ShowWarningsAndSuggestions", true);
        
        EnableAutoRefresh = UnityEditor.EditorPrefs.GetBool("GridMonitor_EnableAutoRefresh", true);
        AutoRefreshInterval = UnityEditor.EditorPrefs.GetFloat("GridMonitor_AutoRefreshInterval", 2f);
        RefreshOnlyInPlayMode = UnityEditor.EditorPrefs.GetBool("GridMonitor_RefreshOnlyInPlayMode", true);
        
        MaxItemsToDisplay = UnityEditor.EditorPrefs.GetInt("GridMonitor_MaxItemsToDisplay", 10);
        MaxAreasToDisplay = UnityEditor.EditorPrefs.GetInt("GridMonitor_MaxAreasToDisplay", 8);
        MaxGridsToDisplay = UnityEditor.EditorPrefs.GetInt("GridMonitor_MaxGridsToDisplay", 20);
        
        HighOccupancyThreshold = UnityEditor.EditorPrefs.GetFloat("GridMonitor_HighOccupancyThreshold", 80f);
        LowEfficiencyThreshold = UnityEditor.EditorPrefs.GetFloat("GridMonitor_LowEfficiencyThreshold", 50f);
        HighFragmentationThreshold = UnityEditor.EditorPrefs.GetFloat("GridMonitor_HighFragmentationThreshold", 60f);
        CapacityWarningThreshold = UnityEditor.EditorPrefs.GetFloat("GridMonitor_CapacityWarningThreshold", 85f);
        
        ShowOnlyNonEmptyGrids = UnityEditor.EditorPrefs.GetBool("GridMonitor_ShowOnlyNonEmptyGrids", false);
        ShowOnlyGridsWithWarnings = UnityEditor.EditorPrefs.GetBool("GridMonitor_ShowOnlyGridsWithWarnings", false);
        SortByOccupancy = UnityEditor.EditorPrefs.GetBool("GridMonitor_SortByOccupancy", true);
        SortByName = UnityEditor.EditorPrefs.GetBool("GridMonitor_SortByName", false);
        
        GridVisualizationScale = UnityEditor.EditorPrefs.GetFloat("GridMonitor_GridVisualizationScale", 1f);
        ShowGridLines = UnityEditor.EditorPrefs.GetBool("GridMonitor_ShowGridLines", true);
        ShowItemIcons = UnityEditor.EditorPrefs.GetBool("GridMonitor_ShowItemIcons", true);
        ShowHeatmap = UnityEditor.EditorPrefs.GetBool("GridMonitor_ShowHeatmap", false);
        HeatmapAlpha = UnityEditor.EditorPrefs.GetFloat("GridMonitor_HeatmapAlpha", 0.6f);
        
        EnableDetailedLogging = UnityEditor.EditorPrefs.GetBool("GridMonitor_EnableDetailedLogging", false);
        EnablePerformanceMonitoring = UnityEditor.EditorPrefs.GetBool("GridMonitor_EnablePerformanceMonitoring", true);
        CacheDetectorData = UnityEditor.EditorPrefs.GetBool("GridMonitor_CacheDetectorData", true);
        CacheValidityDuration = UnityEditor.EditorPrefs.GetFloat("GridMonitor_CacheValidityDuration", 5f);
        EnableMultithreading = UnityEditor.EditorPrefs.GetBool("GridMonitor_EnableMultithreading", false);
        
        EnableDataExport = UnityEditor.EditorPrefs.GetBool("GridMonitor_EnableDataExport", true);
        ExportFormat = (ExportFormat)UnityEditor.EditorPrefs.GetInt("GridMonitor_ExportFormat", 0);
        IncludeTimestamp = UnityEditor.EditorPrefs.GetBool("GridMonitor_IncludeTimestamp", true);
        IncludeDetailedInfo = UnityEditor.EditorPrefs.GetBool("GridMonitor_IncludeDetailedInfo", true);
    }

    /// <summary>
    /// 验证设置的有效性
    /// </summary>
    public bool ValidateSettings()
    {
        bool isValid = true;

        // 检查阈值范围
        if (HighOccupancyThreshold < 50f || HighOccupancyThreshold > 95f)
        {
            Debug.LogWarning("GridMonitorSettings: 高占用率阈值超出有效范围 (50-95)");
            isValid = false;
        }

        if (LowEfficiencyThreshold < 30f || LowEfficiencyThreshold > 70f)
        {
            Debug.LogWarning("GridMonitorSettings: 低效率阈值超出有效范围 (30-70)");
            isValid = false;
        }

        if (AutoRefreshInterval < 0.5f || AutoRefreshInterval > 10f)
        {
            Debug.LogWarning("GridMonitorSettings: 自动刷新间隔超出有效范围 (0.5-10)");
            isValid = false;
        }

        return isValid;
    }

    /// <summary>
    /// 获取设置摘要
    /// </summary>
    public string GetSettingsSummary()
    {
        return $"网格监控设置摘要:\n" +
               $"- 自动刷新: {(EnableAutoRefresh ? $"启用 ({AutoRefreshInterval}s)" : "禁用")}\n" +
               $"- 显示限制: 物品{MaxItemsToDisplay}个, 网格{MaxGridsToDisplay}个\n" +
               $"- 占用率阈值: {HighOccupancyThreshold}%\n" +
               $"- 效率阈值: {LowEfficiencyThreshold}%\n" +
               $"- 数据缓存: {(CacheDetectorData ? $"启用 ({CacheValidityDuration}s)" : "禁用")}";
    }
}