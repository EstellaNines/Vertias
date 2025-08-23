using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System;
using GridSystem.Editor;

/// <summary>
/// 网格监控数据管理器 - 处理Unity Editor窗口中网格数据的实时同步和更新
/// </summary>
public class GridMonitorDataManager
{
    // 单例实例
    private static GridMonitorDataManager instance;
    public static GridMonitorDataManager Instance
    {
        get
        {
            if (instance == null)
                instance = new GridMonitorDataManager();
            return instance;
        }
    }

    // 数据缓存
    private Dictionary<BaseItemGrid, InventorySystem.Grid.GridDetectorInfo> gridDataCache = new Dictionary<BaseItemGrid, InventorySystem.Grid.GridDetectorInfo>();
    private Dictionary<BaseItemGrid, InventorySystem.Grid.GridDetectorInfo> gridInfoCache = new Dictionary<BaseItemGrid, InventorySystem.Grid.GridDetectorInfo>();
    private Dictionary<BaseItemGrid, double> lastUpdateTimes = new Dictionary<BaseItemGrid, double>();
    private Dictionary<BaseItemGrid, float> customUpdateIntervals = new Dictionary<BaseItemGrid, float>();

    // 更新设置
    private float cacheValidityDuration = 5f; // 缓存有效期（秒）
    private bool enableAutoUpdate = true;
    private float autoUpdateInterval = 2f;

    // 事件系统
    public event Action<BaseItemGrid, InventorySystem.Grid.GridDetectorInfo> OnGridDataUpdated;
    public event Action<BaseItemGrid> OnGridAdded;
    public event Action<BaseItemGrid> OnGridRemoved;
    public event Action OnAllGridsRefreshed;

    // 监控状态
    private bool isMonitoring = false;
    private List<BaseItemGrid> registeredGrids = new List<BaseItemGrid>();
    private List<BaseItemGrid> monitoredGrids = new List<BaseItemGrid>();
    private double lastFullRefresh;

    // 性能统计
    private int totalRefreshCount = 0;
    private float totalRefreshTime = 0f;
    private Dictionary<string, int> gridTypeUpdateCounts = new Dictionary<string, int>();

    /// <summary>
    /// 私有构造函数
    /// </summary>
    private GridMonitorDataManager()
    {
        // 注册Editor更新回调
        EditorApplication.update += OnEditorUpdate;

        // 注册场景变化回调
        EditorApplication.hierarchyChanged += OnHierarchyChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

        // 初始化监控
        StartMonitoring();
        lastFullRefresh = EditorApplication.timeSinceStartup;
    }

    /// <summary>
    /// 析构函数 - 清理资源
    /// </summary>
    ~GridMonitorDataManager()
    {
        StopMonitoring();

        // 取消注册Unity编辑器事件
        EditorApplication.update -= OnEditorUpdate;
        EditorApplication.hierarchyChanged -= OnHierarchyChanged;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
    }

    /// <summary>
    /// 开始监控网格系统
    /// </summary>
    public void StartMonitoring()
    {
        if (isMonitoring) return;

        isMonitoring = true;
        RefreshAllGrids();

        Debug.Log("网格监控系统已启动");
    }

    /// <summary>
    /// 停止监控网格系统
    /// </summary>
    public void StopMonitoring()
    {
        if (!isMonitoring) return;

        isMonitoring = false;
        gridDataCache.Clear();
        lastUpdateTimes.Clear();
        registeredGrids.Clear();

        Debug.Log("网格监控系统已停止");
    }

    /// <summary>
    /// 设置缓存有效期
    /// </summary>
    public void SetCacheValidityDuration(float duration)
    {
        cacheValidityDuration = Mathf.Max(0.1f, duration);
    }

    /// <summary>
    /// 设置自动更新间隔
    /// </summary>
    public void SetAutoUpdateInterval(float interval)
    {
        autoUpdateInterval = Mathf.Max(0.1f, interval);
    }

    /// <summary>
    /// 启用或禁用自动更新
    /// </summary>
    public void SetAutoUpdateEnabled(bool enabled)
    {
        enableAutoUpdate = enabled;
    }

    /// <summary>
    /// 获取所有网格数据
    /// </summary>
    public Dictionary<BaseItemGrid, InventorySystem.Grid.GridDetectorInfo> GetAllGridData()
    {
        RefreshExpiredData();
        return new Dictionary<BaseItemGrid, InventorySystem.Grid.GridDetectorInfo>(gridDataCache);
    }

