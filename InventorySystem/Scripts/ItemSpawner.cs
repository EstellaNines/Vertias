using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class ItemSpawner : MonoBehaviour
{
    [Header("物品生成器设置")]
    [SerializeField] private ItemGrid targetGrid; // 目标网格
    [SerializeField] private Transform itemParent; // 物品父对象

    [Header("生成控制设置")]
    [Range(1, 50)]
    [SerializeField] private int spawnCount = 5; // 生成数量滑条

    [Header("物品类型选择")]
    [SerializeField][FieldLabel("头盔")] private bool spawnHelmet = true;
    [SerializeField][FieldLabel("护甲")] private bool spawnArmor = true;
    [SerializeField][FieldLabel("战术挂具")] private bool spawnTacticalRig = true;
    [SerializeField][FieldLabel("背包")] private bool spawnBackpack = true;
    [SerializeField][FieldLabel("武器")] private bool spawnWeapon = true;
    [SerializeField][FieldLabel("弹药")] private bool spawnAmmunition = true;
    [SerializeField][FieldLabel("食物")] private bool spawnFood = true;
    [SerializeField][FieldLabel("饮料")] private bool spawnDrink = true;
    [SerializeField][FieldLabel("治疗药物")] private bool spawnHealing = true;
    [SerializeField][FieldLabel("止血药物")] private bool spawnHemostatic = true;
    [SerializeField][FieldLabel("镇静药物")] private bool spawnSedative = true;
    [SerializeField][FieldLabel("情报")] private bool spawnIntelligence = true;
    [SerializeField][FieldLabel("货币")] private bool spawnCurrency = true;

    [Header("文件夹路径设置")]
    [SerializeField] private string databasePath = "Assets/InventorySystem/Database/Scriptable Object数据对象";
    [SerializeField] private string prefabPath = "Assets/InventorySystem/Prefab";

    [Header("调试信息设置")]
    [SerializeField] private bool showDebugInfo = true;
    [SerializeField] private bool showGridOccupancy = false; // 显示网格占用状态
    [SerializeField] private bool autoLoadOnStart = true; // 启动时自动加载

    // 自动加载的物品数据和预制体
    private Dictionary<string, InventorySystemItemDataSO> itemDataDict = new Dictionary<string, InventorySystemItemDataSO>();
    private Dictionary<string, GameObject> itemPrefabDict = new Dictionary<string, GameObject>();
    private List<InventorySystemItemDataSO> allItemData = new List<InventorySystemItemDataSO>();

    // 网格占用状态数组 (0=空闲, 1=占用)
    private int[,] gridOccupancy;
    private List<SpawnedItemInfo> spawnedItems = new List<SpawnedItemInfo>();

    // 生成物品信息类
    [System.Serializable]
    public class SpawnedItemInfo
    {
        public GameObject itemObject;
        public Vector2Int gridPosition;
        public Vector2Int size;
        public InventorySystemItemDataSO itemData;
    }

    private void Start()
    {
        if (targetGrid == null)
        {
            targetGrid = FindObjectOfType<ItemGrid>();
        }

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

    // 延迟初始化协程
    private IEnumerator DelayedInitialization()
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

    // 新增：使用GridConfig初始化网格
    private void InitializeGridWithConfig(GridConfig config)
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

    // 修改原有的InitializeGrid方法作为备用
    private void InitializeGrid()
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

    // 从文件夹加载所有物品数据和预制体
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

    // 从Database文件夹加载所有ScriptableObject数据
    private void LoadItemDataFromDatabase()
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

    // 从Prefab文件夹加载所有预制体
    private void LoadPrefabsFromFolder()
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

    // 生成物品键值（用于匹配数据和预制体）
    private string GetItemKey(string name)
    {
        // 移除特殊字符和空格，转换为小写用于匹配
        return name.Replace(" ", "").Replace("(", "").Replace(")", "").Replace("-", "").Replace("_", "").ToLower();
    }

    // 根据物品数据查找对应的预制体
    private GameObject FindPrefabForItemData(InventorySystemItemDataSO itemData)
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
                return kvp.Value;
            }
        }

        if (showDebugInfo)
        {
            Debug.LogWarning($"未找到物品 '{itemData.itemName}' 对应的预制体");
        }

        return null;
    }

    // 生成多个随机物品
    public void SpawnRandomItems()
    {
        if (allItemData.Count == 0)
        {
            Debug.LogError("没有加载任何物品数据！请先调用LoadAllItemsFromFolders()");
            return;
        }

        List<InventorySystemItemDataSO> availableItems = GetAvailableItemsByCategory();
        if (availableItems.Count == 0)
        {
            Debug.LogWarning("没有选择任何物品类型！");
            return;
        }

        int successCount = 0;
        int attemptCount = 0;
        int maxAttempts = spawnCount * 3; // 最大尝试次数

        while (successCount < spawnCount && attemptCount < maxAttempts)
        {
            attemptCount++;

            // 随机选择物品
            InventorySystemItemDataSO randomItemData = availableItems[UnityEngine.Random.Range(0, availableItems.Count)];
            GameObject prefab = FindPrefabForItemData(randomItemData);

            if (prefab != null)
            {
                GameObject spawnedItem = SpawnItemAtRandomPosition(prefab, randomItemData);
                if (spawnedItem != null)
                {
                    successCount++;
                }
            }
            else
            {
                if (showDebugInfo)
                {
                    Debug.LogWarning($"物品 '{randomItemData.itemName}' 没有对应的预制体");
                }
            }
        }

        if (showDebugInfo)
        {
            Debug.Log($"生成完成: 成功生成 {successCount}/{spawnCount} 个物品，尝试次数: {attemptCount}");
            if (showGridOccupancy)
            {
                PrintGridOccupancy();
            }
        }
    }

    // 根据选择的类型获取可用物品列表
    private List<InventorySystemItemDataSO> GetAvailableItemsByCategory()
    {
        List<InventorySystemItemDataSO> availableItems = new List<InventorySystemItemDataSO>();

        foreach (var itemData in allItemData)
        {
            if (itemData == null) continue;

            bool shouldInclude = false;
            switch (itemData.itemCategory)
            {
                case InventorySystemItemCategory.Helmet:
                    shouldInclude = spawnHelmet;
                    break;
                case InventorySystemItemCategory.Armor:
                    shouldInclude = spawnArmor;
                    break;
                case InventorySystemItemCategory.TacticalRig:
                    shouldInclude = spawnTacticalRig;
                    break;
                case InventorySystemItemCategory.Backpack:
                    shouldInclude = spawnBackpack;
                    break;
                case InventorySystemItemCategory.Weapon:
                    shouldInclude = spawnWeapon;
                    break;
                case InventorySystemItemCategory.Ammunition:
                    shouldInclude = spawnAmmunition;
                    break;
                case InventorySystemItemCategory.Food:
                    shouldInclude = spawnFood;
                    break;
                case InventorySystemItemCategory.Drink:
                    shouldInclude = spawnDrink;
                    break;
                case InventorySystemItemCategory.Healing:
                    shouldInclude = spawnHealing;
                    break;
                case InventorySystemItemCategory.Hemostatic:
                    shouldInclude = spawnHemostatic;
                    break;
                case InventorySystemItemCategory.Sedative:
                    shouldInclude = spawnSedative;
                    break;
                case InventorySystemItemCategory.Intelligence:
                    shouldInclude = spawnIntelligence;
                    break;
                case InventorySystemItemCategory.Currency:
                    shouldInclude = spawnCurrency;
                    break;
            }

            if (shouldInclude)
            {
                availableItems.Add(itemData);
            }
        }

        return availableItems;
    }

    // 在随机位置生成物品
    public GameObject SpawnItemAtRandomPosition(GameObject prefab, InventorySystemItemDataSO data)
    {
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

    // 在指定位置生成物品
    public GameObject SpawnItemAtPosition(GameObject prefab, InventorySystemItemDataSO data, Vector2Int gridPos)
    {
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
                itemData = data
            });

            if (showDebugInfo)
            {
                Debug.Log($"成功生成物品: {data.itemName} 位置: ({gridPos.x}, {gridPos.y}) 尺寸: ({itemSize.x}x{itemSize.y})");
            }
        }

        return spawnedItem;
    }

    // 创建物品
    private GameObject CreateItem(GameObject prefab, InventorySystemItemDataSO data, Vector2Int gridPos)
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

    // 确保物品具有必要的组件
    private void EnsureRequiredComponents(GameObject item)
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

    // 设置物品在网格中的位置
    private void SetItemGridPosition(GameObject item, Vector2Int gridPos)
    {
        if (targetGrid == null) return;

        // 获取物品组件
        var itemComponent = item.GetComponent<InventorySystemItem>();
        if (itemComponent == null) return;

        Vector2Int itemSize = new Vector2Int(itemComponent.Data.width, itemComponent.Data.height);

        // 直接调用ItemGrid的PlaceItem方法（不再使用反射）
        if (targetGrid != null)
        {
            targetGrid.PlaceItem(item, gridPos, itemSize);
        }
    }

    // 检查位置是否可用 (使用0/1标记)
    private bool IsPositionAvailable(Vector2Int position, Vector2Int size)
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

    // 寻找可用位置
    private Vector2Int FindAvailablePosition(Vector2Int itemSize)
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

    // 标记网格占用状态 (0=空闲, 1=占用)
    private void MarkGridOccupied(Vector2Int position, Vector2Int size, int occupancyValue)
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

    // 移除物品
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

    // 清空所有物品
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
            Debug.Log("已清空所有物品并重置网格占用状态");
        }
    }

    // 打印网格占用状态 (调试用)
    private void PrintGridOccupancy()
    {
        if (gridOccupancy == null) return;

        int width = gridOccupancy.GetLength(0);
        int height = gridOccupancy.GetLength(1);

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("网格占用状态 (0=空闲, 1=占用):");

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                sb.Append(gridOccupancy[x, y]);
                sb.Append(" ");
            }
            sb.AppendLine();
        }

        Debug.Log(sb.ToString());
    }

    // 获取网格占用状态数组（供外部调用）
    public int[,] GetGridOccupancy()
    {
        return gridOccupancy;
    }

    // 获取生成的物品列表
    public List<SpawnedItemInfo> GetSpawnedItems()
    {
        return spawnedItems;
    }

    // 设置生成数量
    public void SetSpawnCount(int count)
    {
        spawnCount = Mathf.Clamp(count, 1, 50);
    }

    // 获取生成数量
    public int GetSpawnCount()
    {
        return spawnCount;
    }

    // 获取加载的物品数据数量
    public int GetLoadedItemDataCount()
    {
        return allItemData.Count;
    }

    // 获取加载的预制体数量
    public int GetLoadedPrefabCount()
    {
        return itemPrefabDict.Count;
    }

    // 获取所有物品数据
    public List<InventorySystemItemDataSO> GetAllItemData()
    {
        return allItemData;
    }

    private void Update()
    {
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

    // 验证已生成物品的位置是否仍然有效
    private void ValidateSpawnedItems()
    {
        if (gridOccupancy == null) return;

        // 清空网格占用状态
        int width = gridOccupancy.GetLength(0);
        int height = gridOccupancy.GetLength(1);
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                gridOccupancy[x, y] = 0;
            }
        }

        // 重新标记已生成物品的占用状态
        foreach (var spawnedItem in spawnedItems)
        {
            if (spawnedItem.itemObject != null)
            {
                MarkGridOccupied(spawnedItem.gridPosition, spawnedItem.size, 1);
            }
        }

        if (showDebugInfo)
        {
            Debug.Log($"重新验证了 {spawnedItems.Count} 个已生成物品的位置");
        }
    }

}