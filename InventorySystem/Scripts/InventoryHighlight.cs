using UnityEngine;
using UnityEngine.UI;

public class InventoryHighlight : MonoBehaviour
{
    [Header("高亮设置")]
    [SerializeField] private GameObject highlightPrefab; // 高亮预制体
    [SerializeField] private Color canPlaceColor = new Color(0, 1, 0, 0.5f); // 可放置颜色（绿色半透明）
    [SerializeField] private Color cannotPlaceColor = new Color(1, 0, 0, 0.5f); // 不可放置颜色（红色半透明）

    private GameObject currentHighlight; // 当前高亮对象
    private Image highlightImage; // 高亮图像组件
    private RectTransform highlightRect; // 高亮矩形变换
    private Transform currentParent; // 当前父级网格

    private void Awake()
    {
        // 如果没有指定预制体，创建默认的高亮对象
        if (highlightPrefab == null)
        {
            CreateDefaultHighlight();
        }
    }

    /// <summary>
    /// 创建默认的高亮预制体
    /// </summary>
    private void CreateDefaultHighlight()
    {
        // 创建高亮游戏对象
        highlightPrefab = new GameObject("ItemHighlight");

        // 添加RectTransform组件
        RectTransform rect = highlightPrefab.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(64, 64); // 默认大小

        // 添加Image组件
        Image img = highlightPrefab.AddComponent<Image>();
        img.color = canPlaceColor;
        img.raycastTarget = false; // 不阻挡射线检测

        // 设置为预制体（在运行时不需要真正的预制体）
        highlightPrefab.SetActive(false);
    }

    /// <summary>
    /// 显示或隐藏高亮
    /// </summary>
    /// <param name="show">是否显示</param>
    public void Show(bool show)
    {
        if (show)
        {
            if (currentHighlight == null)
            {
                // 实例化高亮对象
                currentHighlight = Instantiate(highlightPrefab);
                highlightRect = currentHighlight.GetComponent<RectTransform>();
                highlightImage = currentHighlight.GetComponent<Image>();

                // 确保不阻挡射线检测
                if (highlightImage != null)
                {
                    highlightImage.raycastTarget = false;
                }
                
                Debug.Log("创建新的高亮对象");
            }

            currentHighlight.SetActive(true);
            Debug.Log($"高亮对象显示状态: {currentHighlight.activeInHierarchy}, 位置: {highlightRect?.anchoredPosition}, 尺寸: {highlightRect?.sizeDelta}");
        }
        else
        {
            if (currentHighlight != null)
            {
                currentHighlight.SetActive(false);
                Debug.Log("隐藏高亮对象");
            }
        }
    }

    /// <summary>
    /// 调试高亮对象状态
    /// </summary>
    public void DebugHighlightState()
    {
        if (currentHighlight == null)
        {
            Debug.LogWarning("高亮对象为空！");
            return;
        }

        if (highlightRect == null)
        {
            Debug.LogWarning("高亮RectTransform为空！");
            return;
        }

        if (highlightImage == null)
        {
            Debug.LogWarning("高亮Image为空！");
            return;
        }

        Debug.Log($"=== 高亮对象状态 ===\n" +
                  $"激活状态: {currentHighlight.activeInHierarchy}\n" +
                  $"父级: {highlightRect.parent?.name ?? "无"}\n" +
                  $"位置: {highlightRect.anchoredPosition}\n" +
                  $"尺寸: {highlightRect.sizeDelta}\n" +
                  $"轴心: {highlightRect.pivot}\n" +
                  $"锚点: {highlightRect.anchorMin} - {highlightRect.anchorMax}\n" +
                  $"旋转: {highlightRect.localRotation.eulerAngles}\n" +
                  $"颜色: {highlightImage.color}\n" +
                  $"透明度: {highlightImage.color.a}");
    }

    /// <summary>
    /// 强制刷新高亮对象
    /// </summary>
    public void ForceRefresh()
    {
        if (currentHighlight != null && highlightRect != null)
        {
            // 强制刷新Canvas
            Canvas.ForceUpdateCanvases();
            
            // 确保对象激活
            currentHighlight.SetActive(true);
            
            // 强制更新布局
            LayoutRebuilder.ForceRebuildLayoutImmediate(highlightRect);
            
            Debug.Log($"强制刷新高亮对象 - 激活状态: {currentHighlight.activeInHierarchy}, 位置: {highlightRect.anchoredPosition}, 尺寸: {highlightRect.sizeDelta}, 父级: {highlightRect.parent?.name}");
        }
    }

    /// <summary>
    /// 设置高亮大小（基于物品）
    /// </summary>
    /// <param name="item">物品组件</param>
    public void SetSize(InventorySystemItem item)
    {
        if (highlightRect == null || item == null) return;

        // 获取物品数据
        var itemData = item.Data;
        if (itemData == null) return;

        // 计算大小（假设每个网格单元为64x64）
        float cellSize = 64f; // 可以从网格配置中获取
        Vector2 size = new Vector2(itemData.width * cellSize, itemData.height * cellSize);

        highlightRect.sizeDelta = size;
    }

