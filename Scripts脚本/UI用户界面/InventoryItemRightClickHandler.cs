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
		// 左键双击：打开检查界面
		if (eventData != null && eventData.button == PointerEventData.InputButton.Left && eventData.clickCount >= 2)
		{
			OpenInspectPanel();
			return;
		}

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
        bool isSedative = hasReader && targetItem.ItemDataReader.ItemData.category == ItemCategory.Sedative;
        bool isEquipCategory = hasReader && IsEquipCategory(targetItem.ItemDataReader.ItemData.category);

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
        else if (isSedative)
        {
            bool canUse = CanUseSedative();
            list.Add(new MenuAction("use", "Use", () => UseSedative(), canUse));
        }
        else if (isEquipCategory)
        {
            bool canUse = CanAutoEquipSelectedItem();
            list.Add(new MenuAction("use", "Use", () => AutoEquipSelectedItem(), canUse));
        }
        else
        {
            list.Add(new MenuAction("use", "Use", () => Debug.Log($"Use {GetDisplayName()}")));
        }
        list.Add(new MenuAction("inspect", "Inspect", () => OpenInspectPanel()));
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

    [Header("检查面板预制体设置")]
    [SerializeField] private CheckInterfacePanelController checkPanelPrefab;
    [SerializeField] private RectTransform checkPanelParent; // 若为空，将放到当前 Canvas 根下

    private void OpenInspectPanel()
    {
        if (targetItem == null || targetItem.ItemDataReader == null)
        {
            Debug.LogWarning("InventoryItemRightClickHandler: 无法检查，缺少 ItemDataReader。");
            return;
        }

        CheckInterfacePanelController panel = null;

        // 选择父节点：优先使用配置的父节点；其次使用最近的 BackpackPanelController；最后使用最近 Canvas
        RectTransform parentForPanel = ResolvePanelParent();

        // 1) 优先查找激活/未激活的已存在实例
        panel = FindObjectOfType<CheckInterfacePanelController>(true);

        // 2) 不存在则尝试通过预制体实例化
        if (panel == null)
        {
            if (checkPanelPrefab != null)
            {
                var instance = Instantiate(checkPanelPrefab, parentForPanel);
                panel = instance.GetComponent<CheckInterfacePanelController>();
                if (panel == null)
                {
                    Debug.LogError("InventoryItemRightClickHandler: 预制体缺少 CheckInterfacePanelController 组件。");
                    Destroy(instance.gameObject);
                    return;
                }
                // 新实例默认只隐藏关闭
                panel.SetDestroyOnClose(false);
            }
            else
            {
                Debug.LogWarning("InventoryItemRightClickHandler: 未配置检查面板预制体，且场景中也未找到实例。");
                return;
            }
        }

        // 若已有实例但父节点不匹配，则重设父节点
        var panelRT = panel.GetComponent<RectTransform>();
        if (panelRT != null && parentForPanel != null && panelRT.parent != parentForPanel)
        {
            panelRT.SetParent(parentForPanel, false);
        }

        // 居中到父容器中心
        CenterPanel(panelRT);

        panel.gameObject.SetActive(true);
        panel.ShowForItem(targetItem.ItemDataReader);
    }

    private RectTransform ResolvePanelParent()
    {
        if (checkPanelParent != null) return checkPanelParent;

        // 优先找背包面板控制器作为父节点（整个背包板块）
        var backpack = GetComponentInParent<BackpackPanelController>(true);
        if (backpack != null)
        {
            var rt = backpack.transform as RectTransform;
            if (rt != null) return rt;
        }

        // 退化：最近 Canvas
        var canvas = GetComponentInParent<Canvas>();
        return canvas != null ? canvas.transform as RectTransform : null;
    }

    private void CenterPanel(RectTransform panelRT)
    {
        if (panelRT == null) return;
        panelRT.anchorMin = new Vector2(0.5f, 0.5f);
        panelRT.anchorMax = new Vector2(0.5f, 0.5f);
        panelRT.pivot = new Vector2(0.5f, 0.5f);
        panelRT.anchoredPosition = Vector2.zero;
        panelRT.localScale = Vector3.one;
        panelRT.SetAsLastSibling();
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

    private bool CanUseSedative()
    {
        if (playerStats == null || targetItem == null || targetItem.ItemDataReader == null || targetItem.ItemDataReader.ItemData == null)
            return false;

        var data = targetItem.ItemDataReader.ItemData;
        if (data.category != ItemCategory.Sedative) return false;

        bool hasUsage = targetItem.ItemDataReader.currentUsageCount > 0;
        bool needMental = playerStats.currentMental < playerStats.maxMental;
        bool hasRestore = targetItem.ItemDataReader.mentalRestore > 0;
        return hasUsage && needMental && hasRestore;
    }

    private void UseSedative()
    {
        if (!CanUseSedative()) return;

        var reader = targetItem.ItemDataReader;
        float missing = Mathf.Max(0f, playerStats.maxMental - playerStats.currentMental);
        if (missing <= 0f) return;

        int perUse = Mathf.Max(0, reader.mentalRestore);
        int uses = Mathf.Max(0, reader.currentUsageCount);
        if (perUse <= 0 || uses <= 0) return;

        float add = Mathf.Min(perUse, missing);
        playerStats.SetMental(playerStats.currentMental + add);

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

    private bool IsEquipCategory(ItemCategory category)
    {
        return category == ItemCategory.Helmet ||
            category == ItemCategory.Armor ||
            category == ItemCategory.TacticalRig ||
            category == ItemCategory.Backpack ||
            category == ItemCategory.Weapon;
    }

    private bool CanAutoEquipSelectedItem()
    {
        if (targetItem == null || targetItem.ItemDataReader == null || targetItem.ItemDataReader.ItemData == null) return false;
        var reader = targetItem.ItemDataReader;
        var slot = FindBestEquipmentSlotFor(reader);
        return slot != null && slot.CanAcceptItem(reader);
    }

    private void AutoEquipSelectedItem()
    {
        if (targetItem == null || targetItem.ItemDataReader == null) return;
        var reader = targetItem.ItemDataReader;
        var slot = FindBestEquipmentSlotFor(reader);
        if (slot == null)
        {
            Debug.LogWarning("InventoryItemRightClickHandler: 未找到可用的装备槽。");
            return;
        }
        bool ok = slot.EquipItem(reader);
        if (!ok)
        {
            Debug.LogWarning($"InventoryItemRightClickHandler: 装备 {reader.ItemData.itemName} 失败，槽位不兼容或不允许替换。");
        }
    }

    private EquipmentSlot FindBestEquipmentSlotFor(ItemDataReader reader)
    {
        if (reader == null || reader.ItemData == null) return null;

        var allSlots = FindObjectsOfType<EquipmentSlot>(true);
        if (allSlots == null || allSlots.Length == 0) return null;

        ItemCategory cat = reader.ItemData.category;

        // 武器：优先主武器，其次副武器
        if (cat == ItemCategory.Weapon)
        {
            EquipmentSlot primary = null;
            EquipmentSlot secondary = null;
            foreach (var s in allSlots)
            {
                if (s == null || s.config == null) continue;
                if (s.config.slotType == EquipmentSlotType.PrimaryWeapon) primary = s;
                else if (s.config.slotType == EquipmentSlotType.SecondaryWeapon) secondary = s;
            }
            if (primary != null && primary.CanAcceptItem(reader)) return primary;
            if (secondary != null && secondary.CanAcceptItem(reader)) return secondary;
            // 如果都不接受，尝试允许替换的优先（返回任一存在的槽）
            return primary ?? secondary;
        }

        EquipmentSlotType targetType = MapCategoryToSlotType(cat);
        if (targetType == 0) return null;

        EquipmentSlot found = null;
        foreach (var s in allSlots)
        {
            if (s == null || s.config == null) continue;
            if (s.config.slotType != targetType) continue;
            if (s.CanAcceptItem(reader)) return s;
            // 记录一个候选（即使当前不接受，稍后可能用于替换失败提示）
            found = s;
        }
        return found;
    }

    private EquipmentSlotType MapCategoryToSlotType(ItemCategory cat)
    {
        switch (cat)
        {
            case ItemCategory.Helmet: return EquipmentSlotType.Helmet;
            case ItemCategory.Armor: return EquipmentSlotType.Armor;
            case ItemCategory.TacticalRig: return EquipmentSlotType.TacticalRig;
            case ItemCategory.Backpack: return EquipmentSlotType.Backpack;
            default: return 0;
        }
    }
}
