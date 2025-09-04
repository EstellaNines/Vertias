冲突解决策略  
1. 保留你本地（TPS-v0.2.6-5-Beta1-背包系统重做）的全部代码——功能最全、最新。  
2. 把 main 分支里“纯注释”或“空行”类改动拿过来，不影响逻辑，但能提升可读性。  
3. 其余 main 分支的代码（特别是 `SetPositionSimple` 重写、缺失的 `SetEquipmentSlotHighlight` 方法等）全部丢弃。  

整合后的最终文件（可直接覆盖到仓库，解决冲突）：  

```csharp
// ==========================================================================
// 物品放置高亮指示器
// 用于在物品放置时显示高亮：绿色表示可放置，红色表示不可放置
// ==========================================================================
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryHighlight : MonoBehaviour
{
    [Header("高亮组件")]
    [SerializeField] private RectTransform highlighter;   // 高亮框 RectTransform
    [SerializeField] private Image highlightImage;        // 高亮框 Image 组件

    [Header("颜色设置")]
    [SerializeField] private Color canPlaceColor    = new Color(0f, 1f, 0f, 0.5f); // 可放置颜色（绿色）
    [SerializeField] private Color cannotPlaceColor = new Color(1f, 0f, 0f, 0.5f); // 不可放置颜色（红色）

    private void Awake()
    {
        // 若未指定组件，则自动获取当前对象上的组件
        if (highlighter == null)
            highlighter = GetComponent<RectTransform>();

        if (highlightImage == null)
            highlightImage = GetComponent<Image>();

        // 初始时隐藏高亮
        Show(false);
    }

    #region 尺寸与位置
    /// <summary>根据物品设置高亮尺寸</summary>
    public void SetSize(Item targetItem)
    {
        if (highlighter == null || targetItem == null) return;

        Vector2 size = new Vector2
        {
            x = targetItem.GetWidth()  * ItemGrid.tileSizeWidth,
            y = targetItem.GetHeight() * ItemGrid.tileSizeHeight
        };
        highlighter.sizeDelta = size;
    }

    /// <summary>直接以宽高设置高亮尺寸</summary>
    public void SetSize(int width, int height)
    {
        if (highlighter == null) return;

        Vector2 size = new Vector2
        {
            x = width  * ItemGrid.tileSizeWidth,
            y = height * ItemGrid.tileSizeHeight
        };
        highlighter.sizeDelta = size;
    }

    /// <summary>设置高亮位置，使用物品当前网格位置</summary>
    public void SetPosition(ItemGrid targetGrid, Item targetItem)
    {
        if (highlighter == null || targetGrid == null || targetItem == null) return;

        Vector2 pos = targetGrid.CalculatePositionOnGrid(
            targetItem,
            targetItem.OnGridPosition.x,
            targetItem.OnGridPosition.y);
        highlighter.localPosition = pos;
    }

    /// <summary>设置高亮位置（指定格子坐标）</summary>
    public void SetPosition(ItemGrid targetGrid, Item targetItem, int posX, int posY)
    {
        if (highlighter == null || targetGrid == null || targetItem == null) return;

        Vector2 pos = targetGrid.CalculatePositionOnGrid(targetItem, posX, posY);
        highlighter.localPosition = pos;
    }

    /// <summary>简化版本的位置设置，与 ItemGrid 计算规则一致</summary>
    public void SetPositionSimple(ItemGrid targetGrid, int posX, int posY)
    {
        if (highlighter == null || targetGrid == null) return;

        Vector2 highlightSize = highlighter.sizeDelta;

        float leftTopX = posX * ItemGrid.tileSizeWidth;
        float leftTopY = posY * ItemGrid.tileSizeHeight;

        Vector2 position = new Vector2
        {
            x = leftTopX + highlightSize.x * 0.5f,
            y = -(leftTopY + highlightSize.y * 0.5f)
        };
        highlighter.localPosition = position;
    }
    #endregion

    #region 显示控制
    /// <summary>显示或隐藏高亮</summary>
    public void Show(bool show)
    {
        if (highlighter != null)
            highlighter.gameObject.SetActive(show);
    }

    /// <summary>获取高亮是否显示</summary>
    public bool IsShowing() =>
        highlighter != null && highlighter.gameObject.activeInHierarchy;
    #endregion

    #region 父级与颜色
    /// <summary>设置高亮的父级</summary>
    public void SetParent(ItemGrid targetGrid)
    {
        if (highlighter == null || targetGrid == null) return;
        highlighter.SetParent(targetGrid.GetComponent<RectTransform>(), false);
    }

    /// <summary>设置高亮颜色（可放置/不可放置）</summary>
    public void SetCanPlace(bool canPlace)
    {
        if (highlightImage == null) return;
        highlightImage.color = canPlace ? canPlaceColor : cannotPlaceColor;
    }

    /// <summary>设置自定义颜色</summary>
    public void SetColor(Color color)
    {
        if (highlightImage == null) return;
        highlightImage.color = color;
    }

    /// <summary>重置高亮为默认状态</summary>
    public void Reset()
    {
        Show(false);
        if (highlightImage != null)
            highlightImage.color = canPlaceColor;
    }
    #endregion

    #region 装备槽专用
    /// <summary>为装备槽设置高亮</summary>
    /// <param name="equipmentSlot">目标装备槽</param>
    /// <param name="canEquip">是否可以装备</param>
    public void SetEquipmentSlotHighlight(InventorySystem.EquipmentSlot equipmentSlot, bool canEquip)
    {
        if (highlighter == null || equipmentSlot == null) return;

        // 设置父级为装备槽
        highlighter.SetParent(equipmentSlot.transform);

        // 设置高亮尺寸为装备槽尺寸
        RectTransform slotRect = equipmentSlot.GetComponent<RectTransform>();
        if (slotRect != null)
            highlighter.sizeDelta = slotRect.sizeDelta;

        // 设置高亮位置为装备槽中心
        highlighter.localPosition   = Vector3.zero;
        highlighter.anchorMin       = new Vector2(0.5f, 0.5f);
        highlighter.anchorMax       = new Vector2(0.5f, 0.5f);
        highlighter.pivot           = new Vector2(0.5f, 0.5f);

        // 设置高亮颜色并显示
        SetCanPlace(canEquip);
        Show(true);
    }
    #endregion

    #region 冲突提示
    /// <summary>设置重叠冲突效果</summary>
    public void SetOverlapWarning(bool hasOverlap)
    {
        if (highlightImage == null) return;
        highlightImage.color = hasOverlap ? cannotPlaceColor : canPlaceColor;
    }

    /// <summary>设置重叠冲突效果（带闪烁）</summary>
    public void SetOverlapWarningWithBlink(bool hasOverlap)
    {
        if (highlightImage == null) return;

        StopAllCoroutines();

        if (hasOverlap)
            StartCoroutine(BlinkWarning());
        else
            highlightImage.color = canPlaceColor;
    }

    /// <summary>闪烁提示的协程</summary>
    private IEnumerator BlinkWarning()
    {
        float blinkInterval = 0.3f; // 闪烁间隔
        Color transparentRed = new Color(
            cannotPlaceColor.r,
            cannotPlaceColor.g,
            cannotPlaceColor.b,
            0.2f);

        while (true)
        {
            highlightImage.color = cannotPlaceColor;   // 纯红色
            yield return new WaitForSeconds(blinkInterval);

            highlightImage.color = transparentRed;     // 半透明红色
            yield return new WaitForSeconds(blinkInterval);
        }
    }
    #endregion
}
```

把以上代码直接提交即可解决该 Git 冲突，同时保留你所需的所有功能与注释。