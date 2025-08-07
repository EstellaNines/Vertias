using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class BackpackEquipSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("装备栏设置")]
    [SerializeField] private Image slotImage;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color highlightColor = Color.yellow;
    [SerializeField] private Color validDropColor = Color.green;
    [SerializeField] private Color invalidDropColor = Color.red;

    [Header("当前装备的背包")]
    public BackpackItem equippedBackpack;

    private InventoryController inventoryController;
    private bool isDragging = false;

    private void Start()
    {
        inventoryController = FindObjectOfType<InventoryController>();

        if (slotImage == null)
            slotImage = GetComponent<Image>();

        slotImage.color = normalColor;
        
        // 确保Image组件启用了Raycast Target
        if (slotImage != null)
        {
            slotImage.raycastTarget = true;
        }
    }

    private void Update()
    {
        // 检测是否有物品被拖拽到装备栏上方
        if (inventoryController != null)
        {
            BaseItem draggedItem = inventoryController.GetSelectedItem();
            if (draggedItem != null && IsMouseOverSlot())
            {
                // 检测鼠标释放
                if (Input.GetMouseButtonUp(0))
                {
                    HandleItemDrop(draggedItem);
                }
            }
        }
    }
    
    private bool IsMouseOverSlot()
    {
        if (slotImage == null) return false;
        
        RectTransform rectTransform = slotImage.rectTransform;
        Vector2 mousePosition = Input.mousePosition;
        
        return RectTransformUtility.RectangleContainsScreenPoint(
            rectTransform, mousePosition, 
            inventoryController.GetComponent<Canvas>()?.worldCamera);
    }
    
    private void HandleItemDrop(BaseItem draggedItem)
    {
        Debug.Log("检测到物品放置到装备栏");
        
        // 检查是否是背包类物品
        if (draggedItem.GetItemCategory() != ItemCategory.背包)
        {
            Debug.Log("只能装备背包类物品");
            return;
        }

        BackpackItem backpackItem = draggedItem as BackpackItem;
        if (backpackItem == null)
        {
            Debug.Log("物品不是背包类型");
            return;
        }

        // 装备背包
        EquipBackpack(backpackItem);
        
        // 清除InventoryController中的选中状态
        inventoryController.ClearSelectedItem();
    }

    private void EquipBackpack(BackpackItem backpack)
    {
        // 如果已经有装备的背包，先卸下
        if (equippedBackpack != null)
        {
            UnequipBackpack();
        }

        // 装备新背包
        equippedBackpack = backpack;

        // 从背包网格中移除物品
        RemoveFromInventoryGrid(backpack);

        // 将背包设置为装备栏的子对象
        backpack.transform.SetParent(this.transform, false);
        
        // 设置背包在装备栏中的位置（居中显示）
        RectTransform backpackRect = backpack.GetComponent<RectTransform>();
        if (backpackRect != null)
        {
            backpackRect.anchoredPosition = Vector2.zero;
            backpackRect.localScale = Vector3.one;
        }

        // 显示背包
        backpack.gameObject.SetActive(true);
        backpack.ShowBackground();

        // 应用背包属性（扩展背包容量等）
        ApplyBackpackStats(backpack);

        Debug.Log($"装备背包: {backpack.itemData.name}");
    }

    private void UnequipBackpack()
    {
        if (equippedBackpack == null) return;
    
        // 尝试将背包放回背包网格
        if (inventoryController != null)
        {
            ReturnBackpackToInventory(equippedBackpack);
        }
    
        // 重置装备栏显示
        slotImage.sprite = null;
        slotImage.color = normalColor;
    
        equippedBackpack = null;
    }

    private void RemoveFromInventoryGrid(BackpackItem backpack)
    {
        // 从背包网格中移除物品
        if (inventoryController != null)
        {
            ItemGrid itemGrid = inventoryController.selectedItemGrid;
            if (itemGrid != null)
            {
                itemGrid.PickUpItem(backpack.onGridPositionX, backpack.onGridPositionY);
            }
        }

        // 隐藏物品
        backpack.gameObject.SetActive(false);
    }

    private void ReturnBackpackToInventory(BackpackItem backpack)
    {
        // 尝试放回背包网格
        if (inventoryController != null)
        {
            if (inventoryController.TryReturnItemToGrid(backpack))
            {
                Debug.Log("背包成功放回背包网格");
            }
            else
            {
                Debug.LogWarning("无法将背包放回背包网格，背包已满");
                // 如果无法放回，可以考虑掉落到地面或其他处理方式
                // 这里暂时重新装备背包
                equippedBackpack = backpack;
            }
        }
    }

    private void ApplyBackpackStats(BackpackItem backpack)
    {
        // 这里可以实现背包属性的应用
        // 比如扩展背包容量、修改背包网格大小等
        if (backpack.backpackStats != null)
        {
            Debug.Log($"应用背包属性 - 容量: {backpack.backpackStats.capacity}, 网格: {backpack.backpackStats.gridWidth}x{backpack.backpackStats.gridHeight}");
            // 实际的背包扩展逻辑可以在这里实现
        }
    }

    // 实现IPointerEnterHandler接口
    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log("鼠标进入装备栏");
        
        // 检查是否正在拖拽背包物品
        BaseItem draggedItem = GetDraggedItem();
        if (draggedItem != null)
        {
            isDragging = true;
            if (draggedItem.GetItemCategory() == ItemCategory.背包)
            {
                slotImage.color = validDropColor;
                Debug.Log("可以放置背包");
            }
            else
            {
                slotImage.color = invalidDropColor;
                Debug.Log("无法放置此物品");
            }
        }
        else
        {
            slotImage.color = highlightColor;
        }
    }

    // 实现IPointerExitHandler接口
    public void OnPointerExit(PointerEventData eventData)
    {
        Debug.Log("鼠标离开装备栏");
        
        // 重置颜色，除非正在进行拖拽操作
        BaseItem draggedItem = GetDraggedItem();
        if (draggedItem == null)
        {
            slotImage.color = normalColor;
            isDragging = false;
        }
    }

    // 实现IPointerClickHandler接口
    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log($"点击装备栏，按钮: {eventData.button}");
        
        if (eventData.button == PointerEventData.InputButton.Right && equippedBackpack != null)
        {
            Debug.Log("右键卸下背包");
            UnequipBackpack();
        }
    }

    private BaseItem GetDraggedItem()
    {
        // 从InventoryController获取当前选中的物品
        if (inventoryController != null)
        {
            BaseItem selectedItem = inventoryController.GetSelectedItem();
            if (selectedItem != null)
            {
                Debug.Log($"获取到选中物品: {selectedItem.itemData.name}");
            }
            return selectedItem;
        }
        return null;
    }
}