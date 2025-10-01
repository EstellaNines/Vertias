using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using InventorySystem;
using InventorySystem.SpawnSystem;

/// <summary>
/// 对话系统-商店物品生成器
/// - LS(LoganStryke): 使用固定物品生成（FixedItemSpawnConfig）
/// - AM(AsherMyles): 使用固定物品概率生成（FixedItemProbabilitySpawnConfig）
/// 每次打开对话时，按所选NPC清空并重新生成交易网格物品
/// </summary>
public class DialogueShopGenerator : MonoBehaviour
{
	[Header("引用")]
	[FieldLabel("交易网格（可选）")]
	[SerializeField] private ItemGrid tradeItemGrid;

	[Header("生成配置")]
	[FieldLabel("LS固定物品配置 (Logan Stryke)")]
	[SerializeField] private FixedItemSpawnConfig loganFixedConfig;
	[FieldLabel("AM概率生成配置 (Asher Myles)")]
	[SerializeField] private FixedItemProbabilitySpawnConfig asherProbabilityConfig;

	[Header("NPC Id 映射")]
	[SerializeField] private string loganNpcId = "LoganStryke";
	[SerializeField] private string asherNpcId = "AsherMyles";

	[Header("调试")]
	[SerializeField] private bool showDebugLog = true;

	// 记录最近一次生成所用的NPC，便于只在AM已生成时读取其价格
	[SerializeField] private string lastGeneratedNpcId = null;
	[SerializeField] private bool autoLogAsherPricesOnGenerate = true; // 为AM生成后自动打印单价
	[SerializeField] private float priceLogTimeoutSeconds = 3f; // 最长等待生成完成的时间
	[SerializeField] private float priceSettleSeconds = 0.3f;   // 物品数量稳定的判定时间

	private Coroutine priceLogRoutine;

	// 折扣控制（AM奖励）
	[SerializeField] private bool asherDiscountActive = false;
	[SerializeField] private float asherDiscountMultiplier = 1f; // 0.8 表示打八折
	private readonly Dictionary<int, int> originalPriceByInstanceId = new Dictionary<int, int>();

	/// <summary>
	/// 对外入口：根据NPC生成对应的售卖物品
	/// </summary>
	public void GenerateForNpc(string npcId)
	{
		var grid = GetTradeGrid();
		if (grid == null)
		{
			LogDebug("未找到交易网格，跳过生成");
			return;
		}

		lastGeneratedNpcId = npcId;

		// 清空现有物品
		SafeClearGrid(grid);

		string containerId = GetContainerId(grid, npcId);

		if (string.Equals(npcId, loganNpcId, System.StringComparison.Ordinal))
		{
			if (loganFixedConfig == null)
			{
				LogDebug("LS固定物品配置未设置");
				return;
			}
			FixedItemSpawnManager.Instance.SpawnFixedItems(grid, loganFixedConfig, containerId);
			LogDebug($"为 {npcId} 使用固定物品配置生成");
			return;
		}

		if (string.Equals(npcId, asherNpcId, System.StringComparison.Ordinal))
		{
			if (asherProbabilityConfig == null)
			{
				LogDebug("AM概率配置未设置");
				return;
			}
			FixedItemProbabilitySpawnManager.Instance.SpawnWithProbability(grid, asherProbabilityConfig, containerId);
			LogDebug($"为 {npcId} 使用概率配置生成");
			if (autoLogAsherPricesOnGenerate || asherDiscountActive)
			{
				StartDeferredAsherPriceLogging();
			}
			return;
		}

		LogDebug($"未匹配的NPC: {npcId}，未生成");
	}

	/// <summary>
	/// 读取 AM(AsherMyles) 的交易网格中所有物品的单价（不乘以堆叠）。
	/// 仅当最近一次生成的 NPC 为 AM 时返回结果，否则返回空列表。
	/// </summary>
	public List<int> GetAsherMylesItemUnitPrices()
	{
		var result = new List<int>();
		if (!string.Equals(lastGeneratedNpcId, asherNpcId, System.StringComparison.Ordinal))
		{
			LogDebug("当前交易网格并非 AM 内容，返回空结果");
			return result;
		}

		var grid = GetTradeGrid();
		if (grid == null) return result;

		var items = grid.GetComponentsInChildren<Item>(true);
		for (int i = 0; i < items.Length; i++)
		{
			var item = items[i];
			if (item == null || item.ItemDataReader == null) continue;
			int unitPrice = GetUnitPrice(item.ItemDataReader);
			if (unitPrice > 0) result.Add(unitPrice);
		}
		return result;
	}

	private int GetUnitPrice(ItemDataReader reader)
	{
		if (reader == null) return 0;
		if (reader.price > 0) return reader.price;
		if (reader.ItemData != null && reader.ItemData.price > 0) return reader.ItemData.price;
		return 0;
	}

	private void StartDeferredAsherPriceLogging()
	{
		if (priceLogRoutine != null)
		{
			StopCoroutine(priceLogRoutine);
		}
		priceLogRoutine = StartCoroutine(WaitAndLogAsherPrices());
	}

