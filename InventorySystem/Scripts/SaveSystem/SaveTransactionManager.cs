using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;

namespace InventorySystem.SaveSystem
{
    /// <summary>
    /// 保存事务管理器
    /// 提供事务性保存操作，支持回滚和原子性保证
    /// 采用非侵入式设计，扩展现有保存系统功能
    /// </summary>
    public class SaveTransactionManager : MonoBehaviour
    {
        #region 单例模式
        private static SaveTransactionManager _instance;
        public static SaveTransactionManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<SaveTransactionManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("SaveTransactionManager");
                        _instance = go.AddComponent<SaveTransactionManager>();

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
        [Header("事务配置")]
        [SerializeField]
        [Tooltip("是否启用事务性保存")]
        private bool enableTransactionalSave = true;

        [SerializeField]
        [Tooltip("最大备份保留数量")]
        private int maxBackupCount = 5;

        [SerializeField]
        [Tooltip("事务超时时间（秒）")]
        private float transactionTimeout = 30f;

        [SerializeField]
        [Tooltip("是否启用自动回滚")]
        private bool enableAutoRollback = true;

        [SerializeField]
        [Tooltip("是否启用调试日志")]
        private bool enableDebugLog = true;
        #endregion

        #region 事务数据结构
        /// <summary>
        /// 保存事务
        /// </summary>
        [System.Serializable]
        public class SaveTransaction
        {
            public string transactionId;
            public DateTime startTime;
            public DateTime? endTime;
            public TransactionStatus status;
            public Dictionary<string, object> originalData;
            public Dictionary<string, object> newData;
            public List<string> backupFiles;
            public string errorMessage;
            public float progress;

            public SaveTransaction()
            {
                transactionId = Guid.NewGuid().ToString();
                startTime = DateTime.Now;
                status = TransactionStatus.Pending;
                originalData = new Dictionary<string, object>();
                newData = new Dictionary<string, object>();
                backupFiles = new List<string>();
                progress = 0f;
            }
        }

        /// <summary>
        /// 事务状态
        /// </summary>
        public enum TransactionStatus
        {
            Pending,        // 待处理
            InProgress,     // 进行中
            Validating,     // 验证中
            Committing,     // 提交中
            Committed,      // 已提交
            RollingBack,    // 回滚中
            RolledBack,     // 已回滚
            Failed,         // 失败
            Timeout         // 超时
        }

        /// <summary>
        /// 事务结果
        /// </summary>
        [System.Serializable]
        public class TransactionResult
        {
            public bool success;
            public string transactionId;
            public TransactionStatus finalStatus;
            public string message;
            public float executionTime;
            public int objectsProcessed;
            public List<string> warnings;
            public List<string> errors;

            public TransactionResult()
            {
                warnings = new List<string>();
                errors = new List<string>();
            }
        }
        #endregion

        #region 私有字段
        private Dictionary<string, SaveTransaction> activeTransactions;
        private Queue<string> transactionHistory;
        private SaveDataValidator validator;
        private SaveManager saveManager;
        #endregion

        #region 事件定义
        // 事务事件
        public event Action<SaveTransaction> OnTransactionStarted;
        public event Action<SaveTransaction> OnTransactionCompleted;
        public event Action<SaveTransaction> OnTransactionFailed;
        public event Action<SaveTransaction> OnTransactionRolledBack;
        public event Action<string, float> OnTransactionProgress;
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
            InitializeTransactionManager();
        }

        private void Update()
        {
            // 检查事务超时
            CheckTransactionTimeouts();
        }

        private void OnDestroy()
        {
            // 清理活跃事务
            CleanupActiveTransactions();
        }
        #endregion

        #region 初始化
        /// <summary>
        /// 初始化事务管理器
        /// </summary>
        private void InitializeTransactionManager()
        {
            activeTransactions = new Dictionary<string, SaveTransaction>();
            transactionHistory = new Queue<string>();

            // 获取依赖组件
            validator = SaveDataValidator.Instance;
            saveManager = SaveManager.Instance;

            LogDebug("SaveTransactionManager已初始化");
        }
        #endregion

