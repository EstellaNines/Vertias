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

    [Header("高亮显示设置")]
    [SerializeField] private Image highlightOverlay;
    [SerializeField] private float fadeTime = 0.15f;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    public Transform originalParent;
    public Vector2 originalPos;

    // 移除了未使用的 wasEquipped 字段
    private bool isDragging = false;
    private bool wasInEquipSlot = false; // 记录拖拽开始时是否在装备栏中

    // 缓存背景对象引用
    private GameObject itemBackground;
    private Vector2 originalSize;
    private Vector3 originalScale;

    // 高亮颜色
    private Color normalColor = Color.white.WithAlpha(0f);
    private Color hoverColor = Color.white.WithAlpha(0.35f);
    private Color validDropColor = Color.green.WithAlpha(0.35f);
    private Color invalidDropColor = Color.red.WithAlpha(0.35f);

    private Coroutine currentFadeCoroutine;

    // 网格相关
    private ItemSpawner itemSpawner;
    private Vector2Int gridPosition;

    // 存储所有Image组件的原始raycastTarget状态
    private List<Image> allImages = new List<Image>();
    private List<bool> originalRaycastTargets = new List<bool>();

    // 添加锚点状态记录
    private Vector2 originalAnchorMin;
    private Vector2 originalAnchorMax;
    private Vector2 originalPivot;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (item == null) item = GetComponent<InventorySystemItem>();

        itemSpawner = FindObjectOfType<ItemSpawner>();

        // 查找ItemBackground子对象
        itemBackground = transform.Find("ItemBackground")?.gameObject;

        // 缓存所有Image组件及其原始raycastTarget状态
        CacheImageComponents();

        // 存储原始锚点设置
        originalAnchorMin = rectTransform.anchorMin;
        originalAnchorMax = rectTransform.anchorMax;
        originalPivot = rectTransform.pivot;

        // 查找或创建高亮覆盖层
        if (highlightOverlay == null)
        {
            highlightOverlay = GetComponentInChildren<Image>();
            if (highlightOverlay != null && highlightOverlay.gameObject != itemBackground)
            {
                // 如果找到的Image不是背景，则使用它作为高亮层
            }
            else
            {
                // 创建新的高亮覆盖层
                GameObject overlayObj = new GameObject("HighlightOverlay");
                overlayObj.transform.SetParent(transform, false);
                highlightOverlay = overlayObj.AddComponent<Image>();
                highlightOverlay.color = normalColor;

                // 设置为全覆盖
                RectTransform overlayRect = highlightOverlay.GetComponent<RectTransform>();
                overlayRect.anchorMin = Vector2.zero;
                overlayRect.anchorMax = Vector2.one;
                overlayRect.sizeDelta = Vector2.zero;
                overlayRect.anchoredPosition = Vector2.zero;

                // 重新缓存Image组件（因为添加了新的）
                CacheImageComponents();
            }
        }

        if (highlightOverlay != null)
        {
            highlightOverlay.color = normalColor;
            highlightOverlay.raycastTarget = false;
        }

        // 存储原始大小和缩放
        originalSize = rectTransform.sizeDelta;
        originalScale = rectTransform.localScale;
    }

    // 缓存所有Image组件及其原始raycastTarget状态
    private void CacheImageComponents()
    {
        allImages.Clear();
        originalRaycastTargets.Clear();

        // 获取自身及所有子对象的Image组件
        Image[] images = GetComponentsInChildren<Image>(true);

        foreach (Image img in images)
        {
            allImages.Add(img);
            originalRaycastTargets.Add(img.raycastTarget);
        }
    }

    // 禁用所有Image组件的raycastTarget
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

    // 恢复所有Image组件的原始raycastTarget状态
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

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isDragging && highlightOverlay != null)
        {
            StartHighlightFade(hoverColor);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isDragging && highlightOverlay != null)
        {
            StartHighlightFade(normalColor);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!item.IsDraggable) return;

        isDragging = true;

        // 检查是否从装备栏开始拖拽
        wasInEquipSlot = GetComponentInParent<EquipSlot>() != null;

        originalParent = transform.parent;
        originalPos = rectTransform.anchoredPosition;

        // 保存当前的世界位置
        Vector3 worldPosition = rectTransform.position;

        // 切换到中心锚点进行拖拽
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);

        // 恢复世界位置，这样视觉上物品不会跳动
        rectTransform.position = worldPosition;

        // 如果从网格开始拖拽，需要清除网格占用状态
        if (!wasInEquipSlot)
        {
            ItemGrid parentGrid = originalParent.GetComponent<ItemGrid>();
            if (parentGrid != null)
            {
                parentGrid.RemoveItem(gameObject);
            }
        }

        // 如果从装备栏拖拽，需要恢复原始大小
        if (wasInEquipSlot)
        {
            RestoreOriginalSize();
        }

        // 隐藏物品背景（拖拽时不显示背景）
        if (itemBackground != null)
        {
            itemBackground.SetActive(false);
        }

        // 拖拽时取消高亮
        if (highlightOverlay != null)
        {
            StartHighlightFade(normalColor);
        }

        // 禁用所有Image组件的raycastTarget，防止阻挡射线检测
        DisableAllRaycastTargets();

        // 将物品临时放到 Canvas 下面，防止被 Mask 遮挡
        transform.SetParent(item.GetComponentInParent<Canvas>().transform, true);
        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!item.IsDraggable) return;

        // 直接设置物品位置为鼠标位置
        Vector2 localPointerPosition;
        Canvas canvas = item.GetComponentInParent<Canvas>();
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out localPointerPosition);

        rectTransform.anchoredPosition = localPointerPosition;

        // 检测当前是否在有效的放置区域中
        CheckValidDropZone(eventData);
    }

    private void CheckValidDropZone(PointerEventData eventData)
    {
        if (highlightOverlay == null) return;

        // 进行射线检测，看是否在有效的装备栏中
        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        bool isOverValidSlot = false;
        bool isOverValidGrid = false;

        foreach (var result in results)
        {
            // 检查装备栏
            EquipSlot equipSlot = result.gameObject.GetComponent<EquipSlot>();
            if (equipSlot != null)
            {
                var itemComponent = GetComponent<InventorySystemItem>();
                if (itemComponent != null && CanEquipToSlot(equipSlot, itemComponent))
                {
                    isOverValidSlot = true;
                    break;
                }
            }

            // 检查网格
            ItemGrid itemGrid = result.gameObject.GetComponent<ItemGrid>();
            if (itemGrid != null)
            {
                // 计算物品在网格中的位置
                Vector2Int gridPosition = itemGrid.GetTileGridPosition(eventData.position);
                var itemComponent = GetComponent<InventorySystemItem>();
                if (itemComponent != null)
                {
                    Vector2Int itemSize = new Vector2Int(itemComponent.Data.width, itemComponent.Data.height);

                    // 检查是否可以放置
                    isOverValidGrid = itemGrid.CanPlaceItem(gridPosition, itemSize);
                }
                break;
            }
        }

        // 根据检测结果设置高亮颜色
        Color targetColor;
        if (isOverValidSlot)
        {
            targetColor = validDropColor; // 绿色 - 可装备
        }
        else if (isOverValidGrid)
        {
            targetColor = validDropColor; // 绿色 - 可放置到网格
        }
        else
        {
            // 检查是否在任何网格区域内
            bool isOverAnyGrid = false;
            foreach (var result in results)
            {
                if (result.gameObject.GetComponent<ItemGrid>() != null)
                {
                    isOverAnyGrid = true;
                    break;
                }
            }

            if (isOverAnyGrid)
            {
                targetColor = Color.red.WithAlpha(0.35f); // 红色 - 在网格内但不能放置
            }
            else
            {
                targetColor = normalColor; // 透明 - 不在任何有效区域
            }
        }

        // 应用颜色变化
        if (highlightOverlay.color != targetColor)
        {
            StartHighlightFade(targetColor);
        }
    }

    private bool CanEquipToSlot(EquipSlot equipSlot, InventorySystemItem itemComponent)
    {
        // 通过反射获取装备栏接受的类型
        var acceptedTypeField = typeof(EquipSlot).GetField("acceptedType",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (acceptedTypeField != null)
        {
            var acceptedType = acceptedTypeField.GetValue(equipSlot);
            return acceptedType != null && acceptedType.Equals(itemComponent.Data.itemCategory);
        }
        return false;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!item.IsDraggable) return;

        isDragging = false;
        bool placementSuccessful = false;

        // 检查是否从装备栏拖拽出来
        EquipSlot originalEquipSlot = originalParent.GetComponent<EquipSlot>();
        if (originalEquipSlot != null && wasInEquipSlot)
        {
            // 通知装备栏物品被移除
            originalEquipSlot.OnItemRemoved();
        }

        // 使用射线检测来确定放置目标
        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        // 首先检查是否放置到装备栏
        foreach (var result in results)
        {
            EquipSlot equipSlot = result.gameObject.GetComponent<EquipSlot>();
            if (equipSlot != null)
            {
                var itemComponent = GetComponent<InventorySystemItem>();
                // 在装备成功的部分
                if (itemComponent != null && CanEquipToSlot(equipSlot, itemComponent))
                {
                    // 成功装备到装备栏
                    transform.SetParent(equipSlot.transform, false);
                    placementSuccessful = true;
                    FitToEquipSlot(equipSlot);

                    // 隐藏物品背景（装备时不显示背景）
                    if (itemBackground != null)
                    {
                        itemBackground.SetActive(false);
                    }
                    break;
                }
            }
        }

        // 如果没有装备成功，检查是否放置到网格中
        if (!placementSuccessful)
        {
            ItemGrid targetGrid = null;

            // 更全面的网格检测
            foreach (var result in results)
            {
                // 直接检查组件
                ItemGrid itemGrid = result.gameObject.GetComponent<ItemGrid>();
                if (itemGrid != null)
                {
                    targetGrid = itemGrid;
                    break;
                }

                // 检查父对象中是否有ItemGrid
                itemGrid = result.gameObject.GetComponentInParent<ItemGrid>();
                if (itemGrid != null)
                {
                    targetGrid = itemGrid;
                    break;
                }
            }

            // 如果通过射线检测没找到，尝试使用InventoryController的selectedItemGrid
            if (targetGrid == null)
            {
                InventoryController inventoryController = FindObjectOfType<InventoryController>();
                if (inventoryController != null && inventoryController.selectedItemGrid != null)
                {
                    // 检查鼠标是否在选中的网格范围内
                    GridInteract gridInteract = inventoryController.selectedItemGrid.GetComponent<GridInteract>();
                    if (gridInteract != null && gridInteract.IsMouseInThisGrid())
                    {
                        targetGrid = inventoryController.selectedItemGrid;
                    }
                }
            }

            if (targetGrid != null)
            {
                // 计算放置位置
                Vector2Int dropPosition = targetGrid.GetTileGridPosition(eventData.position);
                Vector2Int itemSize = new Vector2Int(item.Data.width, item.Data.height);

                Debug.Log($"尝试放置到网格位置: ({dropPosition.x}, {dropPosition.y}), 物品大小: ({itemSize.x}, {itemSize.y})");

                // 检查是否可以放置
                if (targetGrid.CanPlaceItem(dropPosition, itemSize))
                {
                    // 直接调用ItemGrid的PlaceItem方法来确保坐标计算一致
                    var placeItemMethod = typeof(ItemGrid).GetMethod("PlaceItem",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (placeItemMethod != null)
                    {
                        // 先设置父对象
                        transform.SetParent(targetGrid.transform, false);

                        // 调用PlaceItem方法
                        placeItemMethod.Invoke(targetGrid, new object[] { gameObject, dropPosition, itemSize });

                        placementSuccessful = true;

                        // 恢复原始大小
                        RestoreOriginalSize();

                        // 显示物品背景
                        if (itemBackground != null)
                        {
                            itemBackground.SetActive(true);
                        }

                        Debug.Log($"物品从装备栏成功放置到网格位置: ({dropPosition.x}, {dropPosition.y})");
                    }
                    else
                    {
                        Debug.LogError("无法找到ItemGrid的PlaceItem方法");
                    }
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

        // 如果没有成功放置到任何地方，返回原位置
        if (!placementSuccessful)
        {
            transform.SetParent(originalParent, false);
            rectTransform.anchoredPosition = originalPos;

            if (wasInEquipSlot)
            {
                // 返回装备栏 - 重用已声明的originalEquipSlot变量
                if (originalEquipSlot != null)
                {
                    FitToEquipSlot(originalEquipSlot);
                }

                // 隐藏背景
                if (itemBackground != null)
                {
                    itemBackground.SetActive(false);
                }
            }
            else
            {
                // 返回网格，需要重新调用PlaceItem来恢复网格状态
                ItemGrid parentGrid = originalParent.GetComponent<ItemGrid>();
                if (parentGrid != null)
                {
                    // 计算原始位置对应的网格坐标
                    Vector2Int gridPos = parentGrid.GetTileGridPosition(originalPos + (Vector2)parentGrid.transform.position);
                    Vector2Int itemSize = new Vector2Int(item.Data.width, item.Data.height);

                    // 使用反射调用PlaceItem方法来正确恢复状态
                    var placeItemMethod = typeof(ItemGrid).GetMethod("PlaceItem",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (placeItemMethod != null)
                    {
                        placeItemMethod.Invoke(parentGrid, new object[] { gameObject, gridPos, itemSize });
                    }
                }

                // 恢复原始大小
                RestoreOriginalSize();

                // 显示物品背景
                if (itemBackground != null)
                {
                    itemBackground.SetActive(true);
                }
            }

            // 如果成功放置到新位置，且新位置是装备栏，则处理背包网格生成
            if (placementSuccessful)
            {
                EquipSlot newEquipSlot = transform.parent.GetComponent<EquipSlot>();
                if (newEquipSlot != null)
                {
                    var itemComponent = GetComponent<InventorySystemItem>();
                    if (itemComponent != null && itemComponent.Data.itemCategory == InventorySystemItemCategory.Backpack)
                    {
                        // 背包网格的创建已经在EquipSlot.OnDrop中处理了
                        Debug.Log("背包已装备，网格应该已经生成");
                    }
                }
            }
        }

        // 恢复所有Image组件的raycastTarget状态
        RestoreAllRaycastTargets();

        // 恢复正常高亮状态
        if (highlightOverlay != null)
        {
            StartHighlightFade(normalColor);
        }

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
    }

    // 调整物品大小以适应装备栏
    private void FitToEquipSlot(EquipSlot equipSlot)
    {
        RectTransform slotRect = equipSlot.GetComponent<RectTransform>();
        if (slotRect == null) return;

        // 获取装备栏的间距设置（通过反射获取私有字段）
        var paddingField = typeof(EquipSlot).GetField("padding",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        float padding = paddingField != null ? (float)paddingField.GetValue(equipSlot) : 10f;

        // 计算装备栏可用空间（减去间距）
        Vector2 availableSize = slotRect.sizeDelta - new Vector2(padding * 2, padding * 2);

        // 计算缩放比例，保持宽高比
        float scaleX = availableSize.x / originalSize.x;
        float scaleY = availableSize.y / originalSize.y;
        float scale = Mathf.Min(scaleX, scaleY, 1f); // 不放大，只缩小

        // 使用localScale进行缩放，这样会影响所有子对象
        rectTransform.localScale = Vector3.one * scale;

        // 居中定位
        rectTransform.anchoredPosition = Vector2.zero;
    }

    // 恢复物品原始大小
    private void RestoreOriginalSize()
    {
        rectTransform.sizeDelta = originalSize;
        rectTransform.localScale = originalScale;
    }

    // 当物品从装备栏被移除时调用（由EquipSlot调用）
    public void OnRemovedFromEquipSlot()
    {
        // 恢复原始大小
        RestoreOriginalSize();

        // 显示物品背景
        if (itemBackground != null)
        {
            itemBackground.SetActive(true);
        }

        // 恢复正常高亮状态
        if (highlightOverlay != null)
        {
            StartHighlightFade(normalColor);
        }
    }

    private void StartHighlightFade(Color targetColor)
    {
        if (currentFadeCoroutine != null)
        {
            StopCoroutine(currentFadeCoroutine);
        }
        currentFadeCoroutine = StartCoroutine(FadeHighlight(targetColor));
    }

    private IEnumerator FadeHighlight(Color target)
    {
        if (highlightOverlay == null) yield break;

        Color start = highlightOverlay.color;
        for (float t = 0; t < fadeTime; t += Time.unscaledDeltaTime)
        {
            highlightOverlay.color = Color.Lerp(start, target, t / fadeTime);
            yield return null;
        }
        highlightOverlay.color = target;
    }

    // 恢复原始锚点设置
    private void RestoreOriginalAnchor()
    {
        rectTransform.anchorMin = originalAnchorMin;
        rectTransform.anchorMax = originalAnchorMax;
        rectTransform.pivot = originalPivot;
    }
}