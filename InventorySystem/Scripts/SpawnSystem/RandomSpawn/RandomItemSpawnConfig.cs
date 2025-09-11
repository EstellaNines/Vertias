using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using InventorySystem;

namespace InventorySystem.SpawnSystem
{
    /// <summary>
    /// 随机物品生成配置
    /// ScriptableObject配置文件，定义货架的随机物品生成规则
    /// </summary>
    [CreateAssetMenu(fileName = "New Random Item Spawn Config", 
                     menuName = "Inventory System/Spawn System/Random Item Spawn Config")]
    public class RandomItemSpawnConfig : ScriptableObject
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
        [Tooltip("此配置适用的容器类型，货架通常使用Custom")]
        public ContainerType targetContainerType = ContainerType.Custom;
        
        [FieldLabel("容器标识符")]
        [Tooltip("目标容器的唯一标识符，留空则适用于所有货架容器")]
        public string containerIdentifier = "ContainerItemGrid";
        
        [FieldLabel("网格尺寸验证")]
        [Tooltip("验证目标容器的最小尺寸要求")]
        public Vector2Int minimumGridSize = new Vector2Int(10, 6);
        
        [Header("生成数量控制")]
        [FieldLabel("最小总物品数")]
        [Range(1, 50)]
        [Tooltip("随机生成的最少物品总数")]
        public int minTotalItems = 3;
        
        [FieldLabel("最大总物品数")]
        [Range(1, 50)]
        [Tooltip("随机生成的最多物品总数")]
        public int maxTotalItems = 20;
        
        [FieldLabel("生成数量随机模式")]
        [Tooltip("如何确定最终生成的物品数量")]
        public RandomQuantityMode quantityMode = RandomQuantityMode.UniformRandom;
        
        [Header("固定物品优先生成")]
        [FieldLabel("启用固定物品")]
        [Tooltip("是否在随机生成前先生成一个固定物品（计入总数，优先级最高）")]
        public bool enableFixedItemGeneration = false;
        
        [FieldLabel("固定物品")]
        [Tooltip("将优先生成的固定物品（仅支持一个）")]
        public ItemDataSO fixedItemData;
        
        [FieldLabel("固定物品堆叠数量")]
        [Range(1, 1000)]
        [Tooltip("固定物品的堆叠数量（若物品不可堆叠，将按1处理）")]
        public int fixedItemStackAmount = 1;
        
        [Header("物品类别配置")]
        [FieldLabel("随机物品类别")]
        [Tooltip("配置的物品类别列表，每个类别定义不同的珍稀度分布")]
        public RandomItemCategory[] itemCategories = new RandomItemCategory[0];
        
        [Header("生成策略")]
        [FieldLabel("类别选择策略")]
        [Tooltip("如何选择要生成的物品类别")]
        public CategorySelectionStrategy categoryStrategy = CategorySelectionStrategy.AllCategories;
        
        [FieldLabel("珍稀度平衡模式")]
        [Tooltip("如何平衡不同珍稀度物品的生成")]
        public RarityBalanceMode rarityBalance = RarityBalanceMode.WeightedRandom;
        
        [FieldLabel("避免重复物品")]
        [Tooltip("是否避免生成重复的物品")]
        public bool avoidDuplicateItems = true;
        
        [FieldLabel("最大重试次数")]
        [Range(1, 10)]
        [Tooltip("当生成失败时的最大重试次数")]
        public int maxRetryAttempts = 3;
        
        [Header("编辑器预览")]
        [FieldLabel("启用预览")]
        [Tooltip("是否在编辑器中启用预览功能")]
        public bool enablePreview = true;
        
        [FieldLabel("预览随机种子")]
        [Tooltip("预览时使用的随机种子，相同种子产生相同结果")]
        public int previewSeed = 12345;
        
        [FieldLabel("预览网格尺寸")]
        [Tooltip("预览网格的尺寸，通常与实际容器尺寸一致")]
        public Vector2Int previewGridSize = new Vector2Int(10, 6);
        
