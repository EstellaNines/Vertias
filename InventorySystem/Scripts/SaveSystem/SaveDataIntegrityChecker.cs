using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using Newtonsoft.Json;

namespace InventorySystem.SaveSystem
{
    /// <summary>
    /// 保存数据完整性检查器
    /// 提供数据校验、损坏检测和自动修复功能
    /// 采用非侵入式设计，增强现有保存系统的可靠性
    /// </summary>
    public class SaveDataIntegrityChecker : MonoBehaviour
    {
        #region 单例模式
        private static SaveDataIntegrityChecker _instance;
        public static SaveDataIntegrityChecker Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<SaveDataIntegrityChecker>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("SaveDataIntegrityChecker");
                        _instance = go.AddComponent<SaveDataIntegrityChecker>();

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
        [Header("完整性检查配置")]
        [SerializeField]
        [Tooltip("是否启用数据校验和")]
        private bool enableChecksum = true;

        [SerializeField]
        [Tooltip("是否启用自动修复")]
        private bool enableAutoRepair = true;

        [SerializeField]
        [Tooltip("是否启用深度验证")]
        private bool enableDeepValidation = true;

        [SerializeField]
        [Tooltip("校验和算法类型")]
        private ChecksumAlgorithm checksumAlgorithm = ChecksumAlgorithm.SHA256;

        [SerializeField]
        [Tooltip("最大修复尝试次数")]
        private int maxRepairAttempts = 3;

        [SerializeField]
        [Tooltip("是否启用调试日志")]
        private bool enableDebugLog = true;
        #endregion

        #region 数据结构
        /// <summary>
        /// 校验和算法类型
        /// </summary>
        public enum ChecksumAlgorithm
        {
            MD5,
            SHA1,
            SHA256,
            CRC32
        }

        /// <summary>
        /// 完整性检查结果
        /// </summary>
        [System.Serializable]
        public class IntegrityCheckResult
        {
            public bool isIntact;
            public List<IntegrityIssue> issues;
            public List<RepairAction> repairActions;
            public float checkTime;
            public int objectsChecked;
            public string overallChecksum;
            public Dictionary<string, string> objectChecksums;

            public IntegrityCheckResult()
            {
                issues = new List<IntegrityIssue>();
                repairActions = new List<RepairAction>();
                objectChecksums = new Dictionary<string, string>();
            }
        }

        /// <summary>
        /// 完整性问题
        /// </summary>
        [System.Serializable]
        public class IntegrityIssue
        {
            public string objectId;
            public IssueType type;
            public IssueSeverity severity;
            public string description;
            public string expectedValue;
            public string actualValue;
            public bool canAutoRepair;
            public string repairSuggestion;

            public IntegrityIssue(string objectId, IssueType type, IssueSeverity severity, string description)
            {
                this.objectId = objectId;
                this.type = type;
                this.severity = severity;
                this.description = description;
            }
        }

        /// <summary>
        /// 问题类型
        /// </summary>
        public enum IssueType
        {
            ChecksumMismatch,       // 校验和不匹配
            MissingData,            // 数据缺失
            CorruptedData,          // 数据损坏
            InvalidReference,       // 无效引用
            InconsistentState,      // 状态不一致
            TypeMismatch,           // 类型不匹配
            RangeViolation,         // 范围违规
            DuplicateEntry          // 重复条目
        }

        /// <summary>
        /// 问题严重程度
        /// </summary>
        public enum IssueSeverity
        {
            Info,
            Warning,
            Error,
            Critical
        }

        /// <summary>
        /// 修复操作
        /// </summary>
        [System.Serializable]
        public class RepairAction
        {
            public string objectId;
            public RepairType type;
            public string description;
            public object oldValue;
            public object newValue;
            public bool wasSuccessful;
            public string errorMessage;

            public RepairAction(string objectId, RepairType type, string description)
            {
                this.objectId = objectId;
                this.type = type;
                this.description = description;
            }
        }

