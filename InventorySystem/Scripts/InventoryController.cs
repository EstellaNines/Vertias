using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using InventorySystem;

// 背包交互控制器
// 负责处理物品的选择、拖拽和放置逻辑
public class InventoryController : MonoBehaviour
{
    [Header("控制器设置")]
    [FieldLabel("当前操作的背包")] public ItemGrid selectedItemGrid;

    [Header("背包标识")]
    [FieldLabel("背包唯一ID")] public string inventoryId = "";
    [FieldLabel("背包显示名称")] public string inventoryName = "";

    [Header("放置指示器设置")]
    [FieldLabel("放置指示器组件")] public InventoryHighlight inventoryHighlight;

    // 内部状态
    private Item selectedItem;
    private Canvas canvas;

    // 缓存所有网格交互组件，提高性能
    private List<GridInteract> allGridInteracts = new List<GridInteract>();

    // 放置指示器状态
    private bool isHighlightActive = false;
    
    // 提示器原始父对象（用于恢复）
    private Transform originalHighlightParent;

    private void Start()
    {
        canvas = FindObjectOfType<Canvas>();

        // 旧的网格上下文系统已移除

        // 缓存所有GridInteract组件
        RefreshGridInteracts();

        // 如果没有手动分配InventoryHighlight，尝试自动查找
        if (inventoryHighlight == null)
        {
            inventoryHighlight = FindObjectOfType<InventoryHighlight>();
            if (inventoryHighlight == null)
            {
                Debug.LogWarning("InventoryController: 未找到InventoryHighlight组件，放置指示器功能将不可用");
            }
            else
            {
                Debug.Log($"InventoryController: 自动找到InventoryHighlight组件 - {inventoryHighlight.name}");
            }
        }
        else
        {
            Debug.Log($"InventoryController: 使用手动分配的InventoryHighlight组件 - {inventoryHighlight.name}");
        }
        
        // 保存提示器的原始父对象
        if (inventoryHighlight != null)
        {
            originalHighlightParent = inventoryHighlight.transform.parent;
            Debug.Log($"InventoryController: 已保存提示器原始父对象 - {originalHighlightParent?.name}");
        }

        // 设置默认ID和名称（如果未设置）
        if (string.IsNullOrEmpty(inventoryId))
        {
            inventoryId = gameObject.name;
        }

        if (string.IsNullOrEmpty(inventoryName))
        {
            inventoryName = gameObject.name;
        }
    }

    private void Update()
    {
        // 更新放置指示器
        UpdatePlacementHighlight();

        // 旋转键监听（R）：当有选中物品时切换旋转并刷新高亮
        if (Input.GetKeyDown(KeyCode.R) && selectedItem != null)
        {
            selectedItem.ToggleRotation();
            selectedItem.AdjustVisualSizeForGrid();

            // 在拖拽或移动中，立即刷新高亮反馈
            UpdateDragHighlight(selectedItem, Input.mousePosition);
        }
    }

    // 刷新网格交互组件缓存
    public void RefreshGridInteracts()
    {
        allGridInteracts.Clear();
        
        // 查找所有有效的GridInteract组件，过滤掉已销毁的对象
        var foundGridInteracts = FindObjectsOfType<GridInteract>();
        foreach (var gridInteract in foundGridInteracts)
        {
            if (gridInteract != null && gridInteract.gameObject != null)
            {
                allGridInteracts.Add(gridInteract);
            }
        }
        
        Debug.Log($"[InventoryController] 刷新网格交互列表，找到 {allGridInteracts.Count} 个有效网格");
    }
    

    

    
    // 旧的网格上下文事件处理方法已移除
    
    /// <summary>
    /// 更新库存高亮显示
    /// 当上下文切换时重新计算高亮位置
    /// </summary>
    private void UpdateInventoryHighlight()
    {
        if (selectedItem == null || inventoryHighlight == null)
        {
            if (isHighlightActive)
            {
                HideHighlight();
            }
            return;
        }
        
        // 重新执行放置高亮逻辑
        UpdatePlacementHighlight();
    }

