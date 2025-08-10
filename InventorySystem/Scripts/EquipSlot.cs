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

    [Header("背包网格生成设置")]
    [SerializeField] private Transform backpackGridParent; // 背包网格的父对象
    [SerializeField] private Sprite gridSprite; // Grid格子64x64精灵
    [SerializeField] private GridConfig defaultGridConfig; // 默认网格配置

    // 当前格子已装备的物体（可为空）
    private DraggableItem equippedItem;
    // 当前生成的背包网格
    private GameObject currentBackpackGrid;
    // 动态网格管理器
    private DynamicBackpackGridManager dynamicGridManager;

    private void Awake()
    {
        // 获取或创建动态网格管理器
        dynamicGridManager = FindObjectOfType<DynamicBackpackGridManager>();
        if (dynamicGridManager == null)
        {
            GameObject managerObj = new GameObject("DynamicBackpackGridManager");
            dynamicGridManager = managerObj.AddComponent<DynamicBackpackGridManager>();
        }

        // 设置网格精灵
        if (gridSprite != null)
        {
            dynamicGridManager.SetGridSprite(gridSprite);
        }
    }

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
            // 移除旧装备的网格
            RemoveBackpackGrid();
            
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

        // 如果装备的是背包，生成对应的网格
        if (item.Data.itemCategory == InventorySystemItemCategory.Backpack)
        {
            CreateBackpackGrid(item.Data);
        }
    }

    /// <summary>
    /// 创建背包网格
    /// </summary>
    private void CreateBackpackGrid(InventorySystemItemDataSO backpackData)
    {
        if (backpackGridParent == null || !backpackData.IsContainer())
        {
            Debug.LogWarning("无法创建背包网格：缺少父对象或物品不是容器类型");
            return;
        }

        // 使用动态网格管理器创建网格
        currentBackpackGrid = dynamicGridManager.CreateBackpackGrid(
            backpackGridParent, 
            backpackData.CellH, 
            backpackData.CellV,
            defaultGridConfig
        );

        Debug.Log($"为背包 {backpackData.itemName} 创建了 {backpackData.CellH}x{backpackData.CellV} 的网格");
    }

    /// <summary>
    /// 移除背包网格
    /// </summary>
    private void RemoveBackpackGrid()
    {
        if (currentBackpackGrid != null)
        {
            dynamicGridManager.DestroyBackpackGrid(currentBackpackGrid);
            currentBackpackGrid = null;
        }
    }

    /// <summary>
    /// 当物品从装备栏移除时调用
    /// </summary>
    public void OnItemRemoved()
    {
        RemoveBackpackGrid();
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

    // 获取当前生成的背包网格
    public GameObject GetCurrentBackpackGrid()
    {
        return currentBackpackGrid;
    }
}

/// 用于存储物品原始大小数据的组件
public class ItemSizeData : MonoBehaviour
{
    [HideInInspector] public Vector2 originalSize;
    [HideInInspector] public Vector3 originalScale;
}