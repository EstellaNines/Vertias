using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using InventorySystem;

// 可拖拽物品
public class DraggableItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("拖拽设置")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private GraphicRaycaster graphicRaycaster;
    [SerializeField] private CanvasGroup canvasGroup;

    private RectTransform rectTransform;
    private Vector2 originalPosition;
    private Transform originalParent;
    private ItemDataReader itemDataReader; // 物品数据读取器
    private Item item; // Item 组件
    private InventoryController inventoryController; // 背包控制器
    private float originalAlpha = 0.8f; // 用于记录原始透明度
    private ItemHighlight itemHighlight; // 高亮组件

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        itemDataReader = GetComponent<ItemDataReader>();
        item = GetComponent<Item>();

        // 如果没有CanvasGroup则自动添加
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        // 设置Canvas和GraphicRaycaster
        if (canvas == null)
        {
            canvas = GetComponentInParent<Canvas>();
        }

        if (graphicRaycaster == null)
        {
            graphicRaycaster = GetComponentInParent<GraphicRaycaster>();
        }

        // 获取InventoryController
        if (inventoryController == null)
        {
            inventoryController = FindObjectOfType<InventoryController>();
        }

        // 获取高亮组件
        itemHighlight = GetComponent<ItemHighlight>();
    }

    // 鼠标停留事件处理
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (itemHighlight != null)
        {
            itemHighlight.ShowHighlight();
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (itemHighlight != null)
        {
            itemHighlight.HideHighlight();
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // 拖拽开始时隐藏高亮
        if (itemHighlight != null)
        {
            itemHighlight.HideHighlight();
        }

        // 确保必要组件已经初始化
        if (item == null)
        {
            item = GetComponent<Item>();
        }

        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        if (canvas == null)
        {
            canvas = GetComponentInParent<Canvas>();
        }

        if (inventoryController == null)
        {
            inventoryController = FindObjectOfType<InventoryController>();
        }

        // 若仍然缺少关键组件则报错并返回
        if (item == null)
        {
            Debug.LogError($"DraggableItem: 物品 {gameObject.name} 缺少 Item 组件，无法拖拽");
            return;
        }

        if (rectTransform == null)
        {
            Debug.LogError($"DraggableItem: 物品 {gameObject.name} 缺少 RectTransform 组件，无法拖拽");
            return;
        }

        Debug.Log($"DraggableItem: 开始拖拽物品 {gameObject.name}");

        // 记录原始位置和父级
        originalPosition = rectTransform.anchoredPosition;
        originalParent = transform.parent;

        // 若物品在装备槽中，恢复原始大小
        EquipmentSlot equipmentSlot = originalParent?.GetComponent<EquipmentSlot>();
        if (equipmentSlot != null)
        {
            // 物品从装备槽位置移除时恢复原始大小
            RestoreItemOriginalSize();

            // 物品已从装备槽移除，恢复原始大小
        }

        // 若物品在网格中，从网格移除
        if (item != null && item.IsOnGrid())
        {
            ItemGrid currentGrid = item.OnGridReference;
            if (currentGrid != null)
            {
                currentGrid.PickUpItem(item.OnGridPosition.x, item.OnGridPosition.y);
                Debug.Log($"DraggableItem: 从 {currentGrid.name} 位置 ({item.OnGridPosition.x}, {item.OnGridPosition.y}) 拾取物品");
            }
            item.ResetGridState();
        }

        // 设置为半透明并禁用射线检测
        originalAlpha = canvasGroup.alpha; // 记录原始透明度
        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false;

        // 移动到Canvas最顶层
        if (canvas != null)
        {
            transform.SetParent(canvas.transform);
            transform.SetAsLastSibling();
        }

        // 通知InventoryController开始拖拽
        if (inventoryController != null && item != null)
        {
            inventoryController.SetSelectedItem(item);
            Debug.Log($"DraggableItem: 通知 InventoryController 选中物品 {item.name}");
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        // 跟随鼠标移动
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
        
        // 实时更新拖拽高亮（命中检测）
        if (inventoryController != null && item != null)
        {
            inventoryController.UpdateDragHighlight(item, Input.mousePosition);
        }
    }

    // 获取物品所在的格子
    public ItemGrid GetParentGrid()
    {
        // 先检查当前父节点
        if (transform.parent != null)
        {
            ItemGrid grid = transform.parent.GetComponent<ItemGrid>();
            if (grid != null)
            {
                return grid;
            }
        }

        // 如果当前父节点没有，则使用拖拽前的原始父节点
        if (originalParent != null)
        {
            ItemGrid grid = originalParent.GetComponent<ItemGrid>();
            if (grid != null)
            {
                return grid;
            }
        }

        // 若仍未找到则返回 null
        return null;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // 恢复透明度和交互逻辑
        canvasGroup.alpha = originalAlpha; // 恢复原始透明度，通常为 1.0f
        canvasGroup.blocksRaycasts = true;

        // 判断是否有有效的放置目标
        bool validDrop = false;

        // 优先检查背包网格
        if (inventoryController != null && inventoryController.selectedItemGrid != null)
        {
            ItemGrid targetGrid = inventoryController.selectedItemGrid;
            Vector2Int gridPosition = targetGrid.GetTileGridPosition(Input.mousePosition);

            // 使用新的命中检测方法验证放置位置
            if (targetGrid.CanPlaceItemAtPosition(gridPosition.x, gridPosition.y, item.GetWidth(), item.GetHeight(), item))
            {
                // 在目标网格中放置物品
                if (targetGrid.PlaceItem(item, gridPosition.x, gridPosition.y))
                {
                    validDrop = true;

                    // 设置物品的父级为网格
                    transform.SetParent(targetGrid.transform);

                    // 设置物品在网格中的位置
                    Vector2 tilePosition = targetGrid.CalculatePositionOnGrid(item, gridPosition.x, gridPosition.y);
                    rectTransform.localPosition = tilePosition;

                    // 按当前旋转刷新可视尺寸/角度（确保与网格尺寸一致）
                    item.AdjustVisualSizeForGrid();

                    // 更新物品的网格状态
                    item.SetGridState(targetGrid, gridPosition);

                    Debug.Log($"物品已放置在网格位置: ({gridPosition.x}, {gridPosition.y}), 实际坐标: {tilePosition}");
                }
            }
            else
            {
                // 输出放置失败原因
                if (!targetGrid.BoundryCheck(gridPosition.x, gridPosition.y, item.GetWidth(), item.GetHeight()))
                {
                    Debug.Log($"物品 {item.name} 在位置 ({gridPosition.x}, {gridPosition.y}) 超出边界，无法放置");
                }
                else if (targetGrid.HasOverlapConflict(gridPosition.x, gridPosition.y, item.GetWidth(), item.GetHeight(), item))
                {
                    Debug.Log($"物品 {item.name} 在位置 ({gridPosition.x}, {gridPosition.y}) 与其他物品重叠，无法放置");
                }
            }
        }

        // 如果网格放置失败，尝试装备槽
        if (!validDrop && eventData.pointerEnter != null)
        {
            EquipmentSlot equipmentSlot = eventData.pointerEnter.GetComponent<EquipmentSlot>();
            if (equipmentSlot != null)
            {
                // 检查装备槽是否能接收该物品
                ItemDataReader itemDataReader = GetItemDataReader();
                if (itemDataReader != null && itemDataReader.ItemData != null)
                {
                    // 手动触发装备槽 OnDrop 逻辑
                    equipmentSlot.OnDrop(eventData);
                    validDrop = true;
                }
            }
        }

        // 如果没有有效放置目标，则还原位置
        if (!validDrop)
        {
            transform.SetParent(originalParent);
            rectTransform.anchoredPosition = originalPosition;

            // 如果原父级是网格，还原网格状态
            if (item != null && originalParent != null)
            {
                ItemGrid originalGrid = originalParent.GetComponent<ItemGrid>();
                if (originalGrid != null)
                {
                    // 重新计算原始网格位置
                    Vector2Int originalGridPos = originalGrid.GetTileGridPosition(originalPosition);
                    if (originalGrid.PlaceItem(item, originalGridPos.x, originalGridPos.y))
                    {
                        item.SetGridState(originalGrid, originalGridPos);
                    }
                }
            }
        }

        // 清空 InventoryController 的选中物品
        if (inventoryController != null)
        {
            inventoryController.SetSelectedItem(null);
        }
    }

    // 还原物品到原始大小
    private void RestoreItemOriginalSize()
    {
        ItemSizeInfo sizeInfo = GetComponent<ItemSizeInfo>();
        if (sizeInfo != null)
        {
            // 还原本体原始尺寸
            rectTransform.sizeDelta = sizeInfo.originalSize;
            rectTransform.localScale = sizeInfo.originalScale;
            rectTransform.anchorMin = sizeInfo.originalAnchorMin;
            rectTransform.anchorMax = sizeInfo.originalAnchorMax;
            rectTransform.pivot = sizeInfo.originalPivot;
            rectTransform.anchoredPosition = sizeInfo.originalAnchoredPosition;

            // 还原子元素尺寸
            RestoreChildImagesSizes();
        }
    }

    // 还原子对象图片的原始大小
    private void RestoreChildImagesSizes()
    {
        ItemSizeInfo[] childSizeInfos = GetComponentsInChildren<ItemSizeInfo>();

        foreach (ItemSizeInfo sizeInfo in childSizeInfos)
        {
            if (sizeInfo.gameObject != gameObject) // 排除自身
            {
                RectTransform childRect = sizeInfo.GetComponent<RectTransform>();
                if (childRect != null)
                {
                    // 还原子对象的原始尺寸
                    childRect.sizeDelta = sizeInfo.originalSize;
                    childRect.localScale = sizeInfo.originalScale;
                    childRect.anchorMin = sizeInfo.originalAnchorMin;
                    childRect.anchorMax = sizeInfo.originalAnchorMax;
                    childRect.pivot = sizeInfo.originalPivot;
                    childRect.anchoredPosition = sizeInfo.originalAnchoredPosition;
                }
            }
        }
    }

    // 启用或禁用拖拽
    public void SetDragEnabled(bool enabled)
    {
        canvasGroup.blocksRaycasts = enabled;
    }

    // 获取物品数据读取器
    public ItemDataReader GetItemDataReader()
    {
        return itemDataReader;
    }

    // 获取 Item 组件
    public Item GetItem()
    {
        return item;
    }

    // 获取原始位置
    public Vector2 GetOriginalPosition()
    {
        return originalPosition;
    }

    // 获取原始父级
    public Transform GetOriginalParent()
    {
        return originalParent;
    }
}