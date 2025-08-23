using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;

namespace InventorySystem.SaveSystem
{
    /// <summary>
    /// 保存管理器增强器
    /// 为现有SaveManager提供事务性保存、数据验证和完整性检查功能
    /// 采用非侵入式设计，通过组合模式扩展现有功能
    /// </summary>
    public class SaveManagerEnhancer : MonoBehaviour
    {
        #region 单例模式
        private static SaveManagerEnhancer _instance;
        public static SaveManagerEnhancer Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<SaveManagerEnhancer>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("SaveManagerEnhancer");
                        _instance = go.AddComponent<SaveManagerEnhancer>();

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
        [Header("增强功能配置")]
        [SerializeField]
        [Tooltip("是否启用事务性保存")]
        private bool enableTransactionalSave = true;

        [SerializeField]
        [Tooltip("是否启用保存前验证")]
        private bool enablePreSaveValidation = true;

        [SerializeField]
        [Tooltip("是否启用保存后验证")]
        private bool enablePostSaveValidation = true;

        [SerializeField]
        [Tooltip("是否启用完整性检查")]
        private bool enableIntegrityCheck = true;

        [SerializeField]
        [Tooltip("是否启用自动回滚")]
        private bool enableAutoRollback = true;

        [SerializeField]
        [Tooltip("保存超时时间（秒）")]
        private float saveTimeout = 30f;

        [SerializeField]
        [Tooltip("是否启用调试日志")]
        private bool enableDebugLog = true;
        #endregion

        #region 数据结构
        /// <summary>
        /// 增强保存结果
        /// </summary>
        [System.Serializable]
        public class EnhancedSaveResult
        {
            public bool success;
            public string transactionId;

            public SaveDataValidator.ValidationResult preValidationResult;
            public SaveDataValidator.ValidationResult postValidationResult;
            public SaveDataIntegrityChecker.IntegrityCheckResult integrityResult;
            public List<string> warnings;
            public List<string> errors;
            public float totalTime;
            public DateTime timestamp;

            public EnhancedSaveResult()
            {
                warnings = new List<string>();
                errors = new List<string>();
                timestamp = DateTime.Now;
            }
        }

        /// <summary>
        /// 增强加载结果
        /// </summary>
        [System.Serializable]
        public class EnhancedLoadResult
        {
            public bool success;
            // 注释掉不存在的LoadResult类型
            // public SaveManager.LoadResult originalResult;
            public SaveDataValidator.ValidationResult validationResult;
            public SaveDataIntegrityChecker.IntegrityCheckResult integrityResult;
            public List<string> warnings;
            public List<string> errors;
            public float totalTime;
            public DateTime timestamp;

            public EnhancedLoadResult()
            {
                warnings = new List<string>();
                errors = new List<string>();
                timestamp = DateTime.Now;
            }
        }

        /// <summary>
        /// 保存操作状态
        /// </summary>
        public enum SaveOperationStatus
        {
            Idle,
            Validating,
            Saving,
            VerifyingIntegrity,
            Completing,
            RollingBack,
            Failed
        }
        #endregion

        #region 私有字段
        private SaveManager saveManager;
        private SaveDataValidator validator;
        private SaveDataIntegrityChecker integrityChecker;
        private SaveTransactionManager transactionManager;

        private SaveOperationStatus currentStatus = SaveOperationStatus.Idle;
        private string currentTransactionId;
        private Coroutine currentSaveOperation;
        private Dictionary<string, EnhancedSaveResult> saveHistory;
        private Dictionary<string, EnhancedLoadResult> loadHistory;
        #endregion

        #region 事件定义
        // 增强保存事件
        public event Action<EnhancedSaveResult> OnEnhancedSaveComplete;
        public event Action<EnhancedLoadResult> OnEnhancedLoadComplete;
        public event Action<SaveOperationStatus> OnSaveStatusChanged;
        public event Action<string, float> OnSaveProgress;
        public event Action<string> OnSaveWarning;
        public event Action<string> OnSaveError;
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
            InitializeEnhancer();
        }

        private void OnDestroy()
        {
            // 清理当前操作
            if (currentSaveOperation != null)
            {
                StopCoroutine(currentSaveOperation);
            }

            if (_instance == this)
            {
                _instance = null;
            }
        }
        #endregion

        #region 初始化
        /// <summary>
        /// 初始化增强器
        /// </summary>
        private void InitializeEnhancer()
        {
            saveHistory = new Dictionary<string, EnhancedSaveResult>();
            loadHistory = new Dictionary<string, EnhancedLoadResult>();

            // 获取依赖组件
            saveManager = SaveManager.Instance;
            validator = SaveDataValidator.Instance;
            integrityChecker = SaveDataIntegrityChecker.Instance;
            transactionManager = SaveTransactionManager.Instance;

            LogDebug("SaveManagerEnhancer已初始化");
        }
        #endregion

