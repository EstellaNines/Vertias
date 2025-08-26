using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 网格放置指示器
// 负责显示物品放置时的绿色（可放置）或红色（不可放置）高亮提示
public class InventoryHighlight : MonoBehaviour
{
    [Header("高亮设置")]
    [SerializeField] private RectTransform highlighter; // 高亮框的RectTransform
    [SerializeField] private Image highlightImage; // 高亮框的Image组件

    [Header("颜色配置")]
    [SerializeField] private Color canPlaceColor = new Color(0f, 1f, 0f, 0.5f); // 可放置颜色（绿色）
    [SerializeField] private Color cannotPlaceColor = new Color(1f, 0f, 0f, 0.5f); // 不可放置颜色（红色）

    private void Awake()
    {
        // 如果没有指定高亮框，尝试获取当前物体的组件
        if (highlighter == null)
        {
            highlighter = GetComponent<RectTransform>();
        }

        if (highlightImage == null)
        {
            highlightImage = GetComponent<Image>();
        }

        // 初始时隐藏高亮框
        Show(false);
    }

    // 设置高亮框大小
    public void SetSize(Item targetItem)
    {
        if (highlighter == null || targetItem == null) return;

        Vector2 size = new Vector2();
        size.x = targetItem.GetWidth() * ItemGrid.tileSizeWidth;
        size.y = targetItem.GetHeight() * ItemGrid.tileSizeHeight;
        highlighter.sizeDelta = size;

        Debug.Log($"InventoryHighlight: 设置高亮框大小为 {size}, 物品尺寸: {targetItem.GetWidth()}x{targetItem.GetHeight()}");
    }

    // 设置高亮框大小（直接指定宽高）
    public void SetSize(int width, int height)
    {
        if (highlighter == null) return;

        Vector2 size = new Vector2();
        size.x = width * ItemGrid.tileSizeWidth;
        size.y = height * ItemGrid.tileSizeHeight;
        highlighter.sizeDelta = size;

        Debug.Log($"InventoryHighlight: 设置高亮框大小为 {size}, 网格尺寸: {width}x{height}");
    }

    // 设置高亮框位置（使用物品当前位置）
    public void SetPosition(ItemGrid targetGrid, Item targetItem)
    {
        if (highlighter == null || targetGrid == null || targetItem == null) return;

        Vector2 pos = targetGrid.CalculatePositionOnGrid(targetItem, targetItem.OnGridPosition.x, targetItem.OnGridPosition.y);
        highlighter.localPosition = pos;

        Debug.Log($"InventoryHighlight: 设置高亮框位置为 {pos}, 网格位置: ({targetItem.OnGridPosition.x}, {targetItem.OnGridPosition.y})");
    }

    // 设置高亮框位置（指定网格坐标）
    // 修改SetPosition方法，使其能够正确处理旋转物品
    public void SetPosition(ItemGrid targetGrid, Item targetItem, int posX, int posY)
    {
        if (highlighter == null || targetGrid == null || targetItem == null) return;

        // 使用ItemGrid的CalculatePositionOnGrid方法来计算正确位置
        // 这个方法已经考虑了物品的实际尺寸和网格布局
        Vector2 pos = targetGrid.CalculatePositionOnGrid(targetItem, posX, posY);
        highlighter.localPosition = pos;

        Debug.Log($"InventoryHighlight: 设置高亮框位置为 {pos}, 指定位置: ({posX}, {posY}), 物品尺寸: {targetItem.GetWidth()}x{targetItem.GetHeight()}");
    }

    // 修改直接指定宽高的SetPosition方法
    // 简化的位置设置方法，使用与ItemGrid相同的计算逻辑
    public void SetPositionSimple(ItemGrid targetGrid, int posX, int posY)
    {
        if (highlighter == null || targetGrid == null) return;

        // 计算网格的总尺寸
        float gridWidth = targetGrid.gridSizeWidth * ItemGrid.tileSizeWidth;
        float gridHeight = targetGrid.gridSizeHeight * ItemGrid.tileSizeHeight;

        // 使用与ItemGrid.CalculatePositionOnGrid相同的计算逻辑
        Vector2 position = new Vector2();
        position.x = posX * ItemGrid.tileSizeWidth - gridWidth / 2;
        position.y = gridHeight / 2 - posY * ItemGrid.tileSizeHeight;

        highlighter.localPosition = position;

        Debug.Log($"InventoryHighlight: 设置高亮框位置为 {position}, 指定位置: ({posX}, {posY})");
    }

