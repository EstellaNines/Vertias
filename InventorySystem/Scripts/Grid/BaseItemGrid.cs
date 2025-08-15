using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public abstract class BaseItemGrid : MonoBehaviour, IDropHandler
{
    [Header("网格系统基础设置")]
    [SerializeField] protected GridConfig gridConfig;

    // 网格尺寸
    protected int width;
    protected int height;

    // 网格占用状态 - 多种优化方案
    protected bool[,] gridOccupancy;  // 原始bool数组
    protected int[,] prefixSum;       // 二维前缀和数组
    protected HashSet<Vector2Int> occupiedCells; // HashSet存储占用坐标
    protected List<PlacedItem> placedItems = new List<PlacedItem>();

    protected RectTransform rectTransform;
    protected Canvas canvas;

    // 延迟更新相关
    protected bool needsUpdate = false;
    protected int pendingWidth;
    protected int pendingHeight;

    // 防止无限循环的标记
    protected bool isUpdatingFromConfig = false;

    [System.Serializable]
    public class PlacedItem
    {
        public GameObject itemObject;
        public Vector2Int position;
        public Vector2Int size;
    }

    protected virtual void Awake()
    {
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }
        LoadDefaultGridConfig();
    }

    protected virtual void Start()
    {
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }

        if (Application.isPlaying)
        {
            canvas = FindObjectOfType<Canvas>();
            InitializeGridArrays();
        }

        Init(width, height);
    }

    // 初始化所有网格数组
    protected virtual void InitializeGridArrays()
    {
        gridOccupancy = new bool[width, height];
        prefixSum = new int[width + 1, height + 1];
        occupiedCells = new HashSet<Vector2Int>();
    }

    protected virtual void OnValidate()
    {
        if (isUpdatingFromConfig) return;

        LoadDefaultGridConfig();

        width = Mathf.Clamp(width, 1, 50);
        height = Mathf.Clamp(height, 1, 50);

        needsUpdate = true;
        pendingWidth = width;
        pendingHeight = height;

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            UnityEditor.EditorApplication.delayCall += DelayedUpdate;
        }
#endif
    }

#if UNITY_EDITOR
    private void DelayedUpdate()
    {
        UnityEditor.EditorApplication.delayCall -= DelayedUpdate;

        if (this != null && needsUpdate)
        {
            if (rectTransform == null)
            {
                rectTransform = GetComponent<RectTransform>();
            }

            if (rectTransform != null)
            {
                Init(pendingWidth, pendingHeight);
                needsUpdate = false;
                UnityEditor.SceneView.RepaintAll();
            }
        }
    }
