// BackpackDetectorDataStructures.cs
// 背包网格检测器专用数据结构定义
// 包含背包负重分析、整理建议、快速访问区域等功能的数据结构

using System.Collections.Generic;
using UnityEngine;
using InventorySystem.Grid;

namespace InventorySystem.Grid
{
    /// <summary>
    /// 背包负重分析信息
    /// 用于分析背包中物品的重量分布和负重状态
    /// </summary>
    [System.Serializable]
    public class BackpackWeightInfo
    {
        [Header("基础信息")]
        public string gridID;                                    // 网格标识符
        public int totalItems;                                   // 物品总数
        public float totalWeight;                                // 总重量
        public float averageWeight;                              // 平均重量

        [Header("重量统计")]
        public string heaviestItem;                              // 最重物品名称
        public string lightestItem;                              // 最轻物品名称
        public Dictionary<string, float> weightDistribution;     // 按类别的重量分布
        public List<string> overweightItems;                     // 超重物品列表

        [Header("负重建议")]
        public float recommendedMaxWeight;                       // 推荐最大负重
        public bool isOverloaded;                               // 是否超载
        public List<string> weightOptimizationSuggestions;      // 负重优化建议

        /// <summary>
        /// 构造函数，初始化集合
        /// </summary>
        public BackpackWeightInfo()
        {
            weightDistribution = new Dictionary<string, float>();
            overweightItems = new List<string>();
            weightOptimizationSuggestions = new List<string>();
        }

        /// <summary>
        /// 获取负重状态描述
        /// </summary>
        /// <returns>负重状态文本</returns>
        public string GetWeightStatusDescription()
        {
            if (totalWeight == 0f) return "背包为空";

            float loadPercentage = recommendedMaxWeight > 0 ? (totalWeight / recommendedMaxWeight) * 100f : 0f;

            if (loadPercentage < 50f)
                return $"负重轻松 ({loadPercentage:F1}%)";
            else if (loadPercentage < 80f)
                return $"负重适中 ({loadPercentage:F1}%)";
            else if (loadPercentage < 100f)
                return $"负重较重 ({loadPercentage:F1}%)";
            else
                return $"负重超载 ({loadPercentage:F1}%)";
        }
    }

    /// <summary>
    /// 背包整理建议信息
    /// 基于物品分布和使用频率提供整理建议
    /// </summary>
    [System.Serializable]
    public class BackpackOrganizationSuggestion
    {
        [Header("基础信息")]
        public string gridID;                                    // 网格标识符
        public float currentEfficiency;                          // 当前空间利用效率
        public int totalSuggestions;                             // 建议总数

        [Header("整理建议")]
        public List<string> suggestions;                         // 整理建议列表
        public List<string> priorityItems;                       // 优先级物品列表
        public List<string> redundantItems;                      // 冗余物品列表
        public List<string> misplacedItems;                      // 错位物品列表

        [Header("优化分析")]
        public float potentialEfficiencyGain;                    // 潜在效率提升
        public int estimatedTimeToOrganize;                      // 预估整理时间（秒）
        public OrganizationPriority overallPriority;             // 整体整理优先级

        /// <summary>
        /// 构造函数，初始化集合
        /// </summary>
        public BackpackOrganizationSuggestion()
        {
            suggestions = new List<string>();
            priorityItems = new List<string>();
            redundantItems = new List<string>();
            misplacedItems = new List<string>();
            overallPriority = OrganizationPriority.Low;
        }

        /// <summary>
        /// 获取整理优先级描述
        /// </summary>
        /// <returns>优先级描述文本</returns>
        public string GetPriorityDescription()
        {
            switch (overallPriority)
            {
                case OrganizationPriority.Critical:
                    return "紧急整理 - 背包混乱严重影响使用效率";
                case OrganizationPriority.High:
                    return "高优先级 - 建议尽快整理以提升效率";
                case OrganizationPriority.Medium:
                    return "中等优先级 - 有时间时可以整理";
                case OrganizationPriority.Low:
                    return "低优先级 - 背包组织良好";
                default:
                    return "未知优先级";
            }
        }
    }

    /// <summary>
    /// 整理优先级枚举
    /// </summary>
    public enum OrganizationPriority
    {
        Low = 0,        // 低优先级
        Medium = 1,     // 中等优先级
        High = 2,       // 高优先级
        Critical = 3    // 紧急优先级
    }

    /// <summary>
    /// 背包快速访问区域信息
    /// 分析背包中哪些区域适合放置常用物品
    /// </summary>
    [System.Serializable]
    public class BackpackQuickAccessInfo
    {
        [Header("基础信息")]
        public string gridID;                                    // 网格标识符
        public int totalZones;                                   // 快速访问区域总数
        public float overallAccessEfficiency;                    // 整体访问效率

        [Header("访问区域")]
        public List<QuickAccessZone> quickAccessZones;           // 快速访问区域列表
        public Dictionary<string, Vector2Int> recommendedPlacements; // 推荐放置位置

        [Header("使用统计")]
        public Dictionary<string, int> itemTypeAccessFrequency;   // 物品类型访问频率
        public List<string> mostAccessedItems;                   // 最常访问的物品
        public List<string> leastAccessedItems;                  // 最少访问的物品

