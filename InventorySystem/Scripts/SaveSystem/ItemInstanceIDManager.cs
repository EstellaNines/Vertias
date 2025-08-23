using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Newtonsoft.Json;

namespace InventorySystem.SaveSystem
{
    /// <summary>
    /// 物品实例ID一致性管理器
    /// 负责全局ID冲突检测、解决机制和跨场景ID同步功能
    /// 确保所有物品实例在整个游戏生命周期中保持唯一且一致的ID
    /// </summary>
    public class ItemInstanceIDManager : MonoBehaviour
    {
        #region 单例模式
        private static ItemInstanceIDManager _instance;
        public static ItemInstanceIDManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<ItemInstanceIDManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("ItemInstanceIDManager");
                        _instance = go.AddComponent<ItemInstanceIDManager>();

                        // 将新创建的ItemInstanceIDManager设置为SaveSystem的子对象（如果存在SaveSystem）
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

        #region 冲突检测和解决
        /// <summary>
        /// 启动自动检测
        /// </summary>
        private void StartAutoDetection()
        {
            if (detectionCoroutine != null)
            {
                StopCoroutine(detectionCoroutine);
            }
            detectionCoroutine = StartCoroutine(AutoDetectionCoroutine());
        }

        /// <summary>
        /// 自动检测协程
        /// </summary>
        private IEnumerator AutoDetectionCoroutine()
        {
            while (enableAutoDetection)
            {
                yield return new WaitForSeconds(detectionInterval);
                PerformConflictDetection();
            }
        }

        /// <summary>
        /// 执行冲突检测
        /// </summary>
        public void PerformConflictDetection()
        {
            LogMessage("开始执行ID冲突检测...");

            // 检测重复ID
            DetectDuplicateIDs();

            // 检测无效ID
            DetectInvalidIDs();

            // 检测孤立引用
            DetectOrphanedReferences();

            // 如果启用自动解决，尝试解决冲突
            if (enableAutoResolve)
            {
                ResolveActiveConflicts();
            }

            LogMessage($"冲突检测完成，发现 {activeConflicts.Count} 个活跃冲突");
        }

        /// <summary>
        /// 检测重复ID
        /// </summary>
        private void DetectDuplicateIDs()
        {
            // 查找所有InventorySystemItem组件
            InventorySystemItem[] allItems = FindObjectsOfType<InventorySystemItem>(true);
            Dictionary<string, List<InventorySystemItem>> idGroups = new Dictionary<string, List<InventorySystemItem>>();

            // 按ID分组
            foreach (var item in allItems)
            {
                string id = item.GetItemInstanceID();
                if (!string.IsNullOrEmpty(id))
                {
                    if (!idGroups.ContainsKey(id))
                    {
                        idGroups[id] = new List<InventorySystemItem>();
                    }
                    idGroups[id].Add(item);
                }
            }

            // 检测重复
            foreach (var group in idGroups)
            {
                if (group.Value.Count > 1)
                {
                    List<string> conflictObjects = group.Value.Select(item => item.gameObject.name).ToList();
                    DetectAndRecordConflict(group.Key, string.Join(", ", conflictObjects), ConflictType.Duplicate);
                }
            }
        }

        /// <summary>
        /// 检测无效ID
        /// </summary>
        private void DetectInvalidIDs()
        {
            InventorySystemItem[] allItems = FindObjectsOfType<InventorySystemItem>(true);

            foreach (var item in allItems)
            {
                if (!item.IsItemInstanceIDValid())
                {
                    DetectAndRecordConflict(item.GetItemInstanceID(), item.gameObject.name, ConflictType.Invalid);
                }
            }
        }

