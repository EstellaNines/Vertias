using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;

namespace InventorySystem.SaveSystem
{
    /// <summary>
    /// 保存管理器 - 负责协调所有ISaveable对象的保存和加载操作
    /// 提供统一的保存系统入口点，管理对象注册、依赖关系和保存流程
    /// </summary>
    public class SaveManager : MonoBehaviour
    {
        #region 单例模式
        private static SaveManager _instance;
        public static SaveManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<SaveManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("SaveManager");
                        _instance = go.AddComponent<SaveManager>();

                        // 将新创建的SaveManager设置为SaveSystem的子对象（如果存在SaveSystem）
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

        #region 字段和属性
        [Header("保存系统配置")]
        [SerializeField] private bool autoSaveEnabled = true; // 是否启用自动保存
        [SerializeField] private float autoSaveInterval = 60f; // 自动保存间隔（秒）
        [SerializeField] private int maxBackupCount = 5; // 最大备份文件数量
        [SerializeField] private bool enableLogging = true; // 是否启用日志记录

        // 注册的ISaveable对象字典 - 使用SaveID作为键
        private Dictionary<string, ISaveable> registeredObjects = new Dictionary<string, ISaveable>();

        // 对象依赖关系图 - 用于确定保存/加载顺序
        private Dictionary<string, List<string>> dependencyGraph = new Dictionary<string, List<string>>();

        // 变化跟踪 - 记录哪些对象发生了变化
        private HashSet<string> changedObjects = new HashSet<string>();

        // 保存操作状态
        private bool isSaving = false;
        private bool isLoading = false;

        // 组件引用
        private SaveDataSerializer serializer;
        private SaveFileManager fileManager;

        // 组件初始化状态
        private bool isInitialized = false;

        // 自动保存协程
        private Coroutine autoSaveCoroutine;
        #endregion

        #region 事件定义
        // 保存相关事件
        public event Action<string> OnBeforeSave; // 保存前事件
        public event Action<float, string> OnSaveProgress; // 保存进度事件
        public event Action<string, bool> OnSaveComplete; // 保存完成事件

        // 加载相关事件
        public event Action<string> OnBeforeLoad; // 加载前事件
        public event Action<float, string> OnLoadProgress; // 加载进度事件
        public event Action<string, bool> OnLoadComplete; // 加载完成事件

        // 对象注册事件
        public event Action<ISaveable> OnObjectRegistered; // 对象注册事件
        public event Action<string> OnObjectUnregistered; // 对象注销事件
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

            // 注意：不在这里调用DontDestroyOnLoad，因为SaveSystemPersistence已经处理了整个系统的持久化
            // 如果这个组件是SaveSystem的子对象，它会自动跟随父对象进行跨场景持久化

            // 初始化组件
            InitializeComponents();
        }

        private void Start()
        {
            // 启动自动保存
            if (autoSaveEnabled)
            {
                StartAutoSave();
            }

            LogMessage("SaveManager已启动");
        }

        private void OnDestroy()
        {
            // 停止自动保存
            if (autoSaveCoroutine != null)
            {
                StopCoroutine(autoSaveCoroutine);
            }
        }
        #endregion

        #region 初始化方法
        /// <summary>
        /// 公共初始化方法 - 供外部调用
        /// </summary>
        public void Initialize()
        {
            if (!isInitialized)
            {
                InitializeComponents();
            }
            LogMessage("SaveManager初始化完成");
        }

        /// <summary>
        /// 初始化保存系统组件
        /// </summary>
        private void InitializeComponents()
        {
            // 获取或创建序列化器组件
            serializer = GetComponent<SaveDataSerializer>();
            if (serializer == null)
            {
                serializer = gameObject.AddComponent<SaveDataSerializer>();
            }

            // 获取或创建文件管理器组件
            fileManager = GetComponent<SaveFileManager>();
            if (fileManager == null)
            {
                fileManager = gameObject.AddComponent<SaveFileManager>();
            }

            // 初始化组件
            serializer.Initialize();
            fileManager.Initialize();

            isInitialized = true;
        }
        #endregion

