using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // 添加UI命名空间

// 将 selectedItem 和 overlapItem 的类型改为 BaseItem
public class InventoryController : MonoBehaviour
{
    [SerializeField][FieldLabel("物品列表")] List<ItemData> items;
    [SerializeField][FieldLabel("物品预制体")] GameObject itemPrefab;
    [SerializeField][FieldLabel("物品工厂")] ItemFactory itemFactory; // 添加 ItemFactory 引用
    public ItemGrid selectedItemGrid;//操作的背包
    BaseItem selectedItem;//选中物品
    BaseItem overlapItem;//重叠物品
    Canvas canvas;
    InventoryHighlight inventoryHighlight;
    BaseItem itemToHighlight;//高亮显示物品
    Vector2Int oldPosition;

    // 添加索引变量用于按顺序添加物品
    private int currentItemIndex = 0;

    // 添加所有ItemGrid的引用
    private ItemGrid[] allItemGrids;

    // 添加用于管理拖拽物品图层的变量
    private Canvas dragItemCanvas;
    private GraphicRaycaster originalRaycaster;

    private void Start()
    {
        canvas = FindObjectOfType<Canvas>();
        inventoryHighlight = GetComponent<InventoryHighlight>();

        // 自动查找 ItemFactory（如果没有手动分配的话）
        if (itemFactory == null)
        {
            itemFactory = FindObjectOfType<ItemFactory>();
        }

        // 获取场景中所有的ItemGrid
        allItemGrids = FindObjectsOfType<ItemGrid>();

        // 创建专门用于拖拽物品的Canvas
        CreateDragCanvas();
    }

    // 创建拖拽物品专用的Canvas
    // 修改 CreateDragCanvas 方法
    private void CreateDragCanvas()
    {
        GameObject dragCanvasObj = new GameObject("DragItemCanvas");
        // 关键修改：不设置父对象，让它成为独立的顶层Canvas
        // dragCanvasObj.transform.SetParent(canvas.transform, false); // 删除这行
        
        dragItemCanvas = dragCanvasObj.AddComponent<Canvas>();
        dragItemCanvas.overrideSorting = true;
        dragItemCanvas.sortingOrder = 1000; // 现在这个值会生效
        dragItemCanvas.renderMode = RenderMode.ScreenSpaceOverlay; // 确保是屏幕空间覆盖模式
        
        // 添加GraphicRaycaster以支持UI交互
        dragCanvasObj.AddComponent<GraphicRaycaster>();
        
        // 设置RectTransform以覆盖整个屏幕
        RectTransform dragRect = dragCanvasObj.GetComponent<RectTransform>();
        dragRect.anchorMin = Vector2.zero;
        dragRect.anchorMax = Vector2.one;
        dragRect.offsetMin = Vector2.zero;
        dragRect.offsetMax = Vector2.zero;
    }

    private void Update()
    {
        //TODO: 方便测试，动态按顺序添加物品
        if (Input.GetKeyDown(KeyCode.Q))
        {
            CreateSequentialItem();
        }

        //TODO: 方便测试，动态随机添加物品（保留原方法）
        if (Input.GetKeyDown(KeyCode.R))
        {
            CreateRandomItem();
        }

        //物品跟随鼠标
        if (selectedItem) selectedItem.transform.position = Input.mousePosition;

        if (selectedItemGrid == null)
        {
            inventoryHighlight.Show(false);
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            // 获取当前鼠标位置在网格中的格子坐标，并打印到控制台
            Debug.Log(selectedItemGrid.GetTileGridPosition(Input.mousePosition));

            LeftMouseButtonPress();
        }

        //高亮显示
        HandleHighlight();
    }

    //按顺序添加物品
    private void CreateSequentialItem()
    {
        if (selectedItem) return;
        if (items.Count == 0) return;

        BaseItem item = itemFactory.CreateItem(items[currentItemIndex]);
        if (item != null)
        {
            selectedItem = item;
            SetItemToDragLayer(selectedItem);
            selectedItem.HideBackground();
            currentItemIndex = (currentItemIndex + 1) % items.Count;
        }
    }

    private void CreateRandomItem()
    {
        if (selectedItem) return;
        if (items.Count == 0) return;

        int index = UnityEngine.Random.Range(0, items.Count);
        BaseItem item = itemFactory.CreateItem(items[index]);
        if (item != null)
        {
            selectedItem = item;
            SetItemToDragLayer(selectedItem);
            selectedItem.HideBackground();
        }
    }

