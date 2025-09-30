using System;
using UnityEngine;

/// <summary>
/// 表示右键菜单中的一个动作项。
/// </summary>
[Serializable]
public class MenuAction
{
    /// <summary>
    /// 动作唯一标识（可用于调试/统计/区分行为）。
    /// </summary>
    public string Id;

    /// <summary>
    /// 显示名称（按钮文本）。
    /// </summary>
    public string DisplayName;

    /// <summary>
    /// 可选图标。
    /// </summary>
    public Sprite Icon;

    /// <summary>
    /// 是否可交互（置灰）。
    /// </summary>
    public bool Interactable = true;

    /// <summary>
    /// 是否可见（隐藏按钮）。
    /// </summary>
    public bool Visible = true;

    /// <summary>
    /// 点击回调。
    /// </summary>
    [NonSerialized]
    public Action Callback;

    public MenuAction() { }

    public MenuAction(string id, string displayName, Action callback, bool interactable = true, bool visible = true, Sprite icon = null)
    {
        Id = id;
        DisplayName = displayName;
        Callback = callback;
        Interactable = interactable;
        Visible = visible;
        Icon = icon;
    }
}
