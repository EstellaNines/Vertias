using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using InventorySystem;

namespace InventorySystem.SpawnSystem
{
    /// <summary>
    /// 网格空间信息
    /// </summary>
    [System.Serializable]
    public struct GridSpaceInfo
    {
        public Vector2Int position;     // 空间位置
        public Vector2Int size;         // 空间大小
        public int area;               // 空间面积
        public bool isConnected;       // 是否为连续空间
        
        public GridSpaceInfo(Vector2Int pos, Vector2Int sz)
        {
            position = pos;
            size = sz;
            area = sz.x * sz.y;
            isConnected = true;
        }
    }
    
    /// <summary>
    /// 物品占用信息
    /// </summary>
    [System.Serializable]
    public struct ItemOccupancyInfo
    {
        public Item item;               // 物品引用
        public Vector2Int position;     // 物品位置
        public Vector2Int size;         // 物品尺寸
        public List<Vector2Int> occupiedPositions; // 占用的所有格子位置
        
        public ItemOccupancyInfo(Item itm, Vector2Int pos, Vector2Int sz, List<Vector2Int> occupied)
        {
            item = itm;
            position = pos;
            size = sz;
            occupiedPositions = occupied ?? new List<Vector2Int>();
        }
    }
    
    /// <summary>
    /// 网格占用分析器
    /// 负责分析网格的占用状态，检测可用空间，为智能放置提供数据支持
    /// </summary>
    public class GridOccupancyAnalyzer
    {
        #region 私有字段
        
        private ItemGrid targetGrid;
        private bool[,] occupancyMap;
        private Dictionary<Vector2Int, Item> positionToItemMap;
        private List<ItemOccupancyInfo> existingItems;
        private List<GridSpaceInfo> availableSpaces;
        private bool isAnalyzed;
        
        #endregion
        
        #region 公共属性
        
        /// <summary>
        /// 目标网格
        /// </summary>
        public ItemGrid TargetGrid => targetGrid;
        
        /// <summary>
        /// 网格宽度
        /// </summary>
        public int GridWidth => targetGrid?.CurrentWidth ?? 0;
        
        /// <summary>
        /// 网格高度
        /// </summary>
        public int GridHeight => targetGrid?.CurrentHeight ?? 0;
        
        /// <summary>
        /// 总格子数
        /// </summary>
        public int TotalSlots => GridWidth * GridHeight;
        
        /// <summary>
        /// 已占用格子数
        /// </summary>
        public int OccupiedSlots { get; private set; }
        
        /// <summary>
        /// 可用格子数
        /// </summary>
        public int AvailableSlots => TotalSlots - OccupiedSlots;
        
        /// <summary>
        /// 占用率
        /// </summary>
        public float OccupancyRate => TotalSlots > 0 ? (float)OccupiedSlots / TotalSlots : 0f;
        
        /// <summary>
        /// 是否已分析
        /// </summary>
        public bool IsAnalyzed => isAnalyzed;
        
        #endregion
        
        #region 构造函数
        
        public GridOccupancyAnalyzer()
        {
            Initialize();
        }
        
        public GridOccupancyAnalyzer(ItemGrid grid)
        {
            Initialize();
            SetTargetGrid(grid);
        }
        
        #endregion
        
        #region 初始化方法
        
        /// <summary>
        /// 初始化分析器
        /// </summary>
        private void Initialize()
        {
            positionToItemMap = new Dictionary<Vector2Int, Item>();
            existingItems = new List<ItemOccupancyInfo>();
            availableSpaces = new List<GridSpaceInfo>();
            isAnalyzed = false;
        }
        
        /// <summary>
        /// 设置目标网格
        /// </summary>
        public void SetTargetGrid(ItemGrid grid)
        {
            if (grid == null)
            {
                Debug.LogWarning("GridOccupancyAnalyzer: 目标网格不能为空");
                return;
            }
            
            targetGrid = grid;
            isAnalyzed = false;
        }
        
        #endregion
        
        #region 分析方法
        
        /// <summary>
        /// 分析网格占用状态
        /// </summary>
        public void AnalyzeGrid(bool forceRefresh = false)
        {
            if (targetGrid == null)
            {
                Debug.LogError("GridOccupancyAnalyzer: 目标网格未设置");
                return;
            }
            
            if (isAnalyzed && !forceRefresh) return;
            
            Debug.Log($"GridOccupancyAnalyzer: 开始分析网格 {targetGrid.name} ({GridWidth}x{GridHeight})");
            
            // 重置数据
            ResetAnalysisData();
            
            // 初始化占用地图
            InitializeOccupancyMap();
            
            // 扫描现有物品
            ScanExistingItems();
            
            // 分析可用空间
            AnalyzeAvailableSpaces();
            
            isAnalyzed = true;
            
            LogAnalysisResults();
        }
        
        /// <summary>
        /// 重置分析数据
        /// </summary>
        private void ResetAnalysisData()
        {
            positionToItemMap.Clear();
            existingItems.Clear();
            availableSpaces.Clear();
            OccupiedSlots = 0;
        }
        
