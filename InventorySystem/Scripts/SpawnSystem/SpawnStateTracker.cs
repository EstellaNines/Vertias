using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using InventorySystem;

namespace InventorySystem.SpawnSystem
{
    /// <summary>
    /// 生成状态枚举
    /// </summary>
    public enum SpawnState
    {
        [InspectorName("未生成")] NotGenerated = 0,    // 从未生成过
        [InspectorName("已生成")] Generated = 1,       // 已生成，物品仍在容器中
        [InspectorName("已消耗")] Consumed = 2,        // 已生成但被拿走，不再重新生成
        [InspectorName("已失效")] Expired = 3,         // 已过期或因其他原因失效
        [InspectorName("条件不满足")] ConditionNotMet = 4 // 生成条件不满足
    }
    
    /// <summary>
    /// 单个物品的生成记录
    /// </summary>
    [System.Serializable]
    public class ItemSpawnRecord
    {
        [FieldLabel("模板ID")]
        public string templateId;
        
        [FieldLabel("物品ID")]
        public string itemId;
        
        [FieldLabel("容器标识")]
        public string containerId;
        
        [FieldLabel("生成状态")]
        public SpawnState spawnState;
        
        [FieldLabel("生成时间")]
        public string spawnTime;
        
        [FieldLabel("最后检查时间")]
        public string lastCheckTime;
        
        [FieldLabel("生成数量")]
        public int spawnedQuantity;
        
        [FieldLabel("剩余数量")]
        public int remainingQuantity;
        
        [FieldLabel("生成位置")]
        public Vector2Int spawnPosition;
        
        [FieldLabel("额外数据")]
        public string extraData;
        
        public ItemSpawnRecord()
        {
            templateId = "";
            itemId = "";
            containerId = "";
            spawnState = SpawnState.NotGenerated;
            spawnTime = "";
            lastCheckTime = "";
            spawnedQuantity = 0;
            remainingQuantity = 0;
            spawnPosition = Vector2Int.zero;
            extraData = "";
        }
        
        public ItemSpawnRecord(string templateId, string itemId, string containerId)
        {
            this.templateId = templateId;
            this.itemId = itemId;
            this.containerId = containerId;
            this.spawnState = SpawnState.NotGenerated;
            this.spawnTime = "";
            this.lastCheckTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            this.spawnedQuantity = 0;
            this.remainingQuantity = 0;
            this.spawnPosition = Vector2Int.zero;
            this.extraData = "";
        }
        
        /// <summary>
        /// 标记为已生成
        /// </summary>
        public void MarkAsGenerated(int quantity, Vector2Int position)
        {
            spawnState = SpawnState.Generated;
            spawnTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            lastCheckTime = spawnTime;
            spawnedQuantity = quantity;
            remainingQuantity = quantity;
            spawnPosition = position;
        }
        
        /// <summary>
        /// 标记为已消耗
        /// </summary>
        public void MarkAsConsumed()
        {
            spawnState = SpawnState.Consumed;
            lastCheckTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            remainingQuantity = 0;
        }
        
        /// <summary>
        /// 更新检查时间
        /// </summary>
        public void UpdateCheckTime()
        {
            lastCheckTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }
        
        /// <summary>
        /// 获取记录的唯一键
        /// </summary>
        public string GetUniqueKey()
        {
            return $"{containerId}:{templateId}";
        }
    }
    
    /// <summary>
    /// 容器生成记录
    /// </summary>
    [System.Serializable]
    public class ContainerSpawnRecord
    {
        [FieldLabel("容器ID")]
        public string containerId;
        
        [FieldLabel("容器类型")]
        public ContainerType containerType;
        
        [FieldLabel("首次生成时间")]
        public string firstSpawnTime;
        
        [FieldLabel("最后生成时间")]
        public string lastSpawnTime;
        
        [FieldLabel("生成次数")]
        public int spawnCount;
        
        [FieldLabel("物品记录")]
        public List<ItemSpawnRecord> itemRecords;
        