        /// <summary>
        /// 检测孤立引用
        /// </summary>
        private void DetectOrphanedReferences()
        {
            // 检查注册表中的ID是否还有对应的游戏对象
            List<string> orphanedIDs = new List<string>();

            foreach (var kvp in globalIDRegistry)
            {
                string id = kvp.Key;
                IDMappingInfo info = kvp.Value;

                // 尝试找到对应的游戏对象
                InventorySystemItem[] allItems = FindObjectsOfType<InventorySystemItem>(true);
                bool found = allItems.Any(item => item.GetItemInstanceID() == id);

                if (!found && info.isActive)
                {
                    orphanedIDs.Add(id);
                }
            }

            // 记录孤立引用
            foreach (string orphanedID in orphanedIDs)
            {
                DetectAndRecordConflict(orphanedID, "未找到对应对象", ConflictType.Orphaned);
            }
        }

        /// <summary>
        /// 检测并记录冲突
        /// </summary>
        /// <param name="conflictID">冲突ID</param>
        /// <param name="objectInfo">对象信息</param>
        /// <param name="type">冲突类型</param>
        private void DetectAndRecordConflict(string conflictID, string objectInfo, ConflictType type)
        {
            // 检查是否已存在该冲突
            if (activeConflicts.ContainsKey(conflictID))
            {
                // 更新现有冲突信息
                var existingConflict = activeConflicts[conflictID];
                if (!existingConflict.conflictObjects.Contains(objectInfo))
                {
                    existingConflict.conflictObjects.Add(objectInfo);
                }
                return;
            }

            // 创建新的冲突记录
            IDConflictInfo conflict = new IDConflictInfo
            {
                conflictID = conflictID,
                type = type,
                detectedTime = DateTime.Now,
                isResolved = false
            };
            conflict.conflictObjects.Add(objectInfo);

            // 添加到活跃冲突列表
            activeConflicts[conflictID] = conflict;

            // 添加到历史记录
            conflictHistory.Add(conflict);

            // 限制历史记录数量
            if (conflictHistory.Count > maxConflictHistory)
            {
                conflictHistory.RemoveAt(0);
            }

            LogWarning($"检测到ID冲突: {conflictID}, 类型: {type}, 对象: {objectInfo}");
            OnConflictDetected?.Invoke(conflict);
        }

        /// <summary>
        /// 解决活跃冲突
        /// </summary>
        private void ResolveActiveConflicts()
        {
            List<string> resolvedConflicts = new List<string>();

            // 创建activeConflicts的副本来避免在遍历时修改集合
            var conflictsCopy = activeConflicts.ToList();

            foreach (var kvp in conflictsCopy)
            {
                string conflictID = kvp.Key;
                IDConflictInfo conflict = kvp.Value;

                // 检查冲突是否仍然存在（可能已被其他操作解决）
                if (!activeConflicts.ContainsKey(conflictID))
                {
                    continue;
                }

                bool resolved = false;

                switch (conflict.type)
                {
                    case ConflictType.Duplicate:
                        resolved = ResolveDuplicateIDConflict(conflictID);
                        break;
                    case ConflictType.Invalid:
                        resolved = ResolveInvalidIDConflict(conflictID);
                        break;
                    case ConflictType.Orphaned:
                        resolved = ResolveOrphanedReferenceConflict(conflictID);
                        break;
                    case ConflictType.CrossScene:
                        resolved = ResolveCrossSceneConflict(conflictID);
                        break;
                }

                if (resolved)
                {
                    // 检查冲突是否仍在activeConflicts中（可能已被UnregisterInstanceID移除）
                    if (activeConflicts.ContainsKey(conflictID))
                    {
                        activeConflicts[conflictID].isResolved = true;
                        resolvedConflicts.Add(conflictID);
                    }
                }
            }

            // 移除已解决的冲突
            foreach (string resolvedID in resolvedConflicts)
            {
                activeConflicts.Remove(resolvedID);
            }
        }

