using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(InventoryGridInteractionManager))]
public class GridInteractOfInventorySystem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    InventorySystemScreenController ISController;
    InventoryGridInteractionManager interactionManager;

    private void Awake()
    {
        ISController = FindObjectOfType<InventorySystemScreenController>();
        interactionManager = GetComponent<InventoryGridInteractionManager>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // 这里需要根据您的具体需求来实现
        // 如果 InventorySystemScreenController 有 selectInteractionManager 属性
        // ISController.selectInteractionManager = interactionManager;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // ISController.selectInteractionManager = null;
    }
}