        public ContainerSpawnRecord()
        {
            containerId = "";
            containerType = ContainerType.Warehouse;
            firstSpawnTime = "";
            lastSpawnTime = "";
            spawnCount = 0;
            itemRecords = new List<ItemSpawnRecord>();
        }
        
        public ContainerSpawnRecord(string containerId, ContainerType type)
        {
            this.containerId = containerId;
            this.containerType = type;
            this.firstSpawnTime = "";
            this.lastSpawnTime = "";
            this.spawnCount = 0;
            this.itemRecords = new List<ItemSpawnRecord>();
        }
        
        /// <summary>
        /// 添加物品记录
        /// </summary>
        public void AddItemRecord(ItemSpawnRecord record)
        {
            if (record == null) return;
            
            // 检查是否已存在
            var existing = itemRecords.FirstOrDefault(r => r.GetUniqueKey() == record.GetUniqueKey());
            if (existing != null)
            {
                // 更新现有记录
                itemRecords.Remove(existing);
            }
            
            itemRecords.Add(record);
        }
        
        /// <summary>
        /// 获取物品记录
        /// </summary>
        public ItemSpawnRecord GetItemRecord(string templateId)
        {
            return itemRecords.FirstOrDefault(r => r.templateId == templateId);
        }
        
        /// <summary>
        /// 更新生成时间
        /// </summary>
        public void UpdateSpawnTime()
        {
            string currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            
            if (string.IsNullOrEmpty(firstSpawnTime))
            {
                firstSpawnTime = currentTime;
            }
            
            lastSpawnTime = currentTime;
            spawnCount++;
        }
    }
    
    /// <summary>
    /// 生成状态数据集合
    /// </summary>
    [System.Serializable]
    public class SpawnStateData
    {
        [FieldLabel("数据版本")]
        public string version = "1.0.0";
        
        [FieldLabel("创建时间")]
        public string createTime;
        
        [FieldLabel("最后更新时间")]
        public string lastUpdateTime;
        
        [FieldLabel("容器记录")]
        public List<ContainerSpawnRecord> containerRecords;
        
        public SpawnStateData()
        {
            createTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            lastUpdateTime = createTime;
            containerRecords = new List<ContainerSpawnRecord>();
        }
    }
    
    /// <summary>
    /// 生成状态追踪器
    /// 负责跟踪和管理所有固定物品的生成状态，防止重复生成
    /// </summary>
    public class SpawnStateTracker
    {
        #region 常量
        
        private const string SAVE_KEY = "FixedItemSpawnState";
        private const string DEFAULT_CONTAINER_ID = "default";
        
        #endregion
        
        #region 私有字段
        
        private static SpawnStateTracker instance;
        private SpawnStateData stateData;
        private Dictionary<string, ContainerSpawnRecord> containerCache;
        private bool isDirty;
        private bool enableDebugLog;
        
        #endregion
        
        #region 单例模式
        
