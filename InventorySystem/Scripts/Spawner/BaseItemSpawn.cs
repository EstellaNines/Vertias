using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 物品生成器基类 - 提供物品生成的核心功能
/// 可以被继承来实现不同类型的生成器
/// </summary>
public abstract class BaseItemSpawn : MonoBehaviour, ISaveable
{
    [Header("基础生成器设置")]
    [SerializeField] protected ItemGrid targetGrid; // 目标网格
    [SerializeField] protected Transform itemParent; // 物品父对象

    [Header("生成控制设置")]
    [Range(1, 50)]
    [SerializeField] protected int spawnCount = 5; // 生成数量滑条

    [Header("文件夹路径设置")]
    [SerializeField] protected string databasePath = "Assets/InventorySystem/Database/Scriptable Object数据对象";
    [SerializeField] protected string prefabPath = "Assets/InventorySystem/Prefab";

    [Header("调试信息设置")]
    [SerializeField] protected bool showDebugInfo = true;
    [SerializeField] protected bool showGridOccupancy = false; // 显示网格占用状态
    [SerializeField] protected bool autoLoadOnStart = true; // 启动时自动加载

    // 自动加载的物品数据和预制体
    protected Dictionary<string, InventorySystemItemDataSO> itemDataDict = new Dictionary<string, InventorySystemItemDataSO>();
    protected Dictionary<string, GameObject> itemPrefabDict = new Dictionary<string, GameObject>();
    protected List<InventorySystemItemDataSO> allItemData = new List<InventorySystemItemDataSO>();

    // 网格占用状态数组 (0=空闲, 1=占用)
    protected int[,] gridOccupancy;
    protected List<SpawnedItemInfo> spawnedItems = new List<SpawnedItemInfo>();

    // 实时检测相关变量
    protected float lastDetectionTime = 0f;
    protected float detectionInterval = 1f; // 每秒检测一次
    protected bool wasGridAvailable = false; // 上次检测时网格是否可用

    // 生成物品信息类
    [System.Serializable]
    public class SpawnedItemInfo
    {
        public GameObject itemObject;
        public Vector2Int gridPosition;
        public Vector2Int size;
        public InventorySystemItemDataSO itemData;
        public float spawnTime; // 生成时间
    }

    #region Unity生命周期方法

    protected virtual void Awake()
    {
        // 确保生成器在场景切换时不被销毁
        DontDestroyOnLoad(gameObject);
    }

    protected virtual void Start()
    {
        InitializeItemSpawner();

        if (Application.isPlaying)
        {
            InitializeSaveSystem();

            if (autoLoadOnStart)
            {
                InitializeItemSpawner();
            }
        }
    }

    protected virtual void Update()
    {
        // 实时动态检测InventoryGrid
        if (Time.time - lastDetectionTime >= detectionInterval)
        {
            RealTimeGridDetection();
            lastDetectionTime = Time.time;
        }

        // 检测网格尺寸变化并重新初始化
        if (targetGrid != null && gridOccupancy != null)
        {
            GridConfig config = targetGrid.GetGridConfig();
            if (config != null &&
                (config.inventoryWidth != gridOccupancy.GetLength(0) ||
                 config.inventoryHeight != gridOccupancy.GetLength(1)))
            {
                if (showDebugInfo)
                {
                    Debug.Log($"检测到网格尺寸变化，重新初始化: {config.inventoryWidth}x{config.inventoryHeight}");
                }
                InitializeGridWithConfig(config);
            }
        }
    }

    protected virtual void OnEnable()
    {
        // 当对象被激活时重新初始化，确保在新场景中能正常工作
        if (Application.isPlaying)
        {
            StartCoroutine(DelayedSceneInitialization());
        }
    }

    #endregion

    #region 抽象方法 - 子类必须实现

    /// <summary>
    /// 获取可用的物品列表 - 子类需要实现具体的筛选逻辑
    /// </summary>
    /// <returns>可用的物品数据列表</returns>
    protected abstract List<InventorySystemItemDataSO> GetAvailableItemsByCategory();

    /// <summary>
    /// 生成随机物品的主要方法 - 子类可以重写以实现特定逻辑
    /// </summary>
    public abstract void SpawnRandomItems();

    #endregion

    #region 虚方法 - 子类可以重写

    /// <summary>
    /// 根据珍贵程度选择物品 - 子类可以重写以实现不同的选择策略
    /// </summary>
    /// <param name="availableItems">可用物品列表</param>
    /// <returns>选中的物品数据</returns>
    protected virtual InventorySystemItemDataSO SelectItemByRarity(List<InventorySystemItemDataSO> availableItems)
    {
        if (availableItems == null || availableItems.Count == 0)
            return null;

        // 按类别分组
        var itemsByCategory = availableItems.GroupBy(item => item.itemCategory).ToList();

        // 随机选择一个类别
        var selectedCategory = itemsByCategory[UnityEngine.Random.Range(0, itemsByCategory.Count())];
        var categoryItems = selectedCategory.ToList();

        // 如果该类别只有一个物品，直接返回
        if (categoryItems.Count == 1)
            return categoryItems[0];

        // 计算该类别中每个物品的珍贵程度权重
        return SelectItemByRarityWeight(categoryItems);
    }

    /// <summary>
    /// 初始化生成器 - 子类可以重写以添加额外的初始化逻辑
    /// </summary>
    protected virtual void InitializeItemSpawner()
    {
        DetectInventoryGrid();

        if (itemParent == null)
        {
            itemParent = targetGrid?.transform;
        }

        // 延迟初始化，确保targetGrid完全初始化
        StartCoroutine(DelayedInitialization());

        if (autoLoadOnStart)
        {
            LoadAllItemsFromFolders();
        }
    }

    #endregion

    #region 核心功能方法

    /// <summary>
    /// 延迟场景初始化，等待场景完全加载
    /// </summary>
    protected IEnumerator DelayedSceneInitialization()
    {
        yield return new WaitForSeconds(0.1f);

        // 在新场景中强制重新初始化
        ForceReinitialize();

        if (showDebugInfo)
        {
            Debug.Log("场景切换后重新初始化完成");
        }
    }