        #region 对象注册管理
        /// <summary>
        /// 注册ISaveable对象到保存系统
        /// </summary>
        /// <param name="saveable">要注册的ISaveable对象</param>
        /// <returns>注册是否成功</returns>
        public bool RegisterSaveable(ISaveable saveable)
        {
            if (saveable == null)
            {
                LogError("尝试注册空的ISaveable对象");
                return false;
            }

            string saveId = saveable.GetSaveID();
            if (string.IsNullOrEmpty(saveId))
            {
                LogError($"ISaveable对象的SaveID为空: {saveable.GetType().Name}");
                return false;
            }

            // 检查是否已经注册
            if (registeredObjects.ContainsKey(saveId))
            {
                LogWarning($"SaveID已存在，将覆盖注册: {saveId}");
            }

            // 注册对象
            registeredObjects[saveId] = saveable;

            // 初始化依赖关系
            if (!dependencyGraph.ContainsKey(saveId))
            {
                dependencyGraph[saveId] = new List<string>();
            }

            LogMessage($"已注册ISaveable对象: {saveId}");
            OnObjectRegistered?.Invoke(saveable);

            return true;
        }

        /// <summary>
        /// 注销ISaveable对象
        /// </summary>
        /// <param name="saveId">要注销的对象SaveID</param>
        /// <returns>注销是否成功</returns>
        public bool UnregisterSaveable(string saveId)
        {
            if (string.IsNullOrEmpty(saveId))
            {
                return false;
            }

            bool removed = registeredObjects.Remove(saveId);
            if (removed)
            {
                // 清理依赖关系
                dependencyGraph.Remove(saveId);

                // 从其他对象的依赖列表中移除
                foreach (var deps in dependencyGraph.Values)
                {
                    deps.Remove(saveId);
                }

                // 清理变化跟踪
                changedObjects.Remove(saveId);

                LogMessage($"已注销ISaveable对象: {saveId}");
                OnObjectUnregistered?.Invoke(saveId);
            }

            return removed;
        }

        /// <summary>
        /// 注销ISaveable对象（重载方法）
        /// </summary>
        /// <param name="saveable">要注销的ISaveable对象</param>
        /// <returns>注销是否成功</returns>
        public bool UnregisterSaveable(ISaveable saveable)
        {
            if (saveable == null)
            {
                return false;
            }

            return UnregisterSaveable(saveable.GetSaveID());
        }

        /// <summary>
        /// 获取已注册的ISaveable对象
        /// </summary>
        /// <param name="saveId">对象SaveID</param>
        /// <returns>ISaveable对象，如果不存在则返回null</returns>
        public ISaveable GetRegisteredObject(string saveId)
        {
            registeredObjects.TryGetValue(saveId, out ISaveable obj);
            return obj;
        }

        /// <summary>
        /// 获取所有已注册对象的SaveID列表
        /// </summary>
        /// <returns>SaveID列表</returns>
        public List<string> GetAllRegisteredIds()
        {
            return new List<string>(registeredObjects.Keys);
        }
        #endregion

        #region 依赖关系管理
        /// <summary>
        /// 添加对象依赖关系
        /// </summary>
        /// <param name="objectId">依赖者ID</param>
        /// <param name="dependencyId">被依赖者ID</param>
        public void AddDependency(string objectId, string dependencyId)
        {
            if (!dependencyGraph.ContainsKey(objectId))
            {
                dependencyGraph[objectId] = new List<string>();
            }

            if (!dependencyGraph[objectId].Contains(dependencyId))
            {
                dependencyGraph[objectId].Add(dependencyId);
                LogMessage($"添加依赖关系: {objectId} -> {dependencyId}");
            }
        }