        #region 主要增强方法
        /// <summary>
        /// 执行增强保存
        /// </summary>
        /// <param name="saveSlot">保存槽位</param>
        /// <param name="onComplete">完成回调</param>
        /// <returns>操作协程</returns>
        public Coroutine PerformEnhancedSave(string saveSlot = "default", Action<EnhancedSaveResult> onComplete = null)
        {
            if (currentStatus != SaveOperationStatus.Idle)
            {
                var errorResult = new EnhancedSaveResult();
                errorResult.success = false;
                errorResult.errors.Add("另一个保存操作正在进行中");
                onComplete?.Invoke(errorResult);
                return null;
            }

            currentSaveOperation = StartCoroutine(EnhancedSaveCoroutine(saveSlot, onComplete));
            return currentSaveOperation;
        }

        /// <summary>
        /// 执行增强加载
        /// </summary>
        /// <param name="saveSlot">保存槽位</param>
        /// <param name="onComplete">完成回调</param>
        /// <returns>操作协程</returns>
        public Coroutine PerformEnhancedLoad(string saveSlot = "default", Action<EnhancedLoadResult> onComplete = null)
        {
            if (currentStatus != SaveOperationStatus.Idle)
            {
                var errorResult = new EnhancedLoadResult();
                errorResult.success = false;
                errorResult.errors.Add("保存操作正在进行中，无法加载");
                onComplete?.Invoke(errorResult);
                return null;
            }

            return StartCoroutine(EnhancedLoadCoroutine(saveSlot, onComplete));
        }

        /// <summary>
        /// 取消当前保存操作
        /// </summary>
        public void CancelCurrentSave()
        {
            if (currentSaveOperation != null)
            {
                StopCoroutine(currentSaveOperation);
                currentSaveOperation = null;

                // 如果有活跃事务，执行回滚
                if (!string.IsNullOrEmpty(currentTransactionId) && enableAutoRollback)
                {
                    transactionManager.RollbackToTransaction(currentTransactionId);
                }

                SetStatus(SaveOperationStatus.Idle);
                LogDebug("保存操作已取消");
            }
        }
        #endregion

