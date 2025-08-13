using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;

[RequireComponent(typeof(CanvasGroup))]
public class DraggableItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
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

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (item == null) item = GetComponent<InventorySystemItem>();

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

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!item.IsDraggable) return;

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
                // 使用反射调用RemoveItem
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
            itemBackground.SetActive(false);
        }

        DisableAllRaycastTargets();
        transform.SetParent(item.GetComponentInParent<Canvas>().transform, true);
        canvasGroup.alpha = 1.0f;
        canvasGroup.blocksRaycasts = false;

        // 通知InventoryController开始拖拽
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

        // 移除 isDragging = false;
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
                        itemBackground.SetActive(false);
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
                // 检查ItemGrid
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

                // 检查BackpackItemGrid
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

                // 检查TactiaclRigItemGrid
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
                Vector2Int itemSize = new Vector2Int(item.Data.width, item.Data.height);

                // 调用对应的GetTileGridPosition方法
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

                Debug.Log($"尝试放到网格位置: ({dropPosition.x}, {dropPosition.y}), 物品大小: ({itemSize.x}, {itemSize.y})");

                bool canPlace = false;

                // 调用对应的CanPlaceItem方法
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

                    // 调用对应的PlaceItem方法
                    if (gridType == typeof(ItemGrid))
                    {
                        var placeItemMethod = typeof(ItemGrid).GetMethod("PlaceItem",
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (placeItemMethod != null)
                        {
                            placeItemMethod.Invoke(targetGrid, new object[] { gameObject, dropPosition, itemSize });
                        }
                    }
                    else if (gridType == typeof(BackpackItemGrid))
                    {
                        var placeItemMethod = typeof(BackpackItemGrid).GetMethod("PlaceItem",
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (placeItemMethod != null)
                        {
                            placeItemMethod.Invoke(targetGrid, new object[] { gameObject, dropPosition, itemSize });
                        }
                    }
                    else if (gridType == typeof(TactiaclRigItemGrid))
                    {
                        var placeItemMethod = typeof(TactiaclRigItemGrid).GetMethod("PlaceItem",
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (placeItemMethod != null)
                        {
                            placeItemMethod.Invoke(targetGrid, new object[] { gameObject, dropPosition, itemSize });
                        }
                    }

                    placementSuccessful = true;
                    RestoreOriginalSize();

                    if (itemBackground != null)
                    {
                        itemBackground.SetActive(true);
                    }

                    Debug.Log($"物品放置网格成功，放到网格位置: ({dropPosition.x}, {dropPosition.y})");
                }
                else
                {
                    Debug.Log($"无法放到网格位置: ({dropPosition.x}, {dropPosition.y}) - 位置被占用或超出边界");
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
                    // 重新装备物品并创建网格
                    originalEquipSlot.ReequipItem(this);
                }

                if (itemBackground != null)
                {
                    itemBackground.SetActive(false);
                }
            }
            else
            {
                ItemGrid parentGrid = originalParent.GetComponent<ItemGrid>();
                if (parentGrid != null)
                {
                    Vector2Int gridPos = parentGrid.GetTileGridPosition(originalPos + (Vector2)parentGrid.transform.position);
                    Vector2Int itemSize = new Vector2Int(item.Data.width, item.Data.height);

                    var placeItemMethod = typeof(ItemGrid).GetMethod("PlaceItem",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (placeItemMethod != null)
                    {
                        placeItemMethod.Invoke(parentGrid, new object[] { gameObject, gridPos, itemSize });
                    }
                }

                RestoreOriginalSize();

                if (itemBackground != null)
                {
                    itemBackground.SetActive(true);
                }
            }
        }

        RestoreAllRaycastTargets();
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        // 通知InventoryController结束拖拽
        var inventoryController = FindObjectOfType<InventoryController>();
        if (inventoryController != null)
        {
            inventoryController.ClearDraggedItem();
        }
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
}
