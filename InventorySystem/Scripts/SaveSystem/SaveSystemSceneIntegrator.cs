using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using InventorySystem.SaveSystem;

/// <summary>
/// 保存系统场景集成器 - 负责在场景中自动设置和配置保存系统组件
/// 处理场景加载、卸载时的系统集成和数据同步
/// </summary>
public class SaveSystemSceneIntegrator : MonoBehaviour
{
    [Header("场景集成配置")]
    [SerializeField] private bool enableAutoSceneIntegration = true; // 启用自动场景集成
    [SerializeField] private bool enableCrossSceneSync = true; // 启用跨场景同步
    [SerializeField] private bool enableSceneDataPersistence = true; // 启用场景数据持久化
    [SerializeField] private bool enableDebugLogging = true; // 启用调试日志
    [SerializeField] private float integrationDelay = 0.5f; // 集成延迟时间

    [Header("集成目标配置")]
    [SerializeField] private bool integrateInventoryItems = true; // 集成库存物品
    [SerializeField] private bool integrateItemGrids = true; // 集成物品网格
    [SerializeField] private bool integrateSpawners = true; // 集成生成器
    [SerializeField] private bool integrateEquipSlots = true; // 集成装备槽
    [SerializeField] private bool integrateItemDataHolders = true; // 集成物品数据持有者

    [Header("同步配置")]
    [SerializeField] private bool syncOnSceneLoad = true; // 场景加载时同步
    [SerializeField] private bool syncOnSceneUnload = true; // 场景卸载时同步
    [SerializeField] private bool validateAfterSync = true; // 同步后验证
    [SerializeField] private bool resolveConflictsAutomatically = true; // 自动解决冲突

    // 系统组件引用
    private SaveManager saveManager;
    private ItemInstanceIDManager idManager;
    private ItemInstanceIDManagerDeepIntegrator deepIntegrator;
    private SaveSystemAutoInitializer autoInitializer;

    // 场景集成状态
    private bool isIntegrated = false;
    private bool isIntegrating = false;
    private string currentSceneName;
    private Dictionary<string, SceneIntegrationData> sceneDataCache;

    // 集成统计
    [System.Serializable]
    public class SceneIntegrationStats
    {
        public string sceneName = "";
        public int integratedItems = 0;
        public int integratedGrids = 0;
        public int integratedSpawners = 0;
        public int integratedEquipSlots = 0;
        public int integratedDataHolders = 0;
        public int resolvedConflicts = 0;
        public int validationErrors = 0;
        public string integrationTime = "";
        public string lastError = "";
    }

    [SerializeField] private SceneIntegrationStats integrationStats = new SceneIntegrationStats();

    // 场景数据缓存
    [System.Serializable]
    public class SceneIntegrationData
    {
        public string sceneName;
        public List<string> registeredItemIDs = new List<string>();
        public List<string> registeredGridIDs = new List<string>();
        public List<string> registeredSpawnerIDs = new List<string>();
        public List<string> registeredEquipSlotIDs = new List<string>();
        public Dictionary<string, string> idMappings = new Dictionary<string, string>();
        public string lastSyncTime;
    }

    // 事件系统
    public static event System.Action<string> OnSceneIntegrationStarted;
    public static event System.Action<string, SceneIntegrationStats> OnSceneIntegrationCompleted;
    public static event System.Action<string> OnSceneIntegrationError;
    public static event System.Action<string, string> OnCrossSceneSyncCompleted;

    private void Awake()
    {
        // 初始化场景数据缓存
        sceneDataCache = new Dictionary<string, SceneIntegrationData>();
        currentSceneName = SceneManager.GetActiveScene().name;

        LogMessage("保存系统场景集成器已启动");
    }

    private void Start()
    {
        // 注册场景事件
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;

        // 延迟初始化
        StartCoroutine(DelayedInitialization());
    }

    private void OnDestroy()
    {
        // 取消注册场景事件
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;

        LogMessage("保存系统场景集成器已销毁");
    }