        /// <summary>
        /// 初始化占用地图
        /// </summary>
        private void InitializeOccupancyMap()
        {
            occupancyMap = new bool[GridWidth, GridHeight];
            
            // 初始化为未占用
            for (int x = 0; x < GridWidth; x++)
            {
                for (int y = 0; y < GridHeight; y++)
                {
                    occupancyMap[x, y] = false;
                }
            }
        }
        
        /// <summary>
        /// 扫描现有物品
        /// </summary>
        private void ScanExistingItems()
        {
            var processedItems = new HashSet<Item>();
            
            for (int x = 0; x < GridWidth; x++)
            {
                for (int y = 0; y < GridHeight; y++)
                {
                    Item item = targetGrid.GetItemAt(x, y);
                    if (item != null && !processedItems.Contains(item))
                    {
                        ProcessExistingItem(item, new Vector2Int(x, y));
                        processedItems.Add(item);
                    }
                }
            }
            
            Debug.Log($"GridOccupancyAnalyzer: 找到 {existingItems.Count} 个现有物品");
        }
        
        /// <summary>
        /// 处理现有物品
        /// </summary>
        private void ProcessExistingItem(Item item, Vector2Int detectedPosition)
        {
            Vector2Int itemSize = new Vector2Int(item.GetWidth(), item.GetHeight());
            Vector2Int itemPosition = FindItemOriginPosition(item, detectedPosition);
            
            var occupiedPositions = new List<Vector2Int>();
            
            // 标记物品占用的所有格子
            for (int dx = 0; dx < itemSize.x; dx++)
            {
                for (int dy = 0; dy < itemSize.y; dy++)
                {
                    int x = itemPosition.x + dx;
                    int y = itemPosition.y + dy;
                    
                    if (IsValidPosition(x, y))
                    {
                        occupancyMap[x, y] = true;
                        positionToItemMap[new Vector2Int(x, y)] = item;
                        occupiedPositions.Add(new Vector2Int(x, y));
                        OccupiedSlots++;
                    }
                }
            }
            
            // 记录物品信息
            var itemInfo = new ItemOccupancyInfo(item, itemPosition, itemSize, occupiedPositions);
            existingItems.Add(itemInfo);
            
            Debug.Log($"GridOccupancyAnalyzer: 物品 {item.name} 占用区域 {itemPosition} 到 " +
                     $"{itemPosition + itemSize - Vector2Int.one} (面积: {itemSize.x * itemSize.y})");
        }
        
        /// <summary>
        /// 查找物品的原点位置
        /// </summary>
        private Vector2Int FindItemOriginPosition(Item item, Vector2Int detectedPosition)
        {
            // 简化实现：假设检测到的位置就是原点
            // 在实际实现中，可能需要更复杂的逻辑来确定物品的真实原点
            return detectedPosition;
        }
        
        /// <summary>
        /// 分析可用空间
        /// </summary>
        private void AnalyzeAvailableSpaces()
        {
            var visited = new bool[GridWidth, GridHeight];
            
            for (int x = 0; x < GridWidth; x++)
            {
                for (int y = 0; y < GridHeight; y++)
                {
                    if (!occupancyMap[x, y] && !visited[x, y])
                    {
                        var spaceInfo = AnalyzeContinuousSpace(new Vector2Int(x, y), visited);
                        if (spaceInfo.area > 0)
                        {
                            availableSpaces.Add(spaceInfo);
                        }
                    }
                }
            }
            
            // 按面积大小排序可用空间
            availableSpaces = availableSpaces.OrderByDescending(space => space.area).ToList();
            
            Debug.Log($"GridOccupancyAnalyzer: 找到 {availableSpaces.Count} 个可用空间区域");
        }
        
        /// <summary>
        /// 分析连续空间
        /// </summary>
        private GridSpaceInfo AnalyzeContinuousSpace(Vector2Int startPos, bool[,] visited)
        {
            var queue = new Queue<Vector2Int>();
            var spacePositions = new List<Vector2Int>();
            
            queue.Enqueue(startPos);
            visited[startPos.x, startPos.y] = true;
            
            Vector2Int minPos = startPos;
            Vector2Int maxPos = startPos;
            
            while (queue.Count > 0)
            {
                Vector2Int current = queue.Dequeue();
                spacePositions.Add(current);
                
                // 更新边界
                minPos = Vector2Int.Min(minPos, current);
                maxPos = Vector2Int.Max(maxPos, current);
                
                // 检查四个方向的相邻格子
                Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
                
                foreach (var direction in directions)
                {
                    Vector2Int neighbor = current + direction;
                    
                    if (IsValidPosition(neighbor.x, neighbor.y) &&
                        !occupancyMap[neighbor.x, neighbor.y] &&
                        !visited[neighbor.x, neighbor.y])
                    {
                        visited[neighbor.x, neighbor.y] = true;
                        queue.Enqueue(neighbor);
                    }
                }
            }
            
            Vector2Int size = maxPos - minPos + Vector2Int.one;
            return new GridSpaceInfo(minPos, size);
        }
        
        #endregion
        
