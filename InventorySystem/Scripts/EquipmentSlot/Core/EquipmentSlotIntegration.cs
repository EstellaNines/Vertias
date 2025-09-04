using UnityEngine;
using System.Collections.Generic;

namespace InventorySystem
{
    /// <summary>
    /// 装备槽系统与现有库存系统的集成组件
    /// 负责协调装备槽与网格系统之间的交互
    /// </summary>
    public class EquipmentSlotIntegration : MonoBehaviour
    {
        [Header("集成设置")]
        [FieldLabel("自动集成")]
        [Tooltip("启动时自动集成装备槽系统")]
        public bool autoIntegrate = true;
        
        [FieldLabel("支持双向拖拽")]
        [Tooltip("支持物品在装备槽和网格之间双向拖拽")]
        public bool enableBidirectionalDrag = true;
        
        [Header("调试信息")]
        [FieldLabel("显示集成日志")]
        public bool showIntegrationLogs = false;
        
        // 系统组件引用
        private InventoryController inventoryController;
        private EquipmentSlotManager equipmentSlotManager;
        private InventorySaveManager saveManager;
        
        // 集成状态
        private bool isIntegrated = false;
        
        #region Unity生命周期
        
        private void Awake()
        {
            FindSystemComponents();
        }
        
        private void Start()
        {
            if (autoIntegrate)
            {
                IntegrateWithExistingSystem();
            }
        }
        
        private void OnDestroy()
        {
            DisintegrateFromExistingSystem();
        }
        
        #endregion
        
        #region 系统集成
        
        /// <summary>
        /// 查找系统组件
        /// </summary>
        private void FindSystemComponents()
        {
            // 查找库存控制器
            if (inventoryController == null)
            {
                inventoryController = FindObjectOfType<InventoryController>();
            }
            
            // 查找装备槽管理器
            if (equipmentSlotManager == null)
            {
                equipmentSlotManager = EquipmentSlotManager.Instance;
            }
            
            // 查找存档管理器
            if (saveManager == null)
            {
                saveManager = FindObjectOfType<InventorySaveManager>();
            }
            
            if (showIntegrationLogs)
            {
                Debug.Log($"[EquipmentSlotIntegration] 系统组件查找完成: " +
                         $"InventoryController={inventoryController != null}, " +
                         $"EquipmentSlotManager={equipmentSlotManager != null}, " +
                         $"SaveManager={saveManager != null}");
            }
        }
        
        /// <summary>
        /// 与现有系统集成
        /// </summary>
        public void IntegrateWithExistingSystem()
        {
            if (isIntegrated)
            {
                Debug.LogWarning("[EquipmentSlotIntegration] 系统已集成，跳过重复集成");
                return;
            }
            
            // 注册装备槽事件
            RegisterEquipmentSlotEvents();
            
            // 集成拖拽系统
            if (enableBidirectionalDrag)
            {
                IntegrateDragSystem();
            }
            
            // 集成存档系统
            IntegrateSaveSystem();
            
            isIntegrated = true;
            
            if (showIntegrationLogs)
            {
                Debug.Log("[EquipmentSlotIntegration] 装备槽系统集成完成");
            }
        }
        
        /// <summary>
        /// 从现有系统分离
        /// </summary>
        public void DisintegrateFromExistingSystem()
        {
            if (!isIntegrated) return;
            
            // 注销装备槽事件
            UnregisterEquipmentSlotEvents();
            
            isIntegrated = false;
            
            if (showIntegrationLogs)
            {
                Debug.Log("[EquipmentSlotIntegration] 装备槽系统分离完成");
            }
        }
        
        #endregion
        
        #region 事件集成
        
        /// <summary>
        /// 注册装备槽事件
        /// </summary>
        private void RegisterEquipmentSlotEvents()
        {
            if (equipmentSlotManager != null)
            {
                EquipmentSlot.OnItemEquipped += HandleItemEquippedToSlot;
                EquipmentSlot.OnItemUnequipped += HandleItemUnequippedFromSlot;
                EquipmentSlot.OnContainerSlotActivated += HandleContainerSlotActivated;
                EquipmentSlot.OnContainerSlotDeactivated += HandleContainerSlotDeactivated;
                
                EquipmentSlotManager.OnEquipmentChanged += HandleEquipmentChanged;
            }
        }
        
