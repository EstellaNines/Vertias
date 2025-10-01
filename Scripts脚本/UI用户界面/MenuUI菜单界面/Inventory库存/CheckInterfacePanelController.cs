using UnityEngine;
using UnityEngine.UI;
using TMPro;
using InventorySystem;

public class CheckInterfacePanelController : MonoBehaviour
{
	[Header("根节点引用（可留空，按路径自动获取）")]
	[SerializeField] private RectTransform headerRoot;               // Header
	[SerializeField] private RectTransform itemIconRoot;            // ItemIcon
	[SerializeField] private RectTransform descriptionRoot;         // Description

	[Header("Header 组件")]
	[SerializeField] private TextMeshProUGUI headerText;            // Header 下的 TMP（显示：英文类别 + 英文全称）
	[SerializeField] private Button closeButton;                    // Header 右上角关闭按钮
	[SerializeField] private bool destroyOnClose = false;           // 关闭时销毁或仅隐藏

	[Header("ItemIcon 组件")]
	[SerializeField] private Image itemIconImage;                   // ItemIcon 的 Image
	[SerializeField] private RectTransform itemIconRect;            // ItemIcon 的 RectTransform

	[Header("LeftPattern 组件（标签与数据各3行）")]
	[SerializeField] private TextMeshProUGUI[] leftFixedTexts = new TextMeshProUGUI[3];
	[SerializeField] private TextMeshProUGUI[] leftDataTexts = new TextMeshProUGUI[3];

	[Header("RightPattern 组件（标签与数据各3行，可按物品类别隐藏行）")]
	[SerializeField] private RectTransform rightFixedRoot;
	[SerializeField] private RectTransform rightDataRoot;
	[SerializeField] private TextMeshProUGUI[] rightFixedTexts = new TextMeshProUGUI[3];
	[SerializeField] private TextMeshProUGUI[] rightDataTexts = new TextMeshProUGUI[3];

	[Header("价格显示（Price）")]
	[SerializeField] private TextMeshProUGUI priceText;                // Description/Price 的TMP
	[SerializeField] private bool showPrice = true;                    // 是否显示价格
	[SerializeField] private Color priceLabelColor = Color.white;      // “Price:” 标签颜色
	[SerializeField] private Color rarity1Color = new Color(0.2f, 0.85f, 0.2f); // 绿色（常见）
	[SerializeField] private Color rarity2Color = new Color(1f, 0.85f, 0.2f);   // 黄色（稀有）
	[SerializeField] private Color rarity3Color = new Color(1f, 0.55f, 0f);     // 橙色（史诗）
	[SerializeField] private Color rarity4Color = new Color(1f, 0.2f, 0.2f);    // 红色（传说）
	[SerializeField] private int rarity1Price = 100;                   // 各稀有度默认价格（可在Inspector配置）
	[SerializeField] private int rarity2Price = 300;
	[SerializeField] private int rarity3Price = 800;
	[SerializeField] private int rarity4Price = 1500;

	[Header("Owner 显示（玩家Money总额）")]
	[SerializeField] private TextMeshProUGUI ownerText;                 // Description/Owner 的TMP
	[SerializeField] private bool showOwnerMoney = true;                // 是否显示玩家Money
	[SerializeField] private string ownerLabel = "Owner:";             // 左侧标签

#if UNITY_EDITOR
	// 编辑器预览：缓存一个临时的 ItemDataReader 以便在编辑模式下展示
	private ItemDataReader editorPreviewReader;
#endif

