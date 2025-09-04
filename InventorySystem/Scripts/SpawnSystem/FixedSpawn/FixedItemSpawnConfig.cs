using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using InventorySystem;

namespace InventorySystem.SpawnSystem
{
    /// <summary>
    /// 容器类型枚举
    /// </summary>
    public enum ContainerType
    {
        [InspectorName("仓库")] Warehouse = 0,
        [InspectorName("地面网格")] Ground = 1,
        [InspectorName("任务奖励箱")] MissionReward = 2,
        [InspectorName("背包")] Backpack = 3,
        [InspectorName("装备槽")] Equipment = 4,
        [InspectorName("自定义")] Custom = 999
    }
    
    /// <summary>
    /// 生成时机枚举
    /// </summary>
    public enum SpawnTiming
    {
        [InspectorName("游戏启动时")] GameStart = 0,
        [InspectorName("容器首次打开")] ContainerFirstOpen = 1,
        [InspectorName("每次打开容器")] ContainerEveryOpen = 2,
        [InspectorName("手动触发")] Manual = 3,
        [InspectorName("条件满足时")] OnCondition = 4
    }
    
    /// <summary>
    /// 固定物品生成配置
    /// 可配置的ScriptableObject，定义特定容器的固定物品生成规则
    /// </summary>
    [CreateAssetMenu(fileName = "New Fixed Item Spawn Config", 
                     menuName = "Inventory System/Spawn System/Fixed Item Spawn Config")]
    public class FixedItemSpawnConfig : ScriptableObject
    {
        [Header("配置基础信息")]
        [FieldLabel("配置名称")]
        [Tooltip("此配置的显示名称")]
        public string configName;
        
        [FieldLabel("配置描述")]
        [TextArea(2, 4)]
        [Tooltip("此配置的详细描述")]
        public string description;
        
        [FieldLabel("配置版本")]
        [Tooltip("配置版本号，用于兼容性管理")]
        public string version = "1.0.0";
        
        [Header("目标容器配置")]
        [FieldLabel("适用容器类型")]
        [Tooltip("此配置适用的容器类型")]
        public ContainerType targetContainerType = ContainerType.Warehouse;
        
        [FieldLabel("容器标识符")]
        [Tooltip("目标容器的唯一标识符，留空则适用于所有同类型容器")]
        public string containerIdentifier;
        
        [FieldLabel("网格尺寸验证")]
        [Tooltip("验证目标容器的最小尺寸要求")]
        public Vector2Int minimumGridSize = new Vector2Int(10, 10);
        
        [Header("生成时机控制")]
        [FieldLabel("生成时机")]
        [Tooltip("何时触发物品生成")]
        public SpawnTiming spawnTiming = SpawnTiming.ContainerFirstOpen;
        
        [FieldLabel("生成条件标签")]
        [Tooltip("条件生成时需要满足的标签")]
        public string[] requiredConditionTags;
        
        [FieldLabel("冷却时间")]
        [Tooltip("两次生成之间的最小间隔时间（秒）")]
        public float cooldownTime = 0f;
        
        [Header("物品生成模板")]
        [FieldLabel("固定生成物品")]
        [Tooltip("要生成的固定物品列表")]
        public FixedItemTemplate[] fixedItems;
        
        [Header("生成策略")]
        [FieldLabel("生成顺序策略")]
        [Tooltip("物品的生成顺序策略")]
        public ItemSortStrategy sortStrategy = ItemSortStrategy.PriorityThenSize;
        
        [FieldLabel("失败时继续")]
        [Tooltip("当某个物品生成失败时是否继续生成其他物品")]
        public bool continueOnFailure = true;
        
        [FieldLabel("最大生成时间")]
        [Tooltip("单次生成过程的最大允许时间（秒）")]
        public float maxGenerationTime = 10f;
        
        [Header("调试配置")]
        [FieldLabel("启用详细日志")]
        [Tooltip("是否启用详细的调试日志")]
        public bool enableDetailedLogging = false;
        
        [FieldLabel("生成预览")]
        [Tooltip("在编辑器中预览生成结果")]
        public bool enablePreview = false;
        
        /// <summary>
        /// 物品排序策略枚举
        /// </summary>
        public enum ItemSortStrategy
        {
            [InspectorName("按优先级后按尺寸")] PriorityThenSize = 0,
            [InspectorName("按尺寸后按优先级")] SizeThenPriority = 1,
            [InspectorName("仅按优先级")] PriorityOnly = 2,
            [InspectorName("仅按尺寸")] SizeOnly = 3,
            [InspectorName("随机顺序")] Random = 4,
            [InspectorName("配置文件顺序")] ConfigOrder = 5
        }
        
        #region 公共方法
        
        /// <summary>
        /// 获取排序后的物品模板列表
        /// </summary>
        public List<FixedItemTemplate> GetSortedItems()
        {
            if (fixedItems == null || fixedItems.Length == 0)
                return new List<FixedItemTemplate>();
            
            var validItems = fixedItems.Where(item => item != null && item.IsValid(out _)).ToList();
            
            switch (sortStrategy)
            {
                case ItemSortStrategy.PriorityThenSize:
                    return validItems.OrderBy(item => item.priority)
                                   .ThenByDescending(item => GetItemArea(item))
                                   .ToList();
                
                case ItemSortStrategy.SizeThenPriority:
                    return validItems.OrderByDescending(item => GetItemArea(item))
                                   .ThenBy(item => item.priority)
                                   .ToList();
                
                case ItemSortStrategy.PriorityOnly:
                    return validItems.OrderBy(item => item.priority).ToList();
                
                case ItemSortStrategy.SizeOnly:
                    return validItems.OrderByDescending(item => GetItemArea(item)).ToList();
                
                case ItemSortStrategy.Random:
                    return validItems.OrderBy(item => UnityEngine.Random.value).ToList();
                
                case ItemSortStrategy.ConfigOrder:
                default:
                    return validItems;
            }
        }
        
