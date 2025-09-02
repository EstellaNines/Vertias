using UnityEngine;
using InventorySystem.SpawnSystem;

namespace InventorySystem.SpawnSystem
{
    /// <summary>
    /// 生成系统管理器
    /// 负责整个生成系统的初始化和全局管理
    /// </summary>
    [AddComponentMenu("Inventory System/Spawn System/Spawn System Manager")]
    public class SpawnSystemManager : MonoBehaviour
    {
        [Header("全局配置")]
        [FieldLabel("自动初始化")]
        [Tooltip("游戏开始时是否自动初始化生成系统")]
        [SerializeField] private bool autoInitialize = true;
        
        [FieldLabel("启用调试日志")]
        [Tooltip("是否启用全局调试日志")]
        [SerializeField] private bool enableGlobalDebugLog = false;
        
        [Header("默认配置")]
        [FieldLabel("仓库生成配置")]
        [Tooltip("仓库的默认生成配置")]
        [SerializeField] private FixedItemSpawnConfig warehouseConfig;
        
        [FieldLabel("地面生成配置")]
        [Tooltip("地面的默认生成配置")]
        [SerializeField] private FixedItemSpawnConfig groundConfig;
        
        [FieldLabel("背包生成配置")]
        [Tooltip("背包的默认生成配置")]
        [SerializeField] private FixedItemSpawnConfig backpackConfig;
        
        [Header("集成设置")]
        [FieldLabel("启用自动生成")]
        [Tooltip("是否启用网格加载后的自动生成")]
        [SerializeField] private bool enableAutoSpawn = true;
        
        [FieldLabel("生成时机")]
        [Tooltip("自动生成的触发时机")]
        [SerializeField] private SpawnTiming autoSpawnTiming = SpawnTiming.ContainerFirstOpen;
        
        private static SpawnSystemManager instance;
        private SpawnSystemIntegration integration;
        private FixedItemSpawnManager spawnManager;
        
        #region 单例模式
        
        public static SpawnSystemManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = UnityEngine.Object.FindObjectOfType<SpawnSystemManager>();
                    if (instance == null)
                    {
                        var go = new GameObject("SpawnSystemManager");
                        instance = go.AddComponent<SpawnSystemManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return instance;
            }
        }
        
        #endregion
        
        #region 生命周期
        
        private void Awake()
        {
            // 单例处理
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                
                if (autoInitialize)
                {
                    InitializeSpawnSystem();
                }
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }
        
        private void Start()
        {
            if (instance == this)
            {
                SetupDefaultConfigurations();
            }
        }
        
        #endregion
        
        #region 初始化方法
        
        /// <summary>
        /// 初始化生成系统
        /// </summary>
        [ContextMenu("初始化生成系统")]
        public void InitializeSpawnSystem()
        {
            LogDebug("开始初始化固定物品生成系统");
            
            // 初始化主管理器
            spawnManager = FixedItemSpawnManager.Instance;
            
            // 设置全局调试日志
            if (spawnManager != null)
            {
                // 注意：这里需要通过反射或其他方式设置私有字段
                // 或者在FixedItemSpawnManager中添加公共方法来设置调试模式
            }
            
            // 初始化集成组件
            InitializeIntegration();
            
            LogDebug("固定物品生成系统初始化完成");
        }
        
