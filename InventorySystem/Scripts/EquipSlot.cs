using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 挂在装备栏的每个格子上，负责接收拖拽过来的物品
/// </summary>
public class EquipSlot : MonoBehaviour, IDropHandler
{
    [Header("这个格子只能装什么类型？")]
    [SerializeField] private InventorySystemItemCategory acceptedType;
    
    [Header("装备栏适配设置")]
    [SerializeField] private float padding = 10f; // 间距
    [SerializeField] private bool autoResize = true; // 是否自动调整大小

    // 当前格子已装备的物体（可为空）
    private DraggableItem equippedItem;

    public void OnDrop(PointerEventData eventData)
    {
        var dropped = eventData.pointerDrag;
        if (dropped == null) return;

        var draggable = dropped.GetComponent<DraggableItem>();
        if (draggable == null) return;

        var item = draggable.GetComponent<InventorySystemItem>();
        if (item == null || item.Data.itemCategory != acceptedType) return;

        // 如果格子已有物品，交换
        if (equippedItem != null)
        {
            // 把旧装备放回背包，恢复原始大小
            RestoreOriginalSize(equippedItem);
            equippedItem.transform.SetParent(draggable.originalParent, false);
            equippedItem.GetComponent<RectTransform>().anchoredPosition = draggable.originalPos;
            equippedItem.GetComponent<CanvasGroup>().blocksRaycasts = true;
        }

        // 把新物品放进格子
        draggable.transform.SetParent(transform, false);
        
        // 自动适配装备栏大小
        if (autoResize)
        {
            FitToEquipSlot(draggable);
        }
        else
        {
            draggable.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        }
        
        draggable.GetComponent<CanvasGroup>().blocksRaycasts = true;
        equippedItem = draggable;
    }
    
    /// 让物品适配装备栏大小，保持间距
    private void FitToEquipSlot(DraggableItem draggable)
    {
        RectTransform slotRect = GetComponent<RectTransform>();
        RectTransform itemRect = draggable.GetComponent<RectTransform>();
        
        if (slotRect == null || itemRect == null) return;
        
        // 存储原始大小和缩放，用于恢复
        ItemSizeData sizeData = draggable.GetComponent<ItemSizeData>();
        if (sizeData == null)
        {
            sizeData = draggable.gameObject.AddComponent<ItemSizeData>();
        }
        sizeData.originalSize = itemRect.sizeDelta;
        sizeData.originalScale = itemRect.localScale;
        
        // 计算装备栏可用空间（减去间距）
        Vector2 availableSize = slotRect.sizeDelta - new Vector2(padding * 2, padding * 2);
        
        // 获取物品原始大小
        Vector2 originalSize = sizeData.originalSize;
        
        // 计算缩放比例，保持宽高比
        float scaleX = availableSize.x / originalSize.x;
        float scaleY = availableSize.y / originalSize.y;
        float scale = Mathf.Min(scaleX, scaleY, 1f); // 不放大，只缩小
        
        // 使用localScale进行缩放，这样会影响所有子对象
        itemRect.localScale = Vector3.one * scale;
        
        // 居中定位
        itemRect.anchoredPosition = Vector2.zero;
    }
    
    /// 恢复物品原始大小
    private void RestoreOriginalSize(DraggableItem draggable)
    {
        if (draggable == null) return;
        
        ItemSizeData sizeData = draggable.GetComponent<ItemSizeData>();
        RectTransform itemRect = draggable.GetComponent<RectTransform>();
        
        if (sizeData != null && itemRect != null)
        {
            itemRect.sizeDelta = sizeData.originalSize;
            itemRect.localScale = sizeData.originalScale;
        }
    }
}

/// 用于存储物品原始大小数据的组件
public class ItemSizeData : MonoBehaviour
{
    [HideInInspector] public Vector2 originalSize;
    [HideInInspector] public Vector3 originalScale;
}