using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

// 网格类型枚举
[System.Serializable]
public enum GridType
{
    [InspectorName("背包")] Backpack = 0,
    [InspectorName("仓库")] Storage = 1,
    [InspectorName("挂具")] Equipment = 2,
    [InspectorName("地面")] Ground = 3,
    [InspectorName("其他")] Other = 4,
    [InspectorName("自定义")] Custom = 5,
    [InspectorName("测试")] Test = 99,
}

#region 网格特性标志
// 网格特性标志
[System.Flags]
[System.Serializable]
public enum GridFeatures
{
    [InspectorName("无特性")] None = 0,
    [InspectorName("可保存")] Saveable = 1 << 0,
    [InspectorName("可拖拽")] Draggable = 1 << 1,
    [InspectorName("可排序")] Sortable = 1 << 2,
    [InspectorName("自动整理")] AutoOrganize = 1 << 3,
    [InspectorName("限制物品类型")] ItemTypeRestricted = 1 << 4,
    [InspectorName("只读模式")] ReadOnly = 1 << 5,
    [InspectorName("支持堆叠")] Stackable = 1 << 6,
    [InspectorName("自动保存")] AutoSave = 1 << 7,
    [InspectorName("临时存储")] TemporaryStorage = 1 << 8
}
#endregion

#region 网格访问权限枚举
// 网格访问权限枚举
[System.Serializable]
public enum GridAccessLevel
{
    [InspectorName("公开")] Public = 0,
    [InspectorName("私有")] Private = 1,
    [InspectorName("共享")] Shared = 2,
    [InspectorName("只读")] ReadOnly = 3
}
#endregion


public class ItemGrid : MonoBehaviour
{
    // 网格标识配置
    [Header("网格标识配置")]
    [FieldLabel("网格唯一标识")] 
    [SerializeField] private string gridGUID = "";
    
    [FieldLabel("网格类型")] 
    [SerializeField] private GridType gridType = GridType.Other;
    
    [FieldLabel("网格名称")] 
    [SerializeField] private string gridName = "默认网格";
    
    [FieldLabel("网格描述")] 
    [TextArea(2, 4)]
    [SerializeField] private string gridDescription = "";
    
    // 网格特性配置
    [Header("网格特性配置（预留字段）")]
    [FieldLabel("网格特性")] 
    [SerializeField] private GridFeatures gridFeatures = GridFeatures.Saveable | GridFeatures.Draggable | GridFeatures.Sortable;
    
    [FieldLabel("访问权限")] 
    [SerializeField] private GridAccessLevel accessLevel = GridAccessLevel.Private;
    
    // 网格优先级配置
    [Header("网格优先级配置（预留字段）")]
    [FieldLabel("网格优先级")] 
    [Range(0, 100)]
    [SerializeField] private int gridPriority = 50;
    
    [FieldLabel("排序权重")] 
    [SerializeField] private float sortWeight = 1.0f;
    
    [FieldLabel("是否为默认网格")] 
    [SerializeField] private bool isDefaultGrid = false;
    
    // 网格状态信息
    [Header("网格状态信息")]
    [FieldLabel("网格激活状态")] 
    [SerializeField] private bool isGridActive = true;
    
    [FieldLabel("创建时间")] 
    [SerializeField] private string creationTime = "";
    
    [FieldLabel("最后修改时间")] 
    [SerializeField] private string lastModifiedTime = "";
    
    // 网格尺寸配置
    [Header("网格尺寸配置")]
    [FieldLabel("网格宽度")] public int gridSizeWidth = 10;
    [FieldLabel("网格高度")] public int gridSizeHeight = 10;

    // 格子尺寸
    public static float tileSizeWidth = 64f;
    public static float tileSizeHeight = 64f;

    // 物品存储数组
    Item[,] itemSlot;

    // 组件引用
    RectTransform rectTransform;
    Canvas canvas;
    
