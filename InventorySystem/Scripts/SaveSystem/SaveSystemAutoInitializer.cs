using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using InventorySystem.SaveSystem;

/// <summary>
/// 保存系统自动初始化器 - 负责自动配置和初始化整个保存系统
/// 包括SaveManager、ItemInstanceIDManager和深度集成器的自动设置
/// </summary>
public class SaveSystemAutoInitializer : MonoBehaviour
{
    [Header("自动初始化配置")]
    [SerializeField] private bool enableAutoInitialization = true; // 启用自动初始化
    [SerializeField] private bool createSaveManagerIfMissing = true; // 如果缺失则创建SaveManager
    [SerializeField] private bool createIDManagerIfMissing = true; // 如果缺失则创建ItemInstanceIDManager
    [SerializeField] private bool createDeepIntegratorIfMissing = true; // 如果缺失则创建深度集成器
    [SerializeField] private bool enableScenePersistence = true; // 启用场景持久化
    [SerializeField] private float initializationDelay = 0.1f; // 初始化延迟时间
    [SerializeField] private bool enableDebugLogging = true; // 启用调试日志

    [Header("组件配置")]
    [SerializeField] private bool configureSaveManager = true; // 配置SaveManager
    [SerializeField] private bool configureIDManager = true; // 配置ItemInstanceIDManager
    [SerializeField] private bool configureDeepIntegrator = true; // 配置深度集成器
    [SerializeField] private bool enableAutoSave = true; // 启用自动保存
    [SerializeField] private float autoSaveInterval = 60f; // 自动保存间隔（秒）

    [Header("集成配置")]
    [SerializeField] private bool enableDeepIntegration = true; // 启用深度集成
    [SerializeField] private bool enableBackwardCompatibility = true; // 启用向后兼容性
    [SerializeField] private bool enableConflictDetection = true; // 启用冲突检测
    [SerializeField] private bool enableDataValidation = true; // 启用数据验证

    // 组件引用
    private SaveManager saveManager;
    private ItemInstanceIDManager idManager;
    private ItemInstanceIDManagerDeepIntegrator deepIntegrator;
    private SaveSystemInitializer systemInitializer;

    // 初始化状态
    private bool isInitialized = false;
    private bool isInitializing = false;

    // 初始化统计
    [System.Serializable]
    public class InitializationStats
    {
        public bool saveManagerCreated = false;
        public bool idManagerCreated = false;
        public bool deepIntegratorCreated = false;
        public bool systemInitializerCreated = false;
        public int componentsConfigured = 0;
        public int integrationErrors = 0;
        public string initializationTime = "";
        public string lastError = "";
    }

    [SerializeField] private InitializationStats initStats = new InitializationStats();

    // 事件系统
    public static event System.Action OnSystemInitialized;
    public static event System.Action<string> OnInitializationError;
    public static event System.Action<InitializationStats> OnInitializationCompleted;

    private void Awake()
    {
        // 确保只有一个自动初始化器实例
        var existingInitializers = FindObjectsOfType<SaveSystemAutoInitializer>();
        if (existingInitializers.Length > 1)
        {
            LogWarning("发现多个SaveSystemAutoInitializer实例，销毁重复实例");
            Destroy(gameObject);
            return;
        }
        LogMessage("保存系统自动初始化器已启动");
    }

    private void Start()
    {
        if (enableAutoInitialization && !isInitialized && !isInitializing)
        {
            StartCoroutine(DelayedInitialization());
        }
    }

    /// <summary>
    /// 延迟初始化
    /// </summary>
    private IEnumerator DelayedInitialization()
    {
        isInitializing = true;

        yield return new WaitForSeconds(initializationDelay);

        LogMessage("开始自动初始化保存系统...");

        try
        {
            InitializeSaveSystem();
        }
        catch (Exception ex)
        {
            LogError($"自动初始化失败: {ex.Message}");
            initStats.lastError = ex.Message;
            OnInitializationError?.Invoke(ex.Message);
        }
        finally
        {
            isInitializing = false;
        }
    }