        /// <summary>
        /// 获取拓扑排序后的保存顺序
        /// </summary>
        /// <returns>按依赖关系排序的SaveID列表</returns>
        private List<string> GetSaveOrder()
        {
            var result = new List<string>();
            var visited = new HashSet<string>();
            var visiting = new HashSet<string>();

            foreach (var objectId in registeredObjects.Keys)
            {
                if (!visited.Contains(objectId))
                {
                    if (!TopologicalSort(objectId, visited, visiting, result))
                    {
                        LogError("检测到循环依赖，使用默认顺序");
                        return new List<string>(registeredObjects.Keys);
                    }
                }
            }

            result.Reverse(); // 反转以获得正确的保存顺序
            return result;
        }

        /// <summary>
        /// 拓扑排序辅助方法
        /// </summary>
        private bool TopologicalSort(string objectId, HashSet<string> visited, HashSet<string> visiting, List<string> result)
        {
            if (visiting.Contains(objectId))
            {
                return false; // 检测到循环依赖
            }

            if (visited.Contains(objectId))
            {
                return true;
            }

            visiting.Add(objectId);

            if (dependencyGraph.ContainsKey(objectId))
            {
                foreach (var dependency in dependencyGraph[objectId])
                {
                    if (!TopologicalSort(dependency, visited, visiting, result))
                    {
                        return false;
                    }
                }
            }

            visiting.Remove(objectId);
            visited.Add(objectId);
            result.Add(objectId);

            return true;
        }
        #endregion

        #region 变化跟踪
        /// <summary>
        /// 标记对象已发生变化
        /// </summary>
        /// <param name="saveId">对象SaveID</param>
        public void MarkObjectChanged(string saveId)
        {
            if (registeredObjects.ContainsKey(saveId))
            {
                changedObjects.Add(saveId);
                LogMessage($"对象已标记为变化: {saveId}");
            }
        }

        /// <summary>
        /// 清除对象的变化标记
        /// </summary>
        /// <param name="saveId">对象SaveID</param>
        public void ClearObjectChanged(string saveId)
        {
            changedObjects.Remove(saveId);
        }

        /// <summary>
        /// 检查对象是否发生变化
        /// </summary>
        /// <param name="saveId">对象SaveID</param>
        /// <returns>是否发生变化</returns>
        public bool IsObjectChanged(string saveId)
        {
            return changedObjects.Contains(saveId);
        }

        /// <summary>
        /// 获取所有发生变化的对象ID
        /// </summary>
        /// <returns>变化对象ID列表</returns>
        public List<string> GetChangedObjects()
        {
            return new List<string>(changedObjects);
        }
        #endregion

        #region 保存操作
        /// <summary>
        /// 执行全量保存操作
        /// </summary>
        /// <param name="saveSlot">存档槽位名称</param>
        /// <returns>保存操作的协程</returns>
        public Coroutine SaveAll(string saveSlot)
        {
            return StartCoroutine(SaveAllCoroutine(saveSlot, false));
        }

        /// <summary>
        /// 保存游戏 - SaveAll方法的别名，用于兼容性
        /// </summary>
        /// <param name="saveSlot">存档槽位名称</param>
        /// <returns>保存操作的协程</returns>
        public Coroutine SaveGame(string saveSlot)
        {
            return SaveAll(saveSlot);
        }

        /// <summary>
        /// 执行增量保存操作（只保存变化的对象）
        /// </summary>
        /// <param name="saveSlot">存档槽位名称</param>
        /// <returns>保存操作的协程</returns>
        public Coroutine SaveIncremental(string saveSlot)
        {
            return StartCoroutine(SaveAllCoroutine(saveSlot, true));
        }

