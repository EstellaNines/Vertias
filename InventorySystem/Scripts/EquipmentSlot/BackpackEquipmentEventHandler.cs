using System.Collections;
using UnityEngine;

namespace InventorySystem
{
    /// <summary>
    /// 背包装备事件处理器
    /// 
    /// 【核心功能】
    /// 这个组件负责连接BackpackPanelController和EquipmentPersistenceManager，
    /// 实现背包打开/关闭时的装备数据自动保存和加载功能。
    /// 
    /// 【工作原理】
    /// 1. 监听BackpackPanelController的OnEnable/OnDisable事件
    /// 2. 在背包打开时调用EquipmentPersistenceManager加载装备数据
    /// 3. 在背包关闭时调用EquipmentPersistenceManager保存装备数据
    /// 4. 提供运行时模式检测，避免编辑器模式下的误触发
    /// 
    /// 【使用方式】
    /// 将此组件挂载到BackpackPanelController相同的GameObject上，
    /// 或者挂载到背包UI的根节点上。组件会自动查找并连接相关系统。
    /// </summary>
    public class BackpackEquipmentEventHandler : MonoBehaviour
    {
        [Header("事件处理设置")]
        [FieldLabel("启用自动保存")]
        [Tooltip("背包关闭时自动保存装备数据")]
        public bool enableAutoSave = true;
        
        [FieldLabel("启用自动加载")]
        [Tooltip("背包打开时自动加载装备数据")]
        public bool enableAutoLoad = true;
        
        [FieldLabel("延迟加载时间")]
        [Tooltip("背包打开后延迟多少秒开始加载装备数据")]
        [Range(0f, 2f)]
        public float loadDelay = 0.1f;
        
        [FieldLabel("运行时模式检测")]
        [Tooltip("只在运行时模式下触发事件，避免编辑器模式误触发")]
        public bool runtimeModeOnly = true;
        
        [Header("调试设置")]
        [FieldLabel("显示调试日志")]
        public bool showDebugLogs = true;
        
        [FieldLabel("详细事件日志")]
        [Tooltip("显示更详细的事件处理日志")]
        public bool verboseEventLogs = false;
        
        // 组件引用
        private BackpackPanelController backpackController;
        private EquipmentPersistenceManager persistenceManager;
        
        // 状态标志
        private bool isInitialized = false;
        private bool isBackpackOpen = false;
        private int backpackOpenCount = 0; // 防止重复触发
        
        #region Unity生命周期
        
        private void Awake()
        {
            // 确保跨场景持久化
            DontDestroyOnLoad(gameObject);
            InitializeComponents();
        }
        
        private void Start()
        {
            // 延迟初始化以确保BackpackPanelController完全初始化
            if (gameObject.activeInHierarchy)
            {
                StartCoroutine(DelayedInitialization());
            }
            else
            {
                // 如果GameObject未激活，等待激活后再初始化
                StartCoroutine(WaitForActiveAndInitialize());
            }
        }
        
        /// <summary>
        /// 等待GameObject激活后初始化
        /// </summary>
        private System.Collections.IEnumerator WaitForActiveAndInitialize()
        {
            // 等待GameObject激活
            while (!gameObject.activeInHierarchy)
            {
                yield return null;
            }
            
            // GameObject激活后执行延迟初始化
            yield return StartCoroutine(DelayedInitialization());
        }
        
        /// <summary>
        /// 延迟初始化事件处理器
        /// </summary>
        private IEnumerator DelayedInitialization()
        {
            yield return new WaitForSeconds(0.2f); // 等待BackpackPanelController完全初始化
            
            // 重新检测BackpackPanelController
            if (backpackController == null)
            {
                InitializeComponents();
            }
            
            // 如果仍然找不到，再尝试一次更长的延迟
            if (backpackController == null)
            {
                yield return new WaitForSeconds(1.0f);
                backpackController = FindObjectOfType<BackpackPanelController>();
                if (backpackController != null)
                {
                    LogDebug($"延迟初始化找到BackpackPanelController: {backpackController.name}");
                }
            }
            
            InitializeEventHandler();
        }
        
        private void OnDestroy()
        {
            CleanupEventHandler();
        }
        
        private void OnEnable()
        {
            // 背包面板激活时处理
            HandleBackpackOpened();
        }
        
        private void OnDisable()
        {
            // 背包面板停用时处理
            HandleBackpackClosed();
        }
        
        #endregion
        
        #region 初始化
        