    // 更新放置指示器
    private void UpdatePlacementHighlight()
    {
        // 只有在有选中物品且指示器组件存在时才更新
        if (selectedItem == null || inventoryHighlight == null)
        {
            if (isHighlightActive)
            {
                HideHighlight();
            }
            return;
        }

        // 获取鼠标下的网格
        ItemGrid targetGrid = GetItemGridUnderMouse();
        if (targetGrid == null)
        {
            if (isHighlightActive)
            {
                HideHighlight();
            }
            return;
        }

        // 获取网格坐标
        Vector2Int gridPosition = targetGrid.GetTileGridPosition(Input.mousePosition);

        // 检查是否可以放置
        bool canPlace = CanPlaceItemAt(targetGrid, gridPosition.x, gridPosition.y, selectedItem);

        // 额外：检测是否为可堆叠合并场景
        bool canStackMerge = false;
        Item targetAtCell = targetGrid.GetItemAt(gridPosition.x, gridPosition.y);
        if (targetAtCell != null && targetAtCell != selectedItem)
        {
            var dragReader = selectedItem.GetComponent<ItemDataReader>();
            var targetReader = targetAtCell.GetComponent<ItemDataReader>();
            if (dragReader != null && targetReader != null &&
                dragReader.ItemData != null && targetReader.ItemData != null)
            {
                bool sameId = dragReader.ItemData.id == targetReader.ItemData.id;
                bool stackable = targetReader.ItemData.IsStackable();
                bool targetNotFull = targetReader.CurrentStack < targetReader.ItemData.maxStack;
                canStackMerge = sameId && stackable && targetNotFull;
            }
        }

        // 优先显示“可堆叠合并”黄色态
        if (canStackMerge)
        {
            inventoryHighlight.SetParent(targetGrid);
            inventoryHighlight.SetPositionSimple(targetGrid, gridPosition.x, gridPosition.y);
            inventoryHighlight.SetSize(selectedItem.GetWidth(), selectedItem.GetHeight());
            inventoryHighlight.SetStackableHighlight();
            inventoryHighlight.Show(true);
            isHighlightActive = true;
            return;
        }

        // 默认绿/红态
        ShowHighlight(targetGrid, gridPosition.x, gridPosition.y, selectedItem.GetWidth(), selectedItem.GetHeight(), canPlace);
    }

    // 检查是否可以在指定位置放置物品
    private bool CanPlaceItemAt(ItemGrid targetGrid, int x, int y, Item item)
    {
        if (targetGrid == null || item == null) return false;

        // 检查边界
        if (!targetGrid.BoundryCheck(x, y, item.GetWidth(), item.GetHeight()))
        {
            return false;
        }

        // 检查是否有物品冲突（排除自身）
        if (targetGrid.HasItemConflict(x, y, item.GetWidth(), item.GetHeight(), item))
        {
            return false;
        }

        return true;
    }

    // 显示放置高亮
    private void ShowHighlight(ItemGrid targetGrid, int x, int y, int width, int height, bool canPlace)
    {
        if (inventoryHighlight == null) return;

        // 设置高亮的父级为目标网格
        inventoryHighlight.SetParent(targetGrid);

        // 使用简化的位置设置方法
        inventoryHighlight.SetPositionSimple(targetGrid, x, y);

        // 设置高亮大小
        inventoryHighlight.SetSize(width, height);

        // 设置颜色（绿色表示可放置，红色表示不可放置）
        inventoryHighlight.SetCanPlace(canPlace);

        // 显示高亮
        inventoryHighlight.Show(true);

        isHighlightActive = true;
    }

    // 隐藏放置高亮
    private void HideHighlight()
    {
        if (inventoryHighlight != null)
        {
            inventoryHighlight.Show(false);
        }
        isHighlightActive = false;
    }

    // 通过多种方式获取鼠标下的ItemGrid（重构版）
    private ItemGrid GetItemGridUnderMouse()
    {
        // 使用UI射线检测
        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        // 优先从检测结果中寻找DraggableItem，因为它在最上层
        foreach (RaycastResult result in results)
        {
            DraggableItem draggableItem = result.gameObject.GetComponent<DraggableItem>();
            if (draggableItem != null)
            {
                // 如果找到了可拖拽物品，直接返回它所在的网格
                ItemGrid grid = draggableItem.GetParentGrid();
                if (grid != null)
                {
                    return grid;
                }
            }
        }

        // 如果没有直接命中物品，再检查是否有网格背景
        foreach (RaycastResult result in results)
        {
            // 确保result.gameObject存在且未被销毁
            if (result.gameObject == null) continue;
            
            ItemGrid itemGrid = result.gameObject.GetComponent<ItemGrid>();
            if (itemGrid != null && itemGrid.gameObject != null)
            {
                return itemGrid;
            }

            // 也检查父物体，以防点到网格的子元素
            itemGrid = result.gameObject.GetComponentInParent<ItemGrid>();
            if (itemGrid != null && itemGrid.gameObject != null)
            {
                return itemGrid;
            }
        }

        return null;
    }

