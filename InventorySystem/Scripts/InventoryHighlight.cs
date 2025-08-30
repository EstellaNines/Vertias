using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 物品放置高亮指示器
public class InventoryHighlight : MonoBehaviour
{
    [Header("高亮组件")]
    [SerializeField] private RectTransform highlighter;
    [SerializeField] private Image highlightImage;

    [Header("颜色设置")]
    [SerializeField] private Color canPlaceColor = new Color(0f, 1f, 0f, 0.5f);
    [SerializeField] private Color cannotPlaceColor = new Color(1f, 0f, 0f, 0.5f);

    private void Awake()
    {
        if (highlighter == null)
        {
            highlighter = GetComponent<RectTransform>();
        }

        if (highlightImage == null)
        {
            highlightImage = GetComponent<Image>();
        }

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

    // 直接设置高亮尺寸
    public void SetSize(int width, int height)
    {
        if (highlighter == null) return;

        Vector2 size = new Vector2();
        size.x = width * ItemGrid.tileSizeWidth;
        size.y = height * ItemGrid.tileSizeHeight;
        highlighter.sizeDelta = size;
    }

    // 设置高亮位置（使用物品当前位置）
    public void SetPosition(ItemGrid targetGrid, Item targetItem)
    {
        if (highlighter == null || targetGrid == null || targetItem == null) return;

        Vector2 pos = targetGrid.CalculatePositionOnGrid(targetItem, targetItem.OnGridPosition.x, targetItem.OnGridPosition.y);
        highlighter.localPosition = pos;
    }

    // 设置高亮位置（指定坐标）
    public void SetPosition(ItemGrid targetGrid, Item targetItem, int posX, int posY)
    {
        if (highlighter == null || targetGrid == null || targetItem == null) return;
    
        Vector2 pos = targetGrid.CalculatePositionOnGrid(targetItem, posX, posY);
        highlighter.localPosition = pos;
    }

    // 简化版位置设置
    public void SetPositionSimple(ItemGrid targetGrid, int posX, int posY)
    {
        if (highlighter == null || targetGrid == null) return;
    
        Vector2 position = new Vector2();
        Vector2 highlightSize = highlighter.sizeDelta;
        
        float highlightLeftTopX = posX * ItemGrid.tileSizeWidth;
        float highlightLeftTopY = posY * ItemGrid.tileSizeHeight;
        
        position.x = highlightLeftTopX + highlightSize.x * 0.5f;
        position.y = -(highlightLeftTopY + highlightSize.y * 0.5f);
        
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

    // 设置高亮颜色
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
    public void SetOverlapWarning(bool hasOverlap)
    {
        if (highlightImage == null) return;

        if (hasOverlap)
        {
            highlightImage.color = cannotPlaceColor;
        }
        else
        {
            highlightImage.color = canPlaceColor;
        }
    }

    // 设置重叠冲突效果（带闪烁）
    public void SetOverlapWarningWithBlink(bool hasOverlap)
    {
        if (highlightImage == null) return;

        StopAllCoroutines();

        if (hasOverlap)
        {
            StartCoroutine(BlinkWarning());
        }
        else
        {
            highlightImage.color = canPlaceColor;
        }
    }

    // 闪烁提示协程
    private IEnumerator BlinkWarning()
    {
        float blinkInterval = 0.3f;
        Color transparentRed = new Color(cannotPlaceColor.r, cannotPlaceColor.g, cannotPlaceColor.b, 0.2f);

        while (true)
        {
            highlightImage.color = cannotPlaceColor;
            yield return new WaitForSeconds(blinkInterval);

            highlightImage.color = transparentRed;
            yield return new WaitForSeconds(blinkInterval);
        }
    }
}