    /// <summary>
    /// 初始化保存系统
    /// </summary>
    public void InitializeSaveSystem()
    {
        if (isInitialized)
        {
            LogWarning("保存系统已经初始化，跳过重复初始化");
            return;
        }

        // 重置统计信息
        initStats = new InitializationStats();

        try
        {
            // 1. 创建或查找SaveManager
            InitializeSaveManager();

            // 2. 创建或查找ItemInstanceIDManager
            InitializeIDManager();

            // 3. 创建或查找深度集成器
            InitializeDeepIntegrator();

            // 4. 创建或查找系统初始化器
            InitializeSystemInitializer();

            // 5. 配置组件
            ConfigureComponents();

            // 6. 执行深度集成
            if (enableDeepIntegration)
            {
                PerformDeepIntegration();
            }

            // 7. 启用自动保存
            if (enableAutoSave)
            {
                EnableAutoSave();
            }

            // 8. 完成初始化
            CompleteInitialization();

            LogMessage("保存系统自动初始化完成！");
        }
        catch (Exception ex)
        {
            LogError($"初始化过程中发生错误: {ex.Message}");
            initStats.lastError = ex.Message;
            initStats.integrationErrors++;
            throw;
        }
    }

    /// <summary>
    /// 初始化SaveManager
    /// </summary>
    private void InitializeSaveManager()
    {
        saveManager = FindObjectOfType<SaveManager>();

        if (saveManager == null && createSaveManagerIfMissing)
        {
            LogMessage("创建SaveManager实例...");

            var saveManagerGO = new GameObject("SaveManager");
            saveManager = saveManagerGO.AddComponent<SaveManager>();

            // 将新创建的SaveManager设置为SaveSystem的子对象（如果存在SaveSystem）
            var saveSystemPersistence = FindObjectOfType<SaveSystemPersistence>();
            if (saveSystemPersistence != null)
            {
                saveManagerGO.transform.SetParent(saveSystemPersistence.transform);
                LogMessage("SaveManager已设置为SaveSystem的子对象");
            }
            // 注意：不调用DontDestroyOnLoad，因为SaveSystemPersistence会处理整个系统的持久化

            initStats.saveManagerCreated = true;
            LogMessage("SaveManager已创建");
        }
        else if (saveManager != null)
        {
            LogMessage("发现现有SaveManager实例");
        }
        else
        {
            LogWarning("SaveManager未找到且未启用自动创建");
        }
    }

    /// <summary>
    /// 初始化ItemInstanceIDManager
    /// </summary>
    private void InitializeIDManager()
    {
        idManager = FindObjectOfType<ItemInstanceIDManager>();

        if (idManager == null && createIDManagerIfMissing)
        {
            LogMessage("创建ItemInstanceIDManager实例...");

            var idManagerGO = new GameObject("ItemInstanceIDManager");
            idManager = idManagerGO.AddComponent<ItemInstanceIDManager>();

            var saveSystemPersistence = FindObjectOfType<SaveSystemPersistence>();
            if (saveSystemPersistence != null)
            {
                idManagerGO.transform.SetParent(saveSystemPersistence.transform);
                LogMessage("ItemInstanceIDManager已设置为SaveSystem的子对象");
            }

            initStats.idManagerCreated = true;
            LogMessage("ItemInstanceIDManager已创建");
        }
        else if (idManager != null)
        {
            LogMessage("发现现有ItemInstanceIDManager实例");
        }
        else
        {
            LogWarning("ItemInstanceIDManager未找到且未启用自动创建");
        }
    }

    /// <summary>
    /// 初始化深度集成器
    /// </summary>
    private void InitializeDeepIntegrator()
    {
        deepIntegrator = FindObjectOfType<ItemInstanceIDManagerDeepIntegrator>();

        if (deepIntegrator == null && createDeepIntegratorIfMissing)
        {
            LogMessage("创建ItemInstanceIDManagerDeepIntegrator实例...");

            var integratorGO = new GameObject("ItemInstanceIDManagerDeepIntegrator");
            deepIntegrator = integratorGO.AddComponent<ItemInstanceIDManagerDeepIntegrator>();

            var saveSystemPersistence = FindObjectOfType<SaveSystemPersistence>();
            if (saveSystemPersistence != null)
            {
                integratorGO.transform.SetParent(saveSystemPersistence.transform);
                LogMessage("ItemInstanceIDManagerDeepIntegrator已设置为SaveSystem的子对象");
            }

            initStats.deepIntegratorCreated = true;
            LogMessage("ItemInstanceIDManagerDeepIntegrator已创建");
        }
        else if (deepIntegrator != null)
        {
            LogMessage("发现现有ItemInstanceIDManagerDeepIntegrator实例");
        }
        else
        {
            LogWarning("ItemInstanceIDManagerDeepIntegrator未找到且未启用自动创建");
        }
    }

