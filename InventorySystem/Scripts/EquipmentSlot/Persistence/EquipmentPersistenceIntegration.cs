using UnityEngine;

namespace InventorySystem
{
    /// <summary>
    /// 装备持久化系统集成组件
    /// 
    /// 【核心功能】
    /// 这是一个一站式集成组件，负责统一管理装备持久化系统的各个部分：
    /// - EquipmentPersistenceManager（数据管理）
    /// - BackpackEquipmentEventHandler（事件处理）
    /// - EquipmentSlotManager（装备槽管理）
    /// 
    /// 【使用方式】
    /// 1. 将此组件挂载到场景中的任意GameObject上
    /// 2. 设置相关参数
    /// 3. 系统会自动初始化并连接所有必要的组件
    /// 
    /// 【自动化功能】
    /// - 自动查找和连接系统组件
    /// - 提供统一的状态监控
    /// - 简化的调试和测试接口
    /// - 运行时问题诊断
    /// </summary>
    public class EquipmentPersistenceIntegration : MonoBehaviour
    {
        [Header("系统设置")]
        [FieldLabel("自动初始化")]
        [Tooltip("启动时自动初始化装备持久化系统")]
        public bool autoInitialize = true;
        
        [FieldLabel("延迟初始化时间")]
        [Tooltip("延迟多少秒开始初始化，确保其他系统先完成")]
        [Range(0f, 5f)]
        public float initializationDelay = 0.5f;
        
        [Header("背包事件设置")]
        [FieldLabel("自动查找背包控制器")]
        [Tooltip("自动在场景中查找BackpackPanelController")]
        public bool autoFindBackpackController = true;
        
        [FieldLabel("手动指定背包控制器")]
        [Tooltip("手动指定的BackpackPanelController，优先于自动查找")]
        public BackpackPanelController manualBackpackController;
        
        [Header("调试和监控")]
        [FieldLabel("显示调试日志")]
        public bool showDebugLogs = true;
        
        [FieldLabel("启用状态监控")]
        [Tooltip("定期检查系统状态并输出报告")]
        public bool enableStatusMonitoring = false;
        
        [FieldLabel("状态监控间隔")]
        [Tooltip("状态监控的间隔时间（秒）")]
        [Range(5f, 60f)]
        public float statusMonitoringInterval = 10f;
        
        // 系统组件引用
        private EquipmentPersistenceManager persistenceManager;
        private EquipmentSlotManager equipmentSlotManager;
        private BackpackPanelController backpackController;
        private BackpackEquipmentEventHandler eventHandler;
        
        // 状态标志
        private bool isInitialized = false;
        private bool initializationFailed = false;
        private float lastStatusCheck = 0f;
        
        #region Unity生命周期
        
        private void Awake()
        {
            // 确保跨场景持久化
            DontDestroyOnLoad(gameObject);
            LogDebug("装备持久化系统集成组件启动");
        }
        
        private void Start()
        {
            if (autoInitialize)
            {
                if (initializationDelay > 0f)
                {
                    Invoke(nameof(InitializeSystem), initializationDelay);
                }
                else
                {
                    InitializeSystem();
                }
            }
        }
        
        private void Update()
        {
            // 状态监控
            if (enableStatusMonitoring && isInitialized)
            {
                if (Time.time - lastStatusCheck >= statusMonitoringInterval)
                {
                    MonitorSystemStatus();
                    lastStatusCheck = Time.time;
                }
            }
        }
        
        private void OnDestroy()
        {
            LogDebug("装备持久化系统集成组件销毁");
        }
        
        #endregion
        
        #region 系统初始化
        
        /// <summary>
        /// 初始化装备持久化系统
        /// </summary>
        public void InitializeSystem()
        {
            LogDebug("开始初始化装备持久化系统...");
            
            if (isInitialized)
            {
                LogWarning("系统已初始化，跳过重复初始化");
                return;
            }
            
            try
            {
                // 1. 初始化核心管理器
                InitializePersistenceManager();
                
                // 2. 初始化装备槽管理器
                InitializeEquipmentSlotManager();
                
                // 3. 查找背包控制器
                FindBackpackController();
                
                // 4. 设置事件处理器
                SetupEventHandler();
                
                // 5. 验证初始化结果
                bool success = ValidateInitialization();
                
                if (success)
                {
                    isInitialized = true;
                    initializationFailed = false;
                    LogDebug("�7�3 装备持久化系统初始化成功");
                    
                    // 输出系统状态摘要
                    if (showDebugLogs)
                    {
                        LogDebug($"系统状态摘要:\n{GetSystemStatusSummary()}");
                    }
                }
                else
                {
                    initializationFailed = true;
                    LogError("�7�4 装备持久化系统初始化失败");
                }
            }
            catch (System.Exception e)
            {
                initializationFailed = true;
                LogError($"初始化过程中发生异常: {e.Message}\n{e.StackTrace}");
            }
        }
        
