using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 物品数据结构
/// 用于跨场景ID管理的物品信息
/// </summary>
[System.Serializable]
public class ItemData
{
    public string itemID;           // 物品ID
    public string itemName;         // 物品名称
    public ItemType itemType;       // 物品类型
    public Vector2Int gridPosition; // 网格位置
    public Vector2Int size;         // 物品尺寸
    
    public ItemData()
    {
        itemID = "";
        itemName = "";
        itemType = ItemType.Unknown;
        gridPosition = Vector2Int.zero;
        size = Vector2Int.one;
    }
    
    public ItemData(string id, string name, ItemType type)
    {
        itemID = id;
        itemName = name;
        itemType = type;
        gridPosition = Vector2Int.zero;
        size = Vector2Int.one;
    }
}

/// <summary>
/// 物品类型枚举
/// </summary>
public enum ItemType
{
    Unknown,        // 未知类型
    Weapon,         // 武器
    Armor,          // 护甲
    Consumable,     // 消耗品
    Material,       // 材料
    Tool,           // 工具
    Container,      // 容器
    Ammunition,     // 弹药
    Equipment       // 装备
}

/// <summary>
/// 跨场景ID管理器
/// 负责管理物品在场景切换时的ID一致性和引用完整性
/// </summary>
public class CrossSceneIDManager : MonoBehaviour
{
    private static CrossSceneIDManager instance;

