using UnityEngine;
using InventorySystem;

namespace InventorySystem.Extensions
{
    /// <summary>
    /// ItemGrid扩展方法
    /// ItemGrid Extension Methods
    /// </summary>
    public static class ItemGridExtensions
    {
        /// <summary>
        /// 检查网格是否为容器类型
        /// Check if grid is a container type
        /// </summary>
        /// <param name="grid">要检查的网格</param>
        /// <returns>如果是容器类型返回true</returns>
        public static bool IsContainerType(this ItemGrid grid)
        {
            if (grid == null) return false;
            
            // 容器类型包括：Container, Storage, Backpack
            return grid.GridType == GridType.Container || 
                   grid.GridType == GridType.Storage || 
                   grid.GridType == GridType.Backpack;
        }
        
        /// <summary>
        /// 检查网格是否适合随机生成
        /// Check if grid is suitable for random spawning
        /// </summary>
        /// <param name="grid">要检查的网格</param>
        /// <returns>如果适合随机生成返回true</returns>
        public static bool IsSuitableForRandomSpawn(this ItemGrid grid)
        {
            if (grid == null) return false;
            
            // 检查基本条件
            if (!grid.IsContainerType()) return false;
            if (!grid.IsGridActive) return false;
            
            // 检查网格是否有足够的空间
            var stats = grid.GetGridStatistics();
            if (stats.occupancyRate >= 0.95f) return false; // 如果占用率超过95%则不适合
            
            // 检查访问权限
            if (grid.AccessLevel == GridAccessLevel.ReadOnly) return false;
            
            return true;
        }
        
        /// <summary>
        /// 获取网格的可用空间百分比
        /// Get available space percentage of the grid
        /// </summary>
        /// <param name="grid">要检查的网格</param>
        /// <returns>可用空间百分比(0-1)</returns>
        public static float GetAvailableSpacePercentage(this ItemGrid grid)
        {
            if (grid == null) return 0f;
            
            var stats = grid.GetGridStatistics();
            return 1f - stats.occupancyRate;
        }
        
        /// <summary>
        /// 检查网格是否有足够空间放置指定数量的物品
        /// Check if grid has enough space for specified number of items
        /// </summary>
        /// <param name="grid">要检查的网格</param>
        /// <param name="itemCount">物品数量</param>
        /// <param name="averageItemSize">平均物品大小(默认1x1)</param>
        /// <returns>如果有足够空间返回true</returns>
        public static bool HasEnoughSpaceForItems(this ItemGrid grid, int itemCount, Vector2Int averageItemSize = default)
        {
            if (grid == null || itemCount <= 0) return false;
            
            if (averageItemSize == default)
                averageItemSize = Vector2Int.one;
            
            var stats = grid.GetGridStatistics();
            int requiredSlots = itemCount * averageItemSize.x * averageItemSize.y;
            int availableSlots = stats.totalSlots - stats.occupiedSlots;
            
            return availableSlots >= requiredSlots;
        }
        
        /// <summary>
        /// 获取网格的容量信息字符串
        /// Get grid capacity information as string
        /// </summary>
        /// <param name="grid">要检查的网格</param>
        /// <returns>容量信息字符串</returns>
        public static string GetCapacityInfo(this ItemGrid grid)
        {
            if (grid == null) return "网格无效";
            
            var stats = grid.GetGridStatistics();
            return $"{stats.itemCount} 物品 / {stats.occupiedSlots} 占用 / {stats.totalSlots} 总计 ({stats.occupancyRate:P1})";
        }
        
        /// <summary>
        /// 检查网格是否为空
        /// Check if grid is empty
        /// </summary>
        /// <param name="grid">要检查的网格</param>
        /// <returns>如果网格为空返回true</returns>
        public static bool IsEmpty(this ItemGrid grid)
        {
            if (grid == null) return true;
            
            var stats = grid.GetGridStatistics();
            return stats.itemCount == 0;
        }
        
        /// <summary>
        /// 检查网格是否几乎满了
        /// Check if grid is nearly full
        /// </summary>
        /// <param name="grid">要检查的网格</param>
        /// <param name="threshold">满度阈值(默认0.9)</param>
        /// <returns>如果网格几乎满了返回true</returns>
        public static bool IsNearlyFull(this ItemGrid grid, float threshold = 0.9f)
        {
            if (grid == null) return false;
            
            var stats = grid.GetGridStatistics();
            return stats.occupancyRate >= threshold;
        }
        
        /// <summary>
        /// 获取推荐的生成物品数量
        /// Get recommended spawn item count based on grid capacity
        /// </summary>
        /// <param name="grid">要检查的网格</param>
        /// <param name="maxItems">最大物品数量</param>
        /// <param name="fillRatio">填充比例(默认0.6)</param>
        /// <returns>推荐的生成数量</returns>
        public static int GetRecommendedSpawnCount(this ItemGrid grid, int maxItems, float fillRatio = 0.6f)
        {
            if (grid == null || maxItems <= 0) return 0;
            
            var stats = grid.GetGridStatistics();
            int availableSlots = stats.totalSlots - stats.occupiedSlots;
            int targetSlots = Mathf.RoundToInt(availableSlots * fillRatio);
            
            return Mathf.Min(maxItems, targetSlots);
        }
        
        /// <summary>
        /// 获取网格类型的显示名称
        /// Get display name for grid type
        /// </summary>
        /// <param name="grid">要检查的网格</param>
        /// <returns>网格类型的中文显示名称</returns>
        public static string GetGridTypeDisplayName(this ItemGrid grid)
        {
            if (grid == null) return "未知";
            
            return grid.GridType switch
            {
                GridType.Backpack => "背包",
                GridType.Storage => "仓库",
                GridType.Equipment => "装备",
                GridType.Ground => "地面",
                GridType.Container => "容器",
                GridType.Other => "其他",
                GridType.Custom => "自定义",
                GridType.Trading => "交易",
                GridType.Test => "测试",
                _ => "未知"
            };
        }
    }
}
