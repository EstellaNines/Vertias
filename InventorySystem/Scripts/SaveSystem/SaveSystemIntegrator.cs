using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;

namespace InventorySystem.SaveSystem
{
    /// <summary>
    /// 保存系统集成器
    /// 将所有增强组件（验证器、事务管理器、完整性检查器、ID管理器增强器）
    /// 整合到现有的保存系统中，提供统一的增强保存/加载接口
    /// 采用非侵入式设计，不修改现有SaveManager代码
    /// </summary>
    public class SaveSystemIntegrator : MonoBehaviour
    {
        #region 单例模式
        private static SaveSystemIntegrator _instance;
        public static SaveSystemIntegrator Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<SaveSystemIntegrator>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("SaveSystemIntegrator");
                        _instance = go.AddComponent<SaveSystemIntegrator>();

                        // 设置为SaveSystem的子对象
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
        [Header("保存系统集成配置")]
        [SerializeField]
        [Tooltip("是否启用增强保存功能")]
        private bool enableEnhancedSave = true;

        [SerializeField]
        [Tooltip("是否启用增强加载功能")]
        private bool enableEnhancedLoad = true;

        [SerializeField]
        [Tooltip("是否启用自动初始化")]
        private bool enableAutoInitialization = true;

        [SerializeField]
        [Tooltip("是否启用组件健康检查")]
        private bool enableComponentHealthCheck = true;

        [SerializeField]
        [Tooltip("是否启用性能监控")]
        private bool enablePerformanceMonitoring = true;

        [SerializeField]
        [Tooltip("健康检查间隔（秒）")]
        private float healthCheckInterval = 30f;

        [SerializeField]
        [Tooltip("是否启用调试日志")]
        private bool enableDebugLog = true;
        #endregion

        #region 数据结构
        /// <summary>
        /// 集成状态
        /// </summary>
        public enum IntegrationStatus
        {
            NotInitialized,     // 未初始化
            Initializing,       // 初始化中
            Ready,              // 就绪
            Error,              // 错误
            Disabled            // 禁用
        }

        /// <summary>
        /// 组件健康状态
        /// </summary>
        [System.Serializable]
        public class ComponentHealth
        {
            public string componentName;
            public bool isAvailable;
            public bool isInitialized;
            public bool isWorking;
            public string lastError;
            public DateTime lastCheckTime;
            public float responseTime;

            public ComponentHealth(string name)
            {
                componentName = name;
                lastCheckTime = DateTime.Now;
            }
        }

        /// <summary>
        /// 集成操作结果
        /// </summary>
        [System.Serializable]
        public class IntegrationResult
        {
            public bool success;
            public string message;
            public Dictionary<string, object> data;
            public List<string> warnings;
            public List<string> errors;
            public float executionTime;
            public DateTime timestamp;

            public IntegrationResult()
            {
                data = new Dictionary<string, object>();
                warnings = new List<string>();
                errors = new List<string>();
                timestamp = DateTime.Now;
            }
        }

        /// <summary>
        /// 性能统计
        /// </summary>
        [System.Serializable]
        public class PerformanceStats
        {
            public int totalSaveOperations;
            public int totalLoadOperations;
            public int successfulSaves;
            public int successfulLoads;
            public float averageSaveTime;
            public float averageLoadTime;
            public DateTime lastOperationTime;
            public Dictionary<string, float> componentPerformance;

            public PerformanceStats()
            {
                componentPerformance = new Dictionary<string, float>();
                lastOperationTime = DateTime.Now;
            }
        }
        #endregion

        #region 私有字段
        // 核心组件引用
        private SaveManager saveManager;
        private SaveManagerEnhancer saveManagerEnhancer;
        private SaveDataValidator saveDataValidator;
        private SaveTransactionManager saveTransactionManager;
        private SaveDataIntegrityChecker saveDataIntegrityChecker;
        private ItemInstanceIDManagerEnhancer idManagerEnhancer;

        // 状态管理
        private IntegrationStatus currentStatus = IntegrationStatus.NotInitialized;
        private Dictionary<string, ComponentHealth> componentHealthMap;
        private PerformanceStats performanceStats;
        private Coroutine healthCheckCoroutine;
        private bool isInitialized = false;
        #endregion