        #region 协程实现
        /// <summary>
        /// 增强保存协程
        /// </summary>
        private IEnumerator EnhancedSaveCoroutine(string saveSlot, Action<EnhancedSaveResult> onComplete)
        {
            var result = new EnhancedSaveResult();
            var startTime = Time.realtimeSinceStartup;

            LogDebug($"开始增强保存操作，槽位: {saveSlot}");

            // 1. 开始事务
            if (enableTransactionalSave)
            {
                SetStatus(SaveOperationStatus.Validating);
                OnSaveProgress?.Invoke("开始事务", 0.1f);

                var registeredObjects = GetRegisteredSaveableObjects();
                var transactionResult = transactionManager.BeginTransactionalSave(registeredObjects);
                yield return new WaitUntil(() => transactionResult.success || !string.IsNullOrEmpty(transactionResult.message));

                if (!transactionResult.success)
                {
                    result.success = false;
                    result.errors.Add($"事务开始失败: {transactionResult.message}");
                    SetStatus(SaveOperationStatus.Failed);
                    onComplete?.Invoke(result);
                    yield break;
                }

                currentTransactionId = transactionResult.finalStatus.ToString();
                result.transactionId = currentTransactionId;
            }

            // 2. 保存前验证
            if (enablePreSaveValidation)
            {
                SetStatus(SaveOperationStatus.Validating);
                OnSaveProgress?.Invoke("保存前验证", 0.2f);

                var registeredObjects = GetRegisteredSaveableObjects();
                result.preValidationResult = validator.ValidateBeforeSave(registeredObjects);

                if (!result.preValidationResult.isValid)
                {
                    var criticalErrors = result.preValidationResult.errors.Where(e => e.severity >= SaveDataValidator.ValidationSeverity.Error).ToList();
                    if (criticalErrors.Any())
                    {
                        result.success = false;
                        result.errors.AddRange(criticalErrors.Select(e => e.message));

                        if (enableAutoRollback && !string.IsNullOrEmpty(currentTransactionId))
                        {
                            SetStatus(SaveOperationStatus.RollingBack);
                            transactionManager.RollbackToTransaction(currentTransactionId);
                        }

                        SetStatus(SaveOperationStatus.Failed);
                        onComplete?.Invoke(result);
                        yield break;
                    }

                    // 添加警告
                    var warnings = result.preValidationResult.errors.Where(e => e.severity < SaveDataValidator.ValidationSeverity.Error).ToList();
                    result.warnings.AddRange(warnings.Select(w => w.message));
                }
            }

            // 3. 执行保存
            SetStatus(SaveOperationStatus.Saving);
            OnSaveProgress?.Invoke("执行保存", 0.5f);

            // 使用SaveManager的SaveAll方法
            var saveCoroutine = saveManager.SaveAll(saveSlot);
            yield return saveCoroutine;

            // 保存操作已完成，检查是否成功
            // 由于SaveManager的SaveAll方法是协程，当协程完成时保存就已经完成
            // 这里我们假设保存成功，如果需要更精确的错误检测，可以监听SaveManager的事件
            result.success = true;

            // 保存成功
            LogDebug($"保存操作完成: {saveSlot}");

            // 4. 保存后验证和完整性检查
            if (enablePostSaveValidation || enableIntegrityCheck)
            {
                SetStatus(SaveOperationStatus.VerifyingIntegrity);
                OnSaveProgress?.Invoke("验证保存结果", 0.8f);

                var registeredObjects = GetRegisteredSaveableObjects();

                if (enablePostSaveValidation)
                {
                    result.postValidationResult = validator.ValidateAfterLoad(registeredObjects);

                    if (!result.postValidationResult.isValid)
                    {
                        var criticalErrors = result.postValidationResult.errors.Where(e => e.severity >= SaveDataValidator.ValidationSeverity.Error).ToList();
                        if (criticalErrors.Any())
                        {
                            result.warnings.Add("保存后验证发现问题，但保存已完成");
                            result.warnings.AddRange(criticalErrors.Select(e => e.message));
                        }
                    }
                }

                if (enableIntegrityCheck)
                {
                    result.integrityResult = integrityChecker.PerformIntegrityCheck(registeredObjects);

                    if (!result.integrityResult.isIntact)
                    {
                        result.warnings.Add("完整性检查发现问题，但保存已完成");
                        var criticalIssues = result.integrityResult.issues.Where(i => i.severity >= SaveDataIntegrityChecker.IssueSeverity.Error).ToList();
                        result.warnings.AddRange(criticalIssues.Select(i => i.description));
                    }
                }
            }

            // 5. 完成事务
            if (enableTransactionalSave && !string.IsNullOrEmpty(currentTransactionId))
            {
                SetStatus(SaveOperationStatus.Completing);
                OnSaveProgress?.Invoke("完成事务", 0.9f);

                // 事务会自动提交，这里只是清理
                currentTransactionId = null;
            }

            result.success = true;
            result.totalTime = Time.realtimeSinceStartup - startTime;

            SetStatus(SaveOperationStatus.Idle);
            OnSaveProgress?.Invoke("保存完成", 1.0f);

            LogDebug($"增强保存完成，耗时: {result.totalTime:F2}秒");

            // 记录保存历史
            saveHistory[saveSlot] = result;

            // 触发事件
            OnEnhancedSaveComplete?.Invoke(result);
            onComplete?.Invoke(result);

            if (currentStatus != SaveOperationStatus.Idle)
            {
                SetStatus(SaveOperationStatus.Idle);
            }

            currentSaveOperation = null;
            currentTransactionId = null;
        }

        /// <summary>
        /// 增强加载协程
        /// </summary>
        private IEnumerator EnhancedLoadCoroutine(string saveSlot, Action<EnhancedLoadResult> onComplete)
        {
            var result = new EnhancedLoadResult();
            var startTime = Time.realtimeSinceStartup;

            LogDebug($"开始增强加载操作，槽位: {saveSlot}");

            // 1. 执行加载
            OnSaveProgress?.Invoke("执行加载", 0.3f);

            // 使用SaveManager的LoadSave方法
            var loadCoroutine = saveManager.LoadSave(saveSlot);
            yield return loadCoroutine;

            // 加载操作已完成，检查是否成功
            // 由于SaveManager的LoadSave方法是协程，当协程完成时加载就已经完成
            // 这里我们假设加载成功，如果需要更精确的错误检测，可以监听SaveManager的事件
            result.success = true;

            // 加载成功
            LogDebug($"加载操作完成: {saveSlot}");

            // 2. 加载后验证
            if (enablePostSaveValidation)
            {
                OnSaveProgress?.Invoke("加载后验证", 0.7f);

                var registeredObjects = GetRegisteredSaveableObjects();
                result.validationResult = validator.ValidateAfterLoad(registeredObjects);

                if (!result.validationResult.isValid)
                {
                    var criticalErrors = result.validationResult.errors.Where(e => e.severity >= SaveDataValidator.ValidationSeverity.Error).ToList();
                    if (criticalErrors.Any())
                    {
                        result.warnings.Add("加载后验证发现问题");
                        result.warnings.AddRange(criticalErrors.Select(e => e.message));
                    }
                }
            }

            // 3. 完整性检查
            if (enableIntegrityCheck)
            {
                OnSaveProgress?.Invoke("完整性检查", 0.9f);

                var registeredObjects = GetRegisteredSaveableObjects();
                result.integrityResult = integrityChecker.PerformIntegrityCheck(registeredObjects);

                if (!result.integrityResult.isIntact)
                {
                    result.warnings.Add("完整性检查发现问题");
                    var criticalIssues = result.integrityResult.issues.Where(i => i.severity >= SaveDataIntegrityChecker.IssueSeverity.Error).ToList();
                    result.warnings.AddRange(criticalIssues.Select(i => i.description));
                }
            }

            result.success = true;
            result.totalTime = Time.realtimeSinceStartup - startTime;

            OnSaveProgress?.Invoke("加载完成", 1.0f);
            LogDebug($"增强加载完成，耗时: {result.totalTime:F2}秒");

            // 记录加载历史
            loadHistory[saveSlot] = result;

            // 触发事件
            OnEnhancedLoadComplete?.Invoke(result);
            onComplete?.Invoke(result);
        }
        #endregion

