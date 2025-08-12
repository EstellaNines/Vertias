using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class BackpackItemGrid : MonoBehaviour, IDropHandler
{
    [Header("背包网格系统参数设置")]
    [SerializeField] private GridConfig gridConfig;
    [SerializeField][FieldLabel("默认网格宽度")] private int defaultWidth = 1;
    [SerializeField][FieldLabel("默认网格高度")] private int defaultHeight = 1;
    
    // 当前装备的背包数据对象（动态设置）
    private InventorySystemItemDataSO currentBackpackData;
    private int width;
    private int height;
    
    // 网格占用状态
    private bool[,] gridOccupancy;
    private List<PlacedItem> placedItems = new List<PlacedItem>();

    RectTransform rectTransform;
    Canvas canvas;

    // 添加标记来处理延迟更新
    private bool needsUpdate = false;
    private int pendingWidth;
    private int pendingHeight;

    // 防止无限循环的标记
    private bool isUpdatingFromConfig = false;

    [System.Serializable]
    public class PlacedItem
    {
        public GameObject itemObject;
        public Vector2Int position;
        public Vector2Int size;
    }

    private void Awake()
    {
        // 在Awake中初始化rectTransform，确保在OnValidate之前完成
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }

        // 加载默认GridConfig如果没有设置
        LoadDefaultGridConfig();
        
        // 从背包数据加载配置
        LoadFromBackpackData();
    }

    private void Start()
    {
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }

        // 确保在运行时也从背包数据加载最新配置
        LoadFromBackpackData();

        if (Application.isPlaying)
        {
            canvas = FindObjectOfType<Canvas>();
            // 使用从背包数据加载的参数初始化网格占用状态
            gridOccupancy = new bool[width, height];
        }

        Init(width, height);
    }

    // 加载默认GridConfig
    private void LoadDefaultGridConfig()
    {
        if (gridConfig == null)
        {
            // 尝试加载默认的GridConfig
            gridConfig = Resources.Load<GridConfig>("DefaultGridConfig");
            if (gridConfig == null)
            {
                // 如果Resources中没有，尝试从指定路径加载
                string defaultConfigPath = "Assets/InventorySystem/Database/网格系统参数GridSystemSO/DefaultGridConfig.asset";
#if UNITY_EDITOR
                gridConfig = UnityEditor.AssetDatabase.LoadAssetAtPath<GridConfig>(defaultConfigPath);
#endif
            }
        }
    }

    // 修改OnValidate方法，避免无限循环
    private void OnValidate()
    {
        // 防止无限循环
        if (isUpdatingFromConfig) return;

        // 加载默认GridConfig
        LoadDefaultGridConfig();
        
        // 从背包数据更新尺寸
        LoadFromBackpackData();

        // 确保参数在合理范围内
        width = Mathf.Clamp(width, 1, 50);
        height = Mathf.Clamp(height, 1, 50);

        // 标记需要更新，而不是直接调用Init
        needsUpdate = true;
        pendingWidth = width;
        pendingHeight = height;

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            // 使用EditorApplication.delayCall来延迟执行更新
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

                // 强制刷新Scene视图
                UnityEditor.SceneView.RepaintAll();
            }
        }
    }
