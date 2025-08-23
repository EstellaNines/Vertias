using System;
using System.Collections.Generic;
using UnityEngine;

namespace InventorySystem.Grid
{
    /// <summary>
    /// 网格检测器详细信息数据结构
    /// 包含网格的完整状态信息，用于监控和调试
    /// </summary>
    [System.Serializable]
    public class GridDetectorInfo
    {
        [Header("基本网格信息")]
        public string gridID;                                    // 网格唯一标识符
        public string gridType;                                  // 网格类型名称
        public Vector2Int gridSize;                              // 网格尺寸（宽x高）
        public int totalCells;                                   // 总格子数量

        [Header("占用状态信息")]
        public int occupiedCellsCount;                           // 已占用格子数量
        public float occupancyRate;                              // 占用率（0-1）
        public int availableCells;                               // 可用格子数量

        [Header("物品分布信息")]
        public int placedItemsCount;                             // 已放置物品数量
        public Dictionary<string, ItemPlacementInfo> itemDistribution; // 物品分布映射
        public int[,] occupancyMatrix;                           // 占用矩阵（0=空闲，1=占用）

        [Header("时间戳信息")]
        public string lastModified;                              // 最后修改时间
        public string detectionTime;                             // 检测时间

        [Header("网格配置信息")]
        public Vector2 cellSize;                                 // 单个格子尺寸
        public Vector3 gridWorldPosition;                        // 网格世界坐标
        public bool isActive;                                    // 网格是否激活

        /// <summary>
        /// 获取网格状态摘要信息
        /// </summary>
        /// <returns>状态摘要字符串</returns>
        public string GetStatusSummary()
        {
            return $"网格[{gridType}] ID:{gridID} 尺寸:{gridSize.x}x{gridSize.y} " +
                   $"占用率:{occupancyRate:P1} 物品数:{placedItemsCount} 状态:{(isActive ? "激活" : "未激活")}";
        }

    }

    /// <summary>
    /// 物品放置信息数据结构
    /// 记录单个物品在网格中的详细放置信息
    /// </summary>
    [System.Serializable]
    public class ItemPlacementInfo
    {
        [Header("物品标识信息")]
        public string itemInstanceID;                            // 物品实例ID
        public string itemName;                                  // 物品GameObject名称
        public string itemDataName;                              // 物品数据名称
        public string itemDataPath;                              // 物品数据资源路径

        [Header("位置和尺寸信息")]
        public Vector2Int gridPosition;                          // 网格中的位置
        public Vector2Int itemSize;                              // 物品尺寸
        public List<Vector2Int> occupiedCells;                   // 占用的所有格子坐标

        [Header("放置状态信息")]
        public int placementIndex;                               // 在placedItems列表中的索引
        public GameObject itemGameObject;                        // 物品GameObject引用

        /// <summary>
        /// 获取物品放置摘要信息
        /// </summary>
        /// <returns>放置摘要字符串</returns>
        public string GetPlacementSummary()
        {
            return $"物品[{itemDataName ?? itemName}] 位置:({gridPosition.x},{gridPosition.y}) " +
                   $"尺寸:{itemSize.x}x{itemSize.y} 占用格子数:{occupiedCells?.Count ?? 0}";
        }
    }

    /// <summary>
    /// 区域占用信息数据结构
    /// 用于检测指定区域的占用详情
    /// </summary>
    [System.Serializable]
    public class AreaOccupancyInfo
    {
        [Header("区域基本信息")]
        public Vector2Int areaPosition;                          // 区域起始位置
        public Vector2Int areaSize;                              // 区域尺寸
        public int totalCells;                                   // 区域总格子数

        [Header("占用状态统计")]
        public int occupiedCellsCount;                           // 已占用格子数量
        public int freeCellsCount;                               // 空闲格子数量
        public bool canPlaceItem;                                // 是否可以放置物品
        public string errorMessage;                              // 错误信息（如果有）

        [Header("详细占用信息")]
        public List<CellOccupancyInfo> occupiedCells;            // 已占用格子详情列表
        public List<Vector2Int> freeCells;                       // 空闲格子坐标列表

        /// <summary>
        /// 获取区域占用摘要信息
        /// </summary>
        /// <returns>占用摘要字符串</returns>
        public string GetOccupancySummary()
        {
            float occupancyRate = totalCells > 0 ? (float)occupiedCellsCount / totalCells : 0f;
            return $"区域[{areaPosition.x},{areaPosition.y}] 尺寸:{areaSize.x}x{areaSize.y} " +
                   $"占用率:{occupancyRate:P1} 可放置:{(canPlaceItem ? "是" : "否")}";
        }
    }