    // 鼠标坐标转化为格子坐标
    private Vector2Int GetTileGridPosition()
    {
        Vector2 position = Input.mousePosition;

        if (selectedItemGrid != null)
        {
            Vector2Int tileGridPosition = selectedItemGrid.GetTileGridPosition(position);
            return tileGridPosition;
        }

        // 如果没有选中的网格，尝试找到鼠标下的网格
        ItemGrid targetGrid = GetItemGridUnderMouse();
        if (targetGrid != null)
        {
            return targetGrid.GetTileGridPosition(position);
        }

        return Vector2Int.zero;
    }

    private void TryPickUpItem(int x, int y)
    {
        selectedItem = selectedItemGrid.PickUpItem(x, y);
    }

    // 尝试放置物品
    private void TryPlaceItem(int x, int y)
    {
        if (selectedItem == null) return;

        // 检查目标位置是否已经有物品
        if (selectedItemGrid.HasItemAt(x, y))
        {
            // 如果目标位置有物品，可以实现交换逻辑
            Item targetItem = selectedItemGrid.PickUpItem(x, y);
            selectedItemGrid.PlaceItem(selectedItem, x, y);
            selectedItem = targetItem;
        }
        else
        {
            // 目标位置为空，直接放置
            selectedItemGrid.PlaceItem(selectedItem, x, y);
            selectedItem = null;
        }
    }

    // 设置当前操作的背包
    public void SetSelectedItemGrid(ItemGrid itemGrid)
    {
        selectedItemGrid = itemGrid;
    }

    // 获取当前选中的物品
    public Item GetSelectedItem()
    {
        return selectedItem;
    }

    // 设置当前选中的物品（用于拖拽系统）
    public void SetSelectedItem(Item item)
    {
        selectedItem = item;

        if (selectedItem == null)
        {
            // 清除选中物品时隐藏高亮
            if (isHighlightActive)
            {
                HideHighlight();
            }
        }
    }

    // 外部接口：开始拖拽时调用
    public void OnItemDragStart(Item item)
    {
        SetSelectedItem(item);
        
        // 验证并确保提示器可用
        if (!IsHighlightAvailable())
        {
            Debug.LogError("InventoryController: 拖拽开始时提示器不可用！尝试重新初始化...");
            
            // 强制重新查找提示器
            inventoryHighlight = FindObjectOfType<InventoryHighlight>();
            if (inventoryHighlight != null)
            {
                Debug.Log($"InventoryController: 重新找到提示器 - {inventoryHighlight.name}，父级: {inventoryHighlight.transform.parent?.name}");
                
                // 确保提示器在正确的父级下
                if (inventoryHighlight.transform.parent != this.transform)
                {
                    Debug.Log("InventoryController: 将提示器移回InventoryController");
                    inventoryHighlight.transform.SetParent(this.transform, false);
                }
            }
            else
            {
                Debug.LogError("InventoryController: 无法重新找到提示器组件！");
            }
        }
        else
        {
            Debug.Log($"InventoryController: 拖拽开始，提示器可用 - {inventoryHighlight.name}，父级: {inventoryHighlight.transform.parent?.name}");
        }
    }

    // 外部接口：结束拖拽时调用
    public void OnItemDragEnd()
    {
        if (isHighlightActive)
        {
            HideHighlight();
        }
    }

    // 外部接口：强制隐藏高亮
    public void ForceHideHighlight()
    {
        HideHighlight();
    }

    // 获取背包ID
    public string GetInventoryId()
    {
        return inventoryId;
    }

    // 获取背包名称
    public string GetInventoryName()
    {
        return inventoryName;
    }

    // 设置背包ID（运行时修改）
    public void SetInventoryId(string newId)
    {
        if (string.IsNullOrEmpty(newId))
        {
            Debug.LogWarning("InventoryController: 尝试设置空的背包ID");
            return;
        }

        inventoryId = newId;
        Debug.Log($"InventoryController: 背包ID已更改为 {inventoryId}");
    }

    // 设置背包名称（运行时修改）
    public void SetInventoryName(string newName)
    {
        inventoryName = newName;
    }

