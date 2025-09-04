using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using InventorySystem;

namespace InventorySystem.SpawnSystem
{
    /// <summary>
    /// 智能放置策略
    /// 提供多种算法来寻找物品的最佳放置位置
    /// </summary>
    public class SmartPlacementStrategy
    {
        #region 私有字段
        
        private GridOccupancyAnalyzer analyzer;
        private bool enableDebugLog;
        
        #endregion
        
        #region 构造函数
        
        public SmartPlacementStrategy(GridOccupancyAnalyzer occupancyAnalyzer, bool debugLog = false)
        {
            analyzer = occupancyAnalyzer ?? throw new ArgumentNullException(nameof(occupancyAnalyzer));
            enableDebugLog = debugLog;
        }
        
        #endregion
        
        #region 主要放置方法
        
        /// <summary>
        /// 寻找最佳放置位置
        /// </summary>
        public Vector2Int? FindBestPosition(FixedItemTemplate template)
        {
            if (template == null || !analyzer.IsAnalyzed)
            {
                LogDebug("模板为空或分析器未分析");
                return null;
            }
            
            Vector2Int itemSize = template.GetItemSize();
            
            LogDebug($"开始为物品 {template.itemData.name} (尺寸: {itemSize}) 寻找位置");
            
            Vector2Int? position = null;
            
            switch (template.placementType)
            {
                case PlacementType.Exact:
                    position = TryExactPlacement(template);
                    break;
                    
                case PlacementType.Smart:
                    position = TrySmartPlacement(template);
                    break;
                    
                case PlacementType.AreaConstrained:
                    position = TryAreaConstrainedPlacement(template);
                    break;
                    
                case PlacementType.Priority:
                    position = TryPriorityPlacement(template);
                    break;
            }
            
            // 如果允许旋转且初始放置失败，尝试旋转
            if (!position.HasValue && template.allowRotation)
            {
                LogDebug("尝试旋转物品后重新放置");
                position = TryRotatedPlacement(template);
            }
            
            if (position.HasValue)
            {
                LogDebug($"找到位置: {position.Value}");
            }
            else
            {
                LogDebug("未找到合适位置");
            }
            
            return position;
        }
        
        #endregion
        
        #region 具体放置策略
        
        /// <summary>
        /// 精确位置放置
        /// </summary>
        private Vector2Int? TryExactPlacement(FixedItemTemplate template)
        {
            Vector2Int position = template.exactPosition;
            Vector2Int itemSize = template.GetItemSize();
            
            if (analyzer.CanPlaceItemAtPosition(position, itemSize))
            {
                return position;
            }
            
            LogDebug($"精确位置 {position} 不可用");
            return null;
        }
        
        /// <summary>
        /// 智能放置
        /// </summary>
        private Vector2Int? TrySmartPlacement(FixedItemTemplate template)
        {
            Vector2Int itemSize = template.GetItemSize();
            
            // 策略1: 寻找最大连续空间
            Vector2Int? position = FindLargestContinuousSpace(itemSize);
            if (position.HasValue) return position;
            
            // 策略2: 优化布局，减少碎片化
            position = FindOptimalPosition(template);
            if (position.HasValue) return position;
            
            // 策略3: 按扫描模式寻找
            position = FindPositionByScanPattern(template);
            return position;
        }
        
        /// <summary>
        /// 区域约束放置
        /// </summary>
        private Vector2Int? TryAreaConstrainedPlacement(FixedItemTemplate template)
        {
            RectInt constrainedArea = template.constrainedArea;
            Vector2Int itemSize = template.GetItemSize();
            
            return FindPositionInArea(constrainedArea, itemSize, template.scanPattern);
        }
        
        /// <summary>
        /// 优先级放置
        /// </summary>
        private Vector2Int? TryPriorityPlacement(FixedItemTemplate template)
        {
            // 先在优先区域查找
            Vector2Int? position = FindPositionInArea(template.preferredArea, 
                                                    template.GetItemSize(), 
                                                    template.scanPattern);
            if (position.HasValue) return position;
            
            // 优先区域失败，在整个网格查找
            LogDebug("优先区域放置失败，尝试全网格放置");
            return TrySmartPlacement(template);
        }
        
