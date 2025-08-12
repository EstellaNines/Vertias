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

    // 当前活跃的网格类型枚举
    public enum ActiveGridType
    {
        None,
        MainGrid,
        BackpackGrid,
        TacticalRigGrid
    }

    [SerializeField] private ActiveGridType currentActiveGrid = ActiveGridType.None;

    private void Update()
    {
        // 根据当前活跃网格类型处理输入
        switch (currentActiveGrid)
        {
            case ActiveGridType.MainGrid:
                HandleMainGridInput();
                break;
            case ActiveGridType.BackpackGrid:
                HandleBackpackGridInput();
                break;
            case ActiveGridType.TacticalRigGrid:
                HandleTacticalRigGridInput();
                break;
        }
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

        // 右键按住时持续显示当前鼠标位置（调试用）
        if (showDebugInfo && Input.GetMouseButton(1))
        {
            Vector2Int gridPosition = selectedItemGrid.GetTileGridPosition(Input.mousePosition);
            Debug.Log($"鼠标当前主网格位置: ({gridPosition.x}, {gridPosition.y})");
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

        // 右键按住时持续显示当前鼠标位置（调试用）
        if (showDebugInfo && Input.GetMouseButton(1))
        {
            Vector2Int gridPosition = selectedBackpackGrid.GetTileGridPosition(Input.mousePosition);
            Debug.Log($"鼠标当前背包网格位置: ({gridPosition.x}, {gridPosition.y})");
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

        // 右键按住时持续显示当前鼠标位置（调试用）
        if (showDebugInfo && Input.GetMouseButton(1))
        {
            Vector2Int gridPosition = selectedTacticalRigGrid.GetTileGridPosition(Input.mousePosition);
            Debug.Log($"鼠标当前战术挂具网格位置: ({gridPosition.x}, {gridPosition.y})");
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
}