        /// <summary>
        /// 初始化持久化管理器
        /// </summary>
        private void InitializePersistenceManager()
        {
            persistenceManager = EquipmentPersistenceManager.Instance;
            if (persistenceManager == null)
            {
                LogWarning("未找到EquipmentPersistenceManager，将自动创建");
                // 实例属性访问会自动创建实例
                persistenceManager = EquipmentPersistenceManager.Instance;
            }
            
            LogDebug("持久化管理器初始化完成");
        }
        
        /// <summary>
        /// 初始化装备槽管理器
        /// </summary>
        private void InitializeEquipmentSlotManager()
        {
            equipmentSlotManager = EquipmentSlotManager.Instance;
            if (equipmentSlotManager == null)
            {
                LogWarning("未找到EquipmentSlotManager，将自动创建");
                equipmentSlotManager = EquipmentSlotManager.Instance;
            }
            
            LogDebug("装备槽管理器初始化完成");
        }
        
        /// <summary>
        /// 查找背包控制器
        /// </summary>
        private void FindBackpackController()
        {
            // 优先使用手动指定的控制器
            if (manualBackpackController != null)
            {
                backpackController = manualBackpackController;
                LogDebug("使用手动指定的背包控制器");
                return;
            }
            
            // 自动查找背包控制器
            if (autoFindBackpackController)
            {
                backpackController = FindObjectOfType<BackpackPanelController>();
                if (backpackController != null)
                {
                    LogDebug($"自动找到背包控制器: {backpackController.name}");
                }
                else
                {
                    LogWarning("未找到BackpackPanelController，背包事件功能将不可用");
                }
            }
        }
        
        /// <summary>
        /// 设置事件处理器
        /// </summary>
        private void SetupEventHandler()
        {
            if (backpackController == null)
            {
                LogWarning("没有背包控制器，跳过事件处理器设置");
                return;
            }
            
            // 检查是否已存在事件处理器
            eventHandler = backpackController.GetComponent<BackpackEquipmentEventHandler>();
            
            if (eventHandler == null)
            {
                // 自动添加事件处理器
                eventHandler = backpackController.gameObject.AddComponent<BackpackEquipmentEventHandler>();
                LogDebug("自动添加BackpackEquipmentEventHandler组件");
            }
            else
            {
                LogDebug("发现现有的BackpackEquipmentEventHandler组件");
            }
            
            // 配置事件处理器
            if (eventHandler != null)
            {
                eventHandler.showDebugLogs = showDebugLogs;
                LogDebug("事件处理器配置完成");
            }
        }
        
        /// <summary>
        /// 验证初始化结果
        /// </summary>
        /// <returns>是否初始化成功</returns>
        private bool ValidateInitialization()
        {
            bool success = true;
            var issues = new System.Collections.Generic.List<string>();
            
            // 检查核心组件
            if (persistenceManager == null)
            {
                issues.Add("EquipmentPersistenceManager未初始化");
                success = false;
            }
            
            if (equipmentSlotManager == null)
            {
                issues.Add("EquipmentSlotManager未初始化");
                success = false;
            }
            
            // 检查可选组件
            if (backpackController == null)
            {
                issues.Add("BackpackPanelController未找到（背包事件功能不可用）");
                // 这不是致命错误，不影响success
            }
            
            if (eventHandler == null && backpackController != null)
            {
                issues.Add("BackpackEquipmentEventHandler未设置（背包事件功能不可用）");
                // 这不是致命错误，不影响success
            }
            
            // 输出问题列表
            if (issues.Count > 0)
            {
                LogWarning($"初始化验证发现问题:\n- {string.Join("\n- ", issues)}");
            }
            
            return success;
        }
        
        #endregion
        
        #region 状态监控
        
        /// <summary>
        /// 监控系统状态
        /// </summary>
        private void MonitorSystemStatus()
        {
            if (!isInitialized) return;
            
            var issues = new System.Collections.Generic.List<string>();
            
            // 检查持久化管理器状态
            if (persistenceManager != null)
            {
                var (isReady, statusMessage) = persistenceManager.CheckSystemStatus();
                if (!isReady)
                {
                    issues.Add($"持久化管理器: {statusMessage}");
                }
            }
            else
            {
                issues.Add("持久化管理器丢失");
            }
            
            // 检查装备槽管理器
            if (equipmentSlotManager == null)
            {
                issues.Add("装备槽管理器丢失");
            }
            
            // 输出监控结果
            if (issues.Count > 0)
            {
                LogWarning($"系统状态监控发现问题:\n- {string.Join("\n- ", issues)}");
            }
            else
            {
                LogDebug("系统状态监控: 所有组件运行正常");
            }
        }
        
        #endregion
        
        #region 公共接口
        