        /// <summary>
        /// 初始化集成组件
        /// </summary>
        private void InitializeIntegration()
        {
            // 查找或创建集成组件
            integration = GetComponent<SpawnSystemIntegration>();
            if (integration == null)
            {
                integration = gameObject.AddComponent<SpawnSystemIntegration>();
            }
            
            // 配置集成组件（通过反射设置私有字段，或使用公共方法）
            #if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                var integrationSerializedObject = new UnityEditor.SerializedObject(integration);
                integrationSerializedObject.FindProperty("enableAutoSpawn").boolValue = enableAutoSpawn;
                integrationSerializedObject.FindProperty("autoSpawnTiming").intValue = (int)autoSpawnTiming;
                integrationSerializedObject.FindProperty("warehouseSpawnConfig").objectReferenceValue = warehouseConfig;
                integrationSerializedObject.FindProperty("groundSpawnConfig").objectReferenceValue = groundConfig;
                integrationSerializedObject.FindProperty("enableDebugLog").boolValue = enableGlobalDebugLog;
                integrationSerializedObject.ApplyModifiedProperties();
            }
            #endif
        }
        
        /// <summary>
        /// 设置默认配置
        /// </summary>
        private void SetupDefaultConfigurations()
        {
            if (integration == null) return;
            
            // 注册默认配置
            if (warehouseConfig != null)
            {
                integration.RegisterSpawnConfig("warehouse", warehouseConfig);
                integration.RegisterSpawnConfig("warehouse_main", warehouseConfig);
                LogDebug("已注册仓库生成配置");
            }
            
            if (groundConfig != null)
            {
                integration.RegisterSpawnConfig("ground", groundConfig);
                integration.RegisterSpawnConfig("ground_main", groundConfig);
                LogDebug("已注册地面生成配置");
            }
            
            if (backpackConfig != null)
            {
                integration.RegisterSpawnConfig("backpack", backpackConfig);
                integration.RegisterSpawnConfig("player_backpack", backpackConfig);
                LogDebug("已注册背包生成配置");
            }
        }
        
        #endregion
        
        #region 公共接口
        
        /// <summary>
        /// 注册生成配置
        /// </summary>
        public void RegisterSpawnConfig(string containerId, FixedItemSpawnConfig config)
        {
            if (integration != null)
            {
                integration.RegisterSpawnConfig(containerId, config);
                LogDebug($"已注册生成配置: {containerId}");
            }
        }
        
        /// <summary>
        /// 手动触发生成
        /// </summary>
        public void TriggerSpawn(ItemGrid targetGrid, string containerId = null)
        {
            if (integration != null)
            {
                integration.TriggerSpawnForContainer(targetGrid, containerId);
            }
        }
        
        /// <summary>
        /// 重置所有生成状态
        /// </summary>
        [ContextMenu("重置所有生成状态")]
        public void ResetAllSpawnStates()
        {
            if (spawnManager != null)
            {
                spawnManager.ResetAllSpawnStates();
                LogDebug("已重置所有生成状态");
            }
        }
        
        /// <summary>
        /// 获取系统状态
        /// </summary>
        [ContextMenu("显示系统状态")]
        public void ShowSystemStatus()
        {
            string status = "=== 生成系统状态 ===\n";
            
            if (spawnManager != null)
            {
                status += spawnManager.GetManagerStatistics() + "\n";
            }
            
            if (integration != null)
            {
                status += integration.GetIntegrationStatus();
            }
            
            LogDebug(status);
            
            #if UNITY_EDITOR
            UnityEditor.EditorUtility.DisplayDialog("系统状态", status, "确定");
            #endif
        }
        
        #endregion
        
        #region 配置管理
        
        /// <summary>
        /// 创建默认仓库配置
        /// </summary>
        [ContextMenu("创建默认仓库配置")]
        public void CreateDefaultWarehouseConfig()
        {
            #if UNITY_EDITOR
            var config = ScriptableObject.CreateInstance<FixedItemSpawnConfig>();
            config.configName = "默认仓库配置";
            config.description = "系统自动生成的默认仓库物品配置";
            config.targetContainerType = ContainerType.Warehouse;
            config.spawnTiming = SpawnTiming.ContainerFirstOpen;
            
            string path = "Assets/InventorySystem/Configs/DefaultWarehouseSpawnConfig.asset";
            UnityEditor.AssetDatabase.CreateAsset(config, path);
            UnityEditor.AssetDatabase.SaveAssets();
            
            warehouseConfig = config;
            LogDebug($"已创建默认仓库配置: {path}");
            #endif
        }
        
        /// <summary>
        /// 创建默认地面配置
        /// </summary>
        [ContextMenu("创建默认地面配置")]
        public void CreateDefaultGroundConfig()
        {
            #if UNITY_EDITOR
            var config = ScriptableObject.CreateInstance<FixedItemSpawnConfig>();
            config.configName = "默认地面配置";
            config.description = "系统自动生成的默认地面物品配置";
            config.targetContainerType = ContainerType.Ground;
            config.spawnTiming = SpawnTiming.GameStart;
            
            string path = "Assets/InventorySystem/Configs/DefaultGroundSpawnConfig.asset";
            UnityEditor.AssetDatabase.CreateAsset(config, path);
            UnityEditor.AssetDatabase.SaveAssets();
            
            groundConfig = config;
            LogDebug($"已创建默认地面配置: {path}");
            #endif
        }
        
        #endregion
        
        #region 调试方法
        
        private void LogDebug(string message)
        {
            if (enableGlobalDebugLog)
            {
                Debug.Log($"SpawnSystemManager: {message}");
            }
        }
        
        #endregion
        
        #region Editor支持
        
        #if UNITY_EDITOR
        [UnityEditor.MenuItem("Inventory System/Spawn System/Create Spawn System Manager")]
        public static void CreateSpawnSystemManager()
        {
            var existing = UnityEngine.Object.FindObjectOfType<SpawnSystemManager>();
            if (existing != null)
            {
                UnityEditor.Selection.activeGameObject = existing.gameObject;
                UnityEditor.EditorUtility.DisplayDialog("提示", "场景中已存在SpawnSystemManager", "确定");
                return;
            }
            
            var go = new GameObject("SpawnSystemManager");
            var manager = go.AddComponent<SpawnSystemManager>();
            UnityEditor.Selection.activeGameObject = go;
            
            UnityEditor.EditorUtility.DisplayDialog("创建完成", 
                "SpawnSystemManager已创建。请在Inspector中配置生成规则。", "确定");
        }
        
        [UnityEditor.MenuItem("Inventory System/Spawn System/Reset All Spawn States")]
        public static void MenuResetAllSpawnStates()
        {
            if (UnityEditor.EditorUtility.DisplayDialog("确认重置", 
                "这将重置所有固定物品的生成状态，确定继续？", "确定", "取消"))
            {
                var manager = Instance;
                manager.ResetAllSpawnStates();
                Debug.Log("已重置所有生成状态");
            }
        }
        #endif
        
        #endregion
    }
}
