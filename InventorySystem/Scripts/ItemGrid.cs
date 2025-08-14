using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Unity.VisualScripting;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class ItemGrid : MonoBehaviour, IDropHandler
{
    [Header("网格系统基础设置")]
    [SerializeField] private GridConfig gridConfig;
    [SerializeField][FieldLabel("网格系统宽度格数")] private int width = 12;
    [SerializeField][FieldLabel("网格系统高度格数")] private int height = 20;

    // 网格占用状态
    private bool[,] gridOccupancy;
    private List<PlacedItem> placedItems = new List<PlacedItem>();

    RectTransform rectTransform;
    Canvas canvas;

    // 避免编辑器中的延迟更新
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
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }
        LoadFromGridConfig();
    }

    private void Start()
    {
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }

        LoadFromGridConfig();

        if (Application.isPlaying)
        {
            canvas = FindObjectOfType<Canvas>();
            gridOccupancy = new bool[width, height];
        }

        Init(width, height);
    }

    private void OnValidate()
    {
        if (isUpdatingFromConfig) return;

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
                SaveToGridConfigDelayed();
                UnityEditor.SceneView.RepaintAll();
            }
        }
    }

    private void SaveToGridConfigDelayed()
    {
        UnityEditor.EditorApplication.delayCall += () =>
        {
            SaveToGridConfig();
        };
    }
#endif

    private void Update()
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

    public void LoadFromGridConfig()
    {
        if (gridConfig != null && !isUpdatingFromConfig)
        {
            isUpdatingFromConfig = true;
            width = gridConfig.inventoryWidth;
            height = gridConfig.inventoryHeight;
            isUpdatingFromConfig = false;
        }
    }

    public void SaveToGridConfig()
    {
#if UNITY_EDITOR
        if (gridConfig != null && !isUpdatingFromConfig)
        {
            isUpdatingFromConfig = true;

            bool hasChanged = false;
            if (gridConfig.inventoryWidth != width)
            {
                gridConfig.inventoryWidth = width;
                hasChanged = true;
            }
            if (gridConfig.inventoryHeight != height)
            {
                gridConfig.inventoryHeight = height;
                hasChanged = true;
            }

            if (hasChanged)
            {
                EditorUtility.SetDirty(gridConfig);
                AssetDatabase.SaveAssets();
            }

            isUpdatingFromConfig = false;
        }
#endif
    }

    public void Init(int width, int height)
    {
        if (rectTransform == null) return;

        float cellSize = gridConfig != null ? gridConfig.cellSize : 64f;

        Vector2 size = new Vector2(
            width * cellSize,
            height * cellSize
        );
        rectTransform.sizeDelta = size;
    }

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

        int x = Mathf.FloorToInt(localPoint.x / cellSize);
        int y = Mathf.FloorToInt(-localPoint.y / cellSize);

        x = Mathf.Clamp(x, 0, width - 1);
        y = Mathf.Clamp(y, 0, height - 1);
        return new Vector2Int(x, y);
    }

    public void OnDrop(PointerEventData eventData)
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

    public bool CanPlaceItem(Vector2Int position, Vector2Int size)
    {
        if (position.x < 0 || position.y < 0 ||
            position.x + size.x > width || position.y + size.y > height)
        {
            return false;
        }

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

    private void PlaceItem(GameObject itemObject, Vector2Int position, Vector2Int size)
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

    public void RemoveItem(GameObject item)
    {
        PlacedItem placedItem = placedItems.Find(p => p.itemObject == item);
        if (placedItem != null)
        {
            MarkGridOccupied(placedItem.position, placedItem.size, false);
            placedItems.Remove(placedItem);
        }
    }

    public Vector2Int GetGridSize()
    {
        return new Vector2Int(width, height);
    }

    public bool[,] GetGridOccupancy()
    {
        return gridOccupancy;
    }

    public float GetCellSize()
    {
        return gridConfig != null ? gridConfig.cellSize : 64f;
    }

    public void SetGridConfig(GridConfig config)
    {
        gridConfig = config;
        LoadFromGridConfig();
    }

    public GridConfig GetGridConfig()
    {
        return gridConfig;
    }

    public void SyncToGridConfig()
    {
        SaveToGridConfig();
    }

    
    /// <summary>
    /// 按格子坐标获取物品
    /// </summary>
    /// <param name="x">X坐标</param>
    /// <param name="y">Y坐标</param>
    /// <returns>该位置的物品，如果没有则返回null</returns>
    public InventorySystemItem GetItem(int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height)
        {
            return null;
        }

        // 查找占据该位置的物品
        foreach (PlacedItem placedItem in placedItems)
        {
            // 检查该坐标是否在物品的占用范围内
            if (x >= placedItem.position.x && x < placedItem.position.x + placedItem.size.x &&
                y >= placedItem.position.y && y < placedItem.position.y + placedItem.size.y)
            {
                return placedItem.itemObject.GetComponent<InventorySystemItem>();
            }
        }

        return null;
    }

    /// <summary>
    /// 根据鼠标屏幕位置获取物品
    /// </summary>
    /// <param name="screenMousePos">鼠标屏幕位置</param>
    /// <returns>该位置的物品，如果没有则返回null</returns>
    public InventorySystemItem GetItemAtScreenPosition(Vector2 screenMousePos)
    {
        Vector2Int gridPos = GetTileGridPosition(screenMousePos);
        return GetItem(gridPos.x, gridPos.y);
    }

    /// <summary>
    /// 获取所有已放置的物品
    /// </summary>
    /// <returns>已放置物品列表</returns>
    public List<PlacedItem> GetPlacedItems()
    {
        return new List<PlacedItem>(placedItems);
    }
}