        /// <summary>
        /// 获取特定优先级的物品
        /// </summary>
        public List<FixedItemTemplate> GetItemsByPriority(SpawnPriority priority)
        {
            return fixedItems?.Where(item => item != null && item.priority == priority).ToList() 
                   ?? new List<FixedItemTemplate>();
        }
        
        /// <summary>
        /// 获取关键物品（必须生成的物品）
        /// </summary>
        public List<FixedItemTemplate> GetCriticalItems()
        {
            return GetItemsByPriority(SpawnPriority.Critical);
        }
        
        /// <summary>
        /// 验证配置的有效性
        /// </summary>
        public bool ValidateConfig(out List<string> errors)
        {
            errors = new List<string>();
            
            // 基础验证
            if (string.IsNullOrEmpty(configName))
            {
                errors.Add("配置名称不能为空");
            }
            
            if (minimumGridSize.x <= 0 || minimumGridSize.y <= 0)
            {
                errors.Add("最小网格尺寸必须大于0");
            }
            
            if (maxGenerationTime <= 0)
            {
                errors.Add("最大生成时间必须大于0");
            }
            
            // 物品模板验证
            if (fixedItems == null || fixedItems.Length == 0)
            {
                errors.Add("没有配置任何固定生成物品");
            }
            else
            {
                var templateIds = new HashSet<string>();
                for (int i = 0; i < fixedItems.Length; i++)
                {
                    var item = fixedItems[i];
                    if (item == null)
                    {
                        errors.Add($"物品模板 [{i}] 为空");
                        continue;
                    }
                    
                    if (!item.IsValid(out string itemError))
                    {
                        errors.Add($"物品模板 [{i}] 无效: {itemError}");
                    }
                    
                    // 检查模板ID重复
                    if (!string.IsNullOrEmpty(item.templateId))
                    {
                        if (templateIds.Contains(item.templateId))
                        {
                            errors.Add($"模板ID '{item.templateId}' 重复");
                        }
                        else
                        {
                            templateIds.Add(item.templateId);
                        }
                    }
                }
            }
            
            return errors.Count == 0;
        }
        
        /// <summary>
        /// 检查容器是否匹配此配置
        /// </summary>
        public bool IsContainerMatch(ContainerType containerType, string identifier = null)
        {
            if (targetContainerType != containerType) return false;
            
            if (!string.IsNullOrEmpty(containerIdentifier) && 
                !string.IsNullOrEmpty(identifier))
            {
                return containerIdentifier.Equals(identifier, StringComparison.OrdinalIgnoreCase);
            }
            
            return true;
        }
        
        /// <summary>
        /// 获取配置的统计信息
        /// </summary>
        public SpawnConfigStatistics GetStatistics()
        {
            var stats = new SpawnConfigStatistics();
            
            if (fixedItems != null)
            {
                stats.totalItems = fixedItems.Length;
                stats.validItems = fixedItems.Count(item => item != null && item.IsValid(out _));
                stats.criticalItems = GetCriticalItems().Count;
                stats.uniqueItems = fixedItems.Count(item => item != null && item.isUniqueSpawn);
                
                var totalQuantity = 0;
                var totalArea = 0;
                
                foreach (var item in fixedItems)
                {
                    if (item != null && item.IsValid(out _))
                    {
                        totalQuantity += item.quantity;
                        totalArea += GetItemArea(item) * item.quantity;
                    }
                }
                
                stats.totalQuantity = totalQuantity;
                stats.totalRequiredArea = totalArea;
            }
            
            return stats;
        }
        
        /// <summary>
        /// 获取物品的占用面积
        /// </summary>
        private int GetItemArea(FixedItemTemplate item)
        {
            if (item?.itemData == null) return 0;
            return item.itemData.width * item.itemData.height;
        }
        
        #endregion
        
        #region 编辑器支持
        
        #if UNITY_EDITOR
        /// <summary>
        /// 编辑器验证
        /// </summary>
        private void OnValidate()
        {
            // 自动生成配置名称
            if (string.IsNullOrEmpty(configName))
            {
                configName = $"{targetContainerType}Fixed Items";
            }
            
            // 验证配置
            if (enableDetailedLogging)
            {
                if (ValidateConfig(out var errors))
                {
                    Debug.Log($"配置 '{configName}' 验证通过");
                }
                else
                {
                    Debug.LogWarning($"配置 '{configName}' 验证失败:\n{string.Join("\n", errors)}");
                }
            }
        }
        
        /// <summary>
        /// 在Inspector中显示统计信息
        /// </summary>
        [ContextMenu("显示配置统计")]
        public void ShowStatistics()
        {
            var stats = GetStatistics();
            string message = $"配置统计信息:\n" +
                           $"总物品数: {stats.totalItems}\n" +
                           $"有效物品数: {stats.validItems}\n" +
                           $"关键物品数: {stats.criticalItems}\n" +
                           $"唯一物品数: {stats.uniqueItems}\n" +
                           $"总生成数量: {stats.totalQuantity}\n" +
                           $"总占用面积: {stats.totalRequiredArea} 格";
            
            Debug.Log(message);
        }
        
        #endif
        
        #endregion
    }
    
    /// <summary>
    /// 生成配置统计信息
    /// </summary>
    [System.Serializable]
    public struct SpawnConfigStatistics
    {
        public int totalItems;          // 总物品数
        public int validItems;          // 有效物品数
        public int criticalItems;       // 关键物品数
        public int uniqueItems;         // 唯一物品数
        public int totalQuantity;       // 总生成数量
        public int totalRequiredArea;   // 总占用面积
    }
}
