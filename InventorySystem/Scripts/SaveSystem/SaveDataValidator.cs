using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;

namespace InventorySystem.SaveSystem
{
    /// <summary>
    /// 保存数据验证器
    /// 负责验证保存数据的完整性、一致性和有效性
    /// 采用非侵入式设计，不修改现有类结构
    /// </summary>
    public class SaveDataValidator : MonoBehaviour
    {
        #region 单例模式
        private static SaveDataValidator _instance;
        public static SaveDataValidator Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<SaveDataValidator>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("SaveDataValidator");
                        _instance = go.AddComponent<SaveDataValidator>();

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
        [Header("验证配置")]
        [SerializeField]
        [Tooltip("是否启用严格验证模式")]
        private bool enableStrictValidation = true;

        [SerializeField]
        [Tooltip("是否启用引用完整性检查")]
        private bool enableReferenceIntegrityCheck = true;

        [SerializeField]
        [Tooltip("是否启用ID唯一性检查")]
        private bool enableIDUniquenessCheck = true;

        [SerializeField]
        [Tooltip("是否启用数据类型验证")]
        private bool enableDataTypeValidation = true;

        [SerializeField]
        [Tooltip("是否启用调试日志")]
        private bool enableDebugLog = true;
        #endregion

        #region 验证结果数据结构
        /// <summary>
        /// 验证结果
        /// </summary>
        [System.Serializable]
        public class ValidationResult
        {
            public bool isValid;
            public List<ValidationError> errors;
            public List<ValidationWarning> warnings;
            public float validationTime;
            public int objectsValidated;

            public ValidationResult()
            {
                errors = new List<ValidationError>();
                warnings = new List<ValidationWarning>();
            }
        }

        /// <summary>
        /// 验证错误
        /// </summary>
        [System.Serializable]
        public class ValidationError
        {
            public string objectId;
            public string errorType;
            public string message;
            public string suggestedFix;
            public ValidationSeverity severity;

            public ValidationError(string objectId, string errorType, string message, string suggestedFix = "", ValidationSeverity severity = ValidationSeverity.Error)
            {
                this.objectId = objectId;
                this.errorType = errorType;
                this.message = message;
                this.suggestedFix = suggestedFix;
                this.severity = severity;
            }
        }

        /// <summary>
        /// 验证警告
        /// </summary>
        [System.Serializable]
        public class ValidationWarning
        {
            public string objectId;
            public string warningType;
            public string message;
            public string recommendation;

            public ValidationWarning(string objectId, string warningType, string message, string recommendation = "")
            {
                this.objectId = objectId;
                this.warningType = warningType;
                this.message = message;
                this.recommendation = recommendation;
            }
        }

        /// <summary>
        /// 验证严重程度
        /// </summary>
        public enum ValidationSeverity
        {
            Info,
            Warning,
            Error,
            Critical
        }
        #endregion

        #region 事件定义
        // 验证事件
        public event Action<ValidationResult> OnValidationComplete;
        public event Action<string, float> OnValidationProgress;
        public event Action<ValidationError> OnValidationError;
        public event Action<ValidationWarning> OnValidationWarning;
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
            LogDebug("SaveDataValidator已初始化");
        }
        #endregion

