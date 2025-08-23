using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using InventorySystem.Grid;
using InventorySystem;
using GridSystem.Editor;

namespace InventorySystem.Editor
{
    /// <summary>
    /// 网格系统事件监听器 - 负责监听网格系统事件并执行相应的响应操作
    /// 提供自动化的事件响应机制，支持动态更新和实时监控
    /// </summary>
    public class GridSystemEventListener : ScriptableObject
    {
        #region 单例模式
        private static GridSystemEventListener instance;
        
        /// <summary>
        /// 获取事件监听器实例
        /// </summary>
        public static GridSystemEventListener Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = CreateInstance<GridSystemEventListener>();
                    instance.Initialize();
                }
                return instance;
            }
        }
        #endregion
        
        #region 监听器配置
        
        [SerializeField] private bool isListening = false;
        [SerializeField] private bool autoStartListening = true;
        [SerializeField] private bool enableGridEventListening = true;
        [SerializeField] private bool enableItemEventListening = true;
        [SerializeField] private bool enableSystemEventListening = true;
        [SerializeField] private bool enableMonitoringEventListening = true;
        [SerializeField] private bool enableRefreshEventListening = true;
        
        // 响应延迟设置（避免频繁更新）
        [SerializeField] private float gridUpdateDelay = 0.1f;
        [SerializeField] private float itemUpdateDelay = 0.05f;
        [SerializeField] private float systemUpdateDelay = 0.2f;
        
        // 批处理设置
        [SerializeField] private bool enableBatchProcessing = true;
        [SerializeField] private int maxBatchSize = 10;
        [SerializeField] private float batchProcessInterval = 0.1f;
        
        #endregion
        
        #region 事件响应队列
        
        private Queue<Action> pendingActions = new Queue<Action>();
        private Dictionary<string, DateTime> lastUpdateTimes = new Dictionary<string, DateTime>();
        private List<BaseItemGrid> pendingGridUpdates = new List<BaseItemGrid>();
        private List<InventorySystemItem> pendingItemUpdates = new List<InventorySystemItem>();
        
        // 事件响应统计
        private Dictionary<string, int> responseCount = new Dictionary<string, int>();
        private Dictionary<string, float> averageResponseTime = new Dictionary<string, float>();
        
        #endregion
        
        #region 回调委托
        
        // 网格事件回调
        public static event Action<BaseItemGrid> OnGridEventProcessed;
        public static event Action<InventorySystemItem> OnItemEventProcessed;
        public static event Action<string> OnSystemEventProcessed;
        
        // 批处理完成回调
        public static event Action<int> OnBatchProcessed;
        public static event Action OnAllEventsProcessed;
        
        #endregion
        
        #region 初始化和清理
        
        /// <summary>
        /// 初始化事件监听器
        /// </summary>
        private void Initialize()
        {
            if (pendingActions == null)
                pendingActions = new Queue<Action>();
            if (lastUpdateTimes == null)
                lastUpdateTimes = new Dictionary<string, DateTime>();
            if (pendingGridUpdates == null)
                pendingGridUpdates = new List<BaseItemGrid>();
            if (pendingItemUpdates == null)
                pendingItemUpdates = new List<InventorySystemItem>();
            if (responseCount == null)
                responseCount = new Dictionary<string, int>();
            if (averageResponseTime == null)
                averageResponseTime = new Dictionary<string, float>();
                
            if (autoStartListening)
            {
                StartListening();
            }
            
            GridSystemEventManager.TriggerSystemInfo("事件监听器已初始化");
        }
        
        /// <summary>
        /// 开始监听事件
        /// </summary>
        public void StartListening()
        {
            if (isListening) return;
            
            // 注册网格相关事件
            if (enableGridEventListening)
            {
                GridSystemEventManager.OnGridRegistered += OnGridRegistered;
                GridSystemEventManager.OnGridUnregistered += OnGridUnregistered;
                GridSystemEventManager.OnGridStateChanged += OnGridStateChanged;
                GridSystemEventManager.OnGridDataUpdated += OnGridDataUpdated;
            }
            
            // 注册物品相关事件
            if (enableItemEventListening)
            {
                GridSystemEventManager.OnItemAdded += OnItemAdded;
                GridSystemEventManager.OnItemRemoved += OnItemRemoved;
                GridSystemEventManager.OnItemMoved += OnItemMoved;
                GridSystemEventManager.OnItemStackChanged += OnItemStackChanged;
            }
            
            // 注册系统事件
            if (enableSystemEventListening)
            {
                GridSystemEventManager.OnSystemError += OnSystemError;
                GridSystemEventManager.OnSystemWarning += OnSystemWarning;
                GridSystemEventManager.OnSystemInfo += OnSystemInfo;
                GridSystemEventManager.OnPerformanceUpdate += OnPerformanceUpdate;
            }
            
            // 注册监控事件
            if (enableMonitoringEventListening)
            {
                GridSystemEventManager.OnMonitoringStarted += OnMonitoringStarted;
                GridSystemEventManager.OnMonitoringStopped += OnMonitoringStopped;
                GridSystemEventManager.OnStatisticsUpdated += OnStatisticsUpdated;
                GridSystemEventManager.OnCacheCleared += OnCacheCleared;
            }
            
            // 注册刷新事件
            if (enableRefreshEventListening)
            {
                GridSystemEventManager.OnRefreshStarted += OnRefreshStarted;
                GridSystemEventManager.OnRefreshProgress += OnRefreshProgress;
                GridSystemEventManager.OnRefreshCompleted += OnRefreshCompleted;
                GridSystemEventManager.OnRefreshError += OnRefreshError;
            }
            
            isListening = true;
            GridSystemEventManager.TriggerSystemInfo("事件监听器已开始监听");
        }
        
        /// <summary>
        /// 停止监听事件
        /// </summary>
        public void StopListening()
        {
            if (!isListening) return;
            
            // 注销网格相关事件
            GridSystemEventManager.OnGridRegistered -= OnGridRegistered;
            GridSystemEventManager.OnGridUnregistered -= OnGridUnregistered;
            GridSystemEventManager.OnGridStateChanged -= OnGridStateChanged;
            GridSystemEventManager.OnGridDataUpdated -= OnGridDataUpdated;
            
            // 注销物品相关事件
            GridSystemEventManager.OnItemAdded -= OnItemAdded;
            GridSystemEventManager.OnItemRemoved -= OnItemRemoved;
            GridSystemEventManager.OnItemMoved -= OnItemMoved;
            GridSystemEventManager.OnItemStackChanged -= OnItemStackChanged;
            
            // 注销系统事件
            GridSystemEventManager.OnSystemError -= OnSystemError;
            GridSystemEventManager.OnSystemWarning -= OnSystemWarning;
            GridSystemEventManager.OnSystemInfo -= OnSystemInfo;
            GridSystemEventManager.OnPerformanceUpdate -= OnPerformanceUpdate;
            
            // 注销监控事件
            GridSystemEventManager.OnMonitoringStarted -= OnMonitoringStarted;
            GridSystemEventManager.OnMonitoringStopped -= OnMonitoringStopped;
            GridSystemEventManager.OnStatisticsUpdated -= OnStatisticsUpdated;
            GridSystemEventManager.OnCacheCleared -= OnCacheCleared;
            
            // 注销刷新事件
            GridSystemEventManager.OnRefreshStarted -= OnRefreshStarted;
            GridSystemEventManager.OnRefreshProgress -= OnRefreshProgress;
            GridSystemEventManager.OnRefreshCompleted -= OnRefreshCompleted;
            GridSystemEventManager.OnRefreshError -= OnRefreshError;
            
            isListening = false;
            GridSystemEventManager.TriggerSystemInfo("事件监听器已停止监听");
        }
        
        #endregion
        
        #region 网格事件处理
        
        /// <summary>
        /// 处理网格注册事件
        /// </summary>
        private void OnGridRegistered(BaseItemGrid grid)
        {
            QueueAction(() => {
                ProcessGridEvent(grid, "GridRegistered");
                
                // 通知监控窗口更新网格列表
                if (GridSystemMonitorWindow.HasOpenInstances<GridSystemMonitorWindow>())
                {
                    var window = EditorWindow.GetWindow<GridSystemMonitorWindow>();
                    window.RefreshGridData();
                }
                
                OnGridEventProcessed?.Invoke(grid);
            });
        }
        
        /// <summary>
        /// 处理网格注销事件
        /// </summary>
        private void OnGridUnregistered(BaseItemGrid grid)
        {
            QueueAction(() => {
                ProcessGridEvent(grid, "GridUnregistered");
                
                // 从待处理列表中移除
                pendingGridUpdates.Remove(grid);
                
                // 通知监控窗口更新网格列表
                if (GridSystemMonitorWindow.HasOpenInstances<GridSystemMonitorWindow>())
                {
                    var window = EditorWindow.GetWindow<GridSystemMonitorWindow>();
                    window.RefreshGridData();
                }
                
                OnGridEventProcessed?.Invoke(grid);
            });
        }
        
        /// <summary>
        /// 处理网格状态变化事件
        /// </summary>
        private void OnGridStateChanged(BaseItemGrid grid)
        {
            QueueDelayedGridUpdate(grid, "GridStateChanged");
        }
        
        /// <summary>
        /// 处理网格数据更新事件
        /// </summary>
        private void OnGridDataUpdated(BaseItemGrid grid)
        {
            QueueDelayedGridUpdate(grid, "GridDataUpdated");
        }
        
        #endregion
        
        #region 物品事件处理
        
        /// <summary>
        /// 处理物品添加事件
        /// </summary>
        private void OnItemAdded(BaseItemGrid grid, InventorySystemItem item, Vector2Int position)
        {
            QueueAction(() => {
                ProcessItemEvent(item, "ItemAdded");
                QueueDelayedGridUpdate(grid, "ItemAdded");
                
                OnItemEventProcessed?.Invoke(item);
            });
        }
        
        /// <summary>
        /// 处理物品移除事件
        /// </summary>
        private void OnItemRemoved(BaseItemGrid grid, InventorySystemItem item, Vector2Int position)
        {
            QueueAction(() => {
                ProcessItemEvent(item, "ItemRemoved");
                QueueDelayedGridUpdate(grid, "ItemRemoved");
                
                OnItemEventProcessed?.Invoke(item);
            });
        }
        
        /// <summary>
        /// 处理物品移动事件
        /// </summary>
        private void OnItemMoved(BaseItemGrid grid, InventorySystemItem item, Vector2Int fromPos, Vector2Int toPos)
        {
            QueueAction(() => {
                ProcessItemEvent(item, "ItemMoved");
                QueueDelayedGridUpdate(grid, "ItemMoved");
                
                OnItemEventProcessed?.Invoke(item);
            });
        }
        
        /// <summary>
        /// 处理物品堆叠变化事件
        /// </summary>
        private void OnItemStackChanged(BaseItemGrid grid, InventorySystemItem item)
        {
            QueueAction(() => {
                ProcessItemEvent(item, "ItemStackChanged");
                QueueDelayedGridUpdate(grid, "ItemStackChanged");
                
                OnItemEventProcessed?.Invoke(item);
            });
        }
        
        #endregion
        
        #region 系统事件处理
        
        /// <summary>
        /// 处理系统错误事件
        /// </summary>
        private void OnSystemError(string message)
        {
            QueueAction(() => {
                ProcessSystemEvent("SystemError", message);
                
                // 错误事件需要立即处理，不延迟
                OnSystemEventProcessed?.Invoke($"Error: {message}");
            });
        }
        
        /// <summary>
        /// 处理系统警告事件
        /// </summary>
        private void OnSystemWarning(string message)
        {
            QueueAction(() => {
                ProcessSystemEvent("SystemWarning", message);
                OnSystemEventProcessed?.Invoke($"Warning: {message}");
            });
        }
        
        /// <summary>
        /// 处理系统信息事件
        /// </summary>
        private void OnSystemInfo(string message)
        {
            QueueDelayedSystemUpdate("SystemInfo", message);
        }
        
        /// <summary>
        /// 处理性能更新事件
        /// </summary>
        private void OnPerformanceUpdate(float deltaTime)
        {
            // 性能更新事件频率较高，使用延迟处理
            QueueDelayedSystemUpdate("PerformanceUpdate", $"DeltaTime: {deltaTime:F4}");
        }
        
        #endregion
        
        #region 监控事件处理
        
        /// <summary>
        /// 处理监控开始事件
        /// </summary>
        private void OnMonitoringStarted()
        {
            QueueAction(() => {
                ProcessSystemEvent("MonitoringStarted", "监控已开始");
                OnSystemEventProcessed?.Invoke("MonitoringStarted");
            });
        }
        
        /// <summary>
        /// 处理监控停止事件
        /// </summary>
        private void OnMonitoringStopped()
        {
            QueueAction(() => {
                ProcessSystemEvent("MonitoringStopped", "监控已停止");
                OnSystemEventProcessed?.Invoke("MonitoringStopped");
            });
        }
        
        /// <summary>
        /// 处理统计信息更新事件
        /// </summary>
        private void OnStatisticsUpdated(GridSystemStatistics statistics)
        {
            QueueAction(() => {
                ProcessSystemEvent("StatisticsUpdated", $"统计信息已更新: {statistics?.TotalGrids} 个网格");
                
                // 通知监控窗口更新统计信息
                if (GridSystemMonitorWindow.HasOpenInstances<GridSystemMonitorWindow>())
                {
                    var window = EditorWindow.GetWindow<GridSystemMonitorWindow>();
                    window.Repaint();
                }
                
                OnSystemEventProcessed?.Invoke("StatisticsUpdated");
            });
        }
        
        /// <summary>
        /// 处理缓存清理事件
        /// </summary>
        private void OnCacheCleared()
        {
            QueueAction(() => {
                ProcessSystemEvent("CacheCleared", "缓存已清理");
                
                // 清理本地缓存
                pendingGridUpdates.Clear();
                pendingItemUpdates.Clear();
                
                OnSystemEventProcessed?.Invoke("CacheCleared");
            });
        }
        
        #endregion
        
        #region 刷新事件处理
        
        /// <summary>
        /// 处理刷新开始事件
        /// </summary>
        private void OnRefreshStarted()
        {
            QueueAction(() => {
                ProcessSystemEvent("RefreshStarted", "刷新已开始");
                OnSystemEventProcessed?.Invoke("RefreshStarted");
            });
        }
        
        /// <summary>
        /// 处理刷新进度事件
        /// </summary>
        private void OnRefreshProgress(int progress)
        {
            // 进度事件频率较高，使用延迟处理
            QueueDelayedSystemUpdate("RefreshProgress", $"进度: {progress}%");
        }
        
        /// <summary>
        /// 处理刷新完成事件
        /// </summary>
        private void OnRefreshCompleted(bool success)
        {
            QueueAction(() => {
                ProcessSystemEvent("RefreshCompleted", $"刷新完成: {(success ? "成功" : "失败")}");
                
                // 刷新完成后，通知监控窗口更新
                if (GridSystemMonitorWindow.HasOpenInstances<GridSystemMonitorWindow>())
                {
                    var window = EditorWindow.GetWindow<GridSystemMonitorWindow>();
                    window.RefreshGridData();
                }
                
                OnSystemEventProcessed?.Invoke($"RefreshCompleted: {success}");
            });
        }
        
        /// <summary>
        /// 处理刷新错误事件
        /// </summary>
        private void OnRefreshError(string error)
        {
            QueueAction(() => {
                ProcessSystemEvent("RefreshError", $"刷新错误: {error}");
                OnSystemEventProcessed?.Invoke($"RefreshError: {error}");
            });
        }
        
        #endregion
        
        #region 事件处理核心方法
        
        /// <summary>
        /// 将操作加入队列
        /// </summary>
        private void QueueAction(Action action)
        {
            if (enableBatchProcessing)
            {
                pendingActions.Enqueue(action);
            }
            else
            {
                action?.Invoke();
            }
        }
        
        /// <summary>
        /// 队列延迟的网格更新
        /// </summary>
        private void QueueDelayedGridUpdate(BaseItemGrid grid, string eventType)
        {
            if (grid == null) return;
            
            string key = $"Grid_{grid.GetInstanceID()}_{eventType}";
            DateTime now = DateTime.Now;
            
            if (lastUpdateTimes.ContainsKey(key))
            {
                if ((now - lastUpdateTimes[key]).TotalSeconds < gridUpdateDelay)
                {
                    return; // 跳过频繁更新
                }
            }
            
            lastUpdateTimes[key] = now;
            
            QueueAction(() => {
                ProcessGridEvent(grid, eventType);
                OnGridEventProcessed?.Invoke(grid);
            });
        }
        
        /// <summary>
        /// 队列延迟的系统更新
        /// </summary>
        private void QueueDelayedSystemUpdate(string eventType, string message)
        {
            DateTime now = DateTime.Now;
            
            if (lastUpdateTimes.ContainsKey(eventType))
            {
                if ((now - lastUpdateTimes[eventType]).TotalSeconds < systemUpdateDelay)
                {
                    return; // 跳过频繁更新
                }
            }
            
            lastUpdateTimes[eventType] = now;
            
            QueueAction(() => {
                ProcessSystemEvent(eventType, message);
                OnSystemEventProcessed?.Invoke(eventType);
            });
        }
        
        /// <summary>
        /// 处理网格事件
        /// </summary>
        private void ProcessGridEvent(BaseItemGrid grid, string eventType)
        {
            if (grid == null) return;
            
            var startTime = DateTime.Now;
            
            try
            {
                // 更新网格数据缓存
                var dataManager = GridMonitorDataManager.Instance;
                if (dataManager != null)
                {
                    dataManager.ForceRefreshGrid(grid);
                }
                
                // 记录响应统计
                RecordResponseTime(eventType, (DateTime.Now - startTime).TotalMilliseconds);
            }
            catch (Exception ex)
            {
                GridSystemEventManager.TriggerSystemError($"处理网格事件时发生错误: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 处理物品事件
        /// </summary>
        private void ProcessItemEvent(InventorySystemItem item, string eventType)
        {
            if (item == null) return;
            
            var startTime = DateTime.Now;
            
            try
            {
                // 更新物品相关的网格数据
                var grids = FindObjectsOfType<BaseItemGrid>();
                foreach (var grid in grids)
                {
                    if (grid.ContainsItem(item))
                    {
                        var dataManager = GridMonitorDataManager.Instance;
                        if (dataManager != null)
                        {
                            dataManager.ForceRefreshGrid(grid);
                        }
                        break;
                    }
                }
                
                // 记录响应统计
                RecordResponseTime(eventType, (DateTime.Now - startTime).TotalMilliseconds);
            }
            catch (Exception ex)
            {
                GridSystemEventManager.TriggerSystemError($"处理物品事件时发生错误: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 处理系统事件
        /// </summary>
        private void ProcessSystemEvent(string eventType, string message)
        {
            var startTime = DateTime.Now;
            
            try
            {
                // 根据事件类型执行相应操作
                switch (eventType)
                {
                    case "SystemError":
                    case "RefreshError":
                        // 错误事件可能需要特殊处理
                        break;
                        
                    case "CacheCleared":
                        // 清理本地缓存
                        lastUpdateTimes.Clear();
                        break;
                        
                    case "MonitoringStarted":
                        // 监控开始时重置统计
                        responseCount.Clear();
                        averageResponseTime.Clear();
                        break;
                }
                
                // 记录响应统计
                RecordResponseTime(eventType, (DateTime.Now - startTime).TotalMilliseconds);
            }
            catch (Exception ex)
            {
                GridSystemEventManager.TriggerSystemError($"处理系统事件时发生错误: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 记录响应时间统计
        /// </summary>
        private void RecordResponseTime(string eventType, double responseTimeMs)
        {
            if (responseCount.ContainsKey(eventType))
            {
                responseCount[eventType]++;
                averageResponseTime[eventType] = (averageResponseTime[eventType] + (float)responseTimeMs) / 2f;
            }
            else
            {
                responseCount[eventType] = 1;
                averageResponseTime[eventType] = (float)responseTimeMs;
            }
        }
        
        #endregion
        
        #region 批处理系统
        
        /// <summary>
        /// 处理待处理的操作队列
        /// </summary>
        public void ProcessPendingActions()
        {
            if (!enableBatchProcessing || pendingActions.Count == 0) return;
            
            int processedCount = 0;
            int maxProcess = Mathf.Min(maxBatchSize, pendingActions.Count);
            
            for (int i = 0; i < maxProcess; i++)
            {
                if (pendingActions.Count > 0)
                {
                    var action = pendingActions.Dequeue();
                    try
                    {
                        action?.Invoke();
                        processedCount++;
                    }
                    catch (Exception ex)
                    {
                        GridSystemEventManager.TriggerSystemError($"执行批处理操作时发生错误: {ex.Message}");
                    }
                }
            }
            
            if (processedCount > 0)
            {
                OnBatchProcessed?.Invoke(processedCount);
            }
            
            if (pendingActions.Count == 0)
            {
                OnAllEventsProcessed?.Invoke();
            }
        }
        
        /// <summary>
        /// 获取待处理操作数量
        /// </summary>
        public int GetPendingActionsCount()
        {
            return pendingActions?.Count ?? 0;
        }
        
        /// <summary>
        /// 清空待处理操作队列
        /// </summary>
        public void ClearPendingActions()
        {
            pendingActions?.Clear();
            GridSystemEventManager.TriggerSystemInfo("待处理操作队列已清空");
        }
        
        #endregion
        
        #region 配置和统计
        
        /// <summary>
        /// 设置批处理配置
        /// </summary>
        public void SetBatchProcessing(bool enabled, int batchSize = 10, float interval = 0.1f)
        {
            enableBatchProcessing = enabled;
            maxBatchSize = Mathf.Max(1, batchSize);
            batchProcessInterval = Mathf.Max(0.01f, interval);
            
            GridSystemEventManager.TriggerSystemInfo($"批处理配置已更新: 启用={enabled}, 批大小={maxBatchSize}, 间隔={interval}s");
        }
        
        /// <summary>
        /// 设置事件监听配置
        /// </summary>
        public void SetEventListeningConfig(bool grid, bool item, bool system, bool monitoring, bool refresh)
        {
            enableGridEventListening = grid;
            enableItemEventListening = item;
            enableSystemEventListening = system;
            enableMonitoringEventListening = monitoring;
            enableRefreshEventListening = refresh;
            
            // 如果正在监听，需要重新注册事件
            if (isListening)
            {
                StopListening();
                StartListening();
            }
            
            GridSystemEventManager.TriggerSystemInfo("事件监听配置已更新");
        }
        
        /// <summary>
        /// 获取响应统计信息
        /// </summary>
        public Dictionary<string, int> GetResponseCounts()
        {
            return new Dictionary<string, int>(responseCount);
        }
        
        /// <summary>
        /// 获取平均响应时间
        /// </summary>
        public Dictionary<string, float> GetAverageResponseTimes()
        {
            return new Dictionary<string, float>(averageResponseTime);
        }
        
        /// <summary>
        /// 重置统计信息
        /// </summary>
        public void ResetStatistics()
        {
            responseCount.Clear();
            averageResponseTime.Clear();
            lastUpdateTimes.Clear();
            
            GridSystemEventManager.TriggerSystemInfo("事件监听器统计信息已重置");
        }
        
        #endregion
        
        #region Unity生命周期
        
        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
        }
        
        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            StopListening();
        }
        
        /// <summary>
        /// Editor更新循环
        /// </summary>
        private void OnEditorUpdate()
        {
            if (enableBatchProcessing)
            {
                ProcessPendingActions();
            }
        }
        
        #endregion
    }
}