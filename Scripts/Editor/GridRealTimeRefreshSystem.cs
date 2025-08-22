using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

/// <summary>
/// 网格实时刷新系统
/// 负责管理Editor窗口中网格数据的实时更新和刷新机制
/// </summary>
public class GridRealTimeRefreshSystem
{
    // 单例实例
    private static GridRealTimeRefreshSystem instance;
    public static GridRealTimeRefreshSystem Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new GridRealTimeRefreshSystem();
            }
            return instance;
        }
    }

    // 刷新设置
    private bool isAutoRefreshEnabled = true;
    private float autoRefreshInterval = 1.0f;
    private bool refreshOnlyInPlayMode = false;
    private bool refreshOnHierarchyChange = true;
    private bool refreshOnSelectionChange = true;

    // 刷新状态
    private double lastRefreshTime;
    private bool isRefreshing = false;
    private int refreshCount = 0;
    private float totalRefreshTime = 0f;

    // 事件系统
    public event Action OnRefreshStarted;
    public event Action OnRefreshCompleted;
    public event Action<BaseItemGrid> OnGridRefreshed;
    public event Action<string> OnRefreshError;

    // 刷新队列
    private Queue<BaseItemGrid> refreshQueue = new Queue<BaseItemGrid>();
    private HashSet<BaseItemGrid> pendingRefresh = new HashSet<BaseItemGrid>();

    // 性能监控
    private Dictionary<string, float> refreshTimes = new Dictionary<string, float>();
    private Dictionary<string, int> refreshCounts = new Dictionary<string, int>();

    /// <summary>
    /// 构造函数
    /// </summary>
    private GridRealTimeRefreshSystem()
    {
        Initialize();
    }

    /// <summary>
    /// 初始化刷新系统
    /// </summary>
    private void Initialize()
    {
        // 注册Editor事件
        EditorApplication.update += OnEditorUpdate;
        EditorApplication.hierarchyChanged += OnHierarchyChanged;
        Selection.selectionChanged += OnSelectionChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

        // 加载设置
        LoadSettings();

        lastRefreshTime = EditorApplication.timeSinceStartup;

        Debug.Log("网格实时刷新系统已初始化");
    }

    /// <summary>
    /// 清理资源
    /// </summary>
    public void Cleanup()
    {
        // 注销事件
        EditorApplication.update -= OnEditorUpdate;
        EditorApplication.hierarchyChanged -= OnHierarchyChanged;
        Selection.selectionChanged -= OnSelectionChanged;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;

        // 保存设置
        SaveSettings();

        // 清理队列
        refreshQueue.Clear();
        pendingRefresh.Clear();

        Debug.Log("网格实时刷新系统已清理");
    }

    /// <summary>
    /// 启用自动刷新
    /// </summary>
    public void EnableAutoRefresh(float interval = 1.0f)
    {
        isAutoRefreshEnabled = true;
        autoRefreshInterval = Mathf.Max(0.1f, interval);
        Debug.Log($"自动刷新已启用，间隔: {autoRefreshInterval}秒");
    }

    /// <summary>
    /// 禁用自动刷新
    /// </summary>
    public void DisableAutoRefresh()
    {
        isAutoRefreshEnabled = false;
        Debug.Log("自动刷新已禁用");
    }

    /// <summary>
    /// 设置刷新间隔
    /// </summary>
    public void SetRefreshInterval(float interval)
    {
        autoRefreshInterval = Mathf.Max(0.1f, interval);
        Debug.Log($"刷新间隔已设置为: {autoRefreshInterval}秒");
    }

    /// <summary>
    /// 设置仅在播放模式下刷新
    /// </summary>
    public void SetRefreshOnlyInPlayMode(bool enabled)
    {
        refreshOnlyInPlayMode = enabled;
        Debug.Log($"仅播放模式刷新: {(enabled ? "启用" : "禁用")}");
    }

    /// <summary>
    /// 手动刷新所有网格
    /// </summary>
    public void RefreshAllGrids()
    {
        if (isRefreshing)
        {
            Debug.LogWarning("刷新正在进行中，请稍后再试");
            return;
        }

        StartRefresh(() =>
        {
            BaseItemGrid[] allGrids = UnityEngine.Object.FindObjectsOfType<BaseItemGrid>();
            foreach (BaseItemGrid grid in allGrids)
            {
                QueueGridForRefresh(grid);
            }
        });
    }

    /// <summary>
    /// 刷新指定网格
    /// </summary>
    public void RefreshGrid(BaseItemGrid grid)
    {
        if (grid == null) return;

        QueueGridForRefresh(grid);

        if (!isRefreshing)
        {
            StartRefresh();
        }
    }

    /// <summary>
    /// 刷新选中的网格
    /// </summary>
    public void RefreshSelectedGrids()
    {
        GameObject[] selectedObjects = Selection.gameObjects;
        bool hasGrids = false;

        foreach (GameObject obj in selectedObjects)
        {
            BaseItemGrid grid = obj.GetComponent<BaseItemGrid>();
            if (grid != null)
            {
                QueueGridForRefresh(grid);
                hasGrids = true;
            }
        }

        if (hasGrids && !isRefreshing)
        {
            StartRefresh();
        }
    }

    /// <summary>
    /// 获取刷新统计信息
    /// </summary>
    public RefreshStatistics GetRefreshStatistics()
    {
        return new RefreshStatistics
        {
            TotalRefreshCount = refreshCount,
            AverageRefreshTime = refreshCount > 0 ? totalRefreshTime / refreshCount : 0f,
            IsAutoRefreshEnabled = isAutoRefreshEnabled,
            RefreshInterval = autoRefreshInterval,
            QueuedGridsCount = refreshQueue.Count,
            IsCurrentlyRefreshing = isRefreshing,
            RefreshTimesByType = new Dictionary<string, float>(refreshTimes),
            RefreshCountsByType = new Dictionary<string, int>(refreshCounts)
        };
    }

    /// <summary>
    /// 重置统计信息
    /// </summary>
    public void ResetStatistics()
    {
        refreshCount = 0;
        totalRefreshTime = 0f;
        refreshTimes.Clear();
        refreshCounts.Clear();
        Debug.Log("刷新统计信息已重置");
    }

    /// <summary>
    /// Editor更新回调
    /// </summary>
    private void OnEditorUpdate()
    {
        // 检查是否需要自动刷新
        if (isAutoRefreshEnabled && ShouldAutoRefresh())
        {
            double currentTime = EditorApplication.timeSinceStartup;
            if (currentTime - lastRefreshTime >= autoRefreshInterval)
            {
                RefreshAllGrids();
                lastRefreshTime = currentTime;
            }
        }

        // 处理刷新队列
        ProcessRefreshQueue();
    }

    /// <summary>
    /// 层级变化回调
    /// </summary>
    private void OnHierarchyChanged()
    {
        if (refreshOnHierarchyChange && !isRefreshing)
        {
            // 延迟刷新，避免频繁更新
            EditorApplication.delayCall += () =>
            {
                RefreshAllGrids();
            };
        }
    }

    /// <summary>
    /// 选择变化回调
    /// </summary>
    private void OnSelectionChanged()
    {
        if (refreshOnSelectionChange)
        {
            RefreshSelectedGrids();
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
                Debug.Log("进入播放模式，刷新所有网格");
                RefreshAllGrids();
                break;
            case PlayModeStateChange.ExitingPlayMode:
                Debug.Log("退出播放模式，清理刷新队列");
                ClearRefreshQueue();
                break;
        }
    }

    /// <summary>
    /// 检查是否应该自动刷新
    /// </summary>
    private bool ShouldAutoRefresh()
    {
        if (refreshOnlyInPlayMode && !EditorApplication.isPlaying)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// 将网格加入刷新队列
    /// </summary>
    private void QueueGridForRefresh(BaseItemGrid grid)
    {
        if (grid == null || pendingRefresh.Contains(grid)) return;

        refreshQueue.Enqueue(grid);
        pendingRefresh.Add(grid);
    }

    /// <summary>
    /// 开始刷新
    /// </summary>
    private void StartRefresh(Action additionalAction = null)
    {
        if (isRefreshing) return;

        isRefreshing = true;
        OnRefreshStarted?.Invoke();

        additionalAction?.Invoke();
    }

    /// <summary>
    /// 处理刷新队列
    /// </summary>
    private void ProcessRefreshQueue()
    {
        if (!isRefreshing || refreshQueue.Count == 0) return;

        // 每帧处理一定数量的网格，避免卡顿
        int processCount = Mathf.Min(3, refreshQueue.Count);

        for (int i = 0; i < processCount; i++)
        {
            if (refreshQueue.Count == 0) break;

            BaseItemGrid grid = refreshQueue.Dequeue();
            pendingRefresh.Remove(grid);

            if (grid != null)
            {
                RefreshSingleGrid(grid);
            }
        }

        // 检查是否完成刷新
        if (refreshQueue.Count == 0)
        {
            CompleteRefresh();
        }
    }

    /// <summary>
    /// 刷新单个网格
    /// </summary>
    private void RefreshSingleGrid(BaseItemGrid grid)
    {
        try
        {
            float startTime = Time.realtimeSinceStartup;

            // 通知数据管理器更新网格
            GridMonitorDataManager.Instance.ForceRefreshGrid(grid);

            float endTime = Time.realtimeSinceStartup;
            float refreshTime = endTime - startTime;

            // 更新统计信息
            UpdateRefreshStatistics(grid, refreshTime);

            OnGridRefreshed?.Invoke(grid);
        }
        catch (Exception e)
        {
            string errorMessage = $"刷新网格 {grid.name} 时出错: {e.Message}";
            Debug.LogError(errorMessage);
            OnRefreshError?.Invoke(errorMessage);
        }
    }

    /// <summary>
    /// 完成刷新
    /// </summary>
    private void CompleteRefresh()
    {
        isRefreshing = false;
        refreshCount++;

        OnRefreshCompleted?.Invoke();
    }

    /// <summary>
    /// 清理刷新队列
    /// </summary>
    private void ClearRefreshQueue()
    {
        refreshQueue.Clear();
        pendingRefresh.Clear();
        isRefreshing = false;
    }

    /// <summary>
    /// 更新刷新统计信息
    /// </summary>
    private void UpdateRefreshStatistics(BaseItemGrid grid, float refreshTime)
    {
        string gridType = grid.GetType().Name;

        totalRefreshTime += refreshTime;

        if (!refreshTimes.ContainsKey(gridType))
        {
            refreshTimes[gridType] = 0f;
            refreshCounts[gridType] = 0;
        }

        refreshTimes[gridType] += refreshTime;
        refreshCounts[gridType]++;
    }

    /// <summary>
    /// 加载设置
    /// </summary>
    private void LoadSettings()
    {
        isAutoRefreshEnabled = EditorPrefs.GetBool("GridRefresh_AutoEnabled", true);
        autoRefreshInterval = EditorPrefs.GetFloat("GridRefresh_Interval", 1.0f);
        refreshOnlyInPlayMode = EditorPrefs.GetBool("GridRefresh_PlayModeOnly", false);
        refreshOnHierarchyChange = EditorPrefs.GetBool("GridRefresh_HierarchyChange", true);
        refreshOnSelectionChange = EditorPrefs.GetBool("GridRefresh_SelectionChange", true);
    }

    /// <summary>
    /// 保存设置
    /// </summary>
    private void SaveSettings()
    {
        EditorPrefs.SetBool("GridRefresh_AutoEnabled", isAutoRefreshEnabled);
        EditorPrefs.SetFloat("GridRefresh_Interval", autoRefreshInterval);
        EditorPrefs.SetBool("GridRefresh_PlayModeOnly", refreshOnlyInPlayMode);
        EditorPrefs.SetBool("GridRefresh_HierarchyChange", refreshOnHierarchyChange);
        EditorPrefs.SetBool("GridRefresh_SelectionChange", refreshOnSelectionChange);
    }

    // 公共属性
    public bool IsAutoRefreshEnabled => isAutoRefreshEnabled;
    public float AutoRefreshInterval => autoRefreshInterval;
    public bool RefreshOnlyInPlayMode => refreshOnlyInPlayMode;
    public bool RefreshOnHierarchyChange => refreshOnHierarchyChange;
    public bool RefreshOnSelectionChange => refreshOnSelectionChange;
    public bool IsRefreshing => isRefreshing;
    public int QueuedGridsCount => refreshQueue.Count;
}

/// <summary>
/// 刷新统计信息
/// </summary>
[Serializable]
public class RefreshStatistics
{
    public int TotalRefreshCount;
    public float AverageRefreshTime;
    public bool IsAutoRefreshEnabled;
    public float RefreshInterval;
    public int QueuedGridsCount;
    public bool IsCurrentlyRefreshing;
    public Dictionary<string, float> RefreshTimesByType;
    public Dictionary<string, int> RefreshCountsByType;
}