        [Header("高级设置")]
        [FieldLabel("启用详细日志")]
        [Tooltip("是否启用详细的调试日志")]
        public bool enableDetailedLogging = false;
        
        [FieldLabel("生成超时时间")]
        [Range(1f, 30f)]
        [Tooltip("单次生成过程的最大允许时间（秒）")]
        public float generationTimeout = 10f;
        
        [FieldLabel("兼容性检查")]
        [Tooltip("是否进行严格的兼容性检查")]
        public bool strictCompatibilityCheck = true;
        
        #region 公共接口方法
        
        /// <summary>
        /// 获取启用的物品类别列表
        /// </summary>
        /// <returns>启用的物品类别数组</returns>
        public RandomItemCategory[] GetEnabledCategories()
        {
            if (itemCategories == null) return new RandomItemCategory[0];
            
            return itemCategories.Where(category => category != null && category.isEnabled).ToArray();
        }
        
        /// <summary>
        /// 获取指定类别的配置
        /// </summary>
        /// <param name="targetCategory">目标物品类别</param>
        /// <returns>对应的RandomItemCategory配置，如果没有找到则返回null</returns>
        public RandomItemCategory GetCategoryConfig(ItemCategory targetCategory)
        {
            if (itemCategories == null) return null;
            
            return itemCategories.FirstOrDefault(cat => cat != null && cat.category == targetCategory && cat.isEnabled);
        }
        
        /// <summary>
        /// 获取随机的物品总数量
        /// </summary>
        /// <param name="randomSeed">随机种子，用于可重现的结果</param>
        /// <returns>随机确定的物品总数量</returns>
        public int GetRandomTotalQuantity(int randomSeed = -1)
        {
            // 保存当前随机状态
            var previousState = UnityEngine.Random.state;
            
            try
            {
                // 设置随机种子
                if (randomSeed >= 0)
                {
                    UnityEngine.Random.InitState(randomSeed);
                }
                
                switch (quantityMode)
                {
                    case RandomQuantityMode.UniformRandom:
                        return UnityEngine.Random.Range(minTotalItems, maxTotalItems + 1);
                    
                    case RandomQuantityMode.WeightedTowardsMin:
                        // 偏向最小值的加权随机
                        float biasedRandom = Mathf.Pow(UnityEngine.Random.value, 2f);
                        return Mathf.RoundToInt(Mathf.Lerp(maxTotalItems, minTotalItems, biasedRandom));
                    
                    case RandomQuantityMode.WeightedTowardsMax:
                        // 偏向最大值的加权随机
                        float maxBiasedRandom = Mathf.Pow(UnityEngine.Random.value, 0.5f);
                        return Mathf.RoundToInt(Mathf.Lerp(minTotalItems, maxTotalItems, maxBiasedRandom));
                    
                    case RandomQuantityMode.Fixed:
                        return minTotalItems;
                    
                    default:
                        return UnityEngine.Random.Range(minTotalItems, maxTotalItems + 1);
                }
            }
            finally
            {
                // 恢复随机状态
                UnityEngine.Random.state = previousState;
            }
        }
        
        /// <summary>
        /// 生成预览物品列表
        /// </summary>
        /// <returns>预览用的物品数据列表</returns>
        public List<PreviewItemData> GeneratePreviewItems()
        {
            var previewItems = new List<PreviewItemData>();
            
            if (!enablePreview) return previewItems;
            
            // 保存当前随机状态
            var previousState = UnityEngine.Random.state;
            
            try
            {
                // 使用预览种子
                UnityEngine.Random.InitState(previewSeed);
                
                // 获取目标数量
                int targetQuantity = GetRandomTotalQuantity(previewSeed);
                
                // 优先添加固定物品（计入总量，仅预览用途）
                if (enablePreview && enableFixedItemGeneration && fixedItemData != null)
                {
                    previewItems.Add(new PreviewItemData
                    {
                        itemData = fixedItemData,
                        rarity = ItemRarity.Common,
                        category = fixedItemData.category,
                        categoryName = "固定物品"
                    });
                    targetQuantity = Mathf.Max(0, targetQuantity - 1);
                }
                var enabledCategories = GetEnabledCategories();
                
                if (enabledCategories.Length == 0) return previewItems;
                
                // 为每个类别分配数量
                var categoryQuantities = DistributeQuantityAmongCategories(targetQuantity, enabledCategories);
                
                // 为每个类别生成预览物品
                foreach (var kvp in categoryQuantities)
                {
                    var category = kvp.Key;
                    int quantity = kvp.Value;
                    
                    var categoryItems = GenerateCategoryPreviewItems(category, quantity);
                    previewItems.AddRange(categoryItems);
                }
                
                return previewItems;
            }
            finally
            {
                // 恢复随机状态
                UnityEngine.Random.state = previousState;
            }
        }
        
