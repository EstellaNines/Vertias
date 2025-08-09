using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(ItemGrid))]
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
        rectTransform = GetComponent<RectTransform>();
        canvas = FindObjectOfType<Canvas>();
    }

    private void Update()
    {
        if (inventoryController == null || itemGrid == null) return;

        Vector2 mousePosition = Input.mousePosition;
        bool isInGrid = IsMouseInGridBounds(mousePosition);

        // 检查状态是否发生变化
        if (isInGrid != wasInGrid)
        {
            if (isInGrid)
            {
                // 鼠标进入网格范围，设置selectedItemGrid
                inventoryController.selectedItemGrid = itemGrid;
                Debug.Log($"鼠标进入网格: {gameObject.name}");
            }
            else
            {
                // 鼠标离开网格范围，清除selectedItemGrid（仅当当前选中的是这个网格时）
                if (inventoryController.selectedItemGrid == itemGrid)
                {
                    inventoryController.selectedItemGrid = null;
                    Debug.Log($"鼠标离开网格: {gameObject.name}");
                }
            }
            
            wasInGrid = isInGrid;
        }
    }

    // 检查鼠标位置是否在网格边界内
    private bool IsMouseInGridBounds(Vector2 mousePosition)
    {
        if (rectTransform == null || canvas == null) return false;

        // 将鼠标位置转换为相对于网格的本地坐标
        Vector2 localMousePosition;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform,
            mousePosition,
            canvas.worldCamera,
            out localMousePosition
        );

        // 获取网格的边界
        Rect gridRect = rectTransform.rect;

        // 检查鼠标是否在网格边界内
        return gridRect.Contains(localMousePosition);
    }

    // 获取当前鼠标是否在此网格范围内
    public bool IsMouseInThisGrid()
    {
        return IsMouseInGridBounds(Input.mousePosition);
    }
}