#endif

    private void Update()
    {
        // 处理运行时的延迟更新
        if (needsUpdate && Application.isPlaying)
        {
            Init(pendingWidth, pendingHeight);
            needsUpdate = false;
        }

        if (Application.isPlaying && Input.GetMouseButtonDown(0))
        {
            // 获取当前鼠标位置在网格中的格子坐标，并打印到控制台
            Debug.Log(GetTileGridPosition(Input.mousePosition));
        }
    }

    // 从背包数据加载配置
    private void LoadFromBackpackData()
    {
        // 如果有当前装备的背包数据，使用它；否则使用默认值
        if (currentBackpackData != null && !isUpdatingFromConfig)
        {
            isUpdatingFromConfig = true;
            width = currentBackpackData.CellH;
            height = currentBackpackData.CellV;
            isUpdatingFromConfig = false;
        }
    }
    
    // 获取当前装备的背包数据
    public InventorySystemItemDataSO GetCurrentBackpackData()
    {
        return currentBackpackData;
    }
    
    // 设置背包数据并更新网格
    public void SetBackpackData(InventorySystemItemDataSO data)
    {
        currentBackpackData = data;  // 修改：使用currentBackpackData而不是backpackData
        LoadFromBackpackData();

        // 重新初始化网格占用状态
        if (Application.isPlaying)
        {
            gridOccupancy = new bool[width, height];
            // 清空已放置物品列表
            placedItems.Clear();
        }

        // 立即更新网格尺寸
        Init(width, height);
    }

    // 初始化网格
    public void Init(int width, int height)
    {
        // 确保rectTransform存在
        if (rectTransform == null) return;

        float cellSize = gridConfig != null ? gridConfig.cellSize : 64f;

        Vector2 size = new Vector2(
            width * cellSize, // 宽度
            height * cellSize // 高度
        );
        rectTransform.sizeDelta = size;
    }

    // 根据鼠标位置计算在格子中的位置
    public Vector2Int GetTileGridPosition(Vector2 screenMousePos)
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

        // 本地坐标 → 网格坐标
        int x = Mathf.FloorToInt(localPoint.x / cellSize);
        int y = Mathf.FloorToInt(-localPoint.y / cellSize);

        x = Mathf.Clamp(x, 0, width - 1);
        y = Mathf.Clamp(y, 0, height - 1);
        return new Vector2Int(x, y);
    }

    // 实现IDropHandler接口
    public void OnDrop(PointerEventData eventData)
    {
        var dropped = eventData.pointerDrag;
        if (dropped == null) return;

        var draggable = dropped.GetComponent<DraggableItem>();
        if (draggable == null) return;

        var item = draggable.GetComponent<InventorySystemItem>();
        if (item == null) return;

        // 计算放置位置
        Vector2Int dropPosition = GetTileGridPosition(eventData.position);
        Vector2Int itemSize = new Vector2Int(item.Data.width, item.Data.height);

        // 检查是否可以放置
        if (CanPlaceItem(dropPosition, itemSize))
        {
            // 放置物品
            PlaceItem(dropped, dropPosition, itemSize);
        }
        // 如果不能放置，DraggableItem会自动处理返回原位置
    }

    // 检查是否可以在指定位置放置物品
    public bool CanPlaceItem(Vector2Int position, Vector2Int size)
    {
        // 检查边界
        if (position.x < 0 || position.y < 0 ||
            position.x + size.x > width || position.y + size.y > height)
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

    // 放置物品到网格中
    private void PlaceItem(GameObject itemObject, Vector2Int position, Vector2Int size)
    {
        RectTransform itemRect = itemObject.GetComponent<RectTransform>();
        if (itemRect == null) return;

        float cellSize = gridConfig != null ? gridConfig.cellSize : 64f;

        // 确保物品使用左上角锚点
        itemRect.anchorMin = new Vector2(0, 1);
        itemRect.anchorMax = new Vector2(0, 1);
        itemRect.pivot = new Vector2(0, 1);

        // 计算物品应该放置的位置（基于左上角锚点）
        float itemWidth = size.x * cellSize;
        float itemHeight = size.y * cellSize;

        Vector2 itemPosition = new Vector2(
            position.x * cellSize,
            -position.y * cellSize
        );

        itemRect.anchoredPosition = itemPosition;

        // 标记网格占用
        MarkGridOccupied(position, size, true);

        // 记录放置的物品
        placedItems.Add(new PlacedItem
        {
            itemObject = itemObject,
            position = position,
            size = size
        });

        Debug.Log($"物品放置在背包网格位置: ({position.x}, {position.y}), UI位置: {itemPosition}, 物品尺寸: {size.x}x{size.y}");
    }

    // 标记网格占用状态
    private void MarkGridOccupied(Vector2Int position, Vector2Int size, bool occupied)
    {
        for (int x = position.x; x < position.x + size.x; x++)
        {
            for (int y = position.y; y < position.y + size.y; y++)
            {
                gridOccupancy[x, y] = occupied;
            }
        }
    }

    // 移除物品时调用
    public void RemoveItem(GameObject item)
    {
        PlacedItem placedItem = placedItems.Find(p => p.itemObject == item);
        if (placedItem != null)
        {
            // 清除网格占用
            MarkGridOccupied(placedItem.position, placedItem.size, false);

            // 从列表中移除
            placedItems.Remove(placedItem);
        }
    }

    // 获取网格尺寸（供外部使用）
    public Vector2Int GetGridSize()
    {
        return new Vector2Int(width, height);
    }

    // 获取网格占用状态（供外部使用）
    public bool[,] GetGridOccupancy()
    {
        return gridOccupancy;
    }

    // 获取单元格大小
    public float GetCellSize()
    {
        return gridConfig != null ? gridConfig.cellSize : 64f;
    }

    // 设置GridConfig
    public void SetGridConfig(GridConfig config)
    {
        gridConfig = config;
    }

    // 获取GridConfig
    public GridConfig GetGridConfig()
    {
        return gridConfig;
    }

    // 获取背包数据
    public InventorySystemItemDataSO GetBackpackData()
    {
        return currentBackpackData;  // 修改：使用currentBackpackData而不是backpackData
    }

    // 清空网格中的所有物品
    public void ClearGrid()
    {
        if (gridOccupancy != null)
        {
            // 清空占用状态
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    gridOccupancy[x, y] = false;
                }
            }
        }

        // 清空物品列表
        placedItems.Clear();
    }
}