#endif

    protected virtual void Update()
    {
        if (needsUpdate && Application.isPlaying)
        {
            Init(pendingWidth, pendingHeight);
            needsUpdate = false;
        }

        if (Application.isPlaying && Input.GetMouseButtonDown(0))
        {
            Debug.Log(GetTileGridPosition(Input.mousePosition));
        }
    }

    // 抽象方法，由子类实现具体的初始化逻辑
    protected abstract void Init(int width, int height);

    // 加载默认GridConfig
    protected virtual void LoadDefaultGridConfig()
    {
        if (gridConfig == null)
        {
            gridConfig = Resources.Load<GridConfig>("DefaultGridConfig");
            if (gridConfig == null)
            {
                string defaultConfigPath = "Assets/InventorySystem/Database/网格系统参数GridSystemSO/DefaultGridConfig.asset";
#if UNITY_EDITOR
                gridConfig = UnityEditor.AssetDatabase.LoadAssetAtPath<GridConfig>(defaultConfigPath);
#endif
            }
        }
    }

    // 根据鼠标位置计算在格子中的位置
    public virtual Vector2Int GetTileGridPosition(Vector2 screenMousePos)
    {
        if (canvas == null && Application.isPlaying)
        {
            canvas = FindObjectOfType<Canvas>();
        }

        float cellSize = gridConfig != null ? gridConfig.cellSize : 64f;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform,
            screenMousePos,
            canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null,
            out Vector2 localPoint);

        int x = Mathf.FloorToInt(localPoint.x / cellSize);
        int y = Mathf.FloorToInt(-localPoint.y / cellSize);

        x = Mathf.Clamp(x, 0, width - 1);
        y = Mathf.Clamp(y, 0, height - 1);
        return new Vector2Int(x, y);
    }

    // 实现IDropHandler接口
    public virtual void OnDrop(PointerEventData eventData)
    {
        var dropped = eventData.pointerDrag;
        if (dropped == null) return;

        var draggable = dropped.GetComponent<DraggableItem>();
        if (draggable == null) return;

        var item = draggable.GetComponent<InventorySystemItem>();
        if (item == null) return;

        Vector2Int dropPosition = GetTileGridPosition(eventData.position);
        Vector2Int itemSize = new Vector2Int(item.Data.width, item.Data.height);

        if (CanPlaceItem(dropPosition, itemSize))
        {
            PlaceItem(dropped, dropPosition, itemSize);
        }
    }

    // 优化后的检查是否可以在指定位置放置物品 - 使用HashSet O(1)查询
    public virtual bool CanPlaceItem(Vector2Int position, Vector2Int size)
    {
        // 边界检查
        if (position.x < 0 || position.y < 0 ||
            position.x + size.x > width || position.y + size.y > height)
        {
            return false;
        }

        // 使用HashSet进行O(1)查询
        for (int x = position.x; x < position.x + size.x; x++)
        {
            for (int y = position.y; y < position.y + size.y; y++)
            {
                if (occupiedCells.Contains(new Vector2Int(x, y)))
                {
                    return false;
                }
            }
        }

        return true;
    }

    // 使用前缀和数组的快速检查方法 - O(1)查询
    public virtual bool CanPlaceItemFast(Vector2Int position, Vector2Int size)
    {
        // 边界检查
        if (position.x < 0 || position.y < 0 ||
            position.x + size.x > width || position.y + size.y > height)
        {
            return false;
        }

        // 使用二维前缀和进行O(1)查询
        int x1 = position.x, y1 = position.y;
        int x2 = position.x + size.x - 1, y2 = position.y + size.y - 1;

        int occupiedCount = prefixSum[x2 + 1, y2 + 1]
                          - prefixSum[x1, y2 + 1]
                          - prefixSum[x2 + 1, y1]
                          + prefixSum[x1, y1];

        return occupiedCount == 0;
    }

    // 0/1标记法检测占用情况
    public virtual int[,] GetOccupancyMatrix()
    {
        int[,] matrix = new int[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                matrix[x, y] = gridOccupancy[x, y] ? 1 : 0;
            }
        }
        return matrix;
    }

    // 获取指定区域的占用状态
    public virtual bool IsAreaOccupied(Vector2Int position, Vector2Int size)
    {
        return !CanPlaceItem(position, size);
    }

    // 放置物品到网格中 - 改为public
    public virtual void PlaceItem(GameObject itemObject, Vector2Int position, Vector2Int size)
    {
        RectTransform itemRect = itemObject.GetComponent<RectTransform>();
        if (itemRect == null) return;

        float cellSize = gridConfig != null ? gridConfig.cellSize : 64f;

        itemRect.anchorMin = new Vector2(0, 1);
        itemRect.anchorMax = new Vector2(0, 1);
        itemRect.pivot = new Vector2(0, 1);

        Vector2 itemPosition = new Vector2(
            position.x * cellSize,
            -position.y * cellSize
        );

        itemRect.anchoredPosition = itemPosition;

        MarkGridOccupied(position, size, true);

        placedItems.Add(new PlacedItem
        {
            itemObject = itemObject,
            position = position,
            size = size
        });

        Debug.Log($"物品放置到网格位置: ({position.x}, {position.y}), UI位置: {itemPosition}, 物品尺寸: {size.x}x{size.y}");
    }

    // 优化后的标记网格占用状态
    protected virtual void MarkGridOccupied(Vector2Int position, Vector2Int size, bool occupied)
    {
        for (int x = position.x; x < position.x + size.x; x++)
        {
            for (int y = position.y; y < position.y + size.y; y++)
            {
                // 更新bool数组
                gridOccupancy[x, y] = occupied;

                // 更新HashSet
                Vector2Int cell = new Vector2Int(x, y);
                if (occupied)
                {
                    occupiedCells.Add(cell);
                }
                else
                {
                    occupiedCells.Remove(cell);
                }
            }
        }

        // 更新前缀和数组
        UpdatePrefixSum();
    }

    // 更新二维前缀和数组
    protected virtual void UpdatePrefixSum()
    {
        // 重置前缀和数组
        for (int i = 0; i <= width; i++)
        {
            for (int j = 0; j <= height; j++)
            {
                prefixSum[i, j] = 0;
            }
        }

        // 计算前缀和
        for (int i = 1; i <= width; i++)
        {
            for (int j = 1; j <= height; j++)
            {
                int cellValue = gridOccupancy[i - 1, j - 1] ? 1 : 0;
                prefixSum[i, j] = cellValue + prefixSum[i - 1, j] + prefixSum[i, j - 1] - prefixSum[i - 1, j - 1];
            }
        }
    }

    // 移除物品
    public virtual void RemoveItem(GameObject item)
    {
        PlacedItem placedItem = placedItems.Find(p => p.itemObject == item);
        if (placedItem != null)
        {
            MarkGridOccupied(placedItem.position, placedItem.size, false);
            placedItems.Remove(placedItem);
        }
    }

    // 获取网格尺寸
    public virtual Vector2Int GetGridSize()
    {
        return new Vector2Int(width, height);
    }

    // 获取网格占用状态
    public virtual bool[,] GetGridOccupancy()
    {
        return gridOccupancy;
    }

    // 获取单元格大小
    public virtual float GetCellSize()
    {
        return gridConfig != null ? gridConfig.cellSize : 64f;
    }

    // 设置GridConfig
    public virtual void SetGridConfig(GridConfig config)
    {
        gridConfig = config;
    }

    // 获取GridConfig
    public virtual GridConfig GetGridConfig()
    {
        return gridConfig;
    }

    // 按格子坐标获取物品
    public virtual InventorySystemItem GetItem(int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height)
        {
            return null;
        }

        foreach (PlacedItem placedItem in placedItems)
        {
            if (x >= placedItem.position.x && x < placedItem.position.x + placedItem.size.x &&
                y >= placedItem.position.y && y < placedItem.position.y + placedItem.size.y)
            {
                return placedItem.itemObject.GetComponent<InventorySystemItem>();
            }
        }

        return null;
    }

    // 根据鼠标屏幕位置获取物品
    public virtual InventorySystemItem GetItemAtScreenPosition(Vector2 screenMousePos)
    {
        Vector2Int gridPos = GetTileGridPosition(screenMousePos);
        return GetItem(gridPos.x, gridPos.y);
    }

    // 获取所有已放置的物品
    public virtual List<PlacedItem> GetPlacedItems()
    {
        return new List<PlacedItem>(placedItems);
    }

    // 清空网格中的所有物品 - 优化版本
    public virtual void ClearGrid()
    {
        if (gridOccupancy != null)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    gridOccupancy[x, y] = false;
                }
            }
        }

        // 清空HashSet和前缀和数组
        occupiedCells?.Clear();
        if (prefixSum != null)
        {
            for (int i = 0; i <= width; i++)
            {
                for (int j = 0; j <= height; j++)
                {
                    prefixSum[i, j] = 0;
                }
            }
        }

        placedItems.Clear();
    }

    // 获取占用的坐标集合
    public virtual HashSet<Vector2Int> GetOccupiedCells()
    {
        return new HashSet<Vector2Int>(occupiedCells);
    }

    // 获取占用率
    public virtual float GetOccupancyRate()
    {
        if (width * height == 0) return 0f;
        return (float)occupiedCells.Count / (width * height);
    }

    // 按格子坐标转化为UI坐标位置
    public virtual Vector2 CalculatePositionOnGrid(InventorySystemItem item, int posX, int posY)
    {
        Vector2 position = new Vector2();
        float cellSize = GetCellSize();

        if (item != null && item.Data != null)
        {
            position.x = posX * cellSize + cellSize * item.Data.width / 2;
            position.y = -(posY * cellSize + cellSize * item.Data.height / 2);
        }
        else
        {
            // 如果没有物品数据，使用默认1x1大小
            position.x = posX * cellSize + cellSize / 2;
            position.y = -(posY * cellSize + cellSize / 2);
        }

        return position;
    }

    // 重载方法：直接指定物品大小
    public virtual Vector2 CalculatePositionOnGrid(int itemWidth, int itemHeight, int posX, int posY)
    {
        Vector2 position = new Vector2();
        float cellSize = GetCellSize();

        position.x = posX * cellSize + cellSize * itemWidth / 2;
        position.y = -(posY * cellSize + cellSize * itemHeight / 2);

        return position;
    }

    // 边界检查方法
    public virtual bool BoundryCheck(int posX, int posY, int width, int height)
    {
        if (posX < 0 || posY < 0) return false;
        if (posX + width > this.width || posY + height > this.height) return false;
        return true;
    }
}