using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using InventorySystem.Grid;
#if UNITY_EDITOR
#endif

[ExecuteInEditMode]
public abstract class BaseItemGrid : MonoBehaviour, IDropHandler, ISaveable
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
        public InventorySystemItem item;  // 添加item属性用于兼容性
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
            // 初始化保存系统
            InitializeSaveSystem();
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

        if (CanPlaceItem(dropped, dropPosition))
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

    // 智能检测物品放置（获取物品尺寸）
    public virtual bool CanPlaceItem(GameObject itemObject, Vector2Int position)
    {
        // 尝试获取InventorySystemItem
        var inventoryItem = itemObject.GetComponent<InventorySystemItem>();
        if (inventoryItem != null && inventoryItem.Data != null)
        {
            // 使用原始尺寸
            Vector2Int originalSize = new Vector2Int(inventoryItem.Data.width, inventoryItem.Data.height);
            return CanPlaceItem(position, originalSize);
        }

        // 默认使用1x1尺寸
        return CanPlaceItem(position, Vector2Int.one);
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

        // 获取InventorySystemItem组件用于兼容性
        var inventoryItem = itemObject.GetComponent<InventorySystemItem>();

        placedItems.Add(new PlacedItem
        {
            itemObject = itemObject,
            item = inventoryItem,  // 设置item属性用于兼容性
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

    // 添加公共访问器用于向后兼容
    public virtual int Width => width;
    public virtual int Height => height;

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

    // 检查网格是否包含指定物品
    public virtual bool ContainsItem(GameObject itemObject)
    {
        if (itemObject == null) return false;

        foreach (PlacedItem placedItem in placedItems)
        {
            if (placedItem.itemObject == itemObject)
            {
                return true;
            }
        }

        return false;
    }

    // 检查网格是否包含指定的InventorySystemItem
    public virtual bool ContainsItem(InventorySystemItem item)
    {
        if (item == null) return false;

        foreach (PlacedItem placedItem in placedItems)
        {
            var inventoryItem = placedItem.itemObject?.GetComponent<InventorySystemItem>();
            if (inventoryItem == item)
            {
                return true;
            }
        }

        return false;
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

    // ==================== 网格检测器扩展功能 ====================

    /// <summary>
    /// 获取网格详细状态信息
    /// 包含网格基本信息、占用状态、物品分布等详细数据
    /// </summary>
    /// <returns>网格状态详细信息</returns>
    public virtual GridDetectorInfo GetGridDetectorInfo()
    {
        var detectorInfo = new GridDetectorInfo
        {
            // 基本网格信息
            gridID = GetSaveID(),
            gridType = GetType().Name,
            gridSize = new Vector2Int(width, height),
            totalCells = width * height,

            // 占用状态信息
            occupiedCellsCount = occupiedCells.Count,
            occupancyRate = GetOccupancyRate(),
            availableCells = width * height - occupiedCells.Count,

            // 物品分布信息
            placedItemsCount = placedItems.Count,
            itemDistribution = GetItemDistributionMap(),
            occupancyMatrix = GetOccupancyMatrix(),

            // 时间戳信息
            lastModified = GetLastModified(),
            detectionTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),

            // 网格配置信息
            cellSize = new Vector2(GetCellSize(), GetCellSize()),
            gridWorldPosition = transform.position,
            isActive = gameObject.activeInHierarchy
        };

        return detectorInfo;
    }

    /// <summary>
    /// 获取物品分布映射表
    /// 返回每个物品在网格中的详细位置和尺寸信息
    /// </summary>
    /// <returns>物品分布映射字典</returns>
    public virtual Dictionary<string, ItemPlacementInfo> GetItemDistributionMap()
    {
        var distributionMap = new Dictionary<string, ItemPlacementInfo>();

        for (int i = 0; i < placedItems.Count; i++)
        {
            var placedItem = placedItems[i];
            if (placedItem.itemObject == null) continue;

            string itemKey = $"Item_{i}_{placedItem.itemObject.GetInstanceID()}";

            var placementInfo = new ItemPlacementInfo
            {
                itemInstanceID = objectToInstanceID.ContainsKey(placedItem.itemObject) ?
                    objectToInstanceID[placedItem.itemObject] : "",
                itemName = placedItem.itemObject.name,
                gridPosition = placedItem.position,
                itemSize = placedItem.size,
                occupiedCells = GetItemOccupiedCells(placedItem.position, placedItem.size),
                placementIndex = i,
                itemGameObject = placedItem.itemObject
            };

            // 获取物品数据信息
            var inventoryItem = placedItem.itemObject.GetComponent<InventorySystemItem>();
            if (inventoryItem != null && inventoryItem.Data != null)
            {
                placementInfo.itemDataName = inventoryItem.Data.itemName;
                placementInfo.itemDataPath = GetItemDataPath(inventoryItem.Data);
            }

            distributionMap[itemKey] = placementInfo;
        }

        return distributionMap;
    }

    /// <summary>
    /// 获取指定物品占用的所有网格坐标
    /// </summary>
    /// <param name="position">物品起始位置</param>
    /// <param name="size">物品尺寸</param>
    /// <returns>占用的网格坐标列表</returns>
    public virtual List<Vector2Int> GetItemOccupiedCells(Vector2Int position, Vector2Int size)
    {
        var occupiedCells = new List<Vector2Int>();

        for (int x = position.x; x < position.x + size.x; x++)
        {
            for (int y = position.y; y < position.y + size.y; y++)
            {
                if (x >= 0 && x < width && y >= 0 && y < height)
                {
                    occupiedCells.Add(new Vector2Int(x, y));
                }
            }
        }

        return occupiedCells;
    }

    /// <summary>
    /// 检测指定区域的占用详情
    /// 返回区域内每个格子的占用状态和占用物品信息
    /// </summary>
    /// <param name="startPos">检测区域起始位置</param>
    /// <param name="areaSize">检测区域尺寸</param>
    /// <returns>区域占用详情</returns>
    public virtual AreaOccupancyInfo DetectAreaOccupancy(Vector2Int startPos, Vector2Int areaSize)
    {
        var areaInfo = new AreaOccupancyInfo
        {
            areaPosition = startPos,
            areaSize = areaSize,
            totalCells = areaSize.x * areaSize.y,
            occupiedCells = new List<CellOccupancyInfo>(),
            freeCells = new List<Vector2Int>(),
            canPlaceItem = true
        };

        // 边界检查
        if (startPos.x < 0 || startPos.y < 0 ||
            startPos.x + areaSize.x > width || startPos.y + areaSize.y > height)
        {
            areaInfo.canPlaceItem = false;
            areaInfo.errorMessage = "区域超出网格边界";
            return areaInfo;
        }

        // 检测每个格子的占用状态
        for (int x = startPos.x; x < startPos.x + areaSize.x; x++)
        {
            for (int y = startPos.y; y < startPos.y + areaSize.y; y++)
            {
                Vector2Int cellPos = new Vector2Int(x, y);

                if (occupiedCells.Contains(cellPos))
                {
                    // 找到占用此格子的物品
                    var occupyingItem = FindItemAtPosition(cellPos);

                    var cellInfo = new CellOccupancyInfo
                    {
                        cellPosition = cellPos,
                        isOccupied = true,
                        occupyingItemName = occupyingItem?.itemObject?.name ?? "未知物品",
                        occupyingItemInstanceID = occupyingItem != null &&
                            objectToInstanceID.ContainsKey(occupyingItem.itemObject) ?
                            objectToInstanceID[occupyingItem.itemObject] : ""
                    };

                    areaInfo.occupiedCells.Add(cellInfo);
                    areaInfo.canPlaceItem = false;
                }
                else
                {
                    areaInfo.freeCells.Add(cellPos);
                }
            }
        }

        areaInfo.occupiedCellsCount = areaInfo.occupiedCells.Count;
        areaInfo.freeCellsCount = areaInfo.freeCells.Count;

        return areaInfo;
    }

    /// <summary>
    /// 查找指定位置的物品
    /// </summary>
    /// <param name="position">网格位置</param>
    /// <returns>找到的物品信息，如果没有则返回null</returns>
    public virtual PlacedItem FindItemAtPosition(Vector2Int position)
    {
        foreach (var placedItem in placedItems)
        {
            if (position.x >= placedItem.position.x &&
                position.x < placedItem.position.x + placedItem.size.x &&
                position.y >= placedItem.position.y &&
                position.y < placedItem.position.y + placedItem.size.y)
            {
                return placedItem;
            }
        }
        return null;
    }

    /// <summary>
    /// 获取网格中所有可用的连续空间区域
    /// 用于智能物品放置建议
    /// </summary>
    /// <returns>可用空间区域列表</returns>
    public virtual List<AvailableSpaceInfo> GetAvailableSpaces()
    {
        var availableSpaces = new List<AvailableSpaceInfo>();
        bool[,] visited = new bool[width, height];

        // 使用洪水填充算法找到所有连续的空闲区域
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (!visited[x, y] && !occupiedCells.Contains(new Vector2Int(x, y)))
                {
                    var spaceInfo = FloodFillAvailableSpace(new Vector2Int(x, y), visited);
                    if (spaceInfo.totalCells > 0)
                    {
                        availableSpaces.Add(spaceInfo);
                    }
                }
            }
        }

        // 按可用空间大小排序
        availableSpaces.Sort((a, b) => b.totalCells.CompareTo(a.totalCells));

        return availableSpaces;
    }

    /// <summary>
    /// 洪水填充算法计算连续可用空间
    /// </summary>
    /// <param name="startPos">起始位置</param>
    /// <param name="visited">访问标记数组</param>
    /// <returns>可用空间信息</returns>
    private AvailableSpaceInfo FloodFillAvailableSpace(Vector2Int startPos, bool[,] visited)
    {
        var spaceInfo = new AvailableSpaceInfo
        {
            startPosition = startPos,
            availableCells = new List<Vector2Int>()
        };

        var queue = new Queue<Vector2Int>();
        queue.Enqueue(startPos);
        visited[startPos.x, startPos.y] = true;

        int minX = startPos.x, maxX = startPos.x;
        int minY = startPos.y, maxY = startPos.y;

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            spaceInfo.availableCells.Add(current);

            // 更新边界
            minX = Mathf.Min(minX, current.x);
            maxX = Mathf.Max(maxX, current.x);
            minY = Mathf.Min(minY, current.y);
            maxY = Mathf.Max(maxY, current.y);

            // 检查四个方向的相邻格子
            Vector2Int[] directions = {
                new Vector2Int(0, 1), new Vector2Int(0, -1),
                new Vector2Int(1, 0), new Vector2Int(-1, 0)
            };

            foreach (var dir in directions)
            {
                Vector2Int neighbor = current + dir;

                if (neighbor.x >= 0 && neighbor.x < width &&
                    neighbor.y >= 0 && neighbor.y < height &&
                    !visited[neighbor.x, neighbor.y] &&
                    !occupiedCells.Contains(neighbor))
                {
                    visited[neighbor.x, neighbor.y] = true;
                    queue.Enqueue(neighbor);
                }
            }
        }

        spaceInfo.totalCells = spaceInfo.availableCells.Count;
        spaceInfo.boundingBox = new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
        spaceInfo.maxItemSize = CalculateMaxItemSize(spaceInfo.availableCells);

        return spaceInfo;
    }

    /// <summary>
    /// 计算指定区域能容纳的最大物品尺寸
    /// </summary>
    /// <param name="availableCells">可用格子列表</param>
    /// <returns>最大物品尺寸</returns>
    private Vector2Int CalculateMaxItemSize(List<Vector2Int> availableCells)
    {
        if (availableCells.Count == 0) return Vector2Int.zero;

        // 创建临时占用矩阵
        bool[,] tempOccupancy = new bool[width, height];
        foreach (var cell in availableCells)
        {
            tempOccupancy[cell.x, cell.y] = true;
        }

        Vector2Int maxSize = Vector2Int.zero;

        // 对每个可用格子，尝试找到以它为起点的最大矩形
        foreach (var startCell in availableCells)
        {
            for (int w = 1; startCell.x + w <= width; w++)
            {
                for (int h = 1; startCell.y + h <= height; h++)
                {
                    bool canFit = true;

                    // 检查这个尺寸的矩形是否完全在可用区域内
                    for (int x = startCell.x; x < startCell.x + w && canFit; x++)
                    {
                        for (int y = startCell.y; y < startCell.y + h && canFit; y++)
                        {
                            if (!tempOccupancy[x, y])
                            {
                                canFit = false;
                            }
                        }
                    }

                    if (canFit && w * h > maxSize.x * maxSize.y)
                    {
                        maxSize = new Vector2Int(w, h);
                    }
                }
            }
        }

        return maxSize;
    }

    /// <summary>
    /// 获取物品数据的资源路径
    /// </summary>
    /// <param name="itemData">物品数据</param>
    /// <returns>资源路径</returns>
    private string GetItemDataPath(InventorySystemItemDataSO itemData)
    {
        if (itemData == null) return "";

#if UNITY_EDITOR
        string assetPath = UnityEditor.AssetDatabase.GetAssetPath(itemData);
        if (!string.IsNullOrEmpty(assetPath) && assetPath.StartsWith("Assets/Resources/"))
        {
            string resourcePath = assetPath.Substring("Assets/Resources/".Length);
            if (resourcePath.EndsWith(".asset"))
            {
                resourcePath = resourcePath.Substring(0, resourcePath.Length - ".asset".Length);
            }
            return resourcePath;
        }
#endif
        return itemData.name;
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


    // 支持旋转的CanPlaceItem重载
    public virtual bool CanPlaceItem(Vector2Int position, Vector2Int size, GameObject excludeItem)
    {
        // 边界检查
        if (position.x < 0 || position.y < 0 ||
            position.x + size.x > width || position.y + size.y > height)
        {
            return false;
        }

        // 检查占用状态，排除指定物品
        for (int x = position.x; x < position.x + size.x; x++)
        {
            for (int y = position.y; y < position.y + size.y; y++)
            {
                if (occupiedCells.Contains(new Vector2Int(x, y)))
                {
                    // 检查是否是被排除的物品占用
                    PlacedItem occupyingItem = placedItems.Find(item =>
                        x >= item.position.x && x < item.position.x + item.size.x &&
                        y >= item.position.y && y < item.position.y + item.size.y);

                    if (occupyingItem != null && occupyingItem.itemObject != excludeItem)
                    {
                        return false;
                    }
                }
            }
        }

        return true;
    }

    // ==================== ISaveable接口实现 ====================

    [System.Serializable]
    public class BaseItemGridSaveData
    {
        public string gridID;                    // 网格唯一标识ID
        public int saveVersion;                  // 保存数据版本
        public int gridWidth;                    // 网格宽度
        public int gridHeight;                   // 网格高度
        public PlacedItemSaveData[] placedItems; // 已放置物品的保存数据
        public string lastModified;             // 最后修改时间
        public bool isModified;                  // 是否已修改

        [System.Serializable]
        public class PlacedItemSaveData
        {
            public string itemInstanceID;       // 物品实例ID
            public Vector2Int position;         // 物品在网格中的位置
            public Vector2Int size;             // 物品尺寸
            public string itemDataPath;         // 物品数据资源路径
        }
    }

    [Header("保存系统设置")]
    [SerializeField] protected string gridID = "";           // 网格唯一标识ID
    [SerializeField] protected int saveVersion = 1;         // 保存数据版本
    [SerializeField] protected bool isModified = false;     // 是否已修改标记
    [SerializeField] protected string lastModified = "";    // 最后修改时间

    // 物品实例ID管理
    protected Dictionary<string, GameObject> itemInstanceMap = new Dictionary<string, GameObject>();
    protected Dictionary<GameObject, string> objectToInstanceID = new Dictionary<GameObject, string>();

    /// <summary>
    /// 获取网格的唯一标识ID
    /// </summary>
    /// <returns>网格ID字符串</returns>
    public virtual string GetGridID()
    {
        if (string.IsNullOrEmpty(gridID))
        {
            GenerateNewSaveID();
        }
        return gridID;
    }

    /// <summary>
    /// 设置网格的唯一标识ID
    /// </summary>
    /// <param name="id">新的网格ID</param>
    public virtual void SetGridID(string id)
    {
        if (!string.IsNullOrEmpty(id) && id != gridID)
        {
            gridID = id;
            MarkAsModified();
        }
    }

    /// <summary>
    /// 获取对象的唯一标识ID（ISaveable接口实现）
    /// </summary>
    /// <returns>对象的唯一ID字符串</returns>
    public virtual string GetSaveID()
    {
        return GetGridID();
    }

    /// <summary>
    /// 设置对象的唯一标识ID（ISaveable接口实现）
    /// </summary>
    /// <param name="id">新的ID字符串</param>
    public virtual void SetSaveID(string id)
    {
        SetGridID(id);
    }

    /// <summary>
    /// 生成新的唯一标识ID
    /// </summary>
    public virtual void GenerateNewSaveID()
    {
        gridID = "Grid_" + System.Guid.NewGuid().ToString("N")[..8] + "_" + GetInstanceID();
        MarkAsModified();
    }

    /// <summary>
    /// 验证保存ID是否有效
    /// </summary>
    /// <returns>ID是否有效</returns>
    public virtual bool IsSaveIDValid()
    {
        return !string.IsNullOrEmpty(gridID) && gridID.Length >= 8;
    }

    /// <summary>
    /// 序列化网格数据为JSON字符串
    /// </summary>
    /// <returns>序列化后的JSON字符串</returns>
    public virtual string SerializeToJson()
    {
        try
        {
            BaseItemGridSaveData saveData = GetSaveData();
            return JsonUtility.ToJson(saveData, true);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"网格数据序列化失败: {ex.Message}");
            return "";
        }
    }

    /// <summary>
    /// 从JSON字符串反序列化网格数据
    /// </summary>
    /// <param name="jsonData">JSON数据字符串</param>
    /// <returns>反序列化是否成功</returns>
    public virtual bool DeserializeFromJson(string jsonData)
    {
        try
        {
            if (string.IsNullOrEmpty(jsonData))
            {
                Debug.LogWarning("尝试反序列化空的JSON数据");
                return false;
            }

            BaseItemGridSaveData saveData = JsonUtility.FromJson<BaseItemGridSaveData>(jsonData);
            return LoadSaveData(saveData);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"网格数据反序列化失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 标记网格为已修改状态
    /// </summary>
    public virtual void MarkAsModified()
    {
        isModified = true;
        UpdateLastModified();
    }

    /// <summary>
    /// 重置修改标记
    /// </summary>
    public virtual void ResetModifiedFlag()
    {
        isModified = false;
    }

    /// <summary>
    /// 检查网格是否已被修改
    /// </summary>
    /// <returns>是否已修改</returns>
    public virtual bool IsModified()
    {
        return isModified;
    }

    /// <summary>
    /// 验证网格数据的完整性
    /// </summary>
    /// <returns>数据是否有效</returns>
    public virtual bool ValidateData()
    {
        // 检查基本属性
        if (width <= 0 || height <= 0)
        {
            Debug.LogError("网格尺寸无效");
            return false;
        }

        // 检查网格ID
        if (!IsSaveIDValid())
        {
            Debug.LogError("网格ID无效");
            return false;
        }

        // 检查已放置物品的有效性
        foreach (PlacedItem placedItem in placedItems)
        {
            if (placedItem.itemObject == null)
            {
                Debug.LogError("发现无效的已放置物品");
                return false;
            }

            // 检查位置是否在网格范围内
            if (placedItem.position.x < 0 || placedItem.position.y < 0 ||
                placedItem.position.x + placedItem.size.x > width ||
                placedItem.position.y + placedItem.size.y > height)
            {
                Debug.LogError($"物品位置超出网格范围: {placedItem.position}");
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 获取网格的最后修改时间
    /// </summary>
    /// <returns>最后修改时间字符串</returns>
    public virtual string GetLastModified()
    {
        return lastModified;
    }

    /// <summary>
    /// 更新最后修改时间为当前时间
    /// </summary>
    public virtual void UpdateLastModified()
    {
        lastModified = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }

    /// <summary>
    /// 获取网格的保存数据
    /// </summary>
    /// <returns>网格保存数据对象</returns>
    public virtual BaseItemGridSaveData GetSaveData()
    {
        BaseItemGridSaveData saveData = new BaseItemGridSaveData
        {
            gridID = GetGridID(),
            saveVersion = saveVersion,
            gridWidth = width,
            gridHeight = height,
            lastModified = GetLastModified(),
            isModified = IsModified()
        };

        // 保存已放置物品的数据
        List<BaseItemGridSaveData.PlacedItemSaveData> itemSaveDataList = new List<BaseItemGridSaveData.PlacedItemSaveData>();

        foreach (PlacedItem placedItem in placedItems)
        {
            if (placedItem.itemObject != null)
            {
                string instanceID = GetItemInstanceID(placedItem.itemObject);
                string itemDataPath = "";

                // 尝试获取物品数据路径
                var inventoryItem = placedItem.itemObject.GetComponent<InventorySystemItem>();
                if (inventoryItem != null && inventoryItem.Data != null)
                {
#if UNITY_EDITOR
                    itemDataPath = UnityEditor.AssetDatabase.GetAssetPath(inventoryItem.Data);
#endif
                }

                BaseItemGridSaveData.PlacedItemSaveData itemSaveData = new BaseItemGridSaveData.PlacedItemSaveData
                {
                    itemInstanceID = instanceID,
                    position = placedItem.position,
                    size = placedItem.size,
                    itemDataPath = itemDataPath
                };

                itemSaveDataList.Add(itemSaveData);
            }
        }

        saveData.placedItems = itemSaveDataList.ToArray();
        return saveData;
    }

    /// <summary>
    /// 从保存数据加载网格状态
    /// </summary>
    /// <param name="saveData">保存数据对象</param>
    /// <returns>加载是否成功</returns>
    public virtual bool LoadSaveData(BaseItemGridSaveData saveData)
    {
        try
        {
            if (saveData == null)
            {
                Debug.LogError("保存数据为空");
                return false;
            }

            // 验证保存数据版本
            if (saveData.saveVersion > saveVersion)
            {
                Debug.LogWarning($"保存数据版本({saveData.saveVersion})高于当前版本({saveVersion})，可能存在兼容性问题");
            }

            // 加载基本属性
            gridID = saveData.gridID;
            lastModified = saveData.lastModified;
            isModified = saveData.isModified;

            // 如果网格尺寸发生变化，重新初始化
            if (width != saveData.gridWidth || height != saveData.gridHeight)
            {
                width = saveData.gridWidth;
                height = saveData.gridHeight;
                InitializeGridArrays();
            }

            // 清空当前网格
            ClearGrid();

            // 使用物品恢复系统恢复已放置的物品
            if (saveData.placedItems != null && saveData.placedItems.Length > 0)
            {
                // 确保物品恢复系统已初始化
                if (ItemRestorationSystem.Instance == null)
                {
                    Debug.LogWarning("物品恢复系统未初始化，尝试初始化...");
                    ItemRestorationSystem.Instance.InitializeSystem();
                }

                int restoredCount = 0;
                int failedCount = 0;

                foreach (var itemSaveData in saveData.placedItems)
                {
                    // 构建物品保存数据
                    ItemSaveData itemData = new ItemSaveData
                    {
                        instanceID = itemSaveData.itemInstanceID,
                        gridPosition = itemSaveData.position,
                        itemDataPath = itemSaveData.itemDataPath,
                        isDraggable = true, // 默认可拖拽
                        instanceDataJson = "" // 实例数据将在恢复后单独处理
                    };

                    // 使用物品恢复系统恢复物品
                    GameObject restoredItem = ItemRestorationSystem.Instance.RestoreItem(itemData, this, itemSaveData.position);

                    if (restoredItem != null)
                    {
                        restoredCount++;
                        Debug.Log($"成功恢复物品: ID={itemSaveData.itemInstanceID}, 位置={itemSaveData.position}");
                    }
                    else
                    {
                        failedCount++;
                        Debug.LogWarning($"恢复物品失败: ID={itemSaveData.itemInstanceID}, 位置={itemSaveData.position}");
                    }
                }

                Debug.Log($"物品恢复完成: 成功={restoredCount}, 失败={failedCount}");
            }

            Debug.Log($"网格数据加载成功: {gridID}");
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"网格数据加载失败: {ex.Message}");
            return false;
        }
    }

    // ==================== 物品实例ID管理功能 ====================

    /// <summary>
    /// 为物品对象分配实例ID
    /// </summary>
    /// <param name="itemObject">物品对象</param>
    /// <returns>分配的实例ID</returns>
    public virtual string AllocateItemInstanceID(GameObject itemObject)
    {
        if (itemObject == null)
        {
            Debug.LogError("尝试为空物品对象分配实例ID");
            return "";
        }

        // 检查是否已经有实例ID
        if (objectToInstanceID.ContainsKey(itemObject))
        {
            return objectToInstanceID[itemObject];
        }

        // 生成新的实例ID
        string instanceID = "Item_" + System.Guid.NewGuid().ToString("N")[..8] + "_" + itemObject.GetInstanceID();

        // 注册实例ID映射
        itemInstanceMap[instanceID] = itemObject;
        objectToInstanceID[itemObject] = instanceID;

        MarkAsModified();
        return instanceID;
    }

    /// <summary>
    /// 获取物品对象的实例ID
    /// </summary>
    /// <param name="itemObject">物品对象</param>
    /// <returns>实例ID，如果不存在则返回空字符串</returns>
    public virtual string GetItemInstanceID(GameObject itemObject)
    {
        if (itemObject == null)
        {
            return "";
        }

        if (objectToInstanceID.ContainsKey(itemObject))
        {
            return objectToInstanceID[itemObject];
        }

        // 如果没有实例ID，自动分配一个
        return AllocateItemInstanceID(itemObject);
    }

    /// <summary>
    /// 根据实例ID获取物品对象
    /// </summary>
    /// <param name="instanceID">实例ID</param>
    /// <returns>物品对象，如果不存在则返回null</returns>
    public virtual GameObject GetItemByInstanceID(string instanceID)
    {
        if (string.IsNullOrEmpty(instanceID))
        {
            return null;
        }

        if (itemInstanceMap.ContainsKey(instanceID))
        {
            return itemInstanceMap[instanceID];
        }

        return null;
    }

    /// <summary>
    /// 移除物品的实例ID映射
    /// </summary>
    /// <param name="itemObject">物品对象</param>
    public virtual void RemoveItemInstanceID(GameObject itemObject)
    {
        if (itemObject == null)
        {
            return;
        }

        if (objectToInstanceID.ContainsKey(itemObject))
        {
            string instanceID = objectToInstanceID[itemObject];
            itemInstanceMap.Remove(instanceID);
            objectToInstanceID.Remove(itemObject);
            MarkAsModified();
        }
    }

    /// <summary>
    /// 验证实例ID是否有效
    /// </summary>
    /// <param name="instanceID">实例ID</param>
    /// <returns>是否有效</returns>
    public virtual bool IsItemInstanceIDValid(string instanceID)
    {
        return !string.IsNullOrEmpty(instanceID) && instanceID.StartsWith("Item_") && instanceID.Length >= 12;
    }

    /// <summary>
    /// 获取所有已注册的物品实例ID
    /// </summary>
    /// <returns>实例ID数组</returns>
    public virtual string[] GetAllItemInstanceIDs()
    {
        return itemInstanceMap.Keys.ToArray();
    }

    /// <summary>
    /// 清理无效的实例ID映射
    /// </summary>
    public virtual void CleanupInvalidInstanceIDs()
    {
        List<string> invalidIDs = new List<string>();

        foreach (var kvp in itemInstanceMap)
        {
            if (kvp.Value == null)
            {
                invalidIDs.Add(kvp.Key);
            }
        }

        foreach (string invalidID in invalidIDs)
        {
            itemInstanceMap.Remove(invalidID);
        }

        // 清理反向映射中的无效条目
        List<GameObject> invalidObjects = new List<GameObject>();
        foreach (var kvp in objectToInstanceID)
        {
            if (kvp.Key == null)
            {
                invalidObjects.Add(kvp.Key);
            }
        }

        foreach (GameObject invalidObject in invalidObjects)
        {
            objectToInstanceID.Remove(invalidObject);
        }

        if (invalidIDs.Count > 0 || invalidObjects.Count > 0)
        {
            MarkAsModified();
            Debug.Log($"清理了 {invalidIDs.Count} 个无效实例ID映射");
        }
    }

    /// <summary>
    /// 扩展PlaceItem方法以支持实例ID管理
    /// </summary>
    public virtual void PlaceItemWithInstanceID(GameObject itemObject, Vector2Int position, Vector2Int size)
    {
        // 调用原有PlaceItem方法
        PlaceItem(itemObject, position, size);

        // 为物品分配实例ID
        AllocateItemInstanceID(itemObject);
    }

    /// <summary>
    /// 扩展RemoveItem方法以支持实例ID管理
    /// </summary>
    public virtual void RemoveItemWithInstanceID(GameObject item)
    {
        // 移除实例ID映射
        RemoveItemInstanceID(item);

        // 调用原有RemoveItem方法
        RemoveItem(item);
    }

    /// <summary>
    /// 初始化保存系统
    /// </summary>
    protected virtual void InitializeSaveSystem()
    {
        // 确保有有效的网格ID
        if (!IsSaveIDValid())
        {
            GenerateNewSaveID();
        }

        // 初始化实例ID映射字典
        if (itemInstanceMap == null)
        {
            itemInstanceMap = new Dictionary<string, GameObject>();
        }

        if (objectToInstanceID == null)
        {
            objectToInstanceID = new Dictionary<GameObject, string>();
        }

        // 为现有物品分配实例ID
        foreach (PlacedItem placedItem in placedItems)
        {
            if (placedItem.itemObject != null)
            {
                AllocateItemInstanceID(placedItem.itemObject);
            }
        }

        UpdateLastModified();
        Debug.Log($"网格保存系统初始化完成: {GetGridID()}");
    }

}