        /// <summary>
        /// 检查配置是否与目标容器兼容
        /// </summary>
        /// <param name="targetGrid">目标网格</param>
        /// <param name="containerId">容器ID</param>
        /// <returns>是否兼容</returns>
        public bool IsCompatibleWithContainer(ItemGrid targetGrid, string containerId = null)
        {
            if (targetGrid == null) return false;
            
            // 检查网格尺寸
            if (targetGrid.CurrentWidth < minimumGridSize.x || targetGrid.CurrentHeight < minimumGridSize.y)
            {
                return false;
            }
            
            // 检查容器标识符
            if (!string.IsNullOrEmpty(containerIdentifier) && !string.IsNullOrEmpty(containerId))
            {
                if (!containerId.Contains(containerIdentifier))
                {
                    return false;
                }
            }
            
            return true;
        }
        
        #endregion
        
        #region 配置验证
        
        /// <summary>
        /// 验证配置的完整性和有效性
        /// </summary>
        /// <param name="errors">验证错误列表</param>
        /// <returns>配置是否有效</returns>
        public bool ValidateConfiguration(out List<string> errors)
        {
            errors = new List<string>();
            
            // 基础信息验证
            if (string.IsNullOrEmpty(configName))
            {
                errors.Add("配置名称不能为空");
            }
            
            // 数量限制验证
            if (minTotalItems <= 0)
            {
                errors.Add($"最小总物品数 {minTotalItems} 必须大于0");
            }
            
            if (maxTotalItems < minTotalItems)
            {
                errors.Add($"最大总物品数 {maxTotalItems} 不能小于最小总物品数 {minTotalItems}");
            }
            
            // 网格尺寸验证
            if (minimumGridSize.x <= 0 || minimumGridSize.y <= 0)
            {
                errors.Add($"最小网格尺寸 {minimumGridSize} 必须大于0");
            }
            
            // 物品类别验证
            if (itemCategories == null || itemCategories.Length == 0)
            {
                errors.Add("没有配置任何物品类别");
            }
            else
            {
                var enabledCategories = GetEnabledCategories();
                if (enabledCategories.Length == 0)
                {
                    errors.Add("没有启用任何物品类别");
                }
                else
                {
                    // 验证每个类别
                    for (int i = 0; i < itemCategories.Length; i++)
                    {
                        var category = itemCategories[i];
                        if (category == null) continue;
                        
                        if (!category.ValidateCategory(out string categoryError))
                        {
                            errors.Add($"物品类别 [{i}] 配置无效: {categoryError}");
                        }
                    }
                }
            }
            
            // 预览配置验证
            if (enablePreview)
            {
                if (previewGridSize.x <= 0 || previewGridSize.y <= 0)
                {
                    errors.Add($"预览网格尺寸 {previewGridSize} 必须大于0");
                }
            }
            
            // 固定物品优先生成验证
            if (enableFixedItemGeneration)
            {
                if (fixedItemData == null)
                {
                    errors.Add("已启用固定物品优先生成，但未指定固定物品");
                }
                if (fixedItemStackAmount <= 0)
                {
                    errors.Add($"固定物品堆叠数量 {fixedItemStackAmount} 必须大于0");
                }
            }
            
            // 高级设置验证
            if (generationTimeout <= 0)
            {
                errors.Add($"生成超时时间 {generationTimeout} 必须大于0");
            }
            
            if (maxRetryAttempts <= 0)
            {
                errors.Add($"最大重试次数 {maxRetryAttempts} 必须大于0");
            }
            
            return errors.Count == 0;
        }
        
