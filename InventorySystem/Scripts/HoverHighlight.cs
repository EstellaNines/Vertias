using UnityEngine;
using UnityEngine.UI;

public class HoverHighlight : MonoBehaviour
{
    [Header("悬停高亮设置")]
    [SerializeField] private GameObject hoverHighlightPrefab; // 悬停高亮预制体
    [SerializeField] private Color hoverColor = new Color(1, 1, 0, 0.3f); // 悬停颜色（黄色半透明）

    private GameObject currentHoverHighlight; // 当前悬停高亮对象
    private Image hoverHighlightImage; // 悬停高亮图像组件
    private RectTransform hoverHighlightRect; // 悬停高亮矩形变换
    private Transform currentParent; // 当前父级网格

    private void Awake()
    {
        // 如果没有指定预制体，创建默认的悬停高亮对象
        if (hoverHighlightPrefab == null)
        {
            CreateDefaultHoverHighlight();
        }
    }

    /// <summary>
    /// 创建默认的悬停高亮预制体
    /// </summary>
    private void CreateDefaultHoverHighlight()
    {
        // 创建高亮对象
        hoverHighlightPrefab = new GameObject("HoverHighlight");

        // 添加RectTransform组件
        var rectTransform = hoverHighlightPrefab.AddComponent<RectTransform>();

        // 添加Image组件
        var image = hoverHighlightPrefab.AddComponent<Image>();
        image.color = hoverColor;
        image.raycastTarget = false; // 不阻挡射线检测

        // 设置为预制体（在运行时不需要真正的预制体，只是作为模板）
        hoverHighlightPrefab.SetActive(false);
    }

    /// <summary>
    /// 显示悬停高亮
    /// </summary>
    /// <param name="item">要高亮的物品</param>
    /// <param name="parentGrid">父级网格</param>
    public void ShowHoverHighlight(InventorySystemItem item, Transform parentGrid)
    {
        ShowHoverHighlight(item, parentGrid, null);
    }

    /// <summary>
    /// 显示悬停高亮（装备栏版本）
    /// </summary>
    /// <param name="item">要高亮的物品</param>
    /// <param name="parentGrid">父级网格</param>
    /// <param name="equipSlot">装备栏引用，如果为null则使用物品原始大小</param>
    public void ShowHoverHighlight(InventorySystemItem item, Transform parentGrid, EquipSlot equipSlot)
    {
        if (item == null || parentGrid == null) return;

        Debug.Log($"显示悬停高亮 - 物品: {item.name}, 父级网格: {parentGrid.name}, 装备栏: {(equipSlot != null ? equipSlot.name : "无")}");

        // 如果已经有高亮对象且父级相同，直接更新
        if (currentHoverHighlight != null && currentParent == parentGrid)
        {
            Debug.Log("使用现有高亮对象，直接更新");
            if (equipSlot != null)
            {
                UpdateHighlightForEquipSlot(item, equipSlot);
            }
            else
            {
                UpdateHighlightForItem(item);
            }
            return;
        }

        // 隐藏之前的高亮
        HideHoverHighlight();

        // 创建新的高亮对象
        currentHoverHighlight = Instantiate(hoverHighlightPrefab, parentGrid);
        currentHoverHighlight.SetActive(true);
        currentParent = parentGrid;

        // 获取组件引用
        hoverHighlightRect = currentHoverHighlight.GetComponent<RectTransform>();
        hoverHighlightImage = currentHoverHighlight.GetComponent<Image>();

        // 确保不阻挡射线检测
        if (hoverHighlightImage != null)
        {
            hoverHighlightImage.raycastTarget = false;
            hoverHighlightImage.color = hoverColor;
        }

        // 根据是否在装备栏中设置高亮位置和大小
        if (equipSlot != null)
        {
            UpdateHighlightForEquipSlot(item, equipSlot);
        }
        else
        {
            UpdateHighlightForItem(item);
        }

        // 确保高亮在物品上方
        currentHoverHighlight.transform.SetAsLastSibling();

        Debug.Log($"悬停高亮创建完成 - 高亮对象: {currentHoverHighlight.name}, 父级: {currentHoverHighlight.transform.parent?.name}");
    }