    /// <summary>
    /// 设置高亮大小（直接指定）
    /// </summary>
    /// <param name="width">宽度（网格单位）</param>
    /// <param name="height">高度（网格单位）</param>
    public void SetSize(int width, int height)
    {
        if (highlightRect == null) return;

        float cellSize = 64f; // 可以从网格配置中获取
        Vector2 size = new Vector2(width * cellSize, height * cellSize);

        highlightRect.sizeDelta = size;
    }

    /// <summary>
    /// 设置父级网格
    /// </summary>
    /// <param name="gridTransform">网格变换</param>
    public void SetParent(Transform gridTransform)
    {
        if (currentHighlight == null || gridTransform == null) return;

        currentParent = gridTransform;
        currentHighlight.transform.SetParent(gridTransform, false);

        // 设置为最后渲染（在最上层）
        currentHighlight.transform.SetAsLastSibling();
    }

    /// <summary>
    /// 设置父级网格（通用接口）
    /// </summary>
    /// <param name="grid">网格对象</param>
    public void SetParent(object grid)
    {
        Transform gridTransform = null;

        if (grid is ItemGrid itemGrid)
        {
            gridTransform = itemGrid.transform;
        }
        else if (grid is BackpackItemGrid backpackGrid)
        {
            gridTransform = backpackGrid.transform;
        }
        else if (grid is TactiaclRigItemGrid tacticalGrid)
        {
            gridTransform = tacticalGrid.transform;
        }
        else if (grid is MonoBehaviour monoBehaviour)
        {
            gridTransform = monoBehaviour.transform;
        }

        if (gridTransform != null)
        {
            SetParent(gridTransform);
        }
    }

    /// <summary>
    /// 设置高亮位置
    /// </summary>
    /// <param name="grid">目标网格</param>
    /// <param name="item">物品</param>
    /// <param name="gridX">网格X坐标</param>
    /// <param name="gridY">网格Y坐标</param>
    public void SetPosition(object grid, InventorySystemItem item, int gridX, int gridY)
    {
        if (currentHighlight == null || highlightRect == null) return;

        // 获取网格单元大小
        float cellSize = GetCellSize(grid);

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

        // 计算位置（基于左上角锚点，与网格系统保持一致）
        // 注意：这里使用左上角锚点，因为网格系统通常使用左上角作为原点
        highlightRect.anchorMin = new Vector2(0, 1);
        highlightRect.anchorMax = new Vector2(0, 1);
        highlightRect.pivot = new Vector2(0, 1);

        // 设置位置（左上角对齐）
        Vector2 position = new Vector2(
            gridX * cellSize,
            -gridY * cellSize
        );
        
        highlightRect.anchoredPosition = position;

        // 调试信息
        Debug.Log($"高亮位置设置 - 网格坐标: ({gridX}, {gridY}), 物品尺寸: {itemSize.x}x{itemSize.y}, 像素位置: {position.x}, {position.y}, 轴心: {highlightRect.pivot}, 锚点: {highlightRect.anchorMin}");
    }

    /// <summary>
    /// 设置高亮颜色
    /// </summary>
    /// <param name="color">颜色</param>
    public void SetColor(Color color)
    {
        if (highlightImage != null)
        {
            highlightImage.color = color;
        }
    }

    /// <summary>
    /// 设置是否可放置的颜色
    /// </summary>
    /// <param name="canPlace">是否可放置</param>
    public void SetCanPlace(bool canPlace)
    {
        SetColor(canPlace ? canPlaceColor : cannotPlaceColor);
    }

    /// <summary>
    /// 获取网格单元大小
    /// </summary>
    /// <param name="grid">网格对象</param>
    /// <returns>单元大小</returns>
    private float GetCellSize(object grid)
    {
        float cellSize = 64f; // 默认大小

        if (grid is ItemGrid itemGrid)
        {
            cellSize = itemGrid.GetCellSize();
        }
        else if (grid is BackpackItemGrid backpackGrid)
        {
            // 修复：调用BackpackItemGrid的GetCellSize方法
            cellSize = backpackGrid.GetCellSize();
        }
        else if (grid is TactiaclRigItemGrid tacticalGrid)
        {
            // 修复：调用TactiaclRigItemGrid的GetCellSize方法
            cellSize = tacticalGrid.GetCellSize();
        }

        return cellSize;
    }

    /// <summary>
    /// 隐藏高亮
    /// </summary>
    public void Hide()
    {
        Show(false);
    }

    /// <summary>
    /// 清理资源
    /// </summary>
    private void OnDestroy()
    {
        if (currentHighlight != null && !ReferenceEquals(currentHighlight, null))
        {
            // 使用 Destroy 而不是 DestroyImmediate 来避免报错
            Destroy(currentHighlight);
            currentHighlight = null;
        }
    }


}