        /// <summary>
        /// 旋转放置尝试
        /// </summary>
        private Vector2Int? TryRotatedPlacement(FixedItemTemplate template)
        {
            Vector2Int rotatedSize = template.GetItemSize(true); // 获取旋转后的尺寸
            
            // 创建旋转后的临时模板
            var rotatedTemplate = new FixedItemTemplate
            {
                itemData = template.itemData,
                placementType = template.placementType,
                exactPosition = template.exactPosition,
                constrainedArea = template.constrainedArea,
                preferredArea = template.preferredArea,
                scanPattern = template.scanPattern,
                allowRotation = false // 避免递归
            };
            
            // 使用旋转后的尺寸查找位置
            switch (template.placementType)
            {
                case PlacementType.Exact:
                    return analyzer.CanPlaceItemAtPosition(template.exactPosition, rotatedSize) 
                           ? template.exactPosition : null;
                           
                case PlacementType.AreaConstrained:
                    return FindPositionInArea(template.constrainedArea, rotatedSize, template.scanPattern);
                    
                default:
                    return FindPositionByScanPattern(rotatedTemplate, rotatedSize);
            }
        }
        
        #endregion
        
        #region 高级算法
        
        /// <summary>
        /// 寻找最大连续空间
        /// </summary>
        private Vector2Int? FindLargestContinuousSpace(Vector2Int itemSize)
        {
            var largestSpace = analyzer.GetLargestAvailableSpace();
            
            if (largestSpace.size.x >= itemSize.x && largestSpace.size.y >= itemSize.y)
            {
                LogDebug($"最大空间位置: {largestSpace.position}, 尺寸: {largestSpace.size}");
                
                // 在最大空间内扫描寻找实际可用的位置
                RectInt searchArea = new RectInt(largestSpace.position.x, largestSpace.position.y, 
                                               largestSpace.size.x, largestSpace.size.y);
                
                Vector2Int? position = FindPositionInArea(searchArea, itemSize, ScanPattern.LeftToRight);
                
                if (position.HasValue)
                {
                    LogDebug($"在最大连续空间中找到可用位置: {position.Value}");
                }
                else
                {
                    LogDebug("最大连续空间中无法找到可用位置");
                }
                
                return position;
            }
            
            LogDebug($"最大空间尺寸 {largestSpace.size} 不足以容纳物品 {itemSize}");
            return null;
        }
        
        /// <summary>
        /// 寻找最优位置（考虑布局优化）
        /// </summary>
        private Vector2Int? FindOptimalPosition(FixedItemTemplate template)
        {
            Vector2Int itemSize = template.GetItemSize();
            Vector2Int? bestPosition = null;
            float bestScore = float.MinValue;
            
            // 扫描所有可能的位置
            for (int y = 0; y <= analyzer.GridHeight - itemSize.y; y++)
            {
                for (int x = 0; x <= analyzer.GridWidth - itemSize.x; x++)
                {
                    Vector2Int position = new Vector2Int(x, y);
                    
                    if (analyzer.CanPlaceItemAtPosition(position, itemSize))
                    {
                        float score = CalculatePositionScore(position, itemSize, template);
                        
                        if (score > bestScore)
                        {
                            bestScore = score;
                            bestPosition = position;
                        }
                    }
                }
            }
            
            if (bestPosition.HasValue)
            {
                LogDebug($"最优位置: {bestPosition.Value}, 得分: {bestScore:F2}");
            }
            
            return bestPosition;
        }
        