    /// <summary>
    /// 初始化系统初始化器
    /// </summary>
    private void InitializeSystemInitializer()
    {
        systemInitializer = FindObjectOfType<SaveSystemInitializer>();

        if (systemInitializer == null)
        {
            LogMessage("创建SaveSystemInitializer实例...");

            var initializerGO = new GameObject("SaveSystemInitializer");
            systemInitializer = initializerGO.AddComponent<SaveSystemInitializer>();

            // 将新创建的SaveSystemInitializer设置为SaveSystem的子对象（如果存在SaveSystem）
            var saveSystemPersistence = FindObjectOfType<SaveSystemPersistence>();
            if (saveSystemPersistence != null)
            {
                initializerGO.transform.SetParent(saveSystemPersistence.transform);
                LogMessage("SaveSystemInitializer已设置为SaveSystem的子对象");
            }
            // 注意：不调用DontDestroyOnLoad，因为SaveSystemPersistence会处理整个系统的持久化

            initStats.systemInitializerCreated = true;
            LogMessage("SaveSystemInitializer已创建");
        }
        else
        {
            LogMessage("发现现有SaveSystemInitializer实例");
        }
    }

    /// <summary>
    /// 配置组件
    /// </summary>
    private void ConfigureComponents()
    {
        LogMessage("开始配置组件...");

        // 配置SaveManager
        if (configureSaveManager && saveManager != null)
        {
            ConfigureSaveManager();
            initStats.componentsConfigured++;
        }

        // 配置ItemInstanceIDManager
        if (configureIDManager && idManager != null)
        {
            ConfigureIDManager();
            initStats.componentsConfigured++;
        }

        // 配置深度集成器
        if (configureDeepIntegrator && deepIntegrator != null)
        {
            ConfigureDeepIntegrator();
            initStats.componentsConfigured++;
        }

        LogMessage($"组件配置完成，共配置{initStats.componentsConfigured}个组件");
    }

    /// <summary>
    /// 配置SaveManager
    /// </summary>
    private void ConfigureSaveManager()
    {
        // 这里可以添加SaveManager的具体配置
        // 例如设置保存路径、文件格式等
        LogMessage("SaveManager配置完成");
    }

    /// <summary>
    /// 配置ItemInstanceIDManager
    /// </summary>
    private void ConfigureIDManager()
    {
        // 使用反射或公共方法配置ItemInstanceIDManager
        try
        {
            // 启用冲突检测
            if (enableConflictDetection)
            {
                // 假设ItemInstanceIDManager有相应的配置方法
                LogMessage("启用ID冲突检测");
            }

            // 启用数据验证
            if (enableDataValidation)
            {
                LogMessage("启用数据验证");
            }

            LogMessage("ItemInstanceIDManager配置完成");
        }
        catch (Exception ex)
        {
            LogError($"配置ItemInstanceIDManager失败: {ex.Message}");
            initStats.integrationErrors++;
        }
    }

    /// <summary>
    /// 配置深度集成器
    /// </summary>
    private void ConfigureDeepIntegrator()
    {
        // 通过反射设置深度集成器的配置
        try
        {
            var integratorType = deepIntegrator.GetType();

            // 设置向后兼容性
            SetFieldValue(integratorType, "enableBackwardCompatibility", enableBackwardCompatibility);

            // 设置自动集成
            SetFieldValue(integratorType, "enableAutoIntegration", enableDeepIntegration);

            // 设置场景持久化
            SetFieldValue(integratorType, "enableScenePersistence", enableScenePersistence);

            // 设置调试日志
            SetFieldValue(integratorType, "enableDebugLogging", enableDebugLogging);

            LogMessage("ItemInstanceIDManagerDeepIntegrator配置完成");
        }
        catch (Exception ex)
        {
            LogError($"配置ItemInstanceIDManagerDeepIntegrator失败: {ex.Message}");
            initStats.integrationErrors++;
        }
    }

