using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

// ==================== 网格类型枚举定义 ====================
/// <summary>
/// 网格类型枚举，定义四种核心网格类型
/// </summary>
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

/// <summary>
/// 网格特性标志，支持多重特性组合（为未来功能拆分预留）
/// </summary>
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

/// <summary>
/// 网格访问权限枚举（为未来权限控制预留）
/// </summary>
[System.Serializable]
public enum GridAccessLevel
{
    [InspectorName("公开")] Public = 0,
    [InspectorName("私有")] Private = 1,
    [InspectorName("共享")] Shared = 2,
    [InspectorName("只读")] ReadOnly = 3
}

public class ItemGrid : MonoBehaviour
{
    // ==================== 网格标识和类型配置 ====================
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
    
    // ==================== 网格特性配置（未来功能拆分预留） ====================
    [Header("网格特性配置（预留字段）")]
    [FieldLabel("网格特性")] 
    [SerializeField] private GridFeatures gridFeatures = GridFeatures.Saveable | GridFeatures.Draggable | GridFeatures.Sortable;
    
    [FieldLabel("访问权限")] 
    [SerializeField] private GridAccessLevel accessLevel = GridAccessLevel.Private;
    
    // ==================== 网格优先级和排序（未来功能拆分预留） ====================
    [Header("网格优先级配置（预留字段）")]
    [FieldLabel("网格优先级")] 
    [Range(0, 100)]
    [SerializeField] private int gridPriority = 50;
    
    [FieldLabel("排序权重")] 
    [SerializeField] private float sortWeight = 1.0f;
    
    [FieldLabel("是否为默认网格")] 
    [SerializeField] private bool isDefaultGrid = false;
    
    // ==================== 网格状态和统计 ====================
    [Header("网格状态信息")]
    [FieldLabel("网格激活状态")] 
    [SerializeField] private bool isGridActive = true;
    
    [FieldLabel("创建时间")] 
    [SerializeField] private string creationTime = "";
    
    [FieldLabel("最后修改时间")] 
    [SerializeField] private string lastModifiedTime = "";
    
    // ==================== 原有检查器数据显示 ====================
    [Header("网格尺寸配置")]
    [FieldLabel("网格宽度")] public int gridSizeWidth = 10;
    [FieldLabel("网格高度")] public int gridSizeHeight = 10;

    // ==================== 内部数据 ====================
    // 定义每个格子的宽度和高度
    public static float tileSizeWidth = 64f;
    public static float tileSizeHeight = 64f;

    // 物品存储数组
    Item[,] itemSlot;

    // 计算在格子中的位置
    RectTransform rectTransform;
    Canvas canvas;
    
    // ==================== 网格属性访问器 ====================
    /// <summary>
    /// 获取或设置网格GUID
    /// </summary>
    public string GridGUID 
    { 
        get 
        { 
            // 如果GUID为空，自动生成一个
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
    
    /// <summary>
    /// 获取或设置网格类型
    /// </summary>
    public GridType GridType 
    { 
        get => gridType; 
        set 
        {
            if (gridType != value)
            {
                gridType = value;
                lastModifiedTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            }
        }
    }
    
    /// <summary>
    /// 获取或设置网格名称
    /// </summary>
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
    
    /// <summary>
    /// 获取或设置网格描述
    /// </summary>
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
    
    /// <summary>
    /// 获取或设置网格特性（预留功能）
    /// </summary>
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
    
    /// <summary>
    /// 获取或设置访问权限（预留功能）
    /// </summary>
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
    
    /// <summary>
    /// 获取网格优先级（预留功能）
    /// </summary>
    public int GridPriority => gridPriority;
    
    /// <summary>
    /// 获取排序权重（预留功能）
    /// </summary>
    public float SortWeight => sortWeight;
    
    /// <summary>
    /// 获取是否为默认网格（预留功能）
    /// </summary>
    public bool IsDefaultGrid => isDefaultGrid;
    
    /// <summary>
    /// 获取或设置网格激活状态
    /// </summary>
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
    
    /// <summary>
    /// 获取创建时间
    /// </summary>
    public string CreationTime => creationTime;
    
    /// <summary>
    /// 获取最后修改时间
    /// </summary>
    public string LastModifiedTime => lastModifiedTime;
    
    // ==================== 网格特性检查方法（预留功能） ====================
    /// <summary>
    /// 检查是否具有指定特性（预留功能）
    /// </summary>
    /// <param name="feature">要检查的特性</param>
    /// <returns>是否具有该特性</returns>
    public bool HasFeature(GridFeatures feature)
    {
        return (gridFeatures & feature) == feature;
    }
    
    /// <summary>
    /// 添加网格特性（预留功能）
    /// </summary>
    /// <param name="feature">要添加的特性</param>
    public void AddFeature(GridFeatures feature)
    {
        if (!HasFeature(feature))
        {
            gridFeatures |= feature;
            lastModifiedTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }
    }
    
    /// <summary>
    /// 移除网格特性（预留功能）
    /// </summary>
    /// <param name="feature">要移除的特性</param>
    public void RemoveFeature(GridFeatures feature)
    {
        if (HasFeature(feature))
        {
            gridFeatures &= ~feature;
            lastModifiedTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }
    }
    
    /// <summary>
    /// 获取当前网格的物品数量
    /// </summary>
    /// <returns>物品数量</returns>
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

    // ==================== 初始化方法 ====================
    private void Awake()
    {
        // 确保GUID存在
        if (string.IsNullOrEmpty(gridGUID))
        {
            gridGUID = System.Guid.NewGuid().ToString();
        }
        
        // 设置创建时间（如果未设置）
        if (string.IsNullOrEmpty(creationTime))
        {
            creationTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }
        
        // 设置最后修改时间
        lastModifiedTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }

    // 初始化组件引用和数组
    private void Start()
    {
        // 初始化RectTransform组件
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            Debug.LogError($"ItemGrid [{gridName}]: 未找到RectTransform组件！");
            return;
        }
        
        // 初始化Canvas组件（从父级或当前对象查找）
        canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError($"ItemGrid [{gridName}]: 未找到Canvas组件！");
            return;
        }
        
        // 初始化物品存储数组
        itemSlot = new Item[gridSizeWidth, gridSizeHeight];
        
        // 初始化网格尺寸
        Init(gridSizeWidth, gridSizeHeight);
        
        Debug.Log($"ItemGrid [{gridName}] 初始化完成 - 尺寸: {gridSizeWidth}x{gridSizeHeight}, Canvas缩放: {canvas.scaleFactor}");
    }

