using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace GridSystem.Editor
{
    /// <summary>
    /// 网格系统事件管理器 - 负责管理所有网格系统相关的事件
    /// 提供事件触发、统计和配置功能
    /// </summary>
    [CreateAssetMenu(fileName = "GridSystemEventManager", menuName = "Grid System/Event Manager")]
    public class GridSystemEventManager : ScriptableObject
    {
        #region 单例模式
        private static GridSystemEventManager _instance;
        
        /// <summary>
        /// 获取事件管理器实例
        /// </summary>
        public static GridSystemEventManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<GridSystemEventManager>("GridSystemEventManager");
                    if (_instance == null)
                    {
                        _instance = CreateInstance<GridSystemEventManager>();
                        _instance.Initialize();
                    }
                }
                return _instance;
            }
        }
        #endregion
        
        #region 配置设置
        [Header("事件配置")]
        [SerializeField] private bool enableEventLogging = true;
        [SerializeField] private bool enablePerformanceTracking = true;
        [SerializeField] private int maxLogEntries = 1000;
        
        [Header("事件统计")]
        [SerializeField] private bool trackEventStatistics = true;
        #endregion
        
        #region 事件统计
        // 事件计数
        private Dictionary<string, int> eventCounts = new Dictionary<string, int>();
        // 最后事件时间
        private Dictionary<string, DateTime> lastEventTimes = new Dictionary<string, DateTime>();
        // 事件日志
        private List<EventLogEntry> eventLogs = new List<EventLogEntry>();
        #endregion
        
        #region 网格事件
        /// <summary>
        /// 网格注册事件
        /// </summary>
        public event System.Action<string> OnGridRegistered;
        
        /// <summary>
        /// 网格注销事件
        /// </summary>
        public event System.Action<string> OnGridUnregistered;
        
        /// <summary>
        /// 网格状态变化事件
        /// </summary>
        public event System.Action<string> OnGridStateChanged;
        
        /// <summary>
        /// 网格数据更新事件
        /// </summary>
        public event System.Action<string> OnGridDataUpdated;
        #endregion
        
        #region 物品事件
        /// <summary>
        /// 物品添加事件
        /// </summary>
        public event System.Action<string> OnItemAdded;
        
        /// <summary>
        /// 物品移除事件
        /// </summary>
        public event System.Action<string> OnItemRemoved;
        
        /// <summary>
        /// 物品移动事件
        /// </summary>
        public event System.Action<string> OnItemMoved;
        
        /// <summary>
        /// 物品堆叠变化事件
        /// </summary>
        public event System.Action<string> OnItemStackChanged;
        #endregion
        
        #region 系统事件
        /// <summary>
        /// 系统性能更新事件
        /// </summary>
        public event System.Action<string> OnSystemPerformanceUpdated;
        
        /// <summary>
        /// 系统错误事件
        /// </summary>
        public event System.Action<string> OnSystemError;
        
        /// <summary>
        /// 系统警告事件
        /// </summary>
        public event System.Action<string> OnSystemWarning;
        
        /// <summary>
        /// 系统信息事件
        /// </summary>
        public event System.Action<string> OnSystemInfo;
        #endregion
        
        #region 监控事件
        /// <summary>
        /// 监控开始事件
        /// </summary>
        public event System.Action<string> OnMonitoringStarted;
        
        /// <summary>
        /// 监控停止事件
        /// </summary>
        public event System.Action<string> OnMonitoringStopped;
        
        /// <summary>
        /// 统计信息更新事件
        /// </summary>
        public event System.Action<string> OnStatisticsUpdated;
        
        /// <summary>
        /// 缓存清理事件
        /// </summary>
        public event System.Action<string> OnCacheCleared;
        #endregion
        
        #region 刷新事件
        /// <summary>
        /// 刷新开始事件
        /// </summary>
        public event System.Action<string> OnRefreshStarted;
        
        /// <summary>
        /// 刷新进度事件
        /// </summary>
        public event System.Action<string> OnRefreshProgress;
        
        /// <summary>
        /// 刷新完成事件
        /// </summary>
        public event System.Action<string> OnRefreshCompleted;
        
        /// <summary>
        /// 刷新错误事件
        /// </summary>
        public event System.Action<string> OnRefreshError;
        #endregion
        
        #region 初始化
        /// <summary>
        /// 初始化事件管理器
        /// </summary>
        public void Initialize()
        {
            Debug.Log("[GridSystemEventManager] 事件管理器已初始化");
        }
        #endregion
        
        #region 事件触发方法 - 网格事件
        /// <summary>
        /// 触发网格注册事件
        /// </summary>
        public static void TriggerGridRegistered(string message)
        {
            Instance.TriggerEvent("GridRegistered", message, Instance.OnGridRegistered);
        }
        
        /// <summary>
        /// 触发网格注销事件
        /// </summary>
        public static void TriggerGridUnregistered(string message)
        {
            Instance.TriggerEvent("GridUnregistered", message, Instance.OnGridUnregistered);
        }
        
        /// <summary>
        /// 触发网格状态变化事件
        /// </summary>
        public static void TriggerGridStateChanged(string message)
        {
            Instance.TriggerEvent("GridStateChanged", message, Instance.OnGridStateChanged);
        }
        
        /// <summary>
        /// 触发网格数据更新事件
        /// </summary>
        public static void TriggerGridDataUpdated(string message)
        {
            Instance.TriggerEvent("GridDataUpdated", message, Instance.OnGridDataUpdated);
        }
        #endregion
        
        #region 事件触发方法 - 物品事件
        /// <summary>
        /// 触发物品添加事件
        /// </summary>
        public static void TriggerItemAdded(string message)
        {
            Instance.TriggerEvent("ItemAdded", message, Instance.OnItemAdded);
        }
        
        /// <summary>
        /// 触发物品移除事件
        /// </summary>
        public static void TriggerItemRemoved(string message)
        {
            Instance.TriggerEvent("ItemRemoved", message, Instance.OnItemRemoved);
        }
        
        /// <summary>
        /// 触发物品移动事件
        /// </summary>
        public static void TriggerItemMoved(string message)
        {
            Instance.TriggerEvent("ItemMoved", message, Instance.OnItemMoved);
        }
        
        /// <summary>
        /// 触发物品堆叠变化事件
        /// </summary>
        public static void TriggerItemStackChanged(string message)
        {
            Instance.TriggerEvent("ItemStackChanged", message, Instance.OnItemStackChanged);
        }
        #endregion
        
        #region 事件触发方法 - 系统事件
        /// <summary>
        /// 触发系统性能更新事件
        /// </summary>
        public static void TriggerSystemPerformanceUpdated(string message)
        {
            Instance.TriggerEvent("SystemPerformanceUpdated", message, Instance.OnSystemPerformanceUpdated);
        }
        
        /// <summary>
        /// 触发系统错误事件
        /// </summary>
        public static void TriggerSystemError(string message)
        {
            Instance.TriggerEvent("SystemError", message, Instance.OnSystemError);
        }
        
        /// <summary>
        /// 触发系统警告事件
        /// </summary>
        public static void TriggerSystemWarning(string message)
        {
            Instance.TriggerEvent("SystemWarning", message, Instance.OnSystemWarning);
        }
        
        /// <summary>
        /// 触发系统信息事件
        /// </summary>
        public static void TriggerSystemInfo(string message)
        {
            Instance.TriggerEvent("SystemInfo", message, Instance.OnSystemInfo);
        }
        #endregion
        
        #region 事件触发方法 - 监控事件
        /// <summary>
        /// 触发监控开始事件
        /// </summary>
        public static void TriggerMonitoringStarted(string message)
        {
            Instance.TriggerEvent("MonitoringStarted", message, Instance.OnMonitoringStarted);
        }
        
        /// <summary>
        /// 触发监控停止事件
        /// </summary>
        public static void TriggerMonitoringStopped(string message)
        {
            Instance.TriggerEvent("MonitoringStopped", message, Instance.OnMonitoringStopped);
        }
        
        /// <summary>
        /// 触发统计信息更新事件
        /// </summary>
        public static void TriggerStatisticsUpdated(string message)
        {
            Instance.TriggerEvent("StatisticsUpdated", message, Instance.OnStatisticsUpdated);
        }
        
        /// <summary>
        /// 触发缓存清理事件
        /// </summary>
        public static void TriggerCacheCleared(string message)
        {
            Instance.TriggerEvent("CacheCleared", message, Instance.OnCacheCleared);
        }
        #endregion
        
        #region 事件触发方法 - 刷新事件
        /// <summary>
        /// 触发刷新开始事件
        /// </summary>
        public static void TriggerRefreshStarted(string message)
        {
            Instance.TriggerEvent("RefreshStarted", message, Instance.OnRefreshStarted);
        }
        
        /// <summary>
        /// 触发刷新进度事件
        /// </summary>
        public static void TriggerRefreshProgress(string message)
        {
            Instance.TriggerEvent("RefreshProgress", message, Instance.OnRefreshProgress);
        }
        
        /// <summary>
        /// 触发刷新完成事件
        /// </summary>
        public static void TriggerRefreshCompleted(string message)
        {
            Instance.TriggerEvent("RefreshCompleted", message, Instance.OnRefreshCompleted);
        }
        
        /// <summary>
        /// 触发刷新错误事件
        /// </summary>
        public static void TriggerRefreshError(string message)
        {
            Instance.TriggerEvent("RefreshError", message, Instance.OnRefreshError);
        }
        #endregion
        
        #region 核心事件处理
        /// <summary>
        /// 触发事件的核心方法
        /// </summary>
        private void TriggerEvent(string eventType, string message, System.Action<string> eventAction)
        {
            try
            {
                // 记录事件统计
                RecordEventStatistics(eventType);
                
                // 记录事件日志
                if (enableEventLogging)
                {
                    LogEvent(eventType, message);
                }
                
                // 触发事件
                eventAction?.Invoke(message);
                
                // 性能跟踪
                if (enablePerformanceTracking)
                {
                    TrackEventPerformance(eventType);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[GridSystemEventManager] 触发事件 {eventType} 时发生错误: {ex.Message}");
            }
        }
        #endregion
        
        #region 统计和日志
        /// <summary>
        /// 记录事件统计
        /// </summary>
        private void RecordEventStatistics(string eventType)
        {
            if (!trackEventStatistics) return;
            
            // 更新事件计数
            if (eventCounts.ContainsKey(eventType))
            {
                eventCounts[eventType]++;
            }
            else
            {
                eventCounts[eventType] = 1;
            }
            
            // 更新最后事件时间
            lastEventTimes[eventType] = DateTime.Now;
        }
        
        /// <summary>
        /// 记录事件日志
        /// </summary>
        private void LogEvent(string eventType, string message)
        {
            var logEntry = new EventLogEntry
            {
                EventType = eventType,
                Message = message,
                Timestamp = DateTime.Now
            };
            
            eventLogs.Add(logEntry);
            
            // 限制日志条目数量
            if (eventLogs.Count > maxLogEntries)
            {
                eventLogs.RemoveAt(0);
            }
            
            // 输出到Unity控制台
            Debug.Log($"[GridSystemEvent] {eventType}: {message}");
        }
        
        /// <summary>
        /// 跟踪事件性能
        /// </summary>
        private void TrackEventPerformance(string eventType)
        {
            // 这里可以添加性能跟踪逻辑
            // 例如：测量事件处理时间、内存使用等
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
        public List<EventLogEntry> GetEventLogs()
        {
            return new List<EventLogEntry>(eventLogs);
        }
        
        /// <summary>
        /// 清除所有统计信息
        /// </summary>
        public void ClearStatistics()
        {
            eventCounts.Clear();
            lastEventTimes.Clear();
            eventLogs.Clear();
            
            Debug.Log("[GridSystemEventManager] 统计信息已清除");
        }
        #endregion
        
        #region Unity生命周期
        private void OnEnable()
        {
            if (_instance == null)
            {
                _instance = this;
            }
        }
        #endregion
    }
    
    /// <summary>
    /// 事件日志条目
    /// </summary>
    [System.Serializable]
    public class EventLogEntry
    {
        public string EventType;
        public string Message;
        public DateTime Timestamp;
        
        public override string ToString()
        {
            return $"[{Timestamp:HH:mm:ss}] {EventType}: {Message}";
        }
    }
}