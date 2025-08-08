using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 全局拖拽管理（无预制体脚本）
/// </summary>
public class InventorySystemScreenController : MonoBehaviour
{
    [Header("网格")]
    public InventoryGridInteractionManager[] grids;   // 支持多个背包

    [Header("拖拽设置")]
    public KeyCode rotateKey = KeyCode.R;

    // 当前拖拽信息
    private GameObject dragObject;
    private InventorySystemItemDataSO dragData; // 这是 SO，不是组件
    private Vector2Int dragSize;
    private bool dragRotated;
    private InventoryGridInteractionManager fromGrid;
    private Vector2Int fromPos;

    private Canvas canvas;
    private RectTransform dragRT;

    private void Awake()
    {
        canvas = FindObjectOfType<Canvas>();
    }

    /* ---------- 全局射线检测 ---------- */
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
            TryBeginDrag();

        if (Input.GetMouseButton(0) && dragObject != null)
            DoDrag();

        if (Input.GetMouseButtonUp(0) && dragObject != null)
            EndDrag();

        if (Input.GetKeyDown(rotateKey))
            RotateDragged();
    }

    /* ---------- 开始拖拽 ---------- */
    void TryBeginDrag()
    {
        GraphicRaycaster ray = canvas.GetComponent<GraphicRaycaster>();
        PointerEventData ped = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };
        List<RaycastResult> results = new List<RaycastResult>();
        ray.Raycast(ped, results);

        foreach (var hit in results)
        {
            GameObject go = hit.gameObject;
            
            // 首先尝试从射线检测到的对象获取 ItemDataHolder
            var holder = go.GetComponentInParent<ItemDataHolder>();
            if (holder == null) continue;

            // 然后获取对应的 InventorySystemItem 组件
            var item = holder.GetComponent<InventorySystemItem>();
            if (item == null)
            {
                // 如果当前对象没有，尝试从父对象获取
                item = holder.GetComponentInParent<InventorySystemItem>();
            }
            if (item == null) continue;

            var grid = FindGridUnderMouse();
            if (grid == null) return;

            Vector2Int pos = grid.GetGridPosition(Input.mousePosition);
            var gridItem = grid.GetItem(pos.x, pos.y);
            if (gridItem == null || gridItem != item) continue;

            // 设置拖拽信息
            dragObject = item.gameObject;
            dragData = holder.GetItemData();
            dragSize = new Vector2Int(dragData.width, dragData.height);
            dragRotated = false;
            fromGrid = grid;
            fromPos = pos;

            // 从网格中移除
            grid.Remove(item);
            
            // 设置拖拽UI
            dragRT = dragObject.GetComponent<RectTransform>();
            dragRT.SetParent(canvas.transform, true);
            SetHighlight(dragObject, true);
            return;
        }
    }

    /* ---------- 拖拽中 ---------- */
    void DoDrag()
    {
        Vector3 world; // 修正 Vector3
        RectTransformUtility.ScreenPointToWorldPointInRectangle(
            canvas.transform as RectTransform,
            Input.mousePosition,
            null,
            out world);
        dragRT.position = world;
    }

    /* ---------- 结束拖拽 ---------- */
    void EndDrag()
    {
        var targetGrid = FindGridUnderMouse();
        if (targetGrid != null)
        {
            Vector2Int pos = targetGrid.GetGridPosition(Input.mousePosition);
            var item = dragObject.GetComponent<InventorySystemItem>(); // 4. 取组件
            if (item != null && targetGrid.CanPlace(item, pos, dragRotated))
            {
                targetGrid.Place(item, pos, dragRotated); // 5. 传组件
            }
            else
            {
                fromGrid.Place(item, fromPos, dragRotated);
            }
        }
        else
        {
            var item = dragObject.GetComponent<InventorySystemItem>();
            fromGrid.Place(item, fromPos, dragRotated);
        }

        SetHighlight(dragObject, false);
        dragObject = null;
    }

    /* ---------- 旋转 ---------- */
    void RotateDragged()
    {
        if (dragObject == null) return;
        
        // 交换尺寸
        int temp = dragSize.x;
        dragSize.x = dragSize.y;
        dragSize.y = temp;
        
        // 旋转UI
        dragRT.Rotate(0, 0, -90);
        dragRotated = !dragRotated;
        
        Debug.Log($"物品已旋转，新尺寸: {dragSize}");
    }

    /* ---------- 工具 ---------- */
    InventoryGridInteractionManager FindGridUnderMouse()
    {
        foreach (var g in grids)
            if (RectTransformUtility.RectangleContainsScreenPoint(
                    g.GetComponent<RectTransform>(), Input.mousePosition, null))
                return g;
        return null;
    }

    void SetHighlight(GameObject go, bool on)
    {
        var hl = go.GetComponentInChildren<Image>(true);
        if (hl != null && hl.gameObject.name == "HighlightImage")
        {
            Color c = Color.white;
            c.a = on ? 0.35f : 0f;
            hl.color = c;
        }
    }

}