    // ==================== 网格尺寸访问器（支持未来动态尺寸） ====================
    /// <summary>
    /// 获取当前网格宽度（统一访问器，支持未来动态尺寸）
    /// </summary>
    public int CurrentWidth => gridSizeWidth;
    
    /// <summary>
    /// 获取当前网格高度（统一访问器，支持未来动态尺寸）
    /// </summary>
    public int CurrentHeight => gridSizeHeight;

    // 初始化网格大小
    void Init(int width, int height)
    {
        Vector2 size = new Vector2(width * tileSizeWidth, height * tileSizeHeight);
        rectTransform.sizeDelta = size;
    }


    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // 获取当前鼠标位置在网格中的格子坐标，并打印到控制台
            Debug.Log(GetTileGridPosition(Input.mousePosition));
        }
    }

    // 根据鼠标位置计算在格子中的位置
    public Vector2Int GetTileGridPosition(Vector2 mousePosition)
    {
        // 计算相对于RectTransform的偏移量（从左上角开始）
        Vector2 positionOnTheGrid;
        positionOnTheGrid.x = mousePosition.x - rectTransform.position.x;
        positionOnTheGrid.y = rectTransform.position.y - mousePosition.y;
    
        // 调整到左上角起始点（考虑RectTransform的锚点）
        float gridWidth = CurrentWidth * tileSizeWidth * canvas.scaleFactor;
        float gridHeight = CurrentHeight * tileSizeHeight * canvas.scaleFactor;
    
        // 如果RectTransform的锚点是中心，需要调整偏移
        positionOnTheGrid.x += gridWidth / 2;
        positionOnTheGrid.y += gridHeight / 2;
    
        // 转换为网格坐标，考虑Canvas缩放
        Vector2Int tileGridPosition = new Vector2Int();  // 初始化为默认值
        tileGridPosition.x = Mathf.FloorToInt(positionOnTheGrid.x / (tileSizeWidth * canvas.scaleFactor));
        tileGridPosition.y = Mathf.FloorToInt(positionOnTheGrid.y / (tileSizeHeight * canvas.scaleFactor));
    
        // 确保坐标在有效范围内
        tileGridPosition.x = Mathf.Clamp(tileGridPosition.x, 0, CurrentWidth - 1);
        tileGridPosition.y = Mathf.Clamp(tileGridPosition.y, 0, CurrentHeight - 1);
    
        return tileGridPosition;
    }

    // 在指定位置放置物品
    public bool PlaceItem(Item item, int posX, int posY)
    {
        // 判断物品是否超出边界
        if (BoundryCheck(posX, posY, item.GetWidth(), item.GetHeight()) == false)
        {
            Debug.LogWarning($"ItemGrid: 物品 {item.name} 在位置 ({posX}, {posY}) 超出边界");
            return false;
        }

        // 检查目标位置是否已被占用（检查物品占用的所有格子）
        for (int x = posX; x < posX + item.GetWidth(); x++)
        {
            for (int y = posY; y < posY + item.GetHeight(); y++)
            {
                if (itemSlot[x, y] != null)
                {
                    Debug.LogWarning($"ItemGrid: 位置 ({x}, {y}) 已被占用");
                    return false;
                }
            }
        }

        // 将物品存储到所有占用的格子中
        for (int x = posX; x < posX + item.GetWidth(); x++)
        {
            for (int y = posY; y < posY + item.GetHeight(); y++)
            {
                itemSlot[x, y] = item;
            }
        }

        // 设置物品的父对象为当前网格
        item.transform.SetParent(transform, false);

        // 计算物品在网格中的本地位置
        Vector2 position = CalculatePositionOnGrid(item, posX, posY);

        // 设置物品的本地位置
        item.transform.localPosition = position;

        // 设置物品的网格状态
        item.SetGridState(this, new Vector2Int(posX, posY));
        
        return true;
    }

    // 判断物品是否超出边界（设为public以供DraggableItem调用）
    public bool BoundryCheck(int posX, int posY, int width, int height)
    {
        if (PositionCheck(posX, posY) == false) return false;
        posX += width - 1;
        posY += height - 1;
        if (PositionCheck(posX, posY) == false) return false;
        return true;
    }

    // 判断格子坐标是否超出（设为public以供边界检查调用）
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

        // 计算网格的总尺寸
        float gridWidth = CurrentWidth * tileSizeWidth;
        float gridHeight = CurrentHeight * tileSizeHeight;

        // 基于中心对齐计算位置
        // 物品的中心轴心(0.5,0.5)对准格子的中心
        float itemWidth = item.GetWidth() * tileSizeWidth;
        float itemHeight = item.GetHeight() * tileSizeHeight;
        
        position.x = (posX + item.GetWidth() * 0.5f) * tileSizeWidth - gridWidth / 2;
        position.y = gridHeight / 2 - (posY + item.GetHeight() * 0.5f) * tileSizeHeight;

        return position;
    }



    // 从指定位置拾取物品
    public Item PickUpItem(int x, int y)
    {
        // 检查坐标是否有效
        if (x < 0 || x >= CurrentWidth || y < 0 || y >= CurrentHeight)
        {
            Debug.LogWarning($"ItemGrid: 尝试从无效位置 ({x}, {y}) 拾取物品");
            return null;
        }

        // 获取该位置的物品
        Item toReturn = itemSlot[x, y];

        // 如果有物品，则从所有占用的格子中移除
        if (toReturn != null)
        {
            // 清理物品占用的所有格子
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
        }

        return toReturn;
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

    // 按格子坐标获取物品（添加此方法以匹配教程接口）
    public Item GetItem(int x, int y)
    {
        return GetItemAt(x, y);
    }

    // 检查指定区域是否有物品冲突（排除指定物品）
    public bool HasItemConflict(int posX, int posY, int width, int height, Item excludeItem = null)
    {
        // 首先进行边界检查
        if (!BoundryCheck(posX, posY, width, height))
        {
            Debug.LogWarning($"ItemGrid: 物品位置 ({posX}, {posY}) 超出边界，无法放置");
            return true; // 超出边界视为冲突
        }

        // 检查物品占用的所有格子
        for (int x = posX; x < posX + width; x++)
        {
            for (int y = posY; y < posY + height; y++)
            {
                // 检查坐标是否有效
                if (!PositionCheck(x, y))
                {
                    Debug.LogWarning($"ItemGrid: 检查位置 ({x}, {y}) 超出边界");
                    continue;
                }

                // 检查该位置是否有物品，且不是要排除的物品
                Item itemAt = itemSlot[x, y];
                if (itemAt != null && itemAt != excludeItem)
                {
                    
                    return true; // 发现冲突
                }
            }
        }
        
        return false; // 没有冲突
    }

    // 根据鼠标位置检测物品（优化版）
    public Item GetItemAtMousePosition(int mouseX, int mouseY)
    {
        // 检查坐标是否有效
        if (mouseX < 0 || mouseX >= CurrentWidth || mouseY < 0 || mouseY >= CurrentHeight)
        {
            return null;
        }

        // 直接返回该位置的物品（现在所有占用格子都存储了物品引用）
        Item item = itemSlot[mouseX, mouseY];

        return item;
    }

    // 检查拖拽时的物品重叠冲突（专门用于拖拽场景的重叠检测）
    public bool HasOverlapConflict(int posX, int posY, int width, int height, Item draggingItem)
    {
        // 首先进行边界检查
        if (!BoundryCheck(posX, posY, width, height))
        {
            return true; // 超出边界视为冲突
        }

        // 检查物品占用的所有格子是否与其他物品重叠
        for (int x = posX; x < posX + width; x++)
        {
            for (int y = posY; y < posY + height; y++)
            {
                // 检查该位置是否有物品，且不是正在拖拽的物品
                Item itemAt = itemSlot[x, y];
                if (itemAt != null && itemAt != draggingItem)
                {
                    return true; // 发现重叠冲突
                }
            }
        }

        return false; // 没有重叠冲突
    }

    // 检查指定位置是否可以放置物品（用于拖拽时的实时检测）
    public bool CanPlaceItemAtPosition(int posX, int posY, int width, int height, Item draggingItem)
    {
        // 边界检查
        if (!BoundryCheck(posX, posY, width, height))
        {
            return false;
        }

        // 重叠检查
        return !HasOverlapConflict(posX, posY, width, height, draggingItem);
    }
}