        /// <summary>
        /// 按扫描模式寻找位置
        /// </summary>
        private Vector2Int? FindPositionByScanPattern(FixedItemTemplate template, Vector2Int? customSize = null)
        {
            Vector2Int itemSize = customSize ?? template.GetItemSize();
            RectInt scanArea = template.GetScanArea(analyzer.TargetGrid);
            
            return FindPositionInArea(scanArea, itemSize, template.scanPattern);
        }
        
        /// <summary>
        /// 在指定区域内寻找位置
        /// </summary>
        private Vector2Int? FindPositionInArea(RectInt area, Vector2Int itemSize, ScanPattern pattern)
        {
            var scanPositions = GenerateScanPositions(area, pattern);
            
            foreach (var position in scanPositions)
            {
                if (analyzer.CanPlaceItemAtPosition(position, itemSize))
                {
                    return position;
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// 生成扫描位置序列
        /// </summary>
        private IEnumerable<Vector2Int> GenerateScanPositions(RectInt area, ScanPattern pattern)
        {
            switch (pattern)
            {
                case ScanPattern.LeftToRight:
                    return ScanLeftToRight(area);
                    
                case ScanPattern.TopToBottom:
                    return ScanTopToBottom(area);
                    
                case ScanPattern.SpiralOut:
                    return ScanSpiral(area);
                    
                case ScanPattern.CenterToEdge:
                    return ScanCenterToEdge(area);
                    
                case ScanPattern.LargestGapFirst:
                    return ScanLargestGapFirst(area);
                    
                default:
                    return ScanLeftToRight(area);
            }
        }
        
        #endregion
        
        #region 扫描算法
        
        /// <summary>
        /// 从左到右扫描
        /// </summary>
        private IEnumerable<Vector2Int> ScanLeftToRight(RectInt area)
        {
            for (int y = area.y; y < area.y + area.height; y++)
            {
                for (int x = area.x; x < area.x + area.width; x++)
                {
                    yield return new Vector2Int(x, y);
                }
            }
        }
        
        /// <summary>
        /// 从上到下扫描
        /// </summary>
        private IEnumerable<Vector2Int> ScanTopToBottom(RectInt area)
        {
            for (int x = area.x; x < area.x + area.width; x++)
            {
                for (int y = area.y; y < area.y + area.height; y++)
                {
                    yield return new Vector2Int(x, y);
                }
            }
        }
        
        /// <summary>
        /// 螺旋扫描
        /// </summary>
        private IEnumerable<Vector2Int> ScanSpiral(RectInt area)
        {
            var center = new Vector2Int(Mathf.RoundToInt(area.center.x), Mathf.RoundToInt(area.center.y));
            var visited = new HashSet<Vector2Int>();
            var queue = new Queue<Vector2Int>();
            
            if (area.Contains(center))
            {
                queue.Enqueue(center);
                visited.Add(center);
            }
            
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                yield return current;
                
                // 添加相邻位置
                Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
                
                foreach (var direction in directions)
                {
                    var neighbor = current + direction;
                    
                    if (area.Contains(neighbor) && !visited.Contains(neighbor))
                    {
                        visited.Add(neighbor);
                        queue.Enqueue(neighbor);
                    }
                }
            }
        }
        
        /// <summary>
        /// 从中心向边缘扫描
        /// </summary>
        private IEnumerable<Vector2Int> ScanCenterToEdge(RectInt area)
        {
            var center = new Vector2Int(Mathf.RoundToInt(area.center.x), Mathf.RoundToInt(area.center.y));
            var positions = new List<Vector2Int>();
            
            for (int y = area.y; y < area.y + area.height; y++)
            {
                for (int x = area.x; x < area.x + area.width; x++)
                {
                    positions.Add(new Vector2Int(x, y));
                }
            }
            
            // 按距离中心的距离排序
            return positions.OrderBy(pos => Vector2Int.Distance(pos, center));
        }
        
        /// <summary>
        /// 最大空隙优先扫描
        /// </summary>
        private IEnumerable<Vector2Int> ScanLargestGapFirst(RectInt area)
        {
            var suitableSpaces = analyzer.GetSuitableSpaces(Vector2Int.one);
            
            // 按可用空间大小排序
            var sortedSpaces = suitableSpaces.OrderByDescending(space => space.area);
            
            foreach (var space in sortedSpaces)
            {
                if (area.Overlaps(new RectInt(space.position.x, space.position.y, space.size.x, space.size.y)))
                {
                    yield return space.position;
                }
            }
        }
        
        #endregion
        
        #region 评分系统
        
        /// <summary>
        /// 计算位置得分
        /// </summary>
        private float CalculatePositionScore(Vector2Int position, Vector2Int itemSize, FixedItemTemplate template)
        {
            float score = 0f;
            
            // 评分因子1: 避免创建碎片空间
            score += EvaluateFragmentation(position, itemSize) * 0.4f;
            
            // 评分因子2: 边缘贴合优先
            score += EvaluateEdgeAlignment(position, itemSize) * 0.3f;
            
            // 评分因子3: 优先区域加分
            score += EvaluatePreferredArea(position, template) * 0.2f;
            
            // 评分因子4: 紧凑布局
            score += EvaluateCompactness(position, itemSize) * 0.1f;
            
            return score;
        }
        
        /// <summary>
        /// 评估碎片化程度
        /// </summary>
        private float EvaluateFragmentation(Vector2Int position, Vector2Int itemSize)
        {
            // 检查放置后是否会创建小的无用空间
            float score = 100f; // 基础分数
            
            // 模拟放置
            analyzer.SimulatePlacement(position, itemSize);
            
            // 检查剩余空间的碎片化程度
            var remainingSpaces = analyzer.GetSuitableSpaces(Vector2Int.one);
            float fragmentationPenalty = remainingSpaces.Count(space => space.area < 4) * 10f;
            
            // 撤销模拟
            analyzer.UndoSimulation();
            
            return score - fragmentationPenalty;
        }
        
        /// <summary>
        /// 评估边缘对齐
        /// </summary>
        private float EvaluateEdgeAlignment(Vector2Int position, Vector2Int itemSize)
        {
            float score = 0f;
            
            // 靠近左边缘
            if (position.x == 0) score += 20f;
            
            // 靠近上边缘  
            if (position.y == 0) score += 20f;
            
            // 靠近右边缘
            if (position.x + itemSize.x == analyzer.GridWidth) score += 15f;
            
            // 靠近下边缘
            if (position.y + itemSize.y == analyzer.GridHeight) score += 15f;
            
            return score;
        }
        
        /// <summary>
        /// 评估是否在优先区域
        /// </summary>
        private float EvaluatePreferredArea(Vector2Int position, FixedItemTemplate template)
        {
            if (template.IsInPreferredArea(position))
            {
                return 50f;
            }
            
            return 0f;
        }
        
        /// <summary>
        /// 评估紧凑程度
        /// </summary>
        private float EvaluateCompactness(Vector2Int position, Vector2Int itemSize)
        {
            float score = 0f;
            
            // 检查相邻格子的占用情况
            Vector2Int[] checkPositions = 
            {
                position + Vector2Int.left,
                position + Vector2Int.up,
                position + new Vector2Int(itemSize.x, 0),
                position + new Vector2Int(0, itemSize.y)
            };
            
            foreach (var checkPos in checkPositions)
            {
                if (checkPos.x >= 0 && checkPos.x < analyzer.GridWidth &&
                    checkPos.y >= 0 && checkPos.y < analyzer.GridHeight)
                {
                    var occupancyMap = analyzer.GetOccupancyMapCopy();
                    if (occupancyMap[checkPos.x, checkPos.y])
                    {
                        score += 10f; // 相邻有物品，增加紧凑度分数
                    }
                }
            }
            
            return score;
        }
        
        #endregion
        
        #region 调试方法
        
        private void LogDebug(string message)
        {
            if (enableDebugLog)
            {
                Debug.Log($"SmartPlacementStrategy: {message}");
            }
        }
        
        #endregion
    }
}