        #region 主要验证方法
        /// <summary>
        /// 验证保存前数据
        /// </summary>
        /// <param name="saveableObjects">要保存的对象字典</param>
        /// <returns>验证结果</returns>
        public ValidationResult ValidateBeforeSave(Dictionary<string, ISaveable> saveableObjects)
        {
            var startTime = Time.realtimeSinceStartup;
            var result = new ValidationResult();

            LogDebug($"开始保存前验证，对象数量: {saveableObjects.Count}");

            try
            {
                // 1. 基础数据验证
                ValidateBasicData(saveableObjects, result);

                // 2. ID唯一性检查
                if (enableIDUniquenessCheck)
                {
                    ValidateIDUniqueness(saveableObjects, result);
                }

                // 3. 引用完整性检查
                if (enableReferenceIntegrityCheck)
                {
                    ValidateReferenceIntegrity(saveableObjects, result);
                }

                // 4. 数据类型验证
                if (enableDataTypeValidation)
                {
                    ValidateDataTypes(saveableObjects, result);
                }

                // 5. 业务逻辑验证
                ValidateBusinessLogic(saveableObjects, result);

                result.isValid = result.errors.Count == 0;
                result.objectsValidated = saveableObjects.Count;
                result.validationTime = Time.realtimeSinceStartup - startTime;

                LogDebug($"保存前验证完成，结果: {(result.isValid ? "通过" : "失败")}, 错误数: {result.errors.Count}, 警告数: {result.warnings.Count}");
            }
            catch (Exception ex)
            {
                result.errors.Add(new ValidationError("", "ValidationException", $"验证过程中发生异常: {ex.Message}", "检查验证器配置和数据格式", ValidationSeverity.Critical));
                result.isValid = false;
                LogError($"验证过程中发生异常: {ex.Message}");
            }

            OnValidationComplete?.Invoke(result);
            return result;
        }

        /// <summary>
        /// 验证加载后数据
        /// </summary>
        /// <param name="loadedObjects">已加载的对象字典</param>
        /// <returns>验证结果</returns>
        public ValidationResult ValidateAfterLoad(Dictionary<string, ISaveable> loadedObjects)
        {
            var startTime = Time.realtimeSinceStartup;
            var result = new ValidationResult();

            LogDebug($"开始加载后验证，对象数量: {loadedObjects.Count}");

            try
            {
                // 1. 数据完整性验证
                ValidateDataIntegrity(loadedObjects, result);

                // 2. 引用有效性验证
                ValidateReferenceValidity(loadedObjects, result);

                // 3. 状态一致性验证
                ValidateStateConsistency(loadedObjects, result);

                // 4. 版本兼容性验证
                ValidateVersionCompatibility(loadedObjects, result);

                result.isValid = result.errors.Count == 0;
                result.objectsValidated = loadedObjects.Count;
                result.validationTime = Time.realtimeSinceStartup - startTime;

                LogDebug($"加载后验证完成，结果: {(result.isValid ? "通过" : "失败")}, 错误数: {result.errors.Count}, 警告数: {result.warnings.Count}");
            }
            catch (Exception ex)
            {
                result.errors.Add(new ValidationError("", "ValidationException", $"验证过程中发生异常: {ex.Message}", "检查加载的数据格式和完整性", ValidationSeverity.Critical));
                result.isValid = false;
                LogError($"验证过程中发生异常: {ex.Message}");
            }

            OnValidationComplete?.Invoke(result);
            return result;
        }
        #endregion

        #region 具体验证实现
        /// <summary>
        /// 验证基础数据
        /// </summary>
        private void ValidateBasicData(Dictionary<string, ISaveable> objects, ValidationResult result)
        {
            foreach (var kvp in objects)
            {
                var objectId = kvp.Key;
                var saveable = kvp.Value;

                // 检查SaveID是否有效
                if (string.IsNullOrEmpty(objectId))
                {
                    result.errors.Add(new ValidationError(objectId, "InvalidSaveID", "保存ID为空或无效", "重新生成保存ID"));
                    continue;
                }

                // 检查对象是否为null
                if (saveable == null)
                {
                    result.errors.Add(new ValidationError(objectId, "NullObject", "保存对象为null", "移除无效的对象引用"));
                    continue;
                }

                // 检查保存数据是否可获取
                try
                {
                    var saveData = saveable.SerializeToJson();
                    if (saveData == null)
                    {
                        result.warnings.Add(new ValidationWarning(objectId, "NullSaveData", "对象返回的保存数据为null", "检查对象的SerializeToJson实现"));
                    }
                }
                catch (Exception ex)
                {
                    result.errors.Add(new ValidationError(objectId, "SaveDataException", $"获取保存数据时发生异常: {ex.Message}", "检查对象的SerializeToJson方法实现"));
                }
            }
        }

