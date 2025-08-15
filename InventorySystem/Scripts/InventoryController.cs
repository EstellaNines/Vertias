using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryController : MonoBehaviour
{
    [Header("库存控制器设置")]
    public ItemGrid selectedItemGrid; // 当前选中的主网格
    public BackpackItemGrid selectedBackpackGrid; // 当前选中的背包网格
    public TactiaclRigItemGrid selectedTacticalRigGrid; // 当前选中的战术挂具网格

    [Header("调试信息设置")]
    [SerializeField] private bool showDebugInfo = true; // 是否显示调试信息

    [Header("预览设置")]
    [SerializeField] private bool enablePreview = true; // 是否启用预览功能
    [SerializeField] private Vector2Int previewItemSize = new Vector2Int(2, 1); // 预览物品大小（用于测试）

    // 高亮组件
    private InventoryHighlight inventoryHighlight;
    private HoverHighlight hoverHighlight; // 悬停高亮组件

    // 当前拖拽的物品（用于预览）
    private InventorySystemItem draggedItem;

    // 当前活跃的网格类型枚举
    public enum ActiveGridType
    {
        None,
        MainGrid,
        BackpackGrid,
        TacticalRigGrid
    }

    [SerializeField] private ActiveGridType currentActiveGrid = ActiveGridType.None;

    private void Start()
    {
        // 获取或添加InventoryHighlight组件
        inventoryHighlight = GetComponent<InventoryHighlight>();
        if (inventoryHighlight == null)
        {
            inventoryHighlight = gameObject.AddComponent<InventoryHighlight>();
        }

        // 获取或添加HoverHighlight组件
        hoverHighlight = GetComponent<HoverHighlight>();
        if (hoverHighlight == null)
        {
            hoverHighlight = gameObject.AddComponent<HoverHighlight>();
        }
    }

    private void Update()
    {
        // 只有在拖拽物品时才检测网格切换
        if (draggedItem != null)
        {
            DetectHoveredGrid();
        }

        // 根据当前活跃网格类型处理输入 - 只处理当前活跃的网格
        switch (currentActiveGrid)
        {
            case ActiveGridType.MainGrid:
                if (selectedItemGrid != null)
                    HandleMainGridInput();
                break;
            case ActiveGridType.BackpackGrid:
                if (selectedBackpackGrid != null)
                    HandleBackpackGridInput();
                break;
            case ActiveGridType.TacticalRigGrid:
                if (selectedTacticalRigGrid != null)
                    HandleTacticalRigGridInput();
                break;
        }

        // 处理预览功能
        if (enablePreview)
        {
            HandlePreview();
        }
    }

    // 处理预览功能
    private void HandlePreview()
    {
        // 只有在拖拽物品时才显示预览
        if (draggedItem == null)
        {
            inventoryHighlight.Hide();
            return;
        }

        // 检查是否有活跃的网格
        object targetGrid = GetCurrentActiveGrid();
        if (targetGrid == null)
        {
            inventoryHighlight.Hide();
            return;
        }

        // 获取鼠标位置
        Vector2 mousePos = Input.mousePosition;
        Vector2Int tilePos = GetTileGridPosition(targetGrid, mousePos);

        // 检查位置是否有效
        if (tilePos.x < 0 || tilePos.y < 0)
        {
            inventoryHighlight.Hide();
            return;
        }

        // 使用拖拽物品的实际大小进行预览
        Vector2Int itemSize = draggedItem.Data != null ?
            new Vector2Int(draggedItem.Data.width, draggedItem.Data.height) :
            previewItemSize; // 如果没有数据则使用默认大小

        // 检查是否可以放置
        bool canPlace = CanPlaceItem(targetGrid, tilePos, itemSize);

        // 显示预览
        inventoryHighlight.Show(true);
        inventoryHighlight.SetSize(itemSize.x, itemSize.y);
        inventoryHighlight.SetParent(targetGrid);
        inventoryHighlight.SetPosition(targetGrid, draggedItem, tilePos.x, tilePos.y);
        inventoryHighlight.SetCanPlace(canPlace);

        if (showDebugInfo)
        {
            Debug.Log($"预览位置: ({tilePos.x}, {tilePos.y}) - 可放置: {canPlace} - 网格类型: {currentActiveGrid}");
        }
    }

    // 获取当前活跃的网格
    private object GetCurrentActiveGrid()
    {
        switch (currentActiveGrid)
        {
            case ActiveGridType.MainGrid:
                return selectedItemGrid;
            case ActiveGridType.BackpackGrid:
                return selectedBackpackGrid;
            case ActiveGridType.TacticalRigGrid:
                return selectedTacticalRigGrid;
            default:
                return null;
        }
    }

    // 获取网格位置
    private Vector2Int GetTileGridPosition(object grid, Vector2 screenPos)
    {
        if (grid is ItemGrid itemGrid)
        {
            return itemGrid.GetTileGridPosition(screenPos);
        }
        else if (grid is BackpackItemGrid backpackGrid)
        {
            return backpackGrid.GetTileGridPosition(screenPos);
        }
        else if (grid is TactiaclRigItemGrid tacticalGrid)
        {
            return tacticalGrid.GetTileGridPosition(screenPos);
        }

        return new Vector2Int(-1, -1);
    }

    // 检查是否可以放置物品
    private bool CanPlaceItem(object grid, Vector2Int position, Vector2Int size)
    {
        if (grid is ItemGrid itemGrid)
        {
            return itemGrid.CanPlaceItem(position, size);
        }
        else if (grid is BackpackItemGrid backpackGrid)
        {
            return backpackGrid.CanPlaceItem(position, size);
        }
        else if (grid is TactiaclRigItemGrid tacticalGrid)
        {
            return tacticalGrid.CanPlaceItem(position, size);
        }

        return false;
    }

    // 设置当前拖拽的物品（供外部调用）
    public void SetDraggedItem(InventorySystemItem item)
    {
        draggedItem = item;
    }

    // 清除拖拽的物品
    public void ClearDraggedItem()
    {
        draggedItem = null;
        inventoryHighlight.Hide();
    }

    // 处理主网格输入
    private void HandleMainGridInput()
    {
        if (selectedItemGrid == null) return;

        // 左键点击时获取网格位置并输出调试信息
        if (Input.GetMouseButtonDown(0))
        {
            Vector2Int gridPosition = selectedItemGrid.GetTileGridPosition(Input.mousePosition);

            if (showDebugInfo)
            {
                Debug.Log($"点击主网格位置: ({gridPosition.x}, {gridPosition.y}) - 网格名称: {selectedItemGrid.gameObject.name}");
            }
        }
    }

    // 处理背包网格输入
    private void HandleBackpackGridInput()
    {
        if (selectedBackpackGrid == null) return;

        // 左键点击时获取网格位置并输出调试信息
        if (Input.GetMouseButtonDown(0))
        {
            Vector2Int gridPosition = selectedBackpackGrid.GetTileGridPosition(Input.mousePosition);

            if (showDebugInfo)
            {
                Debug.Log($"点击背包网格位置: ({gridPosition.x}, {gridPosition.y}) - 网格名称: {selectedBackpackGrid.gameObject.name}");
            }
        }
    }

    // 处理战术挂具网格输入
    private void HandleTacticalRigGridInput()
    {
        if (selectedTacticalRigGrid == null) return;

        // 左键点击时获取网格位置并输出调试信息
        if (Input.GetMouseButtonDown(0))
        {
            Vector2Int gridPosition = selectedTacticalRigGrid.GetTileGridPosition(Input.mousePosition);

            if (showDebugInfo)
            {
                Debug.Log($"点击战术挂具网格位置: ({gridPosition.x}, {gridPosition.y}) - 网格名称: {selectedTacticalRigGrid.gameObject.name}");
            }
        }
    }

    // 设置选中的主网格
    public void SetSelectedMainGrid(ItemGrid itemGrid)
    {
        if (selectedItemGrid != itemGrid)
        {
            selectedItemGrid = itemGrid;
            currentActiveGrid = ActiveGridType.MainGrid;
            if (showDebugInfo)
            {
                Debug.Log($"切换选中主网格: {(itemGrid != null ? itemGrid.gameObject.name : "无")}");
            }
        }
    }

    // 设置选中的背包网格
    public void SetSelectedBackpackGrid(BackpackItemGrid backpackGrid)
    {
        if (selectedBackpackGrid != backpackGrid)
        {
            selectedBackpackGrid = backpackGrid;
            currentActiveGrid = ActiveGridType.BackpackGrid;
            if (showDebugInfo)
            {
                Debug.Log($"切换选中背包网格: {(backpackGrid != null ? backpackGrid.gameObject.name : "无")}");
            }
        }
    }

    // 设置选中的战术挂具网格
    public void SetSelectedTacticalRigGrid(TactiaclRigItemGrid tacticalRigGrid)
    {
        if (selectedTacticalRigGrid != tacticalRigGrid)
        {
            selectedTacticalRigGrid = tacticalRigGrid;
            currentActiveGrid = ActiveGridType.TacticalRigGrid;
            if (showDebugInfo)
            {
                Debug.Log($"切换选中战术挂具网格: {(tacticalRigGrid != null ? tacticalRigGrid.gameObject.name : "无")}");
            }
        }
    }

    // 清除选中的网格
    public void ClearSelectedGrid()
    {
        if (showDebugInfo)
        {
            string gridName = "无";
            switch (currentActiveGrid)
            {
                case ActiveGridType.MainGrid:
                    gridName = selectedItemGrid?.gameObject.name ?? "无";
                    break;
                case ActiveGridType.BackpackGrid:
                    gridName = selectedBackpackGrid?.gameObject.name ?? "无";
                    break;
                case ActiveGridType.TacticalRigGrid:
                    gridName = selectedTacticalRigGrid?.gameObject.name ?? "无";
                    break;
            }
            Debug.Log($"清除选中网格: {gridName}");
        }

        selectedItemGrid = null;
        selectedBackpackGrid = null;
        selectedTacticalRigGrid = null;
        currentActiveGrid = ActiveGridType.None;
    }

    // 获取是否有选中的网格
    public bool HasSelectedGrid()
    {
        return currentActiveGrid != ActiveGridType.None &&
               (selectedItemGrid != null || selectedBackpackGrid != null || selectedTacticalRigGrid != null);
    }

    // 获取当前活跃的网格类型
    public ActiveGridType GetActiveGridType()
    {
        return currentActiveGrid;
    }

    // 获取当前活跃的网格对象（通用方法）
    public GameObject GetActiveGridObject()
    {
        switch (currentActiveGrid)
        {
            case ActiveGridType.MainGrid:
                return selectedItemGrid?.gameObject;
            case ActiveGridType.BackpackGrid:
                return selectedBackpackGrid?.gameObject;
            case ActiveGridType.TacticalRigGrid:
                return selectedTacticalRigGrid?.gameObject;
            default:
                return null;
        }
    }

    // 检测鼠标当前悬停的网格并自动切换活跃网格
    private void DetectHoveredGrid()
    {
        Vector2 mousePos = Input.mousePosition;

        // 检查主网格
        if (selectedItemGrid != null && IsMouseOverGrid(selectedItemGrid.transform, mousePos))
        {
            if (currentActiveGrid != ActiveGridType.MainGrid)
            {
                currentActiveGrid = ActiveGridType.MainGrid;
                if (showDebugInfo)
                {
                    Debug.Log("切换到主网格");
                }
            }
            return;
        }

        // 检查背包网格
        if (selectedBackpackGrid != null && IsMouseOverGrid(selectedBackpackGrid.transform, mousePos))
        {
            if (currentActiveGrid != ActiveGridType.BackpackGrid)
            {
                currentActiveGrid = ActiveGridType.BackpackGrid;
                if (showDebugInfo)
                {
                    Debug.Log("切换到背包网格");
                }
            }
            return;
        }

        // 检查战术挂具网格
        if (selectedTacticalRigGrid != null && IsMouseOverGrid(selectedTacticalRigGrid.transform, mousePos))
        {
            if (currentActiveGrid != ActiveGridType.TacticalRigGrid)
            {
                currentActiveGrid = ActiveGridType.TacticalRigGrid;
                if (showDebugInfo)
                {
                    Debug.Log("切换到战术挂具网格");
                }
            }
            return;
        }
    }

    // 检查鼠标是否在指定网格上方
    private bool IsMouseOverGrid(Transform gridTransform, Vector2 mousePos)
    {
        if (gridTransform == null) return false;

        RectTransform rectTransform = gridTransform.GetComponent<RectTransform>();
        if (rectTransform == null) return false;

        return RectTransformUtility.RectangleContainsScreenPoint(rectTransform, mousePos, null);
    }

    /// <summary>
    /// 显示悬停高亮
    /// </summary>
    /// <param name="item">要高亮的物品</param>
    /// <param name="parentGrid">父级网格</param>
    public void ShowHoverHighlight(InventorySystemItem item, Transform parentGrid)
    {
        if (hoverHighlight != null)
        {
            hoverHighlight.ShowHoverHighlight(item, parentGrid);
        }
    }

    /// <summary>
    /// 隐藏悬停高亮
    /// </summary>
    public void HideHoverHighlight()
    {
        if (hoverHighlight != null)
        {
            hoverHighlight.HideHoverHighlight();
        }
    }
}

