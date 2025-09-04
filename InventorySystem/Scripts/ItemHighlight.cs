using UnityEngine;
using UnityEngine.UI;

public class ItemHighlight : MonoBehaviour
{
    [Header("高亮设置")]
    [SerializeField] private Image highlightImage;
    [SerializeField] private Color highlightColor = new Color(1f, 1f, 1f, 0.3f);
    [SerializeField] private float fadeSpeed = 1f;

    private bool isHighlighted = false;
    private Color targetAlpha;

    private void Awake()
    {
        // 如果没有指定高亮图片，尝试查找
        if (highlightImage == null)
        {
            highlightImage = transform.Find("HoverHighlight")?.GetComponent<Image>();
        }

        if (highlightImage != null)
        {
            // 初始化为透明
            Color color = highlightColor;
            color.a = 0f;
            highlightImage.color = color;
            targetAlpha = color;
        }
    }

    private void Update()
    {
        // 平滑过渡透明度
        if (highlightImage != null && highlightImage.color != targetAlpha)
        {
            highlightImage.color = Color.Lerp(highlightImage.color, targetAlpha, fadeSpeed * Time.deltaTime);
        }
    }

    // 显示高亮效果
    public void ShowHighlight()
    {
        if (highlightImage != null && !isHighlighted)
        {
            isHighlighted = true;
            targetAlpha = highlightColor;
        }
    }

    // 隐藏高亮效果
    public void HideHighlight()
    {
        if (highlightImage != null && isHighlighted)
        {
            isHighlighted = false;
            Color color = highlightColor;
            color.a = 0f;
            targetAlpha = color;
        }
    }

    // 切换高亮状态
    public void ToggleHighlight()
    {
        if (isHighlighted)
            HideHighlight();
        else
            ShowHighlight();
    }

    // 设置高亮颜色
    public void SetHighlightColor(Color color)
    {
        highlightColor = color;
        if (isHighlighted)
        {
            targetAlpha = highlightColor;
        }
    }

    // 获取当前高亮状态
    public bool IsHighlighted => isHighlighted;
}