        /// <summary>
        /// 验证ID唯一性
        /// </summary>
        private void ValidateIDUniqueness(Dictionary<string, ISaveable> objects, ValidationResult result)
        {
            var idCounts = new Dictionary<string, int>();
            var duplicateIds = new HashSet<string>();

            foreach (var objectId in objects.Keys)
            {
                if (idCounts.ContainsKey(objectId))
                {
                    idCounts[objectId]++;
                    duplicateIds.Add(objectId);
                }
                else
                {
                    idCounts[objectId] = 1;
                }
            }

            foreach (var duplicateId in duplicateIds)
            {
                result.errors.Add(new ValidationError(duplicateId, "DuplicateID", $"发现重复的保存ID: {duplicateId}, 出现次数: {idCounts[duplicateId]}", "为重复对象重新生成唯一ID"));
            }
        }

        /// <summary>
        /// 验证引用完整性
        /// </summary>
        private void ValidateReferenceIntegrity(Dictionary<string, ISaveable> objects, ValidationResult result)
        {
            // 检查物品与网格的引用关系
            var itemDataHolders = objects.Values.OfType<ItemDataHolder>().ToList();
            var itemGrids = objects.Values.OfType<BaseItemGrid>().ToList();

            foreach (var itemHolder in itemDataHolders)
            {
                // 检查物品是否在某个网格中
                bool foundInGrid = false;
                foreach (var grid in itemGrids)
                {
                    if (grid.ContainsItem(itemHolder.gameObject))
                    {
                        foundInGrid = true;
                        break;
                    }
                }

                if (!foundInGrid)
                {
                    result.warnings.Add(new ValidationWarning(itemHolder.GetSaveID(), "OrphanedItem", "物品未在任何网格中找到", "检查物品的位置和网格关联"));
                }
            }
        }

