using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 右键菜单控制器：负责根据 MenuAction 生成按钮并处理打开/关闭。
/// 支持三种来源顺序：
/// 1) explicitButtons（在 Inspector 直接绑定3个按钮）
/// 2) buttonsParent 子节点收集（如预制体中固定3个按钮）
/// 3) 可选 buttonTemplate 动态实例化补齐
/// </summary>
[DisallowMultipleComponent]
public class RightClickMenuController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform menuRect;
    [SerializeField] private Transform buttonsParent;
    [SerializeField] private Button buttonTemplate;
    [Tooltip("直接在此绑定固定按钮（顺序即显示顺序）。若已设置则优先使用此列表。")]
    [SerializeField] private List<Button> explicitButtons = new List<Button>();

    [Header("Behavior")]
    [SerializeField] private bool closeOnActionClick = true;

    public System.Action RequestClose; // 对外暴露的关闭请求（由服务监听并实际关闭）

    private readonly List<Button> _buttonPool = new List<Button>();

    private void Awake()
    {
        if (menuRect == null) menuRect = GetComponent<RectTransform>();

        BuildInitialPool();

        // 隐藏初始按钮，由 Refresh 决定显隐
        for (int i = 0; i < _buttonPool.Count; i++)
        {
            if (_buttonPool[i] != null) _buttonPool[i].gameObject.SetActive(false);
        }

        // 若提供了模板，将模板隐藏（并确保不计入可用按钮池）
        if (buttonTemplate != null)
        {
            buttonTemplate.gameObject.SetActive(false);
            _buttonPool.Remove(buttonTemplate);
        }
    }

    private void BuildInitialPool()
    {
        _buttonPool.Clear();

        // 1) 优先使用显式绑定的按钮
        if (explicitButtons != null && explicitButtons.Count > 0)
        {
            foreach (var b in explicitButtons)
            {
                if (b != null) _buttonPool.Add(b);
            }
            if (_buttonPool.Count > 0) return;
        }

        // 2) 否则收集父容器下现有按钮
        if (buttonsParent != null)
        {
            for (int i = 0; i < buttonsParent.childCount; i++)
            {
                var child = buttonsParent.GetChild(i);
                var btn = child.GetComponent<Button>();
                if (btn != null)
                {
                    _buttonPool.Add(btn);
                }
            }
        }
    }

    /// <summary>
    /// 在本地坐标（相对于父级 RectTransform）打开菜单，并根据 actions 刷新内容。
    /// </summary>
    public void OpenAt(Vector2 anchoredPosition, IList<MenuAction> actions)
    {
        gameObject.SetActive(true);
        Refresh(actions);
        SetAnchoredPosition(anchoredPosition);
        ForceRebuild();
    }

    /// <summary>
    /// 刷新按钮列表。
    /// </summary>
    public void Refresh(IList<MenuAction> actions)
    {
        int required = actions != null ? actions.Count : 0;
        EnsurePool(required);

        int usableCount = Mathf.Min(required, _buttonPool.Count);
        if (required > _buttonPool.Count)
        {
            Debug.LogWarning($"RightClickMenuController: 动作数量({required})超出可用按钮数量({_buttonPool.Count})，多余动作将被忽略。");
        }

        int index = 0;
        for (; index < usableCount; index++)
        {
            var btn = _buttonPool[index];
            SetupButton(btn, actions[index]);
            btn.gameObject.SetActive(actions[index].Visible);
        }

        for (; index < _buttonPool.Count; index++)
        {
            _buttonPool[index].gameObject.SetActive(false);
        }

        ForceRebuild();
    }

    /// <summary>
    /// 关闭菜单（仅隐藏）。
    /// </summary>
    public void Close()
    {
        gameObject.SetActive(false);
    }

    public RectTransform GetMenuRect() => menuRect;

    public Vector2 GetCurrentSize()
    {
        ForceRebuild();
        return menuRect != null ? menuRect.rect.size : Vector2.zero;
    }

    private void EnsurePool(int required)
    {
        // 若已有显式或父容器按钮，则仅当模板存在且需要更多按钮时才实例化
        if (buttonTemplate == null)
        {
            if (_buttonPool.Count == 0) BuildInitialPool();
            return;
        }

        while (_buttonPool.Count < required)
        {
            var btn = Instantiate(buttonTemplate, buttonsParent != null ? buttonsParent : menuRect);
            btn.gameObject.SetActive(false);
            _buttonPool.Add(btn);
        }
    }

    private void SetupButton(Button btn, MenuAction action)
    {
        btn.onClick.RemoveAllListeners();
        btn.interactable = action.Interactable;

        var label = btn.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            label.text = action.DisplayName ?? string.Empty;
        }
        var image = btn.GetComponentInChildren<Image>(true);
        if (image != null && action.Icon != null)
        {
            image.sprite = action.Icon;
            image.enabled = true;
        }

        btn.onClick.AddListener(() =>
        {
            try { action.Callback?.Invoke(); }
            catch (System.Exception ex) { Debug.LogException(ex); }
            if (closeOnActionClick)
            {
                RequestClose?.Invoke();
            }
        });
    }

    private void SetAnchoredPosition(Vector2 anchoredPosition)
    {
        if (menuRect != null)
        {
            menuRect.anchoredPosition = anchoredPosition;
        }
    }

    private void ForceRebuild()
    {
        if (menuRect == null) return;
        LayoutRebuilder.ForceRebuildLayoutImmediate(menuRect);
    }
}
