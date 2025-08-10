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

        // 检测鼠标进入或离开网格区域
        if (isInGrid != wasInGrid)
        {
            if (isInGrid)
            {
                // 鼠标进入网格区域，设置selectedItemGrid
                inventoryController.selectedItemGrid = itemGrid;
                Debug.Log($"鼠标进入网格: {gameObject.name}");
            }
            else
            {
                // 鼠标离开网格区域，清除selectedItemGrid（只有当前网格是选中的网格时才清除）
                if (inventoryController.selectedItemGrid == itemGrid)
                {
                    inventoryController.selectedItemGrid = null;
                    Debug.Log($"鼠标离开网格: {gameObject.name}");
                }
            }
            
            wasInGrid = isInGrid;
        }
    }

    // 检查鼠标是否在网格边界内
    private bool IsMouseInGridBounds(Vector2 mousePosition)
    {
        if (rectTransform == null || canvas == null) return false;

        // 将屏幕坐标转换为本地坐标系中的坐标
        Vector2 localMousePosition;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform,
            mousePosition,
            canvas.worldCamera,
            out localMousePosition
        );

        // 获取网格的矩形区域
        Rect gridRect = rectTransform.rect;

        // 检查鼠标是否在网格区域内
        return gridRect.Contains(localMousePosition);
    }

    // 获取鼠标是否在当前网格内（供外部调用）
    public bool IsMouseInThisGrid()
    {
        return IsMouseInGridBounds(Input.mousePosition);
    }
}