        /// <summary>
        /// 初始化组件引用
        /// </summary>
        private void InitializeComponents()
        {
            // 查找BackpackPanelController - 扩大搜索范围
            backpackController = GetComponent<BackpackPanelController>();
            if (backpackController == null)
            {
                // 如果同一GameObject上没有，尝试在父级或子级查找
                backpackController = GetComponentInParent<BackpackPanelController>();
                if (backpackController == null)
                {
                    backpackController = GetComponentInChildren<BackpackPanelController>();
                }
                
                // 如果仍然找不到，尝试在整个场景中查找
                if (backpackController == null)
                {
                    backpackController = FindObjectOfType<BackpackPanelController>();
                }
            }
            
            if (backpackController == null)
            {
                LogWarning("未找到BackpackPanelController组件，将在延迟初始化中重试");
                // 不立即禁用，给延迟初始化一个机会
            }
            else
            {
                LogDebug($"找到BackpackPanelController: {backpackController.name}");
            }
            
            // 获取EquipmentPersistenceManager实例
            persistenceManager = EquipmentPersistenceManager.Instance;
            if (persistenceManager == null)
            {
                LogWarning("未找到EquipmentPersistenceManager实例，将在需要时重试");
            }
            else
            {
                LogDebug("找到EquipmentPersistenceManager实例");
            }
            
            LogDebug("组件引用初始化完成");
        }
        
        /// <summary>
        /// 初始化事件处理器
        /// </summary>
        private void InitializeEventHandler()
        {
            if (backpackController == null)
            {
                LogError("BackpackPanelController为null，无法初始化事件处理器");
                return;
            }
            
            // 确保EquipmentPersistenceManager存在
            if (persistenceManager == null)
            {
                persistenceManager = EquipmentPersistenceManager.Instance;
            }
            
            if (persistenceManager == null)
            {
                LogWarning("EquipmentPersistenceManager仍然为null，功能可能受限");
            }
            
            isInitialized = true;
            LogDebug("背包装备事件处理器初始化完成");
        }
        
        /// <summary>
        /// 清理事件处理器
        /// </summary>
        private void CleanupEventHandler()
        {
            // 如果背包仍然打开，执行最后一次保存
            if (isBackpackOpen && enableAutoSave)
            {
                LogDebug("组件销毁时背包仍打开，执行最后保存");
                SaveEquipmentDataImmediate();
            }
            
            isInitialized = false;
            LogDebug("背包装备事件处理器已清理");
        }
        
        #endregion
        
        #region 事件处理
        
        /// <summary>
        /// 处理背包打开事件
        /// </summary>
        private void HandleBackpackOpened()
        {
            // 运行时模式检测
            if (runtimeModeOnly && !Application.isPlaying)
            {
                LogDebug("编辑器模式下跳过背包打开事件");
                return;
            }
            
            if (!isInitialized)
            {
                LogWarning("事件处理器未初始化，跳过背包打开事件");
                return;
            }
            
            // 防止重复触发
            backpackOpenCount++;
            if (isBackpackOpen)
            {
                LogDebug($"背包已打开，跳过重复事件 (计数: {backpackOpenCount})");
                return;
            }
            
            isBackpackOpen = true;
            LogDebug($"背包打开事件触发 (计数: {backpackOpenCount})");
            
            if (verboseEventLogs)
            {
                LogDebug($"背包状态: 打开, 自动加载: {enableAutoLoad}, 延迟: {loadDelay}s");
            }
            
            // 延迟加载装备数据
            if (enableAutoLoad)
            {
                // 检查EquipmentPersistenceManager是否正在加载，避免冲突
                if (persistenceManager != null && persistenceManager.IsLoading)
                {
                    LogDebug("装备持久化管理器正在加载，跳过背包打开时的加载");
                    return;
                }
                
                if (loadDelay > 0f)
                {
                    Invoke(nameof(LoadEquipmentDataDelayed), loadDelay);
                }
                else
                {
                    LoadEquipmentDataImmediate();
                }
            }
        }
        
