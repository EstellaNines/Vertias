using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;

[RequireComponent(typeof(CanvasGroup))]
public class DraggableItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("拖拽时自动寻找")]
    [SerializeField] private InventorySystemItem item;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    public Transform originalParent;
    public Vector2 originalPos;
    private bool wasInEquipSlot = false;

    private GameObject itemBackground;
    private Vector2 originalSize;
    private Vector3 originalScale;

    private ItemSpawner itemSpawner;
    private Vector2Int gridPosition;

    private List<Image> allImages = new List<Image>();
    private List<bool> originalRaycastTargets = new List<bool>();

    private Vector2 originalAnchorMin;
    private Vector2 originalAnchorMax;
    private Vector2 originalPivot;
    public bool isDragging { get; private set; } = false; // 拖拽状态标识



    private ItemDataHolder itemDataHolder;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (item == null) item = GetComponent<InventorySystemItem>();

        // 初始化 ItemDataHolder
        itemDataHolder = GetComponent<ItemDataHolder>();
        if (itemDataHolder == null)
        {
            itemDataHolder = gameObject.AddComponent<ItemDataHolder>();
        }

        itemSpawner = FindObjectOfType<ItemSpawner>();
        itemBackground = transform.Find("ItemBackground")?.gameObject;
        CacheImageComponents();

        originalAnchorMin = rectTransform.anchorMin;
        originalAnchorMax = rectTransform.anchorMax;
        originalPivot = rectTransform.pivot;

        originalSize = rectTransform.sizeDelta;
        originalScale = rectTransform.localScale;

    }

    private void CacheImageComponents()
    {
        allImages.Clear();
        originalRaycastTargets.Clear();

        Image[] images = GetComponentsInChildren<Image>(true);

        foreach (Image img in images)
        {
            allImages.Add(img);
            originalRaycastTargets.Add(img.raycastTarget);
        }
    }

    private void DisableAllRaycastTargets()
    {
        foreach (Image img in allImages)
        {
            if (img != null)
            {
                img.raycastTarget = false;
            }
        }
    }

    private void RestoreAllRaycastTargets()
    {
        for (int i = 0; i < allImages.Count && i < originalRaycastTargets.Count; i++)
        {
            if (allImages[i] != null)
            {
                allImages[i].raycastTarget = originalRaycastTargets[i];
            }
        }
    }

    private void Update()
    {
        // 旋转功能已移除
    }










    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!item.IsDraggable) return;

        isDragging = true; // 设置拖拽状态
        canvasGroup.alpha = 1.0f;
        canvasGroup.blocksRaycasts = false;

        wasInEquipSlot = GetComponentInParent<EquipSlot>() != null;
        originalParent = transform.parent;
        originalPos = rectTransform.anchoredPosition;

        Vector3 worldPosition = rectTransform.position;

        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.position = worldPosition;

        if (!wasInEquipSlot)
        {
            // 检查不同类型的网格
            object parentGrid = null;
            System.Type gridType = null;

            ItemGrid itemGrid = originalParent.GetComponent<ItemGrid>();
            if (itemGrid != null)
            {
                parentGrid = itemGrid;
                gridType = typeof(ItemGrid);
            }
            else
            {
                BackpackItemGrid backpackGrid = originalParent.GetComponent<BackpackItemGrid>();
                if (backpackGrid != null)
                {
                    parentGrid = backpackGrid;
                    gridType = typeof(BackpackItemGrid);
                }
                else
                {
                    TactiaclRigItemGrid tacticalRigGrid = originalParent.GetComponent<TactiaclRigItemGrid>();
                    if (tacticalRigGrid != null)
                    {
                        parentGrid = tacticalRigGrid;
                        gridType = typeof(TactiaclRigItemGrid);
                    }
                }
            }

            if (parentGrid != null)
            {
                // 使用反射调用 RemoveItem
                var removeItemMethod = gridType.GetMethod("RemoveItem",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (removeItemMethod != null)
                {
                    removeItemMethod.Invoke(parentGrid, new object[] { gameObject });
                }
            }
        }

        if (wasInEquipSlot)
        {
            RestoreOriginalSize();
        }

        if (itemBackground != null)
        {
            // 拖拽时背景消失
            itemBackground.SetActive(false);
        }

        DisableAllRaycastTargets();
        transform.SetParent(item.GetComponentInParent<Canvas>().transform, true);
        canvasGroup.alpha = 1.0f;
        canvasGroup.blocksRaycasts = false;

        // 通知 InventoryController 开始拖拽
        var inventoryController = FindObjectOfType<InventoryController>();
        if (inventoryController != null)
        {
            inventoryController.SetDraggedItem(item);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!item.IsDraggable) return;

        Vector2 localPointerPosition;
        Canvas canvas = item.GetComponentInParent<Canvas>();
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out localPointerPosition);

        rectTransform.anchoredPosition = localPointerPosition;
        
        // 检测装备栏并显示高亮提示
        CheckEquipSlotHighlight(eventData);
    }
    
    /// <summary>
    /// 检测装备栏并显示高亮提示
    /// </summary>
    /// <param name="eventData">拖拽事件数据</param>
    private void CheckEquipSlotHighlight(PointerEventData eventData)
    {
        // 进行射线检测
        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
        
        EquipSlot targetEquipSlot = null;
        
        // 查找装备栏
        foreach (var result in results)
        {
            EquipSlot equipSlot = result.gameObject.GetComponent<EquipSlot>();
            if (equipSlot != null)
            {
                targetEquipSlot = equipSlot;
                break;
            }
        }
        
        // 如果找到装备栏，显示高亮提示
        if (targetEquipSlot != null)
        {
            var itemComponent = GetComponent<InventorySystemItem>();
            if (itemComponent != null)
            {
                bool canEquip = CanEquipToSlot(targetEquipSlot, itemComponent);
                targetEquipSlot.ShowEquipHighlight(canEquip);
            }
        }
        else
        {
            // 如果没有找到装备栏，隐藏所有装备栏高亮
            HideAllEquipSlotHighlights();
        }
    }
    
    /// <summary>
    /// 隐藏所有装备栏高亮
    /// </summary>
    private void HideAllEquipSlotHighlights()
    {
        EquipSlot[] allEquipSlots = FindObjectsOfType<EquipSlot>();
        foreach (var equipSlot in allEquipSlots)
        {
            equipSlot.HideEquipHighlight();
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!item.IsDraggable) return;

        // 清理所有装备栏高亮显示
        HideAllEquipSlotHighlights();

        bool placementSuccessful = false;

        EquipSlot originalEquipSlot = originalParent.GetComponent<EquipSlot>();
        if (originalEquipSlot != null && wasInEquipSlot)
        {
            originalEquipSlot.OnItemRemoved();
        }

        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        // 检查装备槽
        foreach (var result in results)
        {
            EquipSlot equipSlot = result.gameObject.GetComponent<EquipSlot>();
            if (equipSlot != null)
            {
                var itemComponent = GetComponent<InventorySystemItem>();
                if (itemComponent != null && CanEquipToSlot(equipSlot, itemComponent))
                {
                    transform.SetParent(equipSlot.transform, false);
                    placementSuccessful = true;
                    FitToEquipSlot(equipSlot);

                    if (itemBackground != null)
                    {
                        // 放入装备槽后恢复背景显示
                        itemBackground.SetActive(true);
                    }
                    break;
                }
            }
        }

        // 检查网格
        if (!placementSuccessful)
        {
            object targetGrid = null;
            System.Type gridType = null;

            foreach (var result in results)
            {
                // 检查 ItemGrid
                ItemGrid itemGrid = result.gameObject.GetComponent<ItemGrid>();
                if (itemGrid != null)
                {
                    targetGrid = itemGrid;
                    gridType = typeof(ItemGrid);
                    break;
                }
                itemGrid = result.gameObject.GetComponentInParent<ItemGrid>();
                if (itemGrid != null)
                {
                    targetGrid = itemGrid;
                    gridType = typeof(ItemGrid);
                    break;
                }
                itemGrid = result.gameObject.GetComponentInChildren<ItemGrid>();
                if (itemGrid != null)
                {
                    targetGrid = itemGrid;
                    gridType = typeof(ItemGrid);
                    break;
                }

                // 检查 BackpackItemGrid
                BackpackItemGrid backpackGrid = result.gameObject.GetComponent<BackpackItemGrid>();
                if (backpackGrid != null)
                {
                    targetGrid = backpackGrid;
                    gridType = typeof(BackpackItemGrid);
                    break;
                }
                backpackGrid = result.gameObject.GetComponentInParent<BackpackItemGrid>();
                if (backpackGrid != null)
                {
                    targetGrid = backpackGrid;
                    gridType = typeof(BackpackItemGrid);
                    break;
                }
                backpackGrid = result.gameObject.GetComponentInChildren<BackpackItemGrid>();
                if (backpackGrid != null)
                {
                    targetGrid = backpackGrid;
                    gridType = typeof(BackpackItemGrid);
                    break;
                }

                // 检查 TactiaclRigItemGrid
                TactiaclRigItemGrid tacticalRigGrid = result.gameObject.GetComponent<TactiaclRigItemGrid>();
                if (tacticalRigGrid != null)
                {
                    targetGrid = tacticalRigGrid;
                    gridType = typeof(TactiaclRigItemGrid);
                    break;
                }
                tacticalRigGrid = result.gameObject.GetComponentInParent<TactiaclRigItemGrid>();
                if (tacticalRigGrid != null)
                {
                    targetGrid = tacticalRigGrid;
                    gridType = typeof(TactiaclRigItemGrid);
                    break;
                }
                tacticalRigGrid = result.gameObject.GetComponentInChildren<TactiaclRigItemGrid>();
                if (tacticalRigGrid != null)
                {
                    targetGrid = tacticalRigGrid;
                    gridType = typeof(TactiaclRigItemGrid);
                    break;
                }
            }

            if (targetGrid != null)
            {
                Vector2Int dropPosition = Vector2Int.zero;
                // 使用原始尺寸进行占位检测
                Vector2Int itemSize = item.Size;

                // 调用对应的 GetTileGridPosition 方法
                if (gridType == typeof(ItemGrid))
                {
                    dropPosition = ((ItemGrid)targetGrid).GetTileGridPosition(eventData.position);
                }
                else if (gridType == typeof(BackpackItemGrid))
                {
                    dropPosition = ((BackpackItemGrid)targetGrid).GetTileGridPosition(eventData.position);
                }
                else if (gridType == typeof(TactiaclRigItemGrid))
                {
                    dropPosition = ((TactiaclRigItemGrid)targetGrid).GetTileGridPosition(eventData.position);
                }

                Debug.Log($"尝试放置到网格位置: ({dropPosition.x}, {dropPosition.y}), 物品大小: ({itemSize.x}, {itemSize.y})");

                bool canPlace = false;

                // 调用对应的 CanPlaceItem 方法（使用智能检测）
                if (gridType == typeof(ItemGrid))
                {
                    canPlace = ((ItemGrid)targetGrid).CanPlaceItem(gameObject, dropPosition);
                }
                else if (gridType == typeof(BackpackItemGrid))
                {
                    canPlace = ((BackpackItemGrid)targetGrid).CanPlaceItem(gameObject, dropPosition);
                }
                else if (gridType == typeof(TactiaclRigItemGrid))
                {
                    canPlace = ((TactiaclRigItemGrid)targetGrid).CanPlaceItem(gameObject, dropPosition);
                }

                if (canPlace)
                {
                    transform.SetParent(((MonoBehaviour)targetGrid).transform, false);

                    // 直接调用PlaceItem方法（不再使用反射）
                    if (gridType == typeof(ItemGrid))
                    {
                        ((ItemGrid)targetGrid).PlaceItem(gameObject, dropPosition, itemSize);
                    }
                    else if (gridType == typeof(BackpackItemGrid))
                    {
                        ((BackpackItemGrid)targetGrid).PlaceItem(gameObject, dropPosition, itemSize);
                    }
                    else if (gridType == typeof(TactiaclRigItemGrid))
                    {
                        ((TactiaclRigItemGrid)targetGrid).PlaceItem(gameObject, dropPosition, itemSize);
                    }

                    placementSuccessful = true;
                    RestoreOriginalSize();

                    if (itemBackground != null)
                    {
                        // 放到任意网格后恢复背景
                        itemBackground.SetActive(true);
                    }

                    Debug.Log($"物品放置成功，放置到网格位置: ({dropPosition.x}, {dropPosition.y})");
                }
                else
                {
                    Debug.Log($"无法放置到网格位置: ({dropPosition.x}, {dropPosition.y}) - 位置被占用或超出边界");
                }
            }
            else
            {
                Debug.Log("未检测到有效的网格目标");
            }
        }

        // 返回原位置
        if (!placementSuccessful)
        {
            transform.SetParent(originalParent, false);

            rectTransform.anchorMin = originalAnchorMin;
            rectTransform.anchorMax = originalAnchorMax;
            rectTransform.pivot = originalPivot;
            rectTransform.anchoredPosition = originalPos;

            if (wasInEquipSlot)
            {
                if (originalEquipSlot != null)
                {
                    // 重新装备物品到装备槽
                    originalEquipSlot.ReequipItem(this);
                }

                if (itemBackground != null)
                {
                    // 仍在装备槽，背景显示
                    itemBackground.SetActive(true);
                }
            }
            else
            {
                // 检查不同类型的网格并返回原位置
                ItemGrid itemGrid = originalParent.GetComponent<ItemGrid>();
                if (itemGrid != null)
                {
                    Vector2Int gridPos = itemGrid.GetTileGridPosition(originalPos + (Vector2)itemGrid.transform.position);
                    // 使用原始尺寸
                    Vector2Int itemSize = item.Size;

                    // 直接调用PlaceItem方法（不再使用反射）
                    itemGrid.PlaceItem(gameObject, gridPos, itemSize);
                }
                else
                {
                    BackpackItemGrid backpackGrid = originalParent.GetComponent<BackpackItemGrid>();
                    if (backpackGrid != null)
                    {
                        Vector2Int gridPos = backpackGrid.GetTileGridPosition(originalPos + (Vector2)backpackGrid.transform.position);
                        // 使用原始尺寸
                        Vector2Int itemSize = item.Size;

                        // 直接调用PlaceItem方法
                        backpackGrid.PlaceItem(gameObject, gridPos, itemSize);
                    }
                    else
                    {
                        TactiaclRigItemGrid tacticalGrid = originalParent.GetComponent<TactiaclRigItemGrid>();
                        if (tacticalGrid != null)
                        {
                            Vector2Int gridPos = tacticalGrid.GetTileGridPosition(originalPos + (Vector2)tacticalGrid.transform.position);
                            // 使用原始尺寸
                            Vector2Int itemSize = item.Size;

                            // 直接调用PlaceItem方法
                            tacticalGrid.PlaceItem(gameObject, gridPos, itemSize);
                        }
                    }
                }
            }

            RestoreOriginalSize();

            if (itemBackground != null)
            {
                // 回到原网格，背景显示
                itemBackground.SetActive(true);
            }
        }

        RestoreAllRaycastTargets();
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        // 通知 InventoryController 结束拖拽
        var inventoryController = FindObjectOfType<InventoryController>();
        if (inventoryController != null)
        {
            inventoryController.ClearDraggedItem();
        }

        isDragging = false; // 清除拖拽状态
    }

    private void FitToEquipSlot(EquipSlot equipSlot)
    {
        RectTransform slotRect = equipSlot.GetComponent<RectTransform>();
        if (slotRect == null) return;

        var paddingField = typeof(EquipSlot).GetField("padding",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        float padding = paddingField != null ? (float)paddingField.GetValue(equipSlot) : 10f;

        Vector2 availableSize = slotRect.sizeDelta - new Vector2(padding * 2, padding * 2);

        float scaleX = availableSize.x / originalSize.x;
        float scaleY = availableSize.y / originalSize.y;
        float scale = Mathf.Min(scaleX, scaleY, 1f);

        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.localScale = originalScale * scale;
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = originalSize;
    }

    private void RestoreOriginalSize()
    {
        rectTransform.sizeDelta = originalSize;
        rectTransform.localScale = originalScale;
    }

    public void OnRemovedFromEquipSlot()
    {
        RestoreOriginalSize();

        if (itemBackground != null)
        {
            itemBackground.SetActive(true);
        }
    }

    private bool CanEquipToSlot(EquipSlot equipSlot, InventorySystemItem itemComponent)
    {
        var acceptedTypeField = typeof(EquipSlot).GetField("acceptedType",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (acceptedTypeField != null)
        {
            InventorySystemItemCategory acceptedType = (InventorySystemItemCategory)acceptedTypeField.GetValue(equipSlot);
            return itemComponent.Data.itemCategory == acceptedType;
        }

        return false;
    }

    // 在OnEndDrag方法后添加悬停事件处理方法

    /// <summary>
    /// 鼠标进入物品时触发
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        // 只有在非拖拽状态下才显示悬停高亮
        if (!isDragging && item.IsDraggable)
        {
            var inventoryController = FindObjectOfType<InventoryController>();
            if (inventoryController != null)
            {
                // 检测是否在装备栏中
                EquipSlot equipSlot = GetComponentInParent<EquipSlot>();
                if (equipSlot != null)
                {
                    // 在装备栏中，传递装备栏信息以适配大小
                    inventoryController.ShowHoverHighlight(item, equipSlot.transform, equipSlot);
                }
                else
                {
                    // 在普通网格中，获取正确的父级网格对象
                    Transform parentGrid = GetParentGridTransform();
                    if (parentGrid != null)
                    {
                        inventoryController.ShowHoverHighlight(item, parentGrid);
                    }
                    else
                    {
                        Debug.LogWarning($"无法获取物品 {item.name} 的父级网格");
                    }
                }
            }
        }
    }

    /// <summary>
    /// 鼠标离开物品时触发
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        // 隐藏悬停高亮
        if (!isDragging)
        {
            var inventoryController = FindObjectOfType<InventoryController>();
            if (inventoryController != null)
            {
                inventoryController.HideHoverHighlight();
            }
        }
    }

    // 添加缺失的方法

    /// <summary>
    /// 获取当前物品在网格中的位置
    /// </summary>
    private Vector2Int GetCurrentGridPosition()
    {
        // 获取当前鼠标位置下的网格
        var results = new System.Collections.Generic.List<RaycastResult>();
        PointerEventData eventData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var result in results)
        {
            // 检查BaseItemGrid（统一接口）
            BaseItemGrid baseGrid = result.gameObject.GetComponent<BaseItemGrid>();
            if (baseGrid == null)
                baseGrid = result.gameObject.GetComponentInParent<BaseItemGrid>();

            if (baseGrid != null)
            {
                return baseGrid.GetTileGridPosition(Input.mousePosition);
            }

            // 兼容旧的BackpackItemGrid（如果还没有继承BaseItemGrid）
            BackpackItemGrid backpackGrid = result.gameObject.GetComponent<BackpackItemGrid>();
            if (backpackGrid == null)
                backpackGrid = result.gameObject.GetComponentInParent<BackpackItemGrid>();

            if (backpackGrid != null)
            {
                return backpackGrid.GetTileGridPosition(Input.mousePosition);
            }
        }

        return Vector2Int.zero;
    }

    /// <summary>
    /// 检查指定位置是否可以放置物品
    /// </summary>
    private bool CanPlaceAtPosition(Vector2Int position, Vector2Int size)
    {
        // 获取当前鼠标位置下的网格
        var results = new System.Collections.Generic.List<RaycastResult>();
        PointerEventData eventData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var result in results)
        {
            // 检查BaseItemGrid（统一接口）
            BaseItemGrid baseGrid = result.gameObject.GetComponent<BaseItemGrid>();
            if (baseGrid == null)
                baseGrid = result.gameObject.GetComponentInParent<BaseItemGrid>();

            if (baseGrid != null)
            {
                return baseGrid.CanPlaceItem(position, size);
            }

            // 兼容旧的BackpackItemGrid
            BackpackItemGrid backpackGrid = result.gameObject.GetComponent<BackpackItemGrid>();
            if (backpackGrid == null)
                backpackGrid = result.gameObject.GetComponentInParent<BackpackItemGrid>();

            if (backpackGrid != null)
            {
                return backpackGrid.CanPlaceItem(position, size);
            }
        }

        return false;
    }

    /// <summary>
    /// 将鼠标光标移动到指定网格位置（模拟功能）
    /// </summary>
    private void MoveCursorToGridPosition(Vector2Int gridPosition)
    {
        // 获取当前网格
        var results = new System.Collections.Generic.List<RaycastResult>();
        PointerEventData eventData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var result in results)
        {
            BaseItemGrid baseGrid = result.gameObject.GetComponent<BaseItemGrid>();
            if (baseGrid == null)
                baseGrid = result.gameObject.GetComponentInParent<BaseItemGrid>();

            if (baseGrid != null)
            {
                float cellSize = baseGrid.GetCellSize();
                Vector2 worldPosition = baseGrid.CalculatePositionOnGrid(1, 1, gridPosition.x, gridPosition.y);

                // 将世界坐标转换为屏幕坐标
                Camera camera = Camera.main;
                if (camera != null)
                {
                    Vector3 screenPos = camera.WorldToScreenPoint(worldPosition);
                    // 注意：实际上无法直接移动鼠标光标，这里只是记录目标位置
                    Debug.Log($"目标屏幕位置: {screenPos}");
                }
                break;
            }
        }
    }

    /// <summary>
    /// 获取物品的父级网格Transform
    /// </summary>
    /// <returns>父级网格Transform</returns>
    private Transform GetParentGridTransform()
    {
        // 尝试获取BaseItemGrid
        var baseGrid = GetComponentInParent<BaseItemGrid>();
        if (baseGrid != null)
        {
            return baseGrid.transform;
        }

        // 尝试获取BackpackItemGrid
        var backpackGrid = GetComponentInParent<BackpackItemGrid>();
        if (backpackGrid != null)
        {
            return backpackGrid.transform;
        }

        // 尝试获取TactiaclRigItemGrid
        var tacticalGrid = GetComponentInParent<TactiaclRigItemGrid>();
        if (tacticalGrid != null)
        {
            return tacticalGrid.transform;
        }

        // 尝试获取ItemGrid
        var itemGrid = GetComponentInParent<ItemGrid>();
        if (itemGrid != null)
        {
            return itemGrid.transform;
        }

        // 如果都找不到，返回transform.parent
        Debug.LogWarning($"无法找到物品 {item.name} 的网格父级，使用默认父级: {transform.parent?.name}");
        return transform.parent;
    }
}