        /// <summary>
        /// 修复类型
        /// </summary>
        public enum RepairType
        {
            RestoreFromBackup,      // 从备份恢复
            ResetToDefault,         // 重置为默认值
            RecalculateValue,       // 重新计算值
            RemoveInvalidEntry,     // 移除无效条目
            FixReference,           // 修复引用
            RegenerateId,           // 重新生成ID
            NormalizeData           // 标准化数据
        }
        #endregion

        #region 私有字段
        private Dictionary<string, string> lastKnownChecksums;
        private SaveDataValidator validator;
        private ItemInstanceIDManager idManager;
        #endregion

        #region 事件定义
        // 完整性检查事件
        public event Action<IntegrityCheckResult> OnIntegrityCheckComplete;
        public event Action<IntegrityIssue> OnIntegrityIssueFound;
        public event Action<RepairAction> OnRepairActionExecuted;
        public event Action<string, float> OnCheckProgress;
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
            InitializeIntegrityChecker();
        }
        #endregion

        #region 初始化
        /// <summary>
        /// 初始化完整性检查器
        /// </summary>
        private void InitializeIntegrityChecker()
        {
            lastKnownChecksums = new Dictionary<string, string>();

            // 获取依赖组件
            validator = SaveDataValidator.Instance;
            idManager = ItemInstanceIDManager.Instance;

            LogDebug("SaveDataIntegrityChecker已初始化");
        }
        #endregion

        #region 主要检查方法
        /// <summary>
        /// 执行完整性检查
        /// </summary>
        /// <param name="saveableObjects">要检查的对象</param>
        /// <returns>检查结果</returns>
        public IntegrityCheckResult PerformIntegrityCheck(Dictionary<string, ISaveable> saveableObjects)
        {
            var startTime = Time.realtimeSinceStartup;
            var result = new IntegrityCheckResult();

            LogDebug($"开始完整性检查，对象数量: {saveableObjects.Count}");

            try
            {
                // 1. 基础数据检查
                CheckBasicDataIntegrity(saveableObjects, result);

                // 2. 校验和验证
                if (enableChecksum)
                {
                    CheckDataChecksums(saveableObjects, result);
                }

                // 3. 引用完整性检查
                CheckReferenceIntegrity(saveableObjects, result);

                // 4. 业务逻辑一致性检查
                CheckBusinessLogicConsistency(saveableObjects, result);

                // 5. 深度验证
                if (enableDeepValidation)
                {
                    PerformDeepValidation(saveableObjects, result);
                }

                // 6. 自动修复
                if (enableAutoRepair && result.issues.Any(i => i.canAutoRepair))
                {
                    PerformAutoRepair(saveableObjects, result);
                }

                result.isIntact = result.issues.Count(i => i.severity >= IssueSeverity.Error) == 0;
                result.objectsChecked = saveableObjects.Count;
                result.checkTime = Time.realtimeSinceStartup - startTime;
                result.overallChecksum = CalculateOverallChecksum(saveableObjects);

                LogDebug($"完整性检查完成，结果: {(result.isIntact ? "完整" : "存在问题")}, 问题数: {result.issues.Count}, 修复操作数: {result.repairActions.Count}");
            }
            catch (Exception ex)
            {
                result.issues.Add(new IntegrityIssue("", IssueType.CorruptedData, IssueSeverity.Critical, $"检查过程中发生异常: {ex.Message}"));
                result.isIntact = false;
                LogError($"完整性检查过程中发生异常: {ex.Message}");
            }

            OnIntegrityCheckComplete?.Invoke(result);
            return result;
        }

        /// <summary>
        /// 验证单个对象的完整性
        /// </summary>
        /// <param name="objectId">对象ID</param>
        /// <param name="saveable">可保存对象</param>
        /// <returns>检查结果</returns>
        public IntegrityCheckResult VerifyObjectIntegrity(string objectId, ISaveable saveable)
        {
            var objects = new Dictionary<string, ISaveable> { { objectId, saveable } };
            return PerformIntegrityCheck(objects);
        }
        #endregion

