using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryController : MonoBehaviour
{
    [Header("库存控制器设置")]
    public ItemGrid selectedItemGrid; // 当前选中的网格

    [Header("调试信息设置")]
    [SerializeField] private bool showDebugInfo = true; // 是否显示调试信息

    private void Update()
    {
        if (selectedItemGrid == null) return;

        // 左键点击时获取网格位置并输出调试信息
        if (Input.GetMouseButtonDown(0))
        {
            Vector2Int gridPosition = selectedItemGrid.GetTileGridPosition(Input.mousePosition);

            if (showDebugInfo)
            {
                Debug.Log($"点击网格位置: ({gridPosition.x}, {gridPosition.y}) - 网格名称: {selectedItemGrid.gameObject.name}");
            }
        }

        // 右键按住时持续显示当前鼠标位置（调试用）
        if (showDebugInfo && Input.GetMouseButton(1)) // 右键按住时显示
        {
            Vector2Int gridPosition = selectedItemGrid.GetTileGridPosition(Input.mousePosition);
            Debug.Log($"鼠标当前网格位置: ({gridPosition.x}, {gridPosition.y})");
        }
    }

    // 设置选中的网格
    public void SetSelectedGrid(ItemGrid itemGrid)
    {
        if (selectedItemGrid != itemGrid)
        {
            selectedItemGrid = itemGrid;
            if (showDebugInfo)
            {
                Debug.Log($"切换选中网格: {(itemGrid != null ? itemGrid.gameObject.name : "无")}");
            }
        }
    }

    // 清除选中的网格
    public void ClearSelectedGrid()
    {
        if (selectedItemGrid != null)
        {
            if (showDebugInfo)
            {
                Debug.Log($"清除选中网格: {selectedItemGrid.gameObject.name}");
            }
            selectedItemGrid = null;
        }
    }

    // 获取是否有选中的网格
    public bool HasSelectedGrid()
    {
        return selectedItemGrid != null;
    }
}