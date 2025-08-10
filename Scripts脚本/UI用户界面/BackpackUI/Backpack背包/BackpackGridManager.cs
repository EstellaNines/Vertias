using UnityEngine;
using System.Collections.Generic;

public class BackpackGridManager : MonoBehaviour
{
    [Header("背包网格配置")]
    [SerializeField] private GridConfig backpackGridConfig;
    [SerializeField] private ItemGrid backpackGrid;
    [SerializeField] private InventoryController inventoryController;

    [Header("网格系统组件")]
    [SerializeField] private GridInteract gridInteract;

    private void Start()
    {
        InitializeBackpackGrid();
    }

    private void InitializeBackpackGrid()
    {
        if (backpackGrid != null && backpackGridConfig != null)
        {
            // 设置网格配置
            backpackGrid.SetGridConfig(backpackGridConfig);

            // 初始化网格
            backpackGrid.LoadFromGridConfig();

            // 设置库存控制器
            if (inventoryController != null)
            {
                inventoryController.SetSelectedGrid(backpackGrid);
            }

            Debug.Log("背包网格系统初始化完成");
        }
        else
        {
            Debug.LogError("背包网格配置或网格组件未设置！");
        }
    }

    // 获取背包网格
    public ItemGrid GetBackpackGrid()
    {
        return backpackGrid;
    }

    // 设置网格配置
    public void SetGridConfig(GridConfig config)
    {
        backpackGridConfig = config;
        if (backpackGrid != null)
        {
            backpackGrid.SetGridConfig(config);
            backpackGrid.LoadFromGridConfig();
        }
    }

    // 清空背包网格
    public void ClearBackpackGrid()
    {
        if (backpackGrid != null)
        {
            // 获取所有已放置的物品
            var placedItems = new List<ItemGrid.PlacedItem>();
            // 注意：需要根据ItemGrid的实际API调整这里的代码

            for (int i = placedItems.Count - 1; i >= 0; i--)
            {
                if (placedItems[i].itemObject != null)
                {
                    backpackGrid.RemoveItem(placedItems[i].itemObject);
                    Destroy(placedItems[i].itemObject);
                }
            }
        }
    }

    // 保存背包状态
    public void SaveBackpackState()
    {
        if (backpackGrid != null)
        {
            backpackGrid.SaveToGridConfig();
        }
    }
}