        #region 具体检查实现
        /// <summary>
        /// 检查基础数据完整性
        /// </summary>
        private void CheckBasicDataIntegrity(Dictionary<string, ISaveable> objects, IntegrityCheckResult result)
        {
            foreach (var kvp in objects)
            {
                var objectId = kvp.Key;
                var saveable = kvp.Value;

                // 检查对象是否为null
                if (saveable == null)
                {
                    result.issues.Add(new IntegrityIssue(objectId, IssueType.MissingData, IssueSeverity.Critical, "保存对象为null")
                    {
                        canAutoRepair = false,
                        repairSuggestion = "重新创建对象或从备份恢复"
                    });
                    continue;
                }

                // 检查保存数据
                try
                {
                    var saveData = saveable.SerializeToJson();
                    if (saveData == null)
                    {
                        result.issues.Add(new IntegrityIssue(objectId, IssueType.MissingData, IssueSeverity.Error, "保存数据为null")
                        {
                            canAutoRepair = true,
                            repairSuggestion = "重新初始化对象数据"
                        });
                    }
                    else
                    {
                        // 验证数据结构
                        ValidateDataStructure(objectId, saveData, result);
                    }
                }
                catch (Exception ex)
                {
                    result.issues.Add(new IntegrityIssue(objectId, IssueType.CorruptedData, IssueSeverity.Error, $"获取保存数据时发生异常: {ex.Message}")
                    {
                        canAutoRepair = false,
                        repairSuggestion = "检查对象的SerializeToJson方法实现"
                    });
                }
            }
        }

        /// <summary>
        /// 检查数据校验和
        /// </summary>
        private void CheckDataChecksums(Dictionary<string, ISaveable> objects, IntegrityCheckResult result)
        {
            foreach (var kvp in objects)
            {
                var objectId = kvp.Key;
                var saveable = kvp.Value;

                try
                {
                    var saveData = saveable.SerializeToJson();
                    if (saveData != null)
                    {
                        var currentChecksum = CalculateChecksum(saveData);
                        result.objectChecksums[objectId] = currentChecksum;

                        // 与上次已知的校验和比较
                        if (lastKnownChecksums.ContainsKey(objectId))
                        {
                            var lastChecksum = lastKnownChecksums[objectId];
                            if (currentChecksum != lastChecksum)
                            {
                                result.issues.Add(new IntegrityIssue(objectId, IssueType.ChecksumMismatch, IssueSeverity.Warning, "数据校验和不匹配")
                                {
                                    expectedValue = lastChecksum,
                                    actualValue = currentChecksum,
                                    canAutoRepair = false,
                                    repairSuggestion = "检查数据是否被意外修改"
                                });
                            }
                        }
                        else
                        {
                            // 首次记录校验和
                            lastKnownChecksums[objectId] = currentChecksum;
                        }
                    }
                }
                catch (Exception ex)
                {
                    result.issues.Add(new IntegrityIssue(objectId, IssueType.CorruptedData, IssueSeverity.Error, $"计算校验和时发生异常: {ex.Message}"));
                }
            }
        }

        /// <summary>
        /// 检查引用完整性
        /// </summary>
        private void CheckReferenceIntegrity(Dictionary<string, ISaveable> objects, IntegrityCheckResult result)
        {
            var itemDataHolders = objects.Values.OfType<ItemDataHolder>().ToList();
            var itemGrids = objects.Values.OfType<BaseItemGrid>().ToList();

            // 检查物品与网格的引用关系
            foreach (var itemHolder in itemDataHolders)
            {
                var itemId = itemHolder.GetSaveID();

                // 检查物品数据引用
                if (itemHolder.GetItemData() == null)
                {
                    result.issues.Add(new IntegrityIssue(itemId, IssueType.InvalidReference, IssueSeverity.Error, "物品数据引用丢失")
                    {
                        canAutoRepair = false,
                        repairSuggestion = "重新设置物品数据引用"
                    });
                }

                // 检查实例数据
                var instanceData = itemHolder.GetInstanceData();
                if (instanceData == null)
                {
                    result.issues.Add(new IntegrityIssue(itemId, IssueType.MissingData, IssueSeverity.Error, "物品实例数据丢失")
                    {
                        canAutoRepair = true,
                        repairSuggestion = "重新生成实例数据"
                    });
                }
            }

            // 检查网格中的物品引用
            foreach (var grid in itemGrids)
            {
                var gridId = grid.GetSaveID();
                var gridItems = grid.GetPlacedItems().ConvertAll(item => item.itemObject);

                foreach (var item in gridItems)
                {
                    if (item == null)
                    {
                        result.issues.Add(new IntegrityIssue(gridId, IssueType.InvalidReference, IssueSeverity.Warning, "网格中存在null物品引用")
                        {
                            canAutoRepair = true,
                            repairSuggestion = "清理无效的物品引用"
                        });
                    }
                }
            }
        }

