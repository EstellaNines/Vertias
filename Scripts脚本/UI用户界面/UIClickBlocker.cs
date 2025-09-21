using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 全屏透明点击拦截器：用于点击菜单外部关闭菜单。
/// 仅响应鼠标左键点击。
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class UIClickBlocker : MonoBehaviour, IPointerClickHandler
{
    public System.Action OnLeftClick;

    public void Bind(System.Action onLeftClick)
    {
        OnLeftClick = onLeftClick;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData != null && eventData.button == PointerEventData.InputButton.Left)
        {
            OnLeftClick?.Invoke();
        }
    }
}
