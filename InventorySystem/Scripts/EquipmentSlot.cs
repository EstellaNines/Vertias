using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using InventorySystem;
using System.Collections.Generic;

/// <summary>
/// 装备槽位组件 - 处理装备的拖拽接收和类型验证
/// </summary>
public class EquipmentSlot : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("装备槽位设置")]
    [SerializeField] private ItemCategory acceptedItemType; // 接受的装备类型
    [SerializeField] private Image slotBackground; // 槽位背景图片
    [SerializeField] private Color normalColor = Color.white; // 正常颜色
    [SerializeField] private Color highlightColor = Color.yellow; // 高亮颜色
    [SerializeField] private Color validDropColor = Color.green; // 有效放置颜色
    [SerializeField] private Color invalidDropColor = Color.red; // 无效放置颜色

    [Header("槽位边距设置")]
    [SerializeField] private float slotMargin = 6f; // 槽位边距

    private GameObject currentEquippedItem; // 当前装备的物品
    private RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        if (slotBackground == null)
        {
            slotBackground = GetComponent<Image>();
        }
    }

    private void Start()
    {
        // 设置初始颜色
        if (slotBackground != null)
        {
            slotBackground.color = normalColor;
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        // 获取被拖拽的物品
        DraggableItem draggedItem = eventData.pointerDrag?.GetComponent<DraggableItem>();

        if (draggedItem != null)
        {
            ItemDataReader itemDataReader = draggedItem.GetItemDataReader();

            if (itemDataReader != null && CanAcceptItem(itemDataReader.ItemData))
            {
                // 如果槽位已有装备，将其移回原位置或交换
                if (currentEquippedItem != null)
                {
                    HandleItemSwap(draggedItem);
                }
                else
                {
                    // 装备新物品
                    EquipItem(draggedItem.gameObject);
                }
            }
            else
            {
                // 类型不匹配，物品返回原位置
                Debug.Log($"装备类型不匹配！槽位接受: {acceptedItemType}, 物品类型: {itemDataReader?.ItemData?.category}");
            }
        }

        // 恢复槽位颜色
        if (slotBackground != null)
        {
            slotBackground.color = normalColor;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // 检查是否正在拖拽物品
        if (eventData.pointerDrag != null)
        {
            DraggableItem draggedItem = eventData.pointerDrag.GetComponent<DraggableItem>();
            if (draggedItem != null)
            {
                ItemDataReader itemDataReader = draggedItem.GetItemDataReader();

                // 根据是否可以接受该物品来改变颜色
                if (slotBackground != null)
                {
                    if (CanAcceptItem(itemDataReader?.ItemData))
                    {
                        slotBackground.color = validDropColor;
                    }
                    else
                    {
                        slotBackground.color = invalidDropColor;
                    }
                }
            }
        }
        else
        {
            // 鼠标悬停高亮
            if (slotBackground != null)
            {
                slotBackground.color = highlightColor;
            }
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // 恢复正常颜色
        if (slotBackground != null)
        {
            slotBackground.color = normalColor;
        }
    }

    /// <summary>
    /// 检查是否可以接受该物品
    /// </summary>
    /// <param name="itemData">物品数据</param>
    /// <returns>是否可以接受</returns>
    private bool CanAcceptItem(ItemDataSO itemData)
    {
        if (itemData == null) return false;
        return itemData.category == acceptedItemType;
    }

    /// <summary>
    /// 装备物品到槽位
    /// </summary>
    /// <param name="item">要装备的物品</param>
    private void EquipItem(GameObject item)
    {
        currentEquippedItem = item;

        // 设置物品的父对象为当前槽位
        item.transform.SetParent(transform);

        // 调整物品大小和位置
        ResizeItemToFitSlot(item);

        Debug.Log($"装备了物品: {item.name}");
    }

    /// <summary>
    /// 卸下当前装备的物品
    /// </summary>
    public void UnequipItem()
    {
        if (currentEquippedItem != null)
        {
            // 恢复原始大小
            RestoreItemOriginalSize(currentEquippedItem);

            // 可以在这里添加将物品移回背包的逻辑
            currentEquippedItem = null;
        }
    }

    /// <summary>
    /// 处理物品交换
    /// </summary>
    /// <param name="newItem">新物品</param>
    private void HandleItemSwap(DraggableItem newItem)
    {
        // 获取当前装备的物品的DraggableItem组件
        DraggableItem currentDraggable = currentEquippedItem.GetComponent<DraggableItem>();

        if (currentDraggable != null)
        {
            // 将当前装备移回新物品的原始位置
            Transform newItemOriginalParent = newItem.transform.parent;
            Vector2 newItemOriginalPosition = newItem.GetComponent<RectTransform>().anchoredPosition;

            // 移动当前装备到新物品的原始位置
            currentEquippedItem.transform.SetParent(newItemOriginalParent);
            currentEquippedItem.GetComponent<RectTransform>().anchoredPosition = newItemOriginalPosition;

            // 恢复当前装备的原始大小
            RestoreItemOriginalSize(currentEquippedItem);
        }

        // 装备新物品
        EquipItem(newItem.gameObject);
    }

    /// <summary>
    /// 调整物品大小以适应槽位
    /// </summary>
    /// <param name="item">要调整的物品</param>
    private void ResizeItemToFitSlot(GameObject item)
    {
        RectTransform itemRect = item.GetComponent<RectTransform>();
        if (itemRect != null)
        {
            // 保存原始大小信息
            ItemSizeInfo sizeInfo = item.GetComponent<ItemSizeInfo>();
            if (sizeInfo == null)
            {
                sizeInfo = item.AddComponent<ItemSizeInfo>();
                sizeInfo.originalSize = itemRect.sizeDelta;
                sizeInfo.originalScale = itemRect.localScale;
                // 保存原始锚点和轴心点信息
                sizeInfo.originalAnchorMin = itemRect.anchorMin;
                sizeInfo.originalAnchorMax = itemRect.anchorMax;
                sizeInfo.originalPivot = itemRect.pivot;
                sizeInfo.originalAnchoredPosition = itemRect.anchoredPosition;
            }

            // 计算新的大小（槽位大小减去边距）
            Vector2 slotSize = rectTransform.sizeDelta;
            Vector2 newSize = new Vector2(slotSize.x - slotMargin * 2, slotSize.y - slotMargin * 2);

            // 设置新的大小
            itemRect.sizeDelta = newSize;
            itemRect.localScale = Vector3.one;

            // 根据轴心点计算正确的居中位置
            // 对于轴心为(0,1)的物品，居中坐标应该是(-114, 114)
            Vector2 pivot = itemRect.pivot;
            Vector2 centeredPosition = new Vector2(
                (pivot.x - 0.5f) * newSize.x,
                (pivot.y - 0.5f) * newSize.y
            );

            itemRect.anchoredPosition = centeredPosition;

            // 调整子对象（图标）的大小
            ResizeChildImages(item, newSize);
        }
    }

    /// <summary>
    /// 调整子对象图片的大小
    /// </summary>
    /// <param name="item">物品对象</param>
    /// <param name="newSize">新的大小</param>
    private void ResizeChildImages(GameObject item, Vector2 newSize)
    {
        Image[] childImages = item.GetComponentsInChildren<Image>();

        foreach (Image childImage in childImages)
        {
            if (childImage.gameObject != item) // 不调整自身
            {
                RectTransform childRect = childImage.GetComponent<RectTransform>();
                if (childRect != null)
                {
                    // 保存原始大小信息
                    ItemSizeInfo childSizeInfo = childImage.GetComponent<ItemSizeInfo>();
                    if (childSizeInfo == null)
                    {
                        childSizeInfo = childImage.gameObject.AddComponent<ItemSizeInfo>();
                        childSizeInfo.originalSize = childRect.sizeDelta;
                        childSizeInfo.originalScale = childRect.localScale;
                        // 保存子对象的原始锚点和轴心点信息
                        childSizeInfo.originalAnchorMin = childRect.anchorMin;
                        childSizeInfo.originalAnchorMax = childRect.anchorMax;
                        childSizeInfo.originalPivot = childRect.pivot;
                        childSizeInfo.originalAnchoredPosition = childRect.anchoredPosition;
                    }

                    // 设置子对象大小为新的大小
                    childRect.sizeDelta = newSize;

                    // 根据子对象的轴心点计算正确的居中位置
                    // 对于轴心为(0,1)的子对象，居中坐标应该是(-114, 114)
                    Vector2 childPivot = childRect.pivot;
                    Vector2 childCenteredPosition = new Vector2(
                        (childPivot.x - 0.5f) * newSize.x,
                        (childPivot.y - 0.5f) * newSize.y
                    );

                    childRect.anchoredPosition = childCenteredPosition;
                }
            }
        }
    }

    /// <summary>
    /// 恢复物品的原始大小
    /// </summary>
    /// <param name="item">要恢复的物品</param>
    private void RestoreItemOriginalSize(GameObject item)
    {
        ItemSizeInfo sizeInfo = item.GetComponent<ItemSizeInfo>();
        if (sizeInfo != null)
        {
            RectTransform itemRect = item.GetComponent<RectTransform>();
            if (itemRect != null)
            {
                // 恢复所有原始属性
                itemRect.sizeDelta = sizeInfo.originalSize;
                itemRect.localScale = sizeInfo.originalScale;
                itemRect.anchorMin = sizeInfo.originalAnchorMin;
                itemRect.anchorMax = sizeInfo.originalAnchorMax;
                itemRect.pivot = sizeInfo.originalPivot;
                itemRect.anchoredPosition = sizeInfo.originalAnchoredPosition;
            }

            // 恢复子对象大小
            RestoreChildImagesSizes(item);
        }
    }

    /// <summary>
    /// 恢复子对象图片的原始大小
    /// </summary>
    /// <param name="item">物品对象</param>
    private void RestoreChildImagesSizes(GameObject item)
    {
        ItemSizeInfo[] childSizeInfos = item.GetComponentsInChildren<ItemSizeInfo>();

        foreach (ItemSizeInfo sizeInfo in childSizeInfos)
        {
            if (sizeInfo.gameObject != item) // 不处理自身
            {
                RectTransform childRect = sizeInfo.GetComponent<RectTransform>();
                if (childRect != null)
                {
                    // 恢复子对象的所有原始属性
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

    /// <summary>
    /// 获取当前装备的物品
    /// </summary>
    /// <returns>当前装备的物品</returns>
    public GameObject GetCurrentEquippedItem()
    {
        return currentEquippedItem;
    }

    /// <summary>
    /// 设置接受的装备类型
    /// </summary>
    /// <param name="itemType">装备类型</param>
    public void SetAcceptedItemType(ItemCategory itemType)
    {
        acceptedItemType = itemType;
    }
}