        /// <summary>
        /// 自动修复配置问题
        /// </summary>
        /// <returns>是否进行了修复</returns>
        public bool AutoFixConfiguration()
        {
            bool hasFixed = false;
            
            // 修复基础信息
            if (string.IsNullOrEmpty(configName))
            {
                configName = "Random Item Config";
                hasFixed = true;
            }
            
            // 修复数量限制
            if (minTotalItems <= 0)
            {
                minTotalItems = 3;
                hasFixed = true;
            }
            
            if (maxTotalItems < minTotalItems)
            {
                maxTotalItems = minTotalItems + 10;
                hasFixed = true;
            }
            
            // 修复网格尺寸
            if (minimumGridSize.x <= 0 || minimumGridSize.y <= 0)
            {
                minimumGridSize = new Vector2Int(10, 6);
                hasFixed = true;
            }
            
            // 修复预览网格尺寸
            if (previewGridSize.x <= 0 || previewGridSize.y <= 0)
            {
                previewGridSize = minimumGridSize;
                hasFixed = true;
            }
            
            // 修复超时时间
            if (generationTimeout <= 0)
            {
                generationTimeout = 10f;
                hasFixed = true;
            }
            
            // 修复重试次数
            if (maxRetryAttempts <= 0)
            {
                maxRetryAttempts = 3;
                hasFixed = true;
            }
            
            // 修复物品类别配置
            if (itemCategories != null)
            {
                foreach (var category in itemCategories)
                {
                    if (category != null && category.AutoFixConfiguration())
                    {
                        hasFixed = true;
                    }
                }
            }
            
            // 修复固定物品配置
            if (fixedItemStackAmount <= 0)
            {
                fixedItemStackAmount = 1;
                hasFixed = true;
            }
            
            return hasFixed;
        }
        
        #endregion
        
        #region 统计和调试
        
        /// <summary>
        /// 获取配置统计信息
        /// </summary>
        /// <returns>配置统计信息结构体</returns>
        public ConfigurationStatistics GetStatistics()
        {
            var stats = new ConfigurationStatistics();
            
            stats.configName = configName;
            stats.targetContainerType = targetContainerType;
            stats.minTotalItems = minTotalItems;
            stats.maxTotalItems = maxTotalItems;
            stats.minimumGridSize = minimumGridSize;
            
            if (itemCategories != null)
            {
                stats.totalCategories = itemCategories.Length;
                stats.enabledCategories = GetEnabledCategories().Length;
                
                int totalItems = 0;
                int totalCommonItems = 0;
                int totalRareItems = 0;
                int totalEpicItems = 0;
                int totalLegendaryItems = 0;
                
                foreach (var category in GetEnabledCategories())
                {
                    var categoryStats = category.GetStatistics();
                    totalItems += categoryStats.totalItemCount;
                    totalCommonItems += categoryStats.commonItemCount;
                    totalRareItems += categoryStats.rareItemCount;
                    totalEpicItems += categoryStats.epicItemCount;
                    totalLegendaryItems += categoryStats.legendaryItemCount;
                }
                
                stats.totalAvailableItems = totalItems;
                stats.commonItemsCount = totalCommonItems;
                stats.rareItemsCount = totalRareItems;
                stats.epicItemsCount = totalEpicItems;
                stats.legendaryItemsCount = totalLegendaryItems;
            }
            
            stats.enablePreview = enablePreview;
            stats.enableDetailedLogging = enableDetailedLogging;
            
            return stats;
        }
        
        /// <summary>
        /// 获取调试信息字符串
        /// </summary>
        /// <returns>包含配置详细信息的调试字符串</returns>
        public string GetDebugInfo()
        {
            var stats = GetStatistics();
            return $"RandomConfig[{configName}]: " +
                   $"Categories={stats.enabledCategories}/{stats.totalCategories}, " +
                   $"Items={stats.totalAvailableItems}, " +
                   $"Range=[{minTotalItems}-{maxTotalItems}], " +
                   $"GridSize={minimumGridSize}";
        }
        
