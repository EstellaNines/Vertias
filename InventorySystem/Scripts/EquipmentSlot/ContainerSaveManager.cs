using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using InventorySystem.Database;

namespace InventorySystem
{
    /// <summary>
    /// 容器内容保存数据
    /// </summary>
    [System.Serializable]
    public class ContainerSaveData
    {
        public string containerItemID;              // 容器物品ID
        public string containerGlobalID;            // 容器全局唯一ID
        public EquipmentSlotType slotType;          // 装备槽类型
        public List<ItemSaveData> containerItems;  // 容器内的物品数据
        public string saveTime;                     // 保存时间

        public ContainerSaveData()
        {
            containerItems = new List<ItemSaveData>();
            saveTime = System.DateTime.Now.ToBinary().ToString();
        }

        public ContainerSaveData(string itemID, string globalID, EquipmentSlotType type, ItemGrid containerGrid)
        {
            containerItemID = itemID;
            containerGlobalID = globalID;
            slotType = type;
            containerItems = new List<ItemSaveData>();
            saveTime = System.DateTime.Now.ToBinary().ToString();

            // 收集容器网格中的所有物品
            if (containerGrid != null)
            {
                CollectContainerItems(containerGrid);
            }
        }

        /// <summary>
        /// 收集容器网格中的所有物品（采用SpawnSystem的智能检测机制）
        /// </summary>
        private void CollectContainerItems(ItemGrid containerGrid)
        {
            containerItems.Clear();

            // 检查容器网格是否已正确初始化
            if (containerGrid == null)
            {
                Debug.LogWarning("[ContainerSaveData] 容器网格为空，无法收集物品");
                return;
            }

            // 方法1：直接从网格的子对象收集物品（更可靠）
            List<Item> allItems = GetAllItemsInGrid(containerGrid);
            
            foreach (Item item in allItems)
            {
                if (item != null)
                {
                    // 获取物品的ItemDataReader组件
                    ItemDataReader itemReader = item.GetComponent<ItemDataReader>();
                    if (itemReader != null && itemReader.ItemData != null)
                    {
                        // 获取物品在网格中的起始位置（左上角位置）
                        Vector2Int itemPosition = item.OnGridPosition;
                        
                        // 验证物品尺寸和位置的合理性
                        Vector2Int itemSize = new Vector2Int(itemReader.ItemData.width, itemReader.ItemData.height);
                        if (IsValidItemPlacement(containerGrid, itemPosition, itemSize))
                        {
                            // 创建物品保存数据
                            ItemSaveData itemSaveData = new ItemSaveData
                            {
                                itemID = itemReader.ItemData.id.ToString(),
                                categoryID = (int)itemReader.ItemData.category,
                                stackCount = itemReader.currentStack,
                                durability = itemReader.currentDurability,
                                usageCount = itemReader.currentUsageCount,
                                gridPosition = itemPosition  // 使用物品的实际起始位置
                            };

                            containerItems.Add(itemSaveData);
                        }
                        else
                        {
                            Debug.LogWarning($"[ContainerSaveData] 物品 {itemReader.ItemData.itemName} 位置或尺寸异常: 位置={itemPosition}, 尺寸={itemSize}");
                        }
                    }
                }
            }

            Debug.Log($"[ContainerSaveData] 收集到 {containerItems.Count} 个容器物品");
        }
        
        /// <summary>
        /// 获取网格中的所有物品（SpawnSystem风格）
        /// </summary>
        private List<Item> GetAllItemsInGrid(ItemGrid targetGrid)
        {
            List<Item> items = new List<Item>();
            
            // 遍历网格的所有子对象
            for (int i = 0; i < targetGrid.transform.childCount; i++)
            {
                Transform child = targetGrid.transform.GetChild(i);
                Item item = child.GetComponent<Item>();
                if (item != null)
                {
                    items.Add(item);
                }
            }
            
            return items;
        }
        
        /// <summary>
        /// 验证物品放置的合理性（SpawnSystem风格）
        /// </summary>
        private bool IsValidItemPlacement(ItemGrid grid, Vector2Int position, Vector2Int itemSize)
        {
            // 边界检查
            if (position.x < 0 || position.y < 0 ||
                position.x + itemSize.x > grid.gridSizeWidth ||
                position.y + itemSize.y > grid.gridSizeHeight)
            {
                return false;
            }
            
            return true;
        }
    }

