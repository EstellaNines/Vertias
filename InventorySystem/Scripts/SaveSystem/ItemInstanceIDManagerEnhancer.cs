using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;

namespace InventorySystem.SaveSystem
{
    /// <summary>
    /// 物品实例ID管理器增强器
    /// 为现有ItemInstanceIDManager提供高级冲突检测、解决和跨场景同步功能
    /// 采用非侵入式设计，通过组合模式扩展现有功能
    /// </summary>
    public class ItemInstanceIDManagerEnhancer : MonoBehaviour
    {
        #region 单例模式
        private static ItemInstanceIDManagerEnhancer _instance;
        public static ItemInstanceIDManagerEnhancer Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<ItemInstanceIDManagerEnhancer>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("ItemInstanceIDManagerEnhancer");
                        _instance = go.AddComponent<ItemInstanceIDManagerEnhancer>();

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
        [Header("ID管理增强配置")]
        [SerializeField]
        [Tooltip("是否启用高级冲突检测")]
        private bool enableAdvancedConflictDetection = true;

        [SerializeField]
        [Tooltip("是否启用自动冲突解决")]
        private bool enableAutoConflictResolution = true;

        [SerializeField]
        [Tooltip("是否启用跨场景同步")]
        private bool enableCrossSceneSync = true;

        [SerializeField]
        [Tooltip("是否启用ID历史追踪")]
        private bool enableIdHistoryTracking = true;

        [SerializeField]
        [Tooltip("是否启用性能监控")]
        private bool enablePerformanceMonitoring = true;

        [SerializeField]
        [Tooltip("冲突检测间隔（秒）")]
        private float conflictDetectionInterval = 5f;

        [SerializeField]
        [Tooltip("最大ID历史记录数")]
        private int maxIdHistoryCount = 1000;

        [SerializeField]
        [Tooltip("是否启用调试日志")]
        private bool enableDebugLog = true;
        #endregion

        #region 数据结构
        /// <summary>
        /// ID冲突信息
        /// </summary>
        [System.Serializable]
        public class IdConflictInfo
        {
            public string conflictId;
            public List<GameObject> conflictingObjects;
            public ConflictType type;
            public ConflictSeverity severity;
            public DateTime detectedTime;
            public bool isResolved;
            public string resolutionMethod;
            public List<string> resolutionActions;

            public IdConflictInfo(string id, ConflictType type)
            {
                conflictId = id;
                this.type = type;
                conflictingObjects = new List<GameObject>();
                resolutionActions = new List<string>();
                detectedTime = DateTime.Now;
            }
        }

        /// <summary>
        /// 冲突类型
        /// </summary>
        public enum ConflictType
        {
            DuplicateId,        // 重复ID
            InvalidFormat,      // 无效格式
            OrphanedReference,  // 孤立引用
            CircularReference,  // 循环引用
            MissingReference,   // 缺失引用
            InconsistentState   // 状态不一致
        }

        /// <summary>
        /// 冲突严重程度
        /// </summary>
        public enum ConflictSeverity
        {
            Low,
            Medium,
            High,
            Critical
        }

        /// <summary>
        /// ID历史记录
        /// </summary>
        [System.Serializable]
        public class IdHistoryRecord
        {
            public string objectId;
            public string oldId;
            public string newId;
            public string operation;
            public DateTime timestamp;
            public string reason;
            public string sceneContext;

            public IdHistoryRecord(string objectId, string oldId, string newId, string operation, string reason = "")
            {
                this.objectId = objectId;
                this.oldId = oldId;
                this.newId = newId;
                this.operation = operation;
                this.reason = reason;
                timestamp = DateTime.Now;
                sceneContext = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            }
        }

        /// <summary>
        /// 性能监控数据
        /// </summary>
        [System.Serializable]
        public class PerformanceMetrics
        {
            public int totalIdsManaged;
            public int conflictsDetected;
            public int conflictsResolved;
            public float averageDetectionTime;
            public float averageResolutionTime;
            public DateTime lastUpdateTime;
            public Dictionary<ConflictType, int> conflictTypeStats;

