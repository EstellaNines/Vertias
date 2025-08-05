using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class ItemSpawner : MonoBehaviour
{
    [FieldLabel("物品工厂")] public ItemFactory itemFactory;
    [FieldLabel("物品列表")] public List<ItemData> itemsToSpawn; // 要生成的物品列表
    [FieldLabel("物品网格")] public ItemGrid itemGrid;
    [FieldLabel("物品预制体")] public GameObject itemPrefab;

    [Header("生成设置")]
    [FieldLabel("大物品优先尺寸排序")] public bool sortBySize = true; // 是否按尺寸排序（大物品优先）
    [FieldLabel("按顺序放置")] public bool sequentialPlacement = true; // 是否按顺序放置（从左到右，从上到下）
    [FieldLabel("最大随机步数")] public int maxAttempts = 200; // 最大尝试次数（仅随机模式使用）

    [ContextMenu("批量生成物品")]
    public void SpawnAllItems()
    {
        if (itemGrid == null || itemPrefab == null || itemsToSpawn == null || itemsToSpawn.Count == 0)
        {
            Debug.LogError("缺少必要引用或物品列表为空");
            return;
        }

        // 清空现有物品
        ClearAllItems();

        // 按尺寸排序物品列表（大物品优先放置）
        List<ItemData> sortedItems = sortBySize ?
            itemsToSpawn.OrderByDescending(item => item.width * item.height).ToList() :
            new List<ItemData>(itemsToSpawn);

        int successCount = 0;
        int totalArea = CalculateTotalArea();
        int gridArea = itemGrid.GridSizeWidth * itemGrid.GridSizeHeight;

        if (totalArea > gridArea)
        {
            Debug.LogWarning($"物品总面积({totalArea})超过背包容量({gridArea})，部分物品可能无法放置");
        }

        foreach (var data in sortedItems)
        {
            if (TrySpawnItem(data))
            {
                successCount++;
            }
            else
            {
                Debug.LogWarning($"无法放置物品：{data.name}");
            }
        }

        Debug.Log($"成功生成 {successCount}/{itemsToSpawn.Count} 个物品");
    }

    // 在 ItemSpawner.cs 中的 TrySpawnItem 方法中：
    
    private bool TrySpawnItem(ItemData data)
    {
        BaseItem newItem = itemFactory.CreateItem(data);
    
        if (newItem == null)
        {
            Debug.LogError($"无法创建物品: {data.name}");
            return false;
        }
    
        // 使用顺序放置算法
        bool placementSuccess;
        if (sequentialPlacement)
        {
            placementSuccess = itemGrid.TryPlaceItemSequentially(newItem);
        }
        else
        {
            placementSuccess = itemGrid.TryPlaceItemRandomly(newItem, maxAttempts);
        }
    
        if (placementSuccess)
        {
            newItem.transform.SetParent(itemGrid.transform, false);
            newItem.transform.SetAsLastSibling();
            return true;
        }
        else
        {
            Destroy(newItem.gameObject);
            return false;
        }
    }

    private void ClearAllItems()
    {
        // 清空背包中的所有物品
        for (int i = itemGrid.transform.childCount - 1; i >= 0; i--)
        {
            Transform child = itemGrid.transform.GetChild(i);
            if (child.GetComponent<BaseItem>() != null)
            {
                DestroyImmediate(child.gameObject);
            }
        }
    
        // 重置网格状态
        itemGrid.ClearGrid();
    }

    private int CalculateTotalArea()
    {
        int totalArea = 0;
        foreach (var item in itemsToSpawn)
        {
            totalArea += item.width * item.height;
        }
        return totalArea;
    }

    [ContextMenu("清空背包")]
    public void ClearBackpack()
    {
        ClearAllItems();
        Debug.Log("背包已清空");
    }
}