        /// <summary>
        /// 注销装备槽事件
        /// </summary>
        private void UnregisterEquipmentSlotEvents()
        {
            EquipmentSlot.OnItemEquipped -= HandleItemEquippedToSlot;
            EquipmentSlot.OnItemUnequipped -= HandleItemUnequippedFromSlot;
            EquipmentSlot.OnContainerSlotActivated -= HandleContainerSlotActivated;
            EquipmentSlot.OnContainerSlotDeactivated -= HandleContainerSlotDeactivated;
            
            EquipmentSlotManager.OnEquipmentChanged -= HandleEquipmentChanged;
        }
        
        #endregion
        
        #region 事件处理器
        
        /// <summary>
        /// 处理物品装备到槽位事件
        /// </summary>
        /// <param name="slotType">槽位类型</param>
        /// <param name="item">装备的物品</param>
        private void HandleItemEquippedToSlot(EquipmentSlotType slotType, ItemDataReader item)
        {
            if (item == null) return;
            
            // 从原网格中移除物品
            RemoveItemFromGrid(item);
            
            // 清除库存控制器的选中状态
            if (inventoryController != null)
            {
                inventoryController.SetSelectedItem(null);
                inventoryController.ForceHideHighlight();
            }
            
            if (showIntegrationLogs)
            {
                Debug.Log($"[EquipmentSlotIntegration] 物品 {item.ItemData.itemName} 已装备到 {slotType}");
            }
        }
        
        /// <summary>
        /// 处理物品从槽位卸装事件
        /// </summary>
        /// <param name="slotType">槽位类型</param>
        /// <param name="item">卸装的物品</param>
        private void HandleItemUnequippedFromSlot(EquipmentSlotType slotType, ItemDataReader item)
        {
            if (item == null) return;
            
            // 物品卸装后的处理逻辑在EquipmentSlot中已处理
            // 这里主要做日志记录和其他全局处理
            
            if (showIntegrationLogs)
            {
                Debug.Log($"[EquipmentSlotIntegration] 物品 {item.ItemData.itemName} 已从 {slotType} 卸装");
            }
        }
        
        /// <summary>
        /// 处理容器槽位激活事件
        /// </summary>
        /// <param name="slotType">槽位类型</param>
        /// <param name="containerGrid">容器网格</param>
        private void HandleContainerSlotActivated(EquipmentSlotType slotType, ItemGrid containerGrid)
        {
            if (containerGrid == null) return;
            
            // 将容器网格注册到存档管理器
            if (saveManager != null)
            {
                string gridKey = $"Equipment_{slotType}_Container";
                saveManager.RegisterGrid(containerGrid, gridKey);
            }
            
            if (showIntegrationLogs)
            {
                Debug.Log($"[EquipmentSlotIntegration] 容器槽位 {slotType} 已激活");
            }
        }
        
        /// <summary>
        /// 处理容器槽位停用事件
        /// </summary>
        /// <param name="slotType">槽位类型</param>
        /// <param name="containerGrid">容器网格</param>
        private void HandleContainerSlotDeactivated(EquipmentSlotType slotType, ItemGrid containerGrid)
        {
            if (containerGrid == null) return;
            
            // 从存档管理器注销容器网格
            if (saveManager != null)
            {
                string gridKey = $"Equipment_{slotType}_Container";
                saveManager.UnregisterGrid(gridKey);
            }
            
            if (showIntegrationLogs)
            {
                Debug.Log($"[EquipmentSlotIntegration] 容器槽位 {slotType} 已停用");
            }
        }
        