        public static SpawnStateTracker Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new SpawnStateTracker();
                }
                return instance;
            }
        }
        
        #endregion
        
        #region 构造函数
        
        private SpawnStateTracker()
        {
            containerCache = new Dictionary<string, ContainerSpawnRecord>();
            isDirty = false;
            enableDebugLog = false;
            LoadStateData();
        }
        
        #endregion
        
        #region 公共属性
        
        /// <summary>
        /// 启用调试日志
        /// </summary>
        public bool EnableDebugLog
        {
            get => enableDebugLog;
            set => enableDebugLog = value;
        }
        
        #endregion
        
        #region 状态查询方法
        
        /// <summary>
        /// 检查物品是否需要生成
        /// </summary>
        public bool ShouldSpawnItem(string containerId, string templateId, ItemGrid targetGrid)
        {
            var record = GetOrCreateItemRecord(containerId, templateId);
            
            LogDebug($"检查物品 {templateId} 在容器 {containerId} 的生成状态: {record.spawnState}");
            
            switch (record.spawnState)
            {
                case SpawnState.NotGenerated:
                    return true;
                    
                case SpawnState.Generated:
                    // 检查物品是否仍在容器中
                    return !IsItemStillInContainer(record, targetGrid);
                    
                case SpawnState.Consumed:
                case SpawnState.Expired:
                    return false;
                    
                case SpawnState.ConditionNotMet:
                    // 可以重新检查条件
                    return true;
                    
                default:
                    return false;
            }
        }
        
        /// <summary>
        /// 检查物品是否仍在容器中
        /// </summary>
        private bool IsItemStillInContainer(ItemSpawnRecord record, ItemGrid targetGrid)
        {
            if (targetGrid == null) return false;
            
            // 改为在整个网格中搜索匹配的物品，而不是只检查原始位置
            bool itemFound = FindItemInGrid(record, targetGrid);
            
            if (itemFound)
            {
                record.UpdateCheckTime();
                LogDebug($"物品 {record.templateId} 仍在容器中");
                return true;
            }
            
            // 物品不在容器中，标记为已消耗
            record.MarkAsConsumed();
            MarkDirty();
            LogDebug($"物品 {record.templateId} 已从容器中移除，标记为已消耗");
            
            return false;
        }
        
        /// <summary>
        /// 在网格中查找匹配的物品
        /// </summary>
        private bool FindItemInGrid(ItemSpawnRecord record, ItemGrid targetGrid)
        {
            // 获取网格中的所有物品
            List<Item> allItems = GetAllItemsInGrid(targetGrid);
            
            foreach (Item item in allItems)
            {
                if (IsMatchingItem(item, record))
                {
                    // 更新物品的当前位置记录
                    Vector2Int currentPosition = GetItemPosition(item, targetGrid);
                    if (currentPosition != record.spawnPosition)
                    {
                        LogDebug($"物品 {record.templateId} 已从位置 {record.spawnPosition} 移动到 {currentPosition}");
                        record.spawnPosition = currentPosition;
                        MarkDirty();
                    }
                    return true;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// 获取网格中的所有物品
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
        /// 检查物品是否匹配记录
        /// </summary>
        private bool IsMatchingItem(Item item, ItemSpawnRecord record)
        {
            if (item == null) return false;
            
            ItemDataReader itemReader = item.GetComponent<ItemDataReader>();
            if (itemReader == null || itemReader.ItemData == null) return false;
            
            // 首先检查物品是否有生成标记
            InventorySpawnTag spawnTag = item.GetComponent<InventorySpawnTag>();
            if (spawnTag != null && spawnTag.templateId == record.templateId)
            {
                LogDebug($"通过生成标记找到匹配物品: {record.templateId}");
                return true;
            }
            
            // 备用检查：通过物品ID匹配
            if (itemReader.ItemData.name.Contains(record.itemId) || 
                itemReader.ItemData.id.ToString() == record.itemId)
            {
                LogDebug($"通过物品ID找到匹配物品: {record.itemId}");
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// 获取物品在网格中的位置
        /// </summary>
        private Vector2Int GetItemPosition(Item item, ItemGrid targetGrid)
        {
            if (item == null) return Vector2Int.zero;
            
            // 尝试从Item组件获取位置（使用公共属性）
            if (item.OnGridPosition != Vector2Int.zero)
            {
                return item.OnGridPosition;
            }
            
            // 备用方案：使用简单的网格位置查找
            // 由于GetGridPositionFromWorldPosition方法不存在，我们使用其他方法
            if (targetGrid != null)
            {
                // 遍历网格寻找物品的位置
                for (int x = 0; x < targetGrid.gridSizeWidth; x++)
                {
                    for (int y = 0; y < targetGrid.gridSizeHeight; y++)
                    {
                        Item itemAtPos = targetGrid.GetItemAt(x, y);
                        if (itemAtPos == item)
                        {
                            return new Vector2Int(x, y);
                        }
                    }
                }
            }
            
            return Vector2Int.zero;
        }
        
        /// <summary>
        /// 获取物品的生成状态
        /// </summary>
        public SpawnState GetItemSpawnState(string containerId, string templateId)
        {
            var record = GetItemRecord(containerId, templateId);
            return record?.spawnState ?? SpawnState.NotGenerated;
        }
        
        #endregion
        
        #region 状态更新方法
        
        /// <summary>
        /// 记录物品生成
        /// </summary>
        public void RecordItemSpawned(string containerId, string templateId, string itemId, 
                                    int quantity, Vector2Int position)
        {
            var containerRecord = GetOrCreateContainerRecord(containerId);
            var itemRecord = GetOrCreateItemRecord(containerId, templateId);
            
            itemRecord.itemId = itemId;
            itemRecord.MarkAsGenerated(quantity, position);
            
            containerRecord.AddItemRecord(itemRecord);
            containerRecord.UpdateSpawnTime();
            
            MarkDirty();
            
            LogDebug($"记录物品生成: {templateId} 在容器 {containerId}，数量: {quantity}，位置: {position}");
        }
        
        /// <summary>
        /// 记录物品被消耗
        /// </summary>
        public void RecordItemConsumed(string containerId, string templateId)
        {
            var record = GetItemRecord(containerId, templateId);
            if (record != null)
            {
                record.MarkAsConsumed();
                MarkDirty();
                
                LogDebug($"记录物品消耗: {templateId} 在容器 {containerId}");
            }
        }
        
        /// <summary>
        /// 重置容器的生成状态
        /// </summary>
        public void ResetContainerSpawnState(string containerId)
        {
            var containerRecord = GetContainerRecord(containerId);
            if (containerRecord != null)
            {
                containerRecord.itemRecords.Clear();
                containerRecord.spawnCount = 0;
                containerRecord.firstSpawnTime = "";
                containerRecord.lastSpawnTime = "";
                
                MarkDirty();
                
                LogDebug($"重置容器 {containerId} 的生成状态");
            }
        }
        
        /// <summary>
        /// 重置所有生成状态
        /// </summary>
        public void ResetAllSpawnStates()
        {
            stateData = new SpawnStateData();
            containerCache.Clear();
            MarkDirty();
            SaveStateData();
            
            LogDebug("重置所有生成状态");
        }
        
        #endregion
        
        #region 记录管理方法
        
        /// <summary>
        /// 获取或创建容器记录
        /// </summary>
        private ContainerSpawnRecord GetOrCreateContainerRecord(string containerId)
        {
            if (string.IsNullOrEmpty(containerId))
            {
                containerId = DEFAULT_CONTAINER_ID;
            }
            
            if (containerCache.ContainsKey(containerId))
            {
                return containerCache[containerId];
            }
            
            var existingRecord = stateData.containerRecords.FirstOrDefault(r => r.containerId == containerId);
            if (existingRecord != null)
            {
                containerCache[containerId] = existingRecord;
                return existingRecord;
            }
            
            var newRecord = new ContainerSpawnRecord(containerId, ContainerType.Warehouse);
            stateData.containerRecords.Add(newRecord);
            containerCache[containerId] = newRecord;
            
            return newRecord;
        }
        
        /// <summary>
        /// 获取容器记录
        /// </summary>
        private ContainerSpawnRecord GetContainerRecord(string containerId)
        {
            if (string.IsNullOrEmpty(containerId))
            {
                containerId = DEFAULT_CONTAINER_ID;
            }
            
            if (containerCache.ContainsKey(containerId))
            {
                return containerCache[containerId];
            }
            
            var record = stateData.containerRecords.FirstOrDefault(r => r.containerId == containerId);
            if (record != null)
            {
                containerCache[containerId] = record;
            }
            
            return record;
        }
        
        /// <summary>
        /// 获取或创建物品记录
        /// </summary>
        private ItemSpawnRecord GetOrCreateItemRecord(string containerId, string templateId)
        {
            var containerRecord = GetOrCreateContainerRecord(containerId);
            var itemRecord = containerRecord.GetItemRecord(templateId);
            
            if (itemRecord == null)
            {
                itemRecord = new ItemSpawnRecord(templateId, "", containerId);
                containerRecord.AddItemRecord(itemRecord);
            }
            
            return itemRecord;
        }
        
        /// <summary>
        /// 获取物品记录
        /// </summary>
        private ItemSpawnRecord GetItemRecord(string containerId, string templateId)
        {
            var containerRecord = GetContainerRecord(containerId);
            return containerRecord?.GetItemRecord(templateId);
        }
        
        #endregion
        
        #region 数据持久化
        
        /// <summary>
        /// 加载状态数据
        /// </summary>
        private void LoadStateData()
        {
            try
            {
                if (PlayerPrefs.HasKey(SAVE_KEY))
                {
                    string jsonData = PlayerPrefs.GetString(SAVE_KEY);
                    stateData = JsonUtility.FromJson<SpawnStateData>(jsonData);
                    
                    LogDebug($"加载生成状态数据: {stateData.containerRecords.Count} 个容器记录");
                }
                else
                {
                    stateData = new SpawnStateData();
                    LogDebug("创建新的生成状态数据");
                }
                
                // 重建缓存
                RebuildCache();
            }
            catch (Exception e)
            {
                Debug.LogError($"SpawnStateTracker: 加载状态数据失败 - {e.Message}");
                stateData = new SpawnStateData();
            }
        }
        
        /// <summary>
        /// 保存状态数据
        /// </summary>
        public void SaveStateData()
        {
            if (!isDirty) return;
            
            try
            {
                stateData.lastUpdateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                string jsonData = JsonUtility.ToJson(stateData, true);
                PlayerPrefs.SetString(SAVE_KEY, jsonData);
                PlayerPrefs.Save();
                
                isDirty = false;
                
                LogDebug("保存生成状态数据");
            }
            catch (Exception e)
            {
                Debug.LogError($"SpawnStateTracker: 保存状态数据失败 - {e.Message}");
            }
        }
        
        /// <summary>
        /// 标记数据为脏
        /// </summary>
        private void MarkDirty()
        {
            isDirty = true;
        }
        
        /// <summary>
        /// 重建缓存
        /// </summary>
        private void RebuildCache()
        {
            containerCache.Clear();
            
            foreach (var containerRecord in stateData.containerRecords)
            {
                containerCache[containerRecord.containerId] = containerRecord;
            }
        }
        
        #endregion
        
        #region 统计和调试方法
        
        /// <summary>
        /// 获取统计信息
        /// </summary>
        public string GetStatistics()
        {
            int totalContainers = stateData.containerRecords.Count;
            int totalItems = stateData.containerRecords.Sum(c => c.itemRecords.Count);
            int generatedItems = stateData.containerRecords.Sum(c => 
                c.itemRecords.Count(i => i.spawnState == SpawnState.Generated));
            int consumedItems = stateData.containerRecords.Sum(c => 
                c.itemRecords.Count(i => i.spawnState == SpawnState.Consumed));
            
            return $"生成状态统计:\n" +
                   $"容器数: {totalContainers}\n" +
                   $"物品记录数: {totalItems}\n" +
                   $"已生成物品: {generatedItems}\n" +
                   $"已消耗物品: {consumedItems}";
        }
        
        /// <summary>
        /// 调试日志
        /// </summary>
        private void LogDebug(string message)
        {
            if (enableDebugLog)
            {
                Debug.Log($"SpawnStateTracker: {message}");
            }
        }
        
        #endregion
        
        #region 生命周期
        
        /// <summary>
        /// 清理资源
        /// </summary>
        public void Cleanup()
        {
            SaveStateData();
            containerCache.Clear();
        }
        
        #endregion
    }
}