    // 拖拽时的实时高亮更新（专门用于拖拽场景）
    public void UpdateDragHighlight(Item draggingItem, Vector3 mousePosition)
    {
        // 检查提示器组件状态
        if (inventoryHighlight == null)
        {
            Debug.LogWarning("InventoryController: UpdateDragHighlight - inventoryHighlight为null");
            return;
        }
        
        if (inventoryHighlight.gameObject == null)
        {
            Debug.LogWarning("InventoryController: UpdateDragHighlight - inventoryHighlight的GameObject被销毁");
            inventoryHighlight = null;
            return;
        }
        
        // 如果没有拖拽物品，隐藏高亮
        if (draggingItem == null)
        {
            if (isHighlightActive)
            {
                HideHighlight();
            }
            return;
        }

        // 优先检查装备槽
        InventorySystem.EquipmentSlot targetEquipmentSlot = GetEquipmentSlotUnderMouse();
        if (targetEquipmentSlot != null)
        {
            // 检查物品是否可以装备到这个槽位
            ItemDataReader itemDataReader = draggingItem.GetComponent<ItemDataReader>();
            bool canEquip = targetEquipmentSlot.CanAcceptItem(itemDataReader);
            
            // 显示装备槽高亮
            ShowEquipmentSlotHighlight(targetEquipmentSlot, canEquip);
            return;
        }

        // 获取鼠标下的网格
        ItemGrid targetGrid = GetItemGridUnderMouse();
        if (targetGrid == null)
        {
            if (isHighlightActive)
            {
                HideHighlight();
            }
            return;
        }

        // 获取网格坐标
        Vector2Int gridPosition = targetGrid.GetTileGridPosition(mousePosition);

        // 使用新的重叠检测方法检查是否可以放置
        bool canPlace = targetGrid.CanPlaceItemAtPosition(
            gridPosition.x,
            gridPosition.y,
            draggingItem.GetWidth(),
            draggingItem.GetHeight(),
            draggingItem
        );

        // 额外检测：可堆叠合并（同类且未满）
        bool canStackMerge = false;
        Item targetAtCell = targetGrid.GetItemAt(gridPosition.x, gridPosition.y);
        if (targetAtCell != null && targetAtCell != draggingItem)
        {
            var dragReader = draggingItem.GetComponent<ItemDataReader>();
            var targetReader = targetAtCell.GetComponent<ItemDataReader>();
            if (dragReader != null && targetReader != null &&
                dragReader.ItemData != null && targetReader.ItemData != null)
            {
                bool sameId = dragReader.ItemData.id == targetReader.ItemData.id;
                bool stackable = targetReader.ItemData.IsStackable();
                bool targetNotFull = targetReader.CurrentStack < targetReader.ItemData.maxStack;
                canStackMerge = sameId && stackable && targetNotFull;
            }
        }

        // 显示高亮
        if (canStackMerge && inventoryHighlight != null)
        {
            // 使用黄色态表示可堆叠合并
            inventoryHighlight.SetParent(targetGrid);
            inventoryHighlight.SetPositionSimple(targetGrid, gridPosition.x, gridPosition.y);
            inventoryHighlight.SetSize(draggingItem.GetWidth(), draggingItem.GetHeight());
            inventoryHighlight.SetStackableHighlight();
            inventoryHighlight.Show(true);
            isHighlightActive = true;
            return;
        }

        // 常规绿/红态
        ShowDragHighlight(targetGrid, gridPosition.x, gridPosition.y,
                         draggingItem.GetWidth(), draggingItem.GetHeight(), canPlace);
    }

    // 显示拖拽时的放置高亮（与普通高亮分离，避免冲突）
    private void ShowDragHighlight(ItemGrid targetGrid, int x, int y, int width, int height, bool canPlace)
    {
        if (inventoryHighlight == null) return;

        // 设置高亮的父级为目标网格
        inventoryHighlight.SetParent(targetGrid);

        // 使用简化的位置设置方法
        inventoryHighlight.SetPositionSimple(targetGrid, x, y);

        // 设置高亮大小
        inventoryHighlight.SetSize(width, height);

        // 设置颜色（绿色表示可放置，红色表示有重叠冲突）
        inventoryHighlight.SetCanPlace(canPlace);

        // 显示高亮
        inventoryHighlight.Show(true);

        isHighlightActive = true;
    }

    /// <summary>
    /// 获取鼠标下方的装备槽
    /// </summary>
    /// <returns>鼠标下方的装备槽，如果没有则返回null</returns>
    private InventorySystem.EquipmentSlot GetEquipmentSlotUnderMouse()
    {
        // 使用UI射线检测
        UnityEngine.EventSystems.PointerEventData pointerData = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current)
        {
            position = Input.mousePosition
        };