        /// <summary>
        /// 构造函数，初始化集合
        /// </summary>
        public BackpackQuickAccessInfo()
        {
            quickAccessZones = new List<QuickAccessZone>();
            recommendedPlacements = new Dictionary<string, Vector2Int>();
            itemTypeAccessFrequency = new Dictionary<string, int>();
            mostAccessedItems = new List<string>();
            leastAccessedItems = new List<string>();
        }

        /// <summary>
        /// 获取访问效率评级
        /// </summary>
        /// <returns>效率评级文本</returns>
        public string GetAccessEfficiencyRating()
        {
            if (overallAccessEfficiency >= 0.9f)
                return "优秀 - 快速访问区域配置完美";
            else if (overallAccessEfficiency >= 0.7f)
                return "良好 - 快速访问区域配置合理";
            else if (overallAccessEfficiency >= 0.5f)
                return "一般 - 快速访问区域有改进空间";
            else
                return "较差 - 建议重新配置快速访问区域";
        }
    }

    /// <summary>
    /// 快速访问区域定义
    /// 定义背包中的特定区域及其推荐用途
    /// </summary>
    [System.Serializable]
    public class QuickAccessZone
    {
        [Header("区域定义")]
        public string zoneName;                                  // 区域名称
        public RectInt zoneArea;                                 // 区域范围
        public int priority;                                     // 优先级（1最高）
        public ZoneType zoneType;                               // 区域类型

        [Header("推荐配置")]
        public List<string> recommendedItemTypes;                // 推荐物品类型
        public List<string> currentItems;                       // 当前物品列表
        public float utilizationRate;                           // 利用率

        [Header("访问分析")]
        public int accessCount;                                  // 访问次数
        public float averageAccessTime;                         // 平均访问时间
        public bool isOptimallyUsed;                           // 是否最优使用

        /// <summary>
        /// 构造函数，初始化集合
        /// </summary>
        public QuickAccessZone()
        {
            recommendedItemTypes = new List<string>();
            currentItems = new List<string>();
            zoneType = ZoneType.General;
        }

        /// <summary>
        /// 获取区域状态描述
        /// </summary>
        /// <returns>区域状态文本</returns>
        public string GetZoneStatusDescription()
        {
            if (utilizationRate == 0f)
                return "空闲 - 区域未使用";
            else if (utilizationRate < 0.3f)
                return "低利用 - 区域使用不充分";
            else if (utilizationRate < 0.7f)
                return "适中 - 区域使用合理";
            else if (utilizationRate < 1.0f)
                return "高利用 - 区域使用充分";
            else
                return "满载 - 区域已满";
        }
    }

    /// <summary>
    /// 快速访问区域类型枚举
    /// </summary>
    public enum ZoneType
    {
        General = 0,        // 通用区域
        Combat = 1,         // 战斗区域
        Consumable = 2,     // 消耗品区域
        Tool = 3,           // 工具区域
        Weapon = 4,         // 武器区域
        Armor = 5,          // 防具区域
        Ammo = 6,           // 弹药区域
        Quest = 7,          // 任务物品区域
        Valuable = 8        // 贵重物品区域
    }

    /// <summary>
    /// 背包容量预警信息
    /// 提供背包容量相关的预警和建议
    /// </summary>
    [System.Serializable]
    public class BackpackCapacityWarning
    {
        [Header("容量状态")]
        public string gridID;                                    // 网格标识符
        public float currentCapacityUsage;                       // 当前容量使用率
        public int remainingSlots;                              // 剩余槽位数
        public CapacityWarningLevel warningLevel;               // 预警级别

        [Header("预警信息")]
        public List<string> warnings;                           // 预警消息列表
        public List<string> recommendations;                     // 推荐操作列表
        public bool requiresImmediateAction;                    // 是否需要立即行动

        [Header("容量分析")]
        public Dictionary<string, int> itemTypeSlotUsage;        // 各类型物品槽位使用情况
        public List<string> largestItems;                       // 最大的物品列表
        public float projectedFullTime;                         // 预计满载时间（小时）

        /// <summary>
        /// 构造函数，初始化集合
        /// </summary>
        public BackpackCapacityWarning()
        {
            warnings = new List<string>();
            recommendations = new List<string>();
            itemTypeSlotUsage = new Dictionary<string, int>();
            largestItems = new List<string>();
            warningLevel = CapacityWarningLevel.Normal;
        }

        /// <summary>
        /// 获取预警级别描述
        /// </summary>
        /// <returns>预警级别描述文本</returns>
        public string GetWarningLevelDescription()
        {
            switch (warningLevel)
            {
                case CapacityWarningLevel.Normal:
                    return "正常 - 背包容量充足";
                case CapacityWarningLevel.Low:
                    return "注意 - 背包容量偏低";
                case CapacityWarningLevel.Medium:
                    return "警告 - 背包容量不足";
                case CapacityWarningLevel.High:
                    return "严重 - 背包容量严重不足";
                case CapacityWarningLevel.Critical:
                    return "紧急 - 背包即将满载";
                default:
                    return "未知预警级别";
            }
        }
    }

    /// <summary>
    /// 容量预警级别枚举
    /// </summary>
    public enum CapacityWarningLevel
    {
        Normal = 0,     // 正常
        Low = 1,        // 低预警
        Medium = 2,     // 中等预警
        High = 3,       // 高预警
        Critical = 4    // 紧急预警
    }
}