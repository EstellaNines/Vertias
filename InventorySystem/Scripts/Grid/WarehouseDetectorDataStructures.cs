using System;
using System.Collections.Generic;
using UnityEngine;


namespace InventorySystem.Grid
{
    /// <summary>
    /// 仓库效率分析信息数据结构
    /// 用于分析仓库网格的存储效率和优化建议
    /// </summary>
    [System.Serializable]
    public class WarehouseEfficiencyInfo
    {
        [Header("基本容量信息")]
        public string gridID;                                    // 网格ID
        public int totalCapacity;                                // 总容量（格子数）
        public int usedCapacity;                                 // 已使用容量
        public int freeCapacity;                                 // 剩余容量
        public float storageEfficiency;                          // 存储效率（0-1）

        [Header("物品分类统计")]
        public Dictionary<string, int> itemCategories;           // 物品类别统计

        [Header("空间分析")]
        public float fragmentationLevel;                         // 碎片化程度（0-1）

        [Header("优化建议")]
        public List<string> optimizationSuggestions;             // 优化建议列表

        /// <summary>
        /// 获取效率分析摘要
        /// </summary>
        /// <returns>效率摘要字符串</returns>
        public string GetEfficiencySummary()
        {
            return $"仓库效率分析 - 容量利用率:{storageEfficiency:P1} 碎片化程度:{fragmentationLevel:P1} " +
                   $"物品类别数:{itemCategories.Count} 优化建议数:{optimizationSuggestions.Count}";
        }

        /// <summary>
        /// 获取最多的物品类别
        /// </summary>
        /// <returns>最多的物品类别名称</returns>
        public string GetMostCommonCategory()
        {
            if (itemCategories.Count == 0) return "无";

            string mostCommon = "";
            int maxCount = 0;

            foreach (var category in itemCategories)
            {
                if (category.Value > maxCount)
                {
                    maxCount = category.Value;
                    mostCommon = category.Key;
                }
            }

            return mostCommon;
        }
    }

    /// <summary>
    /// 仓库搜索条件数据结构
    /// 用于定义仓库物品搜索的各种条件
    /// </summary>
    [System.Serializable]
    public class WarehouseSearchCriteria
    {
        [Header("基本搜索条件")]
        public string itemName;                                  // 物品名称（支持模糊搜索）
        public InventorySystemItemCategory? itemType;            // 物品类型（null表示搜索所有类型）

        [Header("尺寸范围条件")]
        public Vector2Int minSize;                               // 最小尺寸
        public Vector2Int maxSize;                               // 最大尺寸

        [Header("位置范围条件")]
        public Vector2Int searchAreaStart;                       // 搜索区域起始位置
        public Vector2Int searchAreaSize;                        // 搜索区域尺寸
        public bool useAreaFilter;                               // 是否使用区域过滤

        /// <summary>
        /// 创建默认搜索条件（搜索所有物品）
        /// </summary>
        /// <returns>默认搜索条件</returns>
        public static WarehouseSearchCriteria CreateDefault()
        {
            return new WarehouseSearchCriteria
            {
                itemName = "",
                itemType = null,
                minSize = Vector2Int.zero,
                maxSize = Vector2Int.zero,
                searchAreaStart = Vector2Int.zero,
                searchAreaSize = Vector2Int.zero,
                useAreaFilter = false
            };
        }

        /// <summary>
        /// 创建按名称搜索的条件
        /// </summary>
        /// <param name="name">物品名称</param>
        /// <returns>按名称搜索的条件</returns>
        public static WarehouseSearchCriteria CreateByName(string name)
        {
            var criteria = CreateDefault();
            criteria.itemName = name;
            return criteria;
        }

        /// <summary>
        /// 创建按类型搜索的条件
        /// </summary>
        /// <param name="type">物品类型</param>
        /// <returns>按类型搜索的条件</returns>
        public static WarehouseSearchCriteria CreateByType(InventorySystemItemCategory? type)
        {
            var criteria = CreateDefault();
            criteria.itemType = type;
            return criteria;
        }
    }