        #region 查询方法
        
        /// <summary>
        /// 检查位置是否可以放置指定尺寸的物品
        /// </summary>
        public bool CanPlaceItemAtPosition(Vector2Int position, Vector2Int itemSize)
        {
            if (!isAnalyzed)
            {
                Debug.LogWarning("GridOccupancyAnalyzer: 请先调用AnalyzeGrid()进行分析");
                return false;
            }
            
            // 边界检查
            if (position.x < 0 || position.y < 0 ||
                position.x + itemSize.x > GridWidth ||
                position.y + itemSize.y > GridHeight)
            {
                return false;
            }
            
            // 占用检查
            for (int dx = 0; dx < itemSize.x; dx++)
            {
                for (int dy = 0; dy < itemSize.y; dy++)
                {
                    int x = position.x + dx;
                    int y = position.y + dy;
                    
                    if (occupancyMap[x, y])
                    {
                        return false;
                    }
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// 获取指定位置的冲突物品
        /// </summary>
        public List<Item> GetConflictingItems(Vector2Int position, Vector2Int itemSize)
        {
            var conflictingItems = new HashSet<Item>();
            
            for (int dx = 0; dx < itemSize.x; dx++)
            {
                for (int dy = 0; dy < itemSize.y; dy++)
                {
                    Vector2Int checkPos = position + new Vector2Int(dx, dy);
                    
                    if (positionToItemMap.ContainsKey(checkPos))
                    {
                        conflictingItems.Add(positionToItemMap[checkPos]);
                    }
                }
            }
            
            return conflictingItems.ToList();
        }
        
        /// <summary>
        /// 获取最大可用连续空间
        /// </summary>
        public GridSpaceInfo GetLargestAvailableSpace()
        {
            if (!isAnalyzed || availableSpaces.Count == 0)
                return new GridSpaceInfo();
            
            return availableSpaces[0]; // 已按面积排序
        }
        
        /// <summary>
        /// 获取适合指定尺寸的可用空间列表
        /// </summary>
        public List<GridSpaceInfo> GetSuitableSpaces(Vector2Int requiredSize)
        {
            if (!isAnalyzed)
                return new List<GridSpaceInfo>();
            
            return availableSpaces.Where(space => 
                space.size.x >= requiredSize.x && space.size.y >= requiredSize.y).ToList();
        }
        
        /// <summary>
        /// 获取所有现有物品信息
        /// </summary>
        public List<ItemOccupancyInfo> GetExistingItems()
        {
            return new List<ItemOccupancyInfo>(existingItems);
        }
        
        /// <summary>
        /// 获取占用地图的副本
        /// </summary>
        public bool[,] GetOccupancyMapCopy()
        {
            if (occupancyMap == null) return null;
            
            var copy = new bool[GridWidth, GridHeight];
            Array.Copy(occupancyMap, copy, occupancyMap.Length);
            return copy;
        }
        
        #endregion
        
        #region 更新方法
        
        /// <summary>
        /// 模拟放置物品后的占用状态
        /// </summary>
        public void SimulatePlacement(Vector2Int position, Vector2Int itemSize)
        {
            if (!CanPlaceItemAtPosition(position, itemSize))
            {
                Debug.LogWarning($"GridOccupancyAnalyzer: 无法在位置 {position} 放置尺寸 {itemSize} 的物品");
                return;
            }
            
            // 更新占用地图
            for (int dx = 0; dx < itemSize.x; dx++)
            {
                for (int dy = 0; dy < itemSize.y; dy++)
                {
                    int x = position.x + dx;
                    int y = position.y + dy;
                    occupancyMap[x, y] = true;
                }
            }
            
            OccupiedSlots += itemSize.x * itemSize.y;
            
            // 重新分析可用空间
            AnalyzeAvailableSpaces();
        }
        
        /// <summary>
        /// 撤销模拟的放置
        /// </summary>
        public void UndoSimulation()
        {
            // 重新分析整个网格
            AnalyzeGrid(true);
        }
        
        #endregion
        
        #region 工具方法
        
        /// <summary>
        /// 检查位置是否有效
        /// </summary>
        private bool IsValidPosition(int x, int y)
        {
            return x >= 0 && x < GridWidth && y >= 0 && y < GridHeight;
        }
        
        /// <summary>
        /// 记录分析结果
        /// </summary>
        private void LogAnalysisResults()
        {
            Debug.Log($"=== 网格占用分析结果 ===");
            Debug.Log($"网格尺寸: {GridWidth}x{GridHeight} ({TotalSlots} 格)");
            Debug.Log($"已占用: {OccupiedSlots} 格 ({OccupancyRate:P1})");
            Debug.Log($"可用: {AvailableSlots} 格");
            Debug.Log($"现有物品: {existingItems.Count} 个");
            Debug.Log($"可用空间区域: {availableSpaces.Count} 个");
            
            if (availableSpaces.Count > 0)
            {
                var largest = availableSpaces[0];
                Debug.Log($"最大可用空间: {largest.size} (面积: {largest.area} 格)");
            }
        }
        
        #endregion
    }
}