	private void Awake()
	{
		// 自动查找
		if (headerRoot == null) headerRoot = transform.Find("Header") as RectTransform;
		if (itemIconRoot == null) itemIconRoot = transform.Find("ItemIcon") as RectTransform;
		if (descriptionRoot == null) descriptionRoot = transform.Find("Description") as RectTransform;

		if (headerText == null && headerRoot != null)
		{
			var headerTMP = headerRoot.GetComponentInChildren<TextMeshProUGUI>(true);
			if (headerTMP != null) headerText = headerTMP;
		}

		// Header 关闭按钮自动绑定
		if (closeButton == null && headerRoot != null)
		{
			var btn = headerRoot.Find("Button");
			if (btn != null) closeButton = btn.GetComponent<Button>();
		}
		if (closeButton != null)
		{
			closeButton.onClick.RemoveAllListeners();
			closeButton.onClick.AddListener(ClosePanel);
		}

		if (itemIconRect == null) itemIconRect = itemIconRoot;
		if (itemIconImage == null && itemIconRoot != null)
		{
			itemIconImage = itemIconRoot.GetComponent<Image>();
		}

		// LeftPattern
		if (descriptionRoot != null)
		{
			var leftPattern = descriptionRoot.Find("LeftPattern") as RectTransform;
			if (leftPattern != null)
			{
				var leftFixed = leftPattern.Find("Fixed") as RectTransform;
				var leftData = leftPattern.Find("Data") as RectTransform;
				if (leftFixed != null)
				{
					for (int i = 0; i < 3 && i < leftFixed.childCount; i++)
					{
						leftFixedTexts[i] = leftFixed.GetChild(i).GetComponent<TextMeshProUGUI>();
					}
				}
				if (leftData != null)
				{
					for (int i = 0; i < 3 && i < leftData.childCount; i++)
					{
						leftDataTexts[i] = leftData.GetChild(i).GetComponent<TextMeshProUGUI>();
					}
				}
			}

			// RightPattern
			var rightPattern = descriptionRoot.Find("RightPattern") as RectTransform;
			if (rightPattern != null)
			{
				rightFixedRoot = rightPattern.Find("Fixed") as RectTransform;
				rightDataRoot = rightPattern.Find("Data") as RectTransform;
				if (rightFixedRoot != null)
				{
					for (int i = 0; i < 3 && i < rightFixedRoot.childCount; i++)
					{
						rightFixedTexts[i] = rightFixedRoot.GetChild(i).GetComponent<TextMeshProUGUI>();
					}
				}
				if (rightDataRoot != null)
				{
					for (int i = 0; i < 3 && i < rightDataRoot.childCount; i++)
					{
						rightDataTexts[i] = rightDataRoot.GetChild(i).GetComponent<TextMeshProUGUI>();
					}
				}
			}

			// Price 文本自动查找
			if (priceText == null)
			{
				var priceNode = descriptionRoot.Find("Price");
				if (priceNode != null)
				{
					priceText = priceNode.GetComponent<TextMeshProUGUI>();
					if (priceText == null)
					{
						priceText = priceNode.GetComponentInChildren<TextMeshProUGUI>(true);
					}
				}
			}

			// Owner 文本自动查找
			if (ownerText == null)
			{
				var ownerNode = descriptionRoot.Find("Owner");
				if (ownerNode != null)
				{
					ownerText = ownerNode.GetComponent<TextMeshProUGUI>();
					if (ownerText == null)
					{
						ownerText = ownerNode.GetComponentInChildren<TextMeshProUGUI>(true);
					}
				}
			}
		}
	}