            public PerformanceMetrics()
            {
                conflictTypeStats = new Dictionary<ConflictType, int>();
                lastUpdateTime = DateTime.Now;
            }
        }

        /// <summary>
        /// 同步状态
        /// </summary>
        public enum SyncStatus
        {
            Idle,
            Syncing,
            Completed,
            Failed
        }
        #endregion

        #region 私有字段
        private ItemInstanceIDManager idManager;
        private Dictionary<string, IdConflictInfo> activeConflicts;
        private List<IdHistoryRecord> idHistory;
        private PerformanceMetrics performanceMetrics;
        private Coroutine conflictDetectionCoroutine;
        private SyncStatus currentSyncStatus = SyncStatus.Idle;

        // 缓存数据
        private Dictionary<string, GameObject> idToObjectCache;
        private Dictionary<GameObject, string> objectToIdCache;
        private HashSet<string> knownValidIds;
        #endregion

        #region 事件定义
        // ID管理事件
        public event Action<IdConflictInfo> OnConflictDetected;
        public event Action<IdConflictInfo> OnConflictResolved;
        public event Action<IdHistoryRecord> OnIdChanged;
        public event Action<SyncStatus> OnSyncStatusChanged;
        public event Action<PerformanceMetrics> OnPerformanceUpdate;
        public event Action<string, float> OnSyncProgress;
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

        private void Start()
        {
            // 启动冲突检测
            if (enableAdvancedConflictDetection)
            {
                StartConflictDetection();
            }
        }