    // 事件系统
    public event System.Action<Item, Vector2Int> OnItemPlaced;
    public event System.Action<Item, Vector2Int> OnItemRemoved;
    public event System.Action<Item, Vector2Int, Vector2Int> OnItemMoved;
    public event System.Action<ItemGrid> OnGridInitialized;
    public event System.Action<ItemGrid> OnGridCleared;
    
    // 网格GUID属性
    public string GridGUID 
    { 
        get 
        { 
            if (string.IsNullOrEmpty(gridGUID))
            {
                gridGUID = System.Guid.NewGuid().ToString();
                lastModifiedTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            }
            return gridGUID;
        }
        set 
        {
            if (gridGUID != value)
            {
                gridGUID = value;
                lastModifiedTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            }
        }
    }
    
    // 网格类型属性
    public GridType GridType 
    { 
        get => gridType; 
        set 
        {
            if (gridType != value)
            {
                GridType oldType = gridType;
                gridType = value;
                lastModifiedTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                OnGridTypeChanged(oldType, gridType);
            }
        }
    }
    
    // 网格名称属性
    public string GridName 
    { 
        get => gridName; 
        set 
        {
            if (gridName != value)
            {
                gridName = value;
                lastModifiedTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            }
        }
    }
    
    // 网格描述属性
    public string GridDescription 
    { 
        get => gridDescription; 
        set 
        {
            if (gridDescription != value)
            {
                gridDescription = value;
                lastModifiedTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            }
        }
    }
    
    // 网格特性属性
    public GridFeatures GridFeatures 
    { 
        get => gridFeatures; 
        set 
        {
            if (gridFeatures != value)
            {
                gridFeatures = value;
                lastModifiedTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            }
        }
    }
    
    // 访问权限属性
    public GridAccessLevel AccessLevel 
    { 
        get => accessLevel; 
        set 
        {
            if (accessLevel != value)
            {
                accessLevel = value;
                lastModifiedTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            }
        }
    }
    
    // 只读属性
    public int GridPriority => gridPriority;
    public float SortWeight => sortWeight;
    public bool IsDefaultGrid => isDefaultGrid;
    
    // 网格激活状态属性
    public bool IsGridActive 
    { 
        get => isGridActive; 
        set 
        {
            if (isGridActive != value)
            {
                isGridActive = value;
                lastModifiedTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            }
        }
    }
    
    // 时间属性
    public string CreationTime => creationTime;
    public string LastModifiedTime => lastModifiedTime;
    
    // 检查是否具有指定特性
    public bool HasFeature(GridFeatures feature)
    {
        return (gridFeatures & feature) == feature;
    }
    
    // 网格状态变化回调方法（子类可重写）
    protected virtual void OnGridTypeChanged(GridType oldType, GridType newType)
    {
        // 子类可以重写此方法
        Debug.Log($"网格 {gridName} 类型从 {oldType} 变更为 {newType}");
    }
    
    protected virtual void OnGridFeaturesChanged(GridFeatures oldFeatures, GridFeatures newFeatures)
    {
        // 子类可以重写此方法
    }
    
    protected virtual void OnGridSizeChanged(int oldWidth, int oldHeight, int newWidth, int newHeight)
    {
        // 子类可以重写此方法
    }
    
    // 添加网格特性
    public void AddFeature(GridFeatures feature)
    {
        if (!HasFeature(feature))
        {
            gridFeatures |= feature;
            lastModifiedTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }
    }
    
    // 移除网格特性
    public void RemoveFeature(GridFeatures feature)
    {
        if (HasFeature(feature))
        {
            gridFeatures &= ~feature;
            lastModifiedTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }
    }
    
    // 获取当前网格的物品数量
    public int GetCurrentItemCount()
    {
        int count = 0;
        HashSet<Item> countedItems = new HashSet<Item>();
        
        for (int x = 0; x < gridSizeWidth; x++)
        {
            for (int y = 0; y < gridSizeHeight; y++)
            {
                if (itemSlot[x, y] != null && !countedItems.Contains(itemSlot[x, y]))
                {
                    countedItems.Add(itemSlot[x, y]);
                    count++;
                }
            }
        }
        
        return count;
    }
    
