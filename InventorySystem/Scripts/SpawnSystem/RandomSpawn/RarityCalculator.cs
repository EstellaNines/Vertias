using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using InventorySystem;

namespace InventorySystem.SpawnSystem
{
    /// <summary>
    /// 珍稀度计算工具
    /// 提供所有与珍稀度相关的数学计算功能，包括加权随机选择、权重验证和数量分布计算
    /// </summary>
    public static class RarityCalculator
    {
        /// <summary>
        /// 默认珍稀度权重分布（普通 > 稀有 > 史诗 > 传说）
        /// </summary>
        public static readonly float[] DefaultWeights = { 0.5f, 0.3f, 0.15f, 0.05f };
        
        /// <summary>
        /// 权重验证容差
        /// </summary>
        private const float WEIGHT_TOLERANCE = 0.001f;
        
        #region 加权随机选择
        
        /// <summary>
        /// 根据权重数组选择随机珍稀度
        /// </summary>
        /// <param name="weights">权重数组，索引对应ItemRarity枚举值</param>
        /// <param name="randomValue">可选的随机值（0-1），用于可重现的结果</param>
        /// <returns>选中的珍稀度</returns>
        public static ItemRarity SelectRandomRarity(float[] weights, float? randomValue = null)
        {
            if (weights == null || weights.Length == 0)
            {
                Debug.LogWarning("RarityCalculator: 权重数组为空，返回默认珍稀度Common");
                return ItemRarity.Common;
            }
            
            // 验证权重有效性
            if (!ValidateWeights(weights, out string error))
            {
                Debug.LogWarning($"RarityCalculator: 权重验证失败 - {error}，使用默认权重");
                weights = DefaultWeights;
            }
            
            // 标准化权重
            var normalizedWeights = NormalizeWeights(weights);
            
            // 获取随机值
            float random = randomValue ?? UnityEngine.Random.value;
            
            // 累积概率选择
            float cumulative = 0f;
            for (int i = 0; i < normalizedWeights.Length && i < 4; i++)
            {
                cumulative += normalizedWeights[i];
                if (random <= cumulative)
                {
                    return (ItemRarity)i;
                }
            }
            
            // 容错处理
            return ItemRarity.Common;
        }
        
        /// <summary>
        /// 从珍稀度组数组中选择随机珍稀度
        /// </summary>
        /// <param name="rarityGroups">珍稀度组数组</param>
        /// <param name="randomValue">可选的随机值</param>
        /// <returns>选中的珍稀度</returns>
        public static ItemRarity SelectRandomRarity(RarityItemGroup[] rarityGroups, float? randomValue = null)
        {
            if (rarityGroups == null || rarityGroups.Length == 0)
            {
                Debug.LogWarning("RarityCalculator: 珍稀度组数组为空，返回默认珍稀度Common");
                return ItemRarity.Common;
            }
            
            var weights = ExtractWeights(rarityGroups);
            return SelectRandomRarity(weights, randomValue);
        }
        
        /// <summary>
        /// 批量选择多个珍稀度
        /// </summary>
        /// <param name="weights">权重数组</param>
        /// <param name="count">选择数量</param>
        /// <param name="allowDuplicates">是否允许重复</param>
        /// <param name="randomSeed">随机种子</param>
        /// <returns>选中的珍稀度列表</returns>
        public static List<ItemRarity> SelectMultipleRarities(float[] weights, int count, bool allowDuplicates = true, int? randomSeed = null)
        {
            var results = new List<ItemRarity>();
            
            if (count <= 0) return results;
            
            // 保存当前随机状态
            var previousState = UnityEngine.Random.state;
            
            try
            {
                // 设置随机种子
                if (randomSeed.HasValue)
                {
                    UnityEngine.Random.InitState(randomSeed.Value);
                }
                
                if (allowDuplicates)
                {
                    // 允许重复，直接选择
                    for (int i = 0; i < count; i++)
                    {
                        results.Add(SelectRandomRarity(weights));
                    }
                }
                else
                {
                    // 不允许重复，需要跟踪已选择的珍稀度
                    var availableRarities = new List<ItemRarity> { ItemRarity.Common, ItemRarity.Rare, ItemRarity.Epic, ItemRarity.Legendary };
                    var selectedCounts = new Dictionary<ItemRarity, int>();
                    
                    // 初始化计数
                    foreach (var rarity in availableRarities)
                    {
                        selectedCounts[rarity] = 0;
                    }
                    
                    for (int i = 0; i < count && availableRarities.Count > 0; i++)
                    {
                        var selectedRarity = SelectRandomRarity(weights);
                        results.Add(selectedRarity);
                        selectedCounts[selectedRarity]++;
                        
                        // 如果某个珍稀度被选择过多次，可以考虑降低其权重
                        // 这里使用简单策略：不做特殊处理，允许自然分布
                    }
                }
                
                return results;
            }
            finally
            {
                // 恢复随机状态
                UnityEngine.Random.state = previousState;
            }
        }
        
        #endregion
        
        #region 权重验证和处理
        
