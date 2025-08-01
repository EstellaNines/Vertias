using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class ItemDataDisplayer : MonoBehaviour
{
    [SerializeField][FieldLabel("显示所有物品")] bool displayAllItems = false;
    [SerializeField][FieldLabel("物品列表")] List<ItemData> items;
    [SerializeField][FieldLabel("物品预制体")] GameObject itemPrefab;
    [SerializeField][FieldLabel("显示容器")] Transform displayContainer;
    [SerializeField][FieldLabel("物品网格")] ItemGrid itemGrid;
    [SerializeField][FieldLabel("每行最大宽度")] int maxRowWidth = 10;
    [SerializeField][FieldLabel("自动排序")] bool autoSort = false; // 改为false，按顺序显示
    [SerializeField][FieldLabel("显示物品数量")] int displayCount = 12; // 新增：控制显示数量

    private List<GameObject> displayedItems = new List<GameObject>();
    
    // 添加交互相关变量
    Item selectedItem;
    Item overlapItem;
    Canvas canvas;
    InventoryHighlight inventoryHighlight;
    Item itemToHighlight;
    Vector2Int oldPosition;

    // 添加网格占用状态追踪
    [SerializeField][FieldLabel("网格宽度")] int gridWidth = 20;
    [SerializeField][FieldLabel("网格高度")] int gridHeight = 20;
    private bool[,] gridOccupied;
    
    private void Start()
    {
        canvas = FindObjectOfType<Canvas>();
        inventoryHighlight = GetComponent<InventoryHighlight>();
        
        // 初始化网格占用状态
        gridOccupied = new bool[gridWidth, gridHeight];
        
        if (displayAllItems)
        {
            DisplayAllItems();
        }
    }
    
    // 新的智能网格布局算法
    private void DisplayAllItems()
    {
        if (displayContainer == null)
        {
            Debug.LogError("显示容器未设置!");
            return;
        }
    
        ClearDisplayedItems();
        
        if (items == null || items.Count == 0)
        {
            Debug.LogWarning("物品列表为空，请在Inspector中添加ItemData!");
            return;
        }
    
        // 重置网格占用状态
        gridOccupied = new bool[gridWidth, gridHeight];
        
        int itemsToDisplay = Mathf.Min(displayCount, items.Count);
        
        for (int i = 0; i < itemsToDisplay; i++)
        {
            ItemData itemData = items[i];
            Vector2Int position = FindNextAvailablePosition(itemData.width, itemData.height);
            
            if (position.x != -1 && position.y != -1)
            {
                CreateAndPlaceItemWithGridOccupancy(itemData, position.x, position.y, i);
                MarkGridOccupied(position.x, position.y, itemData.width, itemData.height);
                Debug.Log($"放置物品 {i+1}: {itemData.name} 在位置 ({position.x}, {position.y}), 尺寸: {itemData.width}x{itemData.height}");
            }
            else
            {
                Debug.LogWarning($"无法为物品 {itemData.name} (尺寸: {itemData.width}x{itemData.height}) 找到合适的位置");
            }
        }
    
        Debug.Log($"成功显示了 {displayedItems.Count} 个物品");
    }
    
    // 查找下一个可用位置的函数
    private Vector2Int FindNextAvailablePosition(int itemWidth, int itemHeight)
    {
        for (int y = 0; y <= gridHeight - itemHeight; y++)
        {
            for (int x = 0; x <= gridWidth - itemWidth; x++)
            {
                if (CanPlaceItemAt(x, y, itemWidth, itemHeight))
                {
                    return new Vector2Int(x, y);
                }
            }
        }
        return new Vector2Int(-1, -1); // 找不到合适位置
    }
    
    // 检查指定位置是否可以放置物品
    private bool CanPlaceItemAt(int startX, int startY, int itemWidth, int itemHeight)
    {
        // 检查边界
        if (startX + itemWidth > gridWidth || startY + itemHeight > gridHeight)
            return false;
            
        // 检查占用状态
        for (int x = startX; x < startX + itemWidth; x++)
        {
            for (int y = startY; y < startY + itemHeight; y++)
            {
                if (gridOccupied[x, y])
                    return false;
            }
        }
        return true;
    }
    
    // 标记网格为已占用
    private void MarkGridOccupied(int startX, int startY, int itemWidth, int itemHeight)
    {
        for (int x = startX; x < startX + itemWidth; x++)
        {
            for (int y = startY; y < startY + itemHeight; y++)
            {
                gridOccupied[x, y] = true;
            }
        }
    }
    
    // 修改后的物品创建和放置方法
    private void CreateAndPlaceItemWithGridOccupancy(ItemData itemData, int gridX, int gridY, int index)
    {
        GameObject itemObj = Instantiate(itemPrefab, displayContainer);
        Item item = itemObj.GetComponent<Item>();
        item.Set(itemData);
        
        // 设置物品在网格中的位置信息
        item.onGridPositionX = gridX;
        item.onGridPositionY = gridY;

        // 计算实际显示位置
        Vector2 position = new Vector2();
        position.x = gridX * ItemGrid.tileSizeWidth + ItemGrid.tileSizeWidth * itemData.width / 2;
        position.y = -(gridY * ItemGrid.tileSizeHeight + ItemGrid.tileSizeHeight * itemData.height / 2);
        itemObj.transform.localPosition = position;

        // 设置物品名称以便调试
        itemObj.name = $"Item_{index}_{itemData.name}_({gridX},{gridY})";

        // 如果有ItemGrid，尝试将物品放置到网格中
        if (itemGrid != null)
        {
            if (itemGrid.BoundryCheck(gridX, gridY, itemData.width, itemData.height))
            {
                Item tempOverlap = null;
                bool placed = itemGrid.PlaceItem(item, gridX, gridY, ref tempOverlap);
                if (!placed)
                {
                    Debug.LogWarning($"无法将物品 {itemData.name} 放置到网格位置 ({gridX}, {gridY})");
                }
            }
            else
            {
                Debug.LogWarning($"物品 {itemData.name} 超出网格边界，位置: ({gridX}, {gridY}), 尺寸: {itemData.width}x{itemData.height}");
            }
        }

        // 添加文本标签显示物品信息和占用的网格范围
        GameObject textObj = new GameObject("ItemInfo");
        textObj.transform.SetParent(itemObj.transform, false);
        Text infoText = textObj.AddComponent<Text>();
        infoText.text = $"{itemData.name}\n{itemData.width}x{itemData.height}\n({gridX},{gridY})-({gridX+itemData.width-1},{gridY+itemData.height-1})";
        infoText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        infoText.fontSize = 8;
        infoText.color = Color.white;
        infoText.alignment = TextAnchor.MiddleCenter;

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        displayedItems.Add(itemObj);
    }
    
    // 修改清理方法
    private void ClearDisplayedItems()
    {
        foreach (GameObject itemObj in displayedItems)
        {
            if (itemObj != null)
            {
                DestroyImmediate(itemObj);
            }
        }
        displayedItems.Clear();
        
        // 重置网格占用状态
        if (gridOccupied != null)
        {
            gridOccupied = new bool[gridWidth, gridHeight];
        }
        
        // 清理网格
        if (itemGrid != null)
        {
            // 这里可能需要调用itemGrid的清理方法，如果有的话
        }
    }
    
    // 添加调试方法：显示网格占用状态
    [ContextMenu("显示网格占用状态")]
    private void DebugGridOccupancy()
    {
        if (gridOccupied == null) return;
        
        string gridState = "网格占用状态:\n";
        for (int y = 0; y < gridHeight; y++)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                gridState += gridOccupied[x, y] ? "X" : "O";
            }
            gridState += "\n";
        }
        Debug.Log(gridState);
    }
    
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            ToggleDisplayAllItems();
        }
        
        if (selectedItem) selectedItem.transform.position = Input.mousePosition;

        if (itemGrid == null)
        {
            if (inventoryHighlight) inventoryHighlight.Show(false);
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log(itemGrid.GetTileGridPosition(Input.mousePosition));
            LeftMouseButtonPress();
        }

        if (inventoryHighlight) HandleHighlight();
    }
    
    // LeftMouseButtonPress 方法
    private void LeftMouseButtonPress()
    {
        Vector2Int tileGridPosition = GetTileGridPosition();
        if (selectedItem == null)
        {
            selectedItem = itemGrid.PickUpItem(tileGridPosition.x, tileGridPosition.y);
        }
        else
        {
            PlaceItem(tileGridPosition);
        }
    }

    private Vector2Int GetTileGridPosition()
    {
        Vector2 position = Input.mousePosition;
        if (selectedItem != null)
        {
            position.x -= (selectedItem.itemData.width - 1) * ItemGrid.tileSizeWidth / 2;
            position.y += (selectedItem.itemData.height - 1) * ItemGrid.tileSizeHeight / 2;
        }
        Vector2Int tileGridPosition = itemGrid.GetTileGridPosition(position);
        return tileGridPosition;
    }

    void PlaceItem(Vector2Int tileGridPosition)
    {
        bool complete = itemGrid.PlaceItem(selectedItem, tileGridPosition.x, tileGridPosition.y, ref overlapItem);
        if (complete)
        {
            selectedItem = null;
            if (overlapItem != null)
            {
                selectedItem = overlapItem;
                overlapItem = null;
            }
        }
    }

    private void HandleHighlight()
    {
        Vector2Int positionOnGrid = GetTileGridPosition();
        if (oldPosition == positionOnGrid) return;
        oldPosition = positionOnGrid;

        if (selectedItem == null)
        {
            itemToHighlight = itemGrid.GetItem(positionOnGrid.x, positionOnGrid.y);
            if (itemToHighlight != null)
            {
                inventoryHighlight.Show(true);
                inventoryHighlight.SetSize(itemToHighlight);
                inventoryHighlight.SetParent(itemGrid);
                inventoryHighlight.SetPosition(itemGrid, itemToHighlight);
            }
            else
            {
                inventoryHighlight.Show(false);
            }
        }
        else
        {
            inventoryHighlight.Show(itemGrid.BoundryCheck(
                    positionOnGrid.x,
                    positionOnGrid.y,
                    selectedItem.itemData.width,
                    selectedItem.itemData.height)
            );
            inventoryHighlight.SetSize(selectedItem);
            inventoryHighlight.SetParent(itemGrid);
            inventoryHighlight.SetPosition(itemGrid, selectedItem, positionOnGrid.x, positionOnGrid.y);
        }
    }

    private void ToggleDisplayAllItems()
    {
        displayAllItems = !displayAllItems;
        if (displayAllItems)
        {
            DisplayAllItems();
        }
        else
        {
            ClearDisplayedItems();
        }
    }

    public void DisplayItemsByCategory(ItemCategory category)
    {
        ClearDisplayedItems();
        List<ItemData> filteredItems = items.FindAll(item => item.category == category);
        
        // 重置网格占用状态
        gridOccupied = new bool[gridWidth, gridHeight];
        
        int itemsToDisplay = Mathf.Min(displayCount, filteredItems.Count);
        
        for (int i = 0; i < itemsToDisplay; i++)
        {
            ItemData itemData = filteredItems[i];
            Vector2Int position = FindNextAvailablePosition(itemData.width, itemData.height);
            
            if (position.x != -1 && position.y != -1)
            {
                CreateAndPlaceItemWithGridOccupancy(itemData, position.x, position.y, i);
                MarkGridOccupied(position.x, position.y, itemData.width, itemData.height);
            }
            else
            {
                Debug.LogWarning($"无法为物品 {itemData.name} (尺寸: {itemData.width}x{itemData.height}) 找到合适的位置");
            }
        }
        
        Debug.Log($"显示了 {itemsToDisplay} 个 {category} 类别的物品");
    }
    
    // 添加缺失的 CreateAndPlaceItemSimple 方法（用于向后兼容）
    private void CreateAndPlaceItemSimple(ItemData itemData, int gridX, int gridY, int index)
    {
        // 使用新的方法替代
        CreateAndPlaceItemWithGridOccupancy(itemData, gridX, gridY, index);
    }

    [System.Serializable]
    public class ItemInfo
    {
        public string itemName;
        public int width;
        public int height;
        public ItemCategory category;

        public ItemInfo(ItemData itemData)
        {
            itemName = itemData.name;
            width = itemData.width;
            height = itemData.height;
            category = itemData.category;
        }
    }

    [SerializeField][FieldLabel("物品信息列表")] private List<ItemInfo> itemInfoList = new List<ItemInfo>();

    [ContextMenu("更新物品信息列表")]
    private void UpdateItemInfoList()
    {
        itemInfoList.Clear();
        foreach (ItemData itemData in items)
        {
            itemInfoList.Add(new ItemInfo(itemData));
        }
    }
}
