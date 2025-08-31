using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using InventorySystem;

public class InventorySaveManager : MonoBehaviour
{
    [Header("保存设置")]
    [FieldLabel("默认存档文件名")]
    public string defaultSaveFileName = "InventorySave.es3";

    [FieldLabel("自动保存间隔(秒)")]
    public float autoSaveInterval = 5f;

    [FieldLabel("启用自动保存")]
    public bool enableAutoSave = true;

    [FieldLabel("启用备份")]
    public bool enableBackup = true;

    [Header("调试信息")]
    [FieldLabel("显示保存日志")]
    public bool showSaveLog = true;

    // 单例模式
    public static InventorySaveManager Instance { get; private set; }

    // 事件系统
    public static event Action<bool> OnSaveCompleted;    // 保存完成事件
    public static event Action<bool> OnLoadCompleted;    // 加载完成事件

    // 内部变量
    private float lastAutoSaveTime;
    private bool isSaving = false;
    private bool isLoading = false;

    // 缓存的网格引用
    private Dictionary<string, ItemGrid> registeredGrids = new Dictionary<string, ItemGrid>();

    #region Unity生命周期

    private void Awake()
    {
        // 单例模式初始化
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeSaveManager();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        // 自动保存逻辑
        if (enableAutoSave && Time.time - lastAutoSaveTime >= autoSaveInterval)
        {
            AutoSave();
        }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        // 应用暂停时保存
        if (pauseStatus && enableAutoSave)
        {
            SaveInventory();
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        // 应用失去焦点时保存
        if (!hasFocus && enableAutoSave)
        {
            SaveInventory();
        }
    }

    #endregion

    #region 初始化方法

    private void InitializeSaveManager()
    {
        lastAutoSaveTime = Time.time;

        // 自动注册场景中的网格
        RegisterAllGridsInScene();

        if (showSaveLog)
            Debug.Log("[InventorySaveManager] 保存管理器初始化完成");
    }

    // 自动注册场景中的所有网格
    private void RegisterAllGridsInScene()
    {
        ItemGrid[] grids = FindObjectsOfType<ItemGrid>();
        foreach (var grid in grids)
        {
            RegisterGrid(grid);
        }
    }

    // 手动注册网格 - 支持GUID注册
    public void RegisterGrid(ItemGrid grid, string gridName = null)
    {
        if (grid == null) return;
        
        try
        {
            // 优先使用GUID作为注册键，如果没有则使用名称
            string gridGUID = grid.GridGUID;
            string registrationKey = !string.IsNullOrEmpty(gridGUID) ? gridGUID : (gridName ?? grid.gameObject.name);

            if (!registeredGrids.ContainsKey(registrationKey))
            {
                registeredGrids[registrationKey] = grid;
                if (showSaveLog)
                    Debug.Log($"[InventorySaveManager] 注册网格: {registrationKey} (类型: {grid.GridType}, 名称: {grid.GridName})");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[InventorySaveManager] 注册网格时发生异常: {e.Message}");
        }
    }
    
    /// <summary>
    /// 根据GUID注册网格
    /// </summary>
    /// <param name="grid">网格实例</param>
    public void RegisterGridByGUID(ItemGrid grid)
    {
        if (grid == null) return;
        
        try
        {
            string guid = grid.GridGUID;
            if (!string.IsNullOrEmpty(guid) && !registeredGrids.ContainsKey(guid))
            {
                registeredGrids[guid] = grid;
                if (showSaveLog)
                    Debug.Log($"[InventorySaveManager] 按GUID注册网格: {guid} (类型: {grid.GridType}, 名称: {grid.GridName})");
            }
            else if (string.IsNullOrEmpty(guid))
            {
                Debug.LogWarning($"[InventorySaveManager] 网格 {grid.gameObject.name} 的GUID为空，无法注册");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[InventorySaveManager] 按GUID注册网格时发生异常: {e.Message}");
        }
    }
    
    /// <summary>
    /// 根据网格类型获取所有网格
    /// </summary>
    /// <param name="gridType">网格类型</param>
    /// <returns>指定类型的网格列表</returns>
    public List<ItemGrid> GetGridsByType(GridType gridType)
    {
        List<ItemGrid> result = new List<ItemGrid>();
        foreach (var grid in registeredGrids.Values)
        {
            if (grid != null && grid.GridType == gridType)
            {
                result.Add(grid);
            }
        }
        return result;
    }

    /// <summary>
    /// 取消注册网格
    /// </summary>
    /// <param name="gridName">网格名称</param>
    public void UnregisterGrid(string gridName)
    {
        if (registeredGrids.ContainsKey(gridName))
        {
            registeredGrids.Remove(gridName);

            if (showSaveLog)
                Debug.Log($"[InventorySaveManager] 取消注册网格: {gridName}");
        }
    }

    #endregion

    #region 保存方法

    /// <summary>
    /// 保存背包数据到默认文件
    /// </summary>
    /// <returns>保存是否成功</returns>
    public bool SaveInventory()
    {
        return SaveInventory(defaultSaveFileName);
    }

    /// <summary>
    /// 保存背包数据到指定文件
    /// </summary>
    /// <param name="fileName">文件名</param>
    /// <returns>保存是否成功</returns>
    public bool SaveInventory(string fileName)
    {
        if (isSaving)
        {
            if (showSaveLog)
                Debug.LogWarning("[InventorySaveManager] 正在保存中，跳过本次保存请求");
            return false;
        }

        isSaving = true;

        try
        {
            // 创建备份
            if (enableBackup && ES3.FileExists(fileName))
            {
                CreateBackup(fileName);
            }

            // 收集所有网格数据
            InventorySaveData saveData = CollectInventoryData();

            // 使用ES3保存数据
            ES3.Save("InventoryData", saveData, fileName);

            // 更新保存时间
            lastAutoSaveTime = Time.time;

            if (showSaveLog)
                Debug.Log($"[InventorySaveManager] 背包数据保存成功: {fileName}");

            // 触发保存完成事件
            OnSaveCompleted?.Invoke(true);

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[InventorySaveManager] 保存失败: {e.Message}");
            OnSaveCompleted?.Invoke(false);
            return false;
        }
        finally
        {
            isSaving = false;
        }
    }

    /// <summary>
    /// 自动保存
    /// </summary>
    private void AutoSave()
    {
        if (showSaveLog)
            Debug.Log("[InventorySaveManager] 执行自动保存");

        SaveInventory();
    }

    /// <summary>
    /// 强制保存 - 立即保存，忽略保存状态检查
    /// </summary>
    /// <returns>保存是否成功</returns>
    public bool ForceSave()
    {
        return ForceSave(defaultSaveFileName);
    }

    /// <summary>
    /// 强制保存到指定文件 - 立即保存，忽略保存状态检查
    /// </summary>
    /// <param name="fileName">文件名</param>
    /// <returns>保存是否成功</returns>
    public bool ForceSave(string fileName)
    {
        // 临时保存当前的保存状态
        bool wasSaving = isSaving;
        isSaving = false; // 重置保存状态以允许强制保存

        try
        {
            if (showSaveLog)
                Debug.Log("[InventorySaveManager] 执行强制保存");

            // 调用正常的保存方法
            return SaveInventory(fileName);
        }
        finally
        {
            // 如果之前正在保存，恢复保存状态
            if (wasSaving)
                isSaving = true;
        }
    }

    /// <summary>
    /// 拖拽触发保存 - 当物品拖拽到新位置时自动保存
    /// </summary>
    /// <returns>保存是否成功</returns>
    public bool SaveOnDrag()
    {
        // 检查是否正在保存中，避免重复保存
        if (isSaving)
        {
            if (showSaveLog)
                Debug.LogWarning("[InventorySaveManager] 正在保存中，跳过拖拽触发保存");
            return false;
        }

        // 检查是否启用自动保存
        if (!enableAutoSave)
        {
            if (showSaveLog)
                Debug.LogWarning("[InventorySaveManager] 自动保存已禁用，跳过拖拽触发保存");
            return false;
        }

        try
        {
            if (showSaveLog)
                Debug.Log("[InventorySaveManager] 拖拽触发保存开始");

            // 使用默认文件名进行保存
            bool saveResult = SaveInventory(defaultSaveFileName);
            
            if (saveResult && showSaveLog)
                Debug.Log("[InventorySaveManager] 拖拽触发保存完成");
            else if (!saveResult)
                Debug.LogError("[InventorySaveManager] 拖拽触发保存失败");
                
            return saveResult;
        }
        catch (Exception e)
        {
            Debug.LogError($"[InventorySaveManager] 拖拽触发保存时发生异常: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// 收集所有背包数据
    /// </summary>
    /// <returns>完整的背包保存数据</returns>
    private InventorySaveData CollectInventoryData()
    {
        InventorySaveData saveData = new InventorySaveData();

        // 收集所有注册网格的数据
        foreach (var kvp in registeredGrids)
        {
            string gridName = kvp.Key;
            ItemGrid grid = kvp.Value;

            if (grid != null)
            {
                try
                {
                    GridSaveData gridData = CollectGridData(grid, gridName);
                    if (gridData != null)
                    {
                        saveData.grids.Add(gridData);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[InventorySaveManager] 收集网格数据时发生异常 (网格: {gridName}): {e.Message}");
                }
            }
            else
            {
                Debug.LogWarning($"[InventorySaveManager] 注册的网格 {gridName} 为null，跳过收集");
            }
        }

        return saveData;
    }

    /// <summary>
    /// 收集单个网格的数据 - 更新以支持新字段
    /// </summary>
    /// <param name="grid">目标网格</param>
    /// <param name="gridName">网格名称</param>
    /// <returns>网格保存数据</returns>
    private GridSaveData CollectGridData(ItemGrid grid, string gridName)
    {
        // 使用新的构造函数创建GridSaveData，包含所有新字段
        GridSaveData gridData = new GridSaveData(grid, gridName);
        HashSet<Item> processedItems = new HashSet<Item>(); // 防止重复处理

        // 遍历网格中的所有物品
        for (int x = 0; x < grid.CurrentWidth; x++)
        {
            for (int y = 0; y < grid.CurrentHeight; y++)
            {
                Item item = grid.GetItemAt(x, y);
                if (item != null && !processedItems.Contains(item))
                {
                    // 使用物品自身存储的网格位置，而不是遍历坐标
                    Vector2Int actualPosition = item.OnGridPosition;

                    // 验证这是物品的起始位置（左上角）
                    if (actualPosition.x == x && actualPosition.y == y)
                    {
                        ItemDataReader itemReader = item.GetComponent<ItemDataReader>();
                        if (itemReader != null && itemReader.ItemData != null)
                        {
                            try
                            {
                                // 使用物品的实际网格位置
                                ItemSaveData itemSaveData = new ItemSaveData(itemReader, actualPosition);

                                // 验证ItemSaveData是否创建成功（检查是否为默认的错误值）
                                if (itemSaveData.itemID != "unknown")
                                {
                                    // 如果是容器类物品，递归保存容器内容
                                    if (itemReader.ItemData.IsContainer())
                                    {
                                        // itemSaveData.containerItems = CollectContainerItems(item);
                                    }

                                    gridData.items.Add(itemSaveData);
                                    processedItems.Add(item); // 标记已处理

                                    if (showSaveLog)
                                        Debug.Log($"[InventorySaveManager] 保存物品 {itemReader.ItemData.id} 位置: {actualPosition} 到网格 {gridData.gridGUID}");
                                }
                                else
                                {
                                    Debug.LogWarning($"[InventorySaveManager] 物品 {item.name} 创建ItemSaveData失败，跳过保存");
                                }
                            }
                            catch (Exception e)
                            {
                                Debug.LogError($"[InventorySaveManager] 保存物品 {item.name} 时发生异常: {e.Message}");
                            }
                        }
                        else
                        {
                            if (itemReader == null)
                                Debug.LogWarning($"[InventorySaveManager] 物品 {item.name} 缺少ItemDataReader组件，跳过保存");
                            else if (itemReader.ItemData == null)
                                Debug.LogWarning($"[InventorySaveManager] 物品 {item.name} 的ItemDataReader.ItemData为null，跳过保存");
                        }
                    }
                }
            }
        }

        if (showSaveLog)
            Debug.Log($"[InventorySaveManager] {gridData.GetStatistics()}");

        return gridData;
    }

    /// <summary>
    /// 收集装备数据
    /// </summary>
    /// <param name="saveData">保存数据对象</param>
    private void CollectEquipmentData(InventorySaveData saveData)
    {
        // 装备系统待实现
        Debug.Log("装备系统待实现");
    }

    /// <summary>
    /// 计算统计信息
    /// </summary>
    /// <param name="saveData">保存数据对象</param>
    private void CalculateStatistics(InventorySaveData saveData)
    {
        saveData.totalItemCount = 0;
        saveData.categoryStats.Clear();

        // 统计网格中的物品
        foreach (var grid in saveData.grids)
        {
            foreach (var item in grid.items)
            {
                saveData.totalItemCount++;

                if (saveData.categoryStats.ContainsKey(item.categoryID))
                    saveData.categoryStats[item.categoryID]++;
                else
                    saveData.categoryStats[item.categoryID] = 1;
            }
        }

        // 统计装备中的物品
        foreach (var equippedItem in saveData.equippedItems.Values)
        {
            saveData.totalItemCount++;

            if (saveData.categoryStats.ContainsKey(equippedItem.categoryID))
                saveData.categoryStats[equippedItem.categoryID]++;
            else
                saveData.categoryStats[equippedItem.categoryID] = 1;
        }
    }

    /// <summary>
    /// 创建备份文件
    /// </summary>
    /// <param name="fileName">原文件名</param>
    private void CreateBackup(string fileName)
    {
        try
        {
            string backupFileName = fileName.Replace(".es3", "_backup.es3");

            if (ES3.FileExists(fileName))
            {
                byte[] originalData = ES3.LoadRawBytes(fileName);
                ES3.SaveRaw(originalData, backupFileName);

                if (showSaveLog)
                    Debug.Log($"[InventorySaveManager] 创建备份: {backupFileName}");
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[InventorySaveManager] 创建备份失败: {e.Message}");
        }
    }

    #endregion

    #region 加载方法

    /// <summary>
    /// 从默认文件加载背包数据
    /// </summary>
    /// <returns>加载是否成功</returns>
    public bool LoadInventory()
    {
        return LoadInventory(defaultSaveFileName);
    }

    /// <summary>
    /// 从指定文件加载背包数据
    /// </summary>
    /// <param name="fileName">文件名</param>
    /// <returns>加载是否成功</returns>
    public bool LoadInventory(string fileName)
    {
        if (isLoading)
        {
            if (showSaveLog)
                Debug.LogWarning("[InventorySaveManager] 正在加载中，跳过本次加载请求");
            return false;
        }

        if (!ES3.FileExists(fileName))
        {
            if (showSaveLog)
                Debug.LogWarning($"[InventorySaveManager] 存档文件不存在: {fileName}");
            OnLoadCompleted?.Invoke(false);
            return false;
        }

        isLoading = true;

        try
        {
            // 使用ES3加载数据
            InventorySaveData saveData = ES3.Load<InventorySaveData>("InventoryData", fileName);

            // 应用加载的数据
            ApplyInventoryData(saveData);

            if (showSaveLog)
                Debug.Log($"[InventorySaveManager] 背包数据加载成功: {fileName}");

            // 触发加载完成事件
            OnLoadCompleted?.Invoke(true);

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[InventorySaveManager] 加载失败: {e.Message}");
            OnLoadCompleted?.Invoke(false);
            return false;
        }
        finally
        {
            isLoading = false;
        }
    }

    /// <summary>
    /// 应用加载的背包数据
    /// </summary>
    /// <param name="saveData">加载的保存数据</param>
    private void ApplyInventoryData(InventorySaveData saveData)
    {
        // 清空所有网格
        ClearAllGrids();

        // 恢复网格数据
        foreach (var gridData in saveData.grids)
        {
            ApplyGridData(gridData);
        }

        // 恢复装备数据
        ApplyEquipmentData(saveData.equippedItems);

        if (showSaveLog)
            Debug.Log($"[InventorySaveManager] 恢复了 {saveData.totalItemCount} 个物品");
    }

    /// <summary>
    /// 清空所有注册的网格 - 更新以使用CurrentWidth/Height
    /// </summary>
    private void ClearAllGrids()
    {
        foreach (var grid in registeredGrids.Values)
        {
            if (grid != null)
            {
                // 清空网格中的所有物品
                for (int x = 0; x < grid.CurrentWidth; x++)
                {
                    for (int y = 0; y < grid.CurrentHeight; y++)
                    {
                        Item item = grid.GetItemAt(x, y);
                        if (item != null)
                        {
                            grid.PickUpItem(x, y);
                            Destroy(item.gameObject);
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// 应用单个网格的数据 - 更新以支持GUID查找
    /// </summary>
    /// <param name="gridData">网格保存数据</param>
    private void ApplyGridData(GridSaveData gridData)
    {
        ItemGrid grid = null;
        
        // 优先通过GUID查找网格
        if (!string.IsNullOrEmpty(gridData.gridGUID) && registeredGrids.ContainsKey(gridData.gridGUID))
        {
            grid = registeredGrids[gridData.gridGUID];
        }
        // 如果GUID查找失败，尝试通过名称查找
        else if (!string.IsNullOrEmpty(gridData.gridName) && registeredGrids.ContainsKey(gridData.gridName))
        {
            grid = registeredGrids[gridData.gridName];
        }
        
        if (grid == null)
        {
            Debug.LogWarning($"[InventorySaveManager] 未找到网格: GUID={gridData.gridGUID}, Name={gridData.gridName}");
            return;
        }

        // 恢复网格配置（如果需要）
        if (grid.GridType != gridData.gridType)
        {
            if (showSaveLog)
                Debug.Log($"[InventorySaveManager] 更新网格类型: {grid.GridType} -> {gridData.gridType}");
            grid.GridType = gridData.gridType;
        }
        
        // 恢复网格中的物品
        foreach (var itemData in gridData.items)
        {
            RestoreItem(grid, itemData);
        }
        
        if (showSaveLog)
            Debug.Log($"[InventorySaveManager] 恢复网格完成: {gridData.GetStatistics()}");
    }

    /// <summary>
    /// 在网格中恢复物品
    /// </summary>
    /// <param name="grid">目标网格</param>
    /// <param name="itemData">物品保存数据</param>
    private void RestoreItem(ItemGrid grid, ItemSaveData itemData)
    {
        // 从数据库获取物品SO数据
        ItemDataSO itemSO = GetItemDataByID(itemData.itemID);
        if (itemSO == null)
        {
            Debug.LogWarning($"[InventorySaveManager] 未找到物品数据: {itemData.itemID}");
            return;
        }

        // 根据物品类别和ID加载对应的预制件
        GameObject itemPrefab = LoadItemPrefabByCategory(itemSO.category, itemData.itemID);
        if (itemPrefab == null)
        {
            Debug.LogWarning($"[InventorySaveManager] 未找到物品预制体: {itemData.itemID}");
            return;
        }

        GameObject itemObject = Instantiate(itemPrefab, grid.transform);
        Item item = itemObject.GetComponent<Item>();

        if (item != null)
        {
            // 设置物品数据
            ItemDataReader itemReader = item.GetComponent<ItemDataReader>();
            if (itemReader != null)
            {
                // 设置物品数据SO
                itemReader.SetItemData(itemSO);

                // 恢复运行时数据
                itemReader.SetStack(itemData.stackCount);

                // 更新UI显示
                itemReader.UpdateUI();
            }

            // 放置到网格中
            bool placed = grid.PlaceItem(item, itemData.gridPosition.x, itemData.gridPosition.y);

            if (!placed)
            {
                Debug.LogWarning($"[InventorySaveManager] 无法放置物品 {itemData.itemID} 到位置 {itemData.gridPosition}");
                Destroy(itemObject);
            }
            else if (showSaveLog)
            {
                Debug.Log($"[InventorySaveManager] 恢复物品: {itemData.itemID} 到位置 {itemData.gridPosition}");
            }
        }
    }

    /// <summary>
    /// 应用装备数据
    /// </summary>
    /// <param name="equippedItems">装备数据字典</param>
    private void ApplyEquipmentData(Dictionary<string, ItemSaveData> equippedItems)
    {
        // 装备系统待实现
        Debug.Log("装备系统待实现");
    }

    /// <summary>
    /// 根据物品ID获取物品类别
    /// </summary>
    /// <param name="itemID">物品ID</param>
    /// <returns>物品类别</returns>
    private ItemCategory GetCategoryByID(string itemID)
    {
        // 将string类型的itemID转换为int
        if (!int.TryParse(itemID, out int id))
        {
            Debug.LogWarning($"[InventorySaveManager] 无效的物品ID格式: {itemID}");
            return ItemCategory.Helmet; // 默认返回第一个类别
        }

        // 根据实际数据库的ID分配规则判断类别
        if (id >= 1001 && id <= 1999) return ItemCategory.Helmet;        // 头盔: 1xxx
        if (id >= 2001 && id <= 2999) return ItemCategory.Armor;         // 护甲: 2xxx
        if (id >= 3001 && id <= 3999) return ItemCategory.TacticalRig;   // 战术背心: 3xxx
        if (id >= 4001 && id <= 4999) return ItemCategory.Backpack;      // 背包: 4xxx
        if (id >= 5001 && id <= 5999) return ItemCategory.Weapon;        // 武器: 5xxx
        if (id >= 6001 && id <= 6999) return ItemCategory.Ammunition;    // 弹药: 6xxx
        if (id >= 7001 && id <= 7999) return ItemCategory.Food;          // 食物: 7xxx
        if (id >= 8001 && id <= 8999) return ItemCategory.Drink;         // 饮料: 8xxx
        if (id >= 9001 && id <= 9999) return ItemCategory.Sedative;      // 镇静剂: 9xxx
        if (id >= 10001 && id <= 10999) return ItemCategory.Hemostatic;  // 止血剂: 10xxx
        if (id >= 11001 && id <= 11999) return ItemCategory.Healing;     // 治疗药物: 11xxx
        if (id >= 12001 && id <= 12999) return ItemCategory.Intelligence;// 情报: 12xxx
        if (id >= 13001 && id <= 13999) return ItemCategory.Currency;    // 货币: 13xxx
        if (id >= 14001 && id <= 14999) return ItemCategory.Special;    // 特殊物品: 14xxx

        // 如果无法通过ID判断，尝试从ItemDataSO获取
        ItemDataSO itemData = Resources.LoadAll<ItemDataSO>("InventorySystemResources/ItemDataSO")
            .FirstOrDefault(item => item.id == id);
        if (itemData != null)
        {
            return itemData.category;
        }

        Debug.LogWarning($"[InventorySaveManager] 无法确定物品ID {itemID} 的类别，使用默认类别");
        return ItemCategory.Helmet; // 默认类别
    }

    /// <summary>
    /// 根据物品类别获取对应的文件夹名称
    /// </summary>
    /// <param name="category">物品类别</param>
    /// <returns>文件夹名称</returns>
    private string GetCategoryFolderName(ItemCategory category)
    {
        switch (category)
        {
            case ItemCategory.Helmet:
                return "Helmet_头盔";
            case ItemCategory.Armor:
                return "Armor_护甲";
            case ItemCategory.TacticalRig:
                return "TacticalRig_战术背心";
            case ItemCategory.Backpack:
                return "Backpack_背包";
            case ItemCategory.Weapon:
                return "Weapon_武器";
            case ItemCategory.Ammunition:
                return "Ammunition_弹药";
            case ItemCategory.Food:
                return "Food_食物";
            case ItemCategory.Drink:
                return "Drink_饮料";
            case ItemCategory.Sedative:
                return "Sedative_镇静剂";
            case ItemCategory.Hemostatic:
                return "Hemostatic_止血剂";
            case ItemCategory.Healing:
                return "Healing_治疗药物";
            case ItemCategory.Intelligence:
                return "Intelligence_情报";
            case ItemCategory.Currency:
                return "Currency_货币";
            case ItemCategory.Special:
                return "Special_特殊物品";
            default:
                Debug.LogWarning($"[InventorySaveManager] 未知的物品类别: {category}");
                return "Helmet_头盔"; // 默认文件夹
        }
    }
    #endregion

    /// <summary>
    /// 根据物品类别和ID加载对应的预制件
    /// </summary>
    /// <param name="category">物品类别</param>
    /// <param name="itemID">物品ID</param>
    /// <returns>物品预制件GameObject</returns>
    private GameObject LoadItemPrefabByCategory(ItemCategory category, string itemID)
    {
        // 获取类别对应的文件夹名称
        string categoryFolder = GetCategoryFolderName(category);

        // 构建预制件路径 - 修正：使用Prefabs而不是ItemPrefabs
        string prefabPath = $"InventorySystemResources/Prefabs/{categoryFolder}/{itemID}";

        // 尝试加载预制件
        GameObject prefab = Resources.Load<GameObject>(prefabPath);

        // 如果直接路径加载失败，尝试其他可能的命名格式
        if (prefab == null)
        {
            // 尝试带类别前缀的命名格式
            string[] possibleNames = {
                $"{itemID}",
                $"Item_{itemID}",
                $"{category}_{itemID}",
                $"ItemPrefab_{itemID}"
            };

            foreach (string name in possibleNames)
            {
                // 修正：使用Prefabs而不是ItemPrefabs
                string alternatePath = $"InventorySystemResources/Prefabs/{categoryFolder}/{name}";
                prefab = Resources.Load<GameObject>(alternatePath);
                if (prefab != null)
                {
                    if (showSaveLog)
                        Debug.Log($"[InventorySaveManager] 找到预制件: {alternatePath}");
                    break;
                }
            }
        }

        // 如果仍然找不到，尝试在所有类别文件夹中搜索
        if (prefab == null)
        {
            prefab = SearchPrefabInAllCategories(itemID);
        }

        if (prefab == null && showSaveLog)
        {
            Debug.LogWarning($"[InventorySaveManager] 无法找到预制件: {itemID}, 类别: {category}");
        }

        return prefab;
    }


    /// <summary>
    /// 在所有类别文件夹中搜索预制件
    /// </summary>
    /// <param name="itemID">物品ID</param>
    /// <returns>找到的预制件，如果没找到返回null</returns>
    private GameObject SearchPrefabInAllCategories(string itemID)
    {
        // 定义所有可能的类别文件夹
        string[] categoryFolders = {
            "Helmet_头盔",
            "Armor_护甲",
            "TacticalRig_战术背心",
            "Backpack_背包",
            "Weapon_武器",
            "Ammunition_弹药",
            "Food_食物",
            "Drink_饮料",
            "Sedative_镇静剂",
            "Hemostatic_止血剂",
            "Healing_治疗药物",
            "Intelligence_情报",
            "Currency_货币"
        };

        foreach (string folder in categoryFolders)
        {
            // 首先尝试传统命名格式
            string[] possibleNames = {
                $"{itemID}",
                $"Item_{itemID}",
                $"ItemPrefab_{itemID}"
            };

            foreach (string name in possibleNames)
            {
                string path = $"InventorySystemResources/Prefabs/{folder}/{name}";
                GameObject prefab = Resources.Load<GameObject>(path);
                if (prefab != null)
                {
                    if (showSaveLog)
                        Debug.Log($"[InventorySaveManager] 在 {folder} 中找到预制件: {name}");
                    return prefab;
                }
            }

            // 然后尝试前缀匹配
            GameObject prefabByPrefix = SearchPrefabByPrefix(folder, itemID);
            if (prefabByPrefix != null)
            {
                return prefabByPrefix;
            }
        }

        return null;
    }


    /// <summary>
    /// 根据物品ID获取物品数据SO
    /// </summary>
    /// <param name="itemID">物品ID</param>
    /// <returns>物品数据SO，如果未找到返回null</returns>
    private ItemDataSO GetItemDataByID(string itemID)
    {
        // 将string类型的itemID转换为int
        if (!int.TryParse(itemID, out int id))
        {
            Debug.LogWarning($"[InventorySaveManager] 无效的物品ID格式: {itemID}");
            return null;
        }

        // 首先根据ID获取物品类别
        ItemCategory category = GetCategoryByID(itemID);
        string categoryFolder = GetCategoryFolderName(category);

        // 修正路径：使用ItemScriptableObject而不是ItemDataSO
        string categoryPath = $"InventorySystemResources/ItemScriptableObject/{categoryFolder}";
        ItemDataSO[] categoryItems = Resources.LoadAll<ItemDataSO>(categoryPath);

        foreach (var item in categoryItems)
        {
            if (item.id == id)
            {
                if (showSaveLog)
                    Debug.Log($"[InventorySaveManager] 在类别 {category} 中找到物品数据: {itemID}");
                return item;
            }
        }

        // 如果在对应类别中未找到，则在所有ItemScriptableObject中搜索
        ItemDataSO[] allItems = Resources.LoadAll<ItemDataSO>("InventorySystemResources/ItemScriptableObject");

        foreach (var item in allItems)
        {
            if (item.id == id)
            {
                if (showSaveLog)
                    Debug.Log($"[InventorySaveManager] 在全局搜索中找到物品数据: {itemID}");
                return item;
            }
        }

        Debug.LogWarning($"[InventorySaveManager] 未找到物品数据: {itemID}");
        return null;
    }


    /// <summary>
    /// 通过ID前缀在指定文件夹中搜索预制件
    /// </summary>
    /// <param name="categoryFolder">类别文件夹名称</param>
    /// <param name="itemID">物品ID</param>
    /// <returns>找到的预制件，如果没找到返回null</returns>
    private GameObject SearchPrefabByPrefix(string categoryFolder, string itemID)
    {
        // 加载指定类别文件夹中的所有预制件
        string folderPath = $"InventorySystemResources/Prefabs/{categoryFolder}";
        GameObject[] prefabs = Resources.LoadAll<GameObject>(folderPath);

        foreach (GameObject prefab in prefabs)
        {
            // 检查预制件名称是否以物品ID开头
            if (prefab.name.StartsWith(itemID + "_") || prefab.name.StartsWith(itemID + "__"))
            {
                if (showSaveLog)
                    Debug.Log($"[InventorySaveManager] 通过前缀匹配在 {categoryFolder} 中找到预制件: {prefab.name}");
                return prefab;
            }
        }

        return null;
    }

}