        /// <summary>
        /// 验证数据类型
        /// </summary>
        private void ValidateDataTypes(Dictionary<string, ISaveable> objects, ValidationResult result)
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
                        // 尝试序列化数据以验证类型兼容性
                        var serialized = JsonConvert.SerializeObject(saveData);
                        if (string.IsNullOrEmpty(serialized))
                        {
                            result.warnings.Add(new ValidationWarning(objectId, "SerializationIssue", "对象数据序列化结果为空", "检查数据结构和序列化设置"));
                        }
                    }
                }
                catch (Exception ex)
                {
                    result.errors.Add(new ValidationError(objectId, "SerializationError", $"数据序列化失败: {ex.Message}", "检查数据类型和序列化兼容性"));
                }
            }
        }

        /// <summary>
        /// 验证业务逻辑
        /// </summary>
        private void ValidateBusinessLogic(Dictionary<string, ISaveable> objects, ValidationResult result)
        {
            // 验证物品数据的业务逻辑
            var itemDataHolders = objects.Values.OfType<ItemDataHolder>().ToList();
            foreach (var itemHolder in itemDataHolders)
            {
                var itemData = itemHolder.GetItemData();
                var instanceData = itemHolder.GetInstanceData();

                if (itemData != null && instanceData != null)
                {
                    // 检查耐久度是否合理
                    if (instanceData.currentDurability > itemData.durability)
                    {
                        result.warnings.Add(new ValidationWarning(itemHolder.GetSaveID(), "InvalidDurability", "当前耐久度超过最大耐久度", "重置耐久度到合理范围"));
                    }

                    // 检查堆叠数量是否合理
                    if (instanceData.currentStackCount > itemData.maxStack)
                    {
                        result.warnings.Add(new ValidationWarning(itemHolder.GetSaveID(), "InvalidStackCount", "当前堆叠数量超过最大堆叠数量", "调整堆叠数量到合理范围"));
                    }
                }
            }
        }

        /// <summary>
        /// 验证数据完整性
        /// </summary>
        private void ValidateDataIntegrity(Dictionary<string, ISaveable> objects, ValidationResult result)
        {
            foreach (var kvp in objects)
            {
                var objectId = kvp.Key;
                var saveable = kvp.Value;

                // 检查对象是否正确加载
                if (saveable == null)
                {
                    result.errors.Add(new ValidationError(objectId, "LoadFailure", "对象加载失败", "检查保存数据格式和加载逻辑"));
                    continue;
                }

                // 检查关键组件是否存在
                if (saveable is MonoBehaviour mb && mb == null)
                {
                    result.errors.Add(new ValidationError(objectId, "MissingComponent", "MonoBehaviour组件丢失", "重新创建或修复组件引用"));
                }
            }
        }

        /// <summary>
        /// 验证引用有效性
        /// </summary>
        private void ValidateReferenceValidity(Dictionary<string, ISaveable> objects, ValidationResult result)
        {
            // 验证加载后的引用是否仍然有效
            foreach (var kvp in objects)
            {
                var objectId = kvp.Key;
                var saveable = kvp.Value;

                if (saveable is ItemDataHolder itemHolder)
                {
                    // 检查物品数据引用
                    if (itemHolder.GetItemData() == null)
                    {
                        result.errors.Add(new ValidationError(objectId, "MissingItemData", "物品数据引用丢失", "重新设置物品数据引用"));
                    }
                }
            }
        }

        /// <summary>
        /// 验证状态一致性
        /// </summary>
        private void ValidateStateConsistency(Dictionary<string, ISaveable> objects, ValidationResult result)
        {
            // 验证对象状态的一致性
            var itemGrids = objects.Values.OfType<BaseItemGrid>().ToList();
            foreach (var grid in itemGrids)
            {
                // 检查网格状态一致性
                var gridItems = grid.GetPlacedItems().ConvertAll(item => item.itemObject);
                foreach (var item in gridItems)
                {
                    if (item == null)
                    {
                        result.warnings.Add(new ValidationWarning(grid.GetSaveID(), "NullItemInGrid", "网格中存在null物品引用", "清理无效的物品引用"));
                    }
                }
            }
        }

        /// <summary>
        /// 验证版本兼容性
        /// </summary>
        private void ValidateVersionCompatibility(Dictionary<string, ISaveable> objects, ValidationResult result)
        {
            // 检查保存数据版本兼容性
            // 这里可以根据具体需求实现版本检查逻辑
            LogDebug("版本兼容性验证完成");
        }
        #endregion

        #region 公共工具方法
        /// <summary>
        /// 获取验证统计信息
        /// </summary>
        /// <returns>验证统计信息字符串</returns>
        public string GetValidationStatistics()
        {
            var stats = new System.Text.StringBuilder();
            stats.AppendLine("=== 验证器统计信息 ===");
            stats.AppendLine($"严格验证模式: {(enableStrictValidation ? "启用" : "禁用")}");
            stats.AppendLine($"引用完整性检查: {(enableReferenceIntegrityCheck ? "启用" : "禁用")}");
            stats.AppendLine($"ID唯一性检查: {(enableIDUniquenessCheck ? "启用" : "禁用")}");
            stats.AppendLine($"数据类型验证: {(enableDataTypeValidation ? "启用" : "禁用")}");
            return stats.ToString();
        }

        /// <summary>
        /// 重置验证器配置
        /// </summary>
        public void ResetValidatorConfiguration()
        {
            enableStrictValidation = true;
            enableReferenceIntegrityCheck = true;
            enableIDUniquenessCheck = true;
            enableDataTypeValidation = true;
            LogDebug("验证器配置已重置为默认值");
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
                Debug.Log($"[SaveDataValidator] {message}");
            }
        }

        /// <summary>
        /// 输出错误日志
        /// </summary>
        /// <param name="message">错误消息</param>
        private void LogError(string message)
        {
            Debug.LogError($"[SaveDataValidator] {message}");
        }
        #endregion
    }
}