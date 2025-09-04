using System;
using UnityEngine;
using InventorySystem;

namespace InventorySystem.SpawnSystem
{
    /// <summary>
    /// 放置类型枚举
    /// </summary>
    public enum PlacementType
    {
        [InspectorName("精确位置")] Exact = 0,           // 必须放置在指定位置
        [InspectorName("智能放置")] Smart = 1,           // 系统自动寻找最佳位置
        [InspectorName("区域约束")] AreaConstrained = 2, // 在指定区域内寻找位置
        [InspectorName("优先级放置")] Priority = 3       // 按优先级区域放置
    }
    
    /// <summary>
    /// 扫描模式枚举
    /// </summary>
    public enum ScanPattern
    {
        [InspectorName("从左到右")] LeftToRight = 0,
        [InspectorName("从上到下")] TopToBottom = 1,
        [InspectorName("螺旋扫描")] SpiralOut = 2,
        [InspectorName("从中心向外")] CenterToEdge = 3,
        [InspectorName("最大空隙优先")] LargestGapFirst = 4
    }
    
    /// <summary>
    /// 冲突解决策略枚举
    /// </summary>
    public enum ConflictResolutionType
    {
        [InspectorName("跳过")] Skip = 0,              // 跳过冲突物品
        [InspectorName("旋转")] Rotate = 1,            // 尝试旋转物品
        [InspectorName("重新定位")] Relocate = 2,      // 重新寻找位置
        [InspectorName("延后生成")] Defer = 3,         // 延后到其他物品生成完毕
        [InspectorName("强制替换")] ForceReplace = 4   // 强制替换现有物品（谨慎使用）
    }
    
    /// <summary>
    /// 生成优先级枚举
    /// </summary>
    public enum SpawnPriority
    {
        [InspectorName("关键")] Critical = 0,     // 必须生成的关键物品
        [InspectorName("高")] High = 1,           // 高优先级
        [InspectorName("中")] Medium = 2,         // 中优先级
        [InspectorName("低")] Low = 3,            // 低优先级
        [InspectorName("可选")] Optional = 4      // 可选物品，空间不足时可跳过
    }
    
    /// <summary>
    /// 固定物品生成模板
    /// 定义单个物品的生成规则和约束
    /// </summary>
    [System.Serializable]
    public class FixedItemTemplate
    {
        [Header("基础信息")]
        [Tooltip("用于标识此生成模板的唯一ID")]
        public string templateId;
        
        [Tooltip("要生成的物品数据")]
        public ItemDataSO itemData;
        
        [Range(1, 99)]
        [Tooltip("生成此物品的数量")]
        public int quantity = 1;
        
        [Header("位置配置")]
        [Tooltip("决定物品如何被放置")]
        public PlacementType placementType = PlacementType.Smart;
        
        [Tooltip("当使用精确位置放置时的坐标")]
        public Vector2Int exactPosition = Vector2Int.zero;
        
        [Tooltip("限制物品只能在此区域内生成")]
        public RectInt constrainedArea = new RectInt(0, 0, 10, 10);
        
        [Tooltip("优先尝试在此区域生成")]
        public RectInt preferredArea = new RectInt(0, 0, 5, 5);
        
        [Header("生成策略")]
        [Tooltip("生成优先级，优先级高的物品先生成")]
        public SpawnPriority priority = SpawnPriority.Medium;
        
        [Tooltip("寻找可用位置时的扫描模式")]
        public ScanPattern scanPattern = ScanPattern.LeftToRight;
        
        [Tooltip("当放置失败时是否允许尝试旋转物品")]
        public bool allowRotation = true;
        
        [Tooltip("当发生位置冲突时的处理方式")]
        public ConflictResolutionType conflictResolution = ConflictResolutionType.Relocate;
        
        [Header("生成条件")]
        [Tooltip("此物品在游戏中是否只能生成一次")]
        public bool isUniqueSpawn = true;
        
        [Tooltip("可选的生成条件标签，用于条件生成")]
        public string[] conditionTags;
        
        [Range(1, 10)]
        [Tooltip("放置失败时的最大重试次数")]
        public int maxRetryAttempts = 3;
        
        [Header("调试信息")]
        [Tooltip("是否为此模板启用详细的调试日志")]
        public bool enableDebugLog = false;
        
        /// <summary>
        /// 获取物品的实际尺寸（考虑旋转）
        /// </summary>
        public Vector2Int GetItemSize(bool isRotated = false)
        {
            if (itemData == null) return Vector2Int.one;
            
            if (isRotated && allowRotation)
            {
                return new Vector2Int(itemData.height, itemData.width);
            }
            
            return new Vector2Int(itemData.width, itemData.height);
        }
        
        /// <summary>
        /// 检查指定位置是否在约束区域内
        /// </summary>
        public bool IsInConstrainedArea(Vector2Int position)
        {
            if (placementType != PlacementType.AreaConstrained) return true;
            
            return constrainedArea.Contains(position);
        }
        
        /// <summary>
        /// 检查指定位置是否在优先区域内
        /// </summary>
        public bool IsInPreferredArea(Vector2Int position)
        {
            return preferredArea.Contains(position);
        }
        
        /// <summary>
        /// 获取扫描区域
        /// </summary>
        public RectInt GetScanArea(ItemGrid grid)
        {
            switch (placementType)
            {
                case PlacementType.AreaConstrained:
                    return constrainedArea;
                    
                case PlacementType.Priority:
                    return preferredArea;
                    
                default:
                    return new RectInt(0, 0, grid.CurrentWidth, grid.CurrentHeight);
            }
        }
        
        /// <summary>
        /// 验证模板配置的有效性
        /// </summary>
        public bool IsValid(out string errorMessage)
        {
            errorMessage = "";
            
            if (string.IsNullOrEmpty(templateId))
            {
                errorMessage = "模板ID不能为空";
                return false;
            }
            
            if (itemData == null)
            {
                errorMessage = "物品数据不能为空";
                return false;
            }
            
            if (quantity <= 0)
            {
                errorMessage = "生成数量必须大于0";
                return false;
            }
            
            if (placementType == PlacementType.AreaConstrained && 
                (constrainedArea.width <= 0 || constrainedArea.height <= 0))
            {
                errorMessage = "约束区域尺寸必须大于0";
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// 获取模板的调试信息字符串
        /// </summary>
        public string GetDebugInfo()
        {
            return $"Template[{templateId}]: {itemData?.name} x{quantity}, " +
                   $"Type={placementType}, Priority={priority}, " +
                   $"Size={GetItemSize()}, Unique={isUniqueSpawn}";
        }
    }
}
