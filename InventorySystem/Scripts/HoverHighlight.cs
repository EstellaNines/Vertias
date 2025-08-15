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
        if (item == null || parentGrid == null) return;

        // 如果已经有高亮对象且父级相同，直接更新
        if (currentHoverHighlight != null && currentParent == parentGrid)
        {
            UpdateHighlightForItem(item);
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

        // 设置高亮位置和大小
        UpdateHighlightForItem(item);

        // 确保高亮在物品上方
        currentHoverHighlight.transform.SetAsLastSibling();
    }

    /// <summary>
    /// 更新高亮效果以匹配物品
    /// </summary>
    /// <param name="item">目标物品</param>
    private void UpdateHighlightForItem(InventorySystemItem item)
    {
        if (hoverHighlightRect == null || item == null) return;

        // 获取物品的RectTransform
        RectTransform itemRect = item.GetComponent<RectTransform>();
        if (itemRect == null) return;

        // 复制物品的位置和大小
        hoverHighlightRect.anchorMin = itemRect.anchorMin;
        hoverHighlightRect.anchorMax = itemRect.anchorMax;
        hoverHighlightRect.anchoredPosition = itemRect.anchoredPosition;
        hoverHighlightRect.sizeDelta = itemRect.sizeDelta;
        hoverHighlightRect.pivot = itemRect.pivot;
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
