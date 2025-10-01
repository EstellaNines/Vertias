using System.Collections.Generic;
using InventorySystem;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 示例：挂在物品 UI 上，右键时弹出背包右键菜单。
/// 将在 Inspector 中引用 BackpackContextMenuService。
/// </summary>
public class InventoryItemRightClickHandler : MonoBehaviour, IPointerClickHandler
{
    private const int MONEY_ITEM_ID = 1301; // 1301_Money
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
            // 1) 先从父级中查找
            contextMenuService = GetComponentInParent<BackpackContextMenuService>(true);
        }
        if (contextMenuService == null)
        {
            // 2) 从场景中查找已存在的（包括未激活）
            var all = Resources.FindObjectsOfTypeAll<BackpackContextMenuService>();
            if (all != null && all.Length > 0)
            {
                // 优先选择在同一 Canvas 或最近的一个
                contextMenuService = FindNearestService(all);
            }
        }
        if (contextMenuService == null)
        {
            // 3) 运行时保障：自动创建服务 + Canvas + EventSystem
            EnsureEventSystemExists();
            var canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                canvas = CreateAutoCanvas();
            }
            var go = new GameObject("BackpackContextMenuService(Auto)");
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(canvas.transform, false);
            contextMenuService = go.AddComponent<BackpackContextMenuService>();
        }
    }

    private BackpackContextMenuService FindNearestService(BackpackContextMenuService[] services)
    {
        if (services == null || services.Length == 0) return null;
        if (itemRect == null) return services[0];

        BackpackContextMenuService best = null;
        float bestDist = float.MaxValue;
        foreach (var s in services)
        {
            if (s == null) continue;
            // 过滤非场景对象（资源/未载入）
            if (!s.gameObject.scene.IsValid() || !s.gameObject.scene.isLoaded) continue;
            var rt = s.transform as RectTransform;
            float d = 0f;
            if (rt != null)
            {
                d = Vector3.SqrMagnitude(rt.transform.position - itemRect.transform.position);
            }
            else
            {
                d = 0f;
            }
            if (d < bestDist)
            {
                bestDist = d;
                best = s;
            }
        }
        return best;
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
            // 点击时再次保障：若服务缺失则尝试创建
            if (contextMenuService == null)
            {
                EnsureEventSystemExists();
                var canvas = FindObjectOfType<Canvas>();
                if (canvas == null)
                {
                    canvas = CreateAutoCanvas();
                }
                var go = new GameObject("BackpackContextMenuService(Auto)");
                var rt = go.AddComponent<RectTransform>();
                rt.SetParent(canvas.transform, false);
                contextMenuService = go.AddComponent<BackpackContextMenuService>();
            }

            if (contextMenuService == null || itemRect == null)
            {
                Debug.LogWarning("InventoryItemRightClickHandler: 缺少引用 contextMenuService 或 itemRect。");
                return;
            }

            var actions = BuildActions();
            // 如果物品所在网格为交易类型，将第一个“Use”按钮替换为“购买”
            TryRewriteUseToBuy(actions);
            contextMenuService.ShowForItem(itemRect, targetItem != null ? (object)targetItem : itemName, actions);
        }
    }

    private Canvas CreateAutoCanvas()
    {
        var canvasGO = new GameObject("UI Root (Auto)");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private void EnsureEventSystemExists()
    {
        var es = FindObjectOfType<EventSystem>();
        if (es != null) return;
        var esGO = new GameObject("EventSystem");
        esGO.AddComponent<EventSystem>();
        esGO.AddComponent<StandaloneInputModule>();
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

	private void TryRewriteUseToBuy(IList<MenuAction> actions)
    {
        if (actions == null || actions.Count == 0) return;
        if (!IsOnTradingGrid()) return;
		bool hasEnoughMoney = HasEnoughMoneyForCurrentItem();
		bool hasSlot = HasPlacementSlotForCurrentItem();
        // 查找第一个 id 为 "use" 的动作并改名为“购买”
        for (int i = 0; i < actions.Count; i++)
        {
            var a = actions[i];
            if (a != null && a.Id == "use")
            {
				a.Id = "buy";
				a.DisplayName = "Buy";
				a.Interactable = hasEnoughMoney && hasSlot;
				a.Callback = () => BuySelectedItem();
                // 现阶段仅更改展示名称；购买逻辑可在后续接入（扣款/入包等）
                break;
            }
        }
    }

    private bool IsOnTradingGrid()
    {
        if (targetItem == null || targetItem.OnGridReference == null) return false;
        return targetItem.OnGridReference.GridType == GridType.Trading;
    }

	private bool HasEnoughMoneyForCurrentItem()
	{
		int price = GetSelectedItemPrice();
		if (price <= 0) return true; // 免费或未配置价格：允许购买
		int totalMoney = GetTotalPlayerMoney();
		return totalMoney >= price;
	}

	private int GetSelectedItemPrice()
	{
		if (targetItem != null && targetItem.ItemDataReader != null)
		{
			var rdr = targetItem.ItemDataReader;
			int unitPrice = 0;
			if (rdr.price > 0) unitPrice = rdr.price;
			else if (rdr.ItemData != null && rdr.ItemData.price > 0) unitPrice = rdr.ItemData.price;
			if (unitPrice <= 0) return 0;
			// 子弹：售价为单发价格，总价=单价*总数
			if (rdr.ItemData != null && rdr.ItemData.category == ItemCategory.Ammunition)
			{
				int count = Mathf.Max(1, rdr.CurrentStack);
				return unitPrice * count;
			}
			return unitPrice;
		}
		return 0;
	}

    private bool HasPlacementSlotForCurrentItem()
    {
        // 使用与实际购买相同的寻位逻辑，避免判定不一致导致“有空位却被禁用”
        return TryFindPlacementInPlayerGrids(targetItem, out _, out _);
    }

    private System.Collections.Generic.List<ItemGrid> GetPlacementGridsInPriority()
	{
		var all = FindObjectsOfType<ItemGrid>(true);
		if (all == null || all.Length == 0) return null;
		var result = new System.Collections.Generic.List<ItemGrid>();
        // 按优先顺序：Container -> Equipment -> Backpack -> Player -> Custom
        // 说明：某些运行期容器网格会将 GridType 从 Backpack 切换为 Equipment，这里一并纳入“容器优先”
        GridType[] order = new GridType[] { GridType.Container, GridType.Equipment, GridType.Backpack, GridType.Player, GridType.Custom };
		for (int o = 0; o < order.Length; o++)
		{
			for (int i = 0; i < all.Length; i++)
			{
				if (all[i] != null && all[i].GridType == order[o])
				{
					result.Add(all[i]);
				}
			}
		}
		return result;
	}

	private int GetTotalPlayerMoney()
	{
		int total = 0;
		var grids = FindObjectsOfType<ItemGrid>(true);
		if (grids == null || grids.Length == 0) return 0;
		for (int i = 0; i < grids.Length; i++)
		{
			var g = grids[i];
			if (g == null) continue;
			var type = g.GridType;
			if (!(type == GridType.Backpack || type == GridType.Equipment || type == GridType.Player))
				continue;
			for (int c = 0; c < g.transform.childCount; c++)
			{
				var child = g.transform.GetChild(c);
				if (child == null) continue;
				var rdr = child.GetComponent<ItemDataReader>();
				if (rdr == null || rdr.ItemData == null) continue;
				if (rdr.ItemData.id != MONEY_ITEM_ID) continue;
				// 计算该堆的金额：优先用堆叠数（CurrentStack），若为0则用currencyAmount
				int amount = rdr.CurrentStack > 0 ? rdr.CurrentStack : rdr.currencyAmount;
				if (amount > 0) total += amount;
			}
		}
		return total;
	}

	private void BuySelectedItem()
	{
		if (!IsOnTradingGrid()) return;
		if (targetItem == null) return;
		int price = GetSelectedItemPrice();
		if (price < 0) price = 0;
		int totalMoney = GetTotalPlayerMoney();
		if (totalMoney < price)
		{
			Debug.LogWarning("Buy failed: not enough money.");
			return;
		}

		// 先尝试为物品寻找目标网格与位置
		ItemGrid dstGrid;
		Vector2Int dstPos;
		if (!TryFindPlacementInPlayerGrids(targetItem, out dstGrid, out dstPos))
		{
			Debug.LogWarning("Buy failed: no space in player grids.");
			return;
		}

		// 扣费
		if (!TryDeductMoney(price))
		{
			Debug.LogWarning("Buy failed: deduct money error.");
			return;
		}

		// 从交易网格取下并放入玩家网格
		var srcGrid = targetItem.OnGridReference;
		Vector2Int srcPos = targetItem.OnGridPosition;
		Item removed = null;
		if (srcGrid != null)
		{
			removed = srcGrid.PickUpItem(srcPos.x, srcPos.y);
		}
		var itemToPlace = removed != null ? removed : targetItem;
		bool placed = dstGrid.PlaceItem(itemToPlace, dstPos.x, dstPos.y);
		if (!placed)
		{
			// 回滚：放回原网格
			if (removed != null && srcGrid != null)
			{
				// 简化回滚：尝试原位放回
				srcGrid.PlaceItem(removed, srcPos.x, srcPos.y);
			}
			Debug.LogWarning("Buy failed: could not place in destination grid.");
			return;
		}

		Debug.Log("Buy succeeded.");
	}

	private bool TryFindPlacementInPlayerGrids(Item item, out ItemGrid dstGrid, out Vector2Int dstPos)
	{
		dstGrid = null;
		dstPos = default;
		if (item == null) return false;
		int w = item.GetWidth();
		int h = item.GetHeight();

		var grids = FindObjectsOfType<ItemGrid>(true);
		if (grids == null || grids.Length == 0) return false;

        // 按优先顺序尝试：Container -> Equipment -> Backpack -> Player -> Custom
        GridType[] prefs = new GridType[] { GridType.Container, GridType.Equipment, GridType.Backpack, GridType.Player, GridType.Custom };
		for (int p = 0; p < prefs.Length; p++)
		{
			for (int i = 0; i < grids.Length; i++)
			{
				var g = grids[i];
				if (g == null || g.GridType != prefs[p]) continue;
				// 遍历可行位置
				for (int x = 0; x <= g.CurrentWidth - w; x++)
				{
					for (int y = 0; y <= g.CurrentHeight - h; y++)
					{
						if (g.CanPlaceItemAtPosition(x, y, w, h, item))
						{
							dstGrid = g;
							dstPos = new Vector2Int(x, y);
							return true;
						}
					}
				}
			}
		}
		return false;
	}

	private bool TryDeductMoney(int price)
	{
		int remaining = Mathf.Max(0, price);
		if (remaining == 0) return true;
		var grids = FindObjectsOfType<ItemGrid>(true);
		if (grids == null || grids.Length == 0) return false;

		// 仅在玩家所属网格扣费
		for (int i = 0; i < grids.Length && remaining > 0; i++)
		{
			var g = grids[i];
			if (g == null) continue;
			var type = g.GridType;
			if (!(type == GridType.Backpack || type == GridType.Equipment || type == GridType.Player))
				continue;

			for (int c = 0; c < g.transform.childCount && remaining > 0; c++)
			{
				var child = g.transform.GetChild(c);
				if (child == null) continue;
				var rdr = child.GetComponent<ItemDataReader>();
				if (rdr == null || rdr.ItemData == null) continue;
				if (rdr.ItemData.id != MONEY_ITEM_ID) continue;
				int amount = rdr.CurrentStack > 0 ? rdr.CurrentStack : rdr.currencyAmount;
				if (amount <= 0) continue;

				int take = Mathf.Min(amount, remaining);
				if (rdr.CurrentStack > 0)
				{
					rdr.SetStack(rdr.CurrentStack - take);
				}
				else
				{
					rdr.currencyAmount = Mathf.Max(0, rdr.currencyAmount - take);
					rdr.UpdateUI();
					rdr.SaveRuntimeToES3();
				}
				remaining -= take;

				// 若该堆已空，且其为非堆叠货币，考虑删除节点
				int left = rdr.CurrentStack > 0 ? rdr.CurrentStack : rdr.currencyAmount;
				if (left <= 0)
				{
					var moneyItem = child.GetComponent<Item>();
					if (moneyItem != null && g != null)
					{
						Vector2Int pos = moneyItem.OnGridPosition;
						var removed = g.PickUpItem(pos.x, pos.y);
						if (removed != null)
						{
							Destroy(removed.gameObject);
						}
					}
					else
					{
						Destroy(child.gameObject);
					}
				}
			}
		}

		return remaining <= 0;
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

        // 需要有可用治疗量，且玩家未满“基础血量上限”（不含护甲/头盔叠加）
        bool hasHealing = targetItem.ItemDataReader.currentHealAmount > 0 && targetItem.ItemDataReader.healPerUse > 0;
        float baseCap = GetBaseHealthCap();
        bool needHeal = playerStats.currentHealth < baseCap;
        return hasHealing && needHeal;
    }

    private void UseHealing()
    {
        if (!CanUseHealing()) return;

        var reader = targetItem.ItemDataReader;
        // 仅治疗到基础生命上限（不含护甲/头盔叠加）
        float baseCap = GetBaseHealthCap();
        float missing = Mathf.Max(0f, baseCap - playerStats.currentHealth);
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

    /// <summary>
    /// 基础生命上限（不包含护甲/头盔加成）。
    /// 默认 100；若场景中存在 ProtectiveGearHealthIntegration，则读取其 baseHealth。
    /// </summary>
    private float GetBaseHealthCap()
    {
        var gear = FindObjectOfType<ProtectiveGearHealthIntegration>();
        if (gear != null && gear.baseHealth > 0f) return gear.baseHealth;
        return 100f;
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