        /// <summary>
        /// 保存操作协程
        /// </summary>
        private IEnumerator SaveAllCoroutine(string saveSlot, bool incrementalOnly)
        {
            if (isSaving)
            {
                LogWarning("保存操作正在进行中，跳过此次保存");
                yield break;
            }

            isSaving = true;
            bool success = true;

            LogMessage($"开始{(incrementalOnly ? "增量" : "全量")}保存: {saveSlot}");
            OnBeforeSave?.Invoke(saveSlot);

            // 获取要保存的对象列表
            List<string> objectsToSave;
            if (incrementalOnly)
            {
                objectsToSave = GetChangedObjects();
                if (objectsToSave.Count == 0)
                {
                    LogMessage("没有对象发生变化，跳过增量保存");
                    OnSaveComplete?.Invoke(saveSlot, true);
                    isSaving = false;
                    yield break;
                }
            }
            else
            {
                objectsToSave = GetSaveOrder();
            }

            // 创建保存数据容器
            var saveData = new SaveGameData
            {
                saveVersion = "1.0.0",
                gameVersion = Application.version,
                timestamp = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                saveableObjects = new Dictionary<string, object>()
            };

            // 逐个保存对象
            for (int i = 0; i < objectsToSave.Count; i++)
            {
                string objectId = objectsToSave[i];

                if (registeredObjects.TryGetValue(objectId, out ISaveable saveable))
                {
                    // 获取对象保存数据 - 使用SerializeToJson方法
                    var objectData = saveable.SerializeToJson();
                    if (!string.IsNullOrEmpty(objectData))
                    {
                        saveData.saveableObjects[objectId] = objectData;

                        // 清除变化标记
                        ClearObjectChanged(objectId);

                        LogMessage($"已保存对象: {objectId}");
                    }
                    else
                    {
                        LogError($"保存对象失败: {objectId}, 序列化返回空数据");
                        success = false;
                    }
                }

                // 报告进度
                float progress = (float)(i + 1) / objectsToSave.Count;
                OnSaveProgress?.Invoke(progress, objectId);

                // 每处理几个对象后让出一帧
                if (i % 5 == 0)
                {
                    yield return null;
                }
            }

            // 序列化并写入文件
            if (success)
            {
                string serializedData = serializer.SerializeToJson(saveData);
                if (!string.IsNullOrEmpty(serializedData))
                {
                    success = fileManager.WriteSaveFile(saveSlot, serializedData);
                }
                else
                {
                    LogError("序列化保存数据失败");
                    success = false;
                }
            }

            LogMessage($"保存操作完成: {saveSlot}, 成功: {success}");
            isSaving = false;
            OnSaveComplete?.Invoke(saveSlot, success);

            yield return null;
        }
        #endregion

        #region 加载操作
        /// <summary>
        /// 加载指定存档
        /// </summary>
        /// <param name="saveSlot">存档槽位名称</param>
        /// <returns>加载操作的协程</returns>
        public Coroutine LoadSave(string saveSlot)
        {
            return StartCoroutine(LoadSaveCoroutine(saveSlot));
        }

