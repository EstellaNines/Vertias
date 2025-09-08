using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using InventorySystem;

namespace InventorySystem.SpawnSystem
{
    /// <summary>
    /// 随机物品选择器
    /// 核心的随机物品选择逻辑，集成RarityCalculator和RandomItemCategory
    /// 负责根据配置和权重选择合适的物品进行生成
    /// </summary>
    public static class RandomItemSelector
    {
        /// <summary>
        /// 选择结果
        /// </summary>
        public struct SelectionResult
        {
            public List<SelectedItemInfo> selectedItems;    // 选中的物品列表
            public SelectionStatistics statistics;          // 选择统计信息
            public bool success;                            // 是否成功
            public string errorMessage;                     // 错误信息
        }
        
        /// <summary>
        /// 选中物品信息
        /// </summary>
        [System.Serializable]
        public struct SelectedItemInfo
        {
            public ItemDataSO itemData;         // 物品数据
            public ItemRarity rarity;           // 物品珍稀度
            public ItemCategory category;       // 物品类别
            public string categoryName;         // 类别名称
            public int quantity;                // 数量（通常为1）
            public float selectionWeight;       // 选择权重
            public int selectionOrder;          // 选择顺序
        }
        
        /// <summary>
        /// 选择统计信息
        /// </summary>
        [System.Serializable]
        public struct SelectionStatistics
        {
            public int totalRequested;          // 请求总数
            public int totalSelected;           // 实际选择数
            public int duplicatesAvoided;       // 避免的重复数
            public int retryAttempts;           // 重试次数
            public Dictionary<ItemRarity, int> rarityDistribution;      // 珍稀度分布
            public Dictionary<ItemCategory, int> categoryDistribution;   // 类别分布
            public float selectionTime;        // 选择耗时（毫秒）
        }
        
        #region 主要选择方法
        
        /// <summary>
        /// 从配置中选择随机物品
        /// </summary>
        /// <param name="config">随机物品生成配置</param>
        /// <param name="targetQuantity">目标数量</param>
        /// <param name="randomSeed">随机种子</param>
        /// <returns>选择结果</returns>
        public static SelectionResult SelectRandomItems(RandomItemSpawnConfig config, int targetQuantity, int? randomSeed = null)
        {
            var startTime = Time.realtimeSinceStartup;
            var result = new SelectionResult
            {
                selectedItems = new List<SelectedItemInfo>(),
                statistics = new SelectionStatistics
                {
                    totalRequested = targetQuantity,
                    rarityDistribution = new Dictionary<ItemRarity, int>(),
                    categoryDistribution = new Dictionary<ItemCategory, int>()
                },
                success = false
            };
            
            // 验证输入
            if (config == null)
            {
                result.errorMessage = "配置为null";
                return result;
            }
            
            if (targetQuantity <= 0)
            {
                result.errorMessage = $"目标数量 {targetQuantity} 必须大于0";
                return result;
            }
            
            // 获取启用的类别
            var enabledCategories = config.GetEnabledCategories();
            if (enabledCategories.Length == 0)
            {
                result.errorMessage = "没有启用的物品类别";
                return result;
            }
            
            // 保存随机状态
            var previousState = UnityEngine.Random.state;
            
            try
            {
                // 设置随机种子
                if (randomSeed.HasValue)
                {
                    UnityEngine.Random.InitState(randomSeed.Value);
                }
                
                // 为每个类别分配数量
                var categoryQuantities = DistributeQuantityAmongCategories(targetQuantity, enabledCategories);
                
                // 为每个类别选择物品
                int selectionOrder = 0;
                foreach (var kvp in categoryQuantities)
                {
                    var category = kvp.Key;
                    int quantity = kvp.Value;
                    
                    if (quantity <= 0) continue;
                    
                    var categoryItems = SelectItemsFromCategory(
                        category, quantity, ref selectionOrder, config.avoidDuplicateItems, 
                        config.maxRetryAttempts, result.selectedItems);
                    
                    result.selectedItems.AddRange(categoryItems);
                }
                
                // 计算统计信息
                result.statistics = CalculateStatistics(result.selectedItems, targetQuantity, startTime);
                result.success = true;
                
                return result;
            }
            catch (Exception ex)
            {
                result.errorMessage = $"选择过程中发生异常: {ex.Message}";
                Debug.LogError($"RandomItemSelector: {result.errorMessage}");
                return result;
            }
            finally
            {
                // 恢复随机状态
                UnityEngine.Random.state = previousState;
            }
        }
        
