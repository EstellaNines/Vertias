using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
            
            // 加载已放置的物品（这里只是恢复位置信息，实际物品对象需要外部系统处理）
            if (saveData.placedItems != null)
            {
                foreach (var itemSaveData in saveData.placedItems)
                {
                    // 这里只记录物品实例ID和位置信息
                    // 实际的物品对象恢复需要配合InventoryController等系统
                    Debug.Log($"需要恢复物品: ID={itemSaveData.itemInstanceID}, 位置={itemSaveData.position}, 尺寸={itemSaveData.size}");
                }
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