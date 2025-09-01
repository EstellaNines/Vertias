using UnityEngine;

namespace InventorySystem
{
    /// <summary>
    /// 装备槽网格系统扩展
    /// 为ItemGrid添加装备槽相关的支持方法
    /// </summary>
    public static class EquipmentSlotGridExtension
    {
        /// <summary>
        /// 检查网格是否为装备槽类型
        /// </summary>
        /// <param name="grid">要检查的网格</param>
        /// <returns>是否为装备槽网格</returns>
        public static bool IsEquipmentSlotGrid(this ItemGrid grid)
        {
            if (grid == null) return false;
            return grid.GridType == GridType.Equipment;
        }
        
        /// <summary>
        /// 在装备槽中放置物品（绕过边界检查）
        /// </summary>
        /// <param name="grid">装备槽网格</param>
        /// <param name="item">要放置的物品</param>
        /// <returns>是否成功放置</returns>
        public static bool PlaceItemInEquipmentSlot(this ItemGrid grid, Item item)
        {
            if (grid == null || item == null) return false;
            
            // 对于装备槽，不执行传统的边界检查
            if (grid.IsEquipmentSlotGrid())
            {
                return PlaceItemDirectly(grid, item, 0, 0);
            }
            
            // 对于常规网格，使用正常的放置逻辑
            return grid.PlaceItem(item, 0, 0);
        }
        
        /// <summary>
        /// 直接放置物品到网格（绕过验证）
        /// </summary>
        /// <param name="grid">目标网格</param>
        /// <param name="item">要放置的物品</param>
        /// <param name="posX">X位置</param>
        /// <param name="posY">Y位置</param>
        /// <returns>是否成功放置</returns>
        private static bool PlaceItemDirectly(ItemGrid grid, Item item, int posX, int posY)
        {
            try
            {
                // 直接设置物品的网格状态
                item.OnGridReference = grid;
                item.OnGridPosition = new Vector2Int(posX, posY);
                
                // 设置物品的位置
                Vector2 targetPosition = grid.CalculatePositionOnGrid(item, posX, posY);
                RectTransform rectTransform = item.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    rectTransform.localPosition = targetPosition;
                }
                
                // 触发放置事件
                grid.TriggerItemPlacedEvent(item, new Vector2Int(posX, posY));
                
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[EquipmentSlotGridExtension] 直接放置物品失败: {e.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 从装备槽中安全移除物品
        /// </summary>
        /// <param name="grid">装备槽网格</param>
        /// <param name="item">要移除的物品</param>
        /// <returns>是否成功移除</returns>
        public static bool RemoveItemFromEquipmentSlot(this ItemGrid grid, Item item)
        {
            if (grid == null || item == null) return false;
            
            try
            {
                // 清除物品的网格状态
                Vector2Int oldPosition = item.OnGridPosition;
                item.ResetGridState();
                
                // 触发移除事件
                grid.TriggerItemRemovedEvent(item, oldPosition);
                
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[EquipmentSlotGridExtension] 从装备槽移除物品失败: {e.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 检查物品是否可以在装备槽中放置（装备槽特殊逻辑）
        /// </summary>
        /// <param name="grid">装备槽网格</param>
        /// <param name="item">要检查的物品</param>
        /// <returns>是否可以放置</returns>
        public static bool CanPlaceInEquipmentSlot(this ItemGrid grid, Item item)
        {
            if (grid == null || item == null) return false;
            
            // 装备槽总是可以放置物品（由装备槽组件负责验证）
            if (grid.IsEquipmentSlotGrid())
            {
                return true;
            }
            
            // 常规网格使用正常的验证逻辑
            return grid.CanPlaceItemAtPosition(0, 0, item.GetWidth(), item.GetHeight(), item);
        }
        
        /// <summary>
        /// 为装备槽创建临时网格
        /// </summary>
        /// <param name="slotType">装备槽类型</param>
        /// <param name="size">网格尺寸</param>
        /// <param name="parent">父级Transform</param>
        /// <returns>创建的网格</returns>
        public static ItemGrid CreateEquipmentSlotGrid(EquipmentSlotType slotType, Vector2Int size, Transform parent = null)
        {
            GameObject gridObject = new GameObject($"EquipmentSlot_{slotType}_Grid");
            
            if (parent != null)
            {
                gridObject.transform.SetParent(parent);
            }
            
            // 添加必要的UI组件
            var rectTransform = gridObject.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(size.x * 64f, size.y * 64f); // 假设每格64像素
            
            // 添加网格组件
            var grid = gridObject.AddComponent<ItemGrid>();
            grid.gridSizeWidth = size.x;
            grid.gridSizeHeight = size.y;
            grid.GridType = GridType.Equipment;
            grid.GridName = $"Equipment_{slotType}";
            
            return grid;
        }
    }
}
