using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

namespace GridSystem.Editor
{
    /// <summary>
    /// 网格系统事件监听器 - 负责监听和响应网格系统中的各种事件
    /// 实现动态更新和实时监控功能
    /// </summary>
    [CreateAssetMenu(fileName = "GridSystemEventListener", menuName = "Grid System/Event Listener")]
    public class GridSystemEventListener : ScriptableObject
    {
        #region 单例模式
        private static GridSystemEventListener _instance;
        
        /// <summary>
        /// 获取事件监听器实例
        /// </summary>
        public static GridSystemEventListener Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<GridSystemEventListener>("GridSystemEventListener");
                    if (_instance == null)
                    {
                        _instance = CreateInstance<GridSystemEventListener>();
                        _instance.Initialize();
                    }
                }
                return _instance;
            }
        }
        #endregion
        
        #region 配置设置
        [Header("监听配置")]
        [SerializeField] private bool isListening = true;
        [SerializeField] private bool enableBatchProcessing = true;
        [SerializeField] private float batchProcessingDelay = 0.1f;
        [SerializeField] private int maxBatchSize = 50;
        
        [Header("性能配置")]
        [SerializeField] private bool enablePerformanceTracking = true;
        [SerializeField] private int maxResponseTimeEntries = 100;
        #endregion
        
        #region 事件处理
        // 待处理操作队列
        private Queue<System.Action> pendingActions = new Queue<System.Action>();
        private float lastBatchProcessTime;
        
        // 事件响应统计
        private Dictionary<string, int> responseCounts = new Dictionary<string, int>();
        private Dictionary<string, float> averageResponseTimes = new Dictionary<string, float>();
        private DateTime lastEventTime;
        #endregion
        
        #region 事件定义
        // 网格事件处理完成
        public event System.Action<string, string> OnGridEventProcessed;
        // 物品事件处理完成
        public event System.Action<string, string> OnItemEventProcessed;
        // 系统事件处理完成
        public event System.Action<string, string> OnSystemEventProcessed;
        // 批处理完成
        public event System.Action<int> OnBatchProcessed;
        // 所有事件处理完成
        public event System.Action OnAllEventsProcessed;
        #endregion
        
        #region 初始化
        /// <summary>
        /// 初始化事件监听器
        /// </summary>
        public void Initialize()
        {
            if (GridSystemEventManager.Instance != null)
            {
                RegisterEventHandlers();
                Debug.Log("[GridSystemEventListener] 事件监听器已初始化");
            }
            else
            {
                Debug.LogWarning("[GridSystemEventListener] GridSystemEventManager未找到，无法注册事件处理器");
            }
        }
        
        /// <summary>
        /// 注册事件处理器
        /// </summary>
        private void RegisterEventHandlers()
        {
            var eventManager = GridSystemEventManager.Instance;
            
            // 注册网格事件
            eventManager.OnGridRegistered += HandleGridEvent;
            eventManager.OnGridUnregistered += HandleGridEvent;
            eventManager.OnGridStateChanged += HandleGridEvent;
            eventManager.OnGridDataUpdated += HandleGridEvent;
            
            // 注册物品事件
            eventManager.OnItemAdded += HandleItemEvent;
            eventManager.OnItemRemoved += HandleItemEvent;
            eventManager.OnItemMoved += HandleItemEvent;
            eventManager.OnItemStackChanged += HandleItemEvent;
            
            // 注册系统事件
            eventManager.OnSystemPerformanceUpdated += HandleSystemEvent;
            eventManager.OnSystemError += HandleSystemEvent;
            eventManager.OnSystemWarning += HandleSystemEvent;
            eventManager.OnSystemInfo += HandleSystemEvent;
            
            // 注册监控事件
            eventManager.OnMonitoringStarted += HandleMonitoringEvent;
            eventManager.OnMonitoringStopped += HandleMonitoringEvent;
            eventManager.OnStatisticsUpdated += HandleMonitoringEvent;
            eventManager.OnCacheCleared += HandleMonitoringEvent;
            
            // 注册刷新事件
            eventManager.OnRefreshStarted += HandleRefreshEvent;
            eventManager.OnRefreshProgress += HandleRefreshEvent;
            eventManager.OnRefreshCompleted += HandleRefreshEvent;
            eventManager.OnRefreshError += HandleRefreshEvent;
        }
        
        /// <summary>
        /// 注销事件处理器
        /// </summary>
        public void UnregisterEventHandlers()
        {
            var eventManager = GridSystemEventManager.Instance;
            if (eventManager == null) return;
            
            // 注销网格事件
            eventManager.OnGridRegistered -= HandleGridEvent;
            eventManager.OnGridUnregistered -= HandleGridEvent;
            eventManager.OnGridStateChanged -= HandleGridEvent;
            eventManager.OnGridDataUpdated -= HandleGridEvent;
            
            // 注销物品事件
            eventManager.OnItemAdded -= HandleItemEvent;
            eventManager.OnItemRemoved -= HandleItemEvent;
            eventManager.OnItemMoved -= HandleItemEvent;
            eventManager.OnItemStackChanged -= HandleItemEvent;
            
            // 注销系统事件
            eventManager.OnSystemPerformanceUpdated -= HandleSystemEvent;
            eventManager.OnSystemError -= HandleSystemEvent;
            eventManager.OnSystemWarning -= HandleSystemEvent;
            eventManager.OnSystemInfo -= HandleSystemEvent;
            
            // 注销监控事件
            eventManager.OnMonitoringStarted -= HandleMonitoringEvent;
            eventManager.OnMonitoringStopped -= HandleMonitoringEvent;
            eventManager.OnStatisticsUpdated -= HandleMonitoringEvent;
            eventManager.OnCacheCleared -= HandleMonitoringEvent;
            
            // 注销刷新事件
            eventManager.OnRefreshStarted -= HandleRefreshEvent;
            eventManager.OnRefreshProgress -= HandleRefreshEvent;
            eventManager.OnRefreshCompleted -= HandleRefreshEvent;
            eventManager.OnRefreshError -= HandleRefreshEvent;
        }
        #endregion
        
        #region 事件处理方法
        /// <summary>
        /// 处理网格事件
        /// </summary>
        private void HandleGridEvent(string message)
        {
            if (!isListening) return;
            
            var startTime = DateTime.Now;
            
            QueueAction(() => {
                OnGridEventProcessed?.Invoke("Grid", message);
                RecordEventResponse("Grid", startTime);
            });
        }
        
        /// <summary>
        /// 处理物品事件
        /// </summary>
        private void HandleItemEvent(string message)
        {
            if (!isListening) return;
            
            var startTime = DateTime.Now;
            
            QueueAction(() => {
                OnItemEventProcessed?.Invoke("Item", message);
                RecordEventResponse("Item", startTime);
            });
        }
        
        /// <summary>
        /// 处理系统事件
        /// </summary>
        private void HandleSystemEvent(string message)
        {
            if (!isListening) return;
            
            var startTime = DateTime.Now;
            
            QueueAction(() => {
                OnSystemEventProcessed?.Invoke("System", message);
                RecordEventResponse("System", startTime);
            });
        }
        
        /// <summary>
        /// 处理监控事件
        /// </summary>
        private void HandleMonitoringEvent(string message)
        {
            if (!isListening) return;
            
            var startTime = DateTime.Now;
            
            QueueAction(() => {
                OnSystemEventProcessed?.Invoke("Monitoring", message);
                RecordEventResponse("Monitoring", startTime);
            });
        }
        
        /// <summary>
        /// 处理刷新事件
        /// </summary>
        private void HandleRefreshEvent(string message)
        {
            if (!isListening) return;
            
            var startTime = DateTime.Now;
            
            QueueAction(() => {
                OnSystemEventProcessed?.Invoke("Refresh", message);
                RecordEventResponse("Refresh", startTime);
            });
        }
        #endregion
        
        #region 批处理管理
        /// <summary>
        /// 将操作加入队列
        /// </summary>
        private void QueueAction(System.Action action)
        {
            pendingActions.Enqueue(action);
            
            if (enableBatchProcessing)
            {
                // 延迟处理以实现批处理
                EditorApplication.delayCall += ProcessPendingActionsDelayed;
            }
            else
            {
                // 立即处理
                ProcessPendingActions();
            }
        }
        
        /// <summary>
        /// 延迟处理待处理操作
        /// </summary>
        private void ProcessPendingActionsDelayed()
        {
            if (Time.realtimeSinceStartup - lastBatchProcessTime >= batchProcessingDelay)
            {
                ProcessPendingActions();
            }
        }
        
        /// <summary>
        /// 处理待处理操作
        /// </summary>
        public void ProcessPendingActions()
        {
            if (pendingActions.Count == 0) return;
            
            int processedCount = 0;
            int maxProcess = enableBatchProcessing ? maxBatchSize : pendingActions.Count;
            
            while (pendingActions.Count > 0 && processedCount < maxProcess)
            {
                var action = pendingActions.Dequeue();
                try
                {
                    action?.Invoke();
                    processedCount++;
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[GridSystemEventListener] 处理事件时发生错误: {ex.Message}");
                }
            }
            
            lastBatchProcessTime = Time.realtimeSinceStartup;
            
            // 触发批处理完成事件
            OnBatchProcessed?.Invoke(processedCount);
            
            // 如果所有操作都处理完成，触发完成事件
            if (pendingActions.Count == 0)
            {
                OnAllEventsProcessed?.Invoke();
            }
        }
        #endregion
        
        #region 统计和监控
        /// <summary>
        /// 记录事件响应
        /// </summary>
        private void RecordEventResponse(string eventType, DateTime startTime)
        {
            if (!enablePerformanceTracking) return;
            
            // 更新响应次数
            if (responseCounts.ContainsKey(eventType))
            {
                responseCounts[eventType]++;
            }
            else
            {
                responseCounts[eventType] = 1;
            }
            
            // 计算响应时间
            var responseTime = (float)(DateTime.Now - startTime).TotalMilliseconds;
            
            // 更新平均响应时间
            if (averageResponseTimes.ContainsKey(eventType))
            {
                var currentAvg = averageResponseTimes[eventType];
                var count = responseCounts[eventType];
                averageResponseTimes[eventType] = (currentAvg * (count - 1) + responseTime) / count;
            }
            else
            {
                averageResponseTimes[eventType] = responseTime;
            }
            
            lastEventTime = DateTime.Now;
        }
        
        /// <summary>
        /// 重置所有统计信息
        /// </summary>
        public void ResetStatistics()
        {
            responseCounts.Clear();
            averageResponseTimes.Clear();
            lastEventTime = DateTime.MinValue;
            
            Debug.Log("[GridSystemEventListener] 统计信息已重置");
        }
        
        /// <summary>
        /// 开始监听事件
        /// </summary>
        public void StartListening()
        {
            isListening = true;
            Debug.Log("[GridSystemEventListener] 开始监听事件");
        }
        
        /// <summary>
        /// 停止监听事件
        /// </summary>
        public void StopListening()
        {
            isListening = false;
            Debug.Log("[GridSystemEventListener] 停止监听事件");
        }
        
        /// <summary>
        /// 获取待处理操作数量
        /// </summary>
        /// <returns>待处理操作数量</returns>
        public int GetPendingActionsCount()
        {
            return pendingActions.Count;
        }
        
        /// <summary>
        /// 清空待处理操作队列
        /// </summary>
        public void ClearPendingActions()
        {
            pendingActions.Clear();
            Debug.Log("[GridSystemEventListener] 待处理操作队列已清空");
        }
        
        /// <summary>
        /// 获取事件响应次数统计
        /// </summary>
        /// <returns>事件响应次数字典</returns>
        public Dictionary<string, int> GetResponseCounts()
        {
            return new Dictionary<string, int>(responseCounts);
        }
        
        /// <summary>
        /// 获取平均响应时间统计
        /// </summary>
        /// <returns>平均响应时间字典</returns>
        public Dictionary<string, float> GetAverageResponseTimes()
        {
            return new Dictionary<string, float>(averageResponseTimes);
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
        
        private void OnDisable()
        {
            UnregisterEventHandlers();
        }
        #endregion
    }
}