    /// <summary>
    /// 单例实例
    /// </summary>
    public static CrossSceneIDManager Instance
    {
        get
        {
            if (instance == null)
            {
                // 查找现有实例
                instance = FindObjectOfType<CrossSceneIDManager>();

                if (instance == null)
                {
                    // 创建新实例
                    GameObject go = new GameObject("CrossSceneIDManager");
                    instance = go.AddComponent<CrossSceneIDManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return instance;
        }
    }

    [Header("ID同步设置")]
    [SerializeField] private bool enableDebugLogging = true;
    [SerializeField] private bool autoSyncOnSceneLoad = true;
    [SerializeField] private float syncDelayAfterSceneLoad = 0.5f;

    // ID映射表：旧ID -> 新ID
    private Dictionary<string, string> idMappingTable = new Dictionary<string, string>();

    // 物品引用表：物品ID -> 引用该物品的对象列表
    private Dictionary<string, List<IItemReference>> itemReferences = new Dictionary<string, List<IItemReference>>();

    // 待同步的物品列表
    private List<ItemData> pendingSyncItems = new List<ItemData>();

    // 场景切换状态
    private bool isSceneTransitioning = false;
    private string previousSceneName = "";
    private string currentSceneName = "";

    // 统计信息
    private int conflictResolutions = 0;
    private List<float> syncTimes = new List<float>();
    private DateTime lastSyncTime = DateTime.MinValue;

    // 事件定义
    public event System.Action<string, string> OnIDMapped; // 旧ID, 新ID
    public event System.Action<string> OnItemSynced; // 物品ID
    public event System.Action<int> OnSyncCompleted; // 同步的物品数量
    public event System.Action<string, string> OnSyncError; // 错误信息, 物品ID

    private void Awake()
    {
        // 确保只有一个实例
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        // 注册场景管理事件
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;

        // 初始化当前场景名称
        if (SceneManager.GetActiveScene().isLoaded)
        {
            currentSceneName = SceneManager.GetActiveScene().name;
        }

        if (enableDebugLogging)
        {
            Debug.Log($"[CrossSceneIDManager] 初始化完成，当前场景: {currentSceneName}");
        }
    }

    private void OnDestroy()
    {
        // 注销事件
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
    }

    /// <summary>
    /// 场景加载完成事件处理
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        previousSceneName = currentSceneName;
        currentSceneName = scene.name;
        isSceneTransitioning = false;

        if (enableDebugLogging)
        {
            Debug.Log($"[CrossSceneIDManager] 场景加载完成: {currentSceneName} (从 {previousSceneName})");
        }

        if (autoSyncOnSceneLoad)
        {
            // 延迟执行同步，确保所有对象都已初始化
            Invoke(nameof(PerformAutoSync), syncDelayAfterSceneLoad);
        }
    }

    /// <summary>
    /// 场景卸载事件处理
    /// </summary>
    private void OnSceneUnloaded(Scene scene)
    {
        isSceneTransitioning = true;

        if (enableDebugLogging)
        {
            Debug.Log($"[CrossSceneIDManager] 场景卸载: {scene.name}");
        }

        // 清理该场景的引用
        CleanupSceneReferences(scene.name);
    }

    /// <summary>
    /// 执行自动同步
    /// </summary>
    private void PerformAutoSync()
    {
        if (pendingSyncItems.Count > 0)
        {
            SyncPendingItems();
        }
        else
        {
            // 扫描当前场景中的所有物品，检查是否需要同步
            ScanAndSyncSceneItems();
        }
    }

    /// <summary>
    /// 注册物品引用
    /// </summary>
    /// <param name="itemID">物品ID</param>
    /// <param name="reference">引用该物品的对象</param>
    public void RegisterItemReference(string itemID, IItemReference reference)
    {
        if (string.IsNullOrEmpty(itemID) || reference == null)
            return;

        if (!itemReferences.ContainsKey(itemID))
        {
            itemReferences[itemID] = new List<IItemReference>();
        }

        if (!itemReferences[itemID].Contains(reference))
        {
            itemReferences[itemID].Add(reference);

            if (enableDebugLogging)
            {
                Debug.Log($"[CrossSceneIDManager] 注册物品引用: {itemID} -> {reference.GetType().Name}");
            }
        }
    }

    /// <summary>
    /// 注销物品引用
    /// </summary>
    /// <param name="itemID">物品ID</param>
    /// <param name="reference">引用该物品的对象</param>
    public void UnregisterItemReference(string itemID, IItemReference reference)
    {
        if (string.IsNullOrEmpty(itemID) || reference == null)
            return;

        if (itemReferences.ContainsKey(itemID))
        {
            itemReferences[itemID].Remove(reference);

            // 如果没有引用了，移除该条目
            if (itemReferences[itemID].Count == 0)
            {
                itemReferences.Remove(itemID);
            }

            if (enableDebugLogging)
            {
                Debug.Log($"[CrossSceneIDManager] 注销物品引用: {itemID} -> {reference.GetType().Name}");
            }
        }
    }

    /// <summary>
    /// 添加待同步物品
    /// </summary>
    /// <param name="item">物品数据</param>
    public void AddPendingSyncItem(ItemData item)
    {
        if (item == null || string.IsNullOrEmpty(item.itemID))
            return;

        // 检查是否已存在
        if (!pendingSyncItems.Any(i => i.itemID == item.itemID))
        {
            pendingSyncItems.Add(item);

            if (enableDebugLogging)
            {
                Debug.Log($"[CrossSceneIDManager] 添加待同步物品: {item.itemID} ({item.itemName})");
            }
        }
    }

    /// <summary>
    /// 移除待同步物品
    /// </summary>
    /// <param name="itemID">物品ID</param>
    public void RemovePendingSyncItem(string itemID)
    {
        pendingSyncItems.RemoveAll(i => i.itemID == itemID);

        if (enableDebugLogging)
        {
            Debug.Log($"[CrossSceneIDManager] 移除待同步物品: {itemID}");
        }
    }

    /// <summary>
    /// 同步待处理的物品
    /// </summary>
    public void SyncPendingItems()
    {
        if (pendingSyncItems.Count == 0)
        {
            if (enableDebugLogging)
            {
                Debug.Log("[CrossSceneIDManager] 没有待同步的物品");
            }
            return;
        }

        int syncedCount = 0;
        var itemsToRemove = new List<ItemData>();

        foreach (var item in pendingSyncItems)
        {
            try
            {
                if (SyncSingleItem(item))
                {
                    syncedCount++;
                    itemsToRemove.Add(item);
                    OnItemSynced?.Invoke(item.itemID);
                }
            }
            catch (Exception ex)
            {
                string errorMsg = $"同步物品失败: {ex.Message}";
                OnSyncError?.Invoke(errorMsg, item.itemID);

                if (enableDebugLogging)
                {
                    Debug.LogError($"[CrossSceneIDManager] {errorMsg} (物品ID: {item.itemID})");
                }
            }
        }

        // 移除已同步的物品
        foreach (var item in itemsToRemove)
        {
            pendingSyncItems.Remove(item);
        }

        OnSyncCompleted?.Invoke(syncedCount);

        if (enableDebugLogging)
        {
            Debug.Log($"[CrossSceneIDManager] 同步完成，处理了 {syncedCount} 个物品");
        }
    }

    /// <summary>
    /// 同步单个物品
    /// </summary>
    /// <param name="item">物品数据</param>
    /// <returns>是否同步成功</returns>
    private bool SyncSingleItem(ItemData item)
    {
        // 检查当前场景中是否存在相同的物品
        var existingItems = FindObjectsOfType<MonoBehaviour>()
            .OfType<IItemContainer>()
            .SelectMany(container => container.GetAllItems())
            .Where(existingItem => existingItem.itemName == item.itemName &&
                                   existingItem.itemType == item.itemType &&
                                   existingItem.itemID != item.itemID)
            .ToList();

        if (existingItems.Count > 0)
        {
            // 找到重复物品，需要进行ID映射
            var targetItem = existingItems.First();
            string oldID = item.itemID;
            string newID = targetItem.itemID;

            // 更新ID映射表
            idMappingTable[oldID] = newID;

            // 更新所有引用
            UpdateItemReferences(oldID, newID);

            OnIDMapped?.Invoke(oldID, newID);

            if (enableDebugLogging)
            {
                Debug.Log($"[CrossSceneIDManager] ID映射: {oldID} -> {newID} ({item.itemName})");
            }

            return true;
        }

        // 没有找到重复物品，物品可以保持原ID
        return true;
    }

    /// <summary>
    /// 扫描并同步场景中的物品
    /// </summary>
    private void ScanAndSyncSceneItems()
    {
        var allContainers = FindObjectsOfType<MonoBehaviour>().OfType<IItemContainer>().ToList();
        var allItems = new List<ItemData>();

        foreach (var container in allContainers)
        {
            allItems.AddRange(container.GetAllItems());
        }

        // 检查ID冲突
        var duplicateGroups = allItems
            .GroupBy(item => item.itemID)
            .Where(group => group.Count() > 1)
            .ToList();

        foreach (var group in duplicateGroups)
        {
            ResolveDuplicateIDs(group.ToList());
        }

        if (enableDebugLogging)
        {
            Debug.Log($"[CrossSceneIDManager] 场景扫描完成，发现 {duplicateGroups.Count} 组重复ID");
        }
    }

    /// <summary>
    /// 解决重复ID问题
    /// </summary>
    /// <param name="duplicateItems">重复ID的物品列表</param>
    private void ResolveDuplicateIDs(List<ItemData> duplicateItems)
    {
        if (duplicateItems.Count <= 1)
            return;

        // 保留第一个物品的ID，为其他物品生成新ID
        var keepItem = duplicateItems.First();

        for (int i = 1; i < duplicateItems.Count; i++)
        {
            var item = duplicateItems[i];
            string oldID = item.itemID;
            string newID = GenerateUniqueID(item);

            // 更新物品ID
            item.itemID = newID;

            // 更新ID映射表
            idMappingTable[oldID] = newID;

            // 更新引用
            UpdateItemReferences(oldID, newID);

            OnIDMapped?.Invoke(oldID, newID);

            // 增加冲突解决计数
            conflictResolutions++;

            if (enableDebugLogging)
            {
                Debug.Log($"[CrossSceneIDManager] 解决ID冲突: {oldID} -> {newID} ({item.itemName})");
            }
        }
    }

    /// <summary>
    /// 生成唯一ID
    /// </summary>
    /// <param name="item">物品数据</param>
    /// <returns>唯一ID</returns>
    private string GenerateUniqueID(ItemData item)
    {
        string baseID = $"{item.itemName}_{item.itemType}_{currentSceneName}";
        string uniqueID = baseID;
        int counter = 1;

        // 确保ID唯一
        while (IsIDInUse(uniqueID))
        {
            uniqueID = $"{baseID}_{counter}";
            counter++;
        }

        return uniqueID;
    }

    /// <summary>
    /// 检查ID是否已被使用
    /// </summary>
    /// <param name="id">要检查的ID</param>
    /// <returns>是否已被使用</returns>
    private bool IsIDInUse(string id)
    {
        var allContainers = FindObjectsOfType<MonoBehaviour>().OfType<IItemContainer>().ToList();

        foreach (var container in allContainers)
        {
            if (container.GetAllItems().Any(item => item.itemID == id))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 更新物品引用
    /// </summary>
    /// <param name="oldID">旧ID</param>
    /// <param name="newID">新ID</param>
    private void UpdateItemReferences(string oldID, string newID)
    {
        if (itemReferences.ContainsKey(oldID))
        {
            var references = itemReferences[oldID].ToList();

            foreach (var reference in references)
            {
                try
                {
                    reference.UpdateItemID(oldID, newID);
                }
                catch (Exception ex)
                {
                    if (enableDebugLogging)
                    {
                        Debug.LogError($"[CrossSceneIDManager] 更新引用失败: {ex.Message} (引用类型: {reference.GetType().Name})");
                    }
                }
            }

            // 更新引用表
            itemReferences[newID] = references;
            itemReferences.Remove(oldID);
        }
    }

    /// <summary>
    /// 清理场景引用
    /// </summary>
    /// <param name="sceneName">场景名称</param>
    private void CleanupSceneReferences(string sceneName)
    {
        var keysToRemove = new List<string>();

        foreach (var kvp in itemReferences)
        {
            var validReferences = kvp.Value.Where(r => r != null && r as UnityEngine.Object != null).ToList();

            if (validReferences.Count != kvp.Value.Count)
            {
                if (validReferences.Count == 0)
                {
                    keysToRemove.Add(kvp.Key);
                }
                else
                {
                    itemReferences[kvp.Key] = validReferences;
                }
            }
        }

        foreach (var key in keysToRemove)
        {
            itemReferences.Remove(key);
        }

        if (enableDebugLogging && keysToRemove.Count > 0)
        {
            Debug.Log($"[CrossSceneIDManager] 清理了 {keysToRemove.Count} 个无效引用");
        }
    }

    /// <summary>
    /// 获取ID映射
    /// </summary>
    /// <param name="oldID">旧ID</param>
    /// <returns>新ID，如果没有映射则返回原ID</returns>
    public string GetMappedID(string oldID)
    {
        return idMappingTable.ContainsKey(oldID) ? idMappingTable[oldID] : oldID;
    }



    /// <summary>
    /// 获取同步统计信息
    /// </summary>
    public SyncStatistics GetSyncStatistics()
    {
        return new SyncStatistics
        {
            totalMappings = idMappingTable.Count,
            totalReferences = itemReferences.Count,
            pendingItems = pendingSyncItems.Count,
            conflictResolutions = conflictResolutions,
            averageSyncTime = syncTimes.Count > 0 ? syncTimes.Average() : 0f,
            lastSyncTime = lastSyncTime,
            currentScene = currentSceneName,
            previousScene = previousSceneName,
            isTransitioning = isSceneTransitioning
        };
    }

    /// <summary>
    /// 同步统计信息结构
    /// </summary>
    [System.Serializable]
    public struct SyncStatistics
    {
        public int totalMappings;           // 总映射数量
        public int totalReferences;         // 总引用数量
        public int pendingItems;            // 待同步物品数量
        public int conflictResolutions;     // 冲突解决次数
        public float averageSyncTime;       // 平均同步时间
        public DateTime lastSyncTime;       // 最后同步时间
        public string currentScene;         // 当前场景名称
        public string previousScene;        // 前一场景名称
        public bool isTransitioning;        // 是否正在场景切换
    }

    /// <summary>
    /// 清除所有映射和引用
    /// </summary>
    public void ClearAllMappings()
    {
        idMappingTable.Clear();
        itemReferences.Clear();
        pendingSyncItems.Clear();
        conflictResolutions = 0;
        syncTimes.Clear();
        lastSyncTime = DateTime.MinValue;

        if (enableDebugLogging)
        {
            Debug.Log("[CrossSceneIDManager] 已清除所有映射和引用");
        }
    }

    /// <summary>
    /// 获取所有ID映射
    /// </summary>
    /// <returns>ID映射字典的副本</returns>
    public Dictionary<string, string> GetAllIDMappings()
    {
        return new Dictionary<string, string>(idMappingTable);
    }

    /// <summary>
    /// 获取所有物品引用信息
    /// </summary>
    /// <returns>物品引用信息列表</returns>
    public List<ItemReferenceInfo> GetAllItemReferences()
    {
        var referenceInfos = new List<ItemReferenceInfo>();

        foreach (var kvp in itemReferences)
        {
            referenceInfos.Add(new ItemReferenceInfo
            {
                itemID = kvp.Key,
                referenceCount = kvp.Value.Count,
                referenceTypes = kvp.Value.Select(r => r.GetType().Name).ToList()
            });
        }

        return referenceInfos;
    }

    /// <summary>
    /// 物品引用信息结构
    /// </summary>
    [System.Serializable]
    public struct ItemReferenceInfo
    {
        public string itemID;               // 物品ID
        public int referenceCount;          // 引用数量
        public List<string> referenceTypes; // 引用类型列表
    }

    /// <summary>
    /// 获取待同步物品信息
    /// </summary>
    /// <returns>待同步物品的详细信息</returns>
    public List<PendingItemInfo> GetPendingItemsInfo()
    {
        var pendingInfo = new List<PendingItemInfo>();

        foreach (var item in pendingSyncItems)
        {
            pendingInfo.Add(new PendingItemInfo
            {
                itemID = item.itemID,
                itemName = item.itemName,
                itemType = item.itemType.ToString(),
                targetScene = currentSceneName
            });
        }

        return pendingInfo;
    }

    /// <summary>
    /// 待同步物品信息结构
    /// </summary>
    [System.Serializable]
    public struct PendingItemInfo
    {
        public string itemID;        // 物品ID
        public string itemName;      // 物品名称
        public string itemType;      // 物品类型
        public string targetScene;   // 目标场景
    }

    /// <summary>
    /// 强制扫描当前场景中的所有物品
    /// </summary>
    public void ScanCurrentScene()
    {
        var allContainers = FindObjectsOfType<MonoBehaviour>().OfType<IItemContainer>().ToList();
        int scannedCount = 0;

        foreach (var container in allContainers)
        {
            var items = container.GetAllItems();
            scannedCount += items.Count;
        }

        if (enableDebugLogging)
        {
            Debug.Log($"[CrossSceneIDManager] 场景扫描完成，发现 {scannedCount} 个物品");
        }
    }

    /// <summary>
    /// 获取映射详细信息
    /// </summary>
    /// <returns>映射详细信息列表</returns>
    public List<MappingInfo> GetMappingDetails()
    {
        var mappingDetails = new List<MappingInfo>();

        foreach (var mapping in idMappingTable)
        {
            mappingDetails.Add(new MappingInfo
            {
                oldID = mapping.Key,
                newID = mapping.Value,
                usageCount = GetMappingUsageCount(mapping.Key)
            });
        }

        return mappingDetails;
    }

    /// <summary>
    /// 映射信息结构
    /// </summary>
    [System.Serializable]
    public struct MappingInfo
    {
        public string oldID;         // 旧ID
        public string newID;         // 新ID
        public int usageCount;       // 使用次数
    }

    /// <summary>
    /// 获取映射使用次数
    /// </summary>
    private int GetMappingUsageCount(string oldID)
    {
        int count = 0;
        if (itemReferences.ContainsKey(oldID))
        {
            count = itemReferences[oldID].Count;
        }
        return count;
    }
}

/// <summary>
/// 物品引用接口
/// 实现此接口的类可以接收物品ID更新通知
/// </summary>
public interface IItemReference
{
    /// <summary>
    /// 更新物品ID
    /// </summary>
    /// <param name="oldID">旧ID</param>
    /// <param name="newID">新ID</param>
    void UpdateItemID(string oldID, string newID);
}

/// <summary>
/// 物品容器接口
/// 实现此接口的类可以提供其包含的所有物品
/// </summary>
public interface IItemContainer
{
    /// <summary>
    /// 获取所有物品
    /// </summary>
    /// <returns>物品列表</returns>
    List<ItemData> GetAllItems();
}