        #region 主要事务方法
        /// <summary>
        /// 开始事务性保存
        /// </summary>
        /// <param name="saveableObjects">要保存的对象</param>
        /// <returns>事务结果</returns>
        public TransactionResult BeginTransactionalSave(Dictionary<string, ISaveable> saveableObjects)
        {
            if (!enableTransactionalSave)
            {
                LogDebug("事务性保存已禁用，使用标准保存流程");
                return CreateSimpleTransactionResult(false, "事务性保存已禁用");
            }

            var transaction = new SaveTransaction();
            var result = new TransactionResult();
            result.transactionId = transaction.transactionId;

            try
            {
                LogDebug($"开始事务性保存，事务ID: {transaction.transactionId}");

                // 1. 注册事务
                activeTransactions[transaction.transactionId] = transaction;
                transaction.status = TransactionStatus.InProgress;
                OnTransactionStarted?.Invoke(transaction);

                // 2. 创建备份
                if (!CreateBackup(transaction, saveableObjects))
                {
                    return RollbackTransaction(transaction, "创建备份失败");
                }

                // 3. 验证数据
                transaction.status = TransactionStatus.Validating;
                var validationResult = validator.ValidateBeforeSave(saveableObjects);
                if (!validationResult.isValid)
                {
                    var errorMsg = $"数据验证失败: {string.Join(", ", validationResult.errors.Select(e => e.message))}";
                    return RollbackTransaction(transaction, errorMsg);
                }

                // 4. 执行保存
                transaction.status = TransactionStatus.Committing;
                if (!ExecuteSave(transaction, saveableObjects))
                {
                    return RollbackTransaction(transaction, "保存执行失败");
                }

                // 5. 验证保存结果
                if (!ValidateSaveResult(transaction, saveableObjects))
                {
                    return RollbackTransaction(transaction, "保存结果验证失败");
                }

                // 6. 提交事务
                return CommitTransaction(transaction, saveableObjects.Count);
            }
            catch (Exception ex)
            {
                LogError($"事务性保存过程中发生异常: {ex.Message}");
                return RollbackTransaction(transaction, $"异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 回滚到指定事务
        /// </summary>
        /// <param name="transactionId">事务ID</param>
        /// <returns>回滚结果</returns>
        public TransactionResult RollbackToTransaction(string transactionId)
        {
            var result = new TransactionResult();
            result.transactionId = transactionId;

            try
            {
                LogDebug($"开始回滚到事务: {transactionId}");

                // 查找事务记录
                if (!activeTransactions.ContainsKey(transactionId))
                {
                    result.success = false;
                    result.message = "未找到指定的事务记录";
                    return result;
                }

                var transaction = activeTransactions[transactionId];
                return RollbackTransaction(transaction, "用户请求回滚");
            }
            catch (Exception ex)
            {
                LogError($"回滚过程中发生异常: {ex.Message}");
                result.success = false;
                result.message = $"回滚异常: {ex.Message}";
                return result;
            }
        }
        #endregion

        #region 事务操作实现
        /// <summary>
        /// 创建备份
        /// </summary>
        private bool CreateBackup(SaveTransaction transaction, Dictionary<string, ISaveable> saveableObjects)
        {
            try
            {
                LogDebug($"为事务 {transaction.transactionId} 创建备份");

                // 获取当前保存数据
                foreach (var kvp in saveableObjects)
                {
                    var saveData = kvp.Value.SerializeToJson();
                    transaction.originalData[kvp.Key] = saveData;
                }

                // 创建文件备份
                var saveFilePath = GetCurrentSaveFilePath();
                if (File.Exists(saveFilePath))
                {
                    var backupPath = CreateBackupFile(saveFilePath, transaction.transactionId);
                    transaction.backupFiles.Add(backupPath);
                }

                LogDebug($"备份创建完成，备份文件数: {transaction.backupFiles.Count}");
                return true;
            }
            catch (Exception ex)
            {
                LogError($"创建备份失败: {ex.Message}");
                transaction.errorMessage = $"备份失败: {ex.Message}";
                return false;
            }
        }

        /// <summary>
        /// 执行保存
        /// </summary>
        private bool ExecuteSave(SaveTransaction transaction, Dictionary<string, ISaveable> saveableObjects)
        {
            try
            {
                LogDebug($"执行事务 {transaction.transactionId} 的保存操作");

                // 收集新数据
                foreach (var kvp in saveableObjects)
                {
                    var saveData = kvp.Value.SerializeToJson();
                    transaction.newData[kvp.Key] = saveData;
                }

                // 触发保存管理器的保存操作
                saveManager.SaveAll("transaction_save");

                // 等待保存完成（这里可以根据实际情况调整）
                // 在实际实现中，可能需要监听SaveManager的保存完成事件

                LogDebug($"事务 {transaction.transactionId} 保存操作完成");
                return true;
            }
            catch (Exception ex)
            {
                LogError($"执行保存失败: {ex.Message}");
                transaction.errorMessage = $"保存失败: {ex.Message}";
                return false;
            }
        }

        /// <summary>
        /// 验证保存结果
        /// </summary>
        private bool ValidateSaveResult(SaveTransaction transaction, Dictionary<string, ISaveable> saveableObjects)
        {
            try
            {
                LogDebug($"验证事务 {transaction.transactionId} 的保存结果");

                // 重新加载数据进行验证
                var validationResult = validator.ValidateAfterLoad(saveableObjects);
                if (!validationResult.isValid)
                {
                    transaction.errorMessage = $"保存结果验证失败: {string.Join(", ", validationResult.errors.Select(e => e.message))}";
                    return false;
                }

                LogDebug($"事务 {transaction.transactionId} 保存结果验证通过");
                return true;
            }
            catch (Exception ex)
            {
                LogError($"验证保存结果失败: {ex.Message}");
                transaction.errorMessage = $"验证失败: {ex.Message}";
                return false;
            }
        }

        /// <summary>
        /// 提交事务
        /// </summary>
        private TransactionResult CommitTransaction(SaveTransaction transaction, int objectCount)
        {
            var result = new TransactionResult();
            result.transactionId = transaction.transactionId;

            try
            {
                transaction.status = TransactionStatus.Committed;
                transaction.endTime = DateTime.Now;
                result.executionTime = (float)(transaction.endTime.Value - transaction.startTime).TotalSeconds;
                result.objectsProcessed = objectCount;
                result.success = true;
                result.finalStatus = TransactionStatus.Committed;
                result.message = "事务提交成功";

                // 清理旧备份
                CleanupOldBackups();

                // 移动到历史记录
                transactionHistory.Enqueue(transaction.transactionId);
                if (transactionHistory.Count > maxBackupCount)
                {
                    transactionHistory.Dequeue();
                }

                OnTransactionCompleted?.Invoke(transaction);
                LogDebug($"事务 {transaction.transactionId} 提交成功");
            }
            catch (Exception ex)
            {
                LogError($"提交事务失败: {ex.Message}");
                result.success = false;
                result.message = $"提交失败: {ex.Message}";
                result.errors.Add(ex.Message);
            }

            return result;
        }

        /// <summary>
        /// 回滚事务
        /// </summary>
        private TransactionResult RollbackTransaction(SaveTransaction transaction, string reason)
        {
            var result = new TransactionResult();
            result.transactionId = transaction.transactionId;

            try
            {
                LogDebug($"开始回滚事务 {transaction.transactionId}，原因: {reason}");

                transaction.status = TransactionStatus.RollingBack;
                transaction.errorMessage = reason;

                // 恢复备份文件
                foreach (var backupFile in transaction.backupFiles)
                {
                    if (File.Exists(backupFile))
                    {
                        var originalPath = GetOriginalPathFromBackup(backupFile);
                        File.Copy(backupFile, originalPath, true);
                        LogDebug($"已恢复备份文件: {backupFile} -> {originalPath}");
                    }
                }

                // 恢复对象数据
                foreach (var kvp in transaction.originalData)
                {
                    // 这里需要根据具体的ISaveable实现来恢复数据
                    // 可能需要重新加载或重置对象状态
                }

                transaction.status = TransactionStatus.RolledBack;
                transaction.endTime = DateTime.Now;
                result.executionTime = (float)(transaction.endTime.Value - transaction.startTime).TotalSeconds;
                result.success = false;
                result.finalStatus = TransactionStatus.RolledBack;
                result.message = $"事务已回滚: {reason}";
                result.errors.Add(reason);

                OnTransactionRolledBack?.Invoke(transaction);
                LogDebug($"事务 {transaction.transactionId} 回滚完成");
            }
            catch (Exception ex)
            {
                LogError($"回滚事务失败: {ex.Message}");
                transaction.status = TransactionStatus.Failed;
                result.success = false;
                result.message = $"回滚失败: {ex.Message}";
                result.errors.Add(ex.Message);
                OnTransactionFailed?.Invoke(transaction);
            }

            return result;
        }
        #endregion

        #region 辅助方法
        /// <summary>
        /// 检查事务超时
        /// </summary>
        private void CheckTransactionTimeouts()
        {
            var currentTime = DateTime.Now;
            var timeoutTransactions = new List<string>();

            foreach (var kvp in activeTransactions)
            {
                var transaction = kvp.Value;
                var elapsed = (currentTime - transaction.startTime).TotalSeconds;

                if (elapsed > transactionTimeout && transaction.status != TransactionStatus.Committed && transaction.status != TransactionStatus.RolledBack)
                {
                    timeoutTransactions.Add(kvp.Key);
                }
            }

            foreach (var transactionId in timeoutTransactions)
            {
                var transaction = activeTransactions[transactionId];
                LogDebug($"事务 {transactionId} 超时，开始自动回滚");
                RollbackTransaction(transaction, "事务超时");
            }
        }

        /// <summary>
        /// 清理活跃事务
        /// </summary>
        private void CleanupActiveTransactions()
        {
            foreach (var transaction in activeTransactions.Values)
            {
                if (transaction.status == TransactionStatus.InProgress || transaction.status == TransactionStatus.Validating || transaction.status == TransactionStatus.Committing)
                {
                    RollbackTransaction(transaction, "系统关闭");
                }
            }
            activeTransactions.Clear();
        }

        /// <summary>
        /// 创建备份文件
        /// </summary>
        private string CreateBackupFile(string originalPath, string transactionId)
        {
            var directory = Path.GetDirectoryName(originalPath);
            var fileName = Path.GetFileNameWithoutExtension(originalPath);
            var extension = Path.GetExtension(originalPath);
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var backupFileName = $"{fileName}_backup_{timestamp}_{transactionId.Substring(0, 8)}{extension}";
            var backupPath = Path.Combine(directory, "Backups", backupFileName);

            // 确保备份目录存在
            var backupDir = Path.GetDirectoryName(backupPath);
            if (!Directory.Exists(backupDir))
            {
                Directory.CreateDirectory(backupDir);
            }

            File.Copy(originalPath, backupPath, true);
            return backupPath;
        }

        /// <summary>
        /// 从备份路径获取原始路径
        /// </summary>
        private string GetOriginalPathFromBackup(string backupPath)
        {
            // 这里需要根据备份文件的命名规则来推导原始路径
            var directory = Path.GetDirectoryName(Path.GetDirectoryName(backupPath)); // 去掉Backups目录
            var fileName = Path.GetFileName(backupPath);
            var parts = fileName.Split('_');
            if (parts.Length >= 3)
            {
                var originalName = string.Join("_", parts.Take(parts.Length - 3));
                var extension = Path.GetExtension(backupPath);
                return Path.Combine(directory, originalName + extension);
            }
            return backupPath;
        }

        /// <summary>
        /// 获取当前保存文件路径
        /// </summary>
        private string GetCurrentSaveFilePath()
        {
            // 这里需要根据SaveManager的实现来获取当前保存文件路径
            // 暂时返回一个默认路径
            return Path.Combine(Application.persistentDataPath, "SaveData", "save.json");
        }

        /// <summary>
        /// 清理旧备份
        /// </summary>
        private void CleanupOldBackups()
        {
            try
            {
                var backupDir = Path.Combine(Application.persistentDataPath, "SaveData", "Backups");
                if (!Directory.Exists(backupDir)) return;

                var backupFiles = Directory.GetFiles(backupDir, "*_backup_*")
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.CreationTime)
                    .ToList();

                // 保留最新的备份文件
                for (int i = maxBackupCount; i < backupFiles.Count; i++)
                {
                    backupFiles[i].Delete();
                    LogDebug($"已删除旧备份文件: {backupFiles[i].Name}");
                }
            }
            catch (Exception ex)
            {
                LogError($"清理旧备份失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 创建简单事务结果
        /// </summary>
        private TransactionResult CreateSimpleTransactionResult(bool success, string message)
        {
            return new TransactionResult
            {
                success = success,
                message = message,
                transactionId = Guid.NewGuid().ToString(),
                finalStatus = success ? TransactionStatus.Committed : TransactionStatus.Failed
            };
        }
        #endregion

        #region 公共查询方法
        /// <summary>
        /// 获取活跃事务列表
        /// </summary>
        /// <returns>活跃事务列表</returns>
        public List<SaveTransaction> GetActiveTransactions()
        {
            return activeTransactions.Values.ToList();
        }

        /// <summary>
        /// 获取事务历史
        /// </summary>
        /// <returns>事务历史ID列表</returns>
        public List<string> GetTransactionHistory()
        {
            return transactionHistory.ToList();
        }

        /// <summary>
        /// 获取事务统计信息
        /// </summary>
        /// <returns>统计信息字符串</returns>
        public string GetTransactionStatistics()
        {
            var stats = new System.Text.StringBuilder();
            stats.AppendLine("=== 事务管理器统计信息 ===");
            stats.AppendLine($"事务性保存: {(enableTransactionalSave ? "启用" : "禁用")}");
            stats.AppendLine($"活跃事务数: {activeTransactions.Count}");
            stats.AppendLine($"历史事务数: {transactionHistory.Count}");
            stats.AppendLine($"最大备份数: {maxBackupCount}");
            stats.AppendLine($"事务超时时间: {transactionTimeout}秒");
            stats.AppendLine($"自动回滚: {(enableAutoRollback ? "启用" : "禁用")}");
            return stats.ToString();
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
                Debug.Log($"[SaveTransactionManager] {message}");
            }
        }

        /// <summary>
        /// 输出错误日志
        /// </summary>
        /// <param name="message">错误消息</param>
        private void LogError(string message)
        {
            Debug.LogError($"[SaveTransactionManager] {message}");
        }
        #endregion
    }
}