        /// <summary>
        /// 加载操作协程
        /// </summary>
        private IEnumerator LoadSaveCoroutine(string saveSlot)
        {
            if (isLoading)
            {
                LogWarning("加载操作正在进行中，跳过此次加载");
                yield break;
            }

            isLoading = true;
            bool success = true;

            LogMessage($"开始加载存档: {saveSlot}");
            OnBeforeLoad?.Invoke(saveSlot);

            // 读取存档文件
            string saveFileContent = fileManager.ReadSaveFile(saveSlot);
            if (string.IsNullOrEmpty(saveFileContent))
            {
                LogError($"无法读取存档文件: {saveSlot}");
                success = false;
                isLoading = false;
                OnLoadComplete?.Invoke(saveSlot, success);
                yield break;
            }

            // 反序列化存档数据
            SaveGameData saveData = serializer.DeserializeFromJson<SaveGameData>(saveFileContent);
            if (saveData == null)
            {
                LogError($"存档数据反序列化失败: {saveSlot}");
                success = false;
                isLoading = false;
                OnLoadComplete?.Invoke(saveSlot, success);
                yield break;
            }

            LogMessage($"存档版本: {saveData.saveVersion}, 游戏版本: {saveData.gameVersion}");

            // 获取加载顺序（与保存顺序相反）
            var loadOrder = GetSaveOrder();
            loadOrder.Reverse();

            // 逐个加载对象
            int processedCount = 0;
            foreach (string objectId in loadOrder)
            {
                if (saveData.saveableObjects.ContainsKey(objectId) &&
                    registeredObjects.TryGetValue(objectId, out ISaveable saveable))
                {
                    // 加载对象数据 - 使用DeserializeFromJson方法
                    var objectData = saveData.saveableObjects[objectId];
                    if (objectData is string jsonData)
                    {
                        if (!string.IsNullOrEmpty(jsonData))
                        {
                            // 安全地反序列化数据
                            if (!TryDeserializeObject(saveable, jsonData, objectId))
                            {
                                success = false;
                            }
                        }
                        else
                        {
                            LogError($"对象数据为空: {objectId}");
                            success = false;
                        }
                    }
                    else
                    {
                        LogError($"对象数据格式错误: {objectId}");
                        success = false;
                    }
                }

                processedCount++;

                // 报告进度
                float progress = (float)processedCount / loadOrder.Count;
                OnLoadProgress?.Invoke(progress, objectId);

                // 每处理几个对象后让出一帧
                if (processedCount % 5 == 0)
                {
                    yield return null;
                }
            }

            // 清除所有变化标记
            changedObjects.Clear();

            LogMessage($"加载操作完成: {saveSlot}, 成功: {success}");

            // 完成加载操作
            isLoading = false;
            OnLoadComplete?.Invoke(saveSlot, success);
        }

        /// <summary>
        /// 安全地反序列化对象数据
        /// </summary>
        private bool TryDeserializeObject(ISaveable saveable, string jsonData, string objectId)
        {
            try
            {
                saveable.DeserializeFromJson(jsonData);
                LogMessage($"已加载对象: {objectId}");
                return true;
            }
            catch (Exception ex)
            {
                LogError($"加载对象失败: {objectId}, 错误: {ex.Message}");
                return false;
            }
        }
        #endregion

        #region 自动保存
        /// <summary>
        /// 启动自动保存
        /// </summary>
        public void StartAutoSave()
        {
            if (autoSaveCoroutine != null)
            {
                StopCoroutine(autoSaveCoroutine);
            }

            autoSaveCoroutine = StartCoroutine(AutoSaveCoroutine());
            LogMessage($"自动保存已启动，间隔: {autoSaveInterval}秒");
        }

        /// <summary>
        /// 停止自动保存
        /// </summary>
        public void StopAutoSave()
        {
            if (autoSaveCoroutine != null)
            {
                StopCoroutine(autoSaveCoroutine);
                autoSaveCoroutine = null;
                LogMessage("自动保存已停止");
            }
        }

        /// <summary>
        /// 自动保存协程
        /// </summary>
        private IEnumerator AutoSaveCoroutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(autoSaveInterval);