        #region 事件定义
        // 集成状态事件
        public event Action<IntegrationStatus> OnStatusChanged;
        public event Action<ComponentHealth> OnComponentHealthChanged;
        public event Action<IntegrationResult> OnOperationCompleted;
        public event Action<PerformanceStats> OnPerformanceUpdate;
        public event Action<string, string> OnWarning;
        public event Action<string, string> OnError;
        #endregion

        #region Unity生命周期
        private void Awake()
        {
            // 确保单例唯一性
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            InitializeIntegrator();
        }

        private void Start()
        {
            if (enableAutoInitialization)
            {
                StartCoroutine(AutoInitializeCoroutine());
            }
        }

        private void OnDestroy()
        {
            // 停止健康检查
            if (healthCheckCoroutine != null)
            {
                StopCoroutine(healthCheckCoroutine);
            }

            if (_instance == this)
            {
                _instance = null;
            }
        }
        #endregion

        #region 初始化
        /// <summary>
        /// 初始化集成器
        /// </summary>
        private void InitializeIntegrator()
        {
            componentHealthMap = new Dictionary<string, ComponentHealth>();
            performanceStats = new PerformanceStats();

            SetStatus(IntegrationStatus.NotInitialized);
            LogDebug("SaveSystemIntegrator已创建");
        }

