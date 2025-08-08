using UnityEngine;

[RequireComponent(typeof(ItemGridInventory))]
public class InventoryGridInteractionManager : MonoBehaviour
{
    private ItemGridInventory gridInventory;   // 网格数据层

    private void Awake()
    {
        gridInventory = GetComponent<ItemGridInventory>();
    }

    #region ---- 对外 API ----

    // 鼠标位置 → 网格坐标
    public Vector2Int GetGridPosition(Vector2 mousePos)
    {
        RectTransform rt = gridInventory.RectTransform;
        Vector2 local;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rt, mousePos, null, out local);

        float cell = gridInventory.CellSize;

        int x = Mathf.FloorToInt((local.x + rt.rect.width * 0.5f) / cell);
        int y = Mathf.FloorToInt((-local.y + rt.rect.height * 0.5f) / cell);

        return new Vector2Int(x, y);
    }

    // 边界检查
    public bool BoundsCheck(Vector2Int pos, Vector2Int size)
    {
        return pos.x >= 0 && pos.y >= 0 &&
               pos.x + size.x <= gridInventory.Width &&
               pos.y + size.y <= gridInventory.Height;
    }

    // 重叠检查
    public bool OverlapCheck(Vector2Int pos, Vector2Int size)
    {
        for (int x = 0; x < size.x; x++)
            for (int y = 0; y < size.y; y++)
                if (gridInventory.GetItem(pos.x + x, pos.y + y) != null)
                    return false;
        return true;
    }

    // 是否可放置
    public bool CanPlace(InventorySystemItem item, Vector2Int pos, bool rotated = false)
    {
        Vector2Int size = rotated ? new Vector2Int(item.Size.y, item.Size.x)
                                  : item.Size;
        return BoundsCheck(pos, size) && OverlapCheck(pos, size);
    }

    // 放置物品
    public void Place(InventorySystemItem item, Vector2Int pos, bool rotated = false)
    {
        Vector2Int size = rotated ? new Vector2Int(item.Size.y, item.Size.x)
                                  : item.Size;

        // 1. 数据层
        for (int x = 0; x < size.x; x++)
            for (int y = 0; y < size.y; y++)
                gridInventory.SetItem(pos.x + x, pos.y + y, item);

        // 2. UI 层
        UpdateItemUI(item, pos, size, rotated);
    }

    // 移除物品
    public void Remove(InventorySystemItem item)
    {
        for (int x = 0; x < gridInventory.Width; x++)
            for (int y = 0; y < gridInventory.Height; y++)
                if (gridInventory.GetItem(x, y) == item)
                    gridInventory.SetItem(x, y, null);
    }

    // 供外部查询
    public InventorySystemItem GetItem(int x, int y) => gridInventory.GetItem(x, y);

    #endregion

    #region ---- 内部工具 ----

    private void UpdateItemUI(InventorySystemItem item, Vector2Int pos, Vector2Int size, bool rotated)
    {
        RectTransform rt = item.GetComponent<RectTransform>();
        rt.SetParent(transform);
        rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.sizeDelta = new Vector2(size.x * gridInventory.CellSize, size.y * gridInventory.CellSize);
        rt.anchoredPosition = new Vector2(pos.x * gridInventory.CellSize, -pos.y * gridInventory.CellSize);
        rt.localRotation = Quaternion.Euler(0, 0, rotated ? -90 : 0);
    }

    #endregion
}