    /// <summary>
    /// 更新高亮效果以匹配物品
    /// </summary>
    /// <param name="item">目标物品</param>
    public void UpdateHighlightForItem(InventorySystemItem item)
    {
        if (hoverHighlightRect == null || item == null) return;

        // 验证物品的轴心设置和位置信息
        ValidateItemPivotAndPosition(item);

        // 获取物品的RectTransform
        RectTransform itemRect = item.GetComponent<RectTransform>();
        if (itemRect == null) return;

        // 获取物品的尺寸
        Vector2Int itemSize = Vector2Int.one; // 默认尺寸

        // 尝试从ItemDataHolder获取尺寸信息
        var itemDataHolder = item.GetComponent<ItemDataHolder>();
        if (itemDataHolder != null)
        {
            itemSize = new Vector2Int(itemDataHolder.ItemWidth, itemDataHolder.ItemHeight);
        }
        else if (item.Data != null)
        {
            // 如果没有ItemDataHolder，使用原始数据
            itemSize = new Vector2Int(item.Data.width, item.Data.height);
        }

        // 计算实际尺寸（像素）
        float cellSize = 64f; // 与网格统一
        Vector2 pixelSize = new Vector2(
            itemSize.x * cellSize,
            itemSize.y * cellSize
        );

        // 设置高亮框的尺寸
        hoverHighlightRect.sizeDelta = pixelSize;

        // 获取物品在网格中的实际占位位置（基于网格坐标）
        Vector2Int gridPosition = GetItemActualGridPosition(item);

        // 使用网格坐标计算高亮框的左上角位置
        Vector2 highlightPosition = new Vector2(
            gridPosition.x * cellSize,
            -gridPosition.y * cellSize
        );

        // 统一使用左上角轴心设置
        hoverHighlightRect.anchorMin = new Vector2(0, 1);
        hoverHighlightRect.anchorMax = new Vector2(0, 1);
        hoverHighlightRect.pivot = new Vector2(0f, 1f);  // 改为左上角轴心

        // 设置高亮框的左上角位置
        hoverHighlightRect.anchoredPosition = highlightPosition;

        // 高亮框不需要旋转
        hoverHighlightRect.localRotation = Quaternion.identity;

        // 验证高亮框是否正确对齐
        ValidateAlignment(item);

        // 强制刷新高亮对象，确保正确显示
        ForceRefresh();

        // 调试信息
        Debug.Log($"悬停高亮更新 - 网格位置: ({gridPosition.x}, {gridPosition.y}), 尺寸: {itemSize.x}x{itemSize.y}, 轴心: 左上角");
    }

    /// <summary>
    /// 强制刷新高亮对象
    /// </summary>
    public void ForceRefresh()
    {
        if (currentHoverHighlight != null && hoverHighlightRect != null)
        {
            // 强制刷新Canvas
            Canvas.ForceUpdateCanvases();

            // 确保对象激活
            currentHoverHighlight.SetActive(true);

            // 强制更新布局
            LayoutRebuilder.ForceRebuildLayoutImmediate(hoverHighlightRect);

            Debug.Log($"强制刷新悬停高亮 - 激活状态: {currentHoverHighlight.activeInHierarchy}, 世界位置: {hoverHighlightRect.position}, 尺寸: {hoverHighlightRect.sizeDelta}, 父级: {hoverHighlightRect.parent?.name}");
        }
    }

    /// <summary>
    /// 验证高亮框是否正确对齐
    /// </summary>
    /// <param name="item">目标物品</param>
    public void ValidateAlignment(InventorySystemItem item)
    {
        if (hoverHighlightRect == null || item == null) return;

        RectTransform itemRect = item.GetComponent<RectTransform>();
        if (itemRect == null) return;

        // 获取物品和高亮框的网格位置信息
        Vector2Int itemGridPos = GetItemActualGridPosition(item);
        Vector2Int highlightGridPos = GetHighlightGridPosition();

        // 获取尺寸信息
        Vector2 itemSize = itemRect.sizeDelta;
        Vector2 highlightSize = hoverHighlightRect.sizeDelta;
        Vector3 itemRotation = itemRect.localRotation.eulerAngles;
        Vector3 highlightRotation = hoverHighlightRect.localRotation.eulerAngles;

        // 计算网格位置差异
        Vector2Int gridPositionDiff = highlightGridPos - itemGridPos;
        Vector2 sizeDiff = highlightSize - itemSize;
        Vector3 rotationDiff = highlightRotation - itemRotation;

        Debug.Log($"=== 高亮框对齐验证（网格坐标）===\n" +
                  $"物品网格位置: {itemGridPos}, 尺寸: {itemSize}, 旋转: {itemRotation}\n" +
                  $"高亮网格位置: {highlightGridPos}, 尺寸: {highlightSize}, 旋转: {highlightRotation}\n" +
                  $"网格位置差异: {gridPositionDiff}\n" +
                  $"尺寸差异: {sizeDiff}\n" +
                  $"旋转差异: {rotationDiff}\n" +
                  $"对齐状态: {(gridPositionDiff == Vector2Int.zero ? "✓ 已对齐" : "✗ 未对齐")}");

        // 如果网格位置差异太大，记录警告
        if (Mathf.Abs(gridPositionDiff.x) > 1 || Mathf.Abs(gridPositionDiff.y) > 1)
        {
            Debug.LogWarning($"网格位置差异过大: {gridPositionDiff}，请检查网格坐标计算");
        }
    }

