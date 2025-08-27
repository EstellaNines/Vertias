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

    private void Start()
    {
        canvas = FindObjectOfType<Canvas>();

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
        allGridInteracts.AddRange(FindObjectsOfType<GridInteract>());
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

        // 显示高亮并设置颜色
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
            ItemGrid itemGrid = result.gameObject.GetComponent<ItemGrid>();
            if (itemGrid != null)
            {
                return itemGrid;
            }

            // 也检查父物体，以防点到网格的子元素
            itemGrid = result.gameObject.GetComponentInParent<ItemGrid>();
            if (itemGrid != null)
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
        // 如果没有拖拽物品或高亮组件，隐藏高亮
        if (draggingItem == null || inventoryHighlight == null)
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
        Vector2Int gridPosition = targetGrid.GetTileGridPosition(mousePosition);

        // 使用新的重叠检测方法检查是否可以放置
        bool canPlace = targetGrid.CanPlaceItemAtPosition(
            gridPosition.x,
            gridPosition.y,
            draggingItem.GetWidth(),
            draggingItem.GetHeight(),
            draggingItem
        );

        // 显示高亮并设置颜色（绿色表示可放置，红色表示有重叠冲突）
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
}