        #endregion
        
        #region 私有辅助方法
        
        /// <summary>
        /// 在类别之间分配数量
        /// </summary>
        private Dictionary<RandomItemCategory, int> DistributeQuantityAmongCategories(int totalQuantity, RandomItemCategory[] categories)
        {
            var distribution = new Dictionary<RandomItemCategory, int>();
            
            if (categories.Length == 0) return distribution;
            
            // 初始化每个类别的最小数量
            int remainingQuantity = totalQuantity;
            foreach (var category in categories)
            {
                int minForCategory = Mathf.Max(1, category.minSpawnCount);
                distribution[category] = minForCategory;
                remainingQuantity -= minForCategory;
            }
            
            // 分配剩余数量
            while (remainingQuantity > 0)
            {
                foreach (var category in categories)
                {
                    if (remainingQuantity <= 0) break;
                    
                    if (distribution[category] < category.maxSpawnCount)
                    {
                        distribution[category]++;
                        remainingQuantity--;
                    }
                }
                
                // 防止无限循环
                if (remainingQuantity > 0 && categories.All(c => distribution[c] >= c.maxSpawnCount))
                {
                    break;
                }
            }
            
            return distribution;
        }
        
        /// <summary>
        /// 为单个类别生成预览物品
        /// </summary>
        private List<PreviewItemData> GenerateCategoryPreviewItems(RandomItemCategory category, int quantity)
        {
            var items = new List<PreviewItemData>();
            var usedItems = new HashSet<ItemDataSO>();
            
            for (int i = 0; i < quantity; i++)
            {
                // 根据权重选择珍稀度
                var weights = category.GetAllWeights();
                var selectedRarity = SelectRandomRarity(weights);
                
                // 获取该珍稀度的物品
                var availableItems = category.GetItemsByRarity(selectedRarity);
                if (availableItems.Length == 0) continue;
                
                // 选择物品（避免重复）
                ItemDataSO selectedItem = null;
                int attempts = 0;
                
                do
                {
                    int randomIndex = UnityEngine.Random.Range(0, availableItems.Length);
                    selectedItem = availableItems[randomIndex];
                    attempts++;
                } while (avoidDuplicateItems && usedItems.Contains(selectedItem) && attempts < 10);
                
                if (selectedItem != null)
                {
                    items.Add(new PreviewItemData
                    {
                        itemData = selectedItem,
                        rarity = selectedRarity,
                        category = category.category,
                        categoryName = category.categoryName
                    });
                    
                    if (avoidDuplicateItems)
                    {
                        usedItems.Add(selectedItem);
                    }
                }
            }
            
            return items;
        }
        
        /// <summary>
        /// 根据权重选择随机珍稀度
        /// </summary>
        private ItemRarity SelectRandomRarity(float[] weights)
        {
            if (weights == null || weights.Length == 0)
                return ItemRarity.Common;
            
            float random = UnityEngine.Random.value;
            float cumulative = 0f;
            
            for (int i = 0; i < weights.Length && i < 4; i++)
            {
                cumulative += weights[i];
                if (random <= cumulative)
                {
                    return (ItemRarity)i;
                }
            }
            
            return ItemRarity.Common;
        }
        
        #endregion
        
        #region 编辑器支持
        
