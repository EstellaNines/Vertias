using UnityEngine;

/// <summary>
/// 物品尺寸信息组件 - 用于保存物品的原始尺寸信息
/// </summary>
public class ItemSizeInfo : MonoBehaviour
{
    [Header("原始尺寸信息")]
    public Vector2 originalSize; // 原始大小
    public Vector3 originalScale; // 原始缩放
    public Vector2 originalAnchorMin; // 原始锚点最小值
    public Vector2 originalAnchorMax; // 原始锚点最大值
    public Vector2 originalPivot; // 原始轴心点
    public Vector2 originalAnchoredPosition; // 原始锚定位置

    /// <summary>
    /// 保存当前的尺寸信息
    /// </summary>
    public void SaveCurrentSize()
    {
        RectTransform rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            originalSize = rectTransform.sizeDelta;
            originalScale = rectTransform.localScale;
            originalAnchorMin = rectTransform.anchorMin;
            originalAnchorMax = rectTransform.anchorMax;
            originalPivot = rectTransform.pivot;
            originalAnchoredPosition = rectTransform.anchoredPosition;
        }
    }
}