    /// <summary>
    /// 验证物品的轴心设置和位置信息
    /// </summary>
    /// <param name="item">目标物品</param>
    public void ValidateItemPivotAndPosition(InventorySystemItem item)
    {
        if (item == null) return;

        RectTransform itemRect = item.GetComponent<RectTransform>();
        if (itemRect == null) return;

        var itemDataHolder = item.GetComponent<ItemDataHolder>();
        Vector2Int itemSize = Vector2Int.one;

        if (itemDataHolder != null)
        {
            itemSize = new Vector2Int(itemDataHolder.ItemWidth, itemDataHolder.ItemHeight);
        }

        Debug.Log($"=== 物品轴心验证 ===\n" +
                  $"物品名称: {item.name}\n" +
                  $"轴心设置: {itemRect.pivot}\n" +
                  $"锚点设置: {itemRect.anchorMin} - {itemRect.anchorMax}\n" +
                  $"anchoredPosition: {itemRect.anchoredPosition}\n" +
                  $"世界位置: {itemRect.position}\n" +
                  $"物品尺寸: {itemSize.x}x{itemSize.y}\n" +
                  $"轴心类型: {(itemRect.pivot == new Vector2(0f, 1f) ? "左上角轴心" : "其他轴心")}");
    }

    /// <summary>
    /// 获取物品在网格中的位置
    /// </summary>
    /// <param name="item">物品</param>
    /// <returns>网格坐标</returns>
    private Vector2Int GetItemGridPosition(InventorySystemItem item)
    {
        // 尝试从父级网格获取位置信息
        var parentGrid = item.GetComponentInParent<BaseItemGrid>();
        if (parentGrid != null)
        {
            // 使用网格的GetCellSize方法获取准确的格子大小
            float cellSize = parentGrid.GetCellSize();

            // 获取物品的RectTransform
            RectTransform itemRect = item.GetComponent<RectTransform>();
            if (itemRect != null)
            {
                // 根据物品的anchoredPosition反推网格位置
                // 注意：网格系统使用左上角作为原点，Y轴向下为正
                int gridX = Mathf.RoundToInt(itemRect.anchoredPosition.x / cellSize);
                int gridY = Mathf.RoundToInt(-itemRect.anchoredPosition.y / cellSize);

                // 确保网格坐标不为负数
                gridX = Mathf.Max(0, gridX);
                gridY = Mathf.Max(0, gridY);

                Debug.Log($"物品网格位置计算 - anchoredPosition: {itemRect.anchoredPosition}, cellSize: {cellSize}, 计算网格坐标: ({gridX}, {gridY})");

                return new Vector2Int(gridX, gridY);
            }
        }

        // 如果无法获取网格位置，返回默认值
        Debug.LogWarning($"无法获取物品 {item.name} 的网格位置，使用默认值 (0,0)");
        return Vector2Int.zero;
    }

    /// <summary>
    /// 获取物品在网格中的实际占位位置（基于网格坐标，不是视觉位置）
    /// </summary>
    /// <param name="item">物品</param>
    /// <returns>网格坐标</returns>
    private Vector2Int GetItemActualGridPosition(InventorySystemItem item)
    {
        // 尝试从父级网格获取位置信息
        var parentGrid = item.GetComponentInParent<BaseItemGrid>();
        if (parentGrid != null)
        {
            // 使用网格的GetCellSize方法获取准确的格子大小
            float cellSize = parentGrid.GetCellSize();

            // 获取物品的RectTransform
            RectTransform itemRect = item.GetComponent<RectTransform>();
            if (itemRect != null)
            {
                // 统一使用左上角轴心计算网格位置
                // 直接反推网格位置（所有物品现在都使用左上角轴心）
                int gridX = Mathf.RoundToInt(itemRect.anchoredPosition.x / cellSize);
                int gridY = Mathf.RoundToInt(-itemRect.anchoredPosition.y / cellSize);

                Vector2Int gridPosition = new Vector2Int(gridX, gridY);

                Debug.Log($"左上角轴心物品网格位置计算 - anchoredPosition: {itemRect.anchoredPosition}, cellSize: {cellSize}, 网格坐标: ({gridX}, {gridY})");

                // 确保网格坐标不为负数
                gridPosition.x = Mathf.Max(0, gridPosition.x);
                gridPosition.y = Mathf.Max(0, gridPosition.y);

                return gridPosition;
            }
        }

        // 如果无法获取网格位置，返回默认值
        Debug.LogWarning($"无法获取物品 {item.name} 的网格位置，使用默认值 (0,0)");
        return Vector2Int.zero;
    }