        /// <summary>
        /// 处理装备变化事件
        /// </summary>
        /// <param name="slotType">槽位类型</param>
        /// <param name="item">装备的物品（null表示卸装）</param>
        private void HandleEquipmentChanged(EquipmentSlotType slotType, ItemDataReader item)
        {
            // 可以在这里添加装备变化的全局处理逻辑
            // 例如：属性计算、UI更新、成就系统等
            
            if (showIntegrationLogs)
            {
                string action = item != null ? "装备" : "卸装";
                string itemName = item?.ItemData.itemName ?? "无";
                Debug.Log($"[EquipmentSlotIntegration] 装备变化: {slotType} - {action} {itemName}");
            }
        }
        
        #endregion
        
        #region 拖拽系统集成
        
        /// <summary>
        /// 集成拖拽系统
        /// </summary>
        private void IntegrateDragSystem()
        {
            // 拖拽系统的集成主要通过装备槽的拖拽接口实现
            // 这里可以添加额外的拖拽逻辑处理
            
            if (showIntegrationLogs)
            {
                Debug.Log("[EquipmentSlotIntegration] 拖拽系统集成完成");
            }
        }
        
        #endregion
        
        #region 存档系统集成
        
        /// <summary>
        /// 集成存档系统
        /// </summary>
        private void IntegrateSaveSystem()
        {
            if (saveManager == null) return;
            
            // 存档系统集成通过扩展InventorySaveManager实现
            // 这里注册装备槽的存档事件处理
            
            if (showIntegrationLogs)
            {
                Debug.Log("[EquipmentSlotIntegration] 存档系统集成完成");
            }
        }
        
        #endregion
        
        #region 辅助方法
        
        /// <summary>
        /// 从网格中移除物品
        /// </summary>
        /// <param name="item">要移除的物品</param>
        private void RemoveItemFromGrid(ItemDataReader item)
        {
            if (item == null) return;
            
            var itemComponent = item.GetComponent<Item>();
            if (itemComponent != null && itemComponent.OnGridReference != null)
            {
                // 从原网格中拾取物品（这会从网格中移除物品）
                var grid = itemComponent.OnGridReference;
                var position = itemComponent.OnGridPosition;
                
                var pickedItem = grid.PickUpItem(position.x, position.y);
                if (pickedItem != null)
                {
                    if (showIntegrationLogs)
                    {
                        Debug.Log($"[EquipmentSlotIntegration] 成功从网格中移除物品: {item.ItemData.itemName}");
                    }
                }
            }
        }
        
        /// <summary>
        /// 尝试将物品放回网格
        /// </summary>
        /// <param name="item">要放回的物品</param>
        /// <param name="targetGrid">目标网格</param>
        /// <param name="position">目标位置</param>
        /// <returns>是否成功放回</returns>
        public bool TryPlaceItemInGrid(ItemDataReader item, ItemGrid targetGrid, Vector2Int position)
        {
            if (item == null || targetGrid == null) return false;
            
            var itemComponent = item.GetComponent<Item>();
            if (itemComponent == null) return false;
            
            bool success = targetGrid.PlaceItem(itemComponent, position.x, position.y);
            
            if (success && showIntegrationLogs)
            {
                Debug.Log($"[EquipmentSlotIntegration] 成功将物品 {item.ItemData.itemName} 放回网格");
            }
            
            return success;
        }
        
        #endregion
        
        #region 公共接口
        
        /// <summary>
        /// 强制刷新系统集成
        /// </summary>
        public void RefreshIntegration()
        {
            if (isIntegrated)
            {
                DisintegrateFromExistingSystem();
            }
            
            FindSystemComponents();
            IntegrateWithExistingSystem();
        }
        
        /// <summary>
        /// 检查集成状态
        /// </summary>
        /// <returns>集成状态信息</returns>
        public string GetIntegrationStatus()
        {
            return $"集成状态: {(isIntegrated ? "已集成" : "未集成")}\n" +
                   $"InventoryController: {(inventoryController != null ? "已连接" : "未找到")}\n" +
                   $"EquipmentSlotManager: {(equipmentSlotManager != null ? "已连接" : "未找到")}\n" +
                   $"SaveManager: {(saveManager != null ? "已连接" : "未找到")}";
        }
        
        #endregion
    }
}
