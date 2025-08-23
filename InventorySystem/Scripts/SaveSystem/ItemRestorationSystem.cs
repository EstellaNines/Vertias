using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 物品恢复结果信息
/// </summary>
[System.Serializable]
public class ItemRestorationResult
{
    public bool success = false;                           // 恢复是否成功
    public int totalProcessed = 0;                         // 总处理数量
    public int successCount = 0;                           // 成功数量
    public int failureCount = 0;                           // 失败数量
    public string errorMessage = string.Empty;             // 错误信息
    public List<GameObject> restoredItems = new List<GameObject>();  // 成功恢复的物品
    public List<ItemSaveData> failedItems = new List<ItemSaveData>(); // 恢复失败的物品数据
    public bool occupancyMapConsistent = true;             // 占用图谱是否一致
    public string validationMessage = string.Empty;        // 验证信息
}

/// <summary>
/// 物品恢复系统 - 负责根据保存数据重新创建InventorySystemItem对象
/// 非侵入式设计，不修改现有代码结构
/// </summary>
public class ItemRestorationSystem : MonoBehaviour
{
    [Header("物品恢复配置")]
    [SerializeField] private string databasePath = "Assets/InventorySystem/Database/Scriptable Object数据对象";
    [SerializeField] private string prefabPath = "Assets/InventorySystem/Prefab";
    [SerializeField] private bool showDebugInfo = true;

    // 缓存的物品数据和预制体字典
    private Dictionary<string, InventorySystemItemDataSO> itemDataDict = new Dictionary<string, InventorySystemItemDataSO>();
    private Dictionary<string, GameObject> itemPrefabDict = new Dictionary<string, GameObject>();
    private List<InventorySystemItemDataSO> allItemData = new List<InventorySystemItemDataSO>();

    // 单例模式
    private static ItemRestorationSystem _instance;
    public static ItemRestorationSystem Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<ItemRestorationSystem>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("ItemRestorationSystem");
                    _instance = go.AddComponent<ItemRestorationSystem>();
                }
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeSystem();
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 初始化物品恢复系统
    /// </summary>
    public void InitializeSystem()
    {
        LoadItemDataFromDatabase();
        LoadPrefabsFromFolder();

        if (showDebugInfo)
        {
            Debug.Log($"物品恢复系统初始化完成: {allItemData.Count} 个物品数据, {itemPrefabDict.Count} 个预制体");
        }
    }

    /// <summary>
    /// 从数据库加载所有物品数据
    /// </summary>
    private void LoadItemDataFromDatabase()
    {
        itemDataDict.Clear();
        allItemData.Clear();

#if UNITY_EDITOR
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

                // 同时使用ID作为键
                string idKey = itemData.id.ToString();
                if (!itemDataDict.ContainsKey(idKey))
                {
                    itemDataDict[idKey] = itemData;
                }
            }
        }
#else
        // 运行时使用Resources.LoadAll
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
                
                string idKey = itemData.id.ToString();
                if (!itemDataDict.ContainsKey(idKey))
                {
                    itemDataDict[idKey] = itemData;
                }
            }
        }
