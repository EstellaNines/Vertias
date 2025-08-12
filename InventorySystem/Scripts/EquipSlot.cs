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
    [SerializeField] private float padding = 10f;
    [SerializeField] private bool autoResize = true;

    // 网格容器引用
    [Header("网格容器设置")]
    [SerializeField] private Transform backpackContainer; // BackpackContainer引用
    [SerializeField] private Transform tacticalRigContainer; // TacticalRigContainer引用

    // 添加预制体引用
    [Header("网格预制体引用")]
    [SerializeField] private GameObject backpackGridPrefab; // BackpackItemGrid预制体
    [SerializeField] private GameObject tacticalRigGridPrefab; // TacticalRigItemGrid预制体
    
    private DraggableItem equippedItem;
    private GameObject currentBackpackGrid; // 当前实例化的背包网格
    private GameObject currentTacticalRigGrid; // 当前实例化的战术挂具网格

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
            // 移除旧装备时清空对应网格
            RemoveEquippedItemGrid(equippedItem);
            
            RestoreOriginalSize(equippedItem);
            equippedItem.transform.SetParent(draggable.originalParent, false);
            equippedItem.GetComponent<RectTransform>().anchoredPosition = draggable.originalPos;
            equippedItem.GetComponent<CanvasGroup>().blocksRaycasts = true;
        }

        // 把新物品放进格子
        draggable.transform.SetParent(transform, false);
        
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
        
        // 装备新物品时设置对应网格
        SetEquippedItemGrid(item);
    }
    
    // 设置装备物品的网格
    private void SetEquippedItemGrid(InventorySystemItem item)
    {
        if (item.Data.itemCategory == InventorySystemItemCategory.Backpack && backpackContainer != null)
        {
            // 先清理现有的网格
            if (currentBackpackGrid != null)
            {
                DestroyImmediate(currentBackpackGrid);
            }
            
            // 实例化新的背包网格
            if (backpackGridPrefab != null)
            {
                currentBackpackGrid = Instantiate(backpackGridPrefab, backpackContainer);
                
                // 设置网格位置和锚点
                RectTransform gridRect = currentBackpackGrid.GetComponent<RectTransform>();
                if (gridRect != null)
                {
                    gridRect.anchorMin = new Vector2(0, 1); // 左上角锚点
                    gridRect.anchorMax = new Vector2(0, 1);
                    gridRect.pivot = new Vector2(0, 1);
                    gridRect.anchoredPosition = Vector2.zero; // 定位在容器左上角
                }
                
                // 获取BackpackItemGrid组件并设置数据
                BackpackItemGrid backpackGrid = currentBackpackGrid.GetComponent<BackpackItemGrid>();
                if (backpackGrid != null)
                {
                    backpackGrid.SetBackpackData(item.Data);
                    Debug.Log($"背包 {item.Data.itemName} 已装备，网格已实例化，尺寸: {item.Data.CellH}x{item.Data.CellV}");
                }
                else
                {
                    Debug.LogError("BackpackItemGrid预制体缺少BackpackItemGrid组件！");
                }
            }
            else
            {
                Debug.LogError("BackpackItemGrid预制体引用为空！请在EquipSlot中设置backpackGridPrefab");
            }
        }
        else if (item.Data.itemCategory == InventorySystemItemCategory.TacticalRig && tacticalRigContainer != null)
        {
            // 先清理现有的网格
            if (currentTacticalRigGrid != null)
            {
                DestroyImmediate(currentTacticalRigGrid);
            }
            
            // 实例化新的战术挂具网格
            if (tacticalRigGridPrefab != null)
            {
                currentTacticalRigGrid = Instantiate(tacticalRigGridPrefab, tacticalRigContainer);
                
                // 设置网格位置和锚点
                RectTransform gridRect = currentTacticalRigGrid.GetComponent<RectTransform>();
                if (gridRect != null)
                {
                    gridRect.anchorMin = new Vector2(0, 1); // 左上角锚点
                    gridRect.anchorMax = new Vector2(0, 1);
                    gridRect.pivot = new Vector2(0, 1);
                    gridRect.anchoredPosition = Vector2.zero; // 定位在容器左上角
                }
                
                // 获取TactiaclRigItemGrid组件并设置数据
                TactiaclRigItemGrid tacticalRigGrid = currentTacticalRigGrid.GetComponent<TactiaclRigItemGrid>();
                if (tacticalRigGrid != null)
                {
                    tacticalRigGrid.SetTacticalRigData(item.Data);
                    Debug.Log($"战术挂具 {item.Data.itemName} 已装备，网格已实例化，尺寸: {item.Data.CellH}x{item.Data.CellV}");
                }
                else
                {
                    Debug.LogError("TacticalRigItemGrid预制体缺少TactiaclRigItemGrid组件！");
                }
            }
            else
            {
                Debug.LogError("TacticalRigItemGrid预制体引用为空！请在EquipSlot中设置tacticalRigGridPrefab");
            }
        }
    }
    
    // 移除装备物品的网格
    private void RemoveEquippedItemGrid(DraggableItem item)
    {
        var itemComponent = item.GetComponent<InventorySystemItem>();
        if (itemComponent == null) return;
        
        if (itemComponent.Data.itemCategory == InventorySystemItemCategory.Backpack)
        {
            if (currentBackpackGrid != null)
            {
                DestroyImmediate(currentBackpackGrid);
                currentBackpackGrid = null;
                Debug.Log("背包已卸下，网格已销毁");
            }
        }
        else if (itemComponent.Data.itemCategory == InventorySystemItemCategory.TacticalRig)
        {
            if (currentTacticalRigGrid != null)
            {
                DestroyImmediate(currentTacticalRigGrid);
                currentTacticalRigGrid = null;
                Debug.Log("战术挂具已卸下，网格已销毁");
            }
        }
    }
    
    public void OnItemRemoved()
    {
        if (equippedItem != null)
        {
            RemoveEquippedItemGrid(equippedItem);
        }
        equippedItem = null;
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

    // 获取当前装备的物品
    public DraggableItem GetEquippedItem()
    {
        return equippedItem;
    }

    public void ReequipItem(DraggableItem draggable)
    {
        equippedItem = draggable;
        var item = draggable.GetComponent<InventorySystemItem>();
        if (item != null)
        {
            SetEquippedItemGrid(item);
        }
        FitToEquipSlot(draggable);
        draggable.GetComponent<CanvasGroup>().blocksRaycasts = true;
    }
}

/// 用于存储物品原始大小数据的组件
public class ItemSizeData : MonoBehaviour
{
    [HideInInspector] public Vector2 originalSize;
    [HideInInspector] public Vector3 originalScale;
}