	private IEnumerator WaitAndLogAsherPrices()
	{
		var grid = GetTradeGrid();
		float elapsed = 0f;
		int lastCount = -1;
		float stableTimer = 0f;
		while (elapsed < priceLogTimeoutSeconds)
		{
			if (grid != null)
			{
				var items = grid.GetComponentsInChildren<Item>(true);
				int count = items != null ? items.Length : 0;
				if (count > 0)
				{
					if (count == lastCount)
					{
						stableTimer += Time.unscaledDeltaTime;
						if (stableTimer >= priceSettleSeconds)
						{
							break; // 数量在一段时间内稳定，认为生成完成
						}
					}
					else
					{
						lastCount = count;
						stableTimer = 0f; // 数量变化，重置稳定计时
					}
				}
			}
			elapsed += Time.unscaledDeltaTime;
			yield return null; // 下一帧再检查
		}
		// 若已激活AM折扣，先应用折扣再打印
		if (asherDiscountActive && asherDiscountMultiplier > 0f && asherDiscountMultiplier < 1.001f)
		{
			ApplyDiscountToCurrentAsherItems();
		}
		LogAsherMylesItemUnitPrices();
		priceLogRoutine = null;
	}

	/// <summary>
	/// 激活 AM 折扣（例如 20 -> 0.8）。会立即尝试对当前交易网格应用折扣，并在后续生成时自动生效。
	/// </summary>
	public void ActivateAsherDiscount(int percent)
	{
		float p = Mathf.Clamp(percent, 0, 100) / 100f;
		asherDiscountMultiplier = Mathf.Clamp01(1f - p);
		asherDiscountActive = asherDiscountMultiplier < 0.999f;
		if (!asherDiscountActive) return;
		ApplyDiscountToCurrentAsherItems();
	}

	/// <summary>
	/// 对当前 AM 交易网格中所有物品应用折扣（仅修改运行时 reader.price，不改 SO）。
	/// 多次调用不会叠加折扣：对同一实例使用记录的 originalPrice 作为基准。
	/// </summary>
	private void ApplyDiscountToCurrentAsherItems()
	{
		if (!string.Equals(lastGeneratedNpcId, asherNpcId, System.StringComparison.Ordinal)) return;
		var grid = GetTradeGrid();
		if (grid == null) return;
		var items = grid.GetComponentsInChildren<Item>(true);
		for (int i = 0; i < items.Length; i++)
		{
			var item = items[i];
			if (item == null || item.ItemDataReader == null) continue;
			var rdr = item.ItemDataReader;
			int key = rdr.GetInstanceID();
			int basePrice;
			if (!originalPriceByInstanceId.TryGetValue(key, out basePrice))
			{
				basePrice = GetUnitPrice(rdr);
				if (basePrice <= 0) continue;
				originalPriceByInstanceId[key] = basePrice;
			}
			int discounted = Mathf.Max(1, Mathf.RoundToInt(basePrice * asherDiscountMultiplier));
			rdr.price = discounted;
			// 若有UI显示价格，可尝试刷新
			try { rdr.UpdateUI(); } catch {}
		}
		LogDebug($"已对 AM 交易物品应用折扣 x{asherDiscountMultiplier:0.##}");
	}

	/// <summary>
	/// 将 AM(AsherMyles) 交易网格中每一个物品的单价逐条打印到控制台。
	/// 若最近一次生成的 NPC 不是 AM，则给出提示并跳过。
	/// </summary>
	public void LogAsherMylesItemUnitPrices()
	{
		if (!string.Equals(lastGeneratedNpcId, asherNpcId, System.StringComparison.Ordinal))
		{
			LogDebug("当前交易网格并非 AM 内容，跳过价格打印");
			return;
		}

		var grid = GetTradeGrid();
		if (grid == null)
		{
			LogDebug("未找到交易网格，无法打印价格");
			return;
		}

		var items = grid.GetComponentsInChildren<Item>(true);
		for (int i = 0; i < items.Length; i++)
		{
			var item = items[i];
			if (item == null) continue;
			var rdr = item.ItemDataReader;
			string itemName = (rdr != null && rdr.ItemData != null && !string.IsNullOrEmpty(rdr.ItemData.itemName)) ? rdr.ItemData.itemName : item.name;
			int unitPrice = GetUnitPrice(rdr);
			Debug.Log($"[DialogueShopGenerator] AM售卖单价 - {itemName}: {unitPrice}");
		}
	}

	private ItemGrid GetTradeGrid()
	{
		if (tradeItemGrid != null) return tradeItemGrid;
		// 尝试在自身层级内按名称/类型查找
		var grids = GetComponentsInChildren<ItemGrid>(true);
		ItemGrid byName = null;
		for (int i = 0; i < grids.Length; i++)
		{
			if (grids[i] == null) continue;
			var n = grids[i].name;
			if (!string.IsNullOrEmpty(n) && n.IndexOf("TradeItemGrid", System.StringComparison.OrdinalIgnoreCase) >= 0)
			{
				byName = grids[i];
				break;
			}
		}
		tradeItemGrid = byName != null ? byName : (grids.Length > 0 ? grids[0] : null);
		return tradeItemGrid;
	}

	private void SafeClearGrid(ItemGrid grid)
	{
		if (grid == null) return;
		var items = grid.GetComponentsInChildren<Item>(true);
		for (int i = 0; i < items.Length; i++)
		{
			if (items[i] != null && items[i].gameObject != null)
			{
				Destroy(items[i].gameObject);
			}
		}
	}

	private string GetContainerId(ItemGrid grid, string npcId)
	{
		string id = (grid != null && !string.IsNullOrEmpty(grid.GridGUID)) ? grid.GridGUID : (grid != null ? grid.name : "TradeGrid");
		return $"{id}_{npcId}";
	}

	private void LogDebug(string msg)
	{
		if (showDebugLog)
		{
			Debug.Log($"[DialogueShopGenerator] {msg}");
		}
	}
}