        var results = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
        UnityEngine.EventSystems.EventSystem.current.RaycastAll(pointerData, results);

        foreach (var result in results)
        {
            InventorySystem.EquipmentSlot equipmentSlot = result.gameObject.GetComponentInParent<InventorySystem.EquipmentSlot>();
            if (equipmentSlot != null)
            {
                return equipmentSlot;
            }
        }

        return null;
    }

    /// <summary>
    /// 显示装备槽高亮
    /// </summary>
    /// <param name="equipmentSlot">目标装备槽</param>
    /// <param name="canEquip">是否可以装备</param>
    private void ShowEquipmentSlotHighlight(InventorySystem.EquipmentSlot equipmentSlot, bool canEquip)
    {
        if (inventoryHighlight == null || equipmentSlot == null) return;

        // 使用扩展的装备槽高亮方法
        inventoryHighlight.SetEquipmentSlotHighlight(equipmentSlot, canEquip);
        
        isHighlightActive = true;
    }
    
    /// <summary>
    /// 重置提示器状态
    /// 简化版本 - 提示器始终保持在InventoryController下
    /// </summary>
    public void ResetHighlight()
    {
        if (inventoryHighlight == null) return;
        
        // 隐藏提示器
        inventoryHighlight.Show(false);
        
        // 重置提示器状态
        inventoryHighlight.Reset();
        
        isHighlightActive = false;
        
        Debug.Log("InventoryController: 提示器已重置");
    }
    
    /// <summary>
    /// 强制将提示器返回到InventoryController
    /// 用于网格销毁前的安全回收
    /// </summary>
    public void ForceReturnHighlightToController()
    {
        if (inventoryHighlight == null) return;
        
        // 直接检查InventoryHighlight组件的Transform
        Transform highlighterTransform = inventoryHighlight.transform;
        
        // 检查提示器当前是否在其他父级下
        if (highlighterTransform.parent != this.transform)
        {
            string oldParentName = highlighterTransform.parent?.name ?? "null";
            
            // 隐藏提示器
            inventoryHighlight.Show(false);
            
            // 将提示器返回到InventoryController下
            highlighterTransform.SetParent(this.transform, false);
            
            // 重置提示器状态
            inventoryHighlight.Reset();
            
            isHighlightActive = false;
            
            Debug.Log($"InventoryController: 强制回收提示器从 {oldParentName} 到 InventoryController");
        }
        else
        {
            Debug.Log("InventoryController: 提示器已经在InventoryController下，无需回收");
        }
    }
    
    /// <summary>
    /// 获取高亮提示器组件
    /// 供外部使用，确保提示器可用
    /// </summary>
    public InventoryHighlight GetHighlightComponent()
    {
        return inventoryHighlight;
    }
    
    /// <summary>
    /// 设置高亮提示器组件
    /// 允许外部重新分配提示器
    /// </summary>
    public void SetHighlightComponent(InventoryHighlight highlight)
    {
        if (highlight == null)
        {
            Debug.LogWarning("InventoryController: 尝试设置空的高亮提示器");
            return;
        }
        
        inventoryHighlight = highlight;
        
        // 更新原始父对象引用
        if (originalHighlightParent == null)
        {
            originalHighlightParent = this.transform;
        }
        
        Debug.Log("InventoryController: 已设置新的高亮提示器组件");
    }
    
    /// <summary>
    /// 检查提示器是否可用
    /// </summary>
    public bool IsHighlightAvailable()
    {
        bool isAvailable = inventoryHighlight != null && inventoryHighlight.gameObject != null;
        
        if (!isAvailable)
        {
            Debug.LogWarning($"InventoryController: 提示器不可用 - inventoryHighlight={inventoryHighlight != null}, gameObject={(inventoryHighlight?.gameObject != null)}");
            
            // 尝试重新查找提示器
            if (inventoryHighlight == null)
            {
                inventoryHighlight = FindObjectOfType<InventoryHighlight>();
                if (inventoryHighlight != null)
                {
                    Debug.Log($"InventoryController: 重新找到提示器组件 - {inventoryHighlight.name}");
                    isAvailable = true;
                }
            }
        }
        
        return isAvailable;
    }
    
    private void OnDestroy()
    {
        // 旧的网格上下文系统已移除
    }
}
