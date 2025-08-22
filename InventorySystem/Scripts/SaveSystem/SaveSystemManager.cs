using UnityEngine;
using UnityEngine.SceneManagement;

namespace InventorySystem.SaveSystem
{
    /// <summary>
    /// 保存系统管理器
    /// 统一管理SaveManager、ItemInstanceIDManager和ItemInstanceIDManagerIntegrator
    /// 确保它们能够正确地跨场景保存
    /// </summary>
    public class SaveSystemManager : MonoBehaviour
    {
        #region 单例模式
        /// <summary>
        /// 单例实例
        /// </summary>
        private static SaveSystemManager _instance;

        /// <summary>
        /// 获取单例实例
        /// </summary>
        public static SaveSystemManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<SaveSystemManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("SaveSystemManager");
                        _instance = go.AddComponent<SaveSystemManager>();

                        // 将新创建的SaveSystemManager设置为SaveSystem的子对象（如果存在SaveSystem）
                        var saveSystemPersistence = FindObjectOfType<SaveSystemPersistence>();
                        if (saveSystemPersistence != null)
                        {
                            go.transform.SetParent(saveSystemPersistence.transform);
                        }
                        // 注意：不调用DontDestroyOnLoad，因为SaveSystemPersistence会处理整个系统的持久化
                    }
                }
                return _instance;
            }
        }
        #endregion

        #region 配置字段
        [Header("保存系统配置")]
        [SerializeField]
        [Tooltip("是否在启动时自动初始化所有组件")]
        private bool autoInitializeOnStart = true;

        [SerializeField]
        [Tooltip("是否启用调试日志")]
        private bool enableDebugLog = true;

        [SerializeField]
        [Tooltip("初始化延迟时间（秒）")]
        private float initializationDelay = 0.5f;
        #endregion

        #region 组件引用
        /// <summary>
        /// 保存管理器实例
        /// </summary>
        public SaveManager SaveManager { get; private set; }

        /// <summary>
        /// 物品实例ID管理器实例
        /// </summary>
        public ItemInstanceIDManager ItemIDManager { get; private set; }

        /// <summary>
        /// 物品实例ID管理器集成器实例
        /// </summary>
        public ItemInstanceIDManagerIntegrator ItemIDIntegrator { get; private set; }
        #endregion

        #region Unity生命周期
        /// <summary>
        /// 组件初始化
        /// </summary>
        private void Awake()
        {
            // 确保单例唯一性
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;

            LogDebug("SaveSystemManager已初始化");
        }

        /// <summary>
        /// 组件启动
        /// </summary>
        private void Start()
        {
            if (autoInitializeOnStart)
            {
                // 延迟初始化，确保所有系统都已准备就绪
                Invoke(nameof(InitializeAllComponents), initializationDelay);
            }
        }

        /// <summary>
        /// 组件销毁
        /// </summary>
        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
        #endregion

        #region 初始化方法
        /// <summary>
        /// 初始化所有保存系统组件
        /// </summary>
        public void InitializeAllComponents()
        {
            LogDebug("开始初始化保存系统组件...");

            // 初始化SaveManager
            InitializeSaveManager();

            // 初始化ItemInstanceIDManager
            InitializeItemIDManager();

            // 初始化ItemInstanceIDManagerIntegrator
            InitializeItemIDIntegrator();

            LogDebug("保存系统组件初始化完成");
        }

        /// <summary>
        /// 初始化保存管理器
        /// </summary>
        private void InitializeSaveManager()
        {
            SaveManager = SaveManager.Instance;
            if (SaveManager != null)
            {
                LogDebug("SaveManager初始化成功");
            }
            else
            {
                Debug.LogError("SaveManager初始化失败");
            }
        }

        /// <summary>
        /// 初始化物品实例ID管理器
        /// </summary>
        private void InitializeItemIDManager()
        {
            ItemIDManager = ItemInstanceIDManager.Instance;
            if (ItemIDManager != null)
            {
                LogDebug("ItemInstanceIDManager初始化成功");
            }
            else
            {
                Debug.LogError("ItemInstanceIDManager初始化失败");
            }
        }

        /// <summary>
        /// 初始化物品实例ID管理器集成器
        /// </summary>
        private void InitializeItemIDIntegrator()
        {
            ItemIDIntegrator = ItemInstanceIDManagerIntegrator.Instance;
            if (ItemIDIntegrator != null)
            {
                LogDebug("ItemInstanceIDManagerIntegrator初始化成功");
            }
            else
            {
                Debug.LogError("ItemInstanceIDManagerIntegrator初始化失败");
            }
        }
        #endregion

        #region 公共方法
        /// <summary>
        /// 手动触发保存
        /// </summary>
        /// <param name="saveSlot">保存槽位名称，默认为"default"</param>
        public void TriggerSave(string saveSlot = "default")
        {
            if (SaveManager != null)
            {
                SaveManager.SaveAll(saveSlot);
                LogDebug($"手动触发保存完成，保存槽位：{saveSlot}");
            }
            else
            {
                Debug.LogError("SaveManager未初始化，无法执行保存");
            }
        }

        /// <summary>
        /// 手动触发加载
        /// </summary>
        /// <param name="saveSlot">保存槽位名称，默认为"default"</param>
        public void TriggerLoad(string saveSlot = "default")
        {
            if (SaveManager != null)
            {
                SaveManager.LoadSave(saveSlot);
                LogDebug($"手动触发加载完成，保存槽位：{saveSlot}");
            }
            else
            {
                Debug.LogError("SaveManager未初始化，无法执行加载");
            }
        }

        /// <summary>
        /// 手动触发ID集成
        /// </summary>
        public void TriggerIDIntegration()
        {
            if (ItemIDIntegrator != null)
            {
                ItemIDIntegrator.PerformAutoIntegration();
                LogDebug("手动触发ID集成完成");
            }
            else
            {
                Debug.LogError("ItemInstanceIDManagerIntegrator未初始化，无法执行集成");
            }
        }

        /// <summary>
        /// 获取保存系统状态
        /// </summary>
        /// <returns>保存系统状态信息</returns>
        public string GetSystemStatus()
        {
            var status = new System.Text.StringBuilder();
            status.AppendLine("=== 保存系统状态 ===");
            status.AppendLine($"SaveManager: {(SaveManager != null ? "已初始化" : "未初始化")}");
            status.AppendLine($"ItemInstanceIDManager: {(ItemIDManager != null ? "已初始化" : "未初始化")}");
            status.AppendLine($"ItemInstanceIDManagerIntegrator: {(ItemIDIntegrator != null ? "已初始化" : "未初始化")}");
            status.AppendLine($"当前场景: {SceneManager.GetActiveScene().name}");
            return status.ToString();
        }
        #endregion

        #region 调试方法
        /// <summary>
        /// 输出调试日志
        /// </summary>
        /// <param name="message">日志消息</param>
        private void LogDebug(string message)
        {
            if (enableDebugLog)
            {
                Debug.Log($"[SaveSystemManager] {message}");
            }
        }
        #endregion
    }
}