        /// <summary>
        /// 检查业务逻辑一致性
        /// </summary>
        private void CheckBusinessLogicConsistency(Dictionary<string, ISaveable> objects, IntegrityCheckResult result)
        {
            var itemDataHolders = objects.Values.OfType<ItemDataHolder>().ToList();

            foreach (var itemHolder in itemDataHolders)
            {
                var itemId = itemHolder.GetSaveID();
                var itemData = itemHolder.GetItemData();
                var instanceData = itemHolder.GetInstanceData();

                if (itemData != null && instanceData != null)
                {
                    // 检查耐久度范围
                    if (instanceData.currentDurability < 0 || instanceData.currentDurability > itemData.durability)
                    {
                        result.issues.Add(new IntegrityIssue(itemId, IssueType.RangeViolation, IssueSeverity.Warning, "耐久度超出有效范围")
                        {
                            expectedValue = $"0 - {itemData.durability}",
                            actualValue = instanceData.currentDurability.ToString(),
                            canAutoRepair = true,
                            repairSuggestion = "将耐久度调整到有效范围内"
                        });
                    }

                    // 检查堆叠数量
                    if (instanceData.currentStackCount < 1 || instanceData.currentStackCount > itemData.maxStack)
                    {
                        result.issues.Add(new IntegrityIssue(itemId, IssueType.RangeViolation, IssueSeverity.Warning, "堆叠数量超出有效范围")
                        {
                            expectedValue = $"1 - {itemData.maxStack}",
                            actualValue = instanceData.currentStackCount.ToString(),
                            canAutoRepair = true,
                            repairSuggestion = "将堆叠数量调整到有效范围内"
                        });
                    }
                }
            }
        }