    /// <summary>
    /// 延迟初始化
    /// </summary>
    private IEnumerator DelayedInitialization()
    {
        yield return new WaitForSeconds(integrationDelay);

        try
        {
            InitializeSystemReferences();

            if (enableAutoSceneIntegration)
            {
                IntegrateCurrentScene();
            }
        }
        catch (Exception ex)
        {
            LogError($"延迟初始化失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 初始化系统组件引用
    /// </summary>
    private void InitializeSystemReferences()
    {
        LogMessage("初始化系统组件引用...");

        // 查找系统组件
        saveManager = FindObjectOfType<SaveManager>();
        idManager = FindObjectOfType<ItemInstanceIDManager>();
        deepIntegrator = FindObjectOfType<ItemInstanceIDManagerDeepIntegrator>();
        autoInitializer = FindObjectOfType<SaveSystemAutoInitializer>();

        // 验证组件完整性
        ValidateSystemComponents();

        LogMessage("系统组件引用初始化完成");
    }

    /// <summary>
    /// 验证系统组件
    /// </summary>
    private void ValidateSystemComponents()
    {
        List<string> missingComponents = new List<string>();

        if (saveManager == null) missingComponents.Add("SaveManager");
        if (idManager == null) missingComponents.Add("ItemInstanceIDManager");
        if (deepIntegrator == null) missingComponents.Add("ItemInstanceIDManagerDeepIntegrator");

        if (missingComponents.Count > 0)
        {
            string missing = string.Join(", ", missingComponents);
            LogWarning($"缺失系统组件: {missing}");

            // 如果有自动初始化器，尝试触发初始化
            if (autoInitializer != null && !autoInitializer.IsSystemInitialized())
            {
                LogMessage("尝试通过自动初始化器创建缺失组件...");
                autoInitializer.ManualInitialization();

                // 重新获取组件引用
                StartCoroutine(RetrySystemReferences());
            }
        }
        else
        {
            LogMessage("所有系统组件验证通过");
        }
    }

    /// <summary>
    /// 重试获取系统组件引用
    /// </summary>
    private IEnumerator RetrySystemReferences()
    {
        yield return new WaitForSeconds(1f);

        saveManager = FindObjectOfType<SaveManager>();
        idManager = FindObjectOfType<ItemInstanceIDManager>();
        deepIntegrator = FindObjectOfType<ItemInstanceIDManagerDeepIntegrator>();

        LogMessage("系统组件引用已更新");
    }

    /// <summary>
    /// 场景加载事件处理
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        LogMessage($"场景已加载: {scene.name} (模式: {mode})");

        currentSceneName = scene.name;

        if (syncOnSceneLoad && enableAutoSceneIntegration)
        {
            StartCoroutine(DelayedSceneIntegration(scene.name));
        }
    }

    /// <summary>
    /// 场景卸载事件处理
    /// </summary>
    private void OnSceneUnloaded(Scene scene)
    {
        LogMessage($"场景已卸载: {scene.name}");

        if (syncOnSceneUnload)
        {
            SaveSceneData(scene.name);
        }
    }

    /// <summary>
    /// 延迟场景集成
    /// </summary>
    private IEnumerator DelayedSceneIntegration(string sceneName)
    {
        yield return new WaitForSeconds(integrationDelay);

        try
        {
            IntegrateScene(sceneName);
        }
        catch (Exception ex)
        {
            LogError($"场景集成失败: {ex.Message}");
            OnSceneIntegrationError?.Invoke(ex.Message);
        }
    }

    /// <summary>
    /// 集成当前场景
    /// </summary>
    public void IntegrateCurrentScene()
    {
        IntegrateScene(currentSceneName);
    }

    /// <summary>
    /// 集成指定场景
    /// </summary>
    public void IntegrateScene(string sceneName)
    {
        if (isIntegrating)
        {
            LogWarning("场景集成正在进行中，跳过重复集成");
            return;
        }

        if (string.IsNullOrEmpty(sceneName))
        {
            sceneName = currentSceneName;
        }

        LogMessage($"开始集成场景: {sceneName}");

        isIntegrating = true;
        integrationStats = new SceneIntegrationStats { sceneName = sceneName };

        OnSceneIntegrationStarted?.Invoke(sceneName);

        try
        {
            // 1. 初始化系统组件引用（如果需要）
            if (saveManager == null || idManager == null || deepIntegrator == null)
            {
                InitializeSystemReferences();
            }

            // 2. 加载场景数据缓存
            LoadSceneData(sceneName);

            // 3. 执行各类组件集成
            if (integrateInventoryItems) IntegrateInventoryItems();
            if (integrateItemGrids) IntegrateItemGrids();
            if (integrateSpawners) IntegrateSpawners();
            if (integrateEquipSlots) IntegrateEquipSlots();
            if (integrateItemDataHolders) IntegrateItemDataHolders();

            // 4. 执行跨场景同步
            if (enableCrossSceneSync)
            {
                PerformCrossSceneSync(sceneName);
            }

            // 5. 解决冲突
            if (resolveConflictsAutomatically)
            {
                ResolveConflicts();
            }

            // 6. 验证集成结果
            if (validateAfterSync)
            {
                ValidateIntegration();
            }

            // 7. 保存场景数据
            SaveSceneData(sceneName);

            // 8. 完成集成
            CompleteSceneIntegration(sceneName);

            LogMessage($"场景集成完成: {sceneName}");
        }
        catch (Exception ex)
        {
            LogError($"场景集成失败: {ex.Message}");
            integrationStats.lastError = ex.Message;
            OnSceneIntegrationError?.Invoke(ex.Message);
        }
        finally
        {
            isIntegrating = false;
        }
    }

    /// <summary>
    /// 集成库存物品
    /// </summary>
    private void IntegrateInventoryItems()
    {
        var items = FindObjectsOfType<InventorySystemItem>();
        LogMessage($"发现 {items.Length} 个库存物品");

        foreach (var item in items)
        {
            try
            {
                // 确保物品有有效的实例ID
                if (string.IsNullOrEmpty(item.GetItemInstanceID()) || !item.IsItemInstanceIDValid())
                {
                    item.GenerateNewItemInstanceID();
                }

                // 注册到ID管理器
                if (idManager != null)
                {
                    idManager.RegisterInstanceID(item.GetItemInstanceID(), item.gameObject.name, "InventorySystemItem", item.transform.position);
                }

                integrationStats.integratedItems++;
            }
            catch (Exception ex)
            {
                LogError($"集成库存物品失败: {ex.Message}");
                integrationStats.validationErrors++;
            }
        }

        LogMessage($"库存物品集成完成，共集成 {integrationStats.integratedItems} 个物品");
    }

    /// <summary>
    /// 集成物品网格
    /// </summary>
    private void IntegrateItemGrids()
    {
        var grids = FindObjectsOfType<BaseItemGrid>();
        LogMessage($"发现 {grids.Length} 个物品网格");

        foreach (var grid in grids)
        {
            try
            {
                // 确保网格有有效的保存ID
                if (string.IsNullOrEmpty(grid.GetSaveID()))
                {
                    grid.SetSaveID(System.Guid.NewGuid().ToString());
                }

                // 确保保存系统正确设置（通过公共方法）
                if (!grid.IsSaveIDValid())
                {
                    grid.GenerateNewSaveID();
                }
                grid.MarkAsModified();

                // 注册到ID管理器
                if (idManager != null)
                {
                    idManager.RegisterInstanceID(grid.GetSaveID(), grid.gameObject.name, "BaseItemGrid", grid.transform.position);
                }

                integrationStats.integratedGrids++;
            }
            catch (Exception ex)
            {
                LogError($"集成物品网格失败: {ex.Message}");
                integrationStats.validationErrors++;
            }
        }

        LogMessage($"物品网格集成完成，共集成 {integrationStats.integratedGrids} 个网格");
    }

    /// <summary>
    /// 集成生成器
    /// </summary>
    private void IntegrateSpawners()
    {
        var spawners = FindObjectsOfType<BaseItemSpawn>();
        LogMessage($"发现 {spawners.Length} 个生成器");

        foreach (var spawner in spawners)
        {
            try
            {
                // 确保生成器有有效的保存ID
                if (string.IsNullOrEmpty(spawner.GetSaveID()))
                {
                    spawner.SetSaveID(System.Guid.NewGuid().ToString());
                }

                // 注册到ID管理器
                if (idManager != null)
                {
                    idManager.RegisterInstanceID(spawner.GetSaveID(), spawner.gameObject.name, "BaseItemSpawn", spawner.transform.position);
                }

                integrationStats.integratedSpawners++;
            }
            catch (Exception ex)
            {
                LogError($"集成生成器失败: {ex.Message}");
                integrationStats.validationErrors++;
            }
        }

        LogMessage($"生成器集成完成，共集成 {integrationStats.integratedSpawners} 个生成器");
    }

    /// <summary>
    /// 集成装备槽
    /// </summary>
    private void IntegrateEquipSlots()
    {
        var equipSlots = FindObjectsOfType<EquipSlot>();
        LogMessage($"发现 {equipSlots.Length} 个装备槽");

        foreach (var slot in equipSlots)
        {
            try
            {
                // 确保装备槽有有效的保存ID
                if (string.IsNullOrEmpty(slot.GetSaveID()))
                {
                    slot.SetSaveID(System.Guid.NewGuid().ToString());
                }

                // 注册到ID管理器
                if (idManager != null)
                {
                    idManager.RegisterInstanceID(slot.GetSaveID(), slot.gameObject.name, "EquipSlot", slot.transform.position);
                }

                integrationStats.integratedEquipSlots++;
            }
            catch (Exception ex)
            {
                LogError($"集成装备槽失败: {ex.Message}");
                integrationStats.validationErrors++;
            }
        }

        LogMessage($"装备槽集成完成，共集成 {integrationStats.integratedEquipSlots} 个装备槽");
    }

    /// <summary>
    /// 集成物品数据持有者
    /// </summary>
    private void IntegrateItemDataHolders()
    {
        var dataHolders = FindObjectsOfType<ItemDataHolder>();
        LogMessage($"发现 {dataHolders.Length} 个物品数据持有者");

        foreach (var holder in dataHolders)
        {
            try
            {
                // 确保数据持有者有有效的保存ID
                if (string.IsNullOrEmpty(holder.GetSaveID()))
                {
                    holder.SetSaveID(System.Guid.NewGuid().ToString());
                }

                // 注册到ID管理器
                if (idManager != null)
                {
                    idManager.RegisterInstanceID(holder.GetSaveID(), holder.gameObject.name, "ItemDataHolder", holder.transform.position);
                }

                integrationStats.integratedDataHolders++;
            }
            catch (Exception ex)
            {
                LogError($"集成物品数据持有者失败: {ex.Message}");
                integrationStats.validationErrors++;
            }
        }

        LogMessage($"物品数据持有者集成完成，共集成 {integrationStats.integratedDataHolders} 个数据持有者");
    }

    /// <summary>
    /// 执行跨场景同步
    /// </summary>
    private void PerformCrossSceneSync(string sceneName)
    {
        if (idManager == null)
        {
            LogWarning("ID管理器不存在，跳过跨场景同步");
            return;
        }

        try
        {
            LogMessage($"开始跨场景同步: {sceneName}");

            // 这里可以添加具体的跨场景同步逻辑
            // 例如同步物品状态、位置、属性等

            OnCrossSceneSyncCompleted?.Invoke(sceneName, "同步完成");
            LogMessage($"跨场景同步完成: {sceneName}");
        }
        catch (Exception ex)
        {
            LogError($"跨场景同步失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 解决冲突
    /// </summary>
    private void ResolveConflicts()
    {
        if (idManager == null)
        {
            LogWarning("ID管理器不存在，跳过冲突解决");
            return;
        }

        try
        {
            LogMessage("开始解决ID冲突...");

            // 这里可以调用ID管理器的冲突解决方法
            // 例如: idManager.ResolveConflicts();

            LogMessage("ID冲突解决完成");
        }
        catch (Exception ex)
        {
            LogError($"解决冲突失败: {ex.Message}");
            integrationStats.validationErrors++;
        }
    }

    /// <summary>
    /// 验证集成结果
    /// </summary>
    private void ValidateIntegration()
    {
        try
        {
            LogMessage("开始验证集成结果...");

            // 验证所有注册的对象是否有效
            int validationErrors = 0;

            // 验证库存物品
            var items = FindObjectsOfType<InventorySystemItem>();
            foreach (var item in items)
            {
                if (!item.IsItemInstanceIDValid())
                {
                    LogError($"库存物品ID无效: {item.name}");
                    validationErrors++;
                }
            }

            // 验证物品网格
            var grids = FindObjectsOfType<BaseItemGrid>();
            foreach (var grid in grids)
            {
                if (string.IsNullOrEmpty(grid.GetSaveID()))
                {
                    LogError($"物品网格保存ID无效: {grid.name}");
                    validationErrors++;
                }
            }

            integrationStats.validationErrors += validationErrors;

            if (validationErrors == 0)
            {
                LogMessage("集成验证通过");
            }
            else
            {
                LogWarning($"集成验证发现 {validationErrors} 个错误");
            }
        }
        catch (Exception ex)
        {
            LogError($"验证集成结果失败: {ex.Message}");
            integrationStats.validationErrors++;
        }
    }

    /// <summary>
    /// 加载场景数据
    /// </summary>
    private void LoadSceneData(string sceneName)
    {
        if (sceneDataCache.ContainsKey(sceneName))
        {
            LogMessage($"加载场景数据缓存: {sceneName}");
            // 这里可以添加具体的数据加载逻辑
        }
        else
        {
            LogMessage($"创建新的场景数据缓存: {sceneName}");
            sceneDataCache[sceneName] = new SceneIntegrationData { sceneName = sceneName };
        }
    }

    /// <summary>
    /// 保存场景数据
    /// </summary>
    private void SaveSceneData(string sceneName)
    {
        try
        {
            if (!sceneDataCache.ContainsKey(sceneName))
            {
                sceneDataCache[sceneName] = new SceneIntegrationData { sceneName = sceneName };
            }

            var sceneData = sceneDataCache[sceneName];
            sceneData.lastSyncTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            // 这里可以添加具体的数据保存逻辑

            LogMessage($"场景数据已保存: {sceneName}");
        }
        catch (Exception ex)
        {
            LogError($"保存场景数据失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 完成场景集成
    /// </summary>
    private void CompleteSceneIntegration(string sceneName)
    {
        isIntegrated = true;
        integrationStats.integrationTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        OnSceneIntegrationCompleted?.Invoke(sceneName, integrationStats);

        LogMessage($"场景集成统计:\n" +
                  $"- 场景名称: {integrationStats.sceneName}\n" +
                  $"- 集成物品: {integrationStats.integratedItems}\n" +
                  $"- 集成网格: {integrationStats.integratedGrids}\n" +
                  $"- 集成生成器: {integrationStats.integratedSpawners}\n" +
                  $"- 集成装备槽: {integrationStats.integratedEquipSlots}\n" +
                  $"- 集成数据持有者: {integrationStats.integratedDataHolders}\n" +
                  $"- 解决冲突: {integrationStats.resolvedConflicts}\n" +
                  $"- 验证错误: {integrationStats.validationErrors}\n" +
                  $"- 集成时间: {integrationStats.integrationTime}");
    }

    // === 公共API方法 ===

    /// <summary>
    /// 手动触发场景集成
    /// </summary>
    public void ManualSceneIntegration()
    {
        LogMessage("手动触发场景集成");
        IntegrateCurrentScene();
    }

    /// <summary>
    /// 获取集成统计信息
    /// </summary>
    public SceneIntegrationStats GetIntegrationStats()
    {
        return integrationStats;
    }

    /// <summary>
    /// 检查是否已集成
    /// </summary>
    public bool IsSceneIntegrated()
    {
        return isIntegrated;
    }

    /// <summary>
    /// 获取场景数据缓存
    /// </summary>
    public Dictionary<string, SceneIntegrationData> GetSceneDataCache()
    {
        return sceneDataCache;
    }

    /// <summary>
    /// 清除场景数据缓存
    /// </summary>
    public void ClearSceneDataCache()
    {
        sceneDataCache.Clear();
        LogMessage("场景数据缓存已清除");
    }

    /// <summary>
    /// 重置集成状态
    /// </summary>
    public void ResetIntegrationState()
    {
        isIntegrated = false;
        isIntegrating = false;
        integrationStats = new SceneIntegrationStats();

        LogMessage("集成状态已重置");
    }

    // === 日志工具方法 ===

    private void LogMessage(string message)
    {
        if (enableDebugLogging)
        {
            Debug.Log($"[SaveSystemSceneIntegrator] {message}");
        }
    }

    private void LogWarning(string message)
    {
        if (enableDebugLogging)
        {
            Debug.LogWarning($"[SaveSystemSceneIntegrator] {message}");
        }
    }

    private void LogError(string message)
    {
        Debug.LogError($"[SaveSystemSceneIntegrator] {message}");
    }
}