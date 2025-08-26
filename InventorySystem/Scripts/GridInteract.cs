using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

// 网格交互组件
// 负责管理网格状态和背包数据同步
[RequireComponent(typeof(ItemGrid))]
public class GridInteract : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    // 组件引用
    private ItemGrid itemGrid;
    private InventoryController inventoryController; // 添加InventoryController引用
    
    // 鼠标是否在网格范围内
    public bool IsMouseInGrid { get; private set; } = false;
    
    // 背包数据同步事件
    public System.Action<ItemGrid> OnInventoryDataChanged;
    
    private void Awake()
    {
        // 获取当前物体的ItemGrid组件
        itemGrid = GetComponent<ItemGrid>();
        if (itemGrid == null)
        {
            Debug.LogError("GridInteract: 当前物体没有ItemGrid组件！");
        }
        
        // 查找InventoryController
        inventoryController = FindObjectOfType<InventoryController>();
        if (inventoryController == null)
        {
            Debug.LogWarning("GridInteract: 未找到InventoryController组件！");
        }
    }
    
    // 鼠标进入网格范围
    public void OnPointerEnter(PointerEventData eventData)
    {
        IsMouseInGrid = true;
        Debug.Log($"GridInteract: 鼠标进入网格 {gameObject.name}");
        
        // 通知InventoryController设置当前网格为选中背包
        if (inventoryController != null && itemGrid != null)
        {
            inventoryController.SetSelectedItemGrid(itemGrid);
            Debug.Log($"GridInteract: 设置 {itemGrid.name} 为当前选中背包");
        }
        
        // 触发背包数据同步事件
        OnInventoryDataChanged?.Invoke(itemGrid);
    }
    
    // 鼠标离开网格范围
    public void OnPointerExit(PointerEventData eventData)
    {
        IsMouseInGrid = false;
        Debug.Log($"GridInteract: 鼠标离开网格 {gameObject.name}");
        
        // 清除InventoryController中的选中背包
        if (inventoryController != null)
        {
            inventoryController.SetSelectedItemGrid(null);
            Debug.Log("GridInteract: 清除选中背包");
        }
    }
    
    // 获取关联的ItemGrid组件
    public ItemGrid GetItemGrid()
    {
        return itemGrid;
    }
    
    // 手动同步背包数据
    public void SyncInventoryData()
    {
        if (itemGrid != null)
        {
            OnInventoryDataChanged?.Invoke(itemGrid);
            Debug.Log($"GridInteract: 同步背包 {itemGrid.name} 的数据");
        }
    }
    
    // 检查指定位置是否在网格范围内（简化版本）
    public bool IsPositionInGrid(Vector2 screenPosition)
    {
        if (itemGrid == null) return false;
        
        // 将屏幕坐标转换为网格坐标
        Vector2Int gridPos = itemGrid.GetTileGridPosition(screenPosition);
        
        // 检查坐标是否在有效范围内
        return gridPos.x >= 0 && gridPos.x < itemGrid.gridSizeWidth && 
               gridPos.y >= 0 && gridPos.y < itemGrid.gridSizeHeight;
    }
    
    // 手动设置InventoryController引用（用于运行时动态设置）
    public void SetInventoryController(InventoryController controller)
    {
        inventoryController = controller;
        Debug.Log($"GridInteract: 手动设置InventoryController引用");
    }
}
