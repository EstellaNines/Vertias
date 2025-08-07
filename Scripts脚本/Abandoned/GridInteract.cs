using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(ItemGrid))]
public class GridInteract : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private InventoryController inventoryController;
    private ItemGrid itemGrid;

    private void Awake()
    {
        inventoryController = FindObjectOfType<InventoryController>();
        itemGrid = GetComponent<ItemGrid>();
    }

    // 鼠标进入触发
    public void OnPointerEnter(PointerEventData eventData)
    {
        inventoryController.selectedItemGrid = itemGrid;
    }

    // 鼠标退出触发 - 修改：不立即清空selectedItemGrid
    public void OnPointerExit(PointerEventData eventData)
    {

    }
}