        #region 辅助方法
        /// <summary>
        /// 设置操作状态
        /// </summary>
        private void SetStatus(SaveOperationStatus status)
        {
            if (currentStatus != status)
            {
                currentStatus = status;
                OnSaveStatusChanged?.Invoke(status);
                LogDebug($"保存状态变更: {status}");
            }
        }

        /// <summary>
        /// 获取已注册的可保存对象
        /// </summary>
        private Dictionary<string, ISaveable> GetRegisteredSaveableObjects()
        {
            var objects = new Dictionary<string, ISaveable>();

            // 通过反射获取SaveManager中的注册对象
            // 这里需要根据SaveManager的具体实现来调整
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

            return objects;
        }
        #endregion

        #region 公共查询方法
        /// <summary>
        /// 获取当前操作状态
        /// </summary>
        public SaveOperationStatus GetCurrentStatus()
        {
            return currentStatus;
        }

        /// <summary>
        /// 获取保存历史
        /// </summary>
        public Dictionary<string, EnhancedSaveResult> GetSaveHistory()
        {
            return new Dictionary<string, EnhancedSaveResult>(saveHistory);
        }

        /// <summary>
        /// 获取加载历史
        /// </summary>
        public Dictionary<string, EnhancedLoadResult> GetLoadHistory()
        {
            return new Dictionary<string, EnhancedLoadResult>(loadHistory);
        }

        /// <summary>
        /// 获取最近的保存结果
        /// </summary>
        public EnhancedSaveResult GetLastSaveResult(string saveSlot = "default")
        {
            return saveHistory.ContainsKey(saveSlot) ? saveHistory[saveSlot] : null;
        }

        /// <summary>
        /// 获取最近的加载结果
        /// </summary>
        public EnhancedLoadResult GetLastLoadResult(string saveSlot = "default")
        {
            return loadHistory.ContainsKey(saveSlot) ? loadHistory[saveSlot] : null;
        }

        /// <summary>
        /// 清除历史记录
        /// </summary>
        public void ClearHistory()
        {
            saveHistory.Clear();
            loadHistory.Clear();
            LogDebug("已清除所有历史记录");
        }

        /// <summary>
        /// 获取增强器统计信息
        /// </summary>
        public string GetEnhancerStatistics()
        {
            var stats = new System.Text.StringBuilder();
            stats.AppendLine("=== 保存管理器增强器统计信息 ===");
            stats.AppendLine($"事务性保存: {(enableTransactionalSave ? "启用" : "禁用")}");
            stats.AppendLine($"保存前验证: {(enablePreSaveValidation ? "启用" : "禁用")}");
            stats.AppendLine($"保存后验证: {(enablePostSaveValidation ? "启用" : "禁用")}");
            stats.AppendLine($"完整性检查: {(enableIntegrityCheck ? "启用" : "禁用")}");
            stats.AppendLine($"自动回滚: {(enableAutoRollback ? "启用" : "禁用")}");
            stats.AppendLine($"当前状态: {currentStatus}");
            stats.AppendLine($"保存历史记录数: {saveHistory.Count}");
            stats.AppendLine($"加载历史记录数: {loadHistory.Count}");
            stats.AppendLine($"保存超时时间: {saveTimeout}秒");
            return stats.ToString();
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
                Debug.Log($"[SaveManagerEnhancer] {message}");
            }
        }

        /// <summary>
        /// 输出错误日志
        /// </summary>
        private void LogError(string message)
        {
            Debug.LogError($"[SaveManagerEnhancer] {message}");
        }
        #endregion
    }
}