    /// <summary>
    /// 获取指定网格的数据
    /// </summary>
    public InventorySystem.Grid.GridDetectorInfo GetGridData(BaseItemGrid grid)
    {
        if (grid == null) return null;

        if (IsDataExpired(grid))
        {
            UpdateGridData(grid);
        }

        return gridDataCache.ContainsKey(grid) ? gridDataCache[grid] : null;
    }

    /// <summary>
    /// 获取所有监控的网格
    /// </summary>
    public List<BaseItemGrid> GetMonitoredGrids()
    {
        return new List<BaseItemGrid>(registeredGrids);
    }

    /// <summary>
    /// 检查数据是否过期
    /// </summary>
    private bool IsDataExpired(BaseItemGrid grid)
    {
        if (!lastUpdateTimes.ContainsKey(grid)) return true;

        double timeSinceUpdate = EditorApplication.timeSinceStartup - lastUpdateTimes[grid];
        return timeSinceUpdate > cacheValidityDuration;
    }

    /// <summary>
    /// 刷新过期数据
    /// </summary>
    private void RefreshExpiredData()
    {
        var expiredGrids = new List<BaseItemGrid>();

        foreach (var grid in registeredGrids)
        {
            if (IsDataExpired(grid))
            {
                expiredGrids.Add(grid);
            }
        }

        foreach (var grid in expiredGrids)
        {
            UpdateGridData(grid);
        }
    }

    /// <summary>
    /// 更新网格数据
    /// </summary>
    private void UpdateGridData(BaseItemGrid grid)
    {
        if (grid == null) return;

        try
        {
            var gridInfo = grid.GetGridDetectorInfo();
            gridDataCache[grid] = gridInfo;
            lastUpdateTimes[grid] = EditorApplication.timeSinceStartup;

            // 更新统计信息
            string gridType = grid.GetType().Name;
            if (!gridTypeUpdateCounts.ContainsKey(gridType))
            {
                gridTypeUpdateCounts[gridType] = 0;
            }
            gridTypeUpdateCounts[gridType]++;

            OnGridDataUpdated?.Invoke(grid, gridInfo);
        }
        catch (Exception e)
        {
            Debug.LogError($"更新网格 {grid.name} 数据时出错: {e.Message}");
        }
    }

    /// <summary>
    /// 强制刷新特定网格
    /// </summary>
    public void ForceRefreshGrid(BaseItemGrid grid)
    {
        if (grid != null)
        {
            UpdateGridInfo(grid, true);
        }
    }

    /// <summary>
    /// 刷新所有网格
    /// </summary>
    public void RefreshAllGrids()
    {
        // 查找场景中所有的网格
        BaseItemGrid[] allGrids = UnityEngine.Object.FindObjectsOfType<BaseItemGrid>();

        // 更新监控列表
        UpdateMonitoredGridsList(allGrids);

        // 刷新所有网格信息
        foreach (BaseItemGrid grid in monitoredGrids)
        {
            UpdateGridInfo(grid, true);
        }

        OnAllGridsRefreshed?.Invoke();
    }

    /// <summary>
    /// 更新监控网格列表
    /// </summary>
    private void UpdateMonitoredGridsList(BaseItemGrid[] currentGrids)
    {
        // 检查新增的网格
        foreach (BaseItemGrid grid in currentGrids)
        {
            if (!monitoredGrids.Contains(grid))
            {
                monitoredGrids.Add(grid);
                OnGridAdded?.Invoke(grid);
            }
        }

        // 检查移除的网格
        List<BaseItemGrid> gridsToRemove = new List<BaseItemGrid>();
        foreach (BaseItemGrid grid in monitoredGrids)
        {
            if (grid == null || !currentGrids.Contains(grid))
            {
                gridsToRemove.Add(grid);
            }
        }

        foreach (BaseItemGrid grid in gridsToRemove)
        {
            monitoredGrids.Remove(grid);
            if (gridInfoCache.ContainsKey(grid))
            {
                gridInfoCache.Remove(grid);
            }
            if (lastUpdateTimes.ContainsKey(grid))
            {
                lastUpdateTimes.Remove(grid);
            }
            if (customUpdateIntervals.ContainsKey(grid))
            {
                customUpdateIntervals.Remove(grid);
            }
            OnGridRemoved?.Invoke(grid);
        }
    }

