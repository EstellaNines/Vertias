using UnityEngine;
using UnityEngine.EventSystems;

public class GridInteract : MonoBehaviour
{
    private InventoryController inventoryController;
    private ItemGrid itemGrid;
    private RectTransform rectTransform;
    private Canvas canvas;
    private bool wasInGrid = false;

    private void Awake()
    {
        inventoryController = FindObjectOfType<InventoryController>();
        itemGrid = GetComponent<ItemGrid>();
        
        if (itemGrid == null)
        {
            Debug.LogError($"GridInteract on {gameObject.name} requires an ItemGrid component!");
        }

        rectTransform = GetComponent<RectTransform>();
        canvas = FindObjectOfType<Canvas>();
    }

    private void Update()
    {
        if (inventoryController == null || itemGrid == null) return;

        Vector2 mousePosition = Input.mousePosition;
        bool isInGrid = IsMouseInGridBounds(mousePosition);

        if (isInGrid != wasInGrid)
        {
            if (isInGrid)
            {
                inventoryController.selectedItemGrid = itemGrid;
                Debug.Log($"鼠标进入网格: {gameObject.name}");
            }
            else
            {
                if (inventoryController.selectedItemGrid == itemGrid)
                {
                    inventoryController.selectedItemGrid = null;
                    Debug.Log($"鼠标离开网格: {gameObject.name}");
                }
            }

            wasInGrid = isInGrid;
        }
    }

    private bool IsMouseInGridBounds(Vector2 mousePosition)
    {
        if (rectTransform == null || canvas == null) return false;

        Vector2 localMousePosition;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform,
            mousePosition,
            canvas.worldCamera,
            out localMousePosition
        );

        Rect gridRect = rectTransform.rect;
        return gridRect.Contains(localMousePosition);
    }

    public bool IsMouseInThisGrid()
    {
        return IsMouseInGridBounds(Input.mousePosition);
    }
}