        #if UNITY_EDITOR
        /// <summary>
        /// 编辑器验证回调
        /// </summary>
        private void OnValidate()
        {
            // 自动生成配置名称
            if (string.IsNullOrEmpty(configName))
            {
                configName = $"Random Items - {name}";
            }
            
            // 验证配置
            if (enableDetailedLogging)
            {
                if (ValidateConfiguration(out var errors))
                {
                    Debug.Log($"随机物品配置 '{configName}' 验证通过");
                }
                else
                {
                    Debug.LogWarning($"随机物品配置 '{configName}' 验证失败:\n{string.Join("\n", errors)}");
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
            string message = $"随机物品配置统计:\n" +
                           $"配置名称: {stats.configName}\n" +
                           $"启用类别: {stats.enabledCategories}/{stats.totalCategories}\n" +
                           $"可用物品总数: {stats.totalAvailableItems}\n" +
                           $"生成数量范围: {stats.minTotalItems} - {stats.maxTotalItems}\n" +
                           $"最小网格尺寸: {stats.minimumGridSize}\n" +
                           $"珍稀度分布:\n" +
                           $"  普通: {stats.commonItemsCount}\n" +
                           $"  稀有: {stats.rareItemsCount}\n" +
                           $"  史诗: {stats.epicItemsCount}\n" +
                           $"  传说: {stats.legendaryItemsCount}";
            
            Debug.Log(message);
        }
        
        /// <summary>
        /// 创建示例配置
        /// </summary>
        [ContextMenu("创建示例配置")]
        public void CreateExampleConfiguration()
        {
            configName = "示例货架随机物品配置";
            description = "这是一个示例配置，展示如何设置货架的随机物品生成。";
            minTotalItems = 5;
            maxTotalItems = 15;
            
            // 创建示例类别（需要手动配置物品）
            itemCategories = new RandomItemCategory[2];
            
            // 食物类别
            itemCategories[0] = new RandomItemCategory();
            itemCategories[0].category = ItemCategory.Food;
            itemCategories[0].categoryName = "食物";
            itemCategories[0].description = "各种食物物品";
            itemCategories[0].minSpawnCount = 2;
            itemCategories[0].maxSpawnCount = 8;
            itemCategories[0].isEnabled = true;
            
            // 饮料类别
            itemCategories[1] = new RandomItemCategory();
            itemCategories[1].category = ItemCategory.Drink;
            itemCategories[1].categoryName = "饮料";
            itemCategories[1].description = "各种饮料物品";
            itemCategories[1].minSpawnCount = 1;
            itemCategories[1].maxSpawnCount = 5;
            itemCategories[1].isEnabled = true;
            
            Debug.Log("示例配置已创建，请手动配置各类别的物品列表");
        }
        
        #endif
        
        #endregion
    }
    
    #region 辅助枚举和数据结构
    
    /// <summary>
    /// 随机数量模式
    /// </summary>
    public enum RandomQuantityMode
    {
        [InspectorName("均匀随机")] UniformRandom = 0,
        [InspectorName("偏向最小值")] WeightedTowardsMin = 1,
        [InspectorName("偏向最大值")] WeightedTowardsMax = 2,
        [InspectorName("固定数量")] Fixed = 3
    }
    
    /// <summary>
    /// 类别选择策略
    /// </summary>
    public enum CategorySelectionStrategy
    {
        [InspectorName("所有类别")] AllCategories = 0,
        [InspectorName("随机选择类别")] RandomCategories = 1,
        [InspectorName("按优先级选择")] PriorityBased = 2
    }
    
    /// <summary>
    /// 珍稀度平衡模式
    /// </summary>
    public enum RarityBalanceMode
    {
        [InspectorName("权重随机")] WeightedRandom = 0,
        [InspectorName("保证至少一个稀有")] GuaranteeRare = 1,
        [InspectorName("平均分布")] EvenDistribution = 2
    }
    
    /// <summary>
    /// 预览物品数据
    /// </summary>
    [System.Serializable]
    public struct PreviewItemData
    {
        public ItemDataSO itemData;
        public ItemRarity rarity;
        public ItemCategory category;
        public string categoryName;
    }
    
    /// <summary>
    /// 配置统计信息
    /// </summary>
    [System.Serializable]
    public struct ConfigurationStatistics
    {
        public string configName;
        public ContainerType targetContainerType;
        public int minTotalItems;
        public int maxTotalItems;
        public Vector2Int minimumGridSize;
        public int totalCategories;
        public int enabledCategories;
        public int totalAvailableItems;
        public int commonItemsCount;
        public int rareItemsCount;
        public int epicItemsCount;
        public int legendaryItemsCount;
        public bool enablePreview;
        public bool enableDetailedLogging;
    }
    
    #endregion
}