        /// <summary>
        /// 从单个类别中选择物品
        /// </summary>
        /// <param name="category">物品类别配置</param>
        /// <param name="targetQuantity">目标数量</param>
        /// <param name="avoidDuplicates">是否避免重复</param>
        /// <param name="maxRetryAttempts">最大重试次数</param>
        /// <param name="selectionOrder">选择顺序引用</param>
        /// <param name="existingItems">已存在的物品列表（用于避免重复）</param>
        /// <returns>选中的物品列表</returns>
        public static List<SelectedItemInfo> SelectItemsFromCategory(
            RandomItemCategory category, int targetQuantity, ref int selectionOrder, 
            bool avoidDuplicates = true, int maxRetryAttempts = 3, List<SelectedItemInfo> existingItems = null)
        {
            var selectedItems = new List<SelectedItemInfo>();
            
            if (category == null || !category.isEnabled || targetQuantity <= 0)
                return selectedItems;
            
            // 验证类别配置
            if (!category.ValidateCategory(out string categoryError))
            {
                Debug.LogWarning($"RandomItemSelector: 类别 '{category.categoryName}' 配置无效: {categoryError}");
                return selectedItems;
            }
            
            // 获取权重数组
            var weights = category.GetAllWeights();
            if (!RarityCalculator.ValidateWeights(weights, out string weightError))
            {
                Debug.LogWarning($"RandomItemSelector: 类别 '{category.categoryName}' 权重无效: {weightError}");
                weights = RarityCalculator.DefaultWeights;
            }
            
            // 用于避免重复的集合
            var usedItems = new HashSet<ItemDataSO>();
            if (avoidDuplicates && existingItems != null)
            {
                foreach (var item in existingItems)
                {
                    usedItems.Add(item.itemData);
                }
            }
            
            int retryCount = 0;
            for (int i = 0; i < targetQuantity; i++)
            {
                bool itemSelected = false;
                int attemptCount = 0;
                
                while (!itemSelected && attemptCount < maxRetryAttempts)
                {
                    // 选择珍稀度
                    var selectedRarity = RarityCalculator.SelectRandomRarity(weights);
                    
                    // 获取该珍稀度的物品
                    var availableItems = category.GetItemsByRarity(selectedRarity);
                    if (availableItems.Length == 0)
                    {
                        attemptCount++;
                        continue;
                    }
                    
                    // 如果避免重复，过滤已使用的物品
                    if (avoidDuplicates)
                    {
                        availableItems = availableItems.Where(item => !usedItems.Contains(item)).ToArray();
                        if (availableItems.Length == 0)
                        {
                            attemptCount++;
                            continue;
                        }
                    }
                    
                    // 随机选择物品
                    int randomIndex = UnityEngine.Random.Range(0, availableItems.Length);
                    var selectedItem = availableItems[randomIndex];
                    
                    // 创建选择信息
                    var itemInfo = new SelectedItemInfo
                    {
                        itemData = selectedItem,
                        rarity = selectedRarity,
                        category = category.category,
                        categoryName = category.categoryName,
                        quantity = 1,
                        selectionWeight = weights[(int)selectedRarity],
                        selectionOrder = selectionOrder++
                    };
                    
                    selectedItems.Add(itemInfo);
                    
                    if (avoidDuplicates)
                    {
                        usedItems.Add(selectedItem);
                    }
                    
                    itemSelected = true;
                }
                
                if (!itemSelected)
                {
                    retryCount++;
                    Debug.LogWarning($"RandomItemSelector: 类别 '{category.categoryName}' 第 {i + 1} 个物品选择失败，已重试 {maxRetryAttempts} 次");
                }
            }
            
            return selectedItems;
        }
        
