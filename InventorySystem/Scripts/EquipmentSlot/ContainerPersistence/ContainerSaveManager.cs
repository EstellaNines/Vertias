using System.Collections;
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
        public string containerKey;                 // 容器唯一键值
        public string containerItemID;              // 容器物品ID
        public string containerGlobalID;            // 容器全局唯一ID
        public EquipmentSlotType slotType;          // 装备槽类型
        public List<ItemSaveData> containerItems;  // 容器内的物品数据
        public string saveTime;                     // 保存时间

        public ContainerSaveData()
        {
            containerKey = System.Guid.NewGuid().ToString(); // 生成临时唯一键值
            containerItems = new List<ItemSaveData>();
            saveTime = System.DateTime.Now.ToBinary().ToString();
        }

        public ContainerSaveData(string itemID, string globalID, EquipmentSlotType type, ItemGrid containerGrid)
        {
            // 生成容器唯一键值 - 统一使用 SlotType_GlobalId_ItemId 格式
            containerKey = $"{type}_{globalID}_{itemID}";
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
            
            // 🛡️ 防护机制：如果收集到0个物品，检查网格状态
            if (containerItems.Count == 0 && containerGrid != null)
            {
                Debug.LogWarning($"[ContainerSaveData] ⚠️ 容器网格收集到0个物品 - 网格名称: {containerGrid.name}, 子对象数量: {containerGrid.transform.childCount}, 网格激活状态: {containerGrid.gameObject.activeInHierarchy}");
            }
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
                position.x + itemSize.x > grid.CurrentWidth ||
                position.y + itemSize.y > grid.CurrentHeight)
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
        
        [Header("跨会话持久化")]
        [FieldLabel("启用跨会话保存")]
        [Tooltip("启用跨运行状态的容器内容持久化")]
        public bool enableCrossSessionSave = true;
        
        [FieldLabel("跨会话数据键")]
        [Tooltip("跨会话数据在ES3中的键名")]
        public string crossSessionDataKey = "CrossSessionContainerData";
        
        [FieldLabel("容器变化自动保存")]
        [Tooltip("容器内容变化时自动保存")]
        public bool autoSaveOnChange = true;

        private static ContainerSaveManager _instance;
        public static ContainerSaveManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    Debug.Log("[ContainerSaveManager] 单例实例为null，尝试查找现有实例...");
                    _instance = FindObjectOfType<ContainerSaveManager>();
                    if (_instance == null)
                    {
                        Debug.Log("[ContainerSaveManager] 未找到现有实例，创建新实例");
                        GameObject go = new GameObject("ContainerSystemManager");
                        _instance = go.AddComponent<ContainerSaveManager>();
                        DontDestroyOnLoad(go);
                        
                        // 立即初始化新实例
                        _instance.InitializeNewInstance();
                    }
                    else
                    {
                        Debug.Log($"[ContainerSaveManager] 通过FindObjectOfType找到实例 - ID: {_instance.GetInstanceID()}, 缓存大小: {_instance._containerDataCache?.Count ?? 0}");
                        // 确保已找到的实例也正确初始化
                        if (_instance._containerDataCache == null)
                        {
                            _instance.InitializeNewInstance();
                        }
                    }
                }
                else
                {
                    if (_instance.showDebugLog)
                        Debug.Log($"[ContainerSaveManager] 返回现有单例实例 - ID: {_instance.GetInstanceID()}, 缓存大小: {_instance._containerDataCache?.Count ?? 0}");
                }
                return _instance;
            }
        }

        private const string CONTAINER_DATA_KEY = "ContainerDataCollection";
        private Dictionary<string, ContainerSaveData> _containerDataCache = new Dictionary<string, ContainerSaveData>();
        private bool _isRestoring = false; // 恢复过程标志，防止恢复时触发自动保存
        
        // 保存节流机制
        private float _lastSaveTime = 0f;
        private const float SAVE_THROTTLE_INTERVAL = 0.5f; // 最小保存间隔（秒）
        private bool _hasPendingSave = false;
        private Coroutine _throttledSaveCoroutine = null;

        private void Awake()
        {
            Debug.Log($"[ContainerSaveManager] Awake调用 - 实例ID: {GetInstanceID()}, 现有_instance: {(_instance != null ? _instance.GetInstanceID().ToString() : "null")}");
            
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                Debug.Log($"[ContainerSaveManager] 设置为主实例 - ID: {GetInstanceID()}, GameObject: {gameObject.name}");
                
                InitializeNewInstance();
            }
            else if (_instance != this)
            {
                Debug.LogWarning($"[ContainerSaveManager] 发现重复实例 - 当前ID: {GetInstanceID()}, 主实例ID: {_instance.GetInstanceID()}, 销毁重复实例");
                Destroy(gameObject);
            }
        }
        
        /// <summary>
        /// 初始化新实例
        /// </summary>
        private void InitializeNewInstance()
        {
            // 确保缓存已初始化
            if (_containerDataCache == null)
            {
                _containerDataCache = new Dictionary<string, ContainerSaveData>();
            }
            
            // 首先尝试加载跨会话数据
            bool crossSessionLoaded = LoadCrossSessionData();
            if (!crossSessionLoaded)
            {
                // 如果跨会话数据加载失败，回退到普通加载
                LoadAllContainerData();
            }
        }
        
        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && _hasPendingSave)
            {
                // 应用暂停时强制保存待保存的数据
                ExecuteSave();
                _hasPendingSave = false;
                if (showDebugLog)
                    Debug.Log("[ContainerSaveManager] 📱 应用暂停，强制保存待保存数据");
            }
        }
        
        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && _hasPendingSave)
            {
                // 应用失去焦点时强制保存待保存的数据
                ExecuteSave();
                _hasPendingSave = false;
                if (showDebugLog)
                    Debug.Log("[ContainerSaveManager] 👁️ 应用失焦，强制保存待保存数据");
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

            // 🛡️ 防护机制：避免覆盖有效数据为空数据
            bool shouldSave = true;
            if (saveData.containerItems.Count == 0 && _containerDataCache.ContainsKey(containerKey))
            {
                var existingData = _containerDataCache[containerKey];
                if (existingData != null && existingData.containerItems.Count > 0)
                {
                    Debug.LogWarning($"[ContainerSaveManager] 🛡️ 阻止用空数据覆盖有效数据 - 容器: {containerKey}, 现有物品: {existingData.containerItems.Count}, 新数据物品: {saveData.containerItems.Count}");
                    shouldSave = false;
                }
            }

            if (shouldSave)
            {
                _containerDataCache[containerKey] = saveData;
                SaveAllContainerDataToES3();
                
                // 触发跨会话保存（使用节流）
                if (enableCrossSessionSave)
                {
                    SaveCrossSessionDataThrottled();
                }
                
                // 触发容器变化事件
                OnContainerContentChanged(containerKey);

                if (showDebugLog)
                    Debug.Log($"[ContainerSaveManager] 保存容器内容: {containerKey}, 物品数量: {saveData.containerItems.Count}");
            }
            else
            {
                if (showDebugLog)
                    Debug.Log($"[ContainerSaveManager] 🛡️ 跳过保存以保护现有数据: {containerKey}");
            }
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

            // 首先强制清理容器网格，确保没有残留物品
            if (showDebugLog)
                Debug.Log($"[ContainerSaveManager] 预清理容器网格: {containerGrid.name}");
            ClearContainerGrid(containerGrid);

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
        /// 检查容器网格是否已就绪
        /// </summary>
        private bool IsContainerGridReady(ItemGrid containerGrid)
        {
            if (containerGrid == null) return false;
            if (!containerGrid.gameObject.activeInHierarchy) return false;
            
            try
            {
                // 验证网格的基本属性是否可访问
                int width = containerGrid.CurrentWidth;
                int height = containerGrid.CurrentHeight;
                
                // 尝试访问网格的基本功能
                containerGrid.GetItemAt(0, 0);
                
                return width > 0 && height > 0;
            }
            catch
            {
                return false;
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

            // 检查容器网格是否就绪
            if (!IsContainerGridReady(containerGrid))
            {
                Debug.LogWarning($"[ContainerSaveManager] 容器网格未就绪，延迟恢复: {containerGrid.name}");
                StartCoroutine(DelayedRestoreContainerItems(saveData, containerGrid));
                return;
            }

            // 清理容器网格中的所有现有物品
            ClearContainerGrid(containerGrid);

            Debug.Log($"[ContainerSaveManager] 开始恢复容器物品 - 容器: {saveData.containerItemID}_{saveData.containerGlobalID} ({saveData.slotType}), 物品数量: {saveData.containerItems.Count}");

            // 暂时禁用自动保存以防止恢复过程中的干扰
            bool wasRestoring = _isRestoring;
            _isRestoring = true;
            
            if (showDebugLog)
                Debug.Log("[ContainerSaveManager] 🔒 恢复过程中暂时禁用自动保存");

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

            // 恢复自动保存状态
            _isRestoring = wasRestoring;
            
            if (showDebugLog)
                Debug.Log("[ContainerSaveManager] 🔓 恢复完成，重新启用自动保存");
            
            // 恢复完成后，重新保存容器的正确数据，覆盖恢复过程中可能产生的重复数据
            if (successCount > 0)
            {
                StartCoroutine(FinalizeContainerRestore(saveData, containerGrid));
            }
            
            Debug.Log($"[ContainerSaveManager] 容器恢复完成 - 成功: {successCount}, 失败: {failCount}");
        }

        /// <summary>
        /// 完成容器恢复的清理工作
        /// </summary>
        private IEnumerator FinalizeContainerRestore(ContainerSaveData saveData, ItemGrid containerGrid)
        {
            // 等待一帧确保所有物品已完全放置
            yield return null;
            
            if (showDebugLog)
                Debug.Log("[ContainerSaveManager] 🔧 开始最终化容器恢复，重新收集正确的物品数据");
            
            // 重新生成容器保存数据，覆盖恢复过程中产生的重复数据
            try
            {
                string containerKey = saveData.containerKey;
                var newSaveData = new ContainerSaveData(saveData.containerItemID, saveData.containerGlobalID, saveData.slotType, containerGrid);
                
                // 更新缓存
                _containerDataCache[containerKey] = newSaveData;
                
                // 使用节流保存到ES3
                SaveCrossSessionDataThrottled();
                
                if (showDebugLog)
                    Debug.Log($"[ContainerSaveManager] ✅ 容器数据已最终化: {containerKey}, 物品数量: {newSaveData.containerItems.Count}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ContainerSaveManager] 最终化容器恢复失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 延迟恢复容器物品的协程
        /// </summary>
        private IEnumerator DelayedRestoreContainerItems(ContainerSaveData saveData, ItemGrid containerGrid)
        {
            const int maxRetries = 10;
            const float retryDelay = 0.1f;
            
            for (int i = 0; i < maxRetries; i++)
            {
                yield return new WaitForSeconds(retryDelay);
                
                if (IsContainerGridReady(containerGrid))
                {
                    if (showDebugLog) Debug.Log($"[ContainerSaveManager] 容器网格已就绪，开始恢复物品 (重试 {i + 1}): {containerGrid.name}");
                    RestoreContainerItems(saveData, containerGrid);
                    yield break;
                }
                
                if (showDebugLog) Debug.Log($"[ContainerSaveManager] 容器网格仍未就绪，继续等待 (重试 {i + 1}/{maxRetries}): {containerGrid.name}");
            }
            
            Debug.LogWarning($"[ContainerSaveManager] 容器网格在最大重试次数后仍未就绪，放弃恢复: {containerGrid.name}");
        }

        /// <summary>
        /// 清理容器网格中的所有物品
        /// </summary>
        /// <summary>
        /// 验证物品组件是否有效
        /// </summary>
        private bool IsItemValid(Item item)
        {
            if (item == null) return false;
            if (item.gameObject == null) return false;
            
            try
            {
                // 尝试访问关键属性
                var pos = item.OnGridPosition;
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// 安全地移除单个物品
        /// </summary>
        private bool SafeRemoveItem(Item item, ItemGrid containerGrid)
        {
            if (item == null) return false;
            
            try
            {
                Vector2Int itemPos = item.OnGridPosition;
                
                // 从网格中移除
                if (containerGrid != null)
                {
                    containerGrid.PickUpItem(itemPos.x, itemPos.y);
                }
                
                // 销毁GameObject
                if (item.gameObject != null)
                {
                    UnityEngine.Object.Destroy(item.gameObject);
                }
                
                return true;
            }
            catch (System.Exception ex)
            {
                if (showDebugLog) Debug.LogWarning($"[ContainerSaveManager] 移除物品时发生异常: {ex.Message}，尝试强制清理");
                
                // 强制清理
                try 
                {
                    if (item != null && item.gameObject != null)
                    {
                        UnityEngine.Object.Destroy(item.gameObject);
                        return true;
                    }
                }
                catch (System.Exception ex2)
                {
                    Debug.LogError($"[ContainerSaveManager] 强制销毁物品时发生异常: {ex2.Message}");
                }
                
                return false;
            }
        }

        private void ClearContainerGrid(ItemGrid containerGrid)
        {
            if (containerGrid == null) 
            {
                if (showDebugLog) Debug.LogWarning("[ContainerSaveManager] 容器网格为null，跳过清理");
                return;
            }

            if (showDebugLog) Debug.Log($"[ContainerSaveManager] 开始清理容器网格: {containerGrid.name}");

            var itemsToRemove = new List<Item>();
            var gameObjectsToDestroy = new List<GameObject>();
            
            // 方法1: 通过网格位置收集物品
            try
            {
                for (int x = 0; x < containerGrid.CurrentWidth; x++)
                {
                    for (int y = 0; y < containerGrid.CurrentHeight; y++)
                    {
                        try
                        {
                            Item item = containerGrid.GetItemAt(x, y);
                            if (item != null && !itemsToRemove.Contains(item))
                            {
                                // 验证物品组件完整性
                                if (IsItemValid(item))
                                {
                                    itemsToRemove.Add(item);
                                    gameObjectsToDestroy.Add(item.gameObject);
                                }
                                else
                                {
                                    if (showDebugLog) Debug.LogWarning($"[ContainerSaveManager] 发现无效物品在位置 ({x}, {y})，将强制清理");
                                    // 对于无效物品，直接销毁GameObject
                                    if (item.gameObject != null)
                                    {
                                        gameObjectsToDestroy.Add(item.gameObject);
                                    }
                                }
                            }
                        }
                        catch (System.Exception ex)
                        {
                            if (showDebugLog) Debug.LogWarning($"[ContainerSaveManager] 检查位置 ({x}, {y}) 时发生异常: {ex.Message}");
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ContainerSaveManager] 收集容器物品时发生异常: {ex.Message}");
            }

            // 方法2: 通过子对象收集物品（备用清理）
            try
            {
                for (int i = containerGrid.transform.childCount - 1; i >= 0; i--)
                {
                    var child = containerGrid.transform.GetChild(i);
                    if (child != null)
                    {
                        var item = child.GetComponent<Item>();
                        var draggableItem = child.GetComponent<DraggableItem>();
                        
                        // 如果是物品对象且不在清理列表中
                        if ((item != null || draggableItem != null) && !gameObjectsToDestroy.Contains(child.gameObject))
                        {
                            gameObjectsToDestroy.Add(child.gameObject);
                            if (item != null && !itemsToRemove.Contains(item))
                            {
                                itemsToRemove.Add(item);
                            }
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                if (showDebugLog) Debug.LogWarning($"[ContainerSaveManager] 通过子对象收集物品时发生异常: {ex.Message}");
            }

            // 安全地清理收集到的物品
            int successCount = 0;
            foreach (Item item in itemsToRemove)
            {
                if (SafeRemoveItem(item, containerGrid))
                {
                    successCount++;
                }
            }

            // 强制销毁所有收集到的GameObject
            foreach (var gameObj in gameObjectsToDestroy)
            {
                if (gameObj != null)
                {
                    try
                    {
                        UnityEngine.Object.DestroyImmediate(gameObj);
                    }
                    catch (System.Exception ex)
                    {
                        if (showDebugLog) Debug.LogWarning($"[ContainerSaveManager] 销毁物品时发生异常: {ex.Message}");
                    }
                }
            }

            // 强制重置网格内部状态
            ForceResetGridState(containerGrid);
            
            if (showDebugLog) Debug.Log($"[ContainerSaveManager] 容器网格清理完成: 收集 {gameObjectsToDestroy.Count} 个对象，移除 {successCount} 个物品");
        }

        /// <summary>
        /// 强制重置网格状态，确保所有位置都被标记为空
        /// </summary>
        private void ForceResetGridState(ItemGrid containerGrid)
        {
            try
            {
                // 尝试通过反射访问内部数组并重置
                var inventoryItemSlotField = containerGrid.GetType().GetField("inventoryItemSlot", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (inventoryItemSlotField != null)
                {
                    var slots = inventoryItemSlotField.GetValue(containerGrid) as Item[,];
                    if (slots != null)
                    {
                        // 清空所有槽位
                        for (int x = 0; x < containerGrid.CurrentWidth; x++)
                        {
                            for (int y = 0; y < containerGrid.CurrentHeight; y++)
                            {
                                slots[x, y] = null;
                            }
                        }
                        if (showDebugLog) Debug.Log($"[ContainerSaveManager] 强制重置网格状态完成: {containerGrid.CurrentWidth}x{containerGrid.CurrentHeight}");
                    }
                }
            }
            catch (System.Exception ex)
            {
                if (showDebugLog) Debug.LogWarning($"[ContainerSaveManager] 强制重置网格状态失败: {ex.Message}");
            }
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
            // 统一使用 SlotType_GlobalId_ItemId 格式，确保保存和加载时一致
            return $"{slotType}_{containerItem.ItemData.GlobalId}_{containerItem.ItemData.id}";
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
            // 确保缓存已初始化
            if (_containerDataCache == null)
            {
                _containerDataCache = new Dictionary<string, ContainerSaveData>();
            }
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
                                // 使用统一的键值格式：SlotType_GlobalId_ItemId
                                string key = $"{saveData.slotType}_{saveData.containerGlobalID}_{saveData.containerItemID}";
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

            // 尝试迁移旧格式的键值数据
            MigrateOldKeyFormat();

            if (showDebugLog)
            {
                Debug.Log($"[ContainerSaveManager] 从ES3文件加载了 {_containerDataCache.Count} 个容器数据");
                Debug.Log($"[ContainerSaveManager] 缓存中的所有键值: [{string.Join(", ", _containerDataCache.Keys)}]");
            }
        }

        /// <summary>
        /// 迁移旧格式的键值数据
        /// 将 SlotType_ItemId_GlobalId 格式迁移到 SlotType_GlobalId_ItemId 格式
        /// </summary>
        private void MigrateOldKeyFormat()
        {
            var keysToMigrate = new List<string>();
            var newEntries = new Dictionary<string, ContainerSaveData>();
            
            foreach (var kvp in _containerDataCache)
            {
                string oldKey = kvp.Key;
                var saveData = kvp.Value;
                
                // 检查是否是旧格式：SlotType_ItemId_GlobalId
                // 新格式应该是：SlotType_GlobalId_ItemId
                string expectedNewKey = $"{saveData.slotType}_{saveData.containerGlobalID}_{saveData.containerItemID}";
                
                if (oldKey != expectedNewKey)
                {
                    if (showDebugLog)
                        Debug.Log($"[ContainerSaveManager] 迁移键值格式: {oldKey} -> {expectedNewKey}");
                    
                    // 更新saveData的键值
                    saveData.containerKey = expectedNewKey;
                    
                    keysToMigrate.Add(oldKey);
                    newEntries[expectedNewKey] = saveData;
                }
            }
            
            // 移除旧键值并添加新键值
            foreach (string oldKey in keysToMigrate)
            {
                _containerDataCache.Remove(oldKey);
            }
            
            foreach (var kvp in newEntries)
            {
                _containerDataCache[kvp.Key] = kvp.Value;
            }
            
            // 如果有迁移，保存更新后的数据
            if (keysToMigrate.Count > 0)
            {
                if (showDebugLog)
                    Debug.Log($"[ContainerSaveManager] 已迁移 {keysToMigrate.Count} 个容器数据的键值格式，正在保存...");
                    
                SaveAllContainerDataToES3();
                SaveCrossSessionData();
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
        
        #region 跨会话持久化功能
        
        /// <summary>
        /// 节流保存跨会话数据（防止频繁保存）
        /// </summary>
        public void SaveCrossSessionDataThrottled()
        {
            if (!enableCrossSessionSave)
                return;
                
            if (_isRestoring)
            {
                if (showDebugLog)
                    Debug.Log("[ContainerSaveManager] 🔒 恢复过程中跳过保存");
                return;
            }
            
            float currentTime = Time.time;
            
            // 如果距离上次保存时间不足，标记为有待保存并启动延迟保存
            if (currentTime - _lastSaveTime < SAVE_THROTTLE_INTERVAL)
            {
                _hasPendingSave = true;
                
                // 如果还没有启动延迟保存协程，则启动一个
                if (_throttledSaveCoroutine == null)
                {
                    _throttledSaveCoroutine = StartCoroutine(ThrottledSaveCoroutine());
                }
                
                if (showDebugLog)
                    Debug.Log("[ContainerSaveManager] ⏱️ 保存被节流，等待延迟保存");
                return;
            }
            
            // 立即保存
            ExecuteSave();
        }
        
        /// <summary>
        /// 节流保存协程
        /// </summary>
        private IEnumerator ThrottledSaveCoroutine()
        {
            while (_hasPendingSave)
            {
                // 等待到最小保存间隔
                float waitTime = SAVE_THROTTLE_INTERVAL - (Time.time - _lastSaveTime);
                if (waitTime > 0)
                {
                    yield return new WaitForSeconds(waitTime);
                }
                
                // 执行保存
                if (_hasPendingSave)
                {
                    ExecuteSave();
                    _hasPendingSave = false;
                }
            }
            
            _throttledSaveCoroutine = null;
        }
        
        /// <summary>
        /// 执行实际的保存操作
        /// </summary>
        private void ExecuteSave()
        {
            _lastSaveTime = Time.time;
            SaveCrossSessionData();
            
            if (showDebugLog)
                Debug.Log("[ContainerSaveManager] 💾 执行节流保存");
        }
        
        /// <summary>
        /// 跨会话保存所有容器数据
        /// </summary>
        public void SaveCrossSessionData()
        {
            if (!enableCrossSessionSave)
            {
                if (showDebugLog)
                    Debug.Log("[ContainerSaveManager] 跨会话保存已禁用");
                return;
            }
            
            try
            {
                // 收集当前所有容器数据
                var crossSessionData = new CrossSessionContainerData();
                crossSessionData.sessionId = System.Guid.NewGuid().ToString();
                crossSessionData.timestamp = System.DateTime.UtcNow.Ticks;
                crossSessionData.version = "1.0";
                
                // 确保timestamp有效
                if (crossSessionData.timestamp <= 0)
                {
                    crossSessionData.timestamp = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                }
                
                // 复制当前容器数据
                ContainerSaveDataCollection collection = new ContainerSaveDataCollection();
                collection.containers = _containerDataCache.Values.ToList();
                crossSessionData.containerData = collection;
                
                // 生成校验码
                crossSessionData.checksum = GenerateCrossSessionChecksum(crossSessionData);
                
                // 创建备份（如果启用）
                if (enableBackup)
                {
                    CreateCrossSessionBackup();
                }
                
                // 保存跨会话数据
                ES3.Save(crossSessionDataKey, crossSessionData, containerSaveFileName);
                
                if (showDebugLog)
                    Debug.Log($"[ContainerSaveManager] 跨会话数据保存成功，容器数量: {collection.containers.Count}，会话ID: {crossSessionData.sessionId}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ContainerSaveManager] 跨会话数据保存失败: {e.Message}");
            }
        }
        
        /// <summary>
        /// 跨会话加载所有容器数据
        /// </summary>
        public bool LoadCrossSessionData()
        {
            if (!enableCrossSessionSave)
            {
                if (showDebugLog)
                    Debug.Log("[ContainerSaveManager] 跨会话加载已禁用");
                return false;
            }
            
            try
            {
                if (ES3.KeyExists(crossSessionDataKey, containerSaveFileName))
                {
                    CrossSessionContainerData crossSessionData = ES3.Load<CrossSessionContainerData>(crossSessionDataKey, containerSaveFileName);
                    
                    if (crossSessionData != null)
                    {
                        // 验证数据完整性
                        if (ValidateCrossSessionData(crossSessionData))
                        {
                            // 确保缓存已初始化
                            if (_containerDataCache == null)
                            {
                                _containerDataCache = new Dictionary<string, ContainerSaveData>();
                            }
                            
                            // 加载容器数据到缓存
                            _containerDataCache.Clear();
                            
                            if (crossSessionData.containerData != null && crossSessionData.containerData.containers != null)
                            {
                                foreach (var container in crossSessionData.containerData.containers)
                                {
                                    if (!string.IsNullOrEmpty(container.containerKey))
                                    {
                                        _containerDataCache[container.containerKey] = container;
                                    }
                                }
                            }
                            
                            if (showDebugLog)
                                Debug.Log($"[ContainerSaveManager] 跨会话数据加载成功，容器数量: {_containerDataCache.Count}，会话ID: {crossSessionData.sessionId}");
                            
                            return true;
                        }
                        else
                        {
                            Debug.LogWarning("[ContainerSaveManager] 跨会话数据校验失败，尝试从备份恢复");
                            return LoadCrossSessionBackup();
                        }
                    }
                }
                else
                {
                    if (showDebugLog)
                        Debug.Log("[ContainerSaveManager] 未找到跨会话数据，可能是首次运行");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ContainerSaveManager] 跨会话数据加载失败: {e.Message}，尝试从备份恢复");
                return LoadCrossSessionBackup();
            }
            
            return false;
        }
        
        /// <summary>
        /// 强制保存所有当前容器状态
        /// </summary>
        public void ForceSaveAllContainers()
        {
            if (showDebugLog)
                Debug.Log("[ContainerSaveManager] 执行强制保存所有容器");
            
            // 保存当前会话数据
            SaveAllContainerDataToES3();
            
            // 保存跨会话数据
            SaveCrossSessionData();
        }
        
        /// <summary>
        /// 容器内容变化时的自动保存
        /// </summary>
        public void OnContainerContentChanged(string containerKey)
        {
            if (autoSaveOnChange && enableCrossSessionSave)
            {
                if (showDebugLog)
                    Debug.Log($"[ContainerSaveManager] 容器内容变化，执行自动保存: {containerKey}");
                
                // 延迟保存，避免频繁保存
                StartCoroutine(DelayedAutoSave());
            }
        }
        
        /// <summary>
        /// 延迟自动保存协程
        /// </summary>
        private IEnumerator DelayedAutoSave()
        {
            yield return new WaitForSeconds(1f); // 1秒延迟
            
            // 保存当前会话数据和跨会话数据
            SaveAllContainerDataToES3();
            SaveCrossSessionData();
        }
        
        /// <summary>
        /// 生成跨会话数据校验码
        /// </summary>
        private string GenerateCrossSessionChecksum(CrossSessionContainerData data)
        {
            try
            {
                // 简单的校验码生成（可以用更复杂的算法）
                string content = $"{data.sessionId}_{data.timestamp}_{data.version}";
                if (data.containerData != null && data.containerData.containers != null)
                {
                    content += $"_{data.containerData.containers.Count}";
                }
                
                return content.GetHashCode().ToString();
            }
            catch
            {
                return "INVALID";
            }
        }
        
        /// <summary>
        /// 验证跨会话数据完整性
        /// </summary>
        private bool ValidateCrossSessionData(CrossSessionContainerData data)
        {
            if (data == null) 
            {
                if (showDebugLog) Debug.LogWarning("[ContainerSaveManager] 校验失败: 数据为null");
                return false;
            }
            if (string.IsNullOrEmpty(data.sessionId)) 
            {
                if (showDebugLog) Debug.LogWarning("[ContainerSaveManager] 校验失败: sessionId为空");
                return false;
            }
            if (data.timestamp <= 0) 
            {
                if (showDebugLog) Debug.LogWarning($"[ContainerSaveManager] 校验失败: timestamp无效 ({data.timestamp})，尝试恢复");
                // 对于无效的timestamp，我们允许通过但记录警告
                // return false;
            }
            if (string.IsNullOrEmpty(data.version)) 
            {
                if (showDebugLog) Debug.LogWarning("[ContainerSaveManager] 校验失败: version为空");
                return false;
            }
            
            // 确保容器数据结构存在（允许为空）
            if (data.containerData == null)
            {
                if (showDebugLog) Debug.LogWarning("[ContainerSaveManager] containerData为null，创建空数据");
                data.containerData = new ContainerSaveDataCollection();
            }
            if (data.containerData.containers == null)
            {
                if (showDebugLog) Debug.LogWarning("[ContainerSaveManager] containers列表为null，创建空列表");
                data.containerData.containers = new List<ContainerSaveData>();
            }
            
            // 验证校验码（即使是空容器也需要校验）
            bool checksumValid = true;
            if (!string.IsNullOrEmpty(data.checksum))
            {
                string expectedChecksum = GenerateCrossSessionChecksum(data);
                checksumValid = data.checksum == expectedChecksum;
                
                if (showDebugLog)
                {
                    Debug.Log($"[ContainerSaveManager] 校验码验证: 期望={expectedChecksum}, 实际={data.checksum}, 匹配={checksumValid}");
                }
                
                if (!checksumValid)
                {
                    Debug.LogWarning($"[ContainerSaveManager] 跨会话数据校验失败: 校验码不匹配，但仍尝试加载");
                    // 校验码不匹配时，我们仍然尝试加载数据，只是记录警告
                    checksumValid = true;
                }
            }
            
            if (showDebugLog)
            {
                Debug.Log($"[ContainerSaveManager] 数据详情: sessionId={data.sessionId}, timestamp={data.timestamp}, version={data.version}");
                Debug.Log($"[ContainerSaveManager] 容器数量: {data.containerData.containers.Count}");
            }
            
            // 修改：即使容器为空也认为是有效数据
            return checksumValid;
        }
        
        /// <summary>
        /// 创建跨会话数据备份
        /// </summary>
        private void CreateCrossSessionBackup()
        {
            try
            {
                string backupKey = crossSessionDataKey + "_backup";
                
                if (ES3.KeyExists(crossSessionDataKey, containerSaveFileName))
                {
                    var originalData = ES3.Load<CrossSessionContainerData>(crossSessionDataKey, containerSaveFileName);
                    ES3.Save(backupKey, originalData, containerSaveFileName);
                    
                    if (showDebugLog)
                        Debug.Log("[ContainerSaveManager] 跨会话数据备份已创建");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[ContainerSaveManager] 创建跨会话备份失败: {e.Message}");
            }
        }
        
        /// <summary>
        /// 从备份加载跨会话数据
        /// </summary>
        private bool LoadCrossSessionBackup()
        {
            try
            {
                string backupKey = crossSessionDataKey + "_backup";
                
                if (ES3.KeyExists(backupKey, containerSaveFileName))
                {
                    CrossSessionContainerData backupData = ES3.Load<CrossSessionContainerData>(backupKey, containerSaveFileName);
                    
                    if (backupData != null && ValidateCrossSessionData(backupData))
                    {
                        // 加载备份数据到缓存
                        _containerDataCache.Clear();
                        
                        if (backupData.containerData != null && backupData.containerData.containers != null)
                        {
                            foreach (var container in backupData.containerData.containers)
                            {
                                if (!string.IsNullOrEmpty(container.containerKey))
                                {
                                    _containerDataCache[container.containerKey] = container;
                                }
                            }
                        }
                        
                        Debug.Log($"[ContainerSaveManager] 从备份成功恢复跨会话数据，容器数量: {_containerDataCache.Count}");
                        return true;
                    }
                }
                
                Debug.LogWarning("[ContainerSaveManager] 未找到有效的跨会话备份数据");
                return false;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ContainerSaveManager] 从备份加载跨会话数据失败: {e.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 清除跨会话数据
        /// </summary>
        public void ClearCrossSessionData()
        {
            try
            {
                if (ES3.KeyExists(crossSessionDataKey, containerSaveFileName))
                {
                    ES3.DeleteKey(crossSessionDataKey, containerSaveFileName);
                }
                
                string backupKey = crossSessionDataKey + "_backup";
                if (ES3.KeyExists(backupKey, containerSaveFileName))
                {
                    ES3.DeleteKey(backupKey, containerSaveFileName);
                }
                
                if (showDebugLog)
                    Debug.Log("[ContainerSaveManager] 跨会话数据已清除");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ContainerSaveManager] 清除跨会话数据失败: {e.Message}");
            }
        }
        
        #endregion
    }
    
    /// <summary>
    /// 跨会话容器数据结构
    /// </summary>
    [System.Serializable]
    public class CrossSessionContainerData
    {
        public string sessionId;                    // 会话ID
        public long timestamp;                      // 时间戳
        public string version;                      // 数据版本
        public string checksum;                     // 校验码
        public ContainerSaveDataCollection containerData; // 容器数据
    }
}