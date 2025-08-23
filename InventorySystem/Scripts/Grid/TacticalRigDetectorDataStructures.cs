// TacticalRigDetectorDataStructures.cs
// 战术挂具网格检测器相关数据结构定义
// 包含战术挂具配置分析、插槽分析、负载平衡等功能的数据结构

using System.Collections.Generic;
using UnityEngine;
using InventorySystem.Grid;

namespace InventorySystem.Grid
{
    // ==================== 战术挂具分析数据结构 ====================

    /// <summary>
    /// 装备配置类型枚举
    /// 定义不同的战术装备配置类型
    /// </summary>
    public enum LoadoutType
    {
        Assault,    // 突击配置 - 重武器和弹药为主
        Support,    // 支援配置 - 医疗用品和支援装备为主
        Marksman,   // 射手配置 - 精确射击装备为主
        Utility,    // 工具配置 - 各种工具和辅助装备为主
        Balanced    // 平衡配置 - 各类装备均衡分布
    }

    /// <summary>
    /// 战术挂具配置分析信息
    /// 用于分析战术挂具中装备的配置合理性和战术效能
    /// </summary>
    [System.Serializable]
    public class TacticalRigConfigInfo
    {
        [Header("基础信息")]
        public string gridID;                           // 网格唯一标识符
        public int totalSlots;                          // 网格总插槽数量
        public int usedSlots;                           // 当前已使用的插槽数量

        [Header("配置评分")]
        [Range(0f, 1f)]
        public float configurationScore;                // 整体配置评分 (0-1，1为最佳)
        [Range(0f, 1f)]
        public float tacticalEfficiency;                // 战术效率评分 (0-1，考虑实战应用)

        [Header("配置分析")]
        public LoadoutType loadoutType;                 // 当前装备配置类型
        [Range(0f, 1f)]
        public float equipmentBalance;                  // 装备类型平衡性 (0-1，1为完全平衡)
        [Range(0f, 1f)]
        public float accessibilityRating;               // 装备可访问性评级 (0-1，1为最易访问)
        [Range(0f, 1f)]
        public float combatReadiness;                   // 战斗准备度评分 (0-1，1为完全准备)

        [Header("建议和提示")]
        public List<string> suggestions;                // 配置改进建议列表
        public List<string> optimizationTips;           // 优化提示列表

        /// <summary>
        /// 构造函数，初始化列表
        /// </summary>
        public TacticalRigConfigInfo()
        {
            suggestions = new List<string>();
            optimizationTips = new List<string>();
        }

        /// <summary>
        /// 获取配置类型的中文描述
        /// </summary>
        /// <returns>配置类型的中文名称</returns>
        public string GetLoadoutTypeDescription()
        {
            switch (loadoutType)
            {
                case LoadoutType.Assault:
                    return "突击配置";
                case LoadoutType.Support:
                    return "支援配置";
                case LoadoutType.Marksman:
                    return "射手配置";
                case LoadoutType.Utility:
                    return "工具配置";
                case LoadoutType.Balanced:
                    return "平衡配置";
                default:
                    return "未知配置";
            }
        }

        /// <summary>
        /// 获取整体配置等级描述
        /// </summary>
        /// <returns>配置等级的文字描述</returns>
        public string GetConfigurationGrade()
        {
            if (configurationScore >= 0.9f)
                return "优秀";
            else if (configurationScore >= 0.7f)
                return "良好";
            else if (configurationScore >= 0.5f)
                return "一般";
            else if (configurationScore >= 0.3f)
                return "较差";
            else
                return "很差";
        }
    }

    /// <summary>
    /// 战术挂具插槽分析信息
    /// 用于分析各个插槽的使用效率和优化建议
    /// </summary>
    [System.Serializable]
    public class TacticalRigSlotAnalysis
    {
        [Header("基础信息")]
        public string gridID;                                       // 网格唯一标识符
        public int totalSlots;                                      // 网格总插槽数量
        public int usedSlots;                                       // 当前已使用插槽数量

        [Header("插槽效率分析")]
        public Dictionary<Vector2Int, float> slotEfficiency;        // 每个插槽的效率评分映射
        public List<Vector2Int> hotSpots;                           // 高效率插槽位置列表（热点区域）
        public List<Vector2Int> coldSpots;                          // 低效率插槽位置列表（冷点区域）

        [Header("使用建议")]
        public Dictionary<Vector2Int, string> recommendedSlotUsage; // 每个插槽的推荐用途映射

