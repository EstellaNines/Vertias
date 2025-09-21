using System.Collections.Generic;
using InventorySystem;
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
    [SerializeField] private Item targetItem; // 右键菜单对应的实际物品
    [SerializeField] private PlayerVitalStats playerStats; // 玩家生命值组件

    // 示例数据：根据你的物品数据结构替换
    [SerializeField] private string itemName = "Item";

    private void Reset()
    {
        itemRect = GetComponent<RectTransform>();
    }

    private void Awake()
    {
        if (itemRect == null) itemRect = GetComponent<RectTransform>();
        if (targetItem == null) targetItem = GetComponent<Item>();
        if (playerStats == null) playerStats = FindObjectOfType<PlayerVitalStats>();
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
            contextMenuService.ShowForItem(itemRect, targetItem != null ? (object)targetItem : itemName, actions);
        }
    }

    private IList<MenuAction> BuildActions()
    {
        var list = new List<MenuAction>();
        bool hasReader = targetItem != null && targetItem.ItemDataReader != null && targetItem.ItemDataReader.ItemData != null;
        bool isHealing = hasReader && targetItem.ItemDataReader.ItemData.category == ItemCategory.Healing;
        bool isFoodOrDrink = hasReader && (targetItem.ItemDataReader.ItemData.category == ItemCategory.Food || targetItem.ItemDataReader.ItemData.category == ItemCategory.Drink);

        // 使用：治疗类物品执行回血逻辑，其他物品仅占位（可按需扩展）
        if (isHealing)
        {
            bool canUse = CanUseHealing();
            list.Add(new MenuAction("use", "Use", () => UseHealing(), canUse));
        }
        else if (isFoodOrDrink)
        {
            bool canUse = CanUseFoodOrDrink();
            list.Add(new MenuAction("use", "Use", () => UseFoodOrDrink(), canUse));
        }
        else
        {
            list.Add(new MenuAction("use", "Use", () => Debug.Log($"Use {GetDisplayName()}")));
        }
        list.Add(new MenuAction("inspect", "Inspect", () => Debug.Log($"Inspect {GetDisplayName()}")));
        list.Add(new MenuAction("discard", "Discard", () => DiscardSelectedItem()));
        return list;
    }

    private string GetDisplayName()
    {
        if (targetItem != null && targetItem.ItemDataReader != null && targetItem.ItemDataReader.ItemData != null)
        {
            return targetItem.ItemDataReader.ItemData.GetDisplayName();
        }
        return itemName;
    }

    private void DiscardSelectedItem()
    {
        if (targetItem == null)
        {
            Debug.LogWarning("InventoryItemRightClickHandler: 无法丢弃，缺少 Item 引用。");
            return;
        }

        var grid = targetItem.OnGridReference;
        if (grid != null)
        {
            Vector2Int pos = targetItem.OnGridPosition;
            var removed = grid.PickUpItem(pos.x, pos.y);
            if (removed != null)
            {
                Destroy(removed.gameObject);
                return;
            }
        }
        Destroy(targetItem.gameObject);
    }

    private bool CanUseHealing()
    {
        if (playerStats == null || targetItem == null || targetItem.ItemDataReader == null || targetItem.ItemDataReader.ItemData == null)
            return false;

        if (targetItem.ItemDataReader.ItemData.category != ItemCategory.Healing)
            return false;

        // 需要有可用治疗量，且玩家未满血
        bool hasHealing = targetItem.ItemDataReader.currentHealAmount > 0 && targetItem.ItemDataReader.healPerUse > 0;
        bool needHeal = playerStats.currentHealth < playerStats.maxHealth;
        return hasHealing && needHeal;
    }

    private void UseHealing()
    {
        if (!CanUseHealing()) return;

        var reader = targetItem.ItemDataReader;
        float missing = Mathf.Max(0f, playerStats.maxHealth - playerStats.currentHealth);
        if (missing <= 0f) return;

        int perUse = Mathf.Max(0, reader.healPerUse);
        int available = Mathf.Max(0, reader.currentHealAmount);
        if (perUse <= 0 || available <= 0) return;

        // 实际回复量：按规则取 min(单次回复, 缺口)，并受剩余治疗量上限限制
        int intended = Mathf.Min(perUse, Mathf.CeilToInt(missing));
        int healAmount = Mathf.Min(intended, available);
        if (healAmount <= 0) return;

        // 应用治疗并扣减等量总治疗量
        playerStats.Heal(healAmount);
        reader.currentHealAmount = Mathf.Max(0, reader.currentHealAmount - healAmount);
        reader.UpdateUI();
        reader.SaveRuntimeToES3();
    }

    private bool CanUseFoodOrDrink()
    {
        if (playerStats == null || targetItem == null || targetItem.ItemDataReader == null || targetItem.ItemDataReader.ItemData == null)
            return false;

        var data = targetItem.ItemDataReader.ItemData;
        if (!(data.category == ItemCategory.Food || data.category == ItemCategory.Drink)) return false;

        bool hasUsage = targetItem.ItemDataReader.currentUsageCount > 0;
        bool needHunger = playerStats.currentHunger < playerStats.maxHunger;
        bool hasRestore = targetItem.ItemDataReader.hungerRestore > 0;
        return hasUsage && needHunger && hasRestore;
    }

    private void UseFoodOrDrink()
    {
        if (!CanUseFoodOrDrink()) return;

        var reader = targetItem.ItemDataReader;
        float missing = Mathf.Max(0f, playerStats.maxHunger - playerStats.currentHunger);
        if (missing <= 0f) return;

        int perUse = Mathf.Max(0, reader.hungerRestore);
        int uses = Mathf.Max(0, reader.currentUsageCount);
        if (perUse <= 0 || uses <= 0) return;

        float add = Mathf.Min(perUse, missing);
        playerStats.SetHunger(playerStats.currentHunger + add);

        reader.currentUsageCount = Mathf.Max(0, reader.currentUsageCount - 1);
        reader.UpdateUI();
        reader.SaveRuntimeToES3();

        if (reader.currentUsageCount <= 0)
        {
            // 用尽后销毁
            if (targetItem.OnGridReference != null)
            {
                Vector2Int pos = targetItem.OnGridPosition;
                var removed = targetItem.OnGridReference.PickUpItem(pos.x, pos.y);
                if (removed != null)
                {
                    Destroy(removed.gameObject);
                    return;
                }
            }
            Destroy(targetItem.gameObject);
        }
    }
}
