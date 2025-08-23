using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using InventorySystem.Grid;
using InventorySystem;

namespace InventorySystem.Editor
{
    /// <summary>
    /// 网格系统事件管理器 - 负责管理所有网格系统相关的事件
    /// 提供统一的事件分发和监听机制，支持动态更新功能
    /// </summary>
    public class GridSystemEventManager : ScriptableObject
    {
        #region 单例模式
        private static GridSystemEventManager instance;
        
        /// <summary>
        /// 获取事件管理器实例
        /// </summary>
        public static GridSystemEventManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = CreateInstance<GridSystemEventManager>();
                    instance.Initialize();
                }
                return instance;
            }
        }
        #endregion
        
        #region 事件定义
        
        // 网格相关事件
        public static event Action<BaseItemGrid> OnGridRegistered;           // 网格注册事件
        public static event Action<BaseItemGrid> OnGridUnregistered;         // 网格注销事件
        public static event Action<BaseItemGrid> OnGridStateChanged;         // 网格状态变化事件
        public static event Action<BaseItemGrid> OnGridDataUpdated;          // 网格数据更新事件
        
        // 物品相关事件
        public static event Action<BaseItemGrid, InventorySystemItem, Vector2Int> OnItemAdded;      // 物品添加事件
        public static event Action<BaseItemGrid, InventorySystemItem, Vector2Int> OnItemRemoved;    // 物品移除事件
        public static event Action<BaseItemGrid, InventorySystemItem, Vector2Int, Vector2Int> OnItemMoved; // 物品移动事件
        public static event Action<BaseItemGrid, InventorySystemItem> OnItemStackChanged;          // 物品堆叠变化事件
        
        // 系统性能事件
        public static event Action<float> OnPerformanceUpdate;               // 性能更新事件
        public static event Action<string> OnSystemError;                    // 系统错误事件
        public static event Action<string> OnSystemWarning;                  // 系统警告事件
        public static event Action<string> OnSystemInfo;                     // 系统信息事件
        
        // 监控相关事件
        public static event Action OnMonitoringStarted;                      // 监控开始事件
        public static event Action OnMonitoringStopped;                      // 监控停止事件
        public static event Action<GridSystemStatistics> OnStatisticsUpdated;   // 统计信息更新事件
        public static event Action OnCacheCleared;                           // 缓存清理事件
        
        // 刷新相关事件
        public static event Action OnRefreshStarted;                         // 刷新开始事件
        public static event Action<int> OnRefreshProgress;                   // 刷新进度事件
        public static event Action<bool> OnRefreshCompleted;                 // 刷新完成事件
        public static event Action<string> OnRefreshError;                   // 刷新错误事件
        
        #endregion
        
        #region 事件统计
        
        [SerializeField] private Dictionary<string, int> eventCounts = new Dictionary<string, int>();
        [SerializeField] private Dictionary<string, DateTime> lastEventTimes = new Dictionary<string, DateTime>();
        [SerializeField] private List<EventLogEntry> eventLog = new List<EventLogEntry>();
        [SerializeField] private int maxLogEntries = 1000;
        [SerializeField] private bool enableEventLogging = true;
        [SerializeField] private bool enablePerformanceTracking = true;
        
        /// <summary>
        /// 事件日志条目
        /// </summary>
        [Serializable]
        public class EventLogEntry
        {
            public string eventName;           // 事件名称
            public DateTime timestamp;         // 时间戳
            public string details;             // 事件详情
            public EventSeverity severity;     // 事件严重程度
            
            public EventLogEntry(string name, string detail, EventSeverity sev = EventSeverity.Info)
            {
                eventName = name;
                timestamp = DateTime.Now;
                details = detail;
                severity = sev;
            }
        }
        
        /// <summary>
        /// 事件严重程度
        /// </summary>
        public enum EventSeverity
        {
            Info,       // 信息
            Warning,    // 警告
            Error,      // 错误
            Critical    // 严重错误
        }
        
        #endregion
        
        #region 初始化和清理
        
        /// <summary>
        /// 初始化事件管理器
        /// </summary>
        private void Initialize()
        {
            if (eventCounts == null)
                eventCounts = new Dictionary<string, int>();
            if (lastEventTimes == null)
                lastEventTimes = new Dictionary<string, DateTime>();
            if (eventLog == null)
                eventLog = new List<EventLogEntry>();
                
            LogEvent("EventManager", "事件管理器已初始化", EventSeverity.Info);
        }
        
        /// <summary>
        /// 清理所有事件监听器
        /// </summary>
        public static void ClearAllListeners()
        {
            OnGridRegistered = null;
            OnGridUnregistered = null;
            OnGridStateChanged = null;
            OnGridDataUpdated = null;
            
            OnItemAdded = null;
            OnItemRemoved = null;
            OnItemMoved = null;
            OnItemStackChanged = null;
            
            OnPerformanceUpdate = null;
            OnSystemError = null;
            OnSystemWarning = null;
            OnSystemInfo = null;
            
            OnMonitoringStarted = null;
            OnMonitoringStopped = null;
            OnStatisticsUpdated = null;
            OnCacheCleared = null;
            
            OnRefreshStarted = null;
            OnRefreshProgress = null;
            OnRefreshCompleted = null;
            OnRefreshError = null;
            
            Instance.LogEvent("EventManager", "所有事件监听器已清理", EventSeverity.Info);
        }
        
        #endregion
        
        #region 事件触发方法
        
        /// <summary>
        /// 触发网格注册事件
        /// </summary>
        public static void TriggerGridRegistered(BaseItemGrid grid)
        {
            Instance.LogEvent("GridRegistered", $"网格已注册: {grid?.name}");
            OnGridRegistered?.Invoke(grid);
        }
        
        /// <summary>
        /// 触发网格注销事件
        /// </summary>
        public static void TriggerGridUnregistered(BaseItemGrid grid)
        {
            Instance.LogEvent("GridUnregistered", $"网格已注销: {grid?.name}");
            OnGridUnregistered?.Invoke(grid);
        }
        
        /// <summary>
        /// 触发网格状态变化事件
        /// </summary>
        public static void TriggerGridStateChanged(BaseItemGrid grid)
        {
            Instance.LogEvent("GridStateChanged", $"网格状态已变化: {grid?.name}");
            OnGridStateChanged?.Invoke(grid);
        }
        
        /// <summary>
        /// 触发网格数据更新事件
        /// </summary>
        public static void TriggerGridDataUpdated(BaseItemGrid grid)
        {
            Instance.LogEvent("GridDataUpdated", $"网格数据已更新: {grid?.name}");
            OnGridDataUpdated?.Invoke(grid);
        }
        
        /// <summary>
        /// 触发物品添加事件
        /// </summary>
        public static void TriggerItemAdded(BaseItemGrid grid, InventorySystemItem item, Vector2Int position)
        {
            Instance.LogEvent("ItemAdded", $"物品已添加: {item?.Data?.itemName} 到 {grid?.name} 位置 {position}");
            OnItemAdded?.Invoke(grid, item, position);
        }
        
        /// <summary>
        /// 触发物品移除事件
        /// </summary>
        public static void TriggerItemRemoved(BaseItemGrid grid, InventorySystemItem item, Vector2Int position)
        {
            Instance.LogEvent("ItemRemoved", $"物品已移除: {item?.Data?.itemName} 从 {grid?.name} 位置 {position}");
            OnItemRemoved?.Invoke(grid, item, position);
        }
        
        /// <summary>
        /// 触发物品移动事件
        /// </summary>
        public static void TriggerItemMoved(BaseItemGrid grid, InventorySystemItem item, Vector2Int fromPos, Vector2Int toPos)
        {
            Instance.LogEvent("ItemMoved", $"物品已移动: {item?.Data?.itemName} 在 {grid?.name} 从 {fromPos} 到 {toPos}");
            OnItemMoved?.Invoke(grid, item, fromPos, toPos);
        }
        
        /// <summary>
        /// 触发物品堆叠变化事件
        /// </summary>
        public static void TriggerItemStackChanged(BaseItemGrid grid, InventorySystemItem item)
        {
            Instance.LogEvent("ItemStackChanged", $"物品堆叠已变化: {item?.Data?.itemName} 在 {grid?.name}");
            OnItemStackChanged?.Invoke(grid, item);
        }
        
        /// <summary>
        /// 触发性能更新事件
        /// </summary>
        public static void TriggerPerformanceUpdate(float deltaTime)
        {
            if (Instance.enablePerformanceTracking)
            {
                OnPerformanceUpdate?.Invoke(deltaTime);
            }
        }
        
        /// <summary>
        /// 触发系统错误事件
        /// </summary>
        public static void TriggerSystemError(string message)
        {
            Instance.LogEvent("SystemError", message, EventSeverity.Error);
            OnSystemError?.Invoke(message);
        }
        
        /// <summary>
        /// 触发系统警告事件
        /// </summary>
        public static void TriggerSystemWarning(string message)
        {
            Instance.LogEvent("SystemWarning", message, EventSeverity.Warning);
            OnSystemWarning?.Invoke(message);
        }
        
        /// <summary>
        /// 触发系统信息事件
        /// </summary>
        public static void TriggerSystemInfo(string message)
        {
            Instance.LogEvent("SystemInfo", message, EventSeverity.Info);
            OnSystemInfo?.Invoke(message);
        }
        
        /// <summary>
        /// 触发监控开始事件
        /// </summary>
        public static void TriggerMonitoringStarted()
        {
            Instance.LogEvent("MonitoringStarted", "监控已开始");
            OnMonitoringStarted?.Invoke();
        }
        
        /// <summary>
        /// 触发监控停止事件
        /// </summary>
        public static void TriggerMonitoringStopped()
        {
            Instance.LogEvent("MonitoringStopped", "监控已停止");
            OnMonitoringStopped?.Invoke();
        }
        
        /// <summary>
        /// 触发统计信息更新事件
        /// </summary>
        public static void TriggerStatisticsUpdated(GridSystemStatistics statistics)
        {
            Instance.LogEvent("StatisticsUpdated", "统计信息已更新");
            OnStatisticsUpdated?.Invoke(statistics);
        }
        
        /// <summary>
        /// 触发缓存清理事件
        /// </summary>
        public static void TriggerCacheCleared()
        {
            Instance.LogEvent("CacheCleared", "缓存已清理");
            OnCacheCleared?.Invoke();
        }
        
        /// <summary>
        /// 触发刷新开始事件
        /// </summary>
        public static void TriggerRefreshStarted()
        {
            Instance.LogEvent("RefreshStarted", "刷新已开始");
            OnRefreshStarted?.Invoke();
        }
        
        /// <summary>
        /// 触发刷新进度事件
        /// </summary>
        public static void TriggerRefreshProgress(int progress)
        {
            OnRefreshProgress?.Invoke(progress);
        }
        
        /// <summary>
        /// 触发刷新完成事件
        /// </summary>
        public static void TriggerRefreshCompleted(bool success)
        {
            Instance.LogEvent("RefreshCompleted", $"刷新已完成: {(success ? "成功" : "失败")}");
            OnRefreshCompleted?.Invoke(success);
        }
        
        /// <summary>
        /// 触发刷新错误事件
        /// </summary>
        public static void TriggerRefreshError(string error)
        {
            Instance.LogEvent("RefreshError", $"刷新错误: {error}", EventSeverity.Error);
            OnRefreshError?.Invoke(error);
        }
        
        #endregion
        
        #region 事件日志和统计
        
        /// <summary>
        /// 记录事件日志
        /// </summary>
        private void LogEvent(string eventName, string details, EventSeverity severity = EventSeverity.Info)
        {
            if (!enableEventLogging) return;
            
            // 更新事件计数
            if (eventCounts.ContainsKey(eventName))
                eventCounts[eventName]++;
            else
                eventCounts[eventName] = 1;
            
            // 更新最后事件时间
            lastEventTimes[eventName] = DateTime.Now;
            
            // 添加到事件日志
            var logEntry = new EventLogEntry(eventName, details, severity);
            eventLog.Add(logEntry);
            
            // 限制日志条目数量
            if (eventLog.Count > maxLogEntries)
            {
                eventLog.RemoveAt(0);
            }
            
            // 输出到Unity控制台（仅错误和警告）
            if (severity == EventSeverity.Error)
            {
                Debug.LogError($"[GridSystem] {eventName}: {details}");
            }
            else if (severity == EventSeverity.Warning)
            {
                Debug.LogWarning($"[GridSystem] {eventName}: {details}");
            }
        }
        
        /// <summary>
        /// 获取事件统计信息
        /// </summary>
        public Dictionary<string, int> GetEventCounts()
        {
            return new Dictionary<string, int>(eventCounts);
        }
        
        /// <summary>
        /// 获取最后事件时间
        /// </summary>
        public Dictionary<string, DateTime> GetLastEventTimes()
        {
            return new Dictionary<string, DateTime>(lastEventTimes);
        }
        
        /// <summary>
        /// 获取事件日志
        /// </summary>
        public List<EventLogEntry> GetEventLog()
        {
            return new List<EventLogEntry>(eventLog);
        }
        
        /// <summary>
        /// 获取指定严重程度的事件日志
        /// </summary>
        public List<EventLogEntry> GetEventLog(EventSeverity severity)
        {
            return eventLog.FindAll(entry => entry.severity == severity);
        }
        
        /// <summary>
        /// 清理事件日志
        /// </summary>
        public void ClearEventLog()
        {
            eventLog.Clear();
            LogEvent("EventLog", "事件日志已清理");
        }
        
        /// <summary>
        /// 重置事件统计
        /// </summary>
        public void ResetEventStatistics()
        {
            eventCounts.Clear();
            lastEventTimes.Clear();
            LogEvent("EventStatistics", "事件统计已重置");
        }
        
        #endregion
        
        #region 配置设置
        
        /// <summary>
        /// 设置事件日志记录状态
        /// </summary>
        public void SetEventLogging(bool enabled)
        {
            enableEventLogging = enabled;
            LogEvent("EventLogging", $"事件日志记录已{(enabled ? "启用" : "禁用")}");
        }
        
        /// <summary>
        /// 设置性能跟踪状态
        /// </summary>
        public void SetPerformanceTracking(bool enabled)
        {
            enablePerformanceTracking = enabled;
            LogEvent("PerformanceTracking", $"性能跟踪已{(enabled ? "启用" : "禁用")}");
        }
        
        /// <summary>
        /// 设置最大日志条目数
        /// </summary>
        public void SetMaxLogEntries(int maxEntries)
        {
            maxLogEntries = Mathf.Max(100, maxEntries);
            
            // 如果当前日志超过新的限制，则删除旧条目
            while (eventLog.Count > maxLogEntries)
            {
                eventLog.RemoveAt(0);
            }
            
            LogEvent("MaxLogEntries", $"最大日志条目数已设置为: {maxLogEntries}");
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
        }
        
        /// <summary>
        /// Editor更新循环
        /// </summary>
        private void OnEditorUpdate()
        {
            if (enablePerformanceTracking)
            {
                TriggerPerformanceUpdate(Time.realtimeSinceStartup);
            }
        }
        
        #endregion
    }
}