    /// <summary>
    /// 更新网格信息
    /// </summary>
    private void UpdateGridInfo(BaseItemGrid grid, bool forceUpdate = false)
    {
        if (grid == null) return;

        double currentTime = EditorApplication.timeSinceStartup;
        float updateInterval = GetGridUpdateInterval(grid);

        // 检查是否需要更新
        if (!forceUpdate && lastUpdateTimes.ContainsKey(grid))
        {
            double timeSinceLastUpdate = currentTime - lastUpdateTimes[grid];
            if (timeSinceLastUpdate < updateInterval)
            {
                return;
            }
        }

        try
        {
            // 获取最新的网格信息
            InventorySystem.Grid.GridDetectorInfo newInfo = grid.GetGridDetectorInfo();

            // 检查信息是否有变化
            bool hasChanged = !gridInfoCache.ContainsKey(grid) || HasGridInfoChanged(gridInfoCache[grid], newInfo);

            if (hasChanged || forceUpdate)
            {
                gridInfoCache[grid] = newInfo;
                lastUpdateTimes[grid] = currentTime;
                OnGridDataUpdated?.Invoke(grid, newInfo);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"更新网格 {grid.name} 的信息时出错: {e.Message}");
        }
    }

    /// <summary>
    /// 检查网格信息是否有变化
    /// </summary>
    private bool HasGridInfoChanged(InventorySystem.Grid.GridDetectorInfo oldInfo, InventorySystem.Grid.GridDetectorInfo newInfo)
    {
        if (oldInfo == null || newInfo == null) return true;

        // 检查基本信息
        if (oldInfo.placedItemsCount != newInfo.placedItemsCount ||
            oldInfo.occupiedCellsCount != newInfo.occupiedCellsCount ||
            Math.Abs(oldInfo.occupancyRate - newInfo.occupancyRate) > 0.001f)
        {
            return true;
        }

        // 检查物品分布
        if (oldInfo.itemDistribution.Count != newInfo.itemDistribution.Count)
        {
            return true;
        }

        foreach (var kvp in oldInfo.itemDistribution)
        {
            if (!newInfo.itemDistribution.ContainsKey(kvp.Key) ||
                newInfo.itemDistribution[kvp.Key] != kvp.Value)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Editor更新回调
    /// </summary>
    private void OnEditorUpdate()
    {
        if (!isMonitoring || !enableAutoUpdate) return;

        double timeSinceLastRefresh = EditorApplication.timeSinceStartup - lastFullRefresh;
        if (timeSinceLastRefresh >= autoUpdateInterval)
        {
            RefreshExpiredData();
            lastFullRefresh = EditorApplication.timeSinceStartup;
        }
    }

    /// <summary>
    /// 层级变化回调
    /// </summary>
    private void OnHierarchyChanged()
    {
        if (isMonitoring)
        {
            // 延迟刷新，避免频繁更新
            EditorApplication.delayCall += () =>
            {
                if (isMonitoring)
                {
                    RefreshAllGrids();
                }
            };
        }
    }

    /// <summary>
    /// 播放模式状态变化回调
    /// </summary>
    private void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        switch (state)
        {
            case PlayModeStateChange.EnteredPlayMode:
                // 进入播放模式时刷新所有数据
                RefreshAllGrids();
                break;
            case PlayModeStateChange.ExitingPlayMode:
                // 退出播放模式时清理缓存
                gridDataCache.Clear();
                lastUpdateTimes.Clear();
                break;
        }
    }

    /// <summary>
    /// 获取网格统计信息
    /// </summary>
    public GridSystem.Editor.GridSystemStatistics GetSystemStatistics()
    {
        return new GridSystem.Editor.GridSystemStatistics
        {
            TotalGrids = registeredGrids.Count,
            ActiveGrids = gridDataCache.Count(kvp => kvp.Value.placedItemsCount > 0),
            TotalItems = gridDataCache.Values.Sum(info => info.placedItemsCount),
            AverageOccupancy = gridDataCache.Values.Any() ?
                gridDataCache.Values.Average(info => info.occupancyRate * 100) : 0f,
            LastUpdateTime = lastFullRefresh,
            TotalRefreshCount = totalRefreshCount,
            AverageRefreshTime = totalRefreshCount > 0 ? totalRefreshTime / totalRefreshCount : 0f,
            GridTypeUpdateCounts = new Dictionary<string, int>(gridTypeUpdateCounts)
        };
    }

    /// <summary>
    /// 获取性能统计信息
    /// </summary>
    public string GetPerformanceReport()
    {
        var stats = GetSystemStatistics();
        return $"性能报告:\n" +
               $"- 总刷新次数: {stats.TotalRefreshCount}\n" +
               $"- 平均刷新时间: {stats.AverageRefreshTime:F3}秒\n" +
               $"- 缓存命中率: {CalculateCacheHitRate():F1}%\n" +
               $"- 内存使用: {GetMemoryUsage():F2}MB";
    }

    /// <summary>
    /// 计算缓存命中率
    /// </summary>
    private float CalculateCacheHitRate()
    {
        int totalRequests = gridTypeUpdateCounts.Values.Sum();
        if (totalRequests == 0) return 100f;

        int cacheHits = gridDataCache.Count;
        return (float)cacheHits / totalRequests * 100f;
    }

    /// <summary>
    /// 获取内存使用量（估算）
    /// </summary>
    private float GetMemoryUsage()
    {
        // 简单估算内存使用量
        int estimatedSize = gridDataCache.Count * 1024; // 每个网格数据约1KB
        return estimatedSize / (1024f * 1024f); // 转换为MB
    }

    /// <summary>
    /// 清理缓存
    /// </summary>
    public void ClearCache()
    {
        gridDataCache.Clear();
        lastUpdateTimes.Clear();
        Debug.Log("网格监控缓存已清理");
    }

    /// <summary>
    /// 重置统计信息
    /// </summary>
    public void ResetStatistics()
    {
        totalRefreshCount = 0;
        totalRefreshTime = 0f;
        gridTypeUpdateCounts.Clear();
        Debug.Log("网格监控统计信息已重置");
    }

    /// <summary>
    /// 检查网格是否需要更新
    /// </summary>
    private bool ShouldUpdateGrid(BaseItemGrid grid)
    {
        if (!lastUpdateTimes.ContainsKey(grid)) return true;

        double timeSinceUpdate = EditorApplication.timeSinceStartup - lastUpdateTimes[grid];
        return timeSinceUpdate >= cacheValidityDuration;
    }

    /// <summary>
    /// 更新网格信息
    /// </summary>
    private void UpdateGridInfo(BaseItemGrid grid)
    {
        if (grid == null) return;

        try
        {
            var startTime = EditorApplication.timeSinceStartup;
            InventorySystem.Grid.GridDetectorInfo info = grid.GetGridDetectorInfo();
            var endTime = EditorApplication.timeSinceStartup;

            // 验证数据完整性
            if (!ValidateGridData(grid, info))
            {
                return;
            }

            gridDataCache[grid] = info;
            lastUpdateTimes[grid] = endTime;

            // 更新统计信息
            string gridType = grid.GetType().Name;
            if (!gridTypeUpdateCounts.ContainsKey(gridType))
            {
                gridTypeUpdateCounts[gridType] = 0;
            }
            gridTypeUpdateCounts[gridType]++;

            // 更新性能统计
            totalRefreshTime += (float)(endTime - startTime);

            OnGridDataUpdated?.Invoke(grid, info);
        }
        catch (Exception e)
        {
            Debug.LogError($"更新网格 {grid.name} 信息时出错: {e.Message}");
        }
    }



    /// <summary>
    /// 获取网格更新间隔
    /// </summary>
    private float GetGridUpdateInterval(BaseItemGrid grid)
    {
        // 根据网格类型返回不同的更新间隔
        switch (grid)
        {
            case BackpackItemGrid _:
                return 0.5f; // 背包更新频率较高
            case TactiaclRigItemGrid _:
                return 1.0f; // 战术挂具中等频率
            case ItemGrid _:
                return 2.0f; // 仓库更新频率较低
            default:
                return 1.0f; // 默认间隔
        }
    }

    /// <summary>
    /// 验证网格数据完整性
    /// </summary>
    private bool ValidateGridData(BaseItemGrid grid, InventorySystem.Grid.GridDetectorInfo info)
    {
        if (grid == null || info == null) return false;

        // 检查基本数据完整性
        if (info.totalCells < 0 || info.occupiedCellsCount < 0 || info.placedItemsCount < 0)
        {
            Debug.LogWarning($"网格 {grid.name} 数据异常: 容量={info.totalCells}, 占用={info.occupiedCellsCount}, 物品数={info.placedItemsCount}");
            return false;
        }

        return true;
    }

    /// <summary>
    /// 清理资源
    /// </summary>
    public void Cleanup()
    {
        StopMonitoring();
        gridDataCache.Clear();
        lastUpdateTimes.Clear();
        registeredGrids.Clear();

        EditorApplication.update -= OnEditorUpdate;
        EditorApplication.hierarchyChanged -= OnHierarchyChanged;
    }
}

/// <summary>
/// 网格系统统计信息
/// </summary>
[Serializable]
public class GridSystemStatistics
{
    public int TotalGrids;
    public int ActiveGrids;
    public int TotalItems;
    public float AverageOccupancy;
    public double LastUpdateTime;
    public int TotalRefreshCount;
    public float AverageRefreshTime;
    public Dictionary<string, int> GridTypeUpdateCounts;
}

// GridMonitorSettings类定义已移至独立的GridMonitorSettings.cs文件中