    // 显示或隐藏高亮框
    public void Show(bool show)
    {
        if (highlighter != null)
        {
            highlighter.gameObject.SetActive(show);
            Debug.Log($"InventoryHighlight: {(show ? "显示" : "隐藏")}高亮框");
        }
    }

    // 设置高亮框的父级
    public void SetParent(ItemGrid targetGrid)
    {
        if (highlighter == null || targetGrid == null) return;

        highlighter.SetParent(targetGrid.GetComponent<RectTransform>(), false);
        Debug.Log($"InventoryHighlight: 设置高亮框父级为 {targetGrid.name}");
    }

    // 设置高亮框颜色（可放置/不可放置）
    public void SetCanPlace(bool canPlace)
    {
        if (highlightImage == null) return;

        Color targetColor = canPlace ? canPlaceColor : cannotPlaceColor;
        highlightImage.color = targetColor;

        Debug.Log($"InventoryHighlight: 设置高亮框颜色为 {(canPlace ? "绿色（可放置）" : "红色（不可放置）")}");
    }

    // 设置自定义颜色
    public void SetColor(Color color)
    {
        if (highlightImage == null) return;

        highlightImage.color = color;
        Debug.Log($"InventoryHighlight: 设置自定义颜色 {color}");
    }

    // 获取高亮框是否显示
    public bool IsShowing()
    {
        return highlighter != null && highlighter.gameObject.activeInHierarchy;
    }

    // 重置高亮框到默认状态
    public void Reset()
    {
        Show(false);
        if (highlightImage != null)
        {
            highlightImage.color = canPlaceColor;
        }
        Debug.Log("InventoryHighlight: 重置高亮框到默认状态");
    }

    // 设置重叠警告效果
    // 用于在拖拽时显示物品重叠冲突的视觉提示
    public void SetOverlapWarning(bool hasOverlap)
    {
        if (highlightImage == null) return;

        if (hasOverlap)
        {
            // 设置为红色警告颜色，表示有重叠冲突
            highlightImage.color = cannotPlaceColor;
            Debug.Log("InventoryHighlight: 设置重叠警告效果 - 红色");
        }
        else
        {
            // 设置为绿色正常颜色，表示可以放置
            highlightImage.color = canPlaceColor;
            Debug.Log("InventoryHighlight: 设置正常放置效果 - 绿色");
        }
    }

    // 设置重叠警告效果（带闪烁）
    // 可选的增强视觉效果，通过协程实现闪烁提示
    public void SetOverlapWarningWithBlink(bool hasOverlap)
    {
        if (highlightImage == null) return;

        // 停止之前的闪烁协程
        StopAllCoroutines();

        if (hasOverlap)
        {
            // 开始闪烁效果
            StartCoroutine(BlinkWarning());
            Debug.Log("InventoryHighlight: 开始重叠警告闪烁效果");
        }
        else
        {
            // 设置为正常绿色
            highlightImage.color = canPlaceColor;
            Debug.Log("InventoryHighlight: 停止闪烁，设置正常颜色");
        }
    }

    // 闪烁警告协程
    // 在红色和透明之间切换，产生闪烁效果
    private IEnumerator BlinkWarning()
    {
        float blinkInterval = 0.3f; // 闪烁间隔
        Color transparentRed = new Color(cannotPlaceColor.r, cannotPlaceColor.g, cannotPlaceColor.b, 0.2f);

        while (true)
        {
            // 设置为红色
            highlightImage.color = cannotPlaceColor;
            yield return new WaitForSeconds(blinkInterval);

            // 设置为半透明红色
            highlightImage.color = transparentRed;
            yield return new WaitForSeconds(blinkInterval);
        }
    }
}