        /// <summary>
        /// 构造函数，初始化集合
        /// </summary>
        public TacticalRigSlotAnalysis()
        {
            slotEfficiency = new Dictionary<Vector2Int, float>();
            hotSpots = new List<Vector2Int>();
            coldSpots = new List<Vector2Int>();
            recommendedSlotUsage = new Dictionary<Vector2Int, string>();
        }

        /// <summary>
        /// 获取平均插槽效率
        /// </summary>
        /// <returns>所有插槽的平均效率值</returns>
        public float GetAverageSlotEfficiency()
        {
            if (slotEfficiency.Count == 0) return 0f;

            float total = 0f;
            foreach (var efficiency in slotEfficiency.Values)
            {
                total += efficiency;
            }
            return total / slotEfficiency.Count;
        }

        /// <summary>
        /// 获取热点区域数量
        /// </summary>
        /// <returns>热点插槽的数量</returns>
        public int GetHotSpotCount()
        {
            return hotSpots?.Count ?? 0;
        }

        /// <summary>
        /// 获取冷点区域数量
        /// </summary>
        /// <returns>冷点插槽的数量</returns>
        public int GetColdSpotCount()
        {
            return coldSpots?.Count ?? 0;
        }
    }

    /// <summary>
    /// 战术挂具负载平衡信息
    /// 用于分析挂具的重量分布和平衡性
    /// </summary>
    [System.Serializable]
    public class TacticalRigLoadBalance
    {
        [Header("重量信息")]
        public string gridID;                               // 网格唯一标识符
        public float totalWeight;                           // 挂具总重量（估算值）
        public Dictionary<string, float> weightDistribution; // 按装备类别的重量分布

        [Header("平衡分析")]
        [Range(0f, 1f)]
        public float balanceScore;                          // 负载平衡评分 (0-1，1为完美平衡)
        public Vector2 centerOfMass;                        // 重心位置坐标

        [Header("平衡建议")]
        public List<string> balanceRecommendations;         // 负载平衡改进建议列表

        /// <summary>
        /// 构造函数，初始化集合
        /// </summary>
        public TacticalRigLoadBalance()
        {
            weightDistribution = new Dictionary<string, float>();
            balanceRecommendations = new List<string>();
        }

        /// <summary>
        /// 获取最重的装备类别
        /// </summary>
        /// <returns>重量最大的装备类别名称</returns>
        public string GetHeaviestCategory()
        {
            if (weightDistribution.Count == 0) return "无";

            string heaviestCategory = "";
            float maxWeight = 0f;

            foreach (var kvp in weightDistribution)
            {
                if (kvp.Value > maxWeight)
                {
                    maxWeight = kvp.Value;
                    heaviestCategory = kvp.Key;
                }
            }

            return heaviestCategory;
        }

        /// <summary>
        /// 获取平衡状态描述
        /// </summary>
        /// <returns>平衡状态的文字描述</returns>
        public string GetBalanceStatusDescription()
        {
            if (balanceScore >= 0.9f)
                return "完美平衡";
            else if (balanceScore >= 0.7f)
                return "良好平衡";
            else if (balanceScore >= 0.5f)
                return "基本平衡";
            else if (balanceScore >= 0.3f)
                return "轻微失衡";
            else
                return "严重失衡";
        }

        /// <summary>
        /// 计算重心偏移距离
        /// </summary>
        /// <param name="gridWidth">网格宽度</param>
        /// <param name="gridHeight">网格高度</param>
        /// <returns>重心距离网格中心的偏移距离</returns>
        public float CalculateCenterOffset(int gridWidth, int gridHeight)
        {
            Vector2 gridCenter = new Vector2(gridWidth / 2f, gridHeight / 2f);
            return Vector2.Distance(centerOfMass, gridCenter);
        }
    }

    /// <summary>
    /// 战术挂具检测器事件参数
    /// 用于战术挂具相关事件的数据传递
    /// </summary>
    [System.Serializable]
    public class TacticalRigDetectorEventArgs
    {
        public string gridID;                           // 触发事件的网格ID
        public TacticalRigConfigInfo configInfo;        // 配置分析信息
        public TacticalRigSlotAnalysis slotAnalysis;    // 插槽分析信息
        public TacticalRigLoadBalance loadBalance;      // 负载平衡信息
        public System.DateTime timestamp;               // 事件时间戳

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="gridID">网格ID</param>
        public TacticalRigDetectorEventArgs(string gridID)
        {
            this.gridID = gridID;
            this.timestamp = System.DateTime.Now;
        }
    }
}