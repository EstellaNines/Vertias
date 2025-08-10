using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class ItemSpawner : MonoBehaviour
{
    [Header("物品生成器设置")]
    [SerializeField] private ItemGrid targetGrid; // 目标网格
    [SerializeField] private Transform itemParent; // 物品父对象

    [Header("物品预制体和数据设置")]
    [SerializeField] private GameObject[] itemPrefabs; // 物品预制体数组
    [SerializeField] private InventorySystemItemDataSO[] itemDataArray; // 物品数据数组

    [Header("调试信息设置")]
    [SerializeField] private bool showDebugInfo = true;

    // 网格占用状态数组
    private bool[,] gridOccupancy;
    private List<SpawnedItemInfo> spawnedItems = new List<SpawnedItemInfo>();

    // 生成物品信息类
    [System.Serializable]
    public class SpawnedItemInfo
    {
        public GameObject itemObject;
        public Vector2Int gridPosition;
        public Vector2Int size;
        public InventorySystemItemDataSO itemData;
    }

    private void Start()
    {
        if (targetGrid == null)
        {
            targetGrid = FindObjectOfType<ItemGrid>();
        }

        if (itemParent == null)
        {
            itemParent = targetGrid?.transform;
        }

        InitializeGrid();
    }

    // 初始化网格占用状态数组
    private void InitializeGrid()
    {
        if (targetGrid == null) return;

        // 获取网格尺寸（通过反射访问私有字段）
        var widthField = typeof(ItemGrid).GetField("width",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var heightField = typeof(ItemGrid).GetField("height",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        int gridWidth = widthField != null ? (int)widthField.GetValue(targetGrid) : 12;
        int gridHeight = heightField != null ? (int)heightField.GetValue(targetGrid) : 20;

        gridOccupancy = new bool[gridWidth, gridHeight];

        if (showDebugInfo)
        {
            Debug.Log($"初始化网格尺寸: {gridWidth}x{gridHeight}");
        }
    }

    // 在指定位置生成头盔物品
    public GameObject SpawnHelmetAtPosition(Vector2Int gridPos, int helmetIndex = 0)
    {
        if (itemPrefabs == null || itemPrefabs.Length == 0)
        {
            Debug.LogError("没有设置物品预制体数组！");
            return null;
        }

        if (itemDataArray == null || itemDataArray.Length == 0)
        {
            Debug.LogError("没有设置物品数据数组！");
            return null;
        }

        helmetIndex = Mathf.Clamp(helmetIndex, 0, itemPrefabs.Length - 1);
        int dataIndex = Mathf.Clamp(helmetIndex, 0, itemDataArray.Length - 1);

        InventorySystemItemDataSO helmetData = itemDataArray[dataIndex];
        Vector2Int itemSize = new Vector2Int(helmetData.width, helmetData.height);

        // 检查位置是否可用
        if (!IsPositionAvailable(gridPos, itemSize))
        {
            // 寻找可用位置
            Vector2Int newPos = FindAvailablePosition(itemSize);
            if (newPos == new Vector2Int(-1, -1))
            {
                Debug.LogWarning("没有足够的空间放置物品！");
                return null;
            }
            gridPos = newPos;
        }

        // 创建物品
        GameObject spawnedItem = CreateHelmetItem(itemPrefabs[helmetIndex], helmetData, gridPos);

        if (spawnedItem != null)
        {
            // 标记网格占用
            MarkGridOccupied(gridPos, itemSize, true);

            // 记录生成的物品信息
            spawnedItems.Add(new SpawnedItemInfo
            {
                itemObject = spawnedItem,
                gridPosition = gridPos,
                size = itemSize,
                itemData = helmetData
            });

            if (showDebugInfo)
            {
                Debug.Log($"成功生成物品: {helmetData.itemName} 位置: ({gridPos.x}, {gridPos.y})");
            }
        }

        return spawnedItem;
    }

    // 创建头盔物品
    private GameObject CreateHelmetItem(GameObject prefab, InventorySystemItemDataSO data, Vector2Int gridPos)
    {
        if (prefab == null || data == null || targetGrid == null) return null;

        // 实例化物品预制体
        GameObject item = Instantiate(prefab, itemParent);

        // 设置物品数据
        ItemDataHolder dataHolder = item.GetComponentInChildren<ItemDataHolder>();
        if (dataHolder != null)
        {
            dataHolder.SetItemData(data);
        }
        else
        {
            Debug.LogError($"预制体 {prefab.name} 中没有找到 ItemDataHolder 组件！");
        }

        // 确保物品具有必要的组件
        EnsureRequiredComponents(item);

        // 设置物品的锚点和轴心点（使用左上角作为参考点）
        RectTransform itemRect = item.GetComponent<RectTransform>();
        if (itemRect != null)
        {
            itemRect.anchorMin = new Vector2(0, 1); // 左上角
            itemRect.anchorMax = new Vector2(0, 1); // 左上角
            itemRect.pivot = new Vector2(0, 1);     // 轴心点也设置为左上角
        }

        // 设置物品在网格中的位置
        SetItemGridPosition(item, gridPos);

        return item;
    }

    // 确保物品具有必要的组件
    private void EnsureRequiredComponents(GameObject item)
    {
        // 确保有 DraggableItem 组件
        if (item.GetComponent<DraggableItem>() == null)
        {
            item.AddComponent<DraggableItem>();
        }

        // 确保有 InventorySystemItem 组件
        if (item.GetComponent<InventorySystemItem>() == null)
        {
            item.AddComponent<InventorySystemItem>();
        }

        // 确保有 ItemHoverHighlight 组件
        if (item.GetComponent<ItemHoverHighlight>() == null)
        {
            item.AddComponent<ItemHoverHighlight>();
        }

        // 确保有 CanvasGroup 组件（DraggableItem 需要）
        if (item.GetComponent<CanvasGroup>() == null)
        {
            item.AddComponent<CanvasGroup>();
        }
    }

    // 设置物品在网格中的位置
    private void SetItemGridPosition(GameObject item, Vector2Int gridPos)
    {
        if (targetGrid == null) return;

        // 获取物品组件
        var itemComponent = item.GetComponent<InventorySystemItem>();
        if (itemComponent == null) return;

        Vector2Int itemSize = new Vector2Int(itemComponent.Data.width, itemComponent.Data.height);

        // 通过反射调用ItemGrid的PlaceItem方法
        var placeItemMethod = typeof(ItemGrid).GetMethod("PlaceItem",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (placeItemMethod != null)
        {
            placeItemMethod.Invoke(targetGrid, new object[] { item, gridPos, itemSize });
        }
    }

    // 检查位置是否可用
    private bool IsPositionAvailable(Vector2Int position, Vector2Int size)
    {
        if (gridOccupancy == null) return false;

        int gridWidth = gridOccupancy.GetLength(0);
        int gridHeight = gridOccupancy.GetLength(1);

        // 检查边界
        if (position.x < 0 || position.y < 0 ||
            position.x + size.x > gridWidth || position.y + size.y > gridHeight)
        {
            return false;
        }

        // 检查重叠
        for (int x = position.x; x < position.x + size.x; x++)
        {
            for (int y = position.y; y < position.y + size.y; y++)
            {
                if (gridOccupancy[x, y])
                {
                    return false;
                }
            }
        }

        return true;
    }

    // 寻找可用位置
    private Vector2Int FindAvailablePosition(Vector2Int itemSize)
    {
        if (gridOccupancy == null) return new Vector2Int(-1, -1);

        int gridWidth = gridOccupancy.GetLength(0);
        int gridHeight = gridOccupancy.GetLength(1);

        // 从左上角开始搜索
        for (int y = 0; y <= gridHeight - itemSize.y; y++)
        {
            for (int x = 0; x <= gridWidth - itemSize.x; x++)
            {
                Vector2Int testPos = new Vector2Int(x, y);
                if (IsPositionAvailable(testPos, itemSize))
                {
                    return testPos;
                }
            }
        }

        return new Vector2Int(-1, -1); // 没有找到可用位置
    }

    // 标记网格占用状态
    private void MarkGridOccupied(Vector2Int position, Vector2Int size, bool occupied)
    {
        if (gridOccupancy == null) return;

        for (int x = position.x; x < position.x + size.x; x++)
        {
            for (int y = position.y; y < position.y + size.y; y++)
            {
                if (x >= 0 && x < gridOccupancy.GetLength(0) &&
                    y >= 0 && y < gridOccupancy.GetLength(1))
                {
                    gridOccupancy[x, y] = occupied;
                }
            }
        }
    }

    // 移除物品
    public void RemoveItem(GameObject item)
    {
        SpawnedItemInfo itemInfo = spawnedItems.FirstOrDefault(info => info.itemObject == item);
        if (itemInfo != null)
        {
            // 清除网格占用
            MarkGridOccupied(itemInfo.gridPosition, itemInfo.size, false);

            // 从列表中移除
            spawnedItems.Remove(itemInfo);

            // 销毁物品
            if (item != null)
            {
                DestroyImmediate(item);
            }

            if (showDebugInfo)
            {
                Debug.Log($"移除物品: {itemInfo.itemData.itemName}");
            }
        }
    }

    // 生成随机头盔（编辑器右键菜单）
    [ContextMenu("生成随机头盔")]
    public void SpawnRandomHelmet()
    {
        if (itemPrefabs != null && itemPrefabs.Length > 0)
        {
            int randomIndex = Random.Range(0, itemPrefabs.Length);
            SpawnHelmetAtPosition(Vector2Int.zero, randomIndex);
        }
    }

    // 清空所有物品
    [ContextMenu("清空所有物品")]
    public void ClearAllItems()
    {
        for (int i = spawnedItems.Count - 1; i >= 0; i--)
        {
            RemoveItem(spawnedItems[i].itemObject);
        }
    }

    // 获取网格占用状态数组（供外部调用）
    public bool[,] GetGridOccupancy()
    {
        return gridOccupancy;
    }
}
