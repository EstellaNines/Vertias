using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ItemGrid : MonoBehaviour, IDropHandler
{
    [Header("网格系统参数设置")]
    [SerializeField][FieldLabel("网格系统宽度容量")] private int width = 12;
    [SerializeField][FieldLabel("网格系统高度容量")] private int height = 20;
    const float cellsize = 80f;

    // 计算在格子中的位置
    Vector2 positionOnTheGrid = new Vector2();
    Vector2Int tileGridPosition = new Vector2Int();

    // 网格占用状态
    private bool[,] gridOccupancy;
    private List<PlacedItem> placedItems = new List<PlacedItem>();

    RectTransform rectTransform;
    Canvas canvas;

    [Header("测试用头盔预制体")]
    [SerializeField] private GameObject helmetPrefab; // 头盔预制体
    [SerializeField] private InventorySystemItemDataSO helmetData; // 头盔数据

    [System.Serializable]
    public class PlacedItem
    {
        public GameObject itemObject;
        public Vector2Int position;
        public Vector2Int size;
    }

    private void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = FindObjectOfType<Canvas>();

        // 初始化网格占用状态
        gridOccupancy = new bool[width, height];

        Init(width, height);
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // 获取当前鼠标位置在网格中的格子坐标，并打印到控制台
            Debug.Log(GetTileGridPosition(Input.mousePosition));
        }
    }

    // 初始化网格
    public void Init(int width, int height)
    {
        Vector2 size = new Vector2(
            width * cellsize, // 宽度
            height * cellsize // 高度
        );
        rectTransform.sizeDelta = size;
    }

    // 根据鼠标位置计算在格子中的位置
    public Vector2Int GetTileGridPosition(Vector2 screenMousePos)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform,
            screenMousePos,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
            out Vector2 localPoint);

        // 本地坐标 → 网格坐标
        int x = Mathf.FloorToInt(localPoint.x / cellsize);
        int y = Mathf.FloorToInt(-localPoint.y / cellsize);

        x = Mathf.Clamp(x, 0, width - 1);
        y = Mathf.Clamp(y, 0, height - 1);
        return new Vector2Int(x, y);
    }

    // 测试方法：在(0,0)位置生成头盔
    [ContextMenu("在(0,0)生成测试头盔")]
    public void SpawnTestHelmetAt00()
    {
        if (helmetPrefab == null)
        {
            Debug.LogError("头盔预制体未设置！请在Inspector中设置helmetPrefab");
            return;
        }

        if (helmetData == null)
        {
            Debug.LogError("头盔数据未设置！请在Inspector中设置helmetData");
            return;
        }

        Vector2Int spawnPosition = new Vector2Int(0, 0);
        Vector2Int itemSize = new Vector2Int(helmetData.width, helmetData.height);

        // 检查位置是否可用
        if (!CanPlaceItem(spawnPosition, itemSize))
        {
            Debug.LogWarning("位置(0,0)被占用或超出边界，无法生成头盔！");
            return;
        }

        // 实例化头盔
        GameObject helmet = Instantiate(helmetPrefab, transform);
        
        // 设置物品数据
        ItemDataHolder dataHolder = helmet.GetComponentInChildren<ItemDataHolder>();
        if (dataHolder != null)
        {
            dataHolder.SetItemData(helmetData);
        }
        else
        {
            Debug.LogError($"预制体 {helmetPrefab.name} 中没有找到 ItemDataHolder 组件！");
        }

        // 确保必要的组件存在
        EnsureRequiredComponents(helmet);

        // 放置物品到网格中
        PlaceItem(helmet, spawnPosition, itemSize);

        Debug.Log($"成功在位置(0,0)生成头盔: {helmetData.itemName}");
    }

    // 确保物品具有所有必要的组件
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
    // 放置物品到网格中
    private void PlaceItem(GameObject itemObject, Vector2Int position, Vector2Int size)
    {
        RectTransform itemRect = itemObject.GetComponent<RectTransform>();
        if (itemRect == null) return;
    
        // 确保物品使用左上角锚点
        itemRect.anchorMin = new Vector2(0, 1);
        itemRect.anchorMax = new Vector2(0, 1);
        itemRect.pivot = new Vector2(0, 1);
    
        // 计算物品应该放置的位置（基于左上角锚点）
        float itemWidth = size.x * cellsize;
        float itemHeight = size.y * cellsize;
        
        Vector2 itemPosition = new Vector2(
            position.x * cellsize,
            -position.y * cellsize
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
    
        Debug.Log($"物品放置在网格位置: ({position.x}, {position.y}), UI位置: {itemPosition}, 物品尺寸: {size.x}x{size.y}");
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
}