    /// <summary>
    /// 容器保存数据集合
    /// </summary>
    [System.Serializable]
    public class ContainerSaveDataCollection
    {
        public List<ContainerSaveData> containers;

        public ContainerSaveDataCollection()
        {
            containers = new List<ContainerSaveData>();
        }
    }

    /// <summary>
    /// 容器保存管理器
    /// 负责管理容器（如背包）内物品的持久化
    /// </summary>
    public class ContainerSaveManager : MonoBehaviour
    {
        [Header("ES3 保存设置")]
        [FieldLabel("容器数据文件名")]
        [Tooltip("用于保存容器数据的ES3文件名")]
        public string containerSaveFileName = "ContainerData.es3";
        
        [FieldLabel("启用调试日志")]
        [Tooltip("显示详细的保存/加载日志")]
        public bool showDebugLog = true;
        
        [FieldLabel("启用备份")]
        [Tooltip("保存时创建备份文件")]
        public bool enableBackup = true;

        private static ContainerSaveManager _instance;
        public static ContainerSaveManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    Debug.LogWarning("[ContainerSaveManager] 单例实例为null，尝试查找现有实例...");
                    _instance = FindObjectOfType<ContainerSaveManager>();
                    if (_instance == null)
                    {
                        Debug.LogWarning("[ContainerSaveManager] 未找到现有实例，创建新实例");
                        GameObject go = new GameObject("ContainerSaveManager");
                        _instance = go.AddComponent<ContainerSaveManager>();
                        DontDestroyOnLoad(go);
                    }
                    else
                    {
                        if (_instance.showDebugLog)
                            Debug.Log($"[ContainerSaveManager] 通过FindObjectOfType找到实例 - ID: {_instance.GetInstanceID()}, 缓存大小: {_instance._containerDataCache.Count}");
                    }
                }
                else
                {
                    if (_instance.showDebugLog)
                        Debug.Log($"[ContainerSaveManager] 返回现有单例实例 - ID: {_instance.GetInstanceID()}, 缓存大小: {_instance._containerDataCache.Count}");
                }
                return _instance;
            }
        }

        private const string CONTAINER_DATA_KEY = "ContainerDataCollection";
        private Dictionary<string, ContainerSaveData> _containerDataCache = new Dictionary<string, ContainerSaveData>();

        private void Awake()
        {
            Debug.Log($"[ContainerSaveManager] Awake调用 - 实例ID: {GetInstanceID()}, 现有_instance: {(_instance != null ? _instance.GetInstanceID().ToString() : "null")}");
            
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                Debug.Log($"[ContainerSaveManager] 设置为主实例 - ID: {GetInstanceID()}, GameObject: {gameObject.name}");
                LoadAllContainerData();
            }
            else if (_instance != this)
            {
                Debug.LogWarning($"[ContainerSaveManager] 发现重复实例 - 当前ID: {GetInstanceID()}, 主实例ID: {_instance.GetInstanceID()}, 销毁重复实例");
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// 保存指定容器的内容
        /// </summary>
        public void SaveContainerContent(ItemDataReader containerItem, EquipmentSlotType slotType, ItemGrid containerGrid)
        {
            if (containerItem?.ItemData == null || containerGrid == null)
            {
                Debug.LogWarning("[ContainerSaveManager] 保存容器内容失败：容器物品或网格为空");
                return;
            }

            string containerKey = GetContainerKey(containerItem, slotType);
            Debug.Log($"[ContainerSaveManager] 保存容器键值: {containerKey}");
            Debug.Log($"[ContainerSaveManager] 保存物品信息: ID={containerItem.ItemData.id}, GlobalId={containerItem.ItemData.GlobalId}, 名称={containerItem.ItemData.itemName}");
            
            ContainerSaveData saveData = new ContainerSaveData(
                containerItem.ItemData.id.ToString(),
                containerItem.ItemData.GlobalId.ToString(),
                slotType,
                containerGrid
            );

            _containerDataCache[containerKey] = saveData;
            SaveAllContainerDataToES3();

            if (showDebugLog)
                Debug.Log($"[ContainerSaveManager] 保存容器内容: {containerKey}, 物品数量: {saveData.containerItems.Count}");
        }

        /// <summary>
        /// 加载指定容器的内容
        /// </summary>
        public void LoadContainerContent(ItemDataReader containerItem, EquipmentSlotType slotType, ItemGrid containerGrid)
        {
            if (containerItem?.ItemData == null || containerGrid == null)
            {
                Debug.LogWarning("[ContainerSaveManager] 加载容器内容失败：容器物品或网格为空");
                return;
            }

            string containerKey = GetContainerKey(containerItem, slotType);
            
            if (showDebugLog)
            {
                Debug.Log($"[ContainerSaveManager] 尝试加载容器: {containerKey}");
                Debug.Log($"[ContainerSaveManager] 物品信息: ID={containerItem.ItemData.id}, GlobalId={containerItem.ItemData.GlobalId}, 名称={containerItem.ItemData.itemName}");
                Debug.Log($"[ContainerSaveManager] 缓存中的键值列表: [{string.Join(", ", _containerDataCache.Keys)}]");
            }
            
            if (_containerDataCache.TryGetValue(containerKey, out ContainerSaveData saveData))
            {
                if (showDebugLog)
                    Debug.Log($"[ContainerSaveManager] 加载容器内容: {containerKey}, 物品数量: {saveData.containerItems.Count}");
                RestoreContainerItems(saveData, containerGrid);
            }
            else
            {
                if (showDebugLog)
                    Debug.Log($"[ContainerSaveManager] 容器 {containerKey} 无保存数据");
            }
        }

        /// <summary>
        /// 恢复容器中的物品
        /// </summary>
        private void RestoreContainerItems(ContainerSaveData saveData, ItemGrid containerGrid)
        {
            if (saveData?.containerItems == null || containerGrid == null)
            {
                Debug.LogWarning("[ContainerSaveManager] 恢复容器物品失败：数据或网格为空");
                return;
            }

            // 清理容器网格中的所有现有物品
            ClearContainerGrid(containerGrid);

            Debug.Log($"[ContainerSaveManager] 开始恢复容器物品 - 容器: {saveData.containerItemID}_{saveData.containerGlobalID} ({saveData.slotType}), 物品数量: {saveData.containerItems.Count}");

            int successCount = 0;
            int failCount = 0;

            foreach (ItemSaveData itemData in saveData.containerItems)
            {
                Debug.Log($"[ContainerSaveManager] 尝试恢复物品 - ID: {itemData.itemID}, 位置: {itemData.gridPosition}, 堆叠: {itemData.stackCount}");
                
                GameObject itemInstance = LoadItemPrefab(itemData);
                if (itemInstance != null)
                {
                    ItemDataReader itemReader = itemInstance.GetComponent<ItemDataReader>();
                    Item itemComponent = itemInstance.GetComponent<Item>();

                    if (itemReader != null && itemComponent != null)
                    {
                        // 设置物品的父级为容器网格
                        itemInstance.transform.SetParent(containerGrid.transform, false);

                        // 尝试将物品放置到指定位置
                        Vector2Int gridPos = itemData.gridPosition;
                        if (containerGrid.PlaceItem(itemComponent, gridPos.x, gridPos.y))
                        {
                            // 后处理恢复的物品
                            PostProcessRestoredItem(itemInstance, itemComponent, itemData, containerGrid);
                            successCount++;
                            Debug.Log($"[ContainerSaveManager] ✅ 成功恢复物品: {itemReader.ItemData?.itemName} 到位置 ({gridPos.x}, {gridPos.y})");
                        }
                        else
                        {
                            Debug.LogWarning($"[ContainerSaveManager] ❌ 无法放置物品: {itemReader.ItemData?.itemName} 到位置 ({gridPos.x}, {gridPos.y}) - 网格可能已被占用");
                            Destroy(itemInstance);
                            failCount++;
                        }
                    }
                    else
                    {
                        Debug.LogError($"[ContainerSaveManager] 物品 {itemInstance.name} 缺少必要组件");
                        Destroy(itemInstance);
                        failCount++;
                    }
                }
                else
                {
                    Debug.LogError($"[ContainerSaveManager] 无法创建物品实例: ID={itemData.itemID}");
                    failCount++;
                }
            }

            Debug.Log($"[ContainerSaveManager] 容器恢复完成 - 成功: {successCount}, 失败: {failCount}");
        }

        /// <summary>
        /// 清理容器网格中的所有物品
        /// </summary>
        private void ClearContainerGrid(ItemGrid containerGrid)
        {
            if (containerGrid == null) return;

            Debug.Log($"[ContainerSaveManager] 清理容器网格: {containerGrid.name}");

            // 收集所有需要清理的物品（避免在遍历时修改集合）
            var itemsToRemove = new List<Item>();
            
            for (int x = 0; x < containerGrid.gridSizeWidth; x++)
            {
                for (int y = 0; y < containerGrid.gridSizeHeight; y++)
                {
                    Item item = containerGrid.GetItemAt(x, y);
                    if (item != null && !itemsToRemove.Contains(item))
                    {
                        itemsToRemove.Add(item);
                    }
                }
            }

            // 移除所有物品
            foreach (Item item in itemsToRemove)
            {
                try
                {
                    // 获取物品的网格位置
                    Vector2Int itemPos = item.OnGridPosition;
                    
                    // 从网格中移除
                    containerGrid.PickUpItem(itemPos.x, itemPos.y);
                    
                    // 销毁物品GameObject
                    if (item.gameObject != null)
                    {
                        UnityEngine.Object.Destroy(item.gameObject);
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[ContainerSaveManager] 清理物品时发生异常: {ex.Message}");
                    // 强制销毁物品GameObject
                    if (item != null && item.gameObject != null)
                    {
                        UnityEngine.Object.Destroy(item.gameObject);
                    }
                }
            }

            Debug.Log($"[ContainerSaveManager] 已清理 {itemsToRemove.Count} 个物品");
        }

        /// <summary>
        /// 从预制体加载物品实例
        /// 恢复原来的预制体加载机制以确保完整的组件配置
        /// </summary>
        private GameObject LoadItemPrefab(ItemSaveData itemData)
        {
            if (showDebugLog)
                Debug.Log($"[ContainerSaveManager] 开始加载物品预制体 - ID={itemData.itemID}, 类别={itemData.categoryID}, 堆叠={itemData.stackCount}");

            // 根据类别ID确定预制体文件夹
            ItemCategory category = (ItemCategory)itemData.categoryID;
            string categoryFolder = GetCategoryFolderName(category);
            
            if (string.IsNullOrEmpty(categoryFolder))
            {
                Debug.LogError($"[ContainerSaveManager] 未知的物品类别: {category}");
                return null;
            }

            // 构建预制体路径并加载
            string prefabPath = $"InventorySystemResources/Prefabs/{categoryFolder}";
            GameObject[] prefabs = Resources.LoadAll<GameObject>(prefabPath);
            
            if (showDebugLog)
                Debug.Log($"[ContainerSaveManager] 在路径 {prefabPath} 中找到 {prefabs.Length} 个预制体");

            // 查找匹配的预制体
            GameObject targetPrefab = null;
            foreach (GameObject prefab in prefabs)
            {
                if (prefab.name.StartsWith(itemData.itemID + "_"))
                {
                    targetPrefab = prefab;
                    if (showDebugLog)
                        Debug.Log($"[ContainerSaveManager] 通过前缀匹配找到预制体: {prefab.name}");
                    break;
                }
            }

            if (targetPrefab == null)
            {
                Debug.LogError($"[ContainerSaveManager] 未找到物品预制体: ID={itemData.itemID}, Category={category}");
                return null;
            }

            // 实例化预制体
            GameObject itemInstance = Instantiate(targetPrefab);
            
            if (itemInstance != null)
            {
                if (showDebugLog)
                    Debug.Log($"[ContainerSaveManager] ✅ 预制体实例化成功: {itemInstance.name}");

                // 恢复物品的运行时数据
                ItemDataReader itemReader = itemInstance.GetComponent<ItemDataReader>();
                if (itemReader != null)
                {
                    itemReader.currentStack = itemData.stackCount;
                    itemReader.currentDurability = (int)itemData.durability;
                    itemReader.currentUsageCount = itemData.usageCount;
                    
                    if (showDebugLog)
                        Debug.Log($"[ContainerSaveManager] 恢复物品运行时数据: 堆叠={itemData.stackCount}, 耐久={itemData.durability}, 使用次数={itemData.usageCount}");
                }
                else
                {
                    Debug.LogWarning($"[ContainerSaveManager] 物品 {itemInstance.name} 缺少 ItemDataReader 组件");
                }
            }
            else
            {
                Debug.LogError($"[ContainerSaveManager] ❌ 预制体实例化失败: {targetPrefab.name}");
            }

            return itemInstance;
        }

        /// <summary>
        /// 根据物品类别获取对应的预制体文件夹名称
        /// </summary>
        private string GetCategoryFolderName(ItemCategory category)
        {
            switch (category)
            {
                case ItemCategory.Weapon:
                    return "Weapon_武器";
                case ItemCategory.Ammunition:
                    return "Ammunition_弹药";
                case ItemCategory.Armor:
                    return "Armor_护甲";
                case ItemCategory.Helmet:
                    return "Helmet_头盔";
                case ItemCategory.TacticalRig:
                    return "TacticalRig_战术背心";
                case ItemCategory.Backpack:
                    return "Backpack_背包";
                case ItemCategory.Healing:
                    return "Healing_治疗药物";
                case ItemCategory.Food:
                    return "Food_食物";
                case ItemCategory.Drink:
                    return "Drink_饮料";
                case ItemCategory.Hemostatic:
                    return "Hemostatic_止血剂";
                case ItemCategory.Sedative:
                    return "Sedative_镇静剂";
                case ItemCategory.Intelligence:
                    return "Intelligence_情报";
                case ItemCategory.Currency:
                    return "Currency_货币";
                case ItemCategory.Special:
                    return "Special";
                default:
                    Debug.LogWarning($"[ContainerSaveManager] 未知的物品类别: {category}");
                    return null;
            }
        }

        /// <summary>
        /// 通过物品ID查找ItemDataSO（遍历所有物品）
        /// 注意：这个方法保留用于测试器，但在正常运行时不使用
        /// </summary>
        private ItemDataSO FindItemByID(int itemId)
        {
            if (!ItemDatabase.Instance.IsInitialized)
            {
                return null;
            }
            
            var allItems = ItemDatabase.Instance.GetAllItems();
            foreach (var item in allItems)
            {
                if (item.id == itemId)
                {
                    return item;
                }
            }
            return null;
        }

        /// <summary>
        /// 对恢复的物品进行后处理
        /// </summary>
        private void PostProcessRestoredItem(GameObject itemInstance, Item itemComponent, ItemSaveData itemData, ItemGrid containerGrid)
        {
            // 设置物品的网格引用和位置
            itemComponent.OnGridReference = containerGrid;
            itemComponent.OnGridPosition = itemData.gridPosition;

            // 调整物品的视觉大小以适配网格
            itemComponent.AdjustVisualSizeForGrid();

            // 确保DraggableItem组件已就绪
            DraggableItem draggableItem = itemInstance.GetComponent<DraggableItem>();
            if (draggableItem != null)
            {
                Debug.Log($"[ContainerSaveManager] DraggableItem组件已就绪");
            }

            // 更新物品的堆叠显示
            UpdateItemStackDisplay(itemInstance, itemData.stackCount);

            // 确保物品在正确的渲染层级
            RectTransform rectTransform = itemInstance.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.SetAsLastSibling();
            }

            Debug.Log($"[ContainerSaveManager] 物品后处理完成: {itemInstance.name}");
        }

        /// <summary>
        /// 更新物品堆叠显示
        /// </summary>
        private void UpdateItemStackDisplay(GameObject itemInstance, int stackCount)
        {
            // 如果有堆叠显示的Text组件，更新它
            Transform textTransform = itemInstance.transform.Find("ItemText");
            if (textTransform != null)
            {
                var textComponent = textTransform.GetComponent<TMPro.TextMeshProUGUI>();
                if (textComponent != null && stackCount > 1)
                {
                    textComponent.text = stackCount.ToString();
                }
            }
        }

        /// <summary>
        /// 生成容器唯一标识符
        /// </summary>
        private string GetContainerKey(ItemDataReader containerItem, EquipmentSlotType slotType)
        {
            return $"{slotType}_{containerItem.ItemData.id}_{containerItem.ItemData.GlobalId}";
        }

        /// <summary>
        /// 保存所有容器数据到ES3文件
        /// </summary>
        private void SaveAllContainerDataToES3()
        {
            try
            {
                ContainerSaveDataCollection collection = new ContainerSaveDataCollection();
                collection.containers = _containerDataCache.Values.ToList();

                // 创建备份（如果启用）
                if (enableBackup && ES3.FileExists(containerSaveFileName))
                {
                    CreateBackupFile();
                }

                // 使用ES3保存数据
                ES3.Save(CONTAINER_DATA_KEY, collection, containerSaveFileName);

                if (showDebugLog)
                    Debug.Log($"[ContainerSaveManager] 保存了 {collection.containers.Count} 个容器数据到ES3文件: {containerSaveFileName}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ContainerSaveManager] ES3保存失败: {e.Message}");
            }
        }

        /// <summary>
        /// 从ES3文件加载所有容器数据
        /// </summary>
        private void LoadAllContainerData()
        {
            _containerDataCache.Clear();

            if (showDebugLog)
                Debug.Log($"[ContainerSaveManager] 开始从ES3文件加载容器数据: {containerSaveFileName}");

            if (ES3.FileExists(containerSaveFileName))
            {
                try
                {
                    if (ES3.KeyExists(CONTAINER_DATA_KEY, containerSaveFileName))
                    {
                        ContainerSaveDataCollection collection = ES3.Load<ContainerSaveDataCollection>(CONTAINER_DATA_KEY, containerSaveFileName);
                        
                        if (showDebugLog)
                            Debug.Log($"[ContainerSaveManager] ES3加载成功，collection是否为null: {collection == null}");
                        
                        if (collection?.containers != null)
                        {
                            if (showDebugLog)
                                Debug.Log($"[ContainerSaveManager] collection.containers数量: {collection.containers.Count}");
                            
                            foreach (ContainerSaveData saveData in collection.containers)
                            {
                                string key = $"{saveData.slotType}_{saveData.containerItemID}_{saveData.containerGlobalID}";
                                _containerDataCache[key] = saveData;
                                
                                if (showDebugLog)
                                    Debug.Log($"[ContainerSaveManager] 已加载容器数据到缓存: {key}, 物品数量: {saveData.containerItems?.Count ?? 0}");
                            }
                        }
                        else
                        {
                            Debug.LogWarning($"[ContainerSaveManager] collection或collection.containers为null");
                        }
                    }
                    else
                    {
                        if (showDebugLog)
                            Debug.Log($"[ContainerSaveManager] ES3文件中没有找到容器数据键: {CONTAINER_DATA_KEY}");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[ContainerSaveManager] ES3加载容器数据失败: {e.Message}");
                    Debug.LogError($"[ContainerSaveManager] 异常堆栈: {e.StackTrace}");
                }
            }
            else
            {
                if (showDebugLog)
                    Debug.Log($"[ContainerSaveManager] ES3文件不存在: {containerSaveFileName} (首次运行是正常的)");
            }

            if (showDebugLog)
            {
                Debug.Log($"[ContainerSaveManager] 从ES3文件加载了 {_containerDataCache.Count} 个容器数据");
                Debug.Log($"[ContainerSaveManager] 缓存中的所有键值: [{string.Join(", ", _containerDataCache.Keys)}]");
            }
        }

        /// <summary>
        /// 创建备份文件
        /// </summary>
        private void CreateBackupFile()
        {
            try
            {
                string backupFileName = containerSaveFileName.Replace(".es3", "_backup.es3");
                
                if (ES3.FileExists(containerSaveFileName))
                {
                    byte[] originalData = ES3.LoadRawBytes(containerSaveFileName);
                    ES3.SaveRaw(originalData, backupFileName);
                    
                    if (showDebugLog)
                        Debug.Log($"[ContainerSaveManager] 创建备份文件: {backupFileName}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[ContainerSaveManager] 创建备份文件失败: {e.Message}");
            }
        }

        /// <summary>
        /// 清除所有容器保存数据
        /// </summary>
        public void ClearAllContainerData()
        {
            _containerDataCache.Clear();
            
            // 删除ES3文件
            if (ES3.FileExists(containerSaveFileName))
            {
                ES3.DeleteFile(containerSaveFileName);
            }
            
            // 同时清理旧的PlayerPrefs数据（迁移兼容性）
            if (PlayerPrefs.HasKey("ContainerSaveData"))
            {
                PlayerPrefs.DeleteKey("ContainerSaveData");
                PlayerPrefs.Save();
            }
            
            if (showDebugLog)
                Debug.Log("[ContainerSaveManager] 清除了所有容器保存数据（ES3文件和旧PlayerPrefs数据）");
        }

        /// <summary>
        /// 手动保存容器数据（供外部调用）
        /// </summary>
        public void ManualSave()
        {
            SaveAllContainerDataToES3();
        }

        /// <summary>
        /// 手动加载容器数据（供外部调用）
        /// </summary>
        public void ManualLoad()
        {
            LoadAllContainerData();
        }

        /// <summary>
        /// 获取容器数据统计信息
        /// </summary>
        public string GetContainerStats()
        {
            int totalContainers = _containerDataCache.Count;
            int totalItems = 0;
            
            foreach (var container in _containerDataCache.Values)
            {
                totalItems += container.containerItems?.Count ?? 0;
            }
            
            return $"容器数量: {totalContainers}, 总物品数量: {totalItems}";
        }
    }
}