    /// <summary>
    /// 单个格子占用信息数据结构
    /// 记录单个格子的占用状态和占用物品信息
    /// </summary>
    [System.Serializable]
    public class CellOccupancyInfo
    {
        [Header("格子位置信息")]
        public Vector2Int cellPosition;                          // 格子坐标
        public bool isOccupied;                                  // 是否被占用

        [Header("占用物品信息")]
        public string occupyingItemName;                         // 占用物品名称
        public string occupyingItemInstanceID;                   // 占用物品实例ID

        /// <summary>
        /// 获取格子占用摘要信息
        /// </summary>
        /// <returns>占用摘要字符串</returns>
        public string GetCellSummary()
        {
            if (isOccupied)
            {
                return $"格子({cellPosition.x},{cellPosition.y}) 被占用 - 物品:{occupyingItemName}";
            }
            else
            {
                return $"格子({cellPosition.x},{cellPosition.y}) 空闲";
            }
        }
    }

    /// <summary>
    /// 可用空间信息数据结构
    /// 用于描述网格中连续的可用空间区域
    /// </summary>
    [System.Serializable]
    public class AvailableSpaceInfo
    {
        [Header("空间区域信息")]
        public Vector2Int startPosition;                         // 空间起始位置
        public int totalCells;                                   // 空间总格子数
        public List<Vector2Int> availableCells;                  // 可用格子坐标列表
        public RectInt boundingBox;                              // 空间边界框

        [Header("容纳能力信息")]
        public Vector2Int maxItemSize;                           // 能容纳的最大物品尺寸

        /// <summary>
        /// 获取可用空间摘要信息
        /// </summary>
        /// <returns>空间摘要字符串</returns>
        public string GetSpaceSummary()
        {
            return $"可用空间[{startPosition.x},{startPosition.y}] 格子数:{totalCells} " +
                   $"最大物品尺寸:{maxItemSize.x}x{maxItemSize.y} 边界:{boundingBox}";
        }

        /// <summary>
        /// 检查是否能容纳指定尺寸的物品
        /// </summary>
        /// <param name="itemSize">物品尺寸</param>
        /// <returns>是否能容纳</returns>
        public bool CanFitItem(Vector2Int itemSize)
        {
            return itemSize.x <= maxItemSize.x && itemSize.y <= maxItemSize.y;
        }

        /// <summary>
        /// 获取推荐的物品放置位置
        /// </summary>
        /// <param name="itemSize">物品尺寸</param>
        /// <returns>推荐位置，如果无法放置则返回(-1,-1)</returns>
        public Vector2Int GetRecommendedPlacement(Vector2Int itemSize)
        {
            if (!CanFitItem(itemSize))
            {
                return new Vector2Int(-1, -1);
            }

            // 尝试在边界框内找到合适的位置
            for (int x = boundingBox.x; x <= boundingBox.xMax - itemSize.x; x++)
            {
                for (int y = boundingBox.y; y <= boundingBox.yMax - itemSize.y; y++)
                {
                    Vector2Int testPos = new Vector2Int(x, y);
                    bool canPlace = true;

                    // 检查这个位置是否所有需要的格子都可用
                    for (int dx = 0; dx < itemSize.x && canPlace; dx++)
                    {
                        for (int dy = 0; dy < itemSize.y && canPlace; dy++)
                        {
                            Vector2Int checkPos = new Vector2Int(x + dx, y + dy);
                            if (!availableCells.Contains(checkPos))
                            {
                                canPlace = false;
                            }
                        }
                    }

                    if (canPlace)
                    {
                        return testPos;
                    }
                }
            }

            return new Vector2Int(-1, -1);
        }
    }

    /// <summary>
    /// 网格检测器事件参数
    /// 用于网格状态变化时的事件通知
    /// </summary>
    [System.Serializable]
    public class GridDetectorEventArgs : EventArgs
    {
        public string gridID;                                    // 网格ID
        public string eventType;                                 // 事件类型
        public string eventDescription;                          // 事件描述
        public DateTime eventTime;                               // 事件时间
        public GridDetectorInfo gridInfo;                        // 网格状态信息

        public GridDetectorEventArgs(string gridID, string eventType, string description, GridDetectorInfo info)
        {
            this.gridID = gridID;
            this.eventType = eventType;
            this.eventDescription = description;
            this.eventTime = DateTime.Now;
            this.gridInfo = info;
        }
    }
}