        /// <summary>
        /// 自动初始化协程
        /// </summary>
        private IEnumerator AutoInitializeCoroutine()
        {
            SetStatus(IntegrationStatus.Initializing);
            LogDebug("开始自动初始化保存系统集成器");

            // 等待一帧确保所有组件都已创建
            yield return null;

            // 1. 获取核心组件引用
            yield return StartCoroutine(InitializeComponentReferences());

            try
            {
                // 2. 初始化组件健康监控
                InitializeComponentHealth();

                // 3. 启动健康检查
                if (enableComponentHealthCheck)
                {
                    StartHealthCheck();
                }

                // 4. 设置事件监听
                SetupEventListeners();

                isInitialized = true;
                SetStatus(IntegrationStatus.Ready);
                LogDebug("保存系统集成器初始化完成");
            }
            catch (Exception ex)
            {
                SetStatus(IntegrationStatus.Error);
                LogError($"初始化过程中发生异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 初始化组件引用
        /// </summary>
        private IEnumerator InitializeComponentReferences()
        {
            LogDebug("正在获取组件引用...");

            // 获取SaveManager
            saveManager = SaveManager.Instance;
            if (saveManager == null)
            {
                LogError("无法找到SaveManager实例");
                yield break;
            }
            yield return null;

            // 获取或创建增强组件
            saveManagerEnhancer = SaveManagerEnhancer.Instance;
            saveDataValidator = SaveDataValidator.Instance;
            saveTransactionManager = SaveTransactionManager.Instance;
            saveDataIntegrityChecker = SaveDataIntegrityChecker.Instance;
            idManagerEnhancer = ItemInstanceIDManagerEnhancer.Instance;

            yield return null;

            LogDebug("组件引用获取完成");
        }

        /// <summary>
        /// 初始化组件健康监控
        /// </summary>
        private void InitializeComponentHealth()
        {
            var components = new string[]
            {
                "SaveManager",
                "SaveManagerEnhancer",
                "SaveDataValidator",
                "SaveTransactionManager",
                "SaveDataIntegrityChecker",
                "ItemInstanceIDManagerEnhancer"
            };

            foreach (var componentName in components)
            {
                componentHealthMap[componentName] = new ComponentHealth(componentName);
            }

            LogDebug("组件健康监控已初始化");
        }

        /// <summary>
        /// 设置事件监听
        /// </summary>
        private void SetupEventListeners()
        {
            try
            {
                // 监听SaveManagerEnhancer事件
                if (saveManagerEnhancer != null)
                {
                    saveManagerEnhancer.OnEnhancedSaveComplete += OnEnhancedSaveCompleted;
                    saveManagerEnhancer.OnEnhancedLoadComplete += OnEnhancedLoadCompleted;
                }

                // 监听SaveDataValidator事件
                if (saveDataValidator != null)
                {
                    saveDataValidator.OnValidationComplete += OnValidationComplete;
                    saveDataValidator.OnValidationError += OnValidationError;
                }

                // 监听SaveTransactionManager事件
                if (saveTransactionManager != null)
                {
                    saveTransactionManager.OnTransactionCompleted += OnTransactionCompleted;
                    saveTransactionManager.OnTransactionFailed += OnTransactionFailed;
                }

                // 监听SaveDataIntegrityChecker事件
                if (saveDataIntegrityChecker != null)
                {
                    saveDataIntegrityChecker.OnIntegrityCheckComplete += OnIntegrityCheckCompleted;
                }

                // 监听ItemInstanceIDManagerEnhancer事件
                if (idManagerEnhancer != null)
                {
                    idManagerEnhancer.OnConflictDetected += OnIdConflictDetected;
                    idManagerEnhancer.OnConflictResolved += OnIdConflictResolved;
                }

                LogDebug("事件监听器设置完成");
            }
            catch (Exception ex)
            {
                LogError($"设置事件监听器时发生异常: {ex.Message}");
            }
        }
        #endregion

        #region 主要集成方法
        /// <summary>
        /// 执行增强保存操作
        /// </summary>
        /// <param name="saveSlot">保存槽位</param>
        /// <param name="useTransaction">是否使用事务</param>
        /// <returns>保存结果</returns>
        public Coroutine PerformEnhancedSave(string saveSlot = "default", bool useTransaction = true)
        {
            if (!IsReady())
            {
                LogError("集成器未就绪，无法执行增强保存");
                return null;
            }

            if (!enableEnhancedSave)
            {
                LogDebug("增强保存功能已禁用，使用标准保存");
                return saveManager.SaveAll(saveSlot);
            }

            return StartCoroutine(EnhancedSaveCoroutine(saveSlot, useTransaction));
        }

        /// <summary>
        /// 执行增强加载操作
        /// </summary>
        /// <param name="saveSlot">保存槽位</param>
        /// <param name="validateData">是否验证数据</param>
        /// <returns>加载结果</returns>
        public Coroutine PerformEnhancedLoad(string saveSlot = "default", bool validateData = true)
        {
            if (!IsReady())
            {
                LogError("集成器未就绪，无法执行增强加载");
                return null;
            }

            if (!enableEnhancedLoad)
            {
                LogDebug("增强加载功能已禁用，使用标准加载");
                return saveManager.LoadSave(saveSlot);
            }

            return StartCoroutine(EnhancedLoadCoroutine(saveSlot, validateData));
        }

        /// <summary>
        /// 执行完整的系统健康检查
        /// </summary>
        /// <returns>健康检查结果</returns>
        public IntegrationResult PerformSystemHealthCheck()
        {
            var result = new IntegrationResult();
            var startTime = Time.realtimeSinceStartup;

            try
            {
                LogDebug("开始系统健康检查");

                // 检查所有组件
                CheckAllComponentsHealth();

                // 统计健康状态
                var healthyComponents = componentHealthMap.Values.Count(h => h.isWorking);
                var totalComponents = componentHealthMap.Count;

                result.success = healthyComponents == totalComponents;
                result.message = $"健康检查完成: {healthyComponents}/{totalComponents} 组件正常工作";
                result.data["healthyComponents"] = healthyComponents;
                result.data["totalComponents"] = totalComponents;
                result.data["componentHealth"] = componentHealthMap;

                if (!result.success)
                {
                    var unhealthyComponents = componentHealthMap.Values.Where(h => !h.isWorking).Select(h => h.componentName);
                    result.warnings.Add($"以下组件工作异常: {string.Join(", ", unhealthyComponents)}");
                }

                LogDebug($"系统健康检查完成: {result.message}");
            }
            catch (Exception ex)
            {
                result.success = false;
                result.message = "健康检查过程中发生异常";
                result.errors.Add(ex.Message);
                LogError($"健康检查异常: {ex.Message}");
            }
            finally
            {
                result.executionTime = Time.realtimeSinceStartup - startTime;
                OnOperationCompleted?.Invoke(result);
            }

            return result;
        }

        /// <summary>
        /// 强制重新初始化所有组件
        /// </summary>
        /// <returns>重新初始化协程</returns>
        public Coroutine ForceReinitialize()
        {
            LogDebug("强制重新初始化所有组件");
            isInitialized = false;
            SetStatus(IntegrationStatus.Initializing);
            return StartCoroutine(AutoInitializeCoroutine());
        }
        #endregion

        #region 协程实现
        /// <summary>
        /// 增强保存协程
        /// </summary>
        private IEnumerator EnhancedSaveCoroutine(string saveSlot, bool useTransaction)
        {
            var result = new IntegrationResult();
            var startTime = Time.realtimeSinceStartup;

            LogDebug($"开始增强保存操作: {saveSlot}");

            // 1. 预保存验证
            try
            {
                if (saveDataValidator != null)
                {
                    LogDebug("执行保存前数据验证");
                    var registeredObjects = GetRegisteredSaveableObjects();
                    var validationResult = saveDataValidator.ValidateBeforeSave(registeredObjects);

                    if (!validationResult.isValid)
                    {
                        result.success = false;
                        result.message = "保存前数据验证失败";
                        result.errors.AddRange(validationResult.errors.Select(e => e.message));
                        result.executionTime = Time.realtimeSinceStartup - startTime;
                        OnOperationCompleted?.Invoke(result);
                        yield break;
                    }
                }
            }
            catch (Exception ex)
            {
                result.success = false;
                result.message = "预保存验证过程中发生异常";
                result.errors.Add(ex.Message);
                LogError($"预保存验证异常: {ex.Message}");
                result.executionTime = Time.realtimeSinceStartup - startTime;
                OnOperationCompleted?.Invoke(result);
                yield break;
            }

            // 2. ID冲突检测和解决
            try
            {
                if (idManagerEnhancer != null)
                {
                    LogDebug("执行ID冲突检测");
                    var conflicts = idManagerEnhancer.PerformAdvancedConflictDetection();
                    if (conflicts.Count > 0)
                    {
                        LogDebug($"检测到{conflicts.Count}个ID冲突，尝试自动解决");
                        var resolutionResults = idManagerEnhancer.AutoResolveConflicts(conflicts);
                        var unresolvedCount = resolutionResults.Values.Count(r => !r);

                        if (unresolvedCount > 0)
                        {
                            result.warnings.Add($"有{unresolvedCount}个ID冲突无法自动解决");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                result.warnings.Add($"ID冲突检测异常: {ex.Message}");
                LogError($"ID冲突检测异常: {ex.Message}");
            }

            yield return null;

            // 3. 执行事务性保存或标准保存
            Coroutine saveCoroutine = null;
            try
            {
                if (useTransaction && saveTransactionManager != null)
                {
                    LogDebug("执行事务性保存");
                    var registeredObjects = GetRegisteredSaveableObjects();
                    var transactionResult = saveTransactionManager.BeginTransactionalSave(registeredObjects);

                    if (!transactionResult.success)
                    {
                        result.success = false;
                        result.message = "事务性保存失败";
                        result.errors.Add(transactionResult.message);
                        result.executionTime = Time.realtimeSinceStartup - startTime;
                        OnOperationCompleted?.Invoke(result);
                        yield break;
                    }
                }
                else if (saveManagerEnhancer != null)
                {
                    LogDebug("执行增强保存");
                    saveCoroutine = saveManagerEnhancer.PerformEnhancedSave(saveSlot);
                }
                else
                {
                    LogDebug("执行标准保存");
                    saveCoroutine = saveManager.SaveAll(saveSlot);
                }
            }
            catch (Exception ex)
            {
                result.success = false;
                result.message = "保存执行过程中发生异常";
                result.errors.Add(ex.Message);
                LogError($"保存执行异常: {ex.Message}");
                result.executionTime = Time.realtimeSinceStartup - startTime;
                OnOperationCompleted?.Invoke(result);
                yield break;
            }

            // 执行保存协程
            if (saveCoroutine != null)
            {
                yield return saveCoroutine;
            }

            // 4. 保存后完整性检查
            try
            {
                if (saveDataIntegrityChecker != null)
                {
                    LogDebug("执行保存后完整性检查");
                    var registeredObjects = GetRegisteredSaveableObjects();
                    var integrityResult = saveDataIntegrityChecker.PerformIntegrityCheck(registeredObjects);

                    if (!integrityResult.isIntact)
                    {
                        result.warnings.Add("保存后完整性检查发现问题");
                        result.warnings.AddRange(integrityResult.issues.Select(i => i.description));
                    }
                }
            }
            catch (Exception ex)
            {
                result.warnings.Add($"完整性检查异常: {ex.Message}");
                LogError($"完整性检查异常: {ex.Message}");
            }

            // 5. 完成保存操作
            try
            {
                result.success = true;
                result.message = "增强保存操作完成";

                // 更新性能统计
                if (enablePerformanceMonitoring)
                {
                    performanceStats.totalSaveOperations++;
                    performanceStats.successfulSaves++;
                    performanceStats.averageSaveTime = (performanceStats.averageSaveTime + (Time.realtimeSinceStartup - startTime)) / 2f;
                    performanceStats.lastOperationTime = DateTime.Now;
                    OnPerformanceUpdate?.Invoke(performanceStats);
                }

                LogDebug($"增强保存完成: {saveSlot}");
            }
            catch (Exception ex)
            {
                result.warnings.Add($"性能统计更新异常: {ex.Message}");
                LogError($"性能统计更新异常: {ex.Message}");
            }
            finally
            {
                result.executionTime = Time.realtimeSinceStartup - startTime;
                OnOperationCompleted?.Invoke(result);
            }
        }

        /// <summary>
        /// 增强加载协程
        /// </summary>
        private IEnumerator EnhancedLoadCoroutine(string saveSlot, bool validateData)
        {
            var result = new IntegrationResult();
            var startTime = Time.realtimeSinceStartup;

            LogDebug($"开始增强加载操作: {saveSlot}");

            // 1. 加载前完整性检查
            try
            {
                if (saveDataIntegrityChecker != null)
                {
                    LogDebug("执行加载前完整性检查");
                    var registeredObjects = GetRegisteredSaveableObjects();
                    var integrityResult = saveDataIntegrityChecker.PerformIntegrityCheck(registeredObjects);

                    if (!integrityResult.isIntact)
                    {
                        var criticalIssues = integrityResult.issues.Where(i => i.severity == SaveDataIntegrityChecker.IssueSeverity.Critical).ToList();
                        if (criticalIssues.Count > 0)
                        {
                            result.success = false;
                            result.message = "加载前完整性检查发现严重问题";
                            result.errors.AddRange(criticalIssues.Select(i => i.description));
                            result.executionTime = Time.realtimeSinceStartup - startTime;
                            OnOperationCompleted?.Invoke(result);
                            yield break;
                        }
                        else
                        {
                            result.warnings.Add("加载前完整性检查发现非严重问题");
                            result.warnings.AddRange(integrityResult.issues.Select(i => i.description));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                result.warnings.Add($"完整性检查异常: {ex.Message}");
                LogError($"完整性检查异常: {ex.Message}");
            }

            // 2. 执行增强加载或标准加载
            Coroutine loadCoroutine = null;
            try
            {
                if (saveManagerEnhancer != null)
                {
                    LogDebug("执行增强加载");
                    loadCoroutine = saveManagerEnhancer.PerformEnhancedLoad(saveSlot);
                }
                else
                {
                    LogDebug("执行标准加载");
                    loadCoroutine = saveManager.LoadSave(saveSlot);
                }
            }
            catch (Exception ex)
            {
                result.success = false;
                result.message = "加载执行过程中发生异常";
                result.errors.Add(ex.Message);
                LogError($"加载执行异常: {ex.Message}");
                result.executionTime = Time.realtimeSinceStartup - startTime;
                OnOperationCompleted?.Invoke(result);
                yield break;
            }

            // 执行加载协程
            if (loadCoroutine != null)
            {
                yield return loadCoroutine;
            }

            // 3. 加载后数据验证
            try
            {
                if (validateData && saveDataValidator != null)
                {
                    LogDebug("执行加载后数据验证");
                    var registeredObjects = GetRegisteredSaveableObjects();
                    var validationResult = saveDataValidator.ValidateAfterLoad(registeredObjects);

                    if (!validationResult.isValid)
                    {
                        result.warnings.Add("加载后数据验证发现问题");
                        result.warnings.AddRange(validationResult.errors.Select(e => e.message));
                    }
                }
            }
            catch (Exception ex)
            {
                result.warnings.Add($"数据验证异常: {ex.Message}");
                LogError($"数据验证异常: {ex.Message}");
            }

            // 4. 跨场景ID同步
            Coroutine syncCoroutine = null;
            try
            {
                if (idManagerEnhancer != null)
                {
                    LogDebug("执行跨场景ID同步");
                    syncCoroutine = idManagerEnhancer.PerformCrossSceneSync();
                }
            }
            catch (Exception ex)
            {
                result.warnings.Add($"跨场景ID同步异常: {ex.Message}");
                LogError($"跨场景ID同步异常: {ex.Message}");
            }

            // 执行跨场景同步协程
            if (syncCoroutine != null)
            {
                yield return syncCoroutine;
            }

            // 5. 完成加载操作
            try
            {
                result.success = true;
                result.message = "增强加载操作完成";

                // 更新性能统计
                if (enablePerformanceMonitoring)
                {
                    performanceStats.totalLoadOperations++;
                    performanceStats.successfulLoads++;
                    performanceStats.averageLoadTime = (performanceStats.averageLoadTime + (Time.realtimeSinceStartup - startTime)) / 2f;
                    performanceStats.lastOperationTime = DateTime.Now;
                    OnPerformanceUpdate?.Invoke(performanceStats);
                }

                LogDebug($"增强加载完成: {saveSlot}");
            }
            catch (Exception ex)
            {
                result.success = false;
                result.message = "增强加载过程中发生异常";
                result.errors.Add(ex.Message);
                LogError($"增强加载异常: {ex.Message}");
            }
            finally
            {
                result.executionTime = Time.realtimeSinceStartup - startTime;
                OnOperationCompleted?.Invoke(result);
            }
        }
        #endregion

        #region 健康检查
        /// <summary>
        /// 启动健康检查
        /// </summary>
        private void StartHealthCheck()
        {
            if (healthCheckCoroutine == null)
            {
                healthCheckCoroutine = StartCoroutine(HealthCheckCoroutine());
                LogDebug("组件健康检查已启动");
            }
        }

        /// <summary>
        /// 停止健康检查
        /// </summary>
        private void StopHealthCheck()
        {
            if (healthCheckCoroutine != null)
            {
                StopCoroutine(healthCheckCoroutine);
                healthCheckCoroutine = null;
                LogDebug("组件健康检查已停止");
            }
        }

        /// <summary>
        /// 健康检查协程
        /// </summary>
        private IEnumerator HealthCheckCoroutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(healthCheckInterval);

                try
                {
                    CheckAllComponentsHealth();
                }
                catch (Exception ex)
                {
                    LogError($"定期健康检查过程中发生异常: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 检查所有组件健康状态
        /// </summary>
        private void CheckAllComponentsHealth()
        {
            var startTime = Time.realtimeSinceStartup;

            // 检查SaveManager
            CheckComponentHealth("SaveManager", () => saveManager != null && saveManager.isActiveAndEnabled);

            // 检查SaveManagerEnhancer
            CheckComponentHealth("SaveManagerEnhancer", () => saveManagerEnhancer != null && saveManagerEnhancer.isActiveAndEnabled);

            // 检查SaveDataValidator
            CheckComponentHealth("SaveDataValidator", () => saveDataValidator != null && saveDataValidator.isActiveAndEnabled);

            // 检查SaveTransactionManager
            CheckComponentHealth("SaveTransactionManager", () => saveTransactionManager != null && saveTransactionManager.isActiveAndEnabled);

            // 检查SaveDataIntegrityChecker
            CheckComponentHealth("SaveDataIntegrityChecker", () => saveDataIntegrityChecker != null && saveDataIntegrityChecker.isActiveAndEnabled);

            // 检查ItemInstanceIDManagerEnhancer
            CheckComponentHealth("ItemInstanceIDManagerEnhancer", () => idManagerEnhancer != null && idManagerEnhancer.isActiveAndEnabled);

            // 更新性能统计
            if (enablePerformanceMonitoring)
            {
                var checkTime = Time.realtimeSinceStartup - startTime;
                performanceStats.componentPerformance["HealthCheck"] = checkTime;
            }
        }

        /// <summary>
        /// 检查单个组件健康状态
        /// </summary>
        private void CheckComponentHealth(string componentName, Func<bool> healthCheck)
        {
            if (!componentHealthMap.ContainsKey(componentName))
            {
                componentHealthMap[componentName] = new ComponentHealth(componentName);
            }

            var health = componentHealthMap[componentName];
            var startTime = Time.realtimeSinceStartup;

            try
            {
                var wasWorking = health.isWorking;
                health.isAvailable = healthCheck();
                health.isInitialized = health.isAvailable;
                health.isWorking = health.isAvailable;
                health.lastError = null;
                health.lastCheckTime = DateTime.Now;
                health.responseTime = Time.realtimeSinceStartup - startTime;

                // 如果状态发生变化，触发事件
                if (wasWorking != health.isWorking)
                {
                    OnComponentHealthChanged?.Invoke(health);

                    if (health.isWorking)
                    {
                        LogDebug($"组件{componentName}恢复正常");
                    }
                    else
                    {
                        LogError($"组件{componentName}工作异常");
                    }
                }
            }
            catch (Exception ex)
            {
                health.isAvailable = false;
                health.isInitialized = false;
                health.isWorking = false;
                health.lastError = ex.Message;
                health.lastCheckTime = DateTime.Now;
                health.responseTime = Time.realtimeSinceStartup - startTime;

                OnComponentHealthChanged?.Invoke(health);
                LogError($"检查组件{componentName}健康状态时发生异常: {ex.Message}");
            }
        }
        #endregion

        #region 事件处理
        /// <summary>
        /// 处理增强保存完成事件
        /// </summary>
        private void OnEnhancedSaveCompleted(SaveManagerEnhancer.EnhancedSaveResult result)
        {
            var status = result.success ? "成功" : "失败";
            var errorInfo = result.errors.Count > 0 ? $", 错误: {string.Join(", ", result.errors)}" : "";
            LogDebug($"增强保存完成: {status}, 耗时: {result.totalTime:F2}秒{errorInfo}");
        }

        /// <summary>
        /// 处理增强加载完成事件
        /// </summary>
        private void OnEnhancedLoadCompleted(SaveManagerEnhancer.EnhancedLoadResult result)
        {
            var status = result.success ? "成功" : "失败";
            var errorInfo = result.errors.Count > 0 ? $", 错误: {string.Join(", ", result.errors)}" : "";
            LogDebug($"增强加载完成: {status}, 耗时: {result.totalTime:F2}秒{errorInfo}");
        }

        /// <summary>
        /// 处理验证完成事件
        /// </summary>
        private void OnValidationComplete(SaveDataValidator.ValidationResult result)
        {
            if (!result.isValid)
            {
                OnWarning?.Invoke("数据验证", $"验证发现{result.errors.Count}个错误和{result.warnings.Count}个警告");
            }
        }

        /// <summary>
        /// 处理验证错误事件
        /// </summary>
        private void OnValidationError(SaveDataValidator.ValidationError error)
        {
            OnError?.Invoke("数据验证", error.message);
        }

        /// <summary>
        /// 处理事务完成事件
        /// </summary>
        private void OnTransactionCompleted(SaveTransactionManager.SaveTransaction transaction)
        {
            LogDebug($"事务完成: {transaction.transactionId}");
        }

        /// <summary>
        /// 处理事务失败事件
        /// </summary>
        private void OnTransactionFailed(SaveTransactionManager.SaveTransaction transaction)
        {
            OnError?.Invoke("事务管理", transaction.errorMessage ?? "事务失败");
        }

        /// <summary>
        /// 处理完整性检查完成事件
        /// </summary>
        private void OnIntegrityCheckCompleted(SaveDataIntegrityChecker.IntegrityCheckResult result)
        {
            if (!result.isIntact)
            {
                OnWarning?.Invoke("完整性检查", $"发现{result.issues.Count}个完整性问题");
            }
        }

        /// <summary>
        /// 处理ID冲突检测事件
        /// </summary>
        private void OnIdConflictDetected(ItemInstanceIDManagerEnhancer.IdConflictInfo conflict)
        {
            OnWarning?.Invoke("ID管理", $"检测到ID冲突: {conflict.conflictId} ({conflict.type})");
        }

        /// <summary>
        /// 处理ID冲突解决事件
        /// </summary>
        private void OnIdConflictResolved(ItemInstanceIDManagerEnhancer.IdConflictInfo conflict)
        {
            LogDebug($"ID冲突已解决: {conflict.conflictId}");
        }
        #endregion

        #region 辅助方法
        /// <summary>
        /// 设置集成状态
        /// </summary>
        private void SetStatus(IntegrationStatus status)
        {
            if (currentStatus != status)
            {
                currentStatus = status;
                OnStatusChanged?.Invoke(status);
                LogDebug($"集成状态变更: {status}");
            }
        }

        /// <summary>
        /// 检查集成器是否就绪
        /// </summary>
        private bool IsReady()
        {
            return isInitialized && currentStatus == IntegrationStatus.Ready;
        }
        #endregion

        #region 公共查询方法
        /// <summary>
        /// 获取当前集成状态
        /// </summary>
        public IntegrationStatus GetCurrentStatus()
        {
            return currentStatus;
        }

        /// <summary>
        /// 获取组件健康状态
        /// </summary>
        public Dictionary<string, ComponentHealth> GetComponentHealth()
        {
            return new Dictionary<string, ComponentHealth>(componentHealthMap);
        }

        /// <summary>
        /// 获取性能统计
        /// </summary>
        public PerformanceStats GetPerformanceStats()
        {
            return performanceStats;
        }

        /// <summary>
        /// 获取集成器统计信息
        /// </summary>
        public string GetIntegratorStatistics()
        {
            var stats = new System.Text.StringBuilder();
            stats.AppendLine("=== 保存系统集成器统计信息 ===");
            stats.AppendLine($"当前状态: {currentStatus}");
            stats.AppendLine($"是否已初始化: {isInitialized}");
            stats.AppendLine($"增强保存: {(enableEnhancedSave ? "启用" : "禁用")}");
            stats.AppendLine($"增强加载: {(enableEnhancedLoad ? "启用" : "禁用")}");
            stats.AppendLine($"组件健康检查: {(enableComponentHealthCheck ? "启用" : "禁用")}");
            stats.AppendLine($"性能监控: {(enablePerformanceMonitoring ? "启用" : "禁用")}");
            stats.AppendLine($"健康检查间隔: {healthCheckInterval}秒");

            stats.AppendLine("\n=== 组件健康状态 ===");
            foreach (var kvp in componentHealthMap)
            {
                var health = kvp.Value;
                var status = health.isWorking ? "正常" : "异常";
                stats.AppendLine($"{health.componentName}: {status} (响应时间: {health.responseTime:F3}秒)");
                if (!string.IsNullOrEmpty(health.lastError))
                {
                    stats.AppendLine($"  最后错误: {health.lastError}");
                }
            }

            if (enablePerformanceMonitoring)
            {
                stats.AppendLine("\n=== 性能统计 ===");
                stats.AppendLine($"总保存操作: {performanceStats.totalSaveOperations}");
                stats.AppendLine($"成功保存: {performanceStats.successfulSaves}");
                stats.AppendLine($"总加载操作: {performanceStats.totalLoadOperations}");
                stats.AppendLine($"成功加载: {performanceStats.successfulLoads}");
                stats.AppendLine($"平均保存时间: {performanceStats.averageSaveTime:F3}秒");
                stats.AppendLine($"平均加载时间: {performanceStats.averageLoadTime:F3}秒");
                stats.AppendLine($"最后操作时间: {performanceStats.lastOperationTime}");
            }

            return stats.ToString();
        }

        /// <summary>
        /// 重置性能统计
        /// </summary>
        public void ResetPerformanceStats()
        {
            performanceStats = new PerformanceStats();
            LogDebug("性能统计已重置");
        }

        /// <summary>
        /// 启用/禁用增强功能
        /// </summary>
        public void SetEnhancedFeatures(bool enableSave, bool enableLoad)
        {
            enableEnhancedSave = enableSave;
            enableEnhancedLoad = enableLoad;
            LogDebug($"增强功能设置: 保存={enableSave}, 加载={enableLoad}");
        }
        #endregion

        #region 调试方法
        /// <summary>
        /// 输出调试日志
        /// </summary>
        private void LogDebug(string message)
        {
            if (enableDebugLog)
            {
                Debug.Log($"[SaveSystemIntegrator] {message}");
            }
        }

        /// <summary>
        /// 输出错误日志
        /// </summary>
        private void LogError(string message)
        {
            Debug.LogError($"[SaveSystemIntegrator] {message}");
        }

        /// <summary>
        /// 获取已注册的可保存对象
        /// </summary>
        private Dictionary<string, ISaveable> GetRegisteredSaveableObjects()
        {
            var objects = new Dictionary<string, ISaveable>();

            if (saveManager != null)
            {
                // 通过反射获取SaveManager中的注册对象
                try
                {
                    var saveManagerType = typeof(SaveManager);
                    var registeredObjectsField = saveManagerType.GetField("registeredObjects",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                    if (registeredObjectsField != null)
                    {
                        var registeredObjects = registeredObjectsField.GetValue(saveManager) as Dictionary<string, ISaveable>;
                        if (registeredObjects != null)
                        {
                            foreach (var kvp in registeredObjects)
                            {
                                objects[kvp.Key] = kvp.Value;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogError($"获取注册对象时发生异常: {ex.Message}");
                }
            }

            return objects;
        }
        #endregion
    }
}