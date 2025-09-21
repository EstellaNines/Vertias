using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 示例：挂在物品 UI 上，右键时弹出背包右键菜单。
/// 将在 Inspector 中引用 BackpackContextMenuService。
/// </summary>
public class InventoryItemRightClickHandler : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private BackpackContextMenuService contextMenuService;
    [SerializeField] private RectTransform itemRect; // 如果为空则自动获取自身 RectTransform

    // 示例数据：根据你的物品数据结构替换
    [SerializeField] private string itemName = "Item";

    private void Reset()
    {
        itemRect = GetComponent<RectTransform>();
    }

    private void Awake()
    {
        if (itemRect == null) itemRect = GetComponent<RectTransform>();
        if (contextMenuService == null)
        {
            // 尝试从父级中查找
            contextMenuService = GetComponentInParent<BackpackContextMenuService>();
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData != null && eventData.button == PointerEventData.InputButton.Right)
        {
            if (contextMenuService == null || itemRect == null)
            {
                Debug.LogWarning("InventoryItemRightClickHandler: 缺少引用 contextMenuService 或 itemRect。");
                return;
            }

            var actions = BuildActions();
            contextMenuService.ShowForItem(itemRect, itemName, actions);
        }
    }

    private IList<MenuAction> BuildActions()
    {
        var list = new List<MenuAction>();
        list.Add(new MenuAction("use", "使用", () => Debug.Log($"使用 {itemName}")));
        list.Add(new MenuAction("inspect", "检查", () => Debug.Log($"检查 {itemName}")));
        list.Add(new MenuAction("drop", "丢弃", () => Debug.Log($"丢弃 {itemName}")));
        return list;
    }
}