                // 只有在有变化时才执行自动保存
                if (changedObjects.Count > 0 && !isSaving && !isLoading)
                {
                    LogMessage("执行自动保存");
                    yield return SaveIncremental("AutoSave");
                }
            }
        }
        #endregion

        #region 数据收集和应用
        /// <summary>
        /// 收集所有ISaveable对象的保存数据
        /// </summary>
        /// <returns>保存游戏数据</returns>
        private SaveGameData CollectSaveData()
        {
            var saveGameData = new SaveGameData
            {
                saveVersion = "1.0",
                gameVersion = Application.version,
                timestamp = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                saveableObjects = new Dictionary<string, object>()
            };

            // 按依赖顺序收集数据
            var sortedObjects = GetSortedSaveableObjects();

            foreach (var saveable in sortedObjects)
            {
                try
                {
                    var saveData = saveable.SerializeToJson();
                    if (saveData != null)
                    {
                        saveGameData.saveableObjects[saveable.GetSaveID()] = saveData;
                        LogMessage($"收集保存数据: {saveable.GetSaveID()}");
                    }
                    else
                    {
                        LogWarning($"对象返回空保存数据: {saveable.GetSaveID()}");
                    }
                }
                catch (System.Exception ex)
                {
                    LogError($"收集保存数据失败: {saveable.GetSaveID()}, 错误: {ex.Message}");
                }
            }

            LogMessage($"数据收集完成，共{saveGameData.saveableObjects.Count}个对象");
            return saveGameData;
        }

        /// <summary>
        /// 应用保存数据到ISaveable对象
        /// </summary>
        /// <param name="saveGameData">保存游戏数据</param>
        /// <returns>是否应用成功</returns>
        private bool ApplySaveData(SaveGameData saveGameData)
        {
            if (saveGameData?.saveableObjects == null)
            {
                LogError("保存数据为空或无效");
                return false;
            }

            int successCount = 0;
            int totalCount = 0;

            // 按依赖顺序应用数据
            var sortedObjects = GetSortedSaveableObjects();

            foreach (var saveable in sortedObjects)
            {
                totalCount++;
                try
                {
                    string saveId = saveable.GetSaveID();
                    if (saveGameData.saveableObjects.ContainsKey(saveId))
                    {
                        var saveData = saveGameData.saveableObjects[saveId];
                        if (saveData is string jsonData)
                        {
                            saveable.DeserializeFromJson(jsonData);
                        }
                        else
                        {
                            LogError($"保存数据格式错误: {saveId}");
                            continue;
                        }
                        successCount++;

                        LogMessage($"应用保存数据: {saveId}");
                    }
                    else
                    {
                        LogWarning($"未找到保存数据: {saveId}");
                    }
                }
                catch (System.Exception ex)
                {
                    LogError($"应用保存数据失败: {saveable.GetSaveID()}, 错误: {ex.Message}");
                }
            }

            LogMessage($"数据应用完成: {successCount}/{totalCount} 成功");
            return successCount > 0; // 只要有一个成功就认为应用成功
        }

        /// <summary>
        /// 获取按依赖关系排序的ISaveable对象列表
        /// </summary>
        /// <returns>排序后的对象列表</returns>
        private List<ISaveable> GetSortedSaveableObjects()
        {
            // 使用拓扑排序确保依赖关系正确
            var sorted = new List<ISaveable>();
            var visited = new HashSet<ISaveable>();
            var visiting = new HashSet<ISaveable>();

            foreach (var obj in registeredObjects.Values)
            {
                if (!visited.Contains(obj))
                {
                    TopologicalSort(obj, visited, visiting, sorted);
                }
            }

            LogMessage($"对象排序完成，共{sorted.Count}个对象");
            return sorted;
        }

        /// <summary>
        /// 拓扑排序辅助方法
        /// </summary>
        private void TopologicalSort(ISaveable obj, HashSet<ISaveable> visited, HashSet<ISaveable> visiting, List<ISaveable> sorted)
        {
            if (visiting.Contains(obj))
            {
                LogWarning($"检测到循环依赖: {obj.GetSaveID()}");
                return;
            }

            if (visited.Contains(obj))
            {
                return;
            }

            visiting.Add(obj);

            // 处理依赖关系
            // 基础规则：先保存/加载基础组件，再保存/加载依赖组件
            // 1. ItemDataHolder (基础数据)
            // 2. BaseItemGrid (网格系统)
            // 3. BaseItemSpawn (生成器)
            // 4. 其他组件

            visiting.Remove(obj);
            visited.Add(obj);
            sorted.Add(obj);
        }

        /// <summary>
        /// 自动发现场景中的ISaveable对象
        /// </summary>
        public void AutoDiscoverSaveableObjects()
        {
            var saveableObjects = FindObjectsOfType<MonoBehaviour>().OfType<ISaveable>();

            int discoveredCount = 0;
            foreach (var saveable in saveableObjects)
            {
                if (RegisterSaveable(saveable))
                {
                    discoveredCount++;
                }
            }

            LogMessage($"自动发现完成，新注册{discoveredCount}个对象");
        }
        #endregion

        #region 实用方法
        /// <summary>
        /// 获取保存系统状态信息
        /// </summary>
        /// <returns>状态信息字符串</returns>
        public string GetSystemStatus()
        {
            return $"注册对象数: {registeredObjects.Count}, " +
                   $"变化对象数: {changedObjects.Count}, " +
                   $"正在保存: {isSaving}, " +
                   $"正在加载: {isLoading}, " +
                   $"自动保存: {autoSaveEnabled}";
        }

        /// <summary>
        /// 验证存档文件
        /// </summary>
        /// <param name="saveSlot">存档槽位名称</param>
        /// <returns>验证是否成功</returns>
        public bool ValidateSave(string saveSlot)
        {
            try
            {
                string saveContent = fileManager.ReadSaveFile(saveSlot);
                if (string.IsNullOrEmpty(saveContent))
                {
                    return false;
                }

                SaveGameData saveData = serializer.DeserializeFromJson<SaveGameData>(saveContent);
                return saveData != null && !string.IsNullOrEmpty(saveData.saveVersion);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 获取存档信息
        /// </summary>
        /// <param name="saveSlot">存档槽位名称</param>
        /// <returns>存档信息，失败返回null</returns>
        public SaveGameInfo GetSaveInfo(string saveSlot)
        {
            try
            {
                string saveContent = fileManager.ReadSaveFile(saveSlot);
                if (string.IsNullOrEmpty(saveContent))
                {
                    return null;
                }

                SaveGameData saveData = serializer.DeserializeFromJson<SaveGameData>(saveContent);
                if (saveData == null)
                {
                    return null;
                }

                return new SaveGameInfo
                {
                    saveSlot = saveSlot,
                    saveVersion = saveData.saveVersion,
                    gameVersion = saveData.gameVersion,
                    timestamp = saveData.timestamp,
                    objectCount = saveData.saveableObjects?.Count ?? 0
                };
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 删除存档
        /// </summary>
        /// <param name="saveSlot">存档槽位名称</param>
        /// <returns>删除是否成功</returns>
        public bool DeleteSave(string saveSlot)
        {
            try
            {
                return fileManager.DeleteSaveFile(saveSlot);
            }
            catch (Exception ex)
            {
                LogError($"删除存档失败: {saveSlot}, 错误: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 检查对象是否已注册
        /// </summary>
        public bool IsObjectRegistered(ISaveable saveableObject)
        {
            if (saveableObject == null) return false;
            return registeredObjects.ContainsKey(saveableObject.GetSaveID());
        }

        /// <summary>
        /// 检查是否存在指定ID的保存数据
        /// </summary>
        /// <param name="saveId">保存ID</param>
        /// <returns>是否存在保存数据</returns>
        public bool HasSaveData(string saveId)
        {
            if (string.IsNullOrEmpty(saveId))
            {
                return false;
            }

            // 检查当前内存中是否有该对象的数据
            if (registeredObjects.ContainsKey(saveId))
            {
                return true;
            }

            // 尝试从最近的保存文件中查找数据
            try
            {
                var saveFileManager = GetComponent<SaveFileManager>();
                if (saveFileManager != null)
                {
                    var saveGameData = saveFileManager.LoadSaveData("autosave");
                    if (saveGameData?.saveableObjects != null)
                    {
                        return saveGameData.saveableObjects.ContainsKey(saveId);
                    }
                }
            }
            catch (System.Exception ex)
            {
                LogError($"检查保存数据时发生错误: {ex.Message}");
            }

            return false;
        }

        /// <summary>
        /// 加载指定ID的保存数据
        /// </summary>
        /// <param name="saveId">保存ID</param>
        /// <returns>保存数据的JSON字符串，如果不存在则返回null</returns>
        public string LoadSaveData(string saveId)
        {
            if (string.IsNullOrEmpty(saveId))
            {
                return null;
            }

            try
            {
                var saveFileManager = GetComponent<SaveFileManager>();
                if (saveFileManager != null)
                {
                    var saveGameData = saveFileManager.LoadSaveData("autosave");
                    if (saveGameData?.saveableObjects != null && saveGameData.saveableObjects.ContainsKey(saveId))
                    {
                        var data = saveGameData.saveableObjects[saveId];
                        if (data is string jsonData)
                        {
                            LogMessage($"成功加载保存数据: {saveId}");
                            return jsonData;
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                LogError($"加载保存数据时发生错误: {saveId}, 错误: {ex.Message}");
            }

            LogWarning($"未找到保存数据: {saveId}");
            return null;
        }

        /// <summary>
        /// 获取已注册对象数量
        /// </summary>
        public int RegisteredObjectCount => registeredObjects.Count;

        /// <summary>
        /// 设置自动保存启用状态
        /// </summary>
        public void SetAutoSaveEnabled(bool enabled)
        {
            autoSaveEnabled = enabled;
            if (enabled)
            {
                StartAutoSave();
            }
            else
            {
                StopAutoSave();
            }
            LogMessage($"自动保存已{(enabled ? "启用" : "禁用")}");
        }

        /// <summary>
        /// 设置自动保存间隔
        /// </summary>
        public void SetAutoSaveInterval(float interval)
        {
            if (interval < 30f)
            {
                LogWarning("自动保存间隔不能小于30秒，已设置为30秒");
                interval = 30f;
            }

            autoSaveInterval = interval;

            // 如果自动保存正在运行，重新启动以应用新间隔
            if (autoSaveEnabled && autoSaveCoroutine != null)
            {
                StartAutoSave();
            }

            LogMessage($"自动保存间隔已设置为: {interval}秒");
        }

        /// <summary>
        /// 获取自动保存状态
        /// </summary>
        public bool IsAutoSaveEnabled => autoSaveEnabled;

        /// <summary>
        /// 获取自动保存间隔
        /// </summary>
        public float AutoSaveInterval => autoSaveInterval;

        /// <summary>
        /// 快速保存 - 使用默认存档槽位
        /// </summary>
        public void QuickSave()
        {
            SaveAll("quicksave");
            LogMessage("快速保存已执行");
        }

        /// <summary>
        /// 快速加载 - 使用默认存档槽位
        /// </summary>
        public void QuickLoad()
        {
            LoadSave("quicksave");
            LogMessage("快速加载已执行");
        }

        /// <summary>
        /// 记录日志消息
        /// </summary>
        private void LogMessage(string message)
        {
            if (enableLogging)
            {
                Debug.Log($"[SaveManager] {message}");
            }
        }

        /// <summary>
        /// 记录警告消息
        /// </summary>
        private void LogWarning(string message)
        {
            if (enableLogging)
            {
                Debug.LogWarning($"[SaveManager] {message}");
            }
        }

        /// <summary>
        /// 记录错误消息
        /// </summary>
        private void LogError(string message)
        {
            if (enableLogging)
            {
                Debug.LogError($"[SaveManager] {message}");
            }
        }
        #endregion
    }

    /// <summary>
    /// 存档游戏数据结构
    /// </summary>
    [Serializable]
    public class SaveGameData
    {
        public string saveVersion;           // 存档版本
        public string gameVersion;           // 游戏版本
        public string timestamp;             // 时间戳
        public Dictionary<string, object> saveableObjects; // 可保存对象数据
    }

    /// <summary>
    /// 存档信息结构
    /// </summary>
    [Serializable]
    public class SaveGameInfo
    {
        public string saveSlot;              // 存档槽位
        public string saveVersion;          // 存档版本
        public string gameVersion;          // 游戏版本
        public string timestamp;            // 时间戳
        public int objectCount;             // 对象数量
    }
}