    /// <summary>
    /// 实时网格检测方法
    /// </summary>
    protected void RealTimeGridDetection()
    {
        bool currentGridAvailable = (targetGrid != null && targetGrid.gameObject != null);

        // 如果网格状态发生变化
        if (currentGridAvailable != wasGridAvailable)
        {
            if (currentGridAvailable)
            {
                if (showDebugInfo)
                {
                    Debug.Log("检测到InventoryGrid出现，开始初始化...");
                }
                // 网格重新出现，重新初始化
                InitializeItemSpawner();
            }
            else
            {
                if (showDebugInfo)
                {
                    Debug.Log("检测到InventoryGrid消失");
                }
                // 网格消失，清理状态
                targetGrid = null;
                itemParent = null;
                gridOccupancy = null;
            }

            wasGridAvailable = currentGridAvailable;
        }
        else if (!currentGridAvailable)
        {
            // 如果网格不可用，尝试重新检测
            DetectInventoryGrid();

            // 检测后如果找到了网格，触发初始化
            if (targetGrid != null && !wasGridAvailable)
            {
                if (showDebugInfo)
                {
                    Debug.Log("实时检测发现新的InventoryGrid，开始初始化...");
                }
                InitializeItemSpawner();
                wasGridAvailable = true;
            }
        }
    }

    /// <summary>
    /// 检测场景中的InventoryGrid预制体
    /// </summary>
    [ContextMenu("重新检测InventoryGrid")]
    public void DetectInventoryGrid()
    {
        // 每次都重新查找，确保获取到最新的InventoryGrid
        targetGrid = null;

        // 首先尝试通过名称查找InventoryGrid预制体
        GameObject inventoryGridObj = GameObject.Find("InventoryGrid");
        if (inventoryGridObj != null)
        {
            targetGrid = inventoryGridObj.GetComponent<ItemGrid>();
            if (showDebugInfo)
            {
                Debug.Log($"通过名称找到InventoryGrid: {inventoryGridObj.name}");
            }
        }

        // 如果没有找到名为InventoryGrid的对象，则使用通用查找方法
        if (targetGrid == null)
        {
            targetGrid = FindObjectOfType<ItemGrid>();
            if (targetGrid != null && showDebugInfo)
            {
                Debug.Log($"通过类型找到ItemGrid: {targetGrid.gameObject.name}");
            }
        }

        if (targetGrid == null)
        {
            Debug.LogWarning("未找到InventoryGrid预制体！请确保场景中存在名为'InventoryGrid'的GameObject，并包含ItemGrid组件。");
        }
        else
        {
            // 更新itemParent引用
            itemParent = targetGrid.transform;
            if (showDebugInfo)
            {
                Debug.Log($"成功检测到InventoryGrid: {targetGrid.gameObject.name}");
            }
        }
    }

    /// <summary>
    /// 延迟初始化协程
    /// </summary>
    protected IEnumerator DelayedInitialization()
    {
        // 等待一帧，确保所有Start()方法执行完毕
        yield return null;

        // 优先从GridConfig获取尺寸，而不是依赖GetGridSize()
        if (targetGrid != null)
        {
            GridConfig config = targetGrid.GetGridConfig();
            if (config != null)
            {
                // 直接从GridConfig获取正确的尺寸
                InitializeGridWithConfig(config);
                if (showDebugInfo)
                {
                    Debug.Log($"从GridConfig获取网格尺寸: {config.inventoryWidth}x{config.inventoryHeight}");
                }
            }
            else
            {
                // 回退到原有方法
                Vector2Int gridSize = targetGrid.GetGridSize();
                if (gridSize.x > 0 && gridSize.y > 0)
                {
                    InitializeGrid();
                }
                else
                {
                    yield return new WaitForSeconds(0.1f);
                    InitializeGrid();
                }
            }
        }
    }

    /// <summary>
    /// 使用GridConfig初始化网格
    /// </summary>
    protected void InitializeGridWithConfig(GridConfig config)
    {
        if (config == null)
        {
            Debug.LogError("GridConfig为空，无法初始化网格！");
            return;
        }

        int gridWidth = config.inventoryWidth;
        int gridHeight = config.inventoryHeight;

        // 添加有效性检查
        if (gridWidth <= 0 || gridHeight <= 0)
        {
            Debug.LogError($"GridConfig中的网格尺寸无效: {gridWidth}x{gridHeight}");
            return;
        }

        gridOccupancy = new int[gridWidth, gridHeight];

        if (showDebugInfo)
        {
            Debug.Log($"使用GridConfig初始化网格尺寸: {gridWidth}x{gridHeight}");
        }
    }

    /// <summary>
    /// 修改原有的InitializeGrid方法作为备用
    /// </summary>
    protected void InitializeGrid()
    {
        if (targetGrid == null)
        {
            Debug.LogError("targetGrid为空，无法初始化网格！");
            return;
        }

        // 先尝试从GridConfig获取
        GridConfig config = targetGrid.GetGridConfig();
        if (config != null)
        {
            InitializeGridWithConfig(config);
            return;
        }

        // 如果没有GridConfig，使用GetGridSize()作为最后手段
        Vector2Int gridSize = targetGrid.GetGridSize();
        int gridWidth = gridSize.x;
        int gridHeight = gridSize.y;

        if (gridWidth <= 0 || gridHeight <= 0)
        {
            Debug.LogError($"获取到无效的网格尺寸: {gridWidth}x{gridHeight}");
            return;
        }

        gridOccupancy = new int[gridWidth, gridHeight];

        if (showDebugInfo)
        {
            Debug.Log($"使用GetGridSize()初始化网格尺寸: {gridWidth}x{gridHeight} (备用方案)");
        }
    }

    /// <summary>
    /// 从文件夹加载所有物品数据和预制体
    /// </summary>
    [ContextMenu("加载所有物品")]
    public void LoadAllItemsFromFolders()
    {
        LoadItemDataFromDatabase();
        LoadPrefabsFromFolder();

        if (showDebugInfo)
        {
            Debug.Log($"加载完成: {allItemData.Count} 个物品数据, {itemPrefabDict.Count} 个预制体");
        }
    }

