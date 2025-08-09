using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryController : MonoBehaviour
{
    [Header("背包控制器设置")]
    public ItemGrid selectedItemGrid; // 当前操作的背包

    [Header("调试信息")]
    [SerializeField] private bool showDebugInfo = true; // 是否显示调试信息

    private void Update()
    {
        if (selectedItemGrid == null) return;

        // 显示当前鼠标在网格中的坐标
        if (Input.GetMouseButtonDown(0))
        {
            Vector2Int gridPosition = selectedItemGrid.GetTileGridPosition(Input.mousePosition);

            if (showDebugInfo)
            {
                Debug.Log($"点击网格坐标: ({gridPosition.x}, {gridPosition.y}) - 网格: {selectedItemGrid.gameObject.name}");
            }
        }

        // 实时显示鼠标位置（可选，用于调试）
        if (showDebugInfo && Input.GetMouseButton(1)) // 右键按住时显示
        {
            Vector2Int gridPosition = selectedItemGrid.GetTileGridPosition(Input.mousePosition);
            Debug.Log($"当前网格坐标: ({gridPosition.x}, {gridPosition.y})");
        }
    }

    // 设置当前选中的网格
    public void SetSelectedGrid(ItemGrid itemGrid)
    {
        if (selectedItemGrid != itemGrid)
        {
            selectedItemGrid = itemGrid;
            if (showDebugInfo)
            {
                Debug.Log($"切换到网格: {(itemGrid != null ? itemGrid.gameObject.name : "无")}");
            }
        }
    }

    // 清除当前选中的网格
    public void ClearSelectedGrid()
    {
        if (selectedItemGrid != null)
        {
            if (showDebugInfo)
            {
                Debug.Log($"清除网格选择: {selectedItemGrid.gameObject.name}");
            }
            selectedItemGrid = null;
        }
    }

    // 获取当前是否有选中的网格
    public bool HasSelectedGrid()
    {
        return selectedItemGrid != null;
    }
}