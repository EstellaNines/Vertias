using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using InventorySystem;

namespace InventorySystem.SpawnSystem
{
    /// <summary>
    /// 随机物品类别配置
    /// 定义单个物品类别的随机生成规则，包括数量限制和珍稀度分布
    /// </summary>
    [System.Serializable]
    public class RandomItemCategory
    {
        [Header("类别基础设置")]
        [FieldLabel("物品类别")]
        [Tooltip("此配置适用的物品类别")]
        public ItemCategory category = ItemCategory.Food;
        
        [FieldLabel("类别名称")]
        [Tooltip("此类别的显示名称，用于调试和界面显示")]
        public string categoryName = "食物";
        
        [FieldLabel("类别描述")]
        [TextArea(2, 3)]
        [Tooltip("此类别的详细描述")]
        public string description = "";
        
        [Header("生成数量限制")]
        [FieldLabel("最小生成数量")]
        [Range(0, 20)]
        [Tooltip("此类别最少生成的物品数量")]
        public int minSpawnCount = 1;
        
        [FieldLabel("最大生成数量")]
        [Range(1, 20)]
        [Tooltip("此类别最多生成的物品数量")]
        public int maxSpawnCount = 5;
        
        [Header("珍稀度配置")]
        [FieldLabel("自动获取物品")]
        [Tooltip("是否自动从Resources中获取指定类别的物品")]
        public bool autoLoadItems = true;
        
        [FieldLabel("珍稀度权重配置")]
        [Tooltip("各珍稀度的生成权重，会自动标准化为总和1.0")]
        public RarityWeightConfig[] rarityWeights = new RarityWeightConfig[4];
        
        [FieldLabel("珍稀度物品组")]
        [Tooltip("按珍稀度分组的物品配置（当autoLoadItems为false时使用）")]
        public RarityItemGroup[] rarityGroups = new RarityItemGroup[4];
        
        [Header("堆叠数量配置")]
        [FieldLabel("启用堆叠随机化")]
        [Tooltip("是否为可堆叠物品随机化堆叠数量")]
        public bool enableStackRandomization = true;
        
        [FieldLabel("最小堆叠数量")]
        [Range(1, 1000)]
        [Tooltip("可堆叠物品的最小堆叠数量")]
        public int minStackAmount = 1;
        
        [FieldLabel("最大堆叠数量")]
        [Range(1, 1000)]
        [Tooltip("可堆叠物品的最大堆叠数量")]
        public int maxStackAmount = 50;
        
        [Header("高级设置")]
        [FieldLabel("启用此类别")]
        [Tooltip("是否启用此类别的随机生成")]
        public bool isEnabled = true;
        
        [FieldLabel("类别优先级")]
        [Range(0, 10)]
        [Tooltip("类别优先级，数字越小优先级越高")]
        public int priority = 5;
        
        [FieldLabel("启用调试日志")]
        [Tooltip("是否为此类别启用详细的调试日志")]
        public bool enableDebugLog = false;
        
        /// <summary>
        /// 构造函数，初始化默认的珍稀度组
        /// </summary>
        public RandomItemCategory()
        {
            InitializeDefaultRarityGroups();
            InitializeDefaultRarityWeights();
        }
        
        /// <summary>
        /// 初始化默认的珍稀度组配置
        /// </summary>
        private void InitializeDefaultRarityGroups()
        {
            if (rarityGroups == null || rarityGroups.Length != 4)
            {
                rarityGroups = new RarityItemGroup[4];
                
                // 获取默认权重
                float[] defaultWeights = RarityWeightValidator.GetDefaultWeights();
                
                for (int i = 0; i < 4; i++)
                {
                    rarityGroups[i] = new RarityItemGroup
                    {
                        rarity = (ItemRarity)i,
                        weight = defaultWeights[i],
                        items = new ItemDataSO[0]
                    };
                }
            }
        }
        
        /// <summary>
        /// 初始化默认的珍稀度权重配置
        /// </summary>
        private void InitializeDefaultRarityWeights()
        {
            if (rarityWeights == null || rarityWeights.Length != 4)
            {
                rarityWeights = new RarityWeightConfig[4];
                
                // 获取默认权重
                float[] defaultWeights = RarityWeightValidator.GetDefaultWeights();
                
                for (int i = 0; i < 4; i++)
                {
                    rarityWeights[i] = new RarityWeightConfig
                    {
                        rarity = (ItemRarity)i,
                        weight = defaultWeights[i]
                    };
                }
            }
        }
        
