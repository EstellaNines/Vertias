using System;
using UnityEngine;
using InventorySystem;

namespace InventorySystem.SpawnSystem
{
    /// <summary>
    /// 物品珍稀度等级枚举
    /// 定义四个珍稀度等级，用于随机物品生成的权重计算
    /// </summary>
    public enum ItemRarity
    {
        [InspectorName("普通")] Common = 0,     // 最高概率 (~70%)
        [InspectorName("稀有")] Rare = 1,       // 中等概率 (~20%)
        [InspectorName("史诗")] Epic = 2,       // 较低概率 (~8%)
        [InspectorName("传说")] Legendary = 3   // 最低概率 (~2%)
    }
    
    /// <summary>
    /// 珍稀度物品组
    /// 将同一珍稀度的物品和其权重组织在一起
    /// </summary>
    [System.Serializable]
    public class RarityItemGroup
    {
        [Header("珍稀度设置")]
        [FieldLabel("珍稀度等级")]
        [Tooltip("此组物品的珍稀度等级")]
        public ItemRarity rarity = ItemRarity.Common;
        
        [FieldLabel("生成权重")]
        [Range(0f, 1f)]
        [Tooltip("此珍稀度的生成权重，所有权重总和应为1.0")]
        public float weight = 0.25f;
        
        [Header("物品列表")]
        [FieldLabel("可生成物品")]
        [Tooltip("此珍稀度等级下可以生成的物品列表")]
        public ItemDataSO[] items = new ItemDataSO[0];
        
        /// <summary>
        /// 验证珍稀度组配置的有效性
        /// </summary>
        /// <param name="errorMessage">验证失败时的错误信息</param>
        /// <returns>配置是否有效</returns>
        public bool IsValid(out string errorMessage)
        {
            errorMessage = "";
            
            // 检查权重是否合法
            if (weight < 0f || weight > 1f)
            {
                errorMessage = $"珍稀度 {rarity} 的权重 {weight} 超出有效范围 [0, 1]";
                return false;
            }
            
            // 检查是否有物品
            if (items == null || items.Length == 0)
            {
                errorMessage = $"珍稀度 {rarity} 没有配置任何物品";
                return false;
            }
            
            // 检查物品是否为空
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] == null)
                {
                    errorMessage = $"珍稀度 {rarity} 的物品列表中第 {i + 1} 项为空";
                    return false;
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// 获取随机物品
        /// </summary>
        /// <returns>从此珍稀度组中随机选择的物品，如果没有物品则返回null</returns>
        public ItemDataSO GetRandomItem()
        {
            if (items == null || items.Length == 0)
                return null;
                
            int randomIndex = UnityEngine.Random.Range(0, items.Length);
            return items[randomIndex];
        }
        
        /// <summary>
        /// 获取物品数量
        /// </summary>
        /// <returns>此珍稀度组中的物品数量</returns>
        public int GetItemCount()
        {
            return items?.Length ?? 0;
        }
        
        /// <summary>
        /// 获取调试信息字符串
        /// </summary>
        /// <returns>包含珍稀度、权重和物品数量的调试信息</returns>
        public string GetDebugInfo()
        {
            return $"Rarity[{rarity}]: Weight={weight:F3}, Items={GetItemCount()}";
        }
    }
    
    /// <summary>
    /// 珍稀度权重工具类
    /// 提供权重验证和标准化功能
    /// </summary>
    public static class RarityWeightValidator
    {
        /// <summary>
        /// 验证权重数组的有效性
        /// </summary>
        /// <param name="weights">权重数组</param>
        /// <param name="tolerance">允许的误差范围</param>
        /// <returns>权重是否有效（总和接近1.0）</returns>
        public static bool ValidateWeights(float[] weights, float tolerance = 0.001f)
        {
            if (weights == null || weights.Length == 0)
                return false;
                
            float sum = 0f;
            foreach (float weight in weights)
            {
                if (weight < 0f || weight > 1f)
                    return false;
                sum += weight;
            }
            
            return Mathf.Abs(sum - 1f) <= tolerance;
        }
        
        /// <summary>
        /// 标准化权重数组使其总和为1.0
        /// </summary>
        /// <param name="weights">要标准化的权重数组</param>
        /// <returns>标准化后的权重数组</returns>
        public static float[] NormalizeWeights(float[] weights)
        {
            if (weights == null || weights.Length == 0)
                return new float[0];
                
            float sum = 0f;
            foreach (float weight in weights)
            {
                sum += Mathf.Max(0f, weight); // 确保权重非负
            }
            
            if (sum <= 0f)
            {
                // 如果所有权重都是0，则平均分配
                float equalWeight = 1f / weights.Length;
                float[] equalWeights = new float[weights.Length];
                for (int i = 0; i < weights.Length; i++)
                {
                    equalWeights[i] = equalWeight;
                }
                return equalWeights;
            }
            
            // 标准化权重
            float[] normalizedWeights = new float[weights.Length];
            for (int i = 0; i < weights.Length; i++)
            {
                normalizedWeights[i] = Mathf.Max(0f, weights[i]) / sum;
            }
            
            return normalizedWeights;
        }
        
        /// <summary>
        /// 获取默认的珍稀度权重分布
        /// </summary>
        /// <returns>默认权重数组 [Common, Rare, Epic, Legendary]</returns>
        public static float[] GetDefaultWeights()
        {
            return new float[] { 0.70f, 0.20f, 0.08f, 0.02f };
        }
        
        /// <summary>
        /// 验证珍稀度组数组的权重
        /// </summary>
        /// <param name="rarityGroups">珍稀度组数组</param>
        /// <param name="errorMessage">验证失败时的错误信息</param>
        /// <returns>权重配置是否有效</returns>
        public static bool ValidateRarityGroups(RarityItemGroup[] rarityGroups, out string errorMessage)
        {
            errorMessage = "";
            
            if (rarityGroups == null || rarityGroups.Length == 0)
            {
                errorMessage = "珍稀度组列表为空";
                return false;
            }
            
            // 提取权重
            float[] weights = new float[rarityGroups.Length];
            for (int i = 0; i < rarityGroups.Length; i++)
            {
                if (rarityGroups[i] == null)
                {
                    errorMessage = $"珍稀度组 [{i}] 为空";
                    return false;
                }
                weights[i] = rarityGroups[i].weight;
            }
            
            // 验证权重
            if (!ValidateWeights(weights))
            {
                float sum = 0f;
                foreach (float weight in weights)
                {
                    sum += weight;
                }
                errorMessage = $"权重总和 {sum:F3} 不等于 1.0，请调整权重配置";
                return false;
            }
            
            // 验证每个珍稀度组
            for (int i = 0; i < rarityGroups.Length; i++)
            {
                if (!rarityGroups[i].IsValid(out string groupError))
                {
                    errorMessage = $"珍稀度组 [{i}] 配置无效: {groupError}";
                    return false;
                }
            }
            
            return true;
        }
    }
}