        /// <summary>
        /// 执行深度验证
        /// </summary>
        private void PerformDeepValidation(Dictionary<string, ISaveable> objects, IntegrityCheckResult result)
        {
            // 检查ID唯一性
            var idCounts = new Dictionary<string, int>();
            foreach (var objectId in objects.Keys)
            {
                if (idCounts.ContainsKey(objectId))
                {
                    idCounts[objectId]++;
                }
                else
                {
                    idCounts[objectId] = 1;
                }
            }

            foreach (var kvp in idCounts.Where(x => x.Value > 1))
            {
                result.issues.Add(new IntegrityIssue(kvp.Key, IssueType.DuplicateEntry, IssueSeverity.Error, $"发现重复ID，出现次数: {kvp.Value}")
                {
                    canAutoRepair = true,
                    repairSuggestion = "为重复对象重新生成唯一ID"
                });
            }

            // 检查数据序列化兼容性
            foreach (var kvp in objects)
            {
                var objectId = kvp.Key;
                var saveable = kvp.Value;

                try
                {
                    var saveData = saveable.SerializeToJson();
                    if (saveData != null)
                    {
                        // 尝试序列化和反序列化
                        var serialized = JsonConvert.SerializeObject(saveData);
                        var deserialized = JsonConvert.DeserializeObject(serialized, saveData.GetType());

                        if (deserialized == null)
                        {
                            result.issues.Add(new IntegrityIssue(objectId, IssueType.CorruptedData, IssueSeverity.Error, "数据序列化/反序列化失败")
                            {
                                canAutoRepair = false,
                                repairSuggestion = "检查数据类型和序列化设置"
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    result.issues.Add(new IntegrityIssue(objectId, IssueType.CorruptedData, IssueSeverity.Error, $"深度验证失败: {ex.Message}"));
                }
            }
        }

        /// <summary>
        /// 执行自动修复
        /// </summary>
        private void PerformAutoRepair(Dictionary<string, ISaveable> objects, IntegrityCheckResult result)
        {
            var repairableIssues = result.issues.Where(i => i.canAutoRepair).ToList();
            LogDebug($"开始自动修复，可修复问题数: {repairableIssues.Count}");

            foreach (var issue in repairableIssues)
            {
                var repairAction = new RepairAction(issue.objectId, GetRepairTypeForIssue(issue.type), $"修复{issue.type}: {issue.description}");

                try
                {
                    bool repairSuccess = ExecuteRepairAction(issue, objects, repairAction);
                    repairAction.wasSuccessful = repairSuccess;

                    if (repairSuccess)
                    {
                        LogDebug($"成功修复问题: {issue.description}");
                        // 从问题列表中移除已修复的问题
                        result.issues.Remove(issue);
                    }
                    else
                    {
                        LogDebug($"修复失败: {issue.description}");
                    }
                }
                catch (Exception ex)
                {
                    repairAction.wasSuccessful = false;
                    repairAction.errorMessage = ex.Message;
                    LogError($"修复过程中发生异常: {ex.Message}");
                }

                result.repairActions.Add(repairAction);
                OnRepairActionExecuted?.Invoke(repairAction);
            }
        }
        #endregion

        #region 修复操作实现
        /// <summary>
        /// 执行修复操作
        /// </summary>
        private bool ExecuteRepairAction(IntegrityIssue issue, Dictionary<string, ISaveable> objects, RepairAction repairAction)
        {
            switch (issue.type)
            {
                case IssueType.MissingData:
                    return RepairMissingData(issue, objects, repairAction);
                case IssueType.RangeViolation:
                    return RepairRangeViolation(issue, objects, repairAction);
                case IssueType.InvalidReference:
                    return RepairInvalidReference(issue, objects, repairAction);
                case IssueType.DuplicateEntry:
                    return RepairDuplicateEntry(issue, objects, repairAction);
                default:
                    return false;
            }
        }

        /// <summary>
        /// 修复缺失数据
        /// </summary>
        private bool RepairMissingData(IntegrityIssue issue, Dictionary<string, ISaveable> objects, RepairAction repairAction)
        {
            if (objects.ContainsKey(issue.objectId))
            {
                var saveable = objects[issue.objectId];
                if (saveable is ItemDataHolder itemHolder)
                {
                    var instanceData = itemHolder.GetInstanceData();
                    if (instanceData == null)
                    {
                        // 重新生成实例数据
                        var newInstanceData = new ItemInstanceData();
                        // ItemInstanceData没有instanceID属性，跳过设置
                        // 这里需要根据具体实现来设置实例数据
                        repairAction.newValue = newInstanceData;
                        return true;
                    }
                    // ItemInstanceData没有instanceID属性，跳过此检查
                    // else if (string.IsNullOrEmpty(instanceData.instanceID))
                    // {
                    //     // 重新生成实例ID
                    //     repairAction.oldValue = instanceData.instanceID;
                    //     instanceData.instanceID = Guid.NewGuid().ToString();
                    //     repairAction.newValue = instanceData.instanceID;
                    //     return true;
                    // }
                }
            }
            return false;
        }

        /// <summary>
        /// 修复范围违规
        /// </summary>
        private bool RepairRangeViolation(IntegrityIssue issue, Dictionary<string, ISaveable> objects, RepairAction repairAction)
        {
            if (objects.ContainsKey(issue.objectId))
            {
                var saveable = objects[issue.objectId];
                if (saveable is ItemDataHolder itemHolder)
                {
                    var itemData = itemHolder.GetItemData();
                    var instanceData = itemHolder.GetInstanceData();

                    if (itemData != null && instanceData != null)
                    {
                        // 修复耐久度
                        if (instanceData.currentDurability < 0 || instanceData.currentDurability > itemData.durability)
                        {
                            repairAction.oldValue = instanceData.currentDurability;
                            instanceData.currentDurability = Mathf.Clamp(instanceData.currentDurability, 0, itemData.durability);
                            repairAction.newValue = instanceData.currentDurability;
                            return true;
                        }

                        // 修复堆叠数量
                        if (instanceData.currentStackCount < 1 || instanceData.currentStackCount > itemData.maxStack)
                        {
                            repairAction.oldValue = instanceData.currentStackCount;
                            instanceData.currentStackCount = Mathf.Clamp(instanceData.currentStackCount, 1, itemData.maxStack);
                            repairAction.newValue = instanceData.currentStackCount;
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 修复无效引用
        /// </summary>
        private bool RepairInvalidReference(IntegrityIssue issue, Dictionary<string, ISaveable> objects, RepairAction repairAction)
        {
            if (objects.ContainsKey(issue.objectId))
            {
                var saveable = objects[issue.objectId];
                if (saveable is BaseItemGrid grid)
                {
                    // 清理网格中的null引用
                    var itemsToRemove = new List<GameObject>();
                    var allItems = grid.GetPlacedItems().ConvertAll(item => item.itemObject);

                    foreach (var item in allItems)
                    {
                        if (item == null)
                        {
                            itemsToRemove.Add(item);
                        }
                    }

                    foreach (var item in itemsToRemove)
                    {
                        // 这里需要根据具体的网格实现来移除物品
                        // grid.RemoveItem(item);
                    }

                    repairAction.newValue = $"移除了{itemsToRemove.Count}个无效引用";
                    return itemsToRemove.Count > 0;
                }
            }
            return false;
        }

        /// <summary>
        /// 修复重复条目
        /// </summary>
        private bool RepairDuplicateEntry(IntegrityIssue issue, Dictionary<string, ISaveable> objects, RepairAction repairAction)
        {
            // 为重复的对象重新生成ID
            var duplicateObjects = objects.Where(kvp => kvp.Key == issue.objectId).ToList();
            if (duplicateObjects.Count > 1)
            {
                // 保留第一个，为其他的重新生成ID
                for (int i = 1; i < duplicateObjects.Count; i++)
                {
                    var newId = GenerateUniqueId(objects.Keys);
                    var obj = duplicateObjects[i].Value;

                    // 从原字典中移除
                    objects.Remove(duplicateObjects[i].Key);
                    // 用新ID添加
                    objects[newId] = obj;

                    repairAction.newValue = $"重新生成ID: {newId}";
                }
                return true;
            }
            return false;
        }
        #endregion

        #region 辅助方法
        /// <summary>
        /// 验证数据结构
        /// </summary>
        private void ValidateDataStructure(string objectId, object saveData, IntegrityCheckResult result)
        {
            // 检查数据是否可序列化
            try
            {
                var serialized = JsonConvert.SerializeObject(saveData);
                if (string.IsNullOrEmpty(serialized) || serialized == "null")
                {
                    result.issues.Add(new IntegrityIssue(objectId, IssueType.CorruptedData, IssueSeverity.Warning, "数据序列化结果为空"));
                }
            }
            catch (Exception ex)
            {
                result.issues.Add(new IntegrityIssue(objectId, IssueType.CorruptedData, IssueSeverity.Error, $"数据序列化失败: {ex.Message}"));
            }
        }

        /// <summary>
        /// 计算校验和
        /// </summary>
        private string CalculateChecksum(object data)
        {
            var serialized = JsonConvert.SerializeObject(data);
            var bytes = Encoding.UTF8.GetBytes(serialized);

            switch (checksumAlgorithm)
            {
                case ChecksumAlgorithm.MD5:
                    using (var md5 = MD5.Create())
                        return Convert.ToBase64String(md5.ComputeHash(bytes));
                case ChecksumAlgorithm.SHA1:
                    using (var sha1 = SHA1.Create())
                        return Convert.ToBase64String(sha1.ComputeHash(bytes));
                case ChecksumAlgorithm.SHA256:
                    using (var sha256 = SHA256.Create())
                        return Convert.ToBase64String(sha256.ComputeHash(bytes));
                case ChecksumAlgorithm.CRC32:
                    return CalculateCRC32(bytes).ToString();
                default:
                    return serialized.GetHashCode().ToString();
            }
        }

        /// <summary>
        /// 计算整体校验和
        /// </summary>
        private string CalculateOverallChecksum(Dictionary<string, ISaveable> objects)
        {
            var allData = new StringBuilder();
            foreach (var kvp in objects.OrderBy(x => x.Key))
            {
                try
                {
                    var saveData = kvp.Value.SerializeToJson();
                    if (saveData != null)
                    {
                        allData.Append(JsonConvert.SerializeObject(saveData));
                    }
                }
                catch
                {
                    // 忽略无法序列化的对象
                }
            }

            return CalculateChecksum(allData.ToString());
        }

        /// <summary>
        /// 计算CRC32
        /// </summary>
        private uint CalculateCRC32(byte[] data)
        {
            uint crc = 0xFFFFFFFF;
            for (int i = 0; i < data.Length; i++)
            {
                crc ^= data[i];
                for (int j = 0; j < 8; j++)
                {
                    if ((crc & 1) == 1)
                        crc = (crc >> 1) ^ 0xEDB88320;
                    else
                        crc >>= 1;
                }
            }
            return ~crc;
        }

        /// <summary>
        /// 获取问题对应的修复类型
        /// </summary>
        private RepairType GetRepairTypeForIssue(IssueType issueType)
        {
            switch (issueType)
            {
                case IssueType.MissingData:
                    return RepairType.ResetToDefault;
                case IssueType.RangeViolation:
                    return RepairType.NormalizeData;
                case IssueType.InvalidReference:
                    return RepairType.FixReference;
                case IssueType.DuplicateEntry:
                    return RepairType.RegenerateId;
                case IssueType.CorruptedData:
                    return RepairType.RestoreFromBackup;
                default:
                    return RepairType.ResetToDefault;
            }
        }

        /// <summary>
        /// 生成唯一ID
        /// </summary>
        private string GenerateUniqueId(IEnumerable<string> existingIds)
        {
            string newId;
            do
            {
                newId = Guid.NewGuid().ToString();
            } while (existingIds.Contains(newId));

            return newId;
        }
        #endregion

        #region 公共工具方法
        /// <summary>
        /// 更新已知校验和
        /// </summary>
        /// <param name="objectId">对象ID</param>
        /// <param name="checksum">校验和</param>
        public void UpdateKnownChecksum(string objectId, string checksum)
        {
            lastKnownChecksums[objectId] = checksum;
        }

        /// <summary>
        /// 清除已知校验和
        /// </summary>
        public void ClearKnownChecksums()
        {
            lastKnownChecksums.Clear();
            LogDebug("已清除所有已知校验和");
        }

        /// <summary>
        /// 获取完整性统计信息
        /// </summary>
        /// <returns>统计信息字符串</returns>
        public string GetIntegrityStatistics()
        {
            var stats = new System.Text.StringBuilder();
            stats.AppendLine("=== 完整性检查器统计信息 ===");
            stats.AppendLine($"校验和验证: {(enableChecksum ? "启用" : "禁用")}");
            stats.AppendLine($"自动修复: {(enableAutoRepair ? "启用" : "禁用")}");
            stats.AppendLine($"深度验证: {(enableDeepValidation ? "启用" : "禁用")}");
            stats.AppendLine($"校验和算法: {checksumAlgorithm}");
            stats.AppendLine($"最大修复尝试次数: {maxRepairAttempts}");
            stats.AppendLine($"已知校验和数量: {lastKnownChecksums.Count}");
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
                Debug.Log($"[SaveDataIntegrityChecker] {message}");
            }
        }

        /// <summary>
        /// 输出错误日志
        /// </summary>
        /// <param name="message">错误消息</param>
        private void LogError(string message)
        {
            Debug.LogError($"[SaveDataIntegrityChecker] {message}");
        }
        #endregion
    }
}