	public void ShowForItem(ItemDataReader reader)
	{
		if (reader == null || reader.ItemData == null)
		{
			Debug.LogWarning("[CheckInterfacePanelController] 无效的 ItemDataReader");
			gameObject.SetActive(false);
			return;
		}

		var data = reader.ItemData;

		// Header: 英文类别 + 英文全称
		if (headerText != null)
		{
			headerText.text = $"{data.category}  {data.itemName}";
		}

		// Icon: 限制在面板最大尺寸内，保持中心，按物品尺寸做适度缩放
		if (itemIconImage != null)
		{
			itemIconImage.sprite = data.itemIcon;
			itemIconImage.preserveAspect = true;
		}
		if (itemIconRect != null)
		{
			// 放大显示，但宽度不超过800，高度不超过500，保持中心与等比
			Vector2 target = new Vector2(800f, 500f);
			itemIconRect.anchorMin = new Vector2(0.5f, 0.5f);
			itemIconRect.anchorMax = new Vector2(0.5f, 0.5f);
			itemIconRect.pivot = new Vector2(0.5f, 0.5f);
			itemIconRect.anchoredPosition = new Vector2(0f, 100f);
			itemIconRect.sizeDelta = target; // 不超过最大，保持中心
		}

		// LeftPattern: 标签固定，数据填充（Category / Rarity / Size=W*H）
		SetTMP(leftFixedTexts, 0, "Category");
		SetTMP(leftFixedTexts, 1, "Rarity");
		SetTMP(leftFixedTexts, 2, "Size");
		SetTMP(leftDataTexts, 0, data.category.ToString());
		SetTMP(leftDataTexts, 1, GetRarityEnglish(data.rarity));
		SetTMP(leftDataTexts, 2, (data.width * data.height).ToString());

		// RightPattern: 根据类别展示/隐藏 3 行
		for (int i = 0; i < 3; i++)
		{
			SetActiveRightRow(i, false);
		}

		switch (data.category)
		{
			case ItemCategory.Food:
			case ItemCategory.Drink:
			{
				int row = 0;
				// 使用次数
				SetRightRow(row++, "Uses", reader.currentUsageCount > 0 ? reader.currentUsageCount.ToString() + " uses" : "0 uses");
				// 饱食度恢复
				if (reader.hungerRestore > 0)
				{
					SetRightRow(row++, "Hunger Restore", reader.hungerRestore + " hunger");
				}
				// 食物/饮水没有最大治疗量，跳过
				break;
			}

			case ItemCategory.Sedative:
			{
				int row = 0;
				SetRightRow(row++, "Uses", reader.currentUsageCount > 0 ? reader.currentUsageCount.ToString() + " uses" : "0 uses");
				if (reader.mentalRestore > 0)
				{
					SetRightRow(row++, "Mental Restore", reader.mentalRestore + " mental");
				}
				// 无最大治疗量
				break;
			}

			case ItemCategory.Healing:
			{
				int row = 0;
				// 治疗类通常不使用使用次数，若有则显示
				if (reader.maxUsageCount > 0)
				{
					SetRightRow(row++, "Uses", reader.currentUsageCount + " uses");
				}
				// 单次治疗量
				if (reader.healPerUse > 0)
				{
					SetRightRow(row++, "Heal Per Use", reader.healPerUse + " HP");
				}
				// 最大治疗量
				if (reader.maxHealAmount > 0)
				{
					SetRightRow(row++, "Max Heal", reader.maxHealAmount + " HP");
				}
				break;
			}

			case ItemCategory.Weapon:
			{
				int row = 0;
				if (data.weapon != null)
				{
					SetRightRow(row++, "Fire Rate", data.weapon.fireRate.ToString("0.##") + " s");
					SetRightRow(row++, "Damage", data.weapon.damage.ToString("0.#") + " dmg");
					SetRightRow(row++, "Magazine", data.weapon.magazineCapacity + " rnd");
				}
				break;
			}

			case ItemCategory.Backpack:
			case ItemCategory.TacticalRig:
			{
				int capacity = Mathf.Max(0, data.cellH * data.cellV);
				SetRightRow(0, "Capacity", capacity + " cells");
				break;
			}

			case ItemCategory.Intelligence:
			{
				if (data.intelligenceValue > 0)
				{
					SetRightRow(0, "Intelligence", data.intelligenceValue + " pts");
				}
				break;
			}

			default:
			{
				// 其他类别：若为可堆叠物品，显示堆叠数量
				if (data.IsStackable())
				{
					SetRightRow(0, "Max Stack", reader.maxStackAmount.ToString());
				}
				break;
			}
		}

		// 显示价格
		UpdatePriceUI(reader);

		// 显示玩家 Money 总额（每次打开都动态统计）
		UpdateOwnerMoneyUI();

		// Left/Right 标签与数据的可见性依据是否填充进行维护
		gameObject.SetActive(true);
	}

	public void ClosePanel()
	{
		if (destroyOnClose)
		{
			Destroy(gameObject);
		}
		else
		{
			gameObject.SetActive(false);
		}
	}

	public void SetDestroyOnClose(bool enable)
	{
		destroyOnClose = enable;
	}

#if UNITY_EDITOR
	/// <summary>
	/// 编辑模式下直接用 ScriptableObject 数据驱动显示
	/// </summary>
	public void ShowForItem(ItemDataSO data)
	{
		if (data == null)
		{
			Debug.LogWarning("[CheckInterfacePanelController] ItemDataSO 为空");
			return;
		}

		if (editorPreviewReader == null)
		{
			var go = new GameObject("__EditorPreviewItemReader");
			go.hideFlags = HideFlags.HideAndDontSave;
			editorPreviewReader = go.AddComponent<ItemDataReader>();
		}

		// 直接设置数据并刷新
		editorPreviewReader.SetItemData(data);
		ShowForItem(editorPreviewReader);
	}
#endif

	private void SetRightRow(int index, string label, string value)
	{
		SetActiveRightRow(index, true);
		SetTMP(rightFixedTexts, index, label);
		SetTMP(rightDataTexts, index, value);
	}