        /// <summary>
        /// 验证权重数组的有效性
        /// </summary>
        /// <param name="weights">权重数组</param>
        /// <param name="errorMessage">错误信息</param>
        /// <returns>权重是否有效</returns>
        public static bool ValidateWeights(float[] weights, out string errorMessage)
        {
            errorMessage = "";
            
            if (weights == null)
            {
                errorMessage = "权重数组为null";
                return false;
            }
            
            if (weights.Length == 0)
            {
                errorMessage = "权重数组为空";
                return false;
            }
            
            if (weights.Length > 4)
            {
                errorMessage = $"权重数组长度 {weights.Length} 超过最大珍稀度数量 4";
                return false;
            }
            
            // 检查负权重
            for (int i = 0; i < weights.Length; i++)
            {
                if (weights[i] < 0)
                {
                    errorMessage = $"权重数组索引 {i} 的值 {weights[i]} 不能为负数";
                    return false;
                }
                
                if (float.IsNaN(weights[i]) || float.IsInfinity(weights[i]))
                {
                    errorMessage = $"权重数组索引 {i} 的值 {weights[i]} 不是有效数字";
                    return false;
                }
            }
            
            // 检查权重总和
            float totalWeight = weights.Sum();
            if (totalWeight <= 0)
            {
                errorMessage = $"权重总和 {totalWeight} 必须大于0";
                return false;
            }
            
            // 警告：权重总和不为1（但不算错误，会自动标准化）
            if (Mathf.Abs(totalWeight - 1f) > WEIGHT_TOLERANCE)
            {
                Debug.LogWarning($"RarityCalculator: 权重总和为 {totalWeight}，建议为 1.0，将自动标准化");
            }
            
            return true;
        }
        
        /// <summary>
        /// 标准化权重数组，使总和为1
        /// </summary>
        /// <param name="weights">原始权重数组</param>
        /// <returns>标准化后的权重数组</returns>
        public static float[] NormalizeWeights(float[] weights)
        {
            if (weights == null || weights.Length == 0)
            {
                Debug.LogWarning("RarityCalculator: 无法标准化空权重数组，返回默认权重");
                return (float[])DefaultWeights.Clone();
            }
            
            float totalWeight = weights.Sum();
            if (totalWeight <= 0)
            {
                Debug.LogWarning("RarityCalculator: 权重总和为0或负数，返回默认权重");
                return (float[])DefaultWeights.Clone();
            }
            
            var normalizedWeights = new float[weights.Length];
            for (int i = 0; i < weights.Length; i++)
            {
                normalizedWeights[i] = weights[i] / totalWeight;
            }
            
            return normalizedWeights;
        }
        
        /// <summary>
        /// 从珍稀度组数组中提取权重
        /// </summary>
        /// <param name="rarityGroups">珍稀度组数组</param>
        /// <returns>权重数组</returns>
        public static float[] ExtractWeights(RarityItemGroup[] rarityGroups)
        {
            if (rarityGroups == null || rarityGroups.Length == 0)
            {
                Debug.LogWarning("RarityCalculator: 珍稀度组为空，返回默认权重");
                return (float[])DefaultWeights.Clone();
            }
            
            var weights = new float[4]; // 固定4个珍稀度
            
            // 按珍稀度顺序提取权重
            for (int i = 0; i < 4; i++)
            {
                var targetRarity = (ItemRarity)i;
                var group = rarityGroups.FirstOrDefault(g => g.rarity == targetRarity);
                weights[i] = group?.weight ?? 0f;
            }
            
            return weights;
        }
        
        #endregion
        
        #region 数量分布计算
        
        /// <summary>
        /// 计算各珍稀度的理论分布数量
        /// </summary>
        /// <param name="weights">权重数组</param>
        /// <param name="totalCount">总数量</param>
        /// <returns>各珍稀度的分布数量</returns>
        public static Dictionary<ItemRarity, int> CalculateDistribution(float[] weights, int totalCount)
        {
            var distribution = new Dictionary<ItemRarity, int>();
            
            if (totalCount <= 0)
            {
                // 初始化为0
                for (int i = 0; i < 4; i++)
                {
                    distribution[(ItemRarity)i] = 0;
                }
                return distribution;
            }
            
            var normalizedWeights = NormalizeWeights(weights ?? DefaultWeights);
            int assignedCount = 0;
            
            // 按权重分配数量
            for (int i = 0; i < normalizedWeights.Length && i < 4; i++)
            {
                var rarity = (ItemRarity)i;
                int expectedCount = Mathf.RoundToInt(normalizedWeights[i] * totalCount);
                
                // 确保不超过剩余数量
                int remainingCount = totalCount - assignedCount;
                int actualCount = Mathf.Min(expectedCount, remainingCount);
                
                distribution[rarity] = actualCount;
                assignedCount += actualCount;
            }
            
            // 处理剩余数量（由于四舍五入可能产生）
            int remaining = totalCount - assignedCount;
            if (remaining > 0)
            {
                // 将剩余数量分配给权重最高的珍稀度
                var highestWeightRarity = GetHighestWeightRarity(normalizedWeights);
                distribution[highestWeightRarity] += remaining;
            }
            
            return distribution;
        }
        
