using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 放置高亮指示器
// 用于在物品放置时显示高亮：绿色表示可放置，红色表示不可放置
public class InventoryHighlight : MonoBehaviour
{
    [Header("高亮组件")]
    [SerializeField] private RectTransform highlighter; // 高亮框 RectTransform
    [SerializeField] private Image highlightImage; // 高亮框 Image 组件

    [Header("颜色设置")]
    [SerializeField] private Color canPlaceColor = new Color(0f, 1f, 0f, 0.5f); // 可放置颜色（绿色）
    [SerializeField] private Color cannotPlaceColor = new Color(1f, 0f, 0f, 0.5f); // 不可放置颜色（红色）

    private void Awake()
    {
        // 若未指定组件，则自动获取当前对象上的组件
        if (highlighter == null)
        {
            highlighter = GetComponent<RectTransform>();
        }

        if (highlightImage == null)
        {
            highlightImage = GetComponent<Image>();
        }

        // 初始时隐藏高亮
        Show(false);
    }

    // 根据物品设置高亮尺寸
    public void SetSize(Item targetItem)
    {
        if (highlighter == null || targetItem == null) return;

        Vector2 size = new Vector2();
        size.x = targetItem.GetWidth() * ItemGrid.tileSizeWidth;
        size.y = targetItem.GetHeight() * ItemGrid.tileSizeHeight;
        highlighter.sizeDelta = size;

        
    }

    // 直接以宽高设置高亮尺寸
    public void SetSize(int width, int height)
    {
        if (highlighter == null) return;

        Vector2 size = new Vector2();
        size.x = width * ItemGrid.tileSizeWidth;
        size.y = height * ItemGrid.tileSizeHeight;
        highlighter.sizeDelta = size;

        
    }

    // 设置高亮位置，使用物品当前网格位置
    public void SetPosition(ItemGrid targetGrid, Item targetItem)
    {
        if (highlighter == null || targetGrid == null || targetItem == null) return;

        Vector2 pos = targetGrid.CalculatePositionOnGrid(targetItem, targetItem.OnGridPosition.x, targetItem.OnGridPosition.y);
        highlighter.localPosition = pos;

        
    }

    // 设置高亮位置（指定格子坐标）
    // 使用 ItemGrid 的 CalculatePositionOnGrid，兼容旋转尺寸
    public void SetPosition(ItemGrid targetGrid, Item targetItem, int posX, int posY)
    {
        if (highlighter == null || targetGrid == null || targetItem == null) return;

        // 计算准确位置（已考虑物品当前尺寸与布局）
        Vector2 pos = targetGrid.CalculatePositionOnGrid(targetItem, posX, posY);
        highlighter.localPosition = pos;

        
    }

    // 简化版本的位置设置：与 ItemGrid 的计算规则一致
    public void SetPositionSimple(ItemGrid targetGrid, int posX, int posY)
    {
        if (highlighter == null || targetGrid == null) return;

        // 网格轴心为(0,1)，高亮框锚点也为(0,1)
        // 计算高亮框在网格坐标系中的位置
        
        // 获取高亮框的当前尺寸
        Vector2 highlightSize = highlighter.sizeDelta;
        float highlightWidth = highlightSize.x;
        float highlightHeight = highlightSize.y;
        
        // 计算高亮框左上角相对于网格左上角的位置
        // 高亮框锚点(0,1)对应高亮框的左上角
        // 要让高亮框左上角对齐到网格格子的左上角
        float gridCellLeftX = posX * ItemGrid.tileSizeWidth;
        float gridCellTopY = posY * ItemGrid.tileSizeHeight;
        
        // 高亮框左上角对齐到网格格子左上角
        Vector2 position = new Vector2();
        position.x = gridCellLeftX;
        position.y = -gridCellTopY; // Y轴向下为负

        highlighter.localPosition = position;
    }

    // 显示或隐藏高亮
    public void Show(bool show)
    {
        if (highlighter != null)
        {
            highlighter.gameObject.SetActive(show);
        }
    }

    // 设置高亮的父级
    public void SetParent(ItemGrid targetGrid)
    {
        if (highlighter == null || targetGrid == null) return;

        highlighter.SetParent(targetGrid.GetComponent<RectTransform>(), false);
    }

    // 设置高亮颜色（可放置/不可放置）
    public void SetCanPlace(bool canPlace)
    {
        if (highlightImage == null) return;

        Color targetColor = canPlace ? canPlaceColor : cannotPlaceColor;
        highlightImage.color = targetColor;

        
    }

    // 设置自定义颜色
    public void SetColor(Color color)
    {
        if (highlightImage == null) return;

        highlightImage.color = color;
    }

    // 获取高亮是否显示
    public bool IsShowing()
    {
        return highlighter != null && highlighter.gameObject.activeInHierarchy;
    }

    // 重置高亮为默认状态
    public void Reset()
    {
        Show(false);
        if (highlightImage != null)
        {
            highlightImage.color = canPlaceColor;
        }
        
    }

    // 设置重叠冲突效果
    // 用于在拖拽时显示与其他物品的冲突视觉效果
    public void SetOverlapWarning(bool hasOverlap)
    {
        if (highlightImage == null) return;

        if (hasOverlap)
        {
            // 红色表示存在冲突
            highlightImage.color = cannotPlaceColor;
        }
        else
        {
            // 绿色表示可以放置
            highlightImage.color = canPlaceColor;
        }
    }

    // 设置重叠冲突效果（带闪烁）
    // 可选的更强视觉提示，通过协程闪烁显示
    public void SetOverlapWarningWithBlink(bool hasOverlap)
    {
        if (highlightImage == null) return;

        // 停止之前的闪烁协程
        StopAllCoroutines();

        if (hasOverlap)
        {
            // 开始闪烁效果
            StartCoroutine(BlinkWarning());
        }
        else
        {
            // 还原为可放置颜色
            highlightImage.color = canPlaceColor;
        }
    }

    // 闪烁提示的协程
    // 在红色与半透明红色之间切换，形成闪烁效果
    private IEnumerator BlinkWarning()
    {
        float blinkInterval = 0.3f; // 闪烁间隔
        Color transparentRed = new Color(cannotPlaceColor.r, cannotPlaceColor.g, cannotPlaceColor.b, 0.2f);

        while (true)
        {
            // 纯红色
            highlightImage.color = cannotPlaceColor;
            yield return new WaitForSeconds(blinkInterval);

            // 半透明红色
            highlightImage.color = transparentRed;
            yield return new WaitForSeconds(blinkInterval);
        }
    }
}