        /// <summary>
        /// 解决重复ID冲突
        /// </summary>
        /// <param name="conflictID">冲突ID</param>
        /// <returns>是否解决成功</returns>
        private bool ResolveDuplicateIDConflict(string conflictID)
        {
            InventorySystemItem[] conflictItems = FindObjectsOfType<InventorySystemItem>(true)
                .Where(item => item.GetItemInstanceID() == conflictID)
                .ToArray();

            if (conflictItems.Length <= 1)
            {
                return true; // 冲突已自然解决
            }

            // 保留第一个对象的ID，为其他对象生成新ID
            for (int i = 1; i < conflictItems.Length; i++)
            {
                string newID = GenerateUniqueInstanceID("ResolvedItem");
                string oldID = conflictItems[i].GetItemInstanceID();

                conflictItems[i].SetItemInstanceID(newID);

                // 注册新ID
                RegisterInstanceID(newID, conflictItems[i].gameObject.name, "InventorySystemItem", conflictItems[i].transform.position);

                LogMessage($"解决重复ID冲突: {oldID} -> {newID}, 对象: {conflictItems[i].gameObject.name}");
                OnConflictResolved?.Invoke(oldID, newID);
            }

            return true;
        }

        /// <summary>
        /// 解决无效ID冲突
        /// </summary>
        /// <param name="conflictID">冲突ID</param>
        /// <returns>是否解决成功</returns>
        private bool ResolveInvalidIDConflict(string conflictID)
        {
            InventorySystemItem[] invalidItems = FindObjectsOfType<InventorySystemItem>(true)
                .Where(item => item.GetItemInstanceID() == conflictID && !item.IsItemInstanceIDValid())
                .ToArray();

            foreach (var item in invalidItems)
            {
                string newID = GenerateUniqueInstanceID("ValidItem");
                string oldID = item.GetItemInstanceID();

                item.SetItemInstanceID(newID);

                // 注册新ID
                RegisterInstanceID(newID, item.gameObject.name, "InventorySystemItem", item.transform.position);

                LogMessage($"解决无效ID冲突: {oldID} -> {newID}, 对象: {item.gameObject.name}");
                OnConflictResolved?.Invoke(oldID, newID);
            }

            return invalidItems.Length > 0;
        }

        /// <summary>
        /// 解决孤立引用冲突
        /// </summary>
        /// <param name="conflictID">冲突ID</param>
        /// <returns>是否解决成功</returns>
        private bool ResolveOrphanedReferenceConflict(string conflictID)
        {
            // 简单地从注册表中移除孤立的引用
            bool removed = UnregisterInstanceID(conflictID);

            if (removed)
            {
                LogMessage($"解决孤立引用冲突: 移除ID {conflictID}");
                OnConflictResolved?.Invoke(conflictID, "");
            }

            return removed;
        }

        /// <summary>
        /// 解决跨场景冲突
        /// </summary>
        /// <param name="conflictID">冲突ID</param>
        /// <returns>是否解决成功</returns>
        private bool ResolveCrossSceneConflict(string conflictID)
        {
            // 检查缓存中是否有正确的映射
            if (sceneTransitionCache.TryGetValue(conflictID, out string cachedMapping))
            {
                // 使用缓存的映射信息恢复
                LogMessage($"使用缓存解决跨场景冲突: {conflictID}");
                return true;
            }

            // 如果没有缓存，尝试重新同步
            return SynchronizeSceneIDs();
        }
        #endregion

        #region 跨场景同步
        /// <summary>
        /// 场景加载事件处理
        /// </summary>
        /// <param name="scene">加载的场景</param>
        /// <param name="mode">加载模式</param>
        private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            LogMessage($"场景已加载: {scene.name}");
            OnSceneTransitionStart?.Invoke(scene.name);

            // 延迟执行同步，确保所有对象都已初始化
            StartCoroutine(DelayedSceneSync(scene.name));
        }

        /// <summary>
        /// 场景卸载事件处理
        /// </summary>
        /// <param name="scene">卸载的场景</param>
        private void OnSceneUnloaded(UnityEngine.SceneManagement.Scene scene)
        {
            LogMessage($"场景已卸载: {scene.name}");

            // 缓存当前场景的ID映射
            CacheSceneIDs(scene.name);

            // 清理该场景的注册信息
            CleanupSceneRegistrations(scene.name);
        }