        /// <summary>
        /// 处理背包关闭事件
        /// </summary>
        private void HandleBackpackClosed()
        {
            // 运行时模式检测
            if (runtimeModeOnly && !Application.isPlaying)
            {
                LogDebug("编辑器模式下跳过背包关闭事件");
                return;
            }
            
            if (!isInitialized)
            {
                LogWarning("事件处理器未初始化，跳过背包关闭事件");
                return;
            }
            
            if (!isBackpackOpen)
            {
                LogDebug("背包未打开，跳过关闭事件");
                return;
            }
            
            isBackpackOpen = false;
            LogDebug("背包关闭事件触发");
            
            if (verboseEventLogs)
            {
                LogDebug($"背包状态: 关闭, 自动保存: {enableAutoSave}");
            }
            
            // 取消可能的延迟加载
            CancelInvoke(nameof(LoadEquipmentDataDelayed));
            
            // 立即保存装备数据
            if (enableAutoSave)
            {
                SaveEquipmentDataImmediate();
            }
            
            // 重置计数器
            backpackOpenCount = 0;
        }
        
        #endregion
        
        #region 装备数据操作
        
        /// <summary>
        /// 延迟加载装备数据
        /// </summary>
        private void LoadEquipmentDataDelayed()
        {
            LogDebug("执行延迟装备数据加载");
            LoadEquipmentDataImmediate();
        }
        
        /// <summary>
        /// 立即加载装备数据
        /// </summary>
        private void LoadEquipmentDataImmediate()
        {
            if (persistenceManager == null)
            {
                persistenceManager = EquipmentPersistenceManager.Instance;
                if (persistenceManager == null)
                {
                    LogError("无法获取EquipmentPersistenceManager实例，加载失败");
                    return;
                }
            }
            
            // 检查是否需要加载
            if (!ShouldLoadEquipmentData())
            {
                LogDebug("跳过装备数据加载 - 条件不满足");
                return;
            }
            
            LogDebug("开始加载装备数据");
            
            try
            {
                persistenceManager.LoadEquipmentData();
                
                if (verboseEventLogs)
                {
                    bool hasData = persistenceManager.HasSavedData();
                    LogDebug($"装备数据加载请求已发送，存在保存数据: {hasData}");
                }
            }
            catch (System.Exception e)
            {
                LogError($"加载装备数据时出错: {e.Message}");
            }
        }
        
