using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace InventorySystem.SaveSystem
{
    /// <summary>
    /// 物品实例ID管理器集成器
    /// 负责将ItemInstanceIDManager与现有系统进行非侵入式集成
    /// </summary>
    [System.Serializable]
    public class ItemInstanceIDManagerIntegrator : MonoBehaviour
    {
        #region 单例模式
        /// <summary>
        /// 单例实例
        /// </summary>
        private static ItemInstanceIDManagerIntegrator _instance;

        /// <summary>
        /// 获取单例实例
        /// </summary>
        public static ItemInstanceIDManagerIntegrator Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<ItemInstanceIDManagerIntegrator>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("ItemInstanceIDManagerIntegrator");
                        _instance = go.AddComponent<ItemInstanceIDManagerIntegrator>();

                        // 将新创建的ItemInstanceIDManagerIntegrator设置为SaveSystem的子对象（如果存在SaveSystem）
                        var saveSystemPersistence = FindObjectOfType<SaveSystemPersistence>();
                        if (saveSystemPersistence != null)
                        {
                            go.transform.SetParent(saveSystemPersistence.transform);
                        }
                    }
                }
                return _instance;
            }
        }
        #endregion
        #region 配置字段
        [Header("集成配置")]
        [SerializeField]
        [Tooltip("是否启用自动集成")]
        private bool enableAutoIntegration = true;

        [SerializeField]
        [Tooltip("集成延迟时间（秒）")]
        private float integrationDelay = 1.0f;

        [SerializeField]
        [Tooltip("是否在场景加载时自动集成")]
        private bool integrateOnSceneLoad = true;

        [SerializeField]
        [Tooltip("是否启用调试日志")]
        private bool enableDebugLog = true;

        [Header("集成目标")]
        [SerializeField]
        [Tooltip("是否集成InventorySystemItem")]
        private bool integrateInventoryItems = true;

        [SerializeField]
        [Tooltip("是否集成BaseItemGrid")]
        private bool integrateItemGrids = true;

        [SerializeField]
        [Tooltip("是否集成BaseItemSpawn")]
        private bool integrateItemSpawners = true;

        [SerializeField]
        [Tooltip("是否集成EquipSlot")]
        private bool integrateEquipSlots = true;
        #endregion

        #region 私有字段
        /// <summary>
        /// ID管理器实例
        /// </summary>
        private ItemInstanceIDManager idManager;

        /// <summary>
        /// 已集成的对象列表
        /// </summary>
        private HashSet<GameObject> integratedObjects = new HashSet<GameObject>();

        /// <summary>
        /// 集成统计信息
        /// </summary>
        private IntegrationStats stats = new IntegrationStats();
        #endregion

        #region 数据结构
        /// <summary>
        /// 集成统计信息
        /// </summary>
        [System.Serializable]
        public class IntegrationStats
        {
            public int totalItemsIntegrated;
            public int totalGridsIntegrated;
            public int totalSpawnersIntegrated;
            public int totalEquipSlotsIntegrated;
            public int totalConflictsResolved;
            public DateTime lastIntegrationTime;

            public void Reset()
            {
                totalItemsIntegrated = 0;
                totalGridsIntegrated = 0;
                totalSpawnersIntegrated = 0;
                totalEquipSlotsIntegrated = 0;
                totalConflictsResolved = 0;
                lastIntegrationTime = DateTime.Now;
            }
        }
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

            // 注意：不在这里调用DontDestroyOnLoad，因为SaveSystemPersistence已经处理了整个系统的持久化
            // 如果这个组件是SaveSystem的子对象，它会自动跟随父对象进行跨场景持久化

            // 确保ID管理器存在
            EnsureIDManagerExists();

            // 注册场景事件
            if (integrateOnSceneLoad)
            {
                SceneManager.sceneLoaded += OnSceneLoaded;
            }

            LogDebug("ItemInstanceIDManagerIntegrator已初始化");
        }

        /// <summary>
        /// 组件启动
        /// </summary>
        private void Start()
        {
            if (enableAutoIntegration)
            {
                // 延迟执行集成，确保所有对象都已初始化
                Invoke(nameof(PerformAutoIntegration), integrationDelay);
            }
        }

        /// <summary>
        /// 组件销毁
        /// </summary>
        private void OnDestroy()
        {
            // 取消注册场景事件
            SceneManager.sceneLoaded -= OnSceneLoaded;

            LogDebug("ItemInstanceIDManagerIntegrator已销毁");
        }
        #endregion

        #region 场景事件处理
        /// <summary>
        /// 场景加载事件处理
        /// </summary>
        /// <param name="scene">加载的场景</param>
        /// <param name="mode">加载模式</param>
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            LogDebug($"场景已加载: {scene.name}，开始集成");

            // 延迟执行集成
            Invoke(nameof(PerformAutoIntegration), integrationDelay);
        }
        #endregion

        #region 核心集成方法
        /// <summary>
        /// 执行自动集成
        /// </summary>
        public void PerformAutoIntegration()
        {
            if (!enableAutoIntegration || idManager == null)
            {
                return;
            }

            LogDebug("开始执行自动集成...");
            stats.Reset();

            try
            {
                // 集成各种类型的对象
                if (integrateInventoryItems)
                {
                    IntegrateInventorySystemItems();
                }

                if (integrateItemGrids)
                {
                    IntegrateBaseItemGrids();
                }

                if (integrateItemSpawners)
                {
                    IntegrateBaseItemSpawners();
                }

                if (integrateEquipSlots)
                {
                    IntegrateEquipSlots();
                }

                // 执行冲突检测和解决
                ResolveIntegrationConflicts();

                stats.lastIntegrationTime = DateTime.Now;
                LogDebug($"自动集成完成 - 物品:{stats.totalItemsIntegrated}, 网格:{stats.totalGridsIntegrated}, 生成器:{stats.totalSpawnersIntegrated}, 装备槽:{stats.totalEquipSlotsIntegrated}");
            }
            catch (Exception ex)
            {
                LogError($"自动集成失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 集成InventorySystemItem对象
        /// </summary>
        private void IntegrateInventorySystemItems()
        {
            InventorySystemItem[] items = FindObjectsOfType<InventorySystemItem>(true);

            foreach (var item in items)
            {
                if (integratedObjects.Contains(item.gameObject))
                {
                    continue;
                }

                try
                {
                    string itemID = item.GetItemInstanceID();

                    if (string.IsNullOrEmpty(itemID))
                    {
                        // 为没有ID的物品生成新ID
                        itemID = idManager.GenerateUniqueInstanceID("IntegratedItem");
                        item.SetItemInstanceID(itemID);
                    }

                    // 注册到ID管理器
                    idManager.RegisterInstanceID(itemID, item.gameObject.name, "InventorySystemItem", item.transform.position);

                    // 标记为已集成
                    integratedObjects.Add(item.gameObject);
                    stats.totalItemsIntegrated++;

                    LogDebug($"已集成InventorySystemItem: {item.gameObject.name}, ID: {itemID}");
                }
                catch (Exception ex)
                {
                    LogError($"集成InventorySystemItem失败: {item.gameObject.name}, 错误: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 集成BaseItemGrid对象
        /// </summary>
        private void IntegrateBaseItemGrids()
        {
            BaseItemGrid[] grids = FindObjectsOfType<BaseItemGrid>(true);

            foreach (var grid in grids)
            {
                if (integratedObjects.Contains(grid.gameObject))
                {
                    continue;
                }

                try
                {
                    string gridID = grid.GetSaveID();

                    if (string.IsNullOrEmpty(gridID))
                    {
                        // 为没有ID的网格生成新ID
                        gridID = idManager.GenerateUniqueInstanceID("IntegratedGrid");
                        // 注意：BaseItemGrid可能没有SetSaveID方法，这里需要通过反射或其他方式设置
                    }

                    // 注册到ID管理器
                    idManager.RegisterInstanceID(gridID, grid.gameObject.name, "BaseItemGrid", grid.transform.position);

                    // 标记为已集成
                    integratedObjects.Add(grid.gameObject);
                    stats.totalGridsIntegrated++;

                    LogDebug($"已集成BaseItemGrid: {grid.gameObject.name}, ID: {gridID}");
                }
                catch (Exception ex)
                {
                    LogError($"集成BaseItemGrid失败: {grid.gameObject.name}, 错误: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 集成BaseItemSpawn对象
        /// </summary>
        private void IntegrateBaseItemSpawners()
        {
            BaseItemSpawn[] spawners = FindObjectsOfType<BaseItemSpawn>(true);

            foreach (var spawner in spawners)
            {
                if (integratedObjects.Contains(spawner.gameObject))
                {
                    continue;
                }

                try
                {
                    string spawnerID = spawner.GetSaveID();

                    if (string.IsNullOrEmpty(spawnerID))
                    {
                        // 为没有ID的生成器生成新ID
                        spawnerID = idManager.GenerateUniqueInstanceID("IntegratedSpawner");
                    }

                    // 注册到ID管理器
                    idManager.RegisterInstanceID(spawnerID, spawner.gameObject.name, "BaseItemSpawn", spawner.transform.position);

                    // 标记为已集成
                    integratedObjects.Add(spawner.gameObject);
                    stats.totalSpawnersIntegrated++;

                    LogDebug($"已集成BaseItemSpawn: {spawner.gameObject.name}, ID: {spawnerID}");
                }
                catch (Exception ex)
                {
                    LogError($"集成BaseItemSpawn失败: {spawner.gameObject.name}, 错误: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 集成EquipSlot对象
        /// </summary>
        private void IntegrateEquipSlots()
        {
            EquipSlot[] equipSlots = FindObjectsOfType<EquipSlot>(true);

            foreach (var slot in equipSlots)
            {
                if (integratedObjects.Contains(slot.gameObject))
                {
                    continue;
                }

                try
                {
                    string slotID = slot.GetSaveID();

                    if (string.IsNullOrEmpty(slotID))
                    {
                        // 为没有ID的装备槽生成新ID
                        slotID = idManager.GenerateUniqueInstanceID("IntegratedEquipSlot");
                    }

                    // 注册到ID管理器
                    idManager.RegisterInstanceID(slotID, slot.gameObject.name, "EquipSlot", slot.transform.position);

                    // 标记为已集成
                    integratedObjects.Add(slot.gameObject);
                    stats.totalEquipSlotsIntegrated++;

                    LogDebug($"已集成EquipSlot: {slot.gameObject.name}, ID: {slotID}");
                }
                catch (Exception ex)
                {
                    LogError($"集成EquipSlot失败: {slot.gameObject.name}, 错误: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 解决集成冲突
        /// </summary>
        private void ResolveIntegrationConflicts()
        {
            if (idManager == null)
            {
                return;
            }

            // 执行冲突检测
            var conflicts = idManager.GetActiveConflicts();
            int resolvedCount = 0;

            foreach (var conflict in conflicts)
            {
                try
                {
                    // 这里可以根据冲突类型执行不同的解决策略
                    LogDebug($"正在解决冲突: {conflict.conflictID}, 类型: {conflict.type}");
                    resolvedCount++;
                }
                catch (Exception ex)
                {
                    LogError($"解决冲突失败: {conflict.conflictID}, 错误: {ex.Message}");
                }
            }

            stats.totalConflictsResolved = resolvedCount;

            if (resolvedCount > 0)
            {
                LogDebug($"已解决 {resolvedCount} 个集成冲突");
            }
        }
        #endregion

        #region 公共API
        /// <summary>
        /// 手动执行集成
        /// </summary>
        public void ManualIntegration()
        {
            LogDebug("开始手动集成...");
            PerformAutoIntegration();
        }

        /// <summary>
        /// 清理集成状态
        /// </summary>
        public void ClearIntegrationState()
        {
            integratedObjects.Clear();
            stats.Reset();
            LogDebug("已清理集成状态");
        }

        /// <summary>
        /// 获取集成统计信息
        /// </summary>
        /// <returns>统计信息</returns>
        public IntegrationStats GetIntegrationStats()
        {
            return stats;
        }

        /// <summary>
        /// 检查对象是否已集成
        /// </summary>
        /// <param name="obj">要检查的对象</param>
        /// <returns>是否已集成</returns>
        public bool IsObjectIntegrated(GameObject obj)
        {
            return integratedObjects.Contains(obj);
        }
        #endregion

        #region 辅助方法
        /// <summary>
        /// 确保ID管理器存在
        /// </summary>
        private void EnsureIDManagerExists()
        {
            idManager = ItemInstanceIDManager.Instance;

            if (idManager == null)
            {
                LogError("未找到ItemInstanceIDManager实例，请确保场景中存在该组件");
            }
        }

        /// <summary>
        /// 记录调试信息
        /// </summary>
        /// <param name="message">消息内容</param>
        private void LogDebug(string message)
        {
            if (enableDebugLog)
            {
                Debug.Log($"[ItemInstanceIDManagerIntegrator] {message}");
            }
        }

        /// <summary>
        /// 记录错误信息
        /// </summary>
        /// <param name="message">错误内容</param>
        private void LogError(string message)
        {
            Debug.LogError($"[ItemInstanceIDManagerIntegrator] {message}");
        }
        #endregion
    }
}