	private void SetActiveRightRow(int index, bool active)
	{
		if (rightFixedRoot != null && index >= 0 && index < rightFixedRoot.childCount)
			rightFixedRoot.GetChild(index).gameObject.SetActive(active);
		if (rightDataRoot != null && index >= 0 && index < rightDataRoot.childCount)
			rightDataRoot.GetChild(index).gameObject.SetActive(active);
	}

	private void SetTMP(TextMeshProUGUI[] arr, int index, string content)
	{
		if (arr == null || index < 0 || index >= arr.Length) return;
		if (arr[index] == null) return;
		arr[index].text = content ?? string.Empty;
	}

	private string GetRarityEnglish(string rarity)
	{
		switch (rarity)
		{
			case "1": return "Common";
			case "2": return "Rare";
			case "3": return "Epic";
			case "4": return "Legendary";
			default: return rarity;
		}
	}

	private void UpdatePriceUI(ItemDataReader reader)
	{
		if (!showPrice || priceText == null || reader == null || reader.ItemData == null)
		{
			if (priceText != null) priceText.gameObject.SetActive(false);
			return;
		}

		priceText.gameObject.SetActive(true);

		// 依据稀有度选择颜色与价格
		int rarityLevel = ParseRarityLevel(reader.ItemData.rarity);
		Color priceColor = GetRarityColor(rarityLevel);
		// 从物品数据对象读取真实售价（优先运行时reader.price，其次SO中的price）
		int price = reader.price > 0 ? reader.price : (reader.ItemData != null ? reader.ItemData.price : 0);
		if (price <= 0)
		{
			priceText.gameObject.SetActive(false);
			return;
		}

		// 组合富文本：白色标签 + 彩色数字
		string labelHex = ColorUtility.ToHtmlStringRGB(priceLabelColor);
		string priceHex = ColorUtility.ToHtmlStringRGB(priceColor);
		priceText.richText = true;
		priceText.text = $"<color=#{labelHex}>Price: </color><color=#{priceHex}>{price}</color>";
	}

	private int ParseRarityLevel(string rarity)
	{
		// 稀有度通常为 "1"-"4"，非数字时按1处理
		if (int.TryParse(rarity, out int lvl))
		{
			return Mathf.Clamp(lvl, 1, 4);
		}
		return 1;
	}

	private Color GetRarityColor(int level)
	{
		switch (level)
		{
			case 1: return rarity1Color;
			case 2: return rarity2Color;
			case 3: return rarity3Color;
			case 4: return rarity4Color;
			default: return rarity1Color;
		}
	}

	private int GetRarityPrice(int level)
	{
		switch (level)
		{
			case 1: return rarity1Price;
			case 2: return rarity2Price;
			case 3: return rarity3Price;
			case 4: return rarity4Price;
			default: return rarity1Price;
		}
	}

	private void UpdateOwnerMoneyUI()
	{
		if (!showOwnerMoney || ownerText == null)
		{
			if (ownerText != null) ownerText.gameObject.SetActive(false);
			return;
		}

		int totalMoney = ComputeTotalPlayerMoney();
		ownerText.gameObject.SetActive(true);
		ownerText.richText = true;
		string labelHex = ColorUtility.ToHtmlStringRGB(Color.white);
		string valueHex = ColorUtility.ToHtmlStringRGB(new Color(0.2f, 0.85f, 0.2f));
		ownerText.text = $"<color=#{labelHex}>{ownerLabel} </color><color=#{valueHex}>{totalMoney}</color>";
	}

	private int ComputeTotalPlayerMoney()
	{
		const int MONEY_ITEM_ID = 1301;
		int total = 0;
		var grids = FindObjectsOfType<ItemGrid>(true);
		if (grids == null || grids.Length == 0) return 0;
		for (int i = 0; i < grids.Length; i++)
		{
			var g = grids[i];
			if (g == null) continue;
			var type = g.GridType;
			// 玩家所属网格：Backpack / Equipment / Player
			if (!(type == GridType.Backpack || type == GridType.Equipment || type == GridType.Player))
				continue;
			for (int c = 0; c < g.transform.childCount; c++)
			{
				var child = g.transform.GetChild(c);
				if (child == null) continue;
				var rdr = child.GetComponent<ItemDataReader>();
				if (rdr == null || rdr.ItemData == null) continue;
				if (rdr.ItemData.id != MONEY_ITEM_ID) continue;
				int amount = rdr.CurrentStack > 0 ? rdr.CurrentStack : rdr.currencyAmount;
				if (amount > 0) total += amount;
			}
		}
		return total;
	}
}