    /// <summary>
    /// 物品搜索结果数据结构
    /// 包含搜索到的物品的详细信息
    /// </summary>
    [System.Serializable]
    public class ItemSearchResult
    {
        [Header("物品基本信息")]
        public string itemName;                                  // 物品名称
        public string itemType;                                  // 物品类型
        public string itemInstanceID;                            // 物品实例ID

        [Header("位置信息")]
        public Vector2Int gridPosition;                          // 网格位置
        public Vector2Int itemSize;                              // 物品尺寸
        public int placementIndex;                               // 在placedItems中的索引

        [Header("引用信息")]
        public GameObject itemGameObject;                        // 物品GameObject引用

        /// <summary>
        /// 获取搜索结果摘要
        /// </summary>
        /// <returns>结果摘要字符串</returns>
        public string GetResultSummary()
        {
            return $"物品[{itemName}] 类型:{itemType} 位置:({gridPosition.x},{gridPosition.y}) 尺寸:{itemSize.x}x{itemSize.y}";
        }

        /// <summary>
        /// 检查物品是否在指定区域内
        /// </summary>
        /// <param name="areaStart">区域起始位置</param>
        /// <param name="areaSize">区域尺寸</param>
        /// <returns>是否在区域内</returns>
        public bool IsInArea(Vector2Int areaStart, Vector2Int areaSize)
        {
            return gridPosition.x >= areaStart.x &&
                   gridPosition.y >= areaStart.y &&
                   gridPosition.x + itemSize.x <= areaStart.x + areaSize.x &&
                   gridPosition.y + itemSize.y <= areaStart.y + areaSize.y;
        }
    }

    /// <summary>
    /// 预警级别枚举
    /// 定义不同程度的预警级别
    /// </summary>
    public enum WarningLevel
    {
        None = 0,        // 无预警
        Low = 1,         // 低级预警
        Medium = 2,      // 中级预警
        High = 3,        // 高级预警
        Critical = 4     // 严重预警
    }

    /// <summary>
    /// 仓库容量预警信息数据结构
    /// 用于监控仓库容量状态并提供预警
    /// </summary>
    [System.Serializable]
    public class WarehouseCapacityWarning
    {
        [Header("基本预警信息")]
        public string gridID;                                    // 网格ID
        public float currentOccupancyRate;                       // 当前占用率
        public WarningLevel warningLevel;                        // 预警级别
        public string warningMessage;                            // 预警消息

        [Header("建议操作")]
        public List<string> recommendedActions;                   // 推荐操作列表

        /// <summary>
        /// 获取预警级别的颜色
        /// </summary>
        /// <returns>预警级别对应的颜色</returns>
        public Color GetWarningColor()
        {
            switch (warningLevel)
            {
                case WarningLevel.None:
                    return Color.green;
                case WarningLevel.Low:
                    return Color.yellow;
                case WarningLevel.Medium:
                    return new Color(1f, 0.5f, 0f); // 橙色
                case WarningLevel.High:
                    return Color.red;
                case WarningLevel.Critical:
                    return Color.magenta;
                default:
                    return Color.white;
            }
        }

        /// <summary>
        /// 获取预警级别的中文描述
        /// </summary>
        /// <returns>预警级别中文描述</returns>
        public string GetWarningLevelText()
        {
            switch (warningLevel)
            {
                case WarningLevel.None:
                    return "正常";
                case WarningLevel.Low:
                    return "低级预警";
                case WarningLevel.Medium:
                    return "中级预警";
                case WarningLevel.High:
                    return "高级预警";
                case WarningLevel.Critical:
                    return "严重预警";
                default:
                    return "未知";
            }
        }

        /// <summary>
        /// 检查是否需要立即处理
        /// </summary>
        /// <returns>是否需要立即处理</returns>
        public bool RequiresImmediateAction()
        {
            return warningLevel >= WarningLevel.High;
        }

        /// <summary>
        /// 获取预警摘要信息
        /// </summary>
        /// <returns>预警摘要字符串</returns>
        public string GetWarningSummary()
        {
            return $"仓库容量预警 - 占用率:{currentOccupancyRate:P1} 级别:{GetWarningLevelText()} " +
                   $"消息:{warningMessage} 建议操作数:{recommendedActions.Count}";
        }
    }
}