        /// <summary>
        /// 按珍稀度选择物品
        /// </summary>
        /// <param name="category">物品类别配置</param>
        /// <param name="targetRarity">目标珍稀度</param>
        /// <param name="quantity">数量</param>
        /// <param name="avoidDuplicates">是否避免重复</param>
        /// <returns>选中的物品列表</returns>
        public static List<SelectedItemInfo> SelectItemsByRarity(
            RandomItemCategory category, ItemRarity targetRarity, int quantity, bool avoidDuplicates = true)
        {
            var selectedItems = new List<SelectedItemInfo>();
            
            if (category == null || !category.isEnabled || quantity <= 0)
                return selectedItems;
            
            // 获取指定珍稀度的物品
            var availableItems = category.GetItemsByRarity(targetRarity);
            if (availableItems.Length == 0)
            {
                Debug.LogWarning($"RandomItemSelector: 类别 '{category.categoryName}' 中没有 {targetRarity} 珍稀度的物品");
                return selectedItems;
            }
            
            var usedItems = new HashSet<ItemDataSO>();
            int selectionOrder = 0;
            
            for (int i = 0; i < quantity; i++)
            {
                // 过滤已使用的物品
                var filteredItems = avoidDuplicates 
                    ? availableItems.Where(item => !usedItems.Contains(item)).ToArray()
                    : availableItems;
                
                if (filteredItems.Length == 0)
                {
                    Debug.LogWarning($"RandomItemSelector: 类别 '{category.categoryName}' 中 {targetRarity} 珍稀度的可用物品已用完");
                    break;
                }
                
                // 随机选择
                int randomIndex = UnityEngine.Random.Range(0, filteredItems.Length);
                var selectedItem = filteredItems[randomIndex];
                
                var itemInfo = new SelectedItemInfo
                {
                    itemData = selectedItem,
                    rarity = targetRarity,
                    category = category.category,
                    categoryName = category.categoryName,
                    quantity = 1,
                    selectionWeight = 1f, // 直接指定珍稀度时权重为1
                    selectionOrder = selectionOrder++
                };
                
                selectedItems.Add(itemInfo);
                
                if (avoidDuplicates)
                {
                    usedItems.Add(selectedItem);
                }
            }
            
            return selectedItems;
        }
        
        #endregion
        
        #region 辅助方法
        
        /// <summary>
        /// 在类别之间分配数量
        /// </summary>
        /// <param name="totalQuantity">总数量</param>
        /// <param name="categories">类别数组</param>
        /// <returns>类别数量分配</returns>
        private static Dictionary<RandomItemCategory, int> DistributeQuantityAmongCategories(
            int totalQuantity, RandomItemCategory[] categories)
        {
            var distribution = new Dictionary<RandomItemCategory, int>();
            
            if (categories.Length == 0) return distribution;
            
            // 初始化每个类别的最小数量
            int remainingQuantity = totalQuantity;
            foreach (var category in categories)
            {
                int minForCategory = Mathf.Max(0, category.minSpawnCount);
                distribution[category] = minForCategory;
                remainingQuantity -= minForCategory;
            }
            
            // 分配剩余数量
            while (remainingQuantity > 0)
            {
                bool distributed = false;
                
                foreach (var category in categories)
                {
                    if (remainingQuantity <= 0) break;
                    
                    if (distribution[category] < category.maxSpawnCount)
                    {
                        distribution[category]++;
                        remainingQuantity--;
                        distributed = true;
                    }
                }
                
                // 防止无限循环
                if (!distributed)
                {
                    // 如果所有类别都达到最大值，将剩余数量分配给第一个类别
                    if (categories.Length > 0)
                    {
                        distribution[categories[0]] += remainingQuantity;
                    }
                    break;
                }
            }
            
            return distribution;
        }
        
        /// <summary>
        /// 计算选择统计信息
        /// </summary>
        /// <param name="selectedItems">选中的物品</param>
        /// <param name="totalRequested">请求总数</param>
        /// <param name="startTime">开始时间</param>
        /// <returns>统计信息</returns>
        private static SelectionStatistics CalculateStatistics(
            List<SelectedItemInfo> selectedItems, int totalRequested, float startTime)
        {
            var statistics = new SelectionStatistics
            {
                totalRequested = totalRequested,
                totalSelected = selectedItems.Count,
                duplicatesAvoided = 0, // 这个值在选择过程中计算会更准确
                retryAttempts = 0,     // 同上
                rarityDistribution = new Dictionary<ItemRarity, int>(),
                categoryDistribution = new Dictionary<ItemCategory, int>(),
                selectionTime = (Time.realtimeSinceStartup - startTime) * 1000f // 转换为毫秒
            };
            
            // 计算珍稀度分布
            foreach (ItemRarity rarity in System.Enum.GetValues(typeof(ItemRarity)))
            {
                statistics.rarityDistribution[rarity] = selectedItems.Count(item => item.rarity == rarity);
            }
            
            // 计算类别分布
            var categoryGroups = selectedItems.GroupBy(item => item.category);
            foreach (var group in categoryGroups)
            {
                statistics.categoryDistribution[group.Key] = group.Count();
            }
            
            return statistics;
        }
        
