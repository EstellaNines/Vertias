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

    [Header("旋转设置")]
    [SerializeField] private bool enableRotationDuringDrag = true; // 是否允许拖拽时旋转

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
        // 只有在拖拽状态下才允许旋转
        if (isDragging && enableRotationDuringDrag)
        {
            HandleRotationInput();
        }
    }

    // 处理旋转输入
    private void HandleRotationInput()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            TryRotateItem();
        }
    }

    // 尝试旋转物品
    private bool TryRotateItem()
    {
        if (itemDataHolder == null || !itemDataHolder.CanRotate())
        {
            Debug.Log("物品不支持旋转");
            return false;
        }

        // 保存旋转前的状态
        int previousRotation = itemDataHolder.GetCurrentRotation();
        Vector2Int previousSize = itemDataHolder.GetCurrentSize();

        // 执行旋转
        bool rotated = itemDataHolder.RotateItem();
        if (!rotated) return false;

        // 检查旋转后是否会超出边界或发生碰撞
        if (isDragging && !CanRotateAtCurrentPosition())
        {
            // 如果不能旋转，回滚到之前的状态
            itemDataHolder.SetRotation(previousRotation);
            Debug.Log("旋转会导致碰撞或超出边界，已自动回弹");
            return false;
        }

        Debug.Log($"物品旋转成功，当前角度：{itemDataHolder.GetCurrentRotation()}度");
        return true;
    }

    // 检查当前位置是否可以旋转（增强版）
    private bool CanRotateAtCurrentPosition()
    {
        if (itemDataHolder == null) return false;

        // 获取当前鼠标位置下的网格
        var results = new System.Collections.Generic.List<RaycastResult>();
        PointerEventData eventData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var result in results)
        {
            // 检查BackpackItemGrid
            BackpackItemGrid backpackGrid = result.gameObject.GetComponent<BackpackItemGrid>();
            if (backpackGrid == null)
                backpackGrid = result.gameObject.GetComponentInParent<BackpackItemGrid>();

            if (backpackGrid != null)
            {
                Vector2Int dropPosition = backpackGrid.GetTileGridPosition(Input.mousePosition);
                Vector2Int newSize = itemDataHolder.GetRotatedSize();

                // 检查边界
                Vector2Int gridSize = backpackGrid.GetGridSize();
                if (dropPosition.x + newSize.x > gridSize.x ||
                    dropPosition.y + newSize.y > gridSize.y ||
                    dropPosition.x < 0 || dropPosition.y < 0)
                {
                    Debug.Log($"旋转后会超出网格边界: 位置({dropPosition.x}, {dropPosition.y}), 尺寸({newSize.x}, {newSize.y}), 网格大小({gridSize.x}, {gridSize.y})");
                    return false;
                }

                // 检查碰撞（排除自己）
                return backpackGrid.CanPlaceItem(dropPosition, newSize, gameObject);
            }

            // 可以添加其他网格类型的检查...
            // ItemGrid, TacticalRigItemGrid等
        }

        return true; // 如果没有找到网格，默认允许旋转
    }

    // 智能旋转 - 完全修复版
    private bool TrySmartRotate()
    {
        if (itemDataHolder == null || !itemDataHolder.CanRotate())
            return false;

        // 保存当前状态
        int originalRotation = itemDataHolder.GetCurrentRotation();
        Vector3 originalWorldPosition = rectTransform.position;
        
        // 获取当前网格位置
        Vector2Int currentGridPos = GetCurrentGridPosition();
    
        // 执行旋转（ItemDataHolder会处理中心轴心旋转）
        itemDataHolder.RotateItem();
        Vector2Int newSize = itemDataHolder.GetRotatedSize();
    
        // 检查旋转后是否可以在当前位置放置
        if (CanPlaceAtPosition(currentGridPos, newSize))
        {
            // 当前位置可以放置，确保位置正确
            rectTransform.position = originalWorldPosition;
            return true;
        }
    
        // 当前位置不行，尝试在附近寻找合适位置
        for (int offsetX = -1; offsetX <= 1; offsetX++)
        {
            for (int offsetY = -1; offsetY <= 1; offsetY++)
            {
                Vector2Int testPos = currentGridPos + new Vector2Int(offsetX, offsetY);
                if (CanPlaceAtPosition(testPos, newSize))
                {
                    // 找到合适位置，对齐到该位置
                    AlignToGrid(testPos);
                    return true;
                }
            }
        }
    
        // 没找到合适位置，回滚旋转
        itemDataHolder.SetRotation(originalRotation);
        return false;
    }

    // 对齐到网格位置（修复版）
    private void AlignToGrid(Vector2Int gridPosition)
    {
        BaseItemGrid currentGrid = GetCurrentGrid();
        if (currentGrid == null) return;

        float cellSize = currentGrid.GetCellSize();
        Vector2Int itemSize = itemDataHolder.GetRotatedSize();

        // 统一使用中心轴心和锚点
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);

        // 计算网格中心位置（基于中心锚点）
        Vector2 centerPosition = new Vector2(
            gridPosition.x * cellSize + (itemSize.x * cellSize) / 2f,
            -gridPosition.y * cellSize - (itemSize.y * cellSize) / 2f
        );

        rectTransform.anchoredPosition = centerPosition;
    }

    // 获取当前所在的网格
    private BaseItemGrid GetCurrentGrid()
    {
        // 通过射线检测获取当前鼠标下的网格
        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        foreach (var result in results)
        {
            BaseItemGrid grid = result.gameObject.GetComponent<BaseItemGrid>();
            if (grid != null)
            {
                return grid;
            }

            // 检查其他网格类型
            BackpackItemGrid backpackGrid = result.gameObject.GetComponent<BackpackItemGrid>();
            if (backpackGrid != null)
            {
                return backpackGrid;
            }

            TactiaclRigItemGrid tacticalGrid = result.gameObject.GetComponent<TactiaclRigItemGrid>();
            if (tacticalGrid != null)
            {
                return tacticalGrid;
            }
        }

        return null;
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
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!item.IsDraggable) return;

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
                Vector2Int itemSize = item.Size;  //new Vector2Int(item.Data.width, item.Data.height);


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

                // 调用对应的 CanPlaceItem 方法
                if (gridType == typeof(ItemGrid))
                {
                    canPlace = ((ItemGrid)targetGrid).CanPlaceItem(dropPosition, itemSize);
                }
                else if (gridType == typeof(BackpackItemGrid))
                {
                    canPlace = ((BackpackItemGrid)targetGrid).CanPlaceItem(dropPosition, itemSize);
                }
                else if (gridType == typeof(TactiaclRigItemGrid))
                {
                    canPlace = ((TactiaclRigItemGrid)targetGrid).CanPlaceItem(dropPosition, itemSize);
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
                            Vector2Int itemSize = item.Size;

                            // 直接调用PlaceItem方法
                            tacticalGrid.PlaceItem(gameObject, gridPos, itemSize);
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
                inventoryController.ShowHoverHighlight(item, transform.parent);
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
}