        /// <summary>
        /// 延迟场景同步
        /// </summary>
        /// <param name="sceneName">场景名称</param>
        private IEnumerator DelayedSceneSync(string sceneName)
        {
            // 等待一帧，确保所有对象都已初始化
            yield return null;

            // 执行场景同步
            SynchronizeSceneIDs();

            // 恢复缓存的ID映射
            RestoreCachedIDs(sceneName);

            OnSceneTransitionComplete?.Invoke(sceneName);
        }

        /// <summary>
        /// 同步场景ID
        /// </summary>
        /// <returns>同步是否成功</returns>
        public bool SynchronizeSceneIDs()
        {
            try
            {
                LogMessage("开始同步场景ID...");

                // 获取当前场景中的所有物品
                InventorySystemItem[] sceneItems = FindObjectsOfType<InventorySystemItem>(true);
                int syncCount = 0;

                foreach (var item in sceneItems)
                {
                    string itemID = item.GetItemInstanceID();

                    if (!string.IsNullOrEmpty(itemID))
                    {
                        // 检查ID是否已注册
                        if (!IsIDRegistered(itemID))
                        {
                            // 注册新ID
                            RegisterInstanceID(itemID, item.gameObject.name, "InventorySystemItem", item.transform.position);
                            syncCount++;
                        }
                        else
                        {
                            // 更新现有ID的信息
                            UpdateIDMappingInfo(itemID, item.transform.position, item.gameObject.activeInHierarchy);
                        }
                    }
                    else
                    {
                        // 为没有ID的物品生成新ID
                        string newID = GenerateUniqueInstanceID("SyncedItem");
                        item.SetItemInstanceID(newID);
                        RegisterInstanceID(newID, item.gameObject.name, "InventorySystemItem", item.transform.position);
                        syncCount++;
                    }
                }

                LogMessage($"场景ID同步完成，处理了 {syncCount} 个物品");
                return true;
            }
            catch (System.Exception ex)
            {
                LogError($"场景ID同步失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 缓存场景ID
        /// </summary>
        /// <param name="sceneName">场景名称</param>
        private void CacheSceneIDs(string sceneName)
        {
            // 清理旧缓存
            List<string> keysToRemove = sceneTransitionCache.Keys
                .Where(key => key.StartsWith($"{sceneName}_"))
                .ToList();

            foreach (string key in keysToRemove)
            {
                sceneTransitionCache.Remove(key);
            }

            // 缓存当前场景的ID映射
            foreach (var kvp in globalIDRegistry)
            {
                if (kvp.Value.sceneName == sceneName)
                {
                    string cacheKey = $"{sceneName}_{kvp.Key}";
                    sceneTransitionCache[cacheKey] = JsonConvert.SerializeObject(kvp.Value);
                }
            }

            LogMessage($"已缓存场景 {sceneName} 的ID映射，共 {sceneTransitionCache.Count} 项");
        }

        /// <summary>
        /// 恢复缓存的ID
        /// </summary>
        /// <param name="sceneName">场景名称</param>
        private void RestoreCachedIDs(string sceneName)
        {
            List<string> cacheKeys = sceneTransitionCache.Keys
                .Where(key => key.StartsWith($"{sceneName}_"))
                .ToList();

            int restoredCount = 0;

            foreach (string cacheKey in cacheKeys)
            {
                try
                {
                    string cachedData = sceneTransitionCache[cacheKey];
                    IDMappingInfo mappingInfo = JsonConvert.DeserializeObject<IDMappingInfo>(cachedData);

                    if (mappingInfo != null && !IsIDRegistered(mappingInfo.instanceID))
                    {
                        globalIDRegistry[mappingInfo.instanceID] = mappingInfo;
                        restoredCount++;
                    }
                }
                catch (System.Exception ex)
                {
                    LogError($"恢复缓存ID失败: {cacheKey}, 错误: {ex.Message}");
                }
            }

            LogMessage($"已恢复场景 {sceneName} 的ID映射，共 {restoredCount} 项");
        }

        /// <summary>
        /// 清理场景注册信息
        /// </summary>
        /// <param name="sceneName">场景名称</param>
        private void CleanupSceneRegistrations(string sceneName)
        {
            List<string> idsToRemove = globalIDRegistry
                .Where(kvp => kvp.Value.sceneName == sceneName)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (string id in idsToRemove)
            {
                // 标记为非激活状态，而不是直接删除
                if (globalIDRegistry.TryGetValue(id, out IDMappingInfo info))
                {
                    info.isActive = false;
                    info.lastUpdated = DateTime.Now;
                }
            }

            LogMessage($"已清理场景 {sceneName} 的注册信息，共 {idsToRemove.Count} 项");
        }
        #endregion

        #region 公共API方法
        /// <summary>
        /// 获取所有活跃冲突
        /// </summary>
        /// <returns>冲突信息列表</returns>
        public List<IDConflictInfo> GetActiveConflicts()
        {
            return new List<IDConflictInfo>(activeConflicts.Values);
        }

        /// <summary>
        /// 获取冲突历史
        /// </summary>
        /// <returns>历史冲突列表</returns>
        public List<IDConflictInfo> GetConflictHistory()
        {
            return new List<IDConflictInfo>(conflictHistory);
        }

        /// <summary>
        /// 获取所有注册的ID
        /// </summary>
        /// <returns>ID列表</returns>
        public List<string> GetAllRegisteredIDs()
        {
            return new List<string>(globalIDRegistry.Keys);
        }

        /// <summary>
        /// 获取指定场景的ID
        /// </summary>
        /// <param name="sceneName">场景名称</param>
        /// <returns>该场景的ID列表</returns>
        public List<string> GetSceneIDs(string sceneName)
        {
            return globalIDRegistry
                .Where(kvp => kvp.Value.sceneName == sceneName)
                .Select(kvp => kvp.Key)
                .ToList();
        }

        /// <summary>
        /// 强制执行完整的系统验证
        /// </summary>
        /// <returns>验证报告</returns>
        public string PerformSystemValidation()
        {
            LogMessage("开始执行系统验证...");

            var report = new System.Text.StringBuilder();
            report.AppendLine("=== 物品实例ID系统验证报告 ===");
            report.AppendLine($"验证时间: {DateTime.Now}");
            report.AppendLine();

            // 统计信息
            int totalRegistered = globalIDRegistry.Count;
            int activeRegistered = globalIDRegistry.Values.Count(info => info.isActive);
            int totalConflicts = activeConflicts.Count;
            int totalItems = FindObjectsOfType<InventorySystemItem>(true).Length;

            report.AppendLine("=== 统计信息 ===");
            report.AppendLine($"已注册ID总数: {totalRegistered}");
            report.AppendLine($"活跃ID数量: {activeRegistered}");
            report.AppendLine($"当前冲突数: {totalConflicts}");
            report.AppendLine($"场景中物品数: {totalItems}");
            report.AppendLine();

            // 执行检测
            PerformConflictDetection();

            // 冲突详情
            if (activeConflicts.Count > 0)
            {
                report.AppendLine("=== 检测到的冲突 ===");
                foreach (var conflict in activeConflicts.Values)
                {
                    report.AppendLine($"ID: {conflict.conflictID}");
                    report.AppendLine($"类型: {conflict.type}");
                    report.AppendLine($"对象: {string.Join(", ", conflict.conflictObjects)}");
                    report.AppendLine($"检测时间: {conflict.detectedTime}");
                    report.AppendLine();
                }
            }
            else
            {
                report.AppendLine("=== 未检测到冲突 ===");
            }

            string reportText = report.ToString();
            LogMessage("系统验证完成");
            return reportText;
        }

        /// <summary>
        /// 清理所有数据（谨慎使用）
        /// </summary>
        public void ClearAllData()
        {
            globalIDRegistry.Clear();
            activeConflicts.Clear();
            conflictHistory.Clear();
            sceneTransitionCache.Clear();
            idGenerationCounter = 0;

            LogMessage("已清理所有ID管理数据");
        }

        /// <summary>
        /// 导出ID映射数据
        /// </summary>
        /// <returns>JSON格式的映射数据</returns>
        public string ExportIDMappingData()
        {
            try
            {
                var exportData = new
                {
                    exportTime = DateTime.Now,
                    totalCount = globalIDRegistry.Count,
                    mappings = globalIDRegistry
                };

                return JsonConvert.SerializeObject(exportData, Formatting.Indented);
            }
            catch (System.Exception ex)
            {
                LogError($"导出ID映射数据失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 导入ID映射数据
        /// </summary>
        /// <param name="jsonData">JSON格式的映射数据</param>
        /// <returns>导入是否成功</returns>
        public bool ImportIDMappingData(string jsonData)
        {
            try
            {
                var importData = JsonConvert.DeserializeObject<dynamic>(jsonData);
                var mappings = JsonConvert.DeserializeObject<Dictionary<string, IDMappingInfo>>(importData.mappings.ToString());

                foreach (var kvp in mappings)
                {
                    globalIDRegistry[kvp.Key] = kvp.Value;
                }

                LogMessage($"成功导入 {mappings.Count} 个ID映射");
                return true;
            }
            catch (System.Exception ex)
            {
                LogError($"导入ID映射数据失败: {ex.Message}");
                return false;
            }
        }
        #endregion

        #region 日志记录
        /// <summary>
        /// 记录普通消息
        /// </summary>
        /// <param name="message">消息内容</param>
        private void LogMessage(string message)
        {
            if (enableLogging)
            {
                Debug.Log($"[ItemInstanceIDManager] {message}");
            }
        }

        /// <summary>
        /// 记录警告消息
        /// </summary>
        /// <param name="message">警告内容</param>
        private void LogWarning(string message)
        {
            if (enableLogging)
            {
                Debug.LogWarning($"[ItemInstanceIDManager] {message}");
            }
        }

        /// <summary>
        /// 记录错误消息
        /// </summary>
        /// <param name="message">错误内容</param>
        private void LogError(string message)
        {
            if (enableLogging)
            {
                Debug.LogError($"[ItemInstanceIDManager] {message}");
            }
        }
        #endregion

        #region 数据结构定义
        /// <summary>
        /// ID冲突信息
        /// </summary>
        [System.Serializable]
        public class IDConflictInfo
        {
            public string conflictID;           // 冲突的ID
            public List<string> conflictObjects; // 使用该ID的对象列表
            public ConflictType type;           // 冲突类型
            public DateTime detectedTime;       // 检测时间
            public bool isResolved;            // 是否已解决

            public IDConflictInfo()
            {
                conflictObjects = new List<string>();
                detectedTime = DateTime.Now;
                isResolved = false;
            }
        }

        /// <summary>
        /// 冲突类型枚举
        /// </summary>
        public enum ConflictType
        {
            Duplicate,      // 重复ID
            Invalid,        // 无效ID
            Orphaned,       // 孤立引用
            CrossScene      // 跨场景冲突
        }

        /// <summary>
        /// ID映射信息
        /// </summary>
        [System.Serializable]
        public class IDMappingInfo
        {
            public string instanceID;          // 实例ID
            public string objectName;          // 对象名称
            public string sceneName;           // 场景名称
            public string objectType;          // 对象类型
            public Vector3 worldPosition;      // 世界坐标
            public DateTime lastUpdated;       // 最后更新时间
            public bool isActive;              // 是否激活

            public IDMappingInfo()
            {
                lastUpdated = DateTime.Now;
                isActive = true;
            }
        }
        #endregion

        #region 字段和属性
        [Header("ID管理配置")]
        [SerializeField] private bool enableAutoDetection = true;     // 是否启用自动检测
        [SerializeField] private float detectionInterval = 30f;      // 检测间隔（秒）
        [SerializeField] private bool enableAutoResolve = true;      // 是否启用自动解决
        [SerializeField] private bool enableLogging = true;          // 是否启用日志记录
        [SerializeField] private int maxConflictHistory = 100;       // 最大冲突历史记录数

        // 全局ID注册表 - 记录所有已分配的ID
        private Dictionary<string, IDMappingInfo> globalIDRegistry = new Dictionary<string, IDMappingInfo>();

        // ID冲突记录
        private List<IDConflictInfo> conflictHistory = new List<IDConflictInfo>();

        // 当前检测到的冲突
        private Dictionary<string, IDConflictInfo> activeConflicts = new Dictionary<string, IDConflictInfo>();

        // ID生成计数器
        private int idGenerationCounter = 0;

        // 场景切换时的ID缓存
        private Dictionary<string, string> sceneTransitionCache = new Dictionary<string, string>();

        // 检测协程
        private Coroutine detectionCoroutine;

        // 初始化状态
        private bool isInitialized = false;
        #endregion

        #region 事件定义
        // ID冲突事件
        public event Action<IDConflictInfo> OnConflictDetected;
        public event Action<string, string> OnConflictResolved; // 旧ID, 新ID

        // ID注册事件
        public event Action<string, IDMappingInfo> OnIDRegistered;
        public event Action<string> OnIDUnregistered;

        // 场景切换事件
        public event Action<string> OnSceneTransitionStart;
        public event Action<string> OnSceneTransitionComplete;
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

            // 初始化管理器
            InitializeManager();
        }

        private void Start()
        {
            // 启动自动检测
            if (enableAutoDetection)
            {
                StartAutoDetection();
            }
        }

        private void OnDestroy()
        {
            // 停止检测协程
            if (detectionCoroutine != null)
            {
                StopCoroutine(detectionCoroutine);
            }
        }
        #endregion

        #region 初始化方法
        /// <summary>
        /// 初始化管理器
        /// </summary>
        private void InitializeManager()
        {
            if (isInitialized) return;

            // 初始化数据结构
            globalIDRegistry = new Dictionary<string, IDMappingInfo>();
            conflictHistory = new List<IDConflictInfo>();
            activeConflicts = new Dictionary<string, IDConflictInfo>();
            sceneTransitionCache = new Dictionary<string, string>();

            // 重置计数器
            idGenerationCounter = 0;

            // 注册场景切换事件
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
            UnityEngine.SceneManagement.SceneManager.sceneUnloaded += OnSceneUnloaded;

            isInitialized = true;

            LogMessage("物品实例ID管理器初始化完成");
        }
        #endregion

        #region ID注册和管理
        /// <summary>
        /// 注册新的实例ID
        /// </summary>
        /// <param name="instanceID">实例ID</param>
        /// <param name="objectName">对象名称</param>
        /// <param name="objectType">对象类型</param>
        /// <param name="worldPosition">世界坐标</param>
        /// <returns>注册是否成功</returns>
        public bool RegisterInstanceID(string instanceID, string objectName, string objectType, Vector3 worldPosition)
        {
            if (string.IsNullOrEmpty(instanceID))
            {
                LogError($"尝试注册空的实例ID，对象: {objectName}");
                return false;
            }

            // 检查ID是否已存在
            if (globalIDRegistry.ContainsKey(instanceID))
            {
                // 检测到冲突
                DetectAndRecordConflict(instanceID, objectName, ConflictType.Duplicate);
                return false;
            }

            // 创建映射信息
            IDMappingInfo mappingInfo = new IDMappingInfo
            {
                instanceID = instanceID,
                objectName = objectName,
                sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
                objectType = objectType,
                worldPosition = worldPosition,
                lastUpdated = DateTime.Now,
                isActive = true
            };

            // 注册ID
            globalIDRegistry[instanceID] = mappingInfo;

            LogMessage($"成功注册实例ID: {instanceID}, 对象: {objectName}");
            OnIDRegistered?.Invoke(instanceID, mappingInfo);

            return true;
        }

        /// <summary>
        /// 注销实例ID
        /// </summary>
        /// <param name="instanceID">要注销的实例ID</param>
        /// <returns>注销是否成功</returns>
        public bool UnregisterInstanceID(string instanceID)
        {
            if (string.IsNullOrEmpty(instanceID))
            {
                return false;
            }

            bool removed = globalIDRegistry.Remove(instanceID);
            if (removed)
            {
                // 清理相关冲突记录
                activeConflicts.Remove(instanceID);

                LogMessage($"成功注销实例ID: {instanceID}");
                OnIDUnregistered?.Invoke(instanceID);
            }

            return removed;
        }

        /// <summary>
        /// 生成新的唯一实例ID
        /// </summary>
        /// <param name="prefix">ID前缀</param>
        /// <returns>新的唯一ID</returns>
        public string GenerateUniqueInstanceID(string prefix = "Item")
        {
            string newID;
            int attempts = 0;
            const int maxAttempts = 1000;

            do
            {
                idGenerationCounter++;
                string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                string randomPart = UnityEngine.Random.Range(1000, 9999).ToString();
                newID = $"{prefix}_{timestamp}_{idGenerationCounter:D4}_{randomPart}";
                attempts++;

                if (attempts >= maxAttempts)
                {
                    // 如果尝试次数过多，使用GUID确保唯一性
                    newID = $"{prefix}_{System.Guid.NewGuid().ToString("N")[..12]}";
                    break;
                }
            }
            while (globalIDRegistry.ContainsKey(newID));

            LogMessage($"生成新的唯一实例ID: {newID}");
            return newID;
        }

        /// <summary>
        /// 检查ID是否已注册
        /// </summary>
        /// <param name="instanceID">要检查的ID</param>
        /// <returns>是否已注册</returns>
        public bool IsIDRegistered(string instanceID)
        {
            return !string.IsNullOrEmpty(instanceID) && globalIDRegistry.ContainsKey(instanceID);
        }

        /// <summary>
        /// 获取ID映射信息
        /// </summary>
        /// <param name="instanceID">实例ID</param>
        /// <returns>映射信息，如果不存在则返回null</returns>
        public IDMappingInfo GetIDMappingInfo(string instanceID)
        {
            globalIDRegistry.TryGetValue(instanceID, out IDMappingInfo info);
            return info;
        }

        /// <summary>
        /// 更新ID映射信息
        /// </summary>
        /// <param name="instanceID">实例ID</param>
        /// <param name="worldPosition">新的世界坐标</param>
        /// <param name="isActive">是否激活</param>
        public void UpdateIDMappingInfo(string instanceID, Vector3 worldPosition, bool isActive = true)
        {
            if (globalIDRegistry.TryGetValue(instanceID, out IDMappingInfo info))
            {
                info.worldPosition = worldPosition;
                info.isActive = isActive;
                info.lastUpdated = DateTime.Now;
                info.sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            }
        }
        #endregion

        #region Unity生命周期扩展

        /// <summary>
        /// 应用程序暂停时保存数据
        /// </summary>
        /// <param name="pauseStatus">暂停状态</param>
        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                // 应用暂停时执行一次冲突检测
                PerformConflictDetection();
            }
        }

        /// <summary>
        /// 应用程序焦点变化时处理
        /// </summary>
        /// <param name="hasFocus">是否有焦点</param>
        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                // 失去焦点时执行一次冲突检测
                PerformConflictDetection();
            }
        }
        #endregion
    }
}