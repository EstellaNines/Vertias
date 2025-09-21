using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 背包面板中的右键菜单管理服务：
/// - 确保全局唯一的菜单实例（在本背包面板作用域内）
/// - 提供在某物品中心打开、位置边界夹取、以及 Blocker 左键关闭
/// </summary>
[DisallowMultipleComponent]
public class BackpackContextMenuService : MonoBehaviour
{
    [Header("References")]
    [Tooltip("背包面板的根 RectTransform（承载菜单的参照系）")]
    [SerializeField] private RectTransform backpackPanelRect;

    [Tooltip("右键菜单预制体（建议为无 Canvas 的 UI 版本）")]
    [SerializeField] private RightClickMenuController rightClickMenuPrefab;

    [Tooltip("可选：菜单的父级容器，不设置则挂在 backpackPanelRect 下")]
    [SerializeField] private RectTransform menuHost;

    [Tooltip("透明点击拦截器（可动态创建）")]
    [SerializeField] private UIClickBlocker blockerPrefab;

    [Header("Options")]
    [SerializeField] private bool reuseMenuInstance = true;

    private Camera _uiCamera;
    private RightClickMenuController _menuInstance;
    private UIClickBlocker _blockerInstance;
    private RectTransform _menuRect;

    private void Awake()
    {
        if (backpackPanelRect == null)
        {
            backpackPanelRect = GetComponent<RectTransform>();
        }
        if (menuHost == null)
        {
            menuHost = backpackPanelRect;
        }

        var canvas = backpackPanelRect.GetComponentInParent<Canvas>();
        _uiCamera = canvas != null ? canvas.worldCamera : null;
    }

    /// <summary>
    /// 在 itemRect 几何中心打开菜单，并设置动作集合。
    /// </summary>
    public void ShowForItem(RectTransform itemRect, object itemData, IList<MenuAction> actions)
    {
        if (itemRect == null || backpackPanelRect == null || rightClickMenuPrefab == null)
        {
            Debug.LogWarning("BackpackContextMenuService: 缺少必要引用（itemRect/backpackPanelRect/rightClickMenuPrefab）。");
            return;
        }

        EnsureInstances();

        // 先规范菜单 Rect 的锚点：锚定到父容器中心，使 anchoredPosition 与父中心坐标一致
        if (_menuRect != null)
        {
            _menuRect.anchorMin = new Vector2(0.5f, 0.5f);
            _menuRect.anchorMax = new Vector2(0.5f, 0.5f);
            // 维持 pivot=0.5,0.5（默认即为0.5）
        }

        // 定位：物品中心 → 屏幕点 → 背包面板本地坐标（以父 pivot 为原点）
        Vector3 worldCenter = itemRect.TransformPoint(itemRect.rect.center);
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(_uiCamera, worldCenter);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(backpackPanelRect, screenPoint, _uiCamera, out Vector2 localPoint);

        // 先打开并刷新（以便获得正确尺寸）
        _menuInstance.OpenAt(localPoint, actions);

        // 根据尺寸进行 clamp
        _menuRect = _menuInstance.GetMenuRect();
        Vector2 menuSize = _menuInstance.GetCurrentSize();
        Vector2 clamped = ClampToBounds(localPoint, menuSize, backpackPanelRect.rect);
        _menuRect.anchoredPosition = clamped;

        // 显示 Blocker
        if (_blockerInstance != null)
        {
            var go = _blockerInstance.gameObject;
            go.SetActive(true);
            go.transform.SetAsLastSibling();
        }

        // 将菜单放到最前
        _menuRect.SetAsLastSibling();
    }

    /// <summary>
    /// 关闭菜单。
    /// </summary>
    public void Hide()
    {
        if (_menuInstance != null)
        {
            _menuInstance.Close();
        }
        if (_blockerInstance != null)
        {
            _blockerInstance.gameObject.SetActive(false);
        }
    }

    private void EnsureInstances()
    {
        if (_menuInstance == null || !reuseMenuInstance)
        {
            if (_menuInstance != null && !reuseMenuInstance)
            {
                Destroy(_menuInstance.gameObject);
                _menuInstance = null;
            }

            if (_menuInstance == null)
            {
                _menuInstance = Instantiate(rightClickMenuPrefab, menuHost);
                _menuInstance.gameObject.SetActive(false);
                _menuInstance.RequestClose = Hide;
            }
        }

        if (_blockerInstance == null && blockerPrefab != null)
        {
            _blockerInstance = Instantiate(blockerPrefab, backpackPanelRect);
            var blockerRT = _blockerInstance.GetComponent<RectTransform>();
            blockerRT.anchorMin = Vector2.zero;
            blockerRT.anchorMax = Vector2.one;
            blockerRT.offsetMin = Vector2.zero;
            blockerRT.offsetMax = Vector2.zero;

            // 使用 Image + RaycastTarget=true 的对象
            var img = _blockerInstance.GetComponent<Image>();
            if (img == null)
            {
                img = _blockerInstance.gameObject.AddComponent<Image>();
            }
            img.color = new Color(0, 0, 0, 0); // 全透明
            img.raycastTarget = true;

            _blockerInstance.Bind(Hide);
            _blockerInstance.gameObject.SetActive(false);
        }

        if (_menuInstance != null)
        {
            _menuRect = _menuInstance.GetMenuRect();
        }
    }

    private static Vector2 ClampToBounds(Vector2 localPoint, Vector2 size, Rect bounds)
    {
        // 将菜单中心放置在 localPoint 处（菜单 pivot=0.5, anchor=0.5）
        float halfW = size.x * 0.5f;
        float halfH = size.y * 0.5f;

        float minX = bounds.xMin + halfW - (0.5f - 0.5f) * bounds.width; // 表达清晰，保持通用形式
        float maxX = bounds.xMax - halfW - (0.5f - 0.5f) * bounds.width;
        float minY = bounds.yMin + halfH - (0.5f - 0.5f) * bounds.height;
        float maxY = bounds.yMax - halfH - (0.5f - 0.5f) * bounds.height;

        float x = Mathf.Clamp(localPoint.x, minX, maxX);
        float y = Mathf.Clamp(localPoint.y, minY, maxY);
        return new Vector2(x, y);
    }

    private void OnDisable()
    {
        Hide();
    }
}