    /// <summary>
    /// 从Database文件夹加载所有ScriptableObject数据
    /// </summary>
    protected void LoadItemDataFromDatabase()
    {
        itemDataDict.Clear();
        allItemData.Clear();

#if UNITY_EDITOR
        // 在编辑器中使用AssetDatabase
        string[] guids = AssetDatabase.FindAssets("t:InventorySystemItemDataSO", new[] { databasePath });

        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            InventorySystemItemDataSO itemData = AssetDatabase.LoadAssetAtPath<InventorySystemItemDataSO>(assetPath);

            if (itemData != null)
            {
                string key = GetItemKey(itemData.itemName);
                if (!itemDataDict.ContainsKey(key))
                {
                    itemDataDict[key] = itemData;
                    allItemData.Add(itemData);
                }
            }
        }
#else
        // 运行时使用Resources.LoadAll (需要将资源放在Resources文件夹中)
        InventorySystemItemDataSO[] loadedData = Resources.LoadAll<InventorySystemItemDataSO>("");

        foreach (var itemData in loadedData)
        {
            if (itemData != null)
            {
                string key = GetItemKey(itemData.itemName);
                if (!itemDataDict.ContainsKey(key))
                {
                    itemDataDict[key] = itemData;
                    allItemData.Add(itemData);
                }
            }
        }
#endif

        if (showDebugInfo)
        {
            Debug.Log($"从Database加载了 {allItemData.Count} 个物品数据");
        }
    }

    /// <summary>
    /// 从Prefab文件夹加载所有预制体
    /// </summary>
    protected void LoadPrefabsFromFolder()
    {
        itemPrefabDict.Clear();

#if UNITY_EDITOR
        // 在编辑器中使用AssetDatabase
        string[] guids = AssetDatabase.FindAssets("t:GameObject", new[] { prefabPath });

        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

            if (prefab != null)
            {
                string key = GetItemKey(prefab.name);
                if (!itemPrefabDict.ContainsKey(key))
                {
                    itemPrefabDict[key] = prefab;
                }
            }
        }
#else
        // 运行时使用Resources.LoadAll (需要将预制体放在Resources文件夹中)
        GameObject[] loadedPrefabs = Resources.LoadAll<GameObject>("");

        foreach (var prefab in loadedPrefabs)
        {
            if (prefab != null)
            {
                string key = GetItemKey(prefab.name);
                if (!itemPrefabDict.ContainsKey(key))
                {
                    itemPrefabDict[key] = prefab;
                }
            }
        }
