using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// 物品价格提示组件 - 鼠标悬停指定时间后显示物品价格
/// 通过订阅其他组件的事件或直接被调用来工作，避免与DraggableItem的事件接口冲突
/// </summary>
public class ItemPriceTooltip : MonoBehaviour
{
	[Header("价格提示设置")]
	[FieldLabel("价格提示预制体")]
	[SerializeField] private GameObject priceTooltipPrefab; // LootValve 预制体
	[FieldLabel("悬停触发时间(秒)")]
	[SerializeField] private float hoverDelay = 1f;
	[FieldLabel("提示显示时长(秒)")]
	[SerializeField] private float displayDuration = 0f; // 0 表示持续显示直到鼠标离开
	[FieldLabel("相对物品偏移")]
	[SerializeField] private Vector2 tooltipOffset = new Vector2(0, 2f); // 相对于物品中心的偏移

	[Header("稀有度颜色设置")]
	[FieldLabel("普通(绿色)")]
	[SerializeField] private Color normalColor = new Color(0f, 1f, 0f, 1f); // 绿色
	[FieldLabel("稀有(黄色)")]
	[SerializeField] private Color rareColor = new Color(1f, 1f, 0f, 1f); // 黄色
	[FieldLabel("史诗(橙色)")]
	[SerializeField] private Color epicColor = new Color(1f, 0.5f, 0f, 1f); // 橙色
	[FieldLabel("传说(红色)")]
	[SerializeField] private Color legendaryColor = new Color(1f, 0f, 0f, 1f); // 红色

	[Header("自动查找预制体")]
	[FieldLabel("从Resources加载")]
	[SerializeField] private bool loadFromResources = true;
	[FieldLabel("预制体资源路径")]
	[SerializeField] private string resourcePath = "LootValve";

	private ItemDataReader itemDataReader;
	private RectTransform rectTransform;
	private Canvas parentCanvas;
	private Coroutine hoverCoroutine;
	private GameObject activeTooltip;
	private bool isPointerOver = false;

	private void Awake()
	{
		itemDataReader = GetComponent<ItemDataReader>();
		rectTransform = GetComponent<RectTransform>();
		parentCanvas = GetComponentInParent<Canvas>();

		// 从 Resources 加载预制体
		if (loadFromResources && priceTooltipPrefab == null && !string.IsNullOrEmpty(resourcePath))
		{
			priceTooltipPrefab = Resources.Load<GameObject>(resourcePath);
			if (priceTooltipPrefab == null)
			{
				Debug.LogWarning($"[ItemPriceTooltip] 未能从 Resources 加载预制体: {resourcePath}");
			}
		}
	}

	/// <summary>
	/// 外部调用：鼠标进入物品区域
	/// 由 DraggableItem 或其他组件调用
	/// </summary>
	public void OnItemPointerEnter()
	{
		if (!enabled || itemDataReader == null) return;

		// 检查是否有价格（Special 和 Intelligence 类不显示）
		if (itemDataReader.ItemData == null) return;
		var category = itemDataReader.ItemData.category;
		if (category == InventorySystem.ItemCategory.Special || 
		    category == InventorySystem.ItemCategory.Intelligence)
		{
			return;
		}

		int price = itemDataReader.price;
		if (price <= 0) return;

		isPointerOver = true;
		// 启动延迟计时器
		if (hoverCoroutine != null)
		{
			StopCoroutine(hoverCoroutine);
		}
		hoverCoroutine = StartCoroutine(HoverDelayRoutine(price));
	}

	/// <summary>
	/// 外部调用：鼠标离开物品区域
	/// 由 DraggableItem 或其他组件调用
	/// </summary>
	public void OnItemPointerExit()
	{
		isPointerOver = false;
		// 停止计时器
		if (hoverCoroutine != null)
		{
			StopCoroutine(hoverCoroutine);
			hoverCoroutine = null;
		}
		// 隐藏提示
		HideTooltip();
	}

	private IEnumerator HoverDelayRoutine(int price)
	{
		// 等待指定时间
		yield return new WaitForSeconds(hoverDelay);

		// 检查鼠标是否仍在物品上
		if (!isPointerOver)
		{
			hoverCoroutine = null;
			yield break;
		}

		// 显示价格提示
		ShowTooltip(price);

		// 如果设置了显示时长，等待后自动隐藏
		if (displayDuration > 0f)
		{
			yield return new WaitForSeconds(displayDuration);
			HideTooltip();
		}

		hoverCoroutine = null;
	}

	private void ShowTooltip(int price)
	{
		if (priceTooltipPrefab == null)
		{
			Debug.LogWarning("[ItemPriceTooltip] 价格提示预制体未设置。");
			return;
		}

		// 如果已有激活的提示，先销毁
		HideTooltip();

		// 确定父容器（优先使用 Canvas，否则使用当前物品的父对象）
		Transform parent = parentCanvas != null ? parentCanvas.transform : transform.parent;
		if (parent == null) parent = transform;

		// 实例化提示预制体
		activeTooltip = Instantiate(priceTooltipPrefab, parent);
		var tooltipRect = activeTooltip.GetComponent<RectTransform>();

		if (tooltipRect != null && rectTransform != null)
		{
			// 设置为世界空间位置（物品中心）
			Vector3 worldPos = rectTransform.TransformPoint(rectTransform.rect.center);
			
			// 转换为 Canvas 空间
			if (parentCanvas != null)
			{
				RectTransformUtility.ScreenPointToLocalPointInRectangle(
					parentCanvas.transform as RectTransform,
					RectTransformUtility.WorldToScreenPoint(parentCanvas.worldCamera, worldPos),
					parentCanvas.worldCamera,
					out Vector2 localPos
				);
				tooltipRect.anchoredPosition = localPos + tooltipOffset;
			}
			else
			{
				// 后备：直接在物品局部空间中放置
				tooltipRect.SetParent(transform, false);
				tooltipRect.anchoredPosition = tooltipOffset;
			}
		}

		// 设置价格文本和颜色
		var tmp = activeTooltip.GetComponentInChildren<TextMeshProUGUI>(true);
		if (tmp != null)
		{
			tmp.text = price.ToString();
			
			// 根据物品稀有度设置颜色
			if (itemDataReader != null && itemDataReader.ItemData != null)
			{
				tmp.color = GetRarityColorFromString(itemDataReader.ItemData.rarity);
			}
		}

		// 确保提示在最上层渲染
		activeTooltip.transform.SetAsLastSibling();
	}

	private void HideTooltip()
	{
		if (activeTooltip != null)
		{
			Destroy(activeTooltip);
			activeTooltip = null;
		}
	}

	private void OnDisable()
	{
		// 组件禁用时清理
		if (hoverCoroutine != null)
		{
			StopCoroutine(hoverCoroutine);
			hoverCoroutine = null;
		}
		HideTooltip();
		isPointerOver = false;
	}

	private void OnDestroy()
	{
		HideTooltip();
	}

	/// <summary>
	/// 根据稀有度字符串获取对应的颜色
	/// ItemDataSO中的rarity为字符串类型："1"=普通, "2"=稀有, "3"=史诗, "4"=传说
	/// </summary>
	private Color GetRarityColorFromString(string rarity)
	{
		switch (rarity)
		{
			case "1": // 普通 (Common)
				return normalColor;
			case "2": // 稀有 (Rare)
				return rareColor;
			case "3": // 史诗 (Epic)
				return epicColor;
			case "4": // 传说 (Legendary)
				return legendaryColor;
			default:
				return Color.white;
		}
	}
}