        /// <summary>
        /// 手动保存装备数据
        /// </summary>
        public void ManualSaveEquipmentData()
        {
            if (persistenceManager != null)
            {
                LogDebug("手动触发装备数据保存");
                persistenceManager.SaveEquipmentData();
            }
            else
            {
                LogError("持久化管理器不存在，无法保存");
            }
        }
        
        /// <summary>
        /// 手动加载装备数据
        /// </summary>
        public void ManualLoadEquipmentData()
        {
            if (persistenceManager != null)
            {
                LogDebug("手动触发装备数据加载");
                persistenceManager.LoadEquipmentData();
            }
            else
            {
                LogError("持久化管理器不存在，无法加载");
            }
        }
        
        /// <summary>
        /// 清除保存的装备数据
        /// </summary>
        public void ClearSavedEquipmentData()
        {
            if (persistenceManager != null)
            {
                LogDebug("清除保存的装备数据");
                persistenceManager.ClearSavedData();
            }
            else
            {
                LogError("持久化管理器不存在，无法清除数据");
            }
        }
        
        /// <summary>
        /// 强制重新初始化系统
        /// </summary>
        public void ForceReinitialize()
        {
            LogDebug("强制重新初始化系统");
            isInitialized = false;
            initializationFailed = false;
            InitializeSystem();
        }
        
        /// <summary>
        /// 获取系统状态摘要
        /// </summary>
        /// <returns>状态摘要</returns>
        public string GetSystemStatusSummary()
        {
            var summary = new System.Text.StringBuilder();
            summary.AppendLine("=== 装备持久化系统状态 ===");
            summary.AppendLine($"集成组件初始化: {isInitialized}");
            summary.AppendLine($"初始化失败: {initializationFailed}");
            summary.AppendLine("");
            
            if (persistenceManager != null)
            {
                summary.AppendLine(persistenceManager.GetSystemStatusSummary());
            }
            else
            {
                summary.AppendLine("持久化管理器: 未找到");
            }
            summary.AppendLine("");
            
            if (equipmentSlotManager != null)
            {
                var stats = equipmentSlotManager.GetEquipmentStatistics();
                summary.AppendLine($"装备槽统计: {stats.equippedSlots}/{stats.totalSlots} 已装备");
            }
            else
            {
                summary.AppendLine("装备槽管理器: 未找到");
            }
            summary.AppendLine("");
            
            if (eventHandler != null)
            {
                summary.AppendLine(eventHandler.GetStatusInfo());
            }
            else
            {
                summary.AppendLine("背包事件处理器: 未找到");
            }
            
            return summary.ToString();
        }
        
        /// <summary>
        /// 检查系统是否就绪
        /// </summary>
        /// <returns>是否就绪</returns>
        public bool IsSystemReady()
        {
            return isInitialized && !initializationFailed && 
                   persistenceManager != null && equipmentSlotManager != null;
        }
        
        #endregion
        
        #region 调试和日志
        
        /// <summary>
        /// 输出调试日志
        /// </summary>
        /// <param name="message">日志信息</param>
        private void LogDebug(string message)
        {
            if (showDebugLogs)
            {
                Debug.Log($"[EquipmentPersistenceIntegration] {message}");
            }
        }
        
        /// <summary>
        /// 输出警告日志
        /// </summary>
        /// <param name="message">警告信息</param>
        private void LogWarning(string message)
        {
            Debug.LogWarning($"[EquipmentPersistenceIntegration] {message}");
        }
        
        /// <summary>
        /// 输出错误日志
        /// </summary>
        /// <param name="message">错误信息</param>
        private void LogError(string message)
        {
            Debug.LogError($"[EquipmentPersistenceIntegration] {message}");
        }
        
        #endregion
        
        #region 编辑器工具
        
#if UNITY_EDITOR
        /// <summary>
        /// 显示系统状态（编辑器菜单）
        /// </summary>
        [UnityEditor.MenuItem("Tools/Inventory System/Show Equipment Persistence Status", false, 100)]
        public static void ShowSystemStatus()
        {
            var integration = FindObjectOfType<EquipmentPersistenceIntegration>();
            if (integration != null)
            {
                Debug.Log(integration.GetSystemStatusSummary());
            }
            else
            {
                Debug.LogWarning("场景中未找到EquipmentPersistenceIntegration组件");
            }
        }
        
        /// <summary>
        /// 强制初始化系统（编辑器菜单）
        /// </summary>
        [UnityEditor.MenuItem("Tools/Inventory System/Force Initialize Equipment Persistence", false, 101)]
        public static void ForceInitializeSystem()
        {
            var integration = FindObjectOfType<EquipmentPersistenceIntegration>();
            if (integration != null)
            {
                integration.ForceReinitialize();
            }
            else
            {
                Debug.LogWarning("场景中未找到EquipmentPersistenceIntegration组件");
            }
        }
#endif
        
        #endregion
    }
}