    // 将物品设置到拖拽图层
    private void SetItemToDragLayer(BaseItem item)
    {
        if (item != null && dragItemCanvas != null)
        {
            item.transform.SetParent(dragItemCanvas.transform, false);
        
            // 禁用拖拽物品的射线检测，让鼠标事件能够穿透
            DisableRaycastOnDraggedItem(item);
            
            // 确保物品在拖拽Canvas中显示在最前面
            item.transform.SetAsLastSibling();
        }
    }

    // 新增方法：禁用拖拽物品的射线检测
    private void DisableRaycastOnDraggedItem(BaseItem item)
    {
        // 禁用物品本身的射线检测
        Image itemImage = item.GetComponent<Image>();
        if (itemImage != null)
        {
            itemImage.raycastTarget = false;
        }
        
        // 禁用子对象的射线检测（如图标）
        Image[] childImages = item.GetComponentsInChildren<Image>();
        foreach (Image img in childImages)
        {
            img.raycastTarget = false;
        }
    }

    // 新增方法：恢复物品的射线检测
    private void EnableRaycastOnItem(BaseItem item)
    {
        // 恢复物品本身的射线检测
        Image itemImage = item.GetComponent<Image>();
        if (itemImage != null)
        {
            itemImage.raycastTarget = true;
        }
        
        // 恢复子对象的射线检测（如图标）
        Image[] childImages = item.GetComponentsInChildren<Image>();
        foreach (Image img in childImages)
        {
            img.raycastTarget = true;
        }
    }

    // 将物品从拖拽图层移除，恢复到正常图层
    private void RemoveItemFromDragLayer(BaseItem item)
    {
        if (item != null)
        {
            // 恢复射线检测
            EnableRaycastOnItem(item);
            
            // 移除添加的Canvas组件
            Canvas itemCanvas = item.GetComponent<Canvas>();
            if (itemCanvas != null)
            {
                DestroyImmediate(itemCanvas);
            }

            // 移除GraphicRaycaster组件
            GraphicRaycaster raycaster = item.GetComponent<GraphicRaycaster>();
            if (raycaster != null)
            {
                DestroyImmediate(raycaster);
            }
        }
    }

    //鼠标坐标转化为格子坐标 - 修改版本
    private Vector2Int GetTileGridPosition(ItemGrid targetGrid = null)
    {
        // 如果没有指定目标网格，使用当前选中的网格
        ItemGrid gridToUse = targetGrid ?? selectedItemGrid;

        if (gridToUse == null)
        {
            return Vector2Int.zero;
        }

        Vector2 position = Input.mousePosition;
        if (selectedItem != null)
        {
            position.x -= (selectedItem.itemData.width - 1) * ItemGrid.tileSizeWidth / 2;
            position.y += (selectedItem.itemData.height - 1) * ItemGrid.tileSizeHeight / 2;
        }
        Vector2Int tileGridPosition = gridToUse.GetTileGridPosition(position);
        return tileGridPosition;
    }

    //移动物品 - 修改版本
    void PlaceItem(Vector2Int tileGridPosition)
    {
        // 首先获取鼠标下方的网格
        ItemGrid targetGrid = GetItemGridUnderMouse();

        if (targetGrid == null)
        {
            Debug.LogWarning("没有找到可用的背包网格");
            return;
        }

        // 更新选中的网格
        selectedItemGrid = targetGrid;

        // 重新计算基于目标网格的坐标
        Vector2Int correctedPosition = GetTileGridPosition(targetGrid);

        bool complete = targetGrid.PlaceItem(selectedItem, correctedPosition.x, correctedPosition.y, ref overlapItem);
        if (complete)
        {
            // 新增：放下物品时显示背景
            if (selectedItem != null)
            {
                // 从拖拽图层移除
                RemoveItemFromDragLayer(selectedItem);
                selectedItem.ShowBackground();
            }

            selectedItem = null;

            //如果存在重叠物品
            if (overlapItem != null)
            {
                selectedItem = overlapItem;
                // 新增：重叠物品被拿起时隐藏背景并设置到拖拽图层
                if (selectedItem != null)
                {
                    SetItemToDragLayer(selectedItem);
                    selectedItem.HideBackground();
                }
                overlapItem = null;
            }
        }
    }

    //点击操作，选中物品 - 修改版本
    private void LeftMouseButtonPress()
    {
        // 首先获取鼠标下方的网格
        ItemGrid targetGrid = GetItemGridUnderMouse();

        if (targetGrid == null)
        {
            return;
        }

        // 更新选中的网格
        selectedItemGrid = targetGrid;

        // 基于目标网格计算坐标
        Vector2Int tileGridPosition = GetTileGridPosition(targetGrid);

        if (selectedItem == null)
        {
            //选中物品
            selectedItem = selectedItemGrid.PickUpItem(tileGridPosition.x, tileGridPosition.y);

            // 新增：拿起物品时隐藏背景并设置到拖拽图层
            if (selectedItem != null)
            {
                SetItemToDragLayer(selectedItem);
                selectedItem.HideBackground();
            }
        }
        else
        {
            // 移动物品
            PlaceItem(tileGridPosition);
        }
    }

    //高亮显示 - 修改版本
    private void HandleHighlight()
    {
        // 获取鼠标下方的网格
        ItemGrid targetGrid = GetItemGridUnderMouse();

        if (targetGrid == null)
        {
            inventoryHighlight.Show(false);
            return;
        }

        // 更新选中的网格
        selectedItemGrid = targetGrid;

        // 基于目标网格计算坐标
        Vector2Int positionOnGrid = GetTileGridPosition(targetGrid);

        //节约没必要的计算
        if (oldPosition == positionOnGrid) return;
        oldPosition = positionOnGrid;

        if (selectedItem == null)
        {
            // 添加边界检查，确保坐标在有效范围内
            if (positionOnGrid.x >= 0 && positionOnGrid.y >= 0 &&
                positionOnGrid.x < targetGrid.GridSizeWidth &&
                positionOnGrid.y < targetGrid.GridSizeHeight)
            {
                itemToHighlight = targetGrid.GetItem(positionOnGrid.x, positionOnGrid.y);
                if (itemToHighlight != null)
                {
                    inventoryHighlight.Show(true);
                    inventoryHighlight.SetSize(itemToHighlight);
                    inventoryHighlight.SetParent(targetGrid);
                    inventoryHighlight.SetPosition(targetGrid, itemToHighlight);
                }
                else
                {
                    inventoryHighlight.Show(false);
                }
            }
            else
            {
                // 鼠标在网格外，隐藏高亮
                inventoryHighlight.Show(false);
            }
        }
        else
        {
            inventoryHighlight.Show(targetGrid.BoundryCheck(
                    positionOnGrid.x,
                    positionOnGrid.y,
                    selectedItem.itemData.width,
                    selectedItem.itemData.height)
            );//防止显示跨界
            inventoryHighlight.SetSize(selectedItem);
            inventoryHighlight.SetParent(targetGrid);
            inventoryHighlight.SetPosition(targetGrid, selectedItem, positionOnGrid.x, positionOnGrid.y);
        }
    }

    // 新增方法：获取鼠标下方的ItemGrid
    private ItemGrid GetItemGridUnderMouse()
    {
        Vector2 mousePosition = Input.mousePosition;

        foreach (ItemGrid grid in allItemGrids)
        {
            RectTransform gridRect = grid.GetComponent<RectTransform>();
            if (gridRect != null && RectTransformUtility.RectangleContainsScreenPoint(gridRect, mousePosition, canvas.worldCamera))
            {
                return grid;
            }
        }

        return null;
    }

    // 在InventoryController类中添加这个方法
    public BaseItem GetSelectedItem()
    {
        return selectedItem;
    }

    // 添加一个方法来处理物品放回背包
    public bool TryReturnItemToGrid(BaseItem item)
    {
        if (selectedItemGrid != null)
        {
            return selectedItemGrid.TryPlaceItemSequentially(item);
        }
        return false;
    }

    // 新增方法：清除选中的物品
    public void ClearSelectedItem()
    {
        if (selectedItem != null)
        {
            // 从拖拽图层移除物品
            RemoveItemFromDragLayer(selectedItem);
            
            // 销毁物品对象
            Destroy(selectedItem.gameObject);
            
            // 清空选中状态
            selectedItem = null;
            
            Debug.Log("已清除选中物品");
        }
    }
}