using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using InventorySystem;
using System;

/// <summary>
/// 物品数据监控器 - 用于实时监控所有物品的数据变化
/// 提供统计信息、数据变化追踪和调试功能
/// </summary>
public class ItemDataMonitor : MonoBehaviour
{
    [Header("监控器设置")]
    [FieldLabel("启用监控")] public bool enableMonitoring = true;
    [FieldLabel("自动刷新间隔(秒)")] public float refreshInterval = 1.0f;
    [FieldLabel("最大历史记录数")] public int maxHistoryCount = 100;
    [FieldLabel("显示调试信息")] public bool showDebugInfo = false;

    [Header("监控统计")]
    [FieldLabel("当前物品总数")] public int totalItemCount;
    [FieldLabel("活跃物品数")] public int activeItemCount;
    [FieldLabel("网格中物品数")] public int itemsInGridCount;
    [FieldLabel("装备栏物品数")] public int equippedItemCount;

    // 单例模式
    private static ItemDataMonitor instance;
    public static ItemDataMonitor Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<ItemDataMonitor>();
                if (instance == null)
                {
                    GameObject go = new GameObject("ItemDataMonitor");
                    instance = go.AddComponent<ItemDataMonitor>();
                }
            }
            return instance;
        }
    }

    // 监控数据结构
    [System.Serializable]
    public class ItemMonitorData
    {
        // 基础标识信息
        [FieldLabel("全局唯一ID")] public long globalUniqueId;
        [FieldLabel("物品ID")] public int itemId;
        [FieldLabel("物品名称")] public string itemName;
        [FieldLabel("物品简称")] public string shortName;
        [FieldLabel("物品类别")] public ItemCategory category;
        [FieldLabel("稀有度")] public string rarity;
        
        // 尺寸和外观信息
        [FieldLabel("物品宽度")] public int itemWidth;
        [FieldLabel("物品高度")] public int itemHeight;
        [FieldLabel("背景颜色")] public Color backgroundColor;
        [FieldLabel("物品图标名称")] public string iconName;
        
        // 网格位置信息
        [FieldLabel("网格位置")] public Vector2Int gridPosition;
        [FieldLabel("所在网格名称")] public string gridName;
        [FieldLabel("是否在网格中")] public bool isInGrid;
        [FieldLabel("是否已装备")] public bool isEquipped;
        [FieldLabel("是否旋转")] public bool isRotated;
        
        // 运行时数据
        [FieldLabel("当前堆叠数量")] public int currentStack;
        [FieldLabel("最大堆叠数量")] public int maxStack;
        [FieldLabel("当前耐久度")] public int currentDurability;
        [FieldLabel("最大耐久度")] public int maxDurability;
        [FieldLabel("当前使用次数")] public int currentUsageCount;
        [FieldLabel("最大使用次数")] public int maxUsageCount;
        [FieldLabel("当前治疗量")] public int currentHealAmount;
        [FieldLabel("最大治疗量")] public int maxHealAmount;
        [FieldLabel("单次治疗量")] public int healPerUse;
        [FieldLabel("恢复饱食度")] public int hungerRestore;
        [FieldLabel("恢复精神值")] public int mentalRestore;
        [FieldLabel("情报值")] public int intelligenceValue;
        [FieldLabel("货币数量")] public int currencyAmount;
        
        // 装备属性
        [FieldLabel("弹药类型")] public string ammunitionType;
        
        // 容器属性
        [FieldLabel("容器水平格子数")] public int containerCellH;
        [FieldLabel("容器垂直格子数")] public int containerCellV;
        [FieldLabel("是否为容器")] public bool isContainer;
        
        // 物品状态
        [FieldLabel("是否可堆叠")] public bool isStackable;
        [FieldLabel("是否为消耗品")] public bool isConsumable;
        [FieldLabel("是否有耐久度")] public bool hasDurability;
        
        // 系统信息
        [FieldLabel("GameObject实例ID")] public int gameObjectInstanceId;
        [FieldLabel("最后更新时间")] public float lastUpdateTime;
        [FieldLabel("物品对象引用")] public GameObject itemObject;

        public ItemMonitorData(ItemDataReader reader, Item item)
        {
            if (reader?.ItemData != null)
            {
                // 基础标识信息
                globalUniqueId = reader.ItemData.GlobalId;
                itemId = reader.ItemData.id;
                itemName = reader.ItemData.itemName;
                shortName = reader.ItemData.shortName;
                category = reader.ItemData.category;
                rarity = reader.ItemData.rarity;
                
                // 尺寸和外观信息
                itemWidth = reader.ItemData.width;
                itemHeight = reader.ItemData.height;
                backgroundColor = reader.ItemData.backgroundColor;
                iconName = reader.ItemData.itemIcon?.name ?? "无图标";
                
                // 运行时数据
                currentStack = reader.CurrentStack;
                maxStack = reader.ItemData.maxStack;
                currentDurability = reader.CurrentDurability;
                maxDurability = reader.ItemData.durability;
                currentUsageCount = reader.CurrentUsageCount;
                maxUsageCount = reader.ItemData.usageCount;
                currentHealAmount = reader.currentHealAmount;
                maxHealAmount = reader.ItemData.maxHealAmount;
                healPerUse = reader.ItemData.healPerUse;
                hungerRestore = reader.ItemData.hungerRestore;
                mentalRestore = reader.ItemData.mentalRestore;
                intelligenceValue = reader.ItemData.intelligenceValue;
                currencyAmount = reader.currencyAmount;
                
                // 装备属性
                ammunitionType = reader.ItemData.ammunitionType;
                
                // 容器属性
                containerCellH = reader.ItemData.cellH;
                containerCellV = reader.ItemData.cellV;
                isContainer = reader.ItemData.IsContainer();
                
                // 物品状态
                isStackable = reader.ItemData.IsStackable();
                isConsumable = reader.ItemData.IsConsumable();
                hasDurability = reader.ItemData.HasDurability();
            }

            if (item != null)
            {
                // 网格位置信息
                gridPosition = item.OnGridPosition;
                gridName = item.OnGridReference?.name ?? "无";
                isInGrid = item.IsOnGrid();
                isRotated = item.IsRotated();
                itemObject = item.gameObject;
                gameObjectInstanceId = item.gameObject.GetInstanceID();
            }

            lastUpdateTime = Time.time;
            isEquipped = false; // 需要通过装备管理器检查
        }
    }

    // 数据变化历史记录
    [System.Serializable]
    public class ItemChangeRecord
    {
        public float timestamp;
        public string itemName;
        public string changeType;
        public string oldValue;
        public string newValue;
        public string description;

        public ItemChangeRecord(string name, string type, string old, string newVal, string desc = "")
        {
            timestamp = Time.time;
            itemName = name;
            changeType = type;
            oldValue = old;
            newValue = newVal;
            description = desc;
        }
    }

    // 监控数据存储
    private Dictionary<GameObject, ItemMonitorData> monitoredItems = new Dictionary<GameObject, ItemMonitorData>();
    private List<ItemChangeRecord> changeHistory = new List<ItemChangeRecord>();
    private Dictionary<ItemCategory, int> categoryStats = new Dictionary<ItemCategory, int>();

    // 事件系统
    public static event System.Action<ItemMonitorData> OnItemDataChanged;
    public static event System.Action<ItemChangeRecord> OnItemChanged;
    public static event System.Action<Dictionary<ItemCategory, int>> OnCategoryStatsUpdated;

    private float lastRefreshTime;

    private void Awake()
    {
        // 确保单例
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // 初始化分类统计
        InitializeCategoryStats();
    }

    private void Start()
    {
        // 开始监控
        if (enableMonitoring)
        {
            StartMonitoring();
        }
    }

    private void Update()
    {
        if (!enableMonitoring) return;

        // 定时刷新监控数据
        if (Time.time - lastRefreshTime >= refreshInterval)
        {
            RefreshMonitoringData();
            lastRefreshTime = Time.time;
        }
    }

    /// <summary>
    /// 开始监控
    /// </summary>
    public void StartMonitoring()
    {
        enableMonitoring = true;
        RefreshMonitoringData();
        Debug.Log("ItemDataMonitor: 开始监控物品数据");
    }

    /// <summary>
    /// 停止监控
    /// </summary>
    public void StopMonitoring()
    {
        enableMonitoring = false;
        Debug.Log("ItemDataMonitor: 停止监控物品数据");
    }

    /// <summary>
    /// 刷新监控数据
    /// </summary>
    public void RefreshMonitoringData()
    {
        if (!enableMonitoring) return;

        // 查找所有物品
        ItemDataReader[] allReaders = FindObjectsOfType<ItemDataReader>();
        Item[] allItems = FindObjectsOfType<Item>();

        // 创建物品字典以便快速查找
        Dictionary<GameObject, Item> itemDict = new Dictionary<GameObject, Item>();
        foreach (Item item in allItems)
        {
            itemDict[item.gameObject] = item;
        }

        // 清理已销毁的物品
        List<GameObject> toRemove = new List<GameObject>();
        foreach (var kvp in monitoredItems)
        {
            if (kvp.Key == null)
            {
                toRemove.Add(kvp.Key);
            }
        }
        foreach (var key in toRemove)
        {
            monitoredItems.Remove(key);
        }

        // 更新或添加物品数据
        foreach (ItemDataReader reader in allReaders)
        {
            if (reader == null || reader.gameObject == null) continue;

            GameObject itemObj = reader.gameObject;
            Item item = itemDict.ContainsKey(itemObj) ? itemDict[itemObj] : null;

            // 检查是否是新物品或数据有变化
            if (monitoredItems.ContainsKey(itemObj))
            {
                UpdateExistingItem(reader, item);
            }
            else
            {
                AddNewItem(reader, item);
            }
        }

        // 更新统计信息
        UpdateStatistics();
        UpdateCategoryStats();

        // 触发事件
        OnCategoryStatsUpdated?.Invoke(categoryStats);
    }

    /// <summary>
    /// 添加新物品到监控
    /// </summary>
    private void AddNewItem(ItemDataReader reader, Item item)
    {
        ItemMonitorData data = new ItemMonitorData(reader, item);
        monitoredItems[reader.gameObject] = data;

        // 记录变化
        AddChangeRecord(data.itemName, "创建", "", "新物品", "物品被创建或加入监控");

        if (showDebugInfo)
        {
            Debug.Log($"ItemDataMonitor: 添加新物品监控 - {data.itemName}");
        }
    }

    /// <summary>
    /// 更新现有物品数据
    /// </summary>
    private void UpdateExistingItem(ItemDataReader reader, Item item)
    {
        GameObject itemObj = reader.gameObject;
        ItemMonitorData oldData = monitoredItems[itemObj];
        ItemMonitorData newData = new ItemMonitorData(reader, item);

        // 检查数据变化
        CheckForChanges(oldData, newData);

        // 更新数据
        monitoredItems[itemObj] = newData;

        // 触发事件
        OnItemDataChanged?.Invoke(newData);
    }

    /// <summary>
    /// 检查数据变化并记录
    /// </summary>
    private void CheckForChanges(ItemMonitorData oldData, ItemMonitorData newData)
    {
        // 检查堆叠数量变化
        if (oldData.currentStack != newData.currentStack)
        {
            AddChangeRecord(newData.itemName, "堆叠数量", 
                oldData.currentStack.ToString(), 
                newData.currentStack.ToString(),
                "物品堆叠数量发生变化");
        }

        // 检查耐久度变化
        if (oldData.currentDurability != newData.currentDurability)
        {
            AddChangeRecord(newData.itemName, "耐久度", 
                oldData.currentDurability.ToString(), 
                newData.currentDurability.ToString(),
                "物品耐久度发生变化");
        }

        // 检查使用次数变化
        if (oldData.currentUsageCount != newData.currentUsageCount)
        {
            AddChangeRecord(newData.itemName, "使用次数", 
                oldData.currentUsageCount.ToString(), 
                newData.currentUsageCount.ToString(),
                "物品使用次数发生变化");
        }

        // 检查位置变化
        if (oldData.gridPosition != newData.gridPosition)
        {
            AddChangeRecord(newData.itemName, "位置", 
                $"{oldData.gridPosition} ({oldData.gridName})", 
                $"{newData.gridPosition} ({newData.gridName})",
                "物品在网格中的位置发生变化");
        }

        // 检查网格变化
        if (oldData.gridName != newData.gridName)
        {
            AddChangeRecord(newData.itemName, "网格", 
                oldData.gridName, 
                newData.gridName,
                "物品所在网格发生变化");
        }
    }

    /// <summary>
    /// 添加变化记录
    /// </summary>
    private void AddChangeRecord(string itemName, string changeType, string oldValue, string newValue, string description = "")
    {
        ItemChangeRecord record = new ItemChangeRecord(itemName, changeType, oldValue, newValue, description);
        changeHistory.Add(record);

        // 限制历史记录数量
        if (changeHistory.Count > maxHistoryCount)
        {
            changeHistory.RemoveAt(0);
        }

        // 触发事件
        OnItemChanged?.Invoke(record);

        if (showDebugInfo)
        {
            Debug.Log($"ItemDataMonitor: {itemName} - {changeType}: {oldValue} → {newValue}");
        }
    }

    /// <summary>
    /// 更新统计信息
    /// </summary>
    private void UpdateStatistics()
    {
        totalItemCount = monitoredItems.Count;
        activeItemCount = monitoredItems.Values.Count(data => data.itemObject != null && data.itemObject.activeInHierarchy);
        itemsInGridCount = monitoredItems.Values.Count(data => data.isInGrid);
        equippedItemCount = monitoredItems.Values.Count(data => data.isEquipped);
    }

    /// <summary>
    /// 初始化分类统计
    /// </summary>
    private void InitializeCategoryStats()
    {
        categoryStats.Clear();
        foreach (ItemCategory category in System.Enum.GetValues(typeof(ItemCategory)))
        {
            categoryStats[category] = 0;
        }
    }

    /// <summary>
    /// 更新分类统计
    /// </summary>
    private void UpdateCategoryStats()
    {
        InitializeCategoryStats();
        
        foreach (var data in monitoredItems.Values)
        {
            if (data.itemObject != null && data.itemObject.activeInHierarchy)
            {
                categoryStats[data.category]++;
            }
        }
    }

    /// <summary>
    /// 获取所有监控数据
    /// </summary>
    public Dictionary<GameObject, ItemMonitorData> GetAllMonitoredItems()
    {
        return new Dictionary<GameObject, ItemMonitorData>(monitoredItems);
    }

    /// <summary>
    /// 获取变化历史记录
    /// </summary>
    public List<ItemChangeRecord> GetChangeHistory()
    {
        return new List<ItemChangeRecord>(changeHistory);
    }

    /// <summary>
    /// 获取分类统计
    /// </summary>
    public Dictionary<ItemCategory, int> GetCategoryStats()
    {
        return new Dictionary<ItemCategory, int>(categoryStats);
    }

    /// <summary>
    /// 根据分类获取物品
    /// </summary>
    public List<ItemMonitorData> GetItemsByCategory(ItemCategory category)
    {
        return monitoredItems.Values.Where(data => data.category == category).ToList();
    }

    /// <summary>
    /// 搜索物品
    /// </summary>
    public List<ItemMonitorData> SearchItems(string searchTerm)
    {
        if (string.IsNullOrEmpty(searchTerm))
            return monitoredItems.Values.ToList();

        return monitoredItems.Values.Where(data => 
            data.itemName.ToLower().Contains(searchTerm.ToLower()) ||
            data.itemId.ToString().Contains(searchTerm)
        ).ToList();
    }

    /// <summary>
    /// 清除历史记录
    /// </summary>
    public void ClearHistory()
    {
        changeHistory.Clear();
        Debug.Log("ItemDataMonitor: 历史记录已清除");
    }

    /// <summary>
    /// 导出监控数据为JSON
    /// </summary>
    public string ExportDataToJson()
    {
        var exportData = new
        {
            timestamp = System.DateTime.Now.ToString(),
            totalItems = totalItemCount,
            activeItems = activeItemCount,
            categoryStats = categoryStats,
            items = monitoredItems.Values.Select(data => new
            {
                name = data.itemName,
                id = data.itemId,
                category = data.category.ToString(),
                position = data.gridPosition.ToString(),
                grid = data.gridName,
                stack = data.currentStack,
                durability = data.currentDurability,
                usageCount = data.currentUsageCount,
                isEquipped = data.isEquipped,
                isInGrid = data.isInGrid
            }).ToArray(),
            recentChanges = changeHistory.TakeLast(20).Select(record => new
            {
                time = System.DateTime.FromBinary((long)record.timestamp).ToString(),
                item = record.itemName,
                type = record.changeType,
                oldValue = record.oldValue,
                newValue = record.newValue,
                description = record.description
            }).ToArray()
        };

        return JsonUtility.ToJson(exportData, true);
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    // 在Inspector中显示实时信息
    private void OnValidate()
    {
        if (Application.isPlaying && enableMonitoring)
        {
            // 在编辑器中实时更新显示
        }
    }
}