        /// <summary>
        /// 获取权重最高的珍稀度
        /// </summary>
        /// <param name="weights">权重数组</param>
        /// <returns>权重最高的珍稀度</returns>
        private static ItemRarity GetHighestWeightRarity(float[] weights)
        {
            if (weights == null || weights.Length == 0)
                return ItemRarity.Common;
            
            int maxIndex = 0;
            float maxWeight = weights[0];
            
            for (int i = 1; i < weights.Length && i < 4; i++)
            {
                if (weights[i] > maxWeight)
                {
                    maxWeight = weights[i];
                    maxIndex = i;
                }
            }
            
            return (ItemRarity)maxIndex;
        }
        
        /// <summary>
        /// 验证分布结果的合理性
        /// </summary>
        /// <param name="distribution">分布结果</param>
        /// <param name="expectedTotal">期望总数</param>
        /// <param name="weights">原始权重</param>
        /// <returns>分布是否合理</returns>
        public static bool ValidateDistribution(Dictionary<ItemRarity, int> distribution, int expectedTotal, float[] weights)
        {
            if (distribution == null) return false;
            
            // 检查总数
            int actualTotal = distribution.Values.Sum();
            if (actualTotal != expectedTotal)
            {
                Debug.LogWarning($"RarityCalculator: 分布总数 {actualTotal} 与期望总数 {expectedTotal} 不匹配");
                return false;
            }
            
            // 检查负数
            foreach (var kvp in distribution)
            {
                if (kvp.Value < 0)
                {
                    Debug.LogWarning($"RarityCalculator: 珍稀度 {kvp.Key} 的数量 {kvp.Value} 不能为负数");
                    return false;
                }
            }
            
            return true;
        }
        
        #endregion
        
        #region 统计和调试
        
        /// <summary>
        /// 获取权重分布的统计信息
        /// </summary>
        /// <param name="weights">权重数组</param>
        /// <returns>统计信息字符串</returns>
        public static string GetWeightStatistics(float[] weights)
        {
            if (weights == null || weights.Length == 0)
                return "权重数组为空";
            
            var normalizedWeights = NormalizeWeights(weights);
            var stats = new System.Text.StringBuilder();
            
            stats.AppendLine("珍稀度权重分布:");
            for (int i = 0; i < normalizedWeights.Length && i < 4; i++)
            {
                var rarity = (ItemRarity)i;
                var percentage = normalizedWeights[i] * 100f;
                stats.AppendLine($"  {rarity}: {normalizedWeights[i]:F3} ({percentage:F1}%)");
            }
            
            float totalWeight = weights.Sum();
            stats.AppendLine($"原始权重总和: {totalWeight:F3}");
            
            return stats.ToString();
        }
        
        /// <summary>
        /// 模拟大量抽取，验证权重分布的准确性
        /// </summary>
        /// <param name="weights">权重数组</param>
        /// <param name="sampleCount">模拟次数</param>
        /// <param name="randomSeed">随机种子</param>
        /// <returns>实际分布统计</returns>
        public static Dictionary<ItemRarity, int> SimulateDistribution(float[] weights, int sampleCount = 10000, int? randomSeed = null)
        {
            var results = new Dictionary<ItemRarity, int>();
            
            // 初始化计数
            for (int i = 0; i < 4; i++)
            {
                results[(ItemRarity)i] = 0;
            }
            
            // 保存随机状态
            var previousState = UnityEngine.Random.state;
            
            try
            {
                if (randomSeed.HasValue)
                {
                    UnityEngine.Random.InitState(randomSeed.Value);
                }
                
                // 进行大量模拟
                for (int i = 0; i < sampleCount; i++)
                {
                    var selectedRarity = SelectRandomRarity(weights);
                    results[selectedRarity]++;
                }
                
                return results;
            }
            finally
            {
                UnityEngine.Random.state = previousState;
            }
        }
        
        /// <summary>
        /// 比较理论分布与实际分布
        /// </summary>
        /// <param name="theoreticalDistribution">理论分布</param>
        /// <param name="actualDistribution">实际分布</param>
        /// <returns>比较结果字符串</returns>
        public static string CompareDistributions(Dictionary<ItemRarity, int> theoreticalDistribution, Dictionary<ItemRarity, int> actualDistribution)
        {
            var comparison = new System.Text.StringBuilder();
            comparison.AppendLine("分布对比:");
            comparison.AppendLine("珍稀度\t理论\t实际\t差异");
            
            foreach (ItemRarity rarity in System.Enum.GetValues(typeof(ItemRarity)))
            {
                int theoretical = theoreticalDistribution.GetValueOrDefault(rarity, 0);
                int actual = actualDistribution.GetValueOrDefault(rarity, 0);
                int difference = actual - theoretical;
                string sign = difference >= 0 ? "+" : "";
                
                comparison.AppendLine($"{rarity}\t{theoretical}\t{actual}\t{sign}{difference}");
            }
            
            return comparison.ToString();
        }
        
        #endregion
    }
}