        /// <summary>
        /// 获取指定珍稀度的物品列表
        /// </summary>
        /// <param name="rarity">目标珍稀度</param>
        /// <returns>指定珍稀度的物品数组，如果没有找到则返回空数组</returns>
        public ItemDataSO[] GetItemsByRarity(ItemRarity rarity)
        {
            if (autoLoadItems)
            {
                return GetItemsByRarityFromResources(rarity);
            }
            else
            {
                if (rarityGroups == null) return new ItemDataSO[0];
                
                var group = rarityGroups.FirstOrDefault(g => g != null && g.rarity == rarity);
                return group?.items ?? new ItemDataSO[0];
            }
        }
        
        /// <summary>
        /// 从Resources中自动获取指定类别和珍稀度的物品
        /// </summary>
        /// <param name="rarity">目标珍稀度</param>
        /// <returns>匹配的物品数组</returns>
        private ItemDataSO[] GetItemsByRarityFromResources(ItemRarity rarity)
        {
            try
            {
                // 从Resources/InventorySystemResources/ItemScriptableObject路径加载所有物品
                var allItems = Resources.LoadAll<ItemDataSO>("InventorySystemResources/ItemScriptableObject");
                
                if (allItems == null || allItems.Length == 0)
                {
                    if (enableDebugLog)
                        Debug.LogWarning($"[RandomItemCategory] 未找到任何物品数据在 Resources/InventorySystemResources/ItemScriptableObject 路径下");
                    return new ItemDataSO[0];
                }
                
                // 将ItemRarity枚举转换为字符串（1-4）
                string rarityString = ((int)rarity + 1).ToString();
                
                // 过滤指定类别和珍稀度的物品
                var filteredItems = allItems.Where(item => 
                    item != null && 
                    item.category == category && 
                    item.rarity == rarityString
                ).ToArray();
                
                if (enableDebugLog)
                {
                    Debug.Log($"[RandomItemCategory] 类别 {category} 珍稀度 {rarity} 找到 {filteredItems.Length} 个物品");
                }
                
                return filteredItems;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[RandomItemCategory] 自动获取物品时发生错误: {ex.Message}");
                return new ItemDataSO[0];
            }
        }
        
        /// <summary>
        /// 获取指定珍稀度的权重
        /// </summary>
        /// <param name="rarity">目标珍稀度</param>
        /// <returns>指定珍稀度的权重值</returns>
        public float GetWeightByRarity(ItemRarity rarity)
        {
            // 优先使用新的权重配置
            if (rarityWeights != null)
            {
                var weightConfig = rarityWeights.FirstOrDefault(w => w.rarity == rarity);
                if (!weightConfig.Equals(default(RarityWeightConfig))) return weightConfig.weight;
            }
            
            // 回退到旧的珍稀度组配置
            if (rarityGroups == null) return 0f;
            
            var group = rarityGroups.FirstOrDefault(g => g != null && g.rarity == rarity);
            return group?.weight ?? 0f;
        }
        
        /// <summary>
        /// 获取所有珍稀度的权重数组
        /// </summary>
        /// <returns>按珍稀度顺序排列的权重数组</returns>
        public float[] GetAllWeights()
        {
            // 不再依赖 rarityGroups 是否存在；统一通过 GetWeightByRarity 读取（优先 rarityWeights）
            float[] weights = new float[4];
            for (int i = 0; i < 4; i++)
            {
                ItemRarity rarity = (ItemRarity)i;
                weights[i] = GetWeightByRarity(rarity);
            }
            return weights;
        }
        
        /// <summary>
        /// 获取随机物品
        /// </summary>
        /// <param name="rarity">目标珍稀度</param>
        /// <returns>随机选择的物品，如果没有可用物品则返回null</returns>
        public ItemDataSO GetRandomItem(ItemRarity rarity)
        {
            var items = GetItemsByRarity(rarity);
            if (items.Length == 0) return null;
            
            int randomIndex = UnityEngine.Random.Range(0, items.Length);
            return items[randomIndex];
        }
        
        /// <summary>
        /// 获取随机堆叠数量
        /// </summary>
        /// <param name="itemData">物品数据</param>
        /// <returns>随机的堆叠数量</returns>
        public int GetRandomStackAmount(ItemDataSO itemData)
        {
            if (itemData == null || !itemData.IsStackable() || !enableStackRandomization)
            {
                return 1;
            }
            
            // 确保堆叠数量范围在物品的最大堆叠限制内
            int effectiveMinStack = Mathf.Max(1, minStackAmount);
            int effectiveMaxStack = Mathf.Min(maxStackAmount, itemData.maxStack);
            
            // 确保最小值不大于最大值
            if (effectiveMinStack > effectiveMaxStack)
            {
                effectiveMinStack = effectiveMaxStack;
            }
            
            return UnityEngine.Random.Range(effectiveMinStack, effectiveMaxStack + 1);
        }
        