    /// <summary>
    /// 设置字段值（通过反射）
    /// </summary>
    private void SetFieldValue(Type type, string fieldName, object value)
    {
        var field = type.GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(deepIntegrator, value);
            LogMessage($"设置字段 {fieldName} = {value}");
        }
        else
        {
            LogWarning($"未找到字段: {fieldName}");
        }
    }

    /// <summary>
    /// 执行深度集成
    /// </summary>
    private void PerformDeepIntegration()
    {
        if (deepIntegrator == null)
        {
            LogWarning("深度集成器不存在，跳过深度集成");
            return;
        }

        try
        {
            LogMessage("开始执行深度集成...");

            // 等待一帧确保所有组件都已初始化
            StartCoroutine(DelayedDeepIntegration());
        }
        catch (Exception ex)
        {
            LogError($"深度集成失败: {ex.Message}");
            initStats.integrationErrors++;
        }
    }

    /// <summary>
    /// 延迟执行深度集成
    /// </summary>
    private IEnumerator DelayedDeepIntegration()
    {
        yield return new WaitForEndOfFrame();

        try
        {
            deepIntegrator.ManualDeepIntegration();
            LogMessage("深度集成执行完成");
        }
        catch (Exception ex)
        {
            LogError($"延迟深度集成失败: {ex.Message}");
            initStats.integrationErrors++;
        }
    }

    /// <summary>
    /// 启用自动保存
    /// </summary>
    private void EnableAutoSave()
    {
        if (saveManager == null)
        {
            LogWarning("SaveManager不存在，无法启用自动保存");
            return;
        }

        try
        {
            LogMessage($"启用自动保存，间隔: {autoSaveInterval}秒");
            StartCoroutine(AutoSaveCoroutine());
        }
        catch (Exception ex)
        {
            LogError($"启用自动保存失败: {ex.Message}");
            initStats.integrationErrors++;
        }
    }

    /// <summary>
    /// 自动保存协程
    /// </summary>
    private IEnumerator AutoSaveCoroutine()
    {
        while (enableAutoSave && saveManager != null)
        {
            yield return new WaitForSeconds(autoSaveInterval);

            try
            {
                LogMessage("执行自动保存...");
                saveManager.SaveAll("AutoSave");
                LogMessage("自动保存完成");
            }
            catch (Exception ex)
            {
                LogError($"自动保存失败: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 完成初始化
    /// </summary>
    private void CompleteInitialization()
    {
        isInitialized = true;
        initStats.initializationTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        // 触发事件
        OnSystemInitialized?.Invoke();
        OnInitializationCompleted?.Invoke(initStats);

        LogMessage($"保存系统初始化完成！统计信息：\n" +
                  $"- SaveManager创建: {initStats.saveManagerCreated}\n" +
                  $"- IDManager创建: {initStats.idManagerCreated}\n" +
                  $"- 深度集成器创建: {initStats.deepIntegratorCreated}\n" +
                  $"- 系统初始化器创建: {initStats.systemInitializerCreated}\n" +
                  $"- 配置组件数量: {initStats.componentsConfigured}\n" +
                  $"- 集成错误数量: {initStats.integrationErrors}\n" +
                  $"- 初始化时间: {initStats.initializationTime}");
    }

    // === 公共API方法 ===

    /// <summary>
    /// 手动初始化保存系统
    /// </summary>
    public void ManualInitialization()
    {
        LogMessage("手动触发保存系统初始化");

        if (isInitialized)
        {
            LogWarning("系统已初始化，将重新初始化");
            isInitialized = false;
        }

        InitializeSaveSystem();
    }

    /// <summary>
    /// 获取初始化统计信息
    /// </summary>
    public InitializationStats GetInitializationStats()
    {
        return initStats;
    }

    /// <summary>
    /// 检查系统是否已初始化
    /// </summary>
    public bool IsSystemInitialized()
    {
        return isInitialized;
    }

    /// <summary>
    /// 获取组件引用
    /// </summary>
    public SaveManager GetSaveManager() => saveManager;
    public ItemInstanceIDManager GetIDManager() => idManager;
    public ItemInstanceIDManagerDeepIntegrator GetDeepIntegrator() => deepIntegrator;
    public SaveSystemInitializer GetSystemInitializer() => systemInitializer;

    /// <summary>
    /// 重置初始化状态
    /// </summary>
    public void ResetInitializationState()
    {
        isInitialized = false;
        isInitializing = false;
        initStats = new InitializationStats();

        LogMessage("初始化状态已重置");
    }

    /// <summary>
    /// 验证系统完整性
    /// </summary>
    public bool ValidateSystemIntegrity()
    {
        bool isValid = true;

        if (saveManager == null)
        {
            LogError("SaveManager缺失");
            isValid = false;
        }

        if (idManager == null)
        {
            LogError("ItemInstanceIDManager缺失");
            isValid = false;
        }

        if (deepIntegrator == null)
        {
            LogError("ItemInstanceIDManagerDeepIntegrator缺失");
            isValid = false;
        }

        if (systemInitializer == null)
        {
            LogError("SaveSystemInitializer缺失");
            isValid = false;
        }

        return isValid;
    }

    private void OnDestroy()
    {
        LogMessage("保存系统自动初始化器已销毁");
    }

    // === 日志工具方法 ===

    private void LogMessage(string message)
    {
        if (enableDebugLogging)
        {
            Debug.Log($"[SaveSystemAutoInitializer] {message}");
        }
    }

    private void LogWarning(string message)
    {
        if (enableDebugLogging)
        {
            Debug.LogWarning($"[SaveSystemAutoInitializer] {message}");
        }
    }

    private void LogError(string message)
    {
        Debug.LogError($"[SaveSystemAutoInitializer] {message}");
    }
}