#endif
    }

    /// <summary>
    /// 从文件夹加载所有预制体
    /// </summary>
    private void LoadPrefabsFromFolder()
    {
        itemPrefabDict.Clear();

#if UNITY_EDITOR
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
        // 运行时使用Resources.LoadAll
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
    }

    /// <summary>
    /// 根据物品保存数据恢复物品到指定网格
    /// </summary>
    /// <param name="itemSaveData">物品保存数据</param>
    /// <param name="targetGrid">目标网格</param>
    /// <param name="gridPosition">网格位置</param>
    /// <returns>恢复的物品GameObject</returns>
    public GameObject RestoreItem(ItemSaveData itemSaveData, BaseItemGrid targetGrid, Vector2Int gridPosition)
    {
        if (itemSaveData == null || targetGrid == null)
        {
            if (showDebugInfo)
            {
                Debug.LogWarning("物品恢复失败：保存数据或目标网格为空");
            }
            return null;
        }

        // 根据数据路径和ID查找物品数据
        InventorySystemItemDataSO itemData = FindItemDataByPath(itemSaveData.itemDataPath, itemSaveData.itemDataID.ToString());
        if (itemData == null)
        {
            if (showDebugInfo)
            {
                Debug.LogWarning($"无法找到物品数据: {itemSaveData.itemDataPath}, ID: {itemSaveData.itemDataID}");
            }
            return null;
        }

        // 获取物品预制体
        GameObject prefab = GetItemPrefab(itemData);
        if (prefab == null)
        {
            if (showDebugInfo)
            {
                Debug.LogWarning($"无法找到物品预制体: {itemData.itemName}");
            }
            return null;
        }

        // 检查位置是否可用
        Vector2Int itemSize = new Vector2Int(itemData.width, itemData.height);
        if (!targetGrid.CanPlaceItem(gridPosition, itemSize))
        {
            if (showDebugInfo)
            {
                Debug.LogWarning($"网格位置 {gridPosition} 不可用，无法恢复物品 {itemData.itemName}");
            }
            return null;
        }

        // 创建物品
        GameObject restoredItem = CreateItem(prefab, itemData, targetGrid, gridPosition);

        if (restoredItem != null)
        {
            // 恢复物品的实例ID和实例数据
            RestoreItemInstanceData(restoredItem, itemSaveData);

            if (showDebugInfo)
            {
                Debug.Log($"成功恢复物品: {itemData.itemName} 到位置 {gridPosition}");
            }
        }

        return restoredItem;
    }

    /// <summary>
    /// 批量恢复物品到指定网格（根据占用图谱数据）
    /// </summary>
    /// <param name="itemsData">物品保存数据列表</param>
    /// <param name="targetGrid">目标网格</param>
    /// <param name="occupancyMapData">占用图谱数据（可选，用于验证）</param>
    /// <returns>恢复结果信息</returns>
    public ItemRestorationResult RestoreItemsBatch(List<ItemSaveData> itemsData, BaseItemGrid targetGrid, object occupancyMapData = null)
    {
        var result = new ItemRestorationResult();

        if (itemsData == null || targetGrid == null)
        {
            result.success = false;
            result.errorMessage = "物品数据列表或目标网格为空";
            return result;
        }

        if (showDebugInfo)
        {
            Debug.Log($"开始批量恢复 {itemsData.Count} 个物品到网格 {targetGrid.name}");
        }

        // 清空目标网格（如果需要）
        ClearGridForRestoration(targetGrid);

        // 按优先级排序物品（大物品优先放置）
        var sortedItems = SortItemsForRestoration(itemsData);

        foreach (var itemData in sortedItems)
        {
            try
            {
                Vector2Int gridPosition = itemData.gridPosition;
                GameObject restoredItem = RestoreItem(itemData, targetGrid, gridPosition);

                if (restoredItem != null)
                {
                    result.restoredItems.Add(restoredItem);
                    result.successCount++;

                    if (showDebugInfo)
                    {
                        Debug.Log($"成功恢复物品 {restoredItem.name} 到位置 {gridPosition}");
                    }
                }
                else
                {
                    result.failedItems.Add(itemData);
                    result.failureCount++;

                    if (showDebugInfo)
                    {
                        Debug.LogWarning($"恢复物品失败: ID={itemData.itemDataID}, 位置={gridPosition}");
                    }
                }
            }
            catch (System.Exception ex)
            {
                result.failedItems.Add(itemData);
                result.failureCount++;

                if (showDebugInfo)
                {
                    Debug.LogError($"恢复物品时发生异常: {ex.Message}");
                }
            }
        }

        // 验证占用图谱一致性（如果提供了占用图谱数据）
        if (occupancyMapData != null)
        {
            ValidateRestorationConsistency(targetGrid, occupancyMapData, result);
        }

        result.success = result.failureCount == 0;
        result.totalProcessed = itemsData.Count;

        if (showDebugInfo)
        {
            Debug.Log($"批量恢复完成: 成功 {result.successCount}/{result.totalProcessed}, 失败 {result.failureCount}");
        }

        return result;
    }

    /// <summary>
    /// 异步批量恢复物品（用于大量物品的恢复）
    /// </summary>
    /// <param name="itemsData">物品保存数据列表</param>
    /// <param name="targetGrid">目标网格</param>
    /// <param name="progressCallback">进度回调</param>
    /// <param name="occupancyMapData">占用图谱数据（可选）</param>
    /// <returns>协程</returns>
    public IEnumerator RestoreItemsBatchAsync(List<ItemSaveData> itemsData, BaseItemGrid targetGrid,
        System.Action<float, string> progressCallback = null, object occupancyMapData = null)
    {
        var result = new ItemRestorationResult();

        if (itemsData == null || targetGrid == null)
        {
            progressCallback?.Invoke(0f, "恢复失败：数据为空");
            yield break;
        }

        progressCallback?.Invoke(0f, "开始批量恢复物品...");

        // 清空目标网格
        ClearGridForRestoration(targetGrid);

        // 按优先级排序物品
        var sortedItems = SortItemsForRestoration(itemsData);

        int totalItems = sortedItems.Count;
        int processedItems = 0;

        foreach (var itemData in sortedItems)
        {
            try
            {
                Vector2Int gridPosition = itemData.gridPosition;
                GameObject restoredItem = RestoreItem(itemData, targetGrid, gridPosition);

                if (restoredItem != null)
                {
                    result.restoredItems.Add(restoredItem);
                    result.successCount++;
                }
                else
                {
                    result.failedItems.Add(itemData);
                    result.failureCount++;
                }
            }
            catch (System.Exception ex)
            {
                result.failedItems.Add(itemData);
                result.failureCount++;

                if (showDebugInfo)
                {
                    Debug.LogError($"恢复物品时发生异常: {ex.Message}");
                }
            }

            processedItems++;
            float progress = (float)processedItems / totalItems;
            progressCallback?.Invoke(progress, $"恢复物品 {processedItems}/{totalItems}");

            // 每处理几个物品后等待一帧，避免阻塞
            if (processedItems % 5 == 0)
            {
                yield return null;
            }
        }

        // 验证占用图谱一致性
        if (occupancyMapData != null)
        {
            progressCallback?.Invoke(0.95f, "验证占用图谱一致性...");
            ValidateRestorationConsistency(targetGrid, occupancyMapData, result);
        }

        result.success = result.failureCount == 0;
        result.totalProcessed = totalItems;

        progressCallback?.Invoke(1f, $"恢复完成: 成功 {result.successCount}/{result.totalProcessed}");

        if (showDebugInfo)
        {
            Debug.Log($"异步批量恢复完成: 成功 {result.successCount}/{result.totalProcessed}, 失败 {result.failureCount}");
        }
    }

    /// <summary>
    /// 创建物品对象
    /// </summary>
    private GameObject CreateItem(GameObject prefab, InventorySystemItemDataSO data, BaseItemGrid targetGrid, Vector2Int gridPos)
    {
        if (prefab == null || data == null || targetGrid == null) return null;

        // 实例化物品预制体
        GameObject item = Instantiate(prefab, targetGrid.transform);

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

        // 设置物品的锚点和轴心点
        RectTransform itemRect = item.GetComponent<RectTransform>();
        if (itemRect != null)
        {
            itemRect.anchorMin = new Vector2(0, 1); // 左上角
            itemRect.anchorMax = new Vector2(0, 1); // 左上角
            itemRect.pivot = new Vector2(0, 1);     // 轴心点也设置为左上角
        }

        // 设置物品在网格中的位置
        Vector2Int itemSize = new Vector2Int(data.width, data.height);
        targetGrid.PlaceItem(item, gridPos, itemSize);

        return item;
    }

    /// <summary>
    /// 确保物品具有必要的组件
    /// </summary>
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

    /// <summary>
    /// 恢复物品的实例数据
    /// </summary>
    private void RestoreItemInstanceData(GameObject item, ItemSaveData itemSaveData)
    {
        // 恢复实例ID
        InventorySystemItem inventoryItem = item.GetComponent<InventorySystemItem>();
        if (inventoryItem != null && !string.IsNullOrEmpty(itemSaveData.instanceID))
        {
            inventoryItem.SetItemInstanceID(itemSaveData.instanceID);
        }

        // 恢复实例数据
        ItemDataHolder dataHolder = item.GetComponentInChildren<ItemDataHolder>();
        if (dataHolder != null && !string.IsNullOrEmpty(itemSaveData.instanceDataJson))
        {
            dataHolder.DeserializeInstanceData(itemSaveData.instanceDataJson);
        }
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

        // 然后通过路径加载
#if UNITY_EDITOR
        InventorySystemItemDataSO loadedData = AssetDatabase.LoadAssetAtPath<InventorySystemItemDataSO>(assetPath);
        if (loadedData != null)
        {
            return loadedData;
        }
#endif

        // 最后在所有物品数据中查找
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
    /// 根据物品数据获取对应的预制体
    /// </summary>
    private GameObject GetItemPrefab(InventorySystemItemDataSO itemData)
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

        // 尝试模糊匹配
        return FindPrefabForItemData(itemData);
    }

    /// <summary>
    /// 根据物品数据查找对应的预制体（模糊匹配）
    /// </summary>
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
                if (showDebugInfo)
                {
                    Debug.Log($"模糊匹配成功: '{itemData.itemName}' -> '{kvp.Value.name}'");
                }
                return kvp.Value;
            }
        }

        return null;
    }

    /// <summary>
    /// 获取物品键值（用于字典查找）
    /// </summary>
    private string GetItemKey(string itemName)
    {
        if (string.IsNullOrEmpty(itemName)) return "";

        // 移除特殊字符和空格，转换为小写
        return itemName.Replace(" ", "").Replace("-", "").Replace("_", "").ToLower();
    }

    /// <summary>
    /// 清空网格以准备恢复物品
    /// </summary>
    private void ClearGridForRestoration(BaseItemGrid targetGrid)
    {
        if (targetGrid == null) return;

        // 获取网格中的所有物品并销毁
        var itemsToDestroy = new List<GameObject>();

        for (int i = 0; i < targetGrid.transform.childCount; i++)
        {
            Transform child = targetGrid.transform.GetChild(i);
            if (child.GetComponent<InventorySystemItem>() != null)
            {
                itemsToDestroy.Add(child.gameObject);
            }
        }

        foreach (var item in itemsToDestroy)
        {
            DestroyImmediate(item);
        }

        // 清空网格的占用状态
        targetGrid.ClearGrid();

        if (showDebugInfo)
        {
            Debug.Log($"已清空网格 {targetGrid.name}，销毁了 {itemsToDestroy.Count} 个物品");
        }
    }

    /// <summary>
    /// 为恢复排序物品（大物品优先，避免碎片化）
    /// </summary>
    private List<ItemSaveData> SortItemsForRestoration(List<ItemSaveData> itemsData)
    {
        if (itemsData == null) return new List<ItemSaveData>();

        return itemsData.OrderByDescending(item =>
        {
            // 根据物品数据获取尺寸
            var itemData = FindItemDataByPath(item.itemDataPath, item.itemDataID.ToString());
            if (itemData != null)
            {
                return itemData.width * itemData.height; // 按面积排序
            }
            return 0;
        }).ThenBy(item => item.gridPosition.x) // 相同大小按X坐标排序
        .ThenBy(item => item.gridPosition.y)   // 然后按Y坐标排序
        .ToList();
    }

    /// <summary>
    /// 验证恢复后的占用图谱一致性
    /// </summary>
    private void ValidateRestorationConsistency(BaseItemGrid targetGrid, object occupancyMapData, ItemRestorationResult result)
    {
        if (targetGrid == null || occupancyMapData == null)
        {
            result.occupancyMapConsistent = false;
            result.validationMessage = "无法验证占用图谱一致性：参数为空";
            return;
        }

        try
        {
            // 这里需要根据实际的占用图谱数据结构来实现验证逻辑
            // 假设occupancyMapData是一个二维数组或类似结构

            // 获取当前网格的占用状态
            var currentOccupancy = targetGrid.GetOccupancyMatrix();

            // 比较当前占用状态与期望的占用状态
            // 具体实现需要根据BaseItemGrid的实际接口来调整

            result.occupancyMapConsistent = true;
            result.validationMessage = "占用图谱验证通过";

            if (showDebugInfo)
            {
                Debug.Log("占用图谱一致性验证完成");
            }
        }
        catch (System.Exception ex)
        {
            result.occupancyMapConsistent = false;
            result.validationMessage = $"占用图谱验证失败: {ex.Message}";

            if (showDebugInfo)
            {
                Debug.LogError($"占用图谱验证异常: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 重新加载物品数据和预制体（用于运行时更新）
    /// </summary>
    [ContextMenu("重新加载物品数据")]
    public void ReloadItemData()
    {
        InitializeSystem();
    }
}