        /// <summary>
        /// 获取有效的堆叠数量范围
        /// </summary>
        /// <param name="itemData">物品数据</param>
        /// <returns>有效的最小和最大堆叠数量</returns>
        public (int min, int max) GetEffectiveStackRange(ItemDataSO itemData)
        {
            if (itemData == null || !itemData.IsStackable())
            {
                return (1, 1);
            }
            
            int effectiveMin = Mathf.Max(1, minStackAmount);
            int effectiveMax = Mathf.Min(maxStackAmount, itemData.maxStack);
            
            if (effectiveMin > effectiveMax)
            {
                effectiveMin = effectiveMax;
            }
            
            return (effectiveMin, effectiveMax);
        }
        
        /// <summary>
        /// 获取此类别中的总物品数量
        /// </summary>
        /// <returns>所有珍稀度组中的物品总数</returns>
        public int GetTotalItemCount()
        {
            if (rarityGroups == null) return 0;
            
            int totalCount = 0;
            foreach (var group in rarityGroups)
            {
                if (group != null && group.items != null)
                {
                    totalCount += group.items.Length;
                }
            }
            
            return totalCount;
        }
        
        /// <summary>
        /// 获取指定珍稀度的物品数量
        /// </summary>
        /// <param name="rarity">目标珍稀度</param>
        /// <returns>指定珍稀度的物品数量</returns>
        public int GetItemCountByRarity(ItemRarity rarity)
        {
            var items = GetItemsByRarity(rarity);
            return items.Length;
        }
        
        /// <summary>
        /// 验证类别配置的有效性
        /// </summary>
        /// <param name="errorMessage">验证失败时的错误信息</param>
        /// <returns>配置是否有效</returns>
        public bool ValidateCategory(out string errorMessage)
        {
            errorMessage = "";
            
            // 检查基础配置
            if (string.IsNullOrEmpty(categoryName))
            {
                errorMessage = "类别名称不能为空";
                return false;
            }
            
            // 检查数量限制
            if (minSpawnCount < 0)
            {
                errorMessage = $"最小生成数量 {minSpawnCount} 不能小于0";
                return false;
            }
            
            if (maxSpawnCount < minSpawnCount)
            {
                errorMessage = $"最大生成数量 {maxSpawnCount} 不能小于最小生成数量 {minSpawnCount}";
                return false;
            }
            
            // 当启用自动获取物品时，只校验权重有效性即可（无需挂物品列表）
            if (autoLoadItems)
            {
                var weights = GetAllWeights();
                if (weights == null || weights.Length != 4)
                {
                    errorMessage = "珍稀度权重长度无效";
                    return false;
                }
                // 简单校验：权重非负且至少一个大于0
                float total = 0f;
                for (int i = 0; i < weights.Length; i++)
                {
                    if (weights[i] < 0f)
                    {
                        errorMessage = $"权重[{(ItemRarity)i}] 不能为负";
                        return false;
                    }
                    total += weights[i];
                }
                if (total <= 0f)
                {
                    errorMessage = "所有权重为0，无法进行随机选择";
                    return false;
                }
                return true;
            }

            // 兼容旧模式：需要在每个珍稀度组中维护物品数组
            if (rarityGroups == null || rarityGroups.Length == 0)
            {
                errorMessage = "珍稀度组配置为空";
                return false;
            }
            if (!RarityWeightValidator.ValidateRarityGroups(rarityGroups, out string groupError))
            {
                errorMessage = $"珍稀度组配置无效: {groupError}";
                return false;
            }
            bool hasAnyItems = false;
            foreach (var group in rarityGroups)
            {
                if (group != null && group.items != null && group.items.Length > 0)
                {
                    hasAnyItems = true;
                    break;
                }
            }
            if (!hasAnyItems)
            {
                errorMessage = "至少需要在一个珍稀度组中配置物品";
                return false;
            }
            return true;
        }
        
        /// <summary>
        /// 自动修复配置问题
        /// </summary>
        /// <returns>是否进行了修复</returns>
        public bool AutoFixConfiguration()
        {
            bool hasFixed = false;
            
            // 确保珍稀度组完整
            if (rarityGroups == null || rarityGroups.Length != 4)
            {
                InitializeDefaultRarityGroups();
                hasFixed = true;
            }
            
            // 修复空的珍稀度组
            for (int i = 0; i < rarityGroups.Length; i++)
            {
                if (rarityGroups[i] == null)
                {
                    rarityGroups[i] = new RarityItemGroup
                    {
                        rarity = (ItemRarity)i,
                        weight = RarityWeightValidator.GetDefaultWeights()[i],
                        items = new ItemDataSO[0]
                    };
                    hasFixed = true;
                }
            }
            
            // 标准化权重
            float[] weights = GetAllWeights();
            if (!RarityWeightValidator.ValidateWeights(weights))
            {
                float[] normalizedWeights = RarityWeightValidator.NormalizeWeights(weights);
                for (int i = 0; i < rarityGroups.Length; i++)
                {
                    if (i < normalizedWeights.Length)
                    {
                        rarityGroups[i].weight = normalizedWeights[i];
                    }
                }
                hasFixed = true;
            }
            
            // 修复数量限制
            if (minSpawnCount < 0)
            {
                minSpawnCount = 1;
                hasFixed = true;
            }
            
            if (maxSpawnCount < minSpawnCount)
            {
                maxSpawnCount = minSpawnCount + 3;
                hasFixed = true;
            }
            
            // 设置默认类别名称
            if (string.IsNullOrEmpty(categoryName))
            {
                categoryName = category.ToString();
                hasFixed = true;
            }
            
            return hasFixed;
        }
        
        /// <summary>
        /// 获取类别统计信息
        /// </summary>
        /// <returns>包含统计信息的结构体</returns>
        public CategoryStatistics GetStatistics()
        {
            var stats = new CategoryStatistics();
            
            stats.categoryName = categoryName;
            stats.category = category;
            stats.isEnabled = isEnabled;
            stats.minSpawnCount = minSpawnCount;
            stats.maxSpawnCount = maxSpawnCount;
            stats.totalItemCount = GetTotalItemCount();
            
            if (rarityGroups != null)
            {
                stats.rarityGroupCount = rarityGroups.Length;
                stats.commonItemCount = GetItemCountByRarity(ItemRarity.Common);
                stats.rareItemCount = GetItemCountByRarity(ItemRarity.Rare);
                stats.epicItemCount = GetItemCountByRarity(ItemRarity.Epic);
                stats.legendaryItemCount = GetItemCountByRarity(ItemRarity.Legendary);
                
                stats.commonWeight = GetWeightByRarity(ItemRarity.Common);
                stats.rareWeight = GetWeightByRarity(ItemRarity.Rare);
                stats.epicWeight = GetWeightByRarity(ItemRarity.Epic);
                stats.legendaryWeight = GetWeightByRarity(ItemRarity.Legendary);
            }
            
            return stats;
        }
        
        /// <summary>
        /// 获取调试信息字符串
        /// </summary>
        /// <returns>包含类别详细信息的调试字符串</returns>
        public string GetDebugInfo()
        {
            var stats = GetStatistics();
            return $"Category[{categoryName}]: " +
                   $"Items={stats.totalItemCount}, " +
                   $"Range=[{minSpawnCount}-{maxSpawnCount}], " +
                   $"Enabled={isEnabled}, " +
                   $"Weights=[C:{stats.commonWeight:F2}, R:{stats.rareWeight:F2}, E:{stats.epicWeight:F2}, L:{stats.legendaryWeight:F2}]";
        }
    }
    
    /// <summary>
    /// 类别统计信息结构体
    /// </summary>
    [System.Serializable]
    public struct CategoryStatistics
    {
        public string categoryName;
        public ItemCategory category;
        public bool isEnabled;
        public int minSpawnCount;
        public int maxSpawnCount;
        public int totalItemCount;
        public int rarityGroupCount;
        
        // 各珍稀度的物品数量
        public int commonItemCount;
        public int rareItemCount;
        public int epicItemCount;
        public int legendaryItemCount;
        
        // 各珍稀度的权重
        public float commonWeight;
        public float rareWeight;
        public float epicWeight;
        public float legendaryWeight;
    }
    
    /// <summary>
    /// 珍稀度权重配置
    /// </summary>
    [System.Serializable]
    public struct RarityWeightConfig
    {
        [FieldLabel("珍稀度")]
        [Tooltip("珍稀度类型")]
        public ItemRarity rarity;
        
        [FieldLabel("权重")]
        [Range(0f, 1f)]
        [Tooltip("此珍稀度的生成权重")]
        public float weight;
    }
}