#endif

        if (showDebugInfo)
        {
            Debug.Log($"从Prefab文件夹加载了 {itemPrefabDict.Count} 个预制体");
        }
    }

    #endregion

    #region 物品匹配和选择方法

    /// <summary>
    /// 生成物品键值（用于匹配数据和预制体）
    /// </summary>
    protected string GetItemKey(string name)
    {
        // 移除特殊字符和空格，转换为小写用于匹配
        string key = name.Replace(" ", "").Replace("(", "").Replace(")", "").Replace("-", "").Replace("_", "").ToLower();

        // 处理数字格式差异：将"5.56"转换为"556"，"7.62"转换为"762"等
        key = System.Text.RegularExpressions.Regex.Replace(key, @"(\d+)\.(\d+)", "$1$2");

        return key;
    }

    /// <summary>
    /// 根据物品数据查找对应的预制体
    /// </summary>
    protected GameObject FindPrefabForItemData(InventorySystemItemDataSO itemData)
    {
        string dataKey = GetItemKey(itemData.itemName);

        // 直接匹配
        if (itemPrefabDict.ContainsKey(dataKey))
        {
            return itemPrefabDict[dataKey];
        }

        // 模糊匹配
        foreach (var kvp in itemPrefabDict)
        {
            if (kvp.Key.Contains(dataKey) || dataKey.Contains(kvp.Key))
            {
                if (showDebugInfo)
                {
                    Debug.Log($"模糊匹配成功: '{itemData.itemName}' -> '{kvp.Value.name}'");
                }
                return kvp.Value;
            }
        }

        // 尝试更宽松的匹配：提取主要关键词
        string[] dataWords = ExtractKeywords(itemData.itemName);
        foreach (var kvp in itemPrefabDict)
        {
            string[] prefabWords = ExtractKeywords(kvp.Value.name);
            if (HasCommonKeywords(dataWords, prefabWords, 2)) // 至少2个关键词匹配
            {
                if (showDebugInfo)
                {
                    Debug.Log($"关键词匹配成功: '{itemData.itemName}' -> '{kvp.Value.name}'");
                }
                return kvp.Value;
            }
        }

        if (showDebugInfo)
        {
            Debug.LogWarning($"未找到物品 '{itemData.itemName}' 对应的预制体 (Key: '{dataKey}')");
        }

        return null;
    }

    /// <summary>
    /// 提取关键词
    /// </summary>
    protected string[] ExtractKeywords(string name)
    {
        // 移除常见的无意义词汇，提取主要关键词
        string[] commonWords = { "the", "a", "an", "of", "and", "or", "but", "in", "on", "at", "to", "for", "with" };

        string cleanName = name.ToLower();
        // 移除特殊字符但保留空格用于分词
        cleanName = System.Text.RegularExpressions.Regex.Replace(cleanName, @"[^a-z0-9\s]", " ");

        string[] words = cleanName.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        // 过滤掉常见词汇和长度小于2的词
        return words.Where(w => w.Length >= 2 && !commonWords.Contains(w)).ToArray();
    }

    /// <summary>
    /// 检查是否有足够的共同关键词
    /// </summary>
    protected bool HasCommonKeywords(string[] words1, string[] words2, int minMatches)
    {
        int matches = 0;
        foreach (string word1 in words1)
        {
            foreach (string word2 in words2)
            {
                if (word1 == word2 || word1.Contains(word2) || word2.Contains(word1))
                {
                    matches++;
                    break;
                }
            }
        }
        return matches >= minMatches;
    }

    /// <summary>
    /// 根据珍贵程度权重选择物品
    /// </summary>
    protected InventorySystemItemDataSO SelectItemByRarityWeight(List<InventorySystemItemDataSO> categoryItems)
    {
        // 解析珍贵程度并计算权重
        var itemWeights = new List<(InventorySystemItemDataSO item, float weight)>();

        foreach (var item in categoryItems)
        {
            int rarityLevel = ParseRarityLevel(item.rarity);
            float weight = CalculateRarityWeight(rarityLevel);
            itemWeights.Add((item, weight));
        }

        // 检查是否所有物品的珍贵程度都相同
        var uniqueWeights = itemWeights.Select(iw => iw.weight).Distinct().ToList();
        if (uniqueWeights.Count == 1)
        {
            // 所有物品珍贵程度相同，随机选择
            return categoryItems[UnityEngine.Random.Range(0, categoryItems.Count)];
        }

        // 使用权重随机选择
        return SelectItemByWeight(itemWeights);
    }

    /// <summary>
    /// 解析珍贵程度字符串为数值
    /// </summary>
    protected int ParseRarityLevel(string rarity)
    {
        if (string.IsNullOrEmpty(rarity))
            return 1; // 默认珍贵程度为1

        // 尝试直接解析数字
        if (int.TryParse(rarity, out int level))
        {
            return Mathf.Clamp(level, 1, 4); // 限制在1-4范围内
        }

        // 根据字符串内容判断珍贵程度
        string lowerRarity = rarity.ToLower();
        if (lowerRarity.Contains("common") || lowerRarity.Contains("普通") || lowerRarity.Contains("1"))
            return 1;
        else if (lowerRarity.Contains("uncommon") || lowerRarity.Contains("稀有") || lowerRarity.Contains("2"))
            return 2;
        else if (lowerRarity.Contains("rare") || lowerRarity.Contains("珍贵") || lowerRarity.Contains("3"))
            return 3;
        else if (lowerRarity.Contains("epic") || lowerRarity.Contains("史诗") || lowerRarity.Contains("4"))
            return 4;

        return 1; // 默认返回1
    }

    /// <summary>
    /// 根据珍贵程度计算权重（珍贵程度越高，权重越低，生成概率越小）
    /// </summary>
    protected virtual float CalculateRarityWeight(int rarityLevel)
    {
        switch (rarityLevel)
        {
            case 1: return 0.4f; // 普通物品：40%概率区间
            case 2: return 0.3f; // 稀有物品：30%概率区间
            case 3: return 0.2f; // 珍贵物品：20%概率区间
            case 4: return 0.1f; // 史诗物品：10%概率区间
            default: return 0.4f; // 默认权重
        }
    }

    /// <summary>
    /// 根据权重选择物品
    /// </summary>
    protected InventorySystemItemDataSO SelectItemByWeight(List<(InventorySystemItemDataSO item, float weight)> itemWeights)
    {
        // 计算总权重
        float totalWeight = itemWeights.Sum(iw => iw.weight);

        // 生成随机数
        float randomValue = UnityEngine.Random.Range(0f, totalWeight);

        // 根据权重选择物品
        float currentWeight = 0f;
        foreach (var (item, weight) in itemWeights)
        {
            currentWeight += weight;
            if (randomValue <= currentWeight)
            {
                if (showDebugInfo)
                {
                    int rarityLevel = ParseRarityLevel(item.rarity);
                    Debug.Log($"基于珍贵程度选择物品: {item.itemName} (珍贵程度: {rarityLevel}, 权重: {weight:F2}, 随机值: {randomValue:F2}/{totalWeight:F2})");
                }
                return item;
            }
        }

        // 如果没有选中任何物品（理论上不应该发生），返回第一个
        return itemWeights[0].item;
    }

    #endregion

    #region 物品生成和位置管理方法

    /// <summary>
    /// 在随机位置生成物品
    /// </summary>
    public GameObject SpawnItemAtRandomPosition(GameObject prefab, InventorySystemItemDataSO data)
    {
        // 确保InventoryGrid是最新的
        DetectInventoryGrid();

        if (targetGrid == null)
        {
            Debug.LogError("未找到InventoryGrid！无法生成物品。");
            return null;
        }

        Vector2Int itemSize = new Vector2Int(data.width, data.height);
        Vector2Int availablePos = FindAvailablePosition(itemSize);

        if (availablePos == new Vector2Int(-1, -1))
        {
            if (showDebugInfo)
            {
                Debug.LogWarning($"没有足够空间放置物品: {data.itemName} (尺寸: {itemSize.x}x{itemSize.y})");
            }
            return null;
        }

        return SpawnItemAtPosition(prefab, data, availablePos);
    }

    /// <summary>
    /// 在指定位置生成物品
    /// </summary>
    public virtual GameObject SpawnItemAtPosition(GameObject prefab, InventorySystemItemDataSO data, Vector2Int gridPos)
    {
        // 确保InventoryGrid是最新的
        DetectInventoryGrid();

        if (prefab == null || data == null || targetGrid == null)
        {
            Debug.LogError("生成物品参数无效！");
            return null;
        }

        Vector2Int itemSize = new Vector2Int(data.width, data.height);

        // 使用0/1标记检查位置是否可用
        if (!IsPositionAvailable(gridPos, itemSize))
        {
            if (showDebugInfo)
            {
                Debug.LogWarning($"位置 ({gridPos.x}, {gridPos.y}) 被占用或超出边界！");
            }
            return null;
        }

        // 创建物品
        GameObject spawnedItem = CreateItem(prefab, data, gridPos);

        if (spawnedItem != null)
        {
            // 标记网格占用 (设置为1)
            MarkGridOccupied(gridPos, itemSize, 1);

            // 记录生成的物品信息
            spawnedItems.Add(new SpawnedItemInfo
            {
                itemObject = spawnedItem,
                gridPosition = gridPos,
                size = itemSize,
                itemData = data,
                spawnTime = Time.time // 设置生成时间
            });

            if (showDebugInfo)
            {
                Debug.Log($"成功生成物品: {data.itemName} 位置: ({gridPos.x}, {gridPos.y}) 尺寸: ({itemSize.x}x{itemSize.y})");
            }
        }

        return spawnedItem;
    }

    /// <summary>
    /// 创建物品
    /// </summary>
    protected GameObject CreateItem(GameObject prefab, InventorySystemItemDataSO data, Vector2Int gridPos)
    {
        if (prefab == null || data == null || targetGrid == null) return null;

        // 实例化物品预制体
        GameObject item = Instantiate(prefab, itemParent);

        // 设置物品数据
        ItemDataHolder dataHolder = item.GetComponentInChildren<ItemDataHolder>();
        if (dataHolder != null)
        {
            dataHolder.SetItemData(data);
        }
        else
        {
            Debug.LogError($"预制体 {prefab.name} 中没有找到 ItemDataHolder 组件！");
        }

        // 确保物品具有必要的组件
        EnsureRequiredComponents(item);

        // 设置物品的锚点和轴心点（使用左上角作为参考点）
        RectTransform itemRect = item.GetComponent<RectTransform>();
        if (itemRect != null)
        {
            itemRect.anchorMin = new Vector2(0, 1); // 左上角
            itemRect.anchorMax = new Vector2(0, 1); // 左上角
            itemRect.pivot = new Vector2(0, 1);     // 轴心点也设置为左上角
        }

        // 设置物品在网格中的位置
        SetItemGridPosition(item, gridPos);

        return item;
    }

    /// <summary>
    /// 确保物品具有必要的组件
    /// </summary>
    protected void EnsureRequiredComponents(GameObject item)
    {
        // 确保有 DraggableItem 组件
        if (item.GetComponent<DraggableItem>() == null)
        {
            item.AddComponent<DraggableItem>();
        }

        // 确保有 InventorySystemItem 组件
        if (item.GetComponent<InventorySystemItem>() == null)
        {
            item.AddComponent<InventorySystemItem>();
        }

        // 确保有 CanvasGroup 组件（DraggableItem 需要）
        if (item.GetComponent<CanvasGroup>() == null)
        {
            item.AddComponent<CanvasGroup>();
        }
    }

    /// <summary>
    /// 设置物品在网格中的位置
    /// </summary>
    protected void SetItemGridPosition(GameObject item, Vector2Int gridPos)
    {
        if (targetGrid == null) return;

        // 获取物品组件
        var itemComponent = item.GetComponent<InventorySystemItem>();
        if (itemComponent == null) return;

        Vector2Int itemSize = new Vector2Int(itemComponent.Data.width, itemComponent.Data.height);

        // 直接调用ItemGrid的PlaceItem方法
        if (targetGrid != null)
        {
            targetGrid.PlaceItem(item, gridPos, itemSize);
        }
    }

    /// <summary>
    /// 检查位置是否可用 (使用0/1标记)
    /// </summary>
    protected bool IsPositionAvailable(Vector2Int position, Vector2Int size)
    {
        if (gridOccupancy == null) return false;

        int gridWidth = gridOccupancy.GetLength(0);
        int gridHeight = gridOccupancy.GetLength(1);

        // 检查边界
        if (position.x < 0 || position.y < 0 ||
            position.x + size.x > gridWidth || position.y + size.y > gridHeight)
        {
            return false;
        }

        // 检查重叠 (0=空闲, 1=占用)
        for (int x = position.x; x < position.x + size.x; x++)
        {
            for (int y = position.y; y < position.y + size.y; y++)
            {
                if (gridOccupancy[x, y] == 1) // 位置被占用
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// 寻找可用位置
    /// </summary>
    protected Vector2Int FindAvailablePosition(Vector2Int itemSize)
    {
        if (gridOccupancy == null) return new Vector2Int(-1, -1);

        int gridWidth = gridOccupancy.GetLength(0);
        int gridHeight = gridOccupancy.GetLength(1);

        // 从左上角开始搜索
        for (int y = 0; y <= gridHeight - itemSize.y; y++)
        {
            for (int x = 0; x <= gridWidth - itemSize.x; x++)
            {
                Vector2Int testPos = new Vector2Int(x, y);
                if (IsPositionAvailable(testPos, itemSize))
                {
                    return testPos;
                }
            }
        }

        return new Vector2Int(-1, -1); // 没有找到可用位置
    }

    /// <summary>
    /// 标记网格占用状态 (0=空闲, 1=占用)
    /// </summary>
    protected void MarkGridOccupied(Vector2Int position, Vector2Int size, int occupancyValue)
    {
        if (gridOccupancy == null) return;

        for (int x = position.x; x < position.x + size.x; x++)
        {
            for (int y = position.y; y < position.y + size.y; y++)
            {
                if (x >= 0 && x < gridOccupancy.GetLength(0) &&
                    y >= 0 && y < gridOccupancy.GetLength(1))
                {
                    gridOccupancy[x, y] = occupancyValue;
                }
            }
        }
    }

    /// <summary>
    /// 计算已占用的格子数量
    /// </summary>
    protected int CountOccupiedCells()
    {
        if (gridOccupancy == null) return 0;

        int count = 0;
        for (int x = 0; x < gridOccupancy.GetLength(0); x++)
        {
            for (int y = 0; y < gridOccupancy.GetLength(1); y++)
            {
                if (gridOccupancy[x, y] == 1)
                {
                    count++;
                }
            }
        }
        return count;
    }

    #endregion

    #region 物品管理方法

    /// <summary>
    /// 移除物品
    /// </summary>
    public void RemoveItem(GameObject item)
    {
        SpawnedItemInfo itemInfo = spawnedItems.FirstOrDefault(info => info.itemObject == item);
        if (itemInfo != null)
        {
            // 清除网格占用 (设置为0)
            MarkGridOccupied(itemInfo.gridPosition, itemInfo.size, 0);

            // 从列表中移除
            spawnedItems.Remove(itemInfo);

            // 销毁物品
            if (item != null)
            {
                DestroyImmediate(item);
            }

            if (showDebugInfo)
            {
                Debug.Log($"移除物品: {itemInfo.itemData.itemName}");
            }
        }
    }

    /// <summary>
    /// 清空所有物品
    /// </summary>
    public void ClearAllItems()
    {
        for (int i = spawnedItems.Count - 1; i >= 0; i--)
        {
            RemoveItem(spawnedItems[i].itemObject);
        }

        // 重置网格占用状态
        if (gridOccupancy != null)
        {
            int width = gridOccupancy.GetLength(0);
            int height = gridOccupancy.GetLength(1);
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    gridOccupancy[x, y] = 0;
                }
            }
        }

        if (showDebugInfo)
        {
            Debug.Log("已清空所有物品");
        }
    }

    /// <summary>
    /// 强制重新初始化
    /// </summary>
    public void ForceReinitialize()
    {
        // 清理现有状态
        targetGrid = null;
        itemParent = null;
        gridOccupancy = null;
        spawnedItems.Clear();

        // 重新初始化
        InitializeItemSpawner();

        if (showDebugInfo)
        {
            Debug.Log("强制重新初始化完成");
        }
    }

    /// <summary>
    /// 刷新网格占用状态
    /// </summary>
    protected void RefreshGridOccupancy()
    {
        if (gridOccupancy == null || targetGrid == null) return;

        // 重置网格占用状态
        int width = gridOccupancy.GetLength(0);
        int height = gridOccupancy.GetLength(1);
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                gridOccupancy[x, y] = 0;
            }
        }

        // 根据当前生成的物品重新标记占用状态
        foreach (var spawnedItem in spawnedItems)
        {
            if (spawnedItem.itemObject != null)
            {
                MarkGridOccupied(spawnedItem.gridPosition, spawnedItem.size, 1);
            }
        }

        if (showDebugInfo)
        {
            Debug.Log("网格占用状态已刷新");
        }
    }

    /// <summary>
    /// 根据物品数据获取对应的预制体
    /// </summary>
    protected GameObject GetItemPrefab(InventorySystemItemDataSO itemData)
    {
        if (itemData == null) return null;

        // 首先尝试从预制体字典中获取
        string itemKey = itemData.itemName;
        if (itemPrefabDict.ContainsKey(itemKey))
        {
            return itemPrefabDict[itemKey];
        }

        // 如果字典中没有，尝试通过ID查找
        itemKey = itemData.id.ToString();
        if (itemPrefabDict.ContainsKey(itemKey))
        {
            return itemPrefabDict[itemKey];
        }

        // 如果还是没有找到，返回null并记录警告
        if (showDebugInfo)
        {
            Debug.LogWarning($"无法找到物品 {itemData.itemName} 的预制体");
        }
        return null;
    }

    #endregion

    #region 调试和信息方法

    /// <summary>
    /// 打印网格占用状态
    /// </summary>
    protected void PrintGridOccupancy()
    {
        if (gridOccupancy == null)
        {
            Debug.Log("网格占用数组为空");
            return;
        }

        string gridString = "网格占用状态:\n";
        for (int y = 0; y < gridOccupancy.GetLength(1); y++)
        {
            for (int x = 0; x < gridOccupancy.GetLength(0); x++)
            {
                gridString += gridOccupancy[x, y] + " ";
            }
            gridString += "\n";
        }
        Debug.Log(gridString);
    }

    /// <summary>
    /// 获取生成的物品列表（用于编辑器显示）
    /// </summary>
    public List<SpawnedItemInfo> GetSpawnedItems()
    {
        return spawnedItems;
    }

    /// <summary>
    /// 获取网格占用状态（用于编辑器显示）
    /// </summary>
    public int[,] GetGridOccupancy()
    {
        return gridOccupancy;
    }

    #endregion

    // ===== ISaveable接口实现 =====

    /// <summary>
    /// 基础物品生成器保存数据类
    /// </summary>
    [System.Serializable]
    public class BaseItemSpawnSaveData
    {
        public string spawnerName; // 生成器名称
        public int totalSpawnedCount; // 总生成数量
        public float lastSpawnTime; // 最后生成时间
        public List<SpawnedItemSaveInfo> spawnedItemsData; // 生成的物品数据
        public Vector3 spawnerPosition; // 生成器位置
        public Vector3 spawnerRotation; // 生成器旋转
        public bool isActive; // 是否激活
        public string targetGridID; // 目标网格ID
    }

    /// <summary>
    /// 生成物品保存信息
    /// </summary>
    [System.Serializable]
    public class SpawnedItemSaveInfo
    {
        public string itemID; // 物品ID
        public string instanceID; // 实例ID
        public Vector2Int gridPosition; // 网格位置
        public float spawnTime; // 生成时间
        public string itemDataPath; // 物品数据路径
        public bool isValid; // 是否有效
    }

    // ISaveable接口实现
    private string saveID = "";
    private int totalSpawnedCount = 0; // 总生成计数
    private Dictionary<string, string> spawnedItemInstanceIDs = new Dictionary<string, string>(); // 生成物品的实例ID映射

    /// <summary>
    /// 获取保存ID
    /// </summary>
    public string GetSaveID()
    {
        if (string.IsNullOrEmpty(saveID))
        {
            GenerateNewSaveID();
        }
        return saveID;
    }

    /// <summary>
    /// 设置保存ID
    /// </summary>
    public void SetSaveID(string id)
    {
        if (!string.IsNullOrEmpty(id))
        {
            saveID = id;
        }
    }

    /// <summary>
    /// 生成新的保存ID
    /// 格式: BaseSpawner_[生成器名称]_[8位GUID]_[实例ID]
    /// </summary>
    public virtual void GenerateNewSaveID()
    {
        string spawnerName = gameObject.name.Replace(" ", "").Replace("(", "").Replace(")", "");
        string guidPart = System.Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
        int instanceID = GetInstanceID();
        saveID = $"BaseSpawner_{spawnerName}_{guidPart}_{instanceID}";
        
        if (Application.isPlaying && showDebugInfo)
        {
            Debug.Log($"为基础物品生成器生成新的保存ID: {saveID}");
        }
    }

    /// <summary>
    /// 为生成的物品分配实例ID
    /// </summary>
    protected string AllocateItemInstanceID(InventorySystemItemDataSO itemData, Vector2Int gridPos)
    {
        if (itemData == null) return "";
        
        totalSpawnedCount++;
        string instanceID = $"Item_{itemData.id}_{totalSpawnedCount}_{Time.time:F2}_{UnityEngine.Random.Range(1000, 9999)}";
        
        // 记录实例ID映射
        string itemKey = $"{itemData.id}_{gridPos.x}_{gridPos.y}";
        spawnedItemInstanceIDs[itemKey] = instanceID;
        
        if (showDebugInfo)
        {
            Debug.Log($"为物品 {itemData.itemName} 分配实例ID: {instanceID}");
        }
        
        return instanceID;
    }

    /// <summary>
    /// 获取保存数据
    /// </summary>
    public virtual object GetSaveData()
    {
        BaseItemSpawnSaveData saveData = new BaseItemSpawnSaveData();
        
        // 保存基础信息
        saveData.spawnerName = gameObject.name;
        saveData.totalSpawnedCount = totalSpawnedCount;
        saveData.lastSpawnTime = Time.time;
        
        // 保存位置和旋转
        saveData.spawnerPosition = transform.position;
        saveData.spawnerRotation = transform.eulerAngles;
        saveData.isActive = gameObject.activeInHierarchy;
        
        // 保存目标网格ID
        if (targetGrid != null && targetGrid is ISaveable saveableGrid)
        {
            saveData.targetGridID = saveableGrid.GetSaveID();
        }
        
        // 保存生成的物品数据
        saveData.spawnedItemsData = new List<SpawnedItemSaveInfo>();
        foreach (var spawnedItem in spawnedItems)
        {
            if (spawnedItem.itemData != null)
            {
                SpawnedItemSaveInfo itemSaveInfo = new SpawnedItemSaveInfo();
                itemSaveInfo.itemID = spawnedItem.itemData.id.ToString();
                itemSaveInfo.gridPosition = spawnedItem.gridPosition;
                itemSaveInfo.spawnTime = spawnedItem.spawnTime;
                itemSaveInfo.isValid = spawnedItem.itemObject != null;
                
                // 获取物品数据路径
#if UNITY_EDITOR
                itemSaveInfo.itemDataPath = UnityEditor.AssetDatabase.GetAssetPath(spawnedItem.itemData);
#else
                itemSaveInfo.itemDataPath = spawnedItem.itemData.name;
#endif
                
                // 获取实例ID
                string itemKey = $"{spawnedItem.itemData.id}_{spawnedItem.gridPosition.x}_{spawnedItem.gridPosition.y}";
                if (spawnedItemInstanceIDs.ContainsKey(itemKey))
                {
                    itemSaveInfo.instanceID = spawnedItemInstanceIDs[itemKey];
                }
                
                saveData.spawnedItemsData.Add(itemSaveInfo);
            }
        }
        
        return saveData;
    }

    /// <summary>
    /// 加载保存数据
    /// </summary>
    public virtual void LoadSaveData(object data)
    {
        if (data is BaseItemSpawnSaveData saveData)
        {
            try
            {
                // 恢复基础信息
                totalSpawnedCount = saveData.totalSpawnedCount;
                
                // 恢复位置和旋转
                transform.position = saveData.spawnerPosition;
                transform.eulerAngles = saveData.spawnerRotation;
                gameObject.SetActive(saveData.isActive);
                
                // 恢复目标网格引用
                if (!string.IsNullOrEmpty(saveData.targetGridID))
                {
                    RestoreTargetGridReference(saveData.targetGridID);
                }
                
                // 恢复生成的物品
                if (saveData.spawnedItemsData != null && saveData.spawnedItemsData.Count > 0)
                {
                    StartCoroutine(RestoreSpawnedItemsCoroutine(saveData.spawnedItemsData));
                }
                
                if (showDebugInfo)
                {
                    Debug.Log($"成功加载基础物品生成器保存数据: {GetSaveID()}, 恢复 {saveData.spawnedItemsData?.Count ?? 0} 个物品");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"加载基础物品生成器保存数据失败: {e.Message}");
            }
        }
        else
        {
            Debug.LogError("无效的基础物品生成器保存数据格式");
        }
    }

    /// <summary>
    /// 恢复目标网格引用
    /// </summary>
    private void RestoreTargetGridReference(string gridID)
    {
        if (string.IsNullOrEmpty(gridID)) return;
        
        // 在场景中查找具有指定ID的网格
        ItemGrid[] allGrids = FindObjectsOfType<ItemGrid>();
        foreach (var grid in allGrids)
        {
            if (grid is ISaveable saveableGrid && saveableGrid.GetSaveID() == gridID)
            {
                targetGrid = grid;
                if (showDebugInfo)
                {
                    Debug.Log($"成功恢复目标网格引用: {gridID}");
                }
                return;
            }
        }
        
        Debug.LogWarning($"无法找到ID为 {gridID} 的目标网格");
    }

    /// <summary>
    /// 协程：恢复生成的物品
    /// </summary>
    private IEnumerator RestoreSpawnedItemsCoroutine(List<SpawnedItemSaveInfo> itemsData)
    {
        if (targetGrid == null)
        {
            Debug.LogWarning("目标网格为空，无法恢复生成的物品");
            yield break;
        }
        
        // 等待一帧确保网格初始化完成
        yield return null;
        
        int restoredCount = 0;
        foreach (var itemSaveInfo in itemsData)
        {
            if (RestoreSpawnedItem(itemSaveInfo))
            {
                restoredCount++;
            }
            
            // 每恢复几个物品等待一帧，避免卡顿
            if (restoredCount % 5 == 0)
            {
                yield return null;
            }
        }
        
        // 更新网格占用状态
        RefreshGridOccupancy();
        
        if (showDebugInfo)
        {
            Debug.Log($"物品恢复完成，成功恢复 {restoredCount}/{itemsData.Count} 个物品");
        }
    }

    /// <summary>
    /// 恢复单个生成的物品
    /// </summary>
    private bool RestoreSpawnedItem(SpawnedItemSaveInfo itemSaveInfo)
    {
        try
        {
            // 查找物品数据
            InventorySystemItemDataSO itemData = FindItemDataByPath(itemSaveInfo.itemDataPath, itemSaveInfo.itemID);
            if (itemData == null)
            {
                Debug.LogWarning($"无法找到物品数据: {itemSaveInfo.itemDataPath}");
                return false;
            }
            
            // 检查网格位置是否可用
            Vector2Int itemSize = new Vector2Int(itemData.width, itemData.height);
            if (!IsPositionAvailable(itemSaveInfo.gridPosition, itemSize))
            {
                Debug.LogWarning($"网格位置 {itemSaveInfo.gridPosition} 不可用，跳过物品 {itemData.itemName}");
                return false;
            }
            
            // 查找物品预制体
            GameObject prefab = GetItemPrefab(itemData);
            if (prefab == null)
            {
                Debug.LogWarning($"无法找到物品预制体: {itemData.itemName}");
                return false;
            }
            
            // 生成物品
            GameObject spawnedItem = SpawnItemAtPosition(prefab, itemData, itemSaveInfo.gridPosition);
            if (spawnedItem != null)
            {
                // 恢复实例ID
                if (!string.IsNullOrEmpty(itemSaveInfo.instanceID))
                {
                    string itemKey = $"{itemData.id}_{itemSaveInfo.gridPosition.x}_{itemSaveInfo.gridPosition.y}";
                    spawnedItemInstanceIDs[itemKey] = itemSaveInfo.instanceID;
                    
                    // 如果物品有ItemDataHolder组件，设置实例ID
                    ItemDataHolder dataHolder = spawnedItem.GetComponent<ItemDataHolder>();
                    if (dataHolder != null && dataHolder is ISaveable saveableHolder)
                    {
                        saveableHolder.SetSaveID(itemSaveInfo.instanceID);
                    }
                }
                
                return true;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"恢复物品失败: {e.Message}");
        }
        
        return false;
    }

    /// <summary>
    /// 根据路径和ID查找物品数据
    /// </summary>
    private InventorySystemItemDataSO FindItemDataByPath(string assetPath, string itemID)
    {
        // 首先尝试从缓存中查找
        foreach (var kvp in itemDataDict)
        {
            if (kvp.Value.id.ToString() == itemID)
            {
                return kvp.Value;
            }
        }
        
        // 尝试通过路径加载
#if UNITY_EDITOR
        InventorySystemItemDataSO loadedData = UnityEditor.AssetDatabase.LoadAssetAtPath<InventorySystemItemDataSO>(assetPath);
        if (loadedData != null)
        {
            return loadedData;
        }
#endif
        
        // 最后尝试在所有物品数据中查找
        foreach (var itemData in allItemData)
        {
            if (itemData.id.ToString() == itemID)
            {
                return itemData;
            }
        }
        
        return null;
    }

    /// <summary>
    /// 验证保存数据
    /// </summary>
    public virtual bool ValidateData()
    {
        bool isValid = true;
        
        // 验证保存ID
        if (string.IsNullOrEmpty(saveID))
        {
            GenerateNewSaveID();
            isValid = false;
            Debug.LogWarning("基础物品生成器保存ID为空，已重新生成");
        }
        
        // 验证目标网格
        if (targetGrid == null)
        {
            isValid = false;
            Debug.LogWarning("目标网格引用为空");
        }
        
        // 验证生成的物品列表
        if (spawnedItems == null)
        {
            spawnedItems = new List<SpawnedItemInfo>();
            isValid = false;
            Debug.LogWarning("生成物品列表为空，已重新初始化");
        }
        
        // 验证实例ID映射
        if (spawnedItemInstanceIDs == null)
        {
            spawnedItemInstanceIDs = new Dictionary<string, string>();
            isValid = false;
            Debug.LogWarning("实例ID映射为空，已重新初始化");
        }
        
        return isValid;
    }

    /// <summary>
    /// 初始化保存系统
    /// </summary>
    public virtual void InitializeSaveSystem()
    {
        // 确保有有效的保存ID
        if (string.IsNullOrEmpty(saveID))
        {
            GenerateNewSaveID();
        }
        
        // 验证数据完整性
        ValidateData();
        
        // 初始化实例ID映射
        if (spawnedItemInstanceIDs == null)
        {
            spawnedItemInstanceIDs = new Dictionary<string, string>();
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"基础物品生成器保存系统初始化完成: {saveID}");
        }
    }

    /// <summary>
    /// 获取生成器状态摘要
    /// </summary>
    public virtual string GetSpawnerStatusSummary()
    {
        string gridInfo = targetGrid != null ? targetGrid.name : "无目标网格";
        string itemCount = spawnedItems != null ? spawnedItems.Count.ToString() : "0";
        string totalCount = totalSpawnedCount.ToString();
        
        return $"基础物品生成器 [{saveID}] - 目标网格:{gridInfo}, 当前物品:{itemCount}, 总生成数:{totalCount}";
    }

    // ===== ISaveable接口的其他必需方法 =====
    
    private bool isModified = false;
    private string lastModified = "";
    
    /// <summary>
    /// 验证保存ID是否有效
    /// </summary>
    public bool IsSaveIDValid()
    {
        return !string.IsNullOrEmpty(saveID) && saveID.Contains("BaseSpawner_");
    }
    
    /// <summary>
    /// 序列化对象数据为JSON字符串
    /// </summary>
    public string SerializeToJson()
    {
        try
        {
            var saveData = GetSaveData() as BaseItemSpawnSaveData;
            return JsonUtility.ToJson(saveData, true);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"序列化基础物品生成器数据失败: {e.Message}");
            return "";
        }
    }
    
    /// <summary>
    /// 从JSON字符串反序列化对象数据
    /// </summary>
    public bool DeserializeFromJson(string jsonData)
    {
        try
        {
            if (string.IsNullOrEmpty(jsonData)) return false;
            
            BaseItemSpawnSaveData saveData = JsonUtility.FromJson<BaseItemSpawnSaveData>(jsonData);
            if (saveData != null)
            {
                LoadSaveData(saveData);
                return true;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"反序列化基础物品生成器数据失败: {e.Message}");
        }
        return false;
    }
    
    /// <summary>
    /// 标记对象为已修改状态
    /// </summary>
    public void MarkAsModified()
    {
        isModified = true;
        UpdateLastModified();
        
        if (showDebugInfo)
        {
            Debug.Log($"基础物品生成器已标记为修改: {saveID}");
        }
    }
    
    /// <summary>
    /// 重置修改标记
    /// </summary>
    public void ResetModifiedFlag()
    {
        isModified = false;
        
        if (showDebugInfo)
        {
            Debug.Log($"基础物品生成器修改标记已重置: {saveID}");
        }
    }
    
    /// <summary>
    /// 检查对象是否已被修改
    /// </summary>
    public bool IsModified()
    {
        return isModified;
    }
    
    /// <summary>
    /// 获取对象的最后修改时间
    /// </summary>
    public string GetLastModified()
    {
        return lastModified;
    }
    
    /// <summary>
    /// 更新最后修改时间为当前时间
    /// </summary>
    public void UpdateLastModified()
    {
        lastModified = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        
        if (showDebugInfo)
        {
            Debug.Log($"基础物品生成器最后修改时间已更新: {lastModified}");
        }
    }
    
}
