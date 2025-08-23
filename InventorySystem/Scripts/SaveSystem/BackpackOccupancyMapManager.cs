using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Collections;
using InventorySystem.SaveSystem;

/// <summary>
/// 背包占用图谱管理器 - 统一管理不同背包的占用图谱存储
/// 提供集中化的图谱缓存、预加载和优化功能
/// </summary>
public class BackpackOccupancyMapManager : MonoBehaviour
{
    [Header("管理器配置")]
    [SerializeField] private bool enableDebugInfo = true;                    // 启用调试信息
    [SerializeField] private int maxCachedMaps = 10;                        // 最大缓存图谱数量
    [SerializeField] private float mapCacheTimeout = 300f;                  // 图谱缓存超时时间（秒）
    [SerializeField] private bool enableAutoCleanup = true;                 // 启用自动清理
    [SerializeField] private float cleanupInterval = 60f;                   // 清理间隔（秒）

    [Header("性能优化")]
    [SerializeField] private bool enableAsyncOperations = true;             // 启用异步操作
    [SerializeField] private int maxAsyncOperationsPerFrame = 3;            // 每帧最大异步操作数
    [SerializeField] private bool enableMapCompression = false;             // 启用图谱压缩（实验性功能）

    // 单例实例
    private static BackpackOccupancyMapManager _instance;
    public static BackpackOccupancyMapManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<BackpackOccupancyMapManager>();
                if (_instance == null)
                {
                    GameObject managerObject = new GameObject("BackpackOccupancyMapManager");
                    _instance = managerObject.AddComponent<BackpackOccupancyMapManager>();
                    DontDestroyOnLoad(managerObject);
                }
            }
            return _instance;
        }
    }

    // 图谱缓存数据结构
    [System.Serializable]
    public class CachedOccupancyMap
    {
        public string backpackID;                                           // 背包唯一标识
        public BackpackItemGrid.GridOccupancyMapData mapData;              // 占用图谱数据
        public float lastAccessTime;                                       // 最后访问时间
        public int accessCount;                                            // 访问次数
        public bool isDirty;                                               // 是否需要保存
        public Vector2Int gridSize;                                        // 网格尺寸
        public int itemCount;                                              // 物品数量
        public float occupancyRate;                                        // 占用率

        public CachedOccupancyMap(string id, BackpackItemGrid.GridOccupancyMapData data)
        {
            backpackID = id;
            mapData = data;
            lastAccessTime = Time.time;
            accessCount = 1;
            isDirty = false;

            if (data != null)
            {
                gridSize = new Vector2Int(data.gridWidth, data.gridHeight);
                itemCount = data.occupiedCellsList?.Length ?? 0;
                occupancyRate = data.occupancyPercentage;
            }
        }

        public void UpdateAccess()
        {
            lastAccessTime = Time.time;
            accessCount++;
        }
    }

    // 缓存存储
    private Dictionary<string, CachedOccupancyMap> mapCache = new Dictionary<string, CachedOccupancyMap>();
    private Queue<string> accessOrder = new Queue<string>();               // LRU访问顺序
    private HashSet<string> pendingOperations = new HashSet<string>();     // 待处理操作
    private Coroutine cleanupCoroutine;                                    // 清理协程
    private Coroutine asyncProcessorCoroutine;                             // 异步处理协程

    // 统计信息
    [System.Serializable]
    public class ManagerStatistics
    {
        public int totalCachedMaps = 0;                                    // 总缓存图谱数
        public int cacheHits = 0;                                          // 缓存命中次数
        public int cacheMisses = 0;                                        // 缓存未命中次数
        public int mapsEvicted = 0;                                        // 被驱逐的图谱数
        public float averageAccessTime = 0f;                              // 平均访问时间
        public long totalMemoryUsage = 0;                                 // 总内存使用量（估算）
    }

    [SerializeField] private ManagerStatistics statistics = new ManagerStatistics();
    public ManagerStatistics Statistics => statistics;

    #region Unity生命周期

    private void Awake()
    {
        // 确保单例
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeManager();
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        StartManagerServices();
    }

    private void OnDestroy()
    {
        StopManagerServices();

        if (_instance == this)
        {
            _instance = null;
        }
    }

    #endregion

    #region 管理器初始化

    /// <summary>
    /// 初始化管理器
    /// </summary>
    private void InitializeManager()
    {
        if (enableDebugInfo)
        {
            Debug.Log("背包占用图谱管理器初始化开始");
        }

        // 初始化缓存
        mapCache.Clear();
        accessOrder.Clear();
        pendingOperations.Clear();

        // 重置统计信息
        statistics = new ManagerStatistics();

        if (enableDebugInfo)
        {
            Debug.Log($"背包占用图谱管理器初始化完成 - 最大缓存: {maxCachedMaps}, 异步操作: {enableAsyncOperations}");
        }
    }

    /// <summary>
    /// 启动管理器服务
    /// </summary>
    private void StartManagerServices()
    {
        // 启动自动清理
        if (enableAutoCleanup)
        {
            cleanupCoroutine = StartCoroutine(AutoCleanupCoroutine());
        }

        // 启动异步处理器
        if (enableAsyncOperations)
        {
            asyncProcessorCoroutine = StartCoroutine(AsyncProcessorCoroutine());
        }

        if (enableDebugInfo)
        {
            Debug.Log("背包占用图谱管理器服务已启动");
        }
    }

    /// <summary>
    /// 停止管理器服务
    /// </summary>
    private void StopManagerServices()
    {
        if (cleanupCoroutine != null)
        {
            StopCoroutine(cleanupCoroutine);
            cleanupCoroutine = null;
        }

        if (asyncProcessorCoroutine != null)
        {
            StopCoroutine(asyncProcessorCoroutine);
            asyncProcessorCoroutine = null;
        }

        // 保存所有脏数据
        SaveAllDirtyMaps();

        if (enableDebugInfo)
        {
            Debug.Log("背包占用图谱管理器服务已停止");
        }
    }

    #endregion

    #region 核心图谱管理功能

    /// <summary>
    /// 获取背包占用图谱
    /// </summary>
    /// <param name="backpackID">背包唯一标识</param>
    /// <returns>占用图谱数据</returns>
    public BackpackItemGrid.GridOccupancyMapData GetOccupancyMap(string backpackID)
    {
        if (string.IsNullOrEmpty(backpackID))
        {
            if (enableDebugInfo)
            {
                Debug.LogWarning("背包ID为空，无法获取占用图谱");
            }
            return null;
        }

        // 检查缓存
        if (mapCache.TryGetValue(backpackID, out CachedOccupancyMap cachedMap))
        {
            cachedMap.UpdateAccess();
            statistics.cacheHits++;

            if (enableDebugInfo)
            {
                Debug.Log($"从缓存获取背包占用图谱: {backpackID}");
            }

            return cachedMap.mapData;
        }

        // 缓存未命中，尝试从保存系统加载
        statistics.cacheMisses++;
        var mapData = LoadOccupancyMapFromSaveSystem(backpackID);

        if (mapData != null)
        {
            // 添加到缓存
            CacheOccupancyMap(backpackID, mapData);

            if (enableDebugInfo)
            {
                Debug.Log($"从保存系统加载并缓存背包占用图谱: {backpackID}");
            }
        }
        else if (enableDebugInfo)
        {
            Debug.LogWarning($"无法找到背包占用图谱: {backpackID}");
        }

        return mapData;
    }

    /// <summary>
    /// 保存背包占用图谱
    /// </summary>
    /// <param name="backpackID">背包唯一标识</param>
    /// <param name="mapData">占用图谱数据</param>
    /// <param name="immediate">是否立即保存</param>
    public void SaveOccupancyMap(string backpackID, BackpackItemGrid.GridOccupancyMapData mapData, bool immediate = false)
    {
        if (string.IsNullOrEmpty(backpackID) || mapData == null)
        {
            if (enableDebugInfo)
            {
                Debug.LogWarning("背包ID或图谱数据为空，无法保存");
            }
            return;
        }

        // 更新缓存
        CacheOccupancyMap(backpackID, mapData, true);

        if (immediate)
        {
            // 立即保存到保存系统
            SaveOccupancyMapToSaveSystem(backpackID, mapData);

            if (mapCache.TryGetValue(backpackID, out CachedOccupancyMap cachedMap))
            {
                cachedMap.isDirty = false;
            }
        }
        else
        {
            // 标记为脏数据，稍后批量保存
            if (mapCache.TryGetValue(backpackID, out CachedOccupancyMap cachedMap))
            {
                cachedMap.isDirty = true;
            }
        }

        if (enableDebugInfo)
        {
            Debug.Log($"背包占用图谱已{(immediate ? "立即保存" : "标记为待保存")}: {backpackID}");
        }
    }

    /// <summary>
    /// 移除背包占用图谱
    /// </summary>
    /// <param name="backpackID">背包唯一标识</param>
    public void RemoveOccupancyMap(string backpackID)
    {
        if (string.IsNullOrEmpty(backpackID)) return;

        // 从缓存中移除
        if (mapCache.Remove(backpackID))
        {
            statistics.totalCachedMaps--;

            if (enableDebugInfo)
            {
                Debug.Log($"从缓存移除背包占用图谱: {backpackID}");
            }
        }

        // 从保存系统中移除
        RemoveOccupancyMapFromSaveSystem(backpackID);
    }

    /// <summary>
    /// 预加载背包占用图谱
    /// </summary>
    /// <param name="backpackIDs">背包ID列表</param>
    public void PreloadOccupancyMaps(List<string> backpackIDs)
    {
        if (backpackIDs == null || backpackIDs.Count == 0) return;

        if (enableAsyncOperations)
        {
            StartCoroutine(PreloadOccupancyMapsAsync(backpackIDs));
        }
        else
        {
            foreach (string backpackID in backpackIDs)
            {
                GetOccupancyMap(backpackID); // 触发加载和缓存
            }
        }

        if (enableDebugInfo)
        {
            Debug.Log($"开始预加载 {backpackIDs.Count} 个背包占用图谱");
        }
    }

    #endregion

    #region 缓存管理

    /// <summary>
    /// 缓存占用图谱
    /// </summary>
    /// <param name="backpackID">背包ID</param>
    /// <param name="mapData">图谱数据</param>
    /// <param name="markDirty">是否标记为脏数据</param>
    private void CacheOccupancyMap(string backpackID, BackpackItemGrid.GridOccupancyMapData mapData, bool markDirty = false)
    {
        // 检查是否需要驱逐旧缓存
        if (mapCache.Count >= maxCachedMaps && !mapCache.ContainsKey(backpackID))
        {
            EvictLeastRecentlyUsedMap();
        }

        // 添加或更新缓存
        if (mapCache.TryGetValue(backpackID, out CachedOccupancyMap existingMap))
        {
            existingMap.mapData = mapData;
            existingMap.UpdateAccess();
            if (markDirty) existingMap.isDirty = true;
        }
        else
        {
            var newCachedMap = new CachedOccupancyMap(backpackID, mapData);
            if (markDirty) newCachedMap.isDirty = true;

            mapCache[backpackID] = newCachedMap;
            accessOrder.Enqueue(backpackID);
            statistics.totalCachedMaps++;
        }

        UpdateStatistics();
    }

    /// <summary>
    /// 驱逐最近最少使用的图谱
    /// </summary>
    private void EvictLeastRecentlyUsedMap()
    {
        if (accessOrder.Count == 0) return;

        string oldestID = accessOrder.Dequeue();

        if (mapCache.TryGetValue(oldestID, out CachedOccupancyMap mapToEvict))
        {
            // 如果是脏数据，先保存
            if (mapToEvict.isDirty)
            {
                SaveOccupancyMapToSaveSystem(oldestID, mapToEvict.mapData);
            }

            mapCache.Remove(oldestID);
            statistics.mapsEvicted++;
            statistics.totalCachedMaps--;

            if (enableDebugInfo)
            {
                Debug.Log($"驱逐缓存的背包占用图谱: {oldestID}");
            }
        }
    }

    /// <summary>
    /// 清理过期缓存
    /// </summary>
    private void CleanupExpiredCache()
    {
        float currentTime = Time.time;
        var expiredKeys = new List<string>();

        foreach (var kvp in mapCache)
        {
            if (currentTime - kvp.Value.lastAccessTime > mapCacheTimeout)
            {
                expiredKeys.Add(kvp.Key);
            }
        }

        foreach (string key in expiredKeys)
        {
            if (mapCache.TryGetValue(key, out CachedOccupancyMap expiredMap))
            {
                // 保存脏数据
                if (expiredMap.isDirty)
                {
                    SaveOccupancyMapToSaveSystem(key, expiredMap.mapData);
                }

                mapCache.Remove(key);
                statistics.totalCachedMaps--;
            }
        }

        if (expiredKeys.Count > 0 && enableDebugInfo)
        {
            Debug.Log($"清理了 {expiredKeys.Count} 个过期的背包占用图谱缓存");
        }
    }

    #endregion

    #region 保存系统集成

    /// <summary>
    /// 从保存系统加载占用图谱
    /// </summary>
    /// <param name="backpackID">背包ID</param>
    /// <returns>占用图谱数据</returns>
    private BackpackItemGrid.GridOccupancyMapData LoadOccupancyMapFromSaveSystem(string backpackID)
    {
        try
        {
            var saveManager = SaveManager.Instance;
            if (saveManager == null) return null;

            var fileManager = saveManager.GetComponent<SaveFileManager>();
            if (fileManager == null) return null;

            string fileName = $"BackpackOccupancyMap_{backpackID}";

            if (fileManager.SaveFileExists(fileName))
            {
                string jsonData = fileManager.ReadSaveFile(fileName);
                if (!string.IsNullOrEmpty(jsonData))
                {
                    return JsonUtility.FromJson<BackpackItemGrid.GridOccupancyMapData>(jsonData);
                }
            }
        }
        catch (System.Exception ex)
        {
            if (enableDebugInfo)
            {
                Debug.LogError($"从保存系统加载背包占用图谱失败: {ex.Message}");
            }
        }

        return null;
    }

    /// <summary>
    /// 保存占用图谱到保存系统
    /// </summary>
    /// <param name="backpackID">背包ID</param>
    /// <param name="mapData">图谱数据</param>
    private void SaveOccupancyMapToSaveSystem(string backpackID, BackpackItemGrid.GridOccupancyMapData mapData)
    {
        try
        {
            var saveManager = SaveManager.Instance;
            if (saveManager == null) return;

            var fileManager = saveManager.GetComponent<SaveFileManager>();
            if (fileManager == null) return;

            string fileName = $"BackpackOccupancyMap_{backpackID}";
            string jsonData = JsonUtility.ToJson(mapData);

            fileManager.WriteSaveFile(fileName, jsonData);
        }
        catch (System.Exception ex)
        {
            if (enableDebugInfo)
            {
                Debug.LogError($"保存背包占用图谱到保存系统失败: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 从保存系统移除占用图谱
    /// </summary>
    /// <param name="backpackID">背包ID</param>
    private void RemoveOccupancyMapFromSaveSystem(string backpackID)
    {
        try
        {
            var saveManager = SaveManager.Instance;
            if (saveManager == null) return;

            var fileManager = saveManager.GetComponent<SaveFileManager>();
            if (fileManager == null) return;

            string fileName = $"BackpackOccupancyMap_{backpackID}";
            fileManager.DeleteSaveFile(fileName);
        }
        catch (System.Exception ex)
        {
            if (enableDebugInfo)
            {
                Debug.LogError($"从保存系统移除背包占用图谱失败: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 保存所有脏数据
    /// </summary>
    private void SaveAllDirtyMaps()
    {
        int savedCount = 0;

        foreach (var kvp in mapCache)
        {
            if (kvp.Value.isDirty)
            {
                SaveOccupancyMapToSaveSystem(kvp.Key, kvp.Value.mapData);
                kvp.Value.isDirty = false;
                savedCount++;
            }
        }

        if (savedCount > 0 && enableDebugInfo)
        {
            Debug.Log($"批量保存了 {savedCount} 个脏的背包占用图谱");
        }
    }

    #endregion

    #region 异步操作

    /// <summary>
    /// 异步预加载占用图谱
    /// </summary>
    /// <param name="backpackIDs">背包ID列表</param>
    /// <returns>协程</returns>
    private IEnumerator PreloadOccupancyMapsAsync(List<string> backpackIDs)
    {
        int processedCount = 0;

        foreach (string backpackID in backpackIDs)
        {
            if (!mapCache.ContainsKey(backpackID))
            {
                GetOccupancyMap(backpackID);
                processedCount++;

                // 每处理几个后等待一帧
                if (processedCount % maxAsyncOperationsPerFrame == 0)
                {
                    yield return null;
                }
            }
        }

        if (enableDebugInfo)
        {
            Debug.Log($"异步预加载完成: {processedCount} 个背包占用图谱");
        }
    }

    /// <summary>
    /// 自动清理协程
    /// </summary>
    /// <returns>协程</returns>
    private IEnumerator AutoCleanupCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(cleanupInterval);

            CleanupExpiredCache();
            SaveAllDirtyMaps();
            UpdateStatistics();
        }
    }

    /// <summary>
    /// 异步处理器协程
    /// </summary>
    /// <returns>协程</returns>
    private IEnumerator AsyncProcessorCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f); // 每秒检查一次

            // 处理待处理的操作
            if (pendingOperations.Count > 0)
            {
                var operations = pendingOperations.Take(maxAsyncOperationsPerFrame).ToList();

                foreach (string operation in operations)
                {
                    pendingOperations.Remove(operation);
                    // 这里可以添加具体的异步操作处理逻辑
                }

                if (operations.Count > 0)
                {
                    yield return null; // 等待一帧
                }
            }
        }
    }

    #endregion

    #region 统计和调试

    /// <summary>
    /// 更新统计信息
    /// </summary>
    private void UpdateStatistics()
    {
        statistics.totalCachedMaps = mapCache.Count;

        if (mapCache.Count > 0)
        {
            float totalAccessTime = mapCache.Values.Sum(m => m.lastAccessTime);
            statistics.averageAccessTime = totalAccessTime / mapCache.Count;

            // 估算内存使用量（简化计算）
            statistics.totalMemoryUsage = mapCache.Count * 1024; // 假设每个图谱占用1KB
        }
    }

    /// <summary>
    /// 获取缓存信息
    /// </summary>
    /// <returns>缓存信息字符串</returns>
    public string GetCacheInfo()
    {
        return $"缓存图谱数: {statistics.totalCachedMaps}/{maxCachedMaps}, " +
               $"命中率: {(statistics.cacheHits + statistics.cacheMisses > 0 ? (float)statistics.cacheHits / (statistics.cacheHits + statistics.cacheMisses) * 100f : 0f):F1}%, " +
               $"驱逐数: {statistics.mapsEvicted}, " +
               $"内存使用: {statistics.totalMemoryUsage / 1024f:F1}KB";
    }

    /// <summary>
    /// 清空所有缓存
    /// </summary>
    public void ClearAllCache()
    {
        SaveAllDirtyMaps();
        mapCache.Clear();
        accessOrder.Clear();
        statistics.totalCachedMaps = 0;

        if (enableDebugInfo)
        {
            Debug.Log("已清空所有背包占用图谱缓存");
        }
    }

    #endregion

    #region 公共接口

    /// <summary>
    /// 检查背包占用图谱是否存在
    /// </summary>
    /// <param name="backpackID">背包ID</param>
    /// <returns>是否存在</returns>
    public bool HasOccupancyMap(string backpackID)
    {
        if (string.IsNullOrEmpty(backpackID)) return false;

        // 检查缓存
        if (mapCache.ContainsKey(backpackID)) return true;

        // 检查保存系统
        var saveManager = SaveManager.Instance;
        if (saveManager != null)
        {
            string saveKey = $"BackpackOccupancyMap_{backpackID}";
            return saveManager.HasSaveData(saveKey);
        }

        return false;
    }

    /// <summary>
    /// 获取所有缓存的背包ID
    /// </summary>
    /// <returns>背包ID列表</returns>
    public List<string> GetCachedBackpackIDs()
    {
        return new List<string>(mapCache.Keys);
    }

    /// <summary>
    /// 强制保存指定背包的占用图谱
    /// </summary>
    /// <param name="backpackID">背包ID</param>
    public void ForceSaveOccupancyMap(string backpackID)
    {
        if (mapCache.TryGetValue(backpackID, out CachedOccupancyMap cachedMap))
        {
            SaveOccupancyMapToSaveSystem(backpackID, cachedMap.mapData);
            cachedMap.isDirty = false;

            if (enableDebugInfo)
            {
                Debug.Log($"强制保存背包占用图谱: {backpackID}");
            }
        }
    }

    #endregion
}