        private void OnDestroy()
        {
            // 停止冲突检测
            if (conflictDetectionCoroutine != null)
            {
                StopCoroutine(conflictDetectionCoroutine);
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
            activeConflicts = new Dictionary<string, IdConflictInfo>();
            idHistory = new List<IdHistoryRecord>();
            performanceMetrics = new PerformanceMetrics();

            idToObjectCache = new Dictionary<string, GameObject>();
            objectToIdCache = new Dictionary<GameObject, string>();
            knownValidIds = new HashSet<string>();

            // 获取依赖组件
            idManager = ItemInstanceIDManager.Instance;

            // 初始化缓存
            RefreshIdCache();

            LogDebug("ItemInstanceIDManagerEnhancer已初始化");
        }
        #endregion

        #region 主要增强方法
        /// <summary>
        /// 执行高级冲突检测
        /// </summary>
        /// <returns>检测到的冲突列表</returns>
        public List<IdConflictInfo> PerformAdvancedConflictDetection()
        {
            var startTime = Time.realtimeSinceStartup;
            var conflicts = new List<IdConflictInfo>();

            try
            {
                LogDebug("开始高级冲突检测");

                // 1. 检测重复ID
                var duplicateConflicts = DetectDuplicateIds();
                conflicts.AddRange(duplicateConflicts);

                // 2. 检测无效格式
                var formatConflicts = DetectInvalidFormats();
                conflicts.AddRange(formatConflicts);

                // 3. 检测孤立引用
                var orphanedConflicts = DetectOrphanedReferences();
                conflicts.AddRange(orphanedConflicts);

                // 4. 检测循环引用
                var circularConflicts = DetectCircularReferences();
                conflicts.AddRange(circularConflicts);

                // 5. 检测缺失引用
                var missingConflicts = DetectMissingReferences();
                conflicts.AddRange(missingConflicts);

                // 6. 检测状态不一致
                var inconsistentConflicts = DetectInconsistentStates();
                conflicts.AddRange(inconsistentConflicts);

                // 更新活跃冲突列表
                foreach (var conflict in conflicts)
                {
                    if (!activeConflicts.ContainsKey(conflict.conflictId))
                    {
                        activeConflicts[conflict.conflictId] = conflict;
                        OnConflictDetected?.Invoke(conflict);
                    }
                }

                // 更新性能指标
                if (enablePerformanceMonitoring)
                {
                    performanceMetrics.conflictsDetected += conflicts.Count;
                    performanceMetrics.averageDetectionTime = Time.realtimeSinceStartup - startTime;
                    performanceMetrics.lastUpdateTime = DateTime.Now;

                    foreach (var conflict in conflicts)
                    {
                        if (performanceMetrics.conflictTypeStats.ContainsKey(conflict.type))
                        {
                            performanceMetrics.conflictTypeStats[conflict.type]++;
                        }
                        else
                        {
                            performanceMetrics.conflictTypeStats[conflict.type] = 1;
                        }
                    }

                    OnPerformanceUpdate?.Invoke(performanceMetrics);
                }

                LogDebug($"冲突检测完成，发现{conflicts.Count}个冲突，耗时{Time.realtimeSinceStartup - startTime:F3}秒");
            }
            catch (Exception ex)
            {
                LogError($"冲突检测过程中发生异常: {ex.Message}");
            }

            return conflicts;
        }

        /// <summary>
        /// 自动解决冲突
        /// </summary>
        /// <param name="conflicts">要解决的冲突列表</param>
        /// <returns>解决结果</returns>
        public Dictionary<string, bool> AutoResolveConflicts(List<IdConflictInfo> conflicts = null)
        {
            var results = new Dictionary<string, bool>();
            var conflictsToResolve = conflicts ?? activeConflicts.Values.ToList();

            var startTime = Time.realtimeSinceStartup;

            LogDebug($"开始自动解决{conflictsToResolve.Count}个冲突");

            foreach (var conflict in conflictsToResolve)
            {
                try
                {
                    bool resolved = ResolveConflict(conflict);
                    results[conflict.conflictId] = resolved;

                    if (resolved)
                    {
                        conflict.isResolved = true;
                        activeConflicts.Remove(conflict.conflictId);
                        OnConflictResolved?.Invoke(conflict);

                        if (enablePerformanceMonitoring)
                        {
                            performanceMetrics.conflictsResolved++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    results[conflict.conflictId] = false;
                    LogError($"解决冲突{conflict.conflictId}时发生异常: {ex.Message}");
                }
            }

            if (enablePerformanceMonitoring)
            {
                performanceMetrics.averageResolutionTime = Time.realtimeSinceStartup - startTime;
                OnPerformanceUpdate?.Invoke(performanceMetrics);
            }

            LogDebug($"冲突解决完成，成功解决{results.Values.Count(r => r)}个冲突");
            return results;
        }

        /// <summary>
        /// 执行跨场景ID同步
        /// </summary>
        /// <returns>同步协程</returns>
        public Coroutine PerformCrossSceneSync()
        {
            if (currentSyncStatus != SyncStatus.Idle)
            {
                LogDebug("同步操作已在进行中");
                return null;
            }

            return StartCoroutine(CrossSceneSyncCoroutine());
        }

        /// <summary>
        /// 强制刷新ID缓存
        /// </summary>
        public void RefreshIdCache()
        {
            try
            {
                idToObjectCache.Clear();
                objectToIdCache.Clear();
                knownValidIds.Clear();

                // 收集所有ISaveable对象的ID
                var saveableObjects = FindObjectsOfType<MonoBehaviour>().OfType<ISaveable>().ToList();

                foreach (var saveable in saveableObjects)
                {
                    try
                    {
                        var saveId = saveable.GetSaveID();
                        if (!string.IsNullOrEmpty(saveId))
                        {
                            var gameObject = (saveable as MonoBehaviour)?.gameObject;
                            if (gameObject != null)
                            {
                                idToObjectCache[saveId] = gameObject;
                                objectToIdCache[gameObject] = saveId;
                                knownValidIds.Add(saveId);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogError($"获取对象{saveable}的保存ID时发生异常: {ex.Message}");
                    }
                }

                // 更新性能指标
                if (enablePerformanceMonitoring)
                {
                    performanceMetrics.totalIdsManaged = knownValidIds.Count;
                }

                LogDebug($"ID缓存已刷新，管理{knownValidIds.Count}个ID");
            }
            catch (Exception ex)
            {
                LogError($"刷新ID缓存时发生异常: {ex.Message}");
            }
        }
        #endregion

        #region 冲突检测实现
        /// <summary>
        /// 检测重复ID
        /// </summary>
        private List<IdConflictInfo> DetectDuplicateIds()
        {
            var conflicts = new List<IdConflictInfo>();
            var idGroups = idToObjectCache.GroupBy(kvp => kvp.Key).Where(g => g.Count() > 1);

            foreach (var group in idGroups)
            {
                var conflict = new IdConflictInfo(group.Key, ConflictType.DuplicateId)
                {
                    severity = ConflictSeverity.High
                };

                conflict.conflictingObjects.AddRange(group.Select(kvp => kvp.Value));
                conflicts.Add(conflict);
            }

            return conflicts;
        }

        /// <summary>
        /// 检测无效格式
        /// </summary>
        private List<IdConflictInfo> DetectInvalidFormats()
        {
            var conflicts = new List<IdConflictInfo>();

            foreach (var kvp in idToObjectCache)
            {
                var id = kvp.Key;
                var obj = kvp.Value;

                // 检查ID格式是否有效
                if (!IsValidIdFormat(id))
                {
                    var conflict = new IdConflictInfo(id, ConflictType.InvalidFormat)
                    {
                        severity = ConflictSeverity.Medium
                    };
                    conflict.conflictingObjects.Add(obj);
                    conflicts.Add(conflict);
                }
            }

            return conflicts;
        }

        /// <summary>
        /// 检测孤立引用
        /// </summary>
        private List<IdConflictInfo> DetectOrphanedReferences()
        {
            var conflicts = new List<IdConflictInfo>();

            // 检查缓存中的对象是否仍然存在
            var orphanedIds = new List<string>();

            foreach (var kvp in idToObjectCache)
            {
                if (kvp.Value == null)
                {
                    orphanedIds.Add(kvp.Key);
                }
            }

            foreach (var orphanedId in orphanedIds)
            {
                var conflict = new IdConflictInfo(orphanedId, ConflictType.OrphanedReference)
                {
                    severity = ConflictSeverity.Low
                };
                conflicts.Add(conflict);
            }

            return conflicts;
        }

        /// <summary>
        /// 检测循环引用
        /// </summary>
        private List<IdConflictInfo> DetectCircularReferences()
        {
            var conflicts = new List<IdConflictInfo>();

            // 这里可以根据具体的引用关系来检测循环引用
            // 目前简化实现

            return conflicts;
        }

        /// <summary>
        /// 检测缺失引用
        /// </summary>
        private List<IdConflictInfo> DetectMissingReferences()
        {
            var conflicts = new List<IdConflictInfo>();

            // 检查ItemDataHolder中的引用
            var itemHolders = FindObjectsOfType<ItemDataHolder>();

            foreach (var holder in itemHolders)
            {
                try
                {
                    var saveId = holder.GetSaveID();

                    // 检查物品数据引用
                    if (holder.GetItemData() == null)
                    {
                        var conflict = new IdConflictInfo(saveId, ConflictType.MissingReference)
                        {
                            severity = ConflictSeverity.High
                        };
                        conflict.conflictingObjects.Add(holder.gameObject);
                        conflicts.Add(conflict);
                    }

                    // 检查实例数据引用
                    if (holder.GetInstanceData() == null)
                    {
                        var conflict = new IdConflictInfo(saveId, ConflictType.MissingReference)
                        {
                            severity = ConflictSeverity.Medium
                        };
                        conflict.conflictingObjects.Add(holder.gameObject);
                        conflicts.Add(conflict);
                    }
                }
                catch (Exception ex)
                {
                    LogError($"检测缺失引用时发生异常: {ex.Message}");
                }
            }

            return conflicts;
        }

        /// <summary>
        /// 检测状态不一致
        /// </summary>
        private List<IdConflictInfo> DetectInconsistentStates()
        {
            var conflicts = new List<IdConflictInfo>();

            // 检查对象的激活状态与保存数据的一致性
            foreach (var kvp in idToObjectCache)
            {
                var id = kvp.Key;
                var obj = kvp.Value;

                if (obj != null)
                {
                    var saveable = obj.GetComponent<ISaveable>();
                    if (saveable != null)
                    {
                        try
                        {
                            var saveData = saveable.SerializeToJson();
                            // 这里可以根据具体的保存数据结构来检查状态一致性
                            // 目前简化实现
                        }
                        catch (Exception)
                        {
                            var conflict = new IdConflictInfo(id, ConflictType.InconsistentState)
                            {
                                severity = ConflictSeverity.Medium
                            };
                            conflict.conflictingObjects.Add(obj);
                            conflicts.Add(conflict);
                        }
                    }
                }
            }

            return conflicts;
        }
        #endregion

        #region 冲突解决实现
        /// <summary>
        /// 解决单个冲突
        /// </summary>
        private bool ResolveConflict(IdConflictInfo conflict)
        {
            try
            {
                switch (conflict.type)
                {
                    case ConflictType.DuplicateId:
                        return ResolveDuplicateId(conflict);
                    case ConflictType.InvalidFormat:
                        return ResolveInvalidFormat(conflict);
                    case ConflictType.OrphanedReference:
                        return ResolveOrphanedReference(conflict);
                    case ConflictType.MissingReference:
                        return ResolveMissingReference(conflict);
                    case ConflictType.InconsistentState:
                        return ResolveInconsistentState(conflict);
                    default:
                        return false;
                }
            }
            catch (Exception ex)
            {
                LogError($"解决冲突{conflict.conflictId}时发生异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 解决重复ID冲突
        /// </summary>
        private bool ResolveDuplicateId(IdConflictInfo conflict)
        {
            if (conflict.conflictingObjects.Count <= 1)
                return true;

            // 保留第一个对象，为其他对象重新生成ID
            for (int i = 1; i < conflict.conflictingObjects.Count; i++)
            {
                var obj = conflict.conflictingObjects[i];
                if (obj != null)
                {
                    var saveable = obj.GetComponent<ISaveable>();
                    if (saveable != null)
                    {
                        var oldId = conflict.conflictId;
                        var newId = GenerateUniqueId();

                        // 这里需要根据具体的ISaveable实现来设置新ID
                        // 由于ISaveable接口可能没有SetSaveID方法，我们需要特殊处理
                        if (TrySetObjectId(saveable, newId))
                        {
                            // 更新缓存
                            idToObjectCache.Remove(oldId);
                            idToObjectCache[newId] = obj;
                            objectToIdCache[obj] = newId;
                            knownValidIds.Add(newId);

                            // 记录历史
                            if (enableIdHistoryTracking)
                            {
                                var historyRecord = new IdHistoryRecord(obj.name, oldId, newId, "ResolveDuplicateId", "解决重复ID冲突");
                                idHistory.Add(historyRecord);
                                OnIdChanged?.Invoke(historyRecord);
                            }

                            conflict.resolutionActions.Add($"为对象{obj.name}重新生成ID: {newId}");
                        }
                    }
                }
            }

            conflict.resolutionMethod = "重新生成重复对象的ID";
            return true;
        }

        /// <summary>
        /// 解决无效格式冲突
        /// </summary>
        private bool ResolveInvalidFormat(IdConflictInfo conflict)
        {
            foreach (var obj in conflict.conflictingObjects)
            {
                if (obj != null)
                {
                    var saveable = obj.GetComponent<ISaveable>();
                    if (saveable != null)
                    {
                        var oldId = conflict.conflictId;
                        var newId = GenerateUniqueId();

                        if (TrySetObjectId(saveable, newId))
                        {
                            // 更新缓存
                            idToObjectCache.Remove(oldId);
                            idToObjectCache[newId] = obj;
                            objectToIdCache[obj] = newId;
                            knownValidIds.Remove(oldId);
                            knownValidIds.Add(newId);

                            // 记录历史
                            if (enableIdHistoryTracking)
                            {
                                var historyRecord = new IdHistoryRecord(obj.name, oldId, newId, "ResolveInvalidFormat", "修复无效ID格式");
                                idHistory.Add(historyRecord);
                                OnIdChanged?.Invoke(historyRecord);
                            }

                            conflict.resolutionActions.Add($"为对象{obj.name}生成有效格式ID: {newId}");
                        }
                    }
                }
            }

            conflict.resolutionMethod = "重新生成有效格式的ID";
            return true;
        }

        /// <summary>
        /// 解决孤立引用冲突
        /// </summary>
        private bool ResolveOrphanedReference(IdConflictInfo conflict)
        {
            // 从缓存中移除孤立引用
            idToObjectCache.Remove(conflict.conflictId);
            knownValidIds.Remove(conflict.conflictId);

            conflict.resolutionMethod = "清理孤立引用";
            conflict.resolutionActions.Add($"从缓存中移除孤立ID: {conflict.conflictId}");

            return true;
        }

        /// <summary>
        /// 解决缺失引用冲突
        /// </summary>
        private bool ResolveMissingReference(IdConflictInfo conflict)
        {
            // 这里需要根据具体的业务逻辑来修复缺失引用
            // 目前简化实现，只是记录问题
            conflict.resolutionMethod = "记录缺失引用问题";
            conflict.resolutionActions.Add($"检测到缺失引用: {conflict.conflictId}");

            return false; // 需要手动处理
        }

        /// <summary>
        /// 解决状态不一致冲突
        /// </summary>
        private bool ResolveInconsistentState(IdConflictInfo conflict)
        {
            // 这里需要根据具体的状态不一致类型来修复
            // 目前简化实现
            conflict.resolutionMethod = "记录状态不一致问题";
            conflict.resolutionActions.Add($"检测到状态不一致: {conflict.conflictId}");

            return false; // 需要手动处理
        }
        #endregion

        #region 跨场景同步
        /// <summary>
        /// 跨场景同步协程
        /// </summary>
        private IEnumerator CrossSceneSyncCoroutine()
        {
            SetSyncStatus(SyncStatus.Syncing);

            bool syncSuccess = false;
            string errorMessage = "";

            // 执行同步操作
            yield return StartCoroutine(PerformSyncOperations((success, error) =>
            {
                syncSuccess = success;
                errorMessage = error;
            }));

            // 处理同步结果
            if (syncSuccess)
            {
                SetSyncStatus(SyncStatus.Completed);
                LogDebug("跨场景同步完成");
            }
            else
            {
                SetSyncStatus(SyncStatus.Failed);
                LogError($"跨场景同步失败: {errorMessage}");
            }
        }

        /// <summary>
        /// 执行同步操作
        /// </summary>
        private IEnumerator PerformSyncOperations(System.Action<bool, string> callback)
        {
            string errorMessage = "";
            bool hasError = false;

            // 步骤1: 开始同步
            OnSyncProgress?.Invoke("开始跨场景同步", 0.1f);
            yield return null;

            // 步骤2: 收集当前场景的所有ID
            OnSyncProgress?.Invoke("收集当前场景ID", 0.3f);
            try
            {
                RefreshIdCache();
            }
            catch (Exception ex)
            {
                hasError = true;
                errorMessage = $"刷新ID缓存失败: {ex.Message}";
            }
            yield return null;

            if (hasError)
            {
                callback?.Invoke(false, errorMessage);
                yield break;
            }

            // 步骤3: 检测冲突
            OnSyncProgress?.Invoke("检测ID冲突", 0.5f);
            List<IdConflictInfo> conflicts = null;
            try
            {
                conflicts = PerformAdvancedConflictDetection();
            }
            catch (Exception ex)
            {
                hasError = true;
                errorMessage = $"冲突检测失败: {ex.Message}";
            }
            yield return null;

            if (hasError)
            {
                callback?.Invoke(false, errorMessage);
                yield break;
            }

            // 步骤4: 解决冲突
            if (conflicts != null && conflicts.Count > 0 && enableAutoConflictResolution)
            {
                OnSyncProgress?.Invoke("解决ID冲突", 0.7f);
                try
                {
                    AutoResolveConflicts(conflicts);
                }
                catch (Exception ex)
                {
                    hasError = true;
                    errorMessage = $"冲突解决失败: {ex.Message}";
                }
                yield return null;

                if (hasError)
                {
                    callback?.Invoke(false, errorMessage);
                    yield break;
                }
            }

            // 步骤5: 更新全局ID注册表
            OnSyncProgress?.Invoke("更新全局注册表", 0.9f);
            try
            {
                // 这里可以与ItemInstanceIDManager进行同步
                // 目前简化实现
            }
            catch (Exception ex)
            {
                hasError = true;
                errorMessage = $"更新全局注册表失败: {ex.Message}";
            }
            yield return null;

            if (hasError)
            {
                callback?.Invoke(false, errorMessage);
                yield break;
            }

            // 步骤6: 完成同步
            OnSyncProgress?.Invoke("同步完成", 1.0f);
            callback?.Invoke(true, "");
        }
        #endregion

        #region 冲突检测协程
        /// <summary>
        /// 启动冲突检测
        /// </summary>
        private void StartConflictDetection()
        {
            if (conflictDetectionCoroutine == null)
            {
                conflictDetectionCoroutine = StartCoroutine(ConflictDetectionCoroutine());
                LogDebug("冲突检测已启动");
            }
        }

        /// <summary>
        /// 停止冲突检测
        /// </summary>
        private void StopConflictDetection()
        {
            if (conflictDetectionCoroutine != null)
            {
                StopCoroutine(conflictDetectionCoroutine);
                conflictDetectionCoroutine = null;
                LogDebug("冲突检测已停止");
            }
        }

        /// <summary>
        /// 冲突检测协程
        /// </summary>
        private IEnumerator ConflictDetectionCoroutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(conflictDetectionInterval);

                try
                {
                    // 刷新缓存
                    RefreshIdCache();

                    // 执行冲突检测
                    var conflicts = PerformAdvancedConflictDetection();

                    // 自动解决冲突
                    if (conflicts.Count > 0 && enableAutoConflictResolution)
                    {
                        AutoResolveConflicts(conflicts);
                    }
                }
                catch (Exception ex)
                {
                    LogError($"定期冲突检测过程中发生异常: {ex.Message}");
                }
            }
        }
        #endregion

        #region 辅助方法
        /// <summary>
        /// 设置同步状态
        /// </summary>
        private void SetSyncStatus(SyncStatus status)
        {
            if (currentSyncStatus != status)
            {
                currentSyncStatus = status;
                OnSyncStatusChanged?.Invoke(status);
            }
        }

        /// <summary>
        /// 检查ID格式是否有效
        /// </summary>
        private bool IsValidIdFormat(string id)
        {
            if (string.IsNullOrEmpty(id))
                return false;

            // 检查是否包含无效字符
            if (id.Contains("\0") || id.Contains("\n") || id.Contains("\r"))
                return false;

            // 检查长度
            if (id.Length < 5 || id.Length > 200)
                return false;

            return true;
        }

        /// <summary>
        /// 生成唯一ID
        /// </summary>
        private string GenerateUniqueId()
        {
            string newId;
            do
            {
                newId = $"Enhanced_{Guid.NewGuid().ToString().Substring(0, 8)}_{DateTime.Now.Ticks}";
            } while (knownValidIds.Contains(newId));

            return newId;
        }

        /// <summary>
        /// 尝试设置对象ID
        /// </summary>
        private bool TrySetObjectId(ISaveable saveable, string newId)
        {
            try
            {
                // 这里需要根据具体的ISaveable实现来设置ID
                // 由于接口可能没有SetSaveID方法，我们使用反射
                var type = saveable.GetType();
                var setIdMethod = type.GetMethod("SetSaveID");

                if (setIdMethod != null)
                {
                    setIdMethod.Invoke(saveable, new object[] { newId });
                    return true;
                }

                // 如果没有SetSaveID方法，尝试设置字段
                var idField = type.GetField("saveID", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (idField != null)
                {
                    idField.SetValue(saveable, newId);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                LogError($"设置对象ID时发生异常: {ex.Message}");
                return false;
            }
        }
        #endregion

        #region 公共查询方法
        /// <summary>
        /// 获取活跃冲突
        /// </summary>
        public Dictionary<string, IdConflictInfo> GetActiveConflicts()
        {
            return new Dictionary<string, IdConflictInfo>(activeConflicts);
        }

        /// <summary>
        /// 获取ID历史记录
        /// </summary>
        public List<IdHistoryRecord> GetIdHistory(int maxCount = -1)
        {
            if (maxCount <= 0)
                return new List<IdHistoryRecord>(idHistory);

            return idHistory.TakeLast(maxCount).ToList();
        }

        /// <summary>
        /// 获取性能指标
        /// </summary>
        public PerformanceMetrics GetPerformanceMetrics()
        {
            return performanceMetrics;
        }

        /// <summary>
        /// 获取当前同步状态
        /// </summary>
        public SyncStatus GetSyncStatus()
        {
            return currentSyncStatus;
        }

        /// <summary>
        /// 清除历史记录
        /// </summary>
        public void ClearHistory()
        {
            idHistory.Clear();
            LogDebug("ID历史记录已清除");
        }

        /// <summary>
        /// 获取增强器统计信息
        /// </summary>
        public string GetEnhancerStatistics()
        {
            var stats = new System.Text.StringBuilder();
            stats.AppendLine("=== ID管理器增强器统计信息 ===");
            stats.AppendLine($"高级冲突检测: {(enableAdvancedConflictDetection ? "启用" : "禁用")}");
            stats.AppendLine($"自动冲突解决: {(enableAutoConflictResolution ? "启用" : "禁用")}");
            stats.AppendLine($"跨场景同步: {(enableCrossSceneSync ? "启用" : "禁用")}");
            stats.AppendLine($"ID历史追踪: {(enableIdHistoryTracking ? "启用" : "禁用")}");
            stats.AppendLine($"性能监控: {(enablePerformanceMonitoring ? "启用" : "禁用")}");
            stats.AppendLine($"当前同步状态: {currentSyncStatus}");
            stats.AppendLine($"管理的ID数量: {knownValidIds.Count}");
            stats.AppendLine($"活跃冲突数: {activeConflicts.Count}");
            stats.AppendLine($"历史记录数: {idHistory.Count}");
            stats.AppendLine($"冲突检测间隔: {conflictDetectionInterval}秒");

            if (enablePerformanceMonitoring)
            {
                stats.AppendLine($"检测到的冲突总数: {performanceMetrics.conflictsDetected}");
                stats.AppendLine($"解决的冲突总数: {performanceMetrics.conflictsResolved}");
                stats.AppendLine($"平均检测时间: {performanceMetrics.averageDetectionTime:F3}秒");
                stats.AppendLine($"平均解决时间: {performanceMetrics.averageResolutionTime:F3}秒");
            }

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
                Debug.Log($"[ItemInstanceIDManagerEnhancer] {message}");
            }
        }

        /// <summary>
        /// 输出错误日志
        /// </summary>
        private void LogError(string message)
        {
            Debug.LogError($"[ItemInstanceIDManagerEnhancer] {message}");
        }
        #endregion
    }
}