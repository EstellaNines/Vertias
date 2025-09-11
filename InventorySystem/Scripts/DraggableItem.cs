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
    
    [Header("拖拽时物品设置")]
    [SerializeField] private Vector2 dragItemSize = new Vector2(192f, 192f); // 拖拽时物品大小

    private RectTransform rectTransform;
    private Vector2 originalPosition;
    private Transform originalParent;
    private ItemGrid originalGridRef; // 原始所在网格
    private Vector2Int originalGridPos; // 原始网格坐标
    private ItemDataReader itemDataReader; // 物品数据读取器
    private Item item; // Item 组件
    private InventoryController inventoryController; // 背包控制器
    private float originalAlpha = 0.8f; // 用于记录原始透明度
    private ItemHighlight itemHighlightComponent; // 高亮组件
    
    // 拖拽时的状态记录
    private Vector2 originalSize; // 原始大小
    private Vector3 originalScale; // 原始缩放
    private bool originalBackgroundActive; // 原始背景状态
    private GameObject itemBackground; // 物品背景对象引用
    
    // 子对象引用
    private RectTransform itemIcon; // 物品图标
    private RectTransform itemText; // 物品文字
    private RectTransform itemHighlight; // 物品高亮
    private Vector2 originalIconSize; // 图标原始大小
    private Vector2 originalTextSize; // 文字原始大小
    private Vector2 originalTextPosition; // 文字原始位置
    private Vector2 originalHighlightSize; // 高亮原始大小

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
        itemHighlightComponent = GetComponent<ItemHighlight>();
        
        // 获取物品背景对象引用
        Transform backgroundTransform = transform.Find("ItemBackground");
        if (backgroundTransform != null)
        {
            itemBackground = backgroundTransform.gameObject;
        }
        else
        {
            Debug.LogWarning($"物品 {gameObject.name} 未找到ItemBackground子对象");
        }
        
        // 获取物品图标引用
        Transform iconTransform = transform.Find("ItemIcon");
        if (iconTransform != null)
        {
            itemIcon = iconTransform.GetComponent<RectTransform>();
        }
        else
        {
            Debug.LogWarning($"物品 {gameObject.name} 未找到ItemIcon子对象");
        }
        
        // 获取物品文字引用
        Transform textTransform = transform.Find("ItemText");
        if (textTransform != null)
        {
            itemText = textTransform.GetComponent<RectTransform>();
        }
        else
        {
            Debug.LogWarning($"物品 {gameObject.name} 未找到ItemText子对象");
        }
        
        // 获取物品高亮引用
        Transform highlightTransform = transform.Find("ItemHighlight");
        if (highlightTransform != null)
        {
            itemHighlight = highlightTransform.GetComponent<RectTransform>();
        }
        else
        {
            Debug.LogWarning($"物品 {gameObject.name} 未找到ItemHighlight子对象");
        }
    }

    // 鼠标停留事件处理
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (itemHighlightComponent != null)
        {
            itemHighlightComponent.ShowHighlight();
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (itemHighlightComponent != null)
        {
            itemHighlightComponent.HideHighlight();
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // 拖拽开始时隐藏高亮
        if (itemHighlightComponent != null)
        {
            itemHighlightComponent.HideHighlight();
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



        // 记录原始位置、父级、网格与网格坐标
        originalPosition = rectTransform.anchoredPosition;
        originalParent = transform.parent;
        originalGridRef = null;
        originalGridPos = Vector2Int.zero;
        if (item != null && item.IsOnGrid())
        {
            originalGridRef = item.OnGridReference;
            originalGridPos = item.OnGridPosition;
        }
        
        // 记录原始状态
        originalSize = rectTransform.sizeDelta;
        originalScale = rectTransform.localScale;
        originalBackgroundActive = itemBackground != null ? itemBackground.activeSelf : false;
        
        // 记录子对象原始状态
        if (itemIcon != null)
        {
            originalIconSize = itemIcon.sizeDelta;
        }
        if (itemText != null)
        {
            originalTextSize = itemText.sizeDelta;
            originalTextPosition = itemText.anchoredPosition;
        }
        if (itemHighlight != null)
        {
            originalHighlightSize = itemHighlight.sizeDelta;
        }

        // 若物品在装备槽中，从装备槽移除
        if (TryRemoveFromEquipmentSlot())
        {
            Debug.Log($"DraggableItem: 从装备槽中移除物品 {item.name}");
        }

        // 若物品在网格中，从网格移除
        if (item != null && item.IsOnGrid())
        {
            ItemGrid currentGrid = item.OnGridReference;
            if (currentGrid != null)
            {
                currentGrid.PickUpItem(item.OnGridPosition.x, item.OnGridPosition.y);
            }
            item.ResetGridState();
        }
        
        // 恢复物品的真实视觉尺寸（不使用网格强制的尺寸）
        if (item != null)
        {
            item.RestoreRealVisualSize();
        }

        // 设置为半透明并禁用射线检测
        originalAlpha = canvasGroup.alpha; // 记录原始透明度
        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false;
        
        // 应用拖拽时的物品设置
        ApplyDragItemSettings();

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

    /// <summary>
    /// 尝试从装备槽中移除物品
    /// </summary>
    /// <returns>是否成功移除</returns>
    private bool TryRemoveFromEquipmentSlot()
    {
        // 检查物品是否在装备槽中
        InventorySystem.EquipmentSlot currentEquipmentSlot = transform.GetComponentInParent<InventorySystem.EquipmentSlot>();
        if (currentEquipmentSlot == null) return false;

        // 检查这个装备槽是否确实装备了这个物品
        if (currentEquipmentSlot.CurrentEquippedItem?.gameObject != gameObject) return false;

        // 从装备槽中卸下物品
        ItemDataReader unequippedItem = currentEquipmentSlot.UnequipItem();
        return unequippedItem != null;
    }

    /// <summary>
    /// 尝试将物品放置到装备槽中
    /// </summary>
    /// <param name="eventData">拖拽事件数据</param>
    /// <returns>是否成功放置</returns>
    private bool TryPlaceInEquipmentSlot(PointerEventData eventData)
    {
        // 检查鼠标下方是否有装备槽
        GameObject targetObject = eventData.pointerCurrentRaycast.gameObject;
        if (targetObject == null) return false;

        // 向上查找装备槽组件
        InventorySystem.EquipmentSlot equipmentSlot = targetObject.GetComponentInParent<InventorySystem.EquipmentSlot>();
        if (equipmentSlot == null) return false;

        // 检查物品是否有ItemDataReader组件
        ItemDataReader itemDataReader = GetComponent<ItemDataReader>();
        if (itemDataReader == null) return false;

        // 尝试装备物品
        bool equipSuccess = equipmentSlot.EquipItem(itemDataReader);
        
        if (equipSuccess)
        {
            Debug.Log($"DraggableItem: 成功将物品 {item.name} 装备到装备槽 {equipmentSlot.SlotType}");
            return true;
        }
        else
        {
            Debug.LogWarning($"DraggableItem: 无法将物品 {item.name} 装备到装备槽 {equipmentSlot.SlotType}");
            return false;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // 恢复透明度和交互逻辑
        canvasGroup.alpha = originalAlpha; // 恢复原始透明度，通常为 1.0f
        canvasGroup.blocksRaycasts = true;
        
        // 恢复物品原始状态
        RestoreItemOriginalState();

        // 判断是否有有效的放置目标
        bool validDrop = false;

        // 优先检查背包网格
        if (inventoryController != null && inventoryController.selectedItemGrid != null)
        {
            ItemGrid targetGrid = inventoryController.selectedItemGrid;
            Vector2Int gridPosition = targetGrid.GetTileGridPosition(Input.mousePosition);

            // 先处理“堆叠合并”场景：命中格子已有同类未满堆叠的物品
            Item targetAtCell = targetGrid.GetItemAt(gridPosition.x, gridPosition.y);
            bool handledByStackMerge = false;
            if (targetAtCell != null && targetAtCell != item)
            {
                var dragReader = GetComponent<ItemDataReader>();
                var targetReader = targetAtCell.GetComponent<ItemDataReader>();
                if (dragReader != null && targetReader != null &&
                    dragReader.ItemData != null && targetReader.ItemData != null)
                {
                    bool sameId = dragReader.ItemData.id == targetReader.ItemData.id;
                    bool stackable = targetReader.ItemData.IsStackable();
                    bool targetNotFull = targetReader.CurrentStack < targetReader.ItemData.maxStack;
                    if (sameId && stackable && targetNotFull)
                    {
                        int max = targetReader.ItemData.maxStack;
                        int sum = targetReader.CurrentStack + dragReader.CurrentStack;
                        if (sum <= max)
                        {
                            targetReader.SetStack(sum);
                            // 成功合并且用尽，销毁拖拽物
                            validDrop = true;
                            handledByStackMerge = true;
                            Destroy(gameObject);
                        }
                        else
                        {
                            int overflow = sum - max;
                            targetReader.SetStack(max);
                            dragReader.SetStack(overflow);
                            // 回到原网格格子（使用拖拽开始时记录的网格与坐标）
                            if (originalGridRef != null)
                            {
                                // 先尝试直接放回记录的格子
                                if (originalGridRef.PlaceItem(item, originalGridPos.x, originalGridPos.y))
                                {
                                    item.SetGridState(originalGridRef, originalGridPos);
                                    transform.SetParent(originalGridRef.transform);
                                    rectTransform.localPosition = originalGridRef.CalculatePositionOnGrid(item, originalGridPos.x, originalGridPos.y);
                                }
                                else
                                {
                                    // 如果原格子暂不可用，退化到返回原父级与相对位置
                                    transform.SetParent(originalParent);
                                    rectTransform.anchoredPosition = originalPosition;
                                }
                            }
                            else
                            {
                                // 无原网格信息，退化到返回原父级与相对位置
                                transform.SetParent(originalParent);
                                rectTransform.anchoredPosition = originalPosition;
                            }
                            validDrop = true;
                            handledByStackMerge = true;
                        }
                    }
                }
            }

            if (!handledByStackMerge && targetGrid.CanPlaceItemAtPosition(gridPosition.x, gridPosition.y, item.GetWidth(), item.GetHeight(), item))
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


                }
            }
            else
            {
                // 输出放置失败原因
                if (!targetGrid.BoundryCheck(gridPosition.x, gridPosition.y, item.GetWidth(), item.GetHeight()))
                {
                    Debug.LogWarning($"物品 {item.name} 在位置 ({gridPosition.x}, {gridPosition.y}) 超出边界，无法放置");
                }
                else if (targetGrid.HasOverlapConflict(gridPosition.x, gridPosition.y, item.GetWidth(), item.GetHeight(), item))
                {
                    Debug.LogWarning($"物品 {item.name} 在位置 ({gridPosition.x}, {gridPosition.y}) 与其他物品重叠，无法放置");
                }
            }
        }

        // 如果网格放置失败，尝试装备槽
        if (!validDrop)
        {
            validDrop = TryPlaceInEquipmentSlot(eventData);
        }

        // 如果没有有效放置目标，则还原位置
        if (!validDrop)
        {
            transform.SetParent(originalParent);
            rectTransform.anchoredPosition = originalPosition;

            // 如果原父级是网格，还原网格状态
            if (item != null)
            {
                if (originalGridRef != null)
                {
                    if (originalGridRef.PlaceItem(item, originalGridPos.x, originalGridPos.y))
                    {
                        item.SetGridState(originalGridRef, originalGridPos);
                        transform.SetParent(originalGridRef.transform);
                        rectTransform.localPosition = originalGridRef.CalculatePositionOnGrid(item, originalGridPos.x, originalGridPos.y);
                    }
                }
            }
        }

        // 清空 InventoryController 的选中物品
        if (inventoryController != null)
        {
            inventoryController.SetSelectedItem(null);
        }

        // 拖拽完成后触发自动保存（在成功放置或堆叠合并时）
        if (validDrop)
        {
            InventorySaveManager saveManager = InventorySaveManager.Instance;
            if (saveManager != null)
            {
                // 触发拖拽保存
                bool saveResult = saveManager.SaveOnDrag();
                if (!saveResult)
                {
                    Debug.LogWarning("[DraggableItem] 拖拽完成后自动保存失败");
                }
            }
            else
            {
                Debug.LogWarning("[DraggableItem] 未找到 InventorySaveManager 实例，无法执行拖拽保存");
            }
        }
    }

    // 应用拖拽时的物品设置
    private void ApplyDragItemSettings()
    {
        if (rectTransform != null)
        {
            // 计算等比例缩放后的尺寸
            Vector2 currentSize = rectTransform.sizeDelta;
            Vector2 scaledSize = CalculateProportionalSize(currentSize, dragItemSize);
            
            // 应用等比例缩放后的尺寸
            rectTransform.sizeDelta = scaledSize;
            rectTransform.localScale = Vector3.one;
            
            Debug.Log($"物品 {gameObject.name} 主对象尺寸从 {currentSize} 等比例调整为 {scaledSize}");
        }
        
        // 调整物品图标大小为等比例缩放后的尺寸
        if (itemIcon != null)
        {
            Vector2 currentIconSize = originalIconSize;
            Vector2 scaledIconSize = CalculateProportionalSize(currentIconSize, dragItemSize);
            itemIcon.sizeDelta = scaledIconSize;
            Debug.Log($"物品 {gameObject.name} 的ItemIcon已等比例调整为 {scaledIconSize}");
        }
        
        // 调整物品高亮大小为等比例缩放后的尺寸
        if (itemHighlight != null)
        {
            Vector2 currentHighlightSize = originalHighlightSize;
            Vector2 scaledHighlightSize = CalculateProportionalSize(currentHighlightSize, dragItemSize);
            itemHighlight.sizeDelta = scaledHighlightSize;
            Debug.Log($"物品 {gameObject.name} 的ItemHighlight已等比例调整为 {scaledHighlightSize}");
        }
        
        // 调整物品文字大小和位置
        if (itemText != null)
        {
            // 计算文字大小（保持比例，但不超过等比例缩放后的尺寸）
            Vector2 currentTextSize = originalTextSize;
            Vector2 scaledTextSize = CalculateProportionalSize(currentTextSize, dragItemSize);
            itemText.sizeDelta = scaledTextSize;
            
            // 固定文字在右下角
            float rightMargin = 10f; // 右边距
            float bottomMargin = 10f; // 下边距
            itemText.anchoredPosition = new Vector2(
                dragItemSize.x / 2 - rightMargin - scaledTextSize.x / 2,
                -dragItemSize.y / 2 + bottomMargin + scaledTextSize.y / 2
            );
            
            Debug.Log($"物品 {gameObject.name} 的ItemText已等比例调整为大小 {scaledTextSize}，位置固定在右下角");
        }
        
        // 隐藏物品背景
        if (itemBackground != null)
        {
            itemBackground.SetActive(false);
                    Debug.Log($"物品 {gameObject.name} 的ItemBackground已隐藏");
    }
    else
    {
        Debug.LogWarning($"物品 {gameObject.name} 未找到ItemBackground，无法隐藏");
    }
}

// 计算等比例缩放后的尺寸
private Vector2 CalculateProportionalSize(Vector2 originalSize, Vector2 targetSize)
{
    // 计算宽高比例
    float scaleX = targetSize.x / originalSize.x;
    float scaleY = targetSize.y / originalSize.y;
    
    // 使用较小的缩放比例，确保物品完全适应目标尺寸且不变形
    float scale = Mathf.Min(scaleX, scaleY);
    
    // 计算缩放后的尺寸
    Vector2 scaledSize = originalSize * scale;
    
    Debug.Log($"等比例缩放计算: 原始尺寸{originalSize} -> 目标尺寸{targetSize} -> 缩放比例{scale} -> 最终尺寸{scaledSize}");
    
    return scaledSize;
}
    
    // 恢复物品原始状态
    private void RestoreItemOriginalState()
    {
        if (rectTransform != null)
        {
            // 恢复原始大小和缩放
            rectTransform.sizeDelta = originalSize;
            rectTransform.localScale = originalScale;
        }
        
        // 恢复物品图标原始大小
        if (itemIcon != null)
        {
            itemIcon.sizeDelta = originalIconSize;
            Debug.Log($"物品 {gameObject.name} 的ItemIcon已恢复为原始大小: {originalIconSize}");
        }
        
        // 恢复物品高亮原始大小
        if (itemHighlight != null)
        {
            itemHighlight.sizeDelta = originalHighlightSize;
            Debug.Log($"物品 {gameObject.name} 的ItemHighlight已恢复为原始大小: {originalHighlightSize}");
        }
        
        // 恢复物品文字原始大小和位置
        if (itemText != null)
        {
            itemText.sizeDelta = originalTextSize;
            itemText.anchoredPosition = originalTextPosition;
            Debug.Log($"物品 {gameObject.name} 的ItemText已恢复为原始大小: {originalTextSize}，位置: {originalTextPosition}");
        }
        
        // 恢复背景状态
        if (itemBackground != null)
        {
            itemBackground.SetActive(originalBackgroundActive);
            Debug.Log($"物品 {gameObject.name} 的ItemBackground已恢复为: {originalBackgroundActive}");
        }
        
        Debug.Log($"物品 {gameObject.name} 已恢复原始状态: 主对象大小={originalSize}, 背景状态={originalBackgroundActive}");
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