    // 获取空槽位数量
    public int GetEmptySlotCount()
    {
        int emptyCount = 0;
        for (int x = 0; x < gridSizeWidth; x++)
        {
            for (int y = 0; y < gridSizeHeight; y++)
            {
                if (itemSlot[x, y] == null)
                {
                    emptyCount++;
                }
            }
        }
        return emptyCount;
    }
    
    // 获取已占用槽位数量
    public int GetOccupiedSlotCount()
    {
        return (gridSizeWidth * gridSizeHeight) - GetEmptySlotCount();
    }

    // 初始化GUID和时间
    private void Awake()
    {
        if (string.IsNullOrEmpty(gridGUID))
        {
            gridGUID = System.Guid.NewGuid().ToString();
        }
        
        if (string.IsNullOrEmpty(creationTime))
        {
            creationTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }
        
        lastModifiedTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }

    // 初始化组件和数组
    private void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            Debug.LogError($"ItemGrid [{gridName}]: 未找到RectTransform组件！");
            return;
        }
        
        canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError($"ItemGrid [{gridName}]: 未找到Canvas组件！");
            return;
        }
        
        itemSlot = new Item[gridSizeWidth, gridSizeHeight];
        Init(gridSizeWidth, gridSizeHeight);
        
        // 触发网格初始化事件
        OnGridInitialized?.Invoke(this);
        
        Debug.Log($"ItemGrid [{gridName}] 初始化完成 - 尺寸: {gridSizeWidth}x{gridSizeHeight}, Canvas缩放: {canvas.scaleFactor}");
    }

    // 网格尺寸访问器
    public int CurrentWidth => gridSizeWidth;
    public int CurrentHeight => gridSizeHeight;

    // 初始化网格大小
    void Init(int width, int height)
    {
        Vector2 size = new Vector2(width * tileSizeWidth, height * tileSizeHeight);
        rectTransform.sizeDelta = size;
        
        rectTransform.anchorMin = new Vector2(0, 1);
        rectTransform.anchorMax = new Vector2(0, 1);
        rectTransform.pivot = new Vector2(0, 1);
    }

    // 测试用鼠标点击
    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log(GetTileGridPosition(Input.mousePosition));
        }
    }

    // 根据鼠标位置计算格子坐标
    public Vector2Int GetTileGridPosition(Vector2 mousePosition)
    {
        Vector2 positionOnTheGrid;
        
        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);
        
        Vector2 gridTopLeft = corners[1];
        
        positionOnTheGrid.x = mousePosition.x - gridTopLeft.x;
        positionOnTheGrid.y = gridTopLeft.y - mousePosition.y;
        
        Vector2Int tileGridPosition = new Vector2Int();
        tileGridPosition.x = Mathf.FloorToInt(positionOnTheGrid.x / (tileSizeWidth * canvas.scaleFactor));
        tileGridPosition.y = Mathf.FloorToInt(positionOnTheGrid.y / (tileSizeHeight * canvas.scaleFactor));
        
        tileGridPosition.x = Mathf.Clamp(tileGridPosition.x, 0, CurrentWidth - 1);
        tileGridPosition.y = Mathf.Clamp(tileGridPosition.y, 0, CurrentHeight - 1);
        
        return tileGridPosition;
    }

    // 在指定位置放置物品
    public virtual bool PlaceItem(Item item, int posX, int posY)
    {
        // 验证物品放置
        if (!ValidateItemPlacement(item, posX, posY))
        {
            return false;
        }

        // 存储物品到所有占用格子
        for (int x = posX; x < posX + item.GetWidth(); x++)
        {
            for (int y = posY; y < posY + item.GetHeight(); y++)
            {
                itemSlot[x, y] = item;
            }
        }

        item.transform.SetParent(transform, false);
        Vector2 position = CalculatePositionOnGrid(item, posX, posY);
        item.transform.localPosition = position;
        item.SetGridState(this, new Vector2Int(posX, posY));
        
        // 触发物品放置事件
        OnItemPlaced?.Invoke(item, new Vector2Int(posX, posY));
        
        return true;
    }
    
    // 物品放置验证（子类可重写）
    protected virtual bool ValidateItemPlacement(Item item, int posX, int posY)
    {
        // 基础验证：边界检查
        if (!BoundryCheck(posX, posY, item.GetWidth(), item.GetHeight()))
        {
            Debug.LogWarning($"ItemGrid: 物品 {item.name} 在位置 ({posX}, {posY}) 超出边界");
            return false;
        }

        // 基础验证：冲突检查
        if (HasItemConflict(posX, posY, item.GetWidth(), item.GetHeight()))
        {
            Debug.LogWarning($"ItemGrid: 位置 ({posX}, {posY}) 已被占用");
            return false;
        }
        
        return true;
    }

    // 边界检查
    public bool BoundryCheck(int posX, int posY, int width, int height)
    {
        if (PositionCheck(posX, posY) == false) return false;
        posX += width - 1;
        posY += height - 1;
        if (PositionCheck(posX, posY) == false) return false;
        return true;
    }

    // 位置检查
    public bool PositionCheck(int posX, int posY)
    {
        if (posX < 0 || posY < 0) return false;
        if (posX >= CurrentWidth || posY >= CurrentHeight) return false;
        return true;
    }

    // 计算物品在网格中的位置
    public Vector2 CalculatePositionOnGrid(Item item, int posX, int posY)
    {
        Vector2 position = new Vector2();
        
        float itemWidth = item.GetWidth() * tileSizeWidth;
        float itemHeight = item.GetHeight() * tileSizeHeight;
        
        float targetLeftTopX = posX * tileSizeWidth;
        float targetLeftTopY = posY * tileSizeHeight;
        
        float centerOffsetX = itemWidth * 0.5f;
        float centerOffsetY = itemHeight * 0.5f;
        
        position.x = targetLeftTopX + centerOffsetX;
        position.y = -(targetLeftTopY + centerOffsetY);
        
        return position;
    }

    // 从指定位置拾取物品
    public virtual Item PickUpItem(int x, int y)
    {
        // 验证物品拾取
        if (!ValidateItemPickup(x, y))
        {
            return null;
        }

        Item toReturn = itemSlot[x, y];

        if (toReturn != null)
        {
            Vector2Int itemPosition = toReturn.OnGridPosition;
            int itemWidth = toReturn.GetWidth();
            int itemHeight = toReturn.GetHeight();

            for (int clearX = itemPosition.x; clearX < itemPosition.x + itemWidth; clearX++)
            {
                for (int clearY = itemPosition.y; clearY < itemPosition.y + itemHeight; clearY++)
                {
                    if (clearX >= 0 && clearX < CurrentWidth && clearY >= 0 && clearY < CurrentHeight)
                    {
                        itemSlot[clearX, clearY] = null;
                    }
                }
            }
            
            // 触发物品移除事件
            OnItemRemoved?.Invoke(toReturn, itemPosition);
        }

        return toReturn;
    }
    
    // 物品拾取验证（子类可重写）
    protected virtual bool ValidateItemPickup(int x, int y)
    {
        // 基础验证：位置检查
        if (x < 0 || x >= CurrentWidth || y < 0 || y >= CurrentHeight)
        {
            Debug.LogWarning($"ItemGrid: 尝试从无效位置 ({x}, {y}) 拾取物品");
            return false;
        }
        
        return true;
    }

    // 检查指定位置是否有物品
    public bool HasItemAt(int x, int y)
    {
        if (x < 0 || x >= CurrentWidth || y < 0 || y >= CurrentHeight)
        {
            return false;
        }

        return itemSlot[x, y] != null;
    }

    // 获取指定位置的物品
    public Item GetItemAt(int x, int y)
    {
        if (x < 0 || x >= CurrentWidth || y < 0 || y >= CurrentHeight)
        {
            return null;
        }

        return itemSlot[x, y];
    }

    // 按格子坐标获取物品
    public Item GetItem(int x, int y)
    {
        return GetItemAt(x, y);
    }

    // 检查指定区域是否有物品冲突
    public bool HasItemConflict(int posX, int posY, int width, int height, Item excludeItem = null)
    {
        if (!BoundryCheck(posX, posY, width, height))
        {
            Debug.LogWarning($"ItemGrid: 物品位置 ({posX}, {posY}) 超出边界，无法放置");
            return true;
        }

        for (int x = posX; x < posX + width; x++)
        {
            for (int y = posY; y < posY + height; y++)
            {
                if (!PositionCheck(x, y))
                {
                    Debug.LogWarning($"ItemGrid: 检查位置 ({x}, {y}) 超出边界");
                    continue;
                }

                Item itemAt = itemSlot[x, y];
                if (itemAt != null && itemAt != excludeItem)
                {
                    return true;
                }
            }
        }
        
        return false;
    }

    // 根据鼠标位置检测物品
    public Item GetItemAtMousePosition(int mouseX, int mouseY)
    {
        if (mouseX < 0 || mouseX >= CurrentWidth || mouseY < 0 || mouseY >= CurrentHeight)
        {
            return null;
        }

        Item item = itemSlot[mouseX, mouseY];
        return item;
    }

    // 检查拖拽时的物品重叠冲突
    public bool HasOverlapConflict(int posX, int posY, int width, int height, Item draggingItem)
    {
        if (!BoundryCheck(posX, posY, width, height))
        {
            return true;
        }

        for (int x = posX; x < posX + width; x++)
        {
            for (int y = posY; y < posY + height; y++)
            {
                Item itemAt = itemSlot[x, y];
                if (itemAt != null && itemAt != draggingItem)
                {
                    return true;
                }
            }
        }

        return false;
    }

    // 检查指定位置是否可以放置物品
    public bool CanPlaceItemAtPosition(int posX, int posY, int width, int height, Item draggingItem)
    {
        if (!BoundryCheck(posX, posY, width, height))
        {
            return false;
        }

        return !HasOverlapConflict(posX, posY, width, height, draggingItem);
    }
    
    // 获取网格统计信息
    public virtual GridStatistics GetGridStatistics()
    {
        return new GridStatistics
        {
            totalSlots = gridSizeWidth * gridSizeHeight,
            occupiedSlots = GetOccupiedSlotCount(),
            emptySlots = GetEmptySlotCount(),
            itemCount = GetCurrentItemCount()
        };
    }
    
    // 获取指定区域的物品列表
    public virtual List<Item> GetItemsInArea(int startX, int startY, int width, int height)
    {
        List<Item> items = new List<Item>();
        HashSet<Item> uniqueItems = new HashSet<Item>();
        
        for (int x = startX; x < startX + width; x++)
        {
            for (int y = startY; y < startY + height; y++)
            {
                if (x >= 0 && x < CurrentWidth && y >= 0 && y < CurrentHeight)
                {
                    Item item = GetItemAt(x, y);
                    if (item != null && !uniqueItems.Contains(item))
                    {
                        uniqueItems.Add(item);
                        items.Add(item);
                    }
                }
            }
        }
        
        return items;
    }
    
    // 清空网格
    public virtual void ClearGrid()
    {
        for (int x = 0; x < gridSizeWidth; x++)
        {
            for (int y = 0; y < gridSizeHeight; y++)
            {
                itemSlot[x, y] = null;
            }
        }
        
        // 触发网格清空事件
        OnGridCleared?.Invoke(this);
    }
}

// 网格统计信息结构
[System.Serializable]
public struct GridStatistics
{
    public int totalSlots;
    public int occupiedSlots;
    public int emptySlots;
    public int itemCount;
    
    public float occupancyRate => totalSlots > 0 ? (float)occupiedSlots / totalSlots : 0f;
}