        /// <summary>
        /// 检查是否应该加载装备数据
        /// </summary>
        /// <returns>是否应该加载</returns>
        private bool ShouldLoadEquipmentData()
        {
            // 如果持久化管理器正在加载，跳过
            if (persistenceManager.IsLoading)
            {
                LogDebug("装备持久化管理器正在加载，跳过重复加载");
                return false;
            }
            
            // 如果没有保存数据，跳过
            if (!persistenceManager.HasSavedData())
            {
                LogDebug("没有保存的装备数据，跳过加载");
                return false;
            }
            
            // 检查装备槽是否已经有装备（使用EquipmentSlotManager检查）
            var equipmentSlotManager = EquipmentSlotManager.Instance;
            if (equipmentSlotManager != null)
            {
                var allSlots = equipmentSlotManager.GetAllEquipmentSlots();
                bool hasAnyEquipment = false;
                
                foreach (var kvp in allSlots)
                {
                    if (kvp.Value != null && kvp.Value.HasEquippedItem)
                    {
                        hasAnyEquipment = true;
                        break;
                    }
                }
                
                if (hasAnyEquipment)
                {
                    LogDebug("装备槽中已有装备，跳过重复加载");
                    return false;
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// 立即保存装备数据
        /// </summary>
        private void SaveEquipmentDataImmediate()
        {
            if (persistenceManager == null)
            {
                persistenceManager = EquipmentPersistenceManager.Instance;
                if (persistenceManager == null)
                {
                    LogError("无法获取EquipmentPersistenceManager实例，保存失败");
                    return;
                }
            }
            
            LogDebug("开始保存装备数据");
            
            try
            {
                persistenceManager.SaveEquipmentData();
                
                if (verboseEventLogs)
                {
                    LogDebug("装备数据保存请求已发送");
                }
            }
            catch (System.Exception e)
            {
                LogError($"保存装备数据时出错: {e.Message}");
            }
        }
        
        #endregion
        
        #region 公共接口
        
        /// <summary>
        /// 手动触发装备数据加载
        /// </summary>
        public void ManualLoadEquipmentData()
        {
            LogDebug("手动触发装备数据加载");
            LoadEquipmentDataImmediate();
        }
        
        /// <summary>
        /// 手动触发装备数据保存
        /// </summary>
        public void ManualSaveEquipmentData()
        {
            LogDebug("手动触发装备数据保存");
            SaveEquipmentDataImmediate();
        }
        
        /// <summary>
        /// 强制重新初始化
        /// </summary>
        public void ForceReinitialize()
        {
            LogDebug("强制重新初始化事件处理器");
            CleanupEventHandler();
            InitializeComponents();
            InitializeEventHandler();
        }
        
        /// <summary>
        /// 获取当前状态信息
        /// </summary>
        /// <returns>状态信息字符串</returns>
        public string GetStatusInfo()
        {
            var status = new System.Text.StringBuilder();
            status.AppendLine($"背包事件处理器状态:");
            status.AppendLine($"  初始化: {isInitialized}");
            status.AppendLine($"  背包打开: {isBackpackOpen}");
            status.AppendLine($"  打开计数: {backpackOpenCount}");
            status.AppendLine($"  自动保存: {enableAutoSave}");
            status.AppendLine($"  自动加载: {enableAutoLoad}");
            status.AppendLine($"  加载延迟: {loadDelay}s");
            status.AppendLine($"  运行时模式: {runtimeModeOnly}");
            status.AppendLine($"  BackpackController: {(backpackController != null ? "已连接" : "未找到")}");
            status.AppendLine($"  PersistenceManager: {(persistenceManager != null ? "已连接" : "未找到")}");
            
            return status.ToString();
        }
        
        /// <summary>
        /// 检查是否存在保存的装备数据
        /// </summary>
        /// <returns>是否存在保存数据</returns>
        public bool HasSavedEquipmentData()
        {
            if (persistenceManager == null)
            {
                persistenceManager = EquipmentPersistenceManager.Instance;
            }
            
            return persistenceManager?.HasSavedData() ?? false;
        }
        
        /// <summary>
        /// 获取保存数据的调试信息
        /// </summary>
        /// <returns>调试信息</returns>
        public string GetSavedDataDebugInfo()
        {
            if (persistenceManager == null)
            {
                persistenceManager = EquipmentPersistenceManager.Instance;
            }
            
            return persistenceManager?.GetSavedDataDebugInfo() ?? "无法获取保存数据信息";
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
                Debug.Log($"[BackpackEquipmentEventHandler] {message}");
            }
        }
        
        /// <summary>
        /// 输出警告日志
        /// </summary>
        /// <param name="message">警告信息</param>
        private void LogWarning(string message)
        {
            Debug.LogWarning($"[BackpackEquipmentEventHandler] {message}");
        }
        
        /// <summary>
        /// 输出错误日志
        /// </summary>
        /// <param name="message">错误信息</param>
        private void LogError(string message)
        {
            Debug.LogError($"[BackpackEquipmentEventHandler] {message}");
        }
        
        #endregion
        
        #region 编辑器工具
        
#if UNITY_EDITOR
        /// <summary>
        /// 在Inspector中显示当前状态（仅编辑器模式）
        /// </summary>
        [UnityEditor.MenuItem("Tools/Inventory System/Show Backpack Event Handler Status", false, 200)]
        public static void ShowStatusInConsole()
        {
            var handler = FindObjectOfType<BackpackEquipmentEventHandler>();
            if (handler != null)
            {
                Debug.Log($"=== 背包事件处理器状态 ===\n{handler.GetStatusInfo()}");
                
                if (handler.persistenceManager != null)
                {
                    Debug.Log($"=== 保存数据信息 ===\n{handler.GetSavedDataDebugInfo()}");
                }
            }
            else
            {
                Debug.LogWarning("场景中未找到BackpackEquipmentEventHandler组件");
            }
        }
        
        /// <summary>
        /// 手动触发保存（编辑器工具）
        /// </summary>
        [UnityEditor.MenuItem("Tools/Inventory System/Manual Save Equipment Data", false, 201)]
        public static void ManualSaveFromMenu()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("此功能只能在运行时使用");
                return;
            }
            
            var handler = FindObjectOfType<BackpackEquipmentEventHandler>();
            if (handler != null)
            {
                handler.ManualSaveEquipmentData();
            }
            else
            {
                Debug.LogWarning("场景中未找到BackpackEquipmentEventHandler组件");
            }
        }
        
        /// <summary>
        /// 手动触发加载（编辑器工具）
        /// </summary>
        [UnityEditor.MenuItem("Tools/Inventory System/Manual Load Equipment Data", false, 202)]
        public static void ManualLoadFromMenu()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("此功能只能在运行时使用");
                return;
            }
            
            var handler = FindObjectOfType<BackpackEquipmentEventHandler>();
            if (handler != null)
            {
                handler.ManualLoadEquipmentData();
            }
            else
            {
                Debug.LogWarning("场景中未找到BackpackEquipmentEventHandler组件");
            }
        }
#endif
        
        #endregion
    }
}