        #endregion
        
        #region 验证和调试方法
        
        /// <summary>
        /// 验证选择结果的合理性
        /// </summary>
        /// <param name="result">选择结果</param>
        /// <param name="config">原始配置</param>
        /// <returns>验证是否通过</returns>
        public static bool ValidateSelectionResult(SelectionResult result, RandomItemSpawnConfig config)
        {
            if (!result.success) return false;
            
            // 检查基本约束
            if (result.selectedItems.Count > config.maxTotalItems)
            {
                Debug.LogWarning($"RandomItemSelector: 选择数量 {result.selectedItems.Count} 超过最大限制 {config.maxTotalItems}");
                return false;
            }
            
            if (result.selectedItems.Count < config.minTotalItems)
            {
                Debug.LogWarning($"RandomItemSelector: 选择数量 {result.selectedItems.Count} 低于最小要求 {config.minTotalItems}");
                return false;
            }
            
            // 检查重复物品（如果启用避免重复）
            if (config.avoidDuplicateItems)
            {
                var uniqueItems = result.selectedItems.Select(item => item.itemData).Distinct().Count();
                if (uniqueItems != result.selectedItems.Count)
                {
                    Debug.LogWarning($"RandomItemSelector: 发现重复物品，唯一物品数 {uniqueItems}，总数 {result.selectedItems.Count}");
                    return false;
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// 获取选择结果的调试信息
        /// </summary>
        /// <param name="result">选择结果</param>
        /// <returns>调试信息字符串</returns>
        public static string GetSelectionDebugInfo(SelectionResult result)
        {
            if (!result.success)
            {
                return $"Selection Failed: {result.errorMessage}";
            }
            
            var info = new System.Text.StringBuilder();
            info.AppendLine($"Selection Success: {result.statistics.totalSelected}/{result.statistics.totalRequested} items");
            info.AppendLine($"Selection Time: {result.statistics.selectionTime:F1}ms");
            
            info.AppendLine("Rarity Distribution:");
            foreach (var kvp in result.statistics.rarityDistribution.Where(kvp => kvp.Value > 0))
            {
                info.AppendLine($"  {kvp.Key}: {kvp.Value}");
            }
            
            info.AppendLine("Category Distribution:");
            foreach (var kvp in result.statistics.categoryDistribution)
            {
                info.AppendLine($"  {kvp.Key}: {kvp.Value}");
            }
            
            return info.ToString();
        }
        
        /// <summary>
        /// 模拟多次选择以验证分布
        /// </summary>
        /// <param name="config">配置</param>
        /// <param name="simulationCount">模拟次数</param>
        /// <param name="targetQuantity">每次选择的目标数量</param>
        /// <returns>模拟统计结果</returns>
        public static Dictionary<ItemRarity, float> SimulateSelectionDistribution(
            RandomItemSpawnConfig config, int simulationCount = 1000, int? targetQuantity = null)
        {
            var rarityTotals = new Dictionary<ItemRarity, int>();
            int totalItems = 0;
            
            // 初始化计数
            foreach (ItemRarity rarity in System.Enum.GetValues(typeof(ItemRarity)))
            {
                rarityTotals[rarity] = 0;
            }
            
            // 进行多次模拟
            for (int i = 0; i < simulationCount; i++)
            {
                int quantity = targetQuantity ?? config.GetRandomTotalQuantity(i);
                var result = SelectRandomItems(config, quantity, i);
                
                if (result.success)
                {
                    totalItems += result.selectedItems.Count;
                    foreach (var item in result.selectedItems)
                    {
                        rarityTotals[item.rarity]++;
                    }
                }
            }
            
            // 计算百分比
            var distribution = new Dictionary<ItemRarity, float>();
            foreach (var kvp in rarityTotals)
            {
                distribution[kvp.Key] = totalItems > 0 ? (float)kvp.Value / totalItems : 0f;
            }
            
            return distribution;
        }
        
        #endregion
    }
}