    /// <summary>
    /// 获取高亮框在网格中的位置
    /// </summary>
    /// <returns>网格坐标</returns>
    private Vector2Int GetHighlightGridPosition()
    {
        if (hoverHighlightRect == null) return Vector2Int.zero;

        // 尝试从父级网格获取位置信息
        var parentGrid = hoverHighlightRect.GetComponentInParent<BaseItemGrid>();
        if (parentGrid != null)
        {
            float cellSize = parentGrid.GetCellSize();

            // 根据高亮框的anchoredPosition反推网格位置
            int gridX = Mathf.RoundToInt(hoverHighlightRect.anchoredPosition.x / cellSize);
            int gridY = Mathf.RoundToInt(-hoverHighlightRect.anchoredPosition.y / cellSize);

            // 确保网格坐标不为负数
            gridX = Mathf.Max(0, gridX);
            gridY = Mathf.Max(0, gridY);

            return new Vector2Int(gridX, gridY);
        }

        return Vector2Int.zero;
    }

    /// <summary>
    /// 更新高亮效果以匹配装备栏大小
    /// </summary>
    /// <param name="item">目标物品</param>
    /// <param name="equipSlot">装备栏引用</param>
    public void UpdateHighlightForEquipSlot(InventorySystemItem item, EquipSlot equipSlot)
    {
        if (hoverHighlightRect == null || item == null || equipSlot == null) return;

        // 获取装备栏的RectTransform
        RectTransform equipSlotRect = equipSlot.GetComponent<RectTransform>();
        if (equipSlotRect == null) return;

        // 使用装备栏的大小作为高亮框大小
        Vector2 equipSlotSize = equipSlotRect.sizeDelta;
        hoverHighlightRect.sizeDelta = equipSlotSize;

        // 设置高亮框的锚点和轴心与装备栏一致
        hoverHighlightRect.anchorMin = equipSlotRect.anchorMin;
        hoverHighlightRect.anchorMax = equipSlotRect.anchorMax;
        hoverHighlightRect.pivot = equipSlotRect.pivot;

        // 由于高亮框是装备栏父级的子对象，需要将装备栏的世界位置转换为相对于父级的本地位置
        // 获取装备栏在其父级坐标系中的位置
        Vector3 worldPosition = equipSlotRect.TransformPoint(Vector3.zero);
        Vector3 localPosition;
        
        // 将世界坐标转换为高亮框父级的本地坐标
        RectTransform parentRect = hoverHighlightRect.parent.GetComponent<RectTransform>();
        if (parentRect != null)
        {
            localPosition = parentRect.InverseTransformPoint(worldPosition);
            hoverHighlightRect.anchoredPosition = new Vector2(localPosition.x, localPosition.y);
        }
        else
        {
            // 如果没有父级RectTransform，直接使用装备栏的位置
            hoverHighlightRect.anchoredPosition = Vector2.zero;
        }

        // 高亮框不需要旋转
        hoverHighlightRect.localRotation = Quaternion.identity;

        // 强制刷新高亮对象，确保正确显示
        ForceRefresh();

        // 调试信息
        Debug.Log($"装备栏悬停高亮更新 - 装备栏: {equipSlot.name}, 大小: {equipSlotSize}, 本地位置: {hoverHighlightRect.anchoredPosition}");
    }

    /// <summary>
    /// 隐藏悬停高亮
    /// </summary>
    public void HideHoverHighlight()
    {
        if (currentHoverHighlight != null)
        {
            Destroy(currentHoverHighlight);
            currentHoverHighlight = null;
            hoverHighlightRect = null;
            hoverHighlightImage = null;
            currentParent = null;
        }
    }

    /// <summary>
    /// 设置悬停高亮颜色
    /// </summary>
    /// <param name="color">颜色</param>
    public void SetHoverColor(Color color)
    {
        hoverColor = color;
        if (hoverHighlightImage != null)
        {
            hoverHighlightImage.color = hoverColor;
        }
    }

    /// <summary>
    /// 清理资源
    /// </summary>
    private void OnDestroy()
    {
        HideHoverHighlight();
    }
}
