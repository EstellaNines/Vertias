using UnityEngine;
using UnityEngine.EventSystems;

public class GridInteract : MonoBehaviour
{
    public enum GridType
    {
        MainGrid,
        BackpackGrid,
        TacticalRigGrid
    }

    [Header("网格优先级 (数值越小优先级越高)")]
    public int gridPriority = 0;
    
    private InventoryController inventoryController;
    private ItemGrid itemGrid;
    private BackpackItemGrid backpackGrid;
    private TactiaclRigItemGrid tacticalRigGrid;
    private RectTransform rectTransform;
    private Canvas canvas;
    private bool wasInGrid = false;
    private GridType currentGridType;
    
    // 静态变量用于跟踪当前活跃的网格
    private static GridInteract currentActiveGridInteract;

    private void Awake()
    {
        inventoryController = FindObjectOfType<InventoryController>();

        // 检测当前GameObject上的网格类型
        itemGrid = GetComponent<ItemGrid>();
        backpackGrid = GetComponent<BackpackItemGrid>();
        tacticalRigGrid = GetComponent<TactiaclRigItemGrid>();

        // 确定网格类型和默认优先级
        if (itemGrid != null)
        {
            currentGridType = GridType.MainGrid;
            if (gridPriority == 0) gridPriority = 1; // 主网格默认优先级
        }
        else if (backpackGrid != null)
        {
            currentGridType = GridType.BackpackGrid;
            if (gridPriority == 0) gridPriority = 2; // 背包网格默认优先级
        }
        else if (tacticalRigGrid != null)
        {
            currentGridType = GridType.TacticalRigGrid;
            if (gridPriority == 0) gridPriority = 3; // 战术挂具默认优先级
        }
        else
        {
            Debug.LogError($"GridInteract on {gameObject.name} requires one of: ItemGrid, BackpackItemGrid, or TactiaclRigItemGrid component!");
            enabled = false;
            return;
        }

        rectTransform = GetComponent<RectTransform>();
        canvas = FindObjectOfType<Canvas>();

        Debug.Log($"GridInteract initialized on {gameObject.name} as {currentGridType} with priority {gridPriority}");
    }

    private void Update()
    {
        if (inventoryController == null) return;

        Vector2 mousePosition = Input.mousePosition;
        bool isInGrid = IsMouseInGridBounds(mousePosition);

        if (isInGrid != wasInGrid)
        {
            if (isInGrid)
            {
                // 检查是否有更高优先级的网格已经激活
                if (currentActiveGridInteract == null || gridPriority < currentActiveGridInteract.gridPriority)
                {
                    // 清除之前的选择
                    if (currentActiveGridInteract != null)
                    {
                        currentActiveGridInteract.ForceExit();
                    }
                    
                    currentActiveGridInteract = this;
                    SetAsSelectedGrid();
                    Debug.Log($"鼠标进入{currentGridType}网格: {gameObject.name} (优先级: {gridPriority})");
                }
            }
            else
            {
                if (currentActiveGridInteract == this)
                {
                    ClearIfSelected();
                    currentActiveGridInteract = null;
                    Debug.Log($"鼠标离开{currentGridType}网格: {gameObject.name}");
                }
            }

            wasInGrid = isInGrid;
        }
    }

    private void ForceExit()
    {
        wasInGrid = false;
        ClearIfSelected();
    }

    private void SetAsSelectedGrid()
    {
        // 首先清除所有网格选择并重置活跃网格类型
        inventoryController.ClearSelectedGrid();
        
        // 然后设置当前网格
        switch (currentGridType)
        {
            case GridType.MainGrid:
                inventoryController.SetSelectedMainGrid(itemGrid);
                break;
            case GridType.BackpackGrid:
                inventoryController.SetSelectedBackpackGrid(backpackGrid);
                break;
            case GridType.TacticalRigGrid:
                inventoryController.SetSelectedTacticalRigGrid(tacticalRigGrid);
                break;
        }
    }

    private void ClearIfSelected()
    {
        switch (currentGridType)
        {
            case GridType.MainGrid:
                if (inventoryController.selectedItemGrid == itemGrid)
                {
                    inventoryController.selectedItemGrid = null;
                }
                break;
            case GridType.BackpackGrid:
                if (inventoryController.selectedBackpackGrid == backpackGrid)
                {
                    inventoryController.selectedBackpackGrid = null;
                }
                break;
            case GridType.TacticalRigGrid:
                if (inventoryController.selectedTacticalRigGrid == tacticalRigGrid)
                {
                    inventoryController.selectedTacticalRigGrid = null;
                }
                break;
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

    public GridType GetCurrentGridType()
    {
        return currentGridType;
    }
    
    // 静态方法用于清除所有网格选择
    public static void ClearAllGridSelections()
    {
        currentActiveGridInteract = null;
    }
}