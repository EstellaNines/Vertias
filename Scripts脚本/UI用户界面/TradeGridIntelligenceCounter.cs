using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using InventorySystem;
using InventorySystem.SpawnSystem;

namespace Game.UI
{
	/// <summary>
	/// 统计玩家放入 TradeGrid 中的情报物品总情报值，并显示为 当前/要求。
	/// - 要求值来自 Resources/MissionData.json 的 requirements.intelligence（通过 selectedMissionId 选择）
	/// - 当当前值 >= 要求值时，TMP 文本显示为绿色，否则为默认颜色
	/// 将本脚本挂到 MissionReceiveSystem 中合适的对象上，并在 Inspector 里接线。
	/// </summary>
	[DisallowMultipleComponent]
	[ExecuteAlways]
	public sealed class TradeGridIntelligenceCounter : MonoBehaviour
	{
		[Header("引用 - 交易网格 (TradeGrid)")]
		[SerializeField] private ItemGrid tradeGrid;

		[Header("引用 - 显示文本的 TMP (RawImage 下的 Text (TMP))")]
		[SerializeField] private TextMeshProUGUI progressTMP;

		[Header("引用 - Email 管理器 (同任务ID)")]
		[SerializeField] private EmailMissionUIManager emailManager;

		[Header("随机奖励配置 (完成任务后生成)")]
		[SerializeField] private RandomItemSpawnConfig rewardRandomConfig;
		[SerializeField] private bool spawnRewardOnSuccess = true;
		[SerializeField] private bool enforceCurrencyStack = true;
		[SerializeField] private int currencyStackAmount = 1000;

		[Header("提交与领取 UI")]
		[SerializeField] private Button submitButton; // Handout 按钮
		[SerializeField] private Button claimButton;  // Claim 按钮
		[SerializeField] private ItemGrid warehouseGrid; // 仓库网格
		[SerializeField] private bool hasSubmitted = false; // 本任务是否已提交成功

		[Header("任务选择")]
		[SerializeField] private int selectedMissionId = 1;

		[Header("显示样式")]
		[SerializeField] private Color meetRequirementColor = Color.green;
		[SerializeField] private Color defaultColor = Color.white;

		private MissionDataRoot cachedData;

		#region JSON 数据结构
		[Serializable]
		private sealed class MissionDataRoot
		{
			public List<Mission> missions;
		}

		[Serializable]
		private sealed class Mission
		{
			public int id;
			public string name;
			public string description;
			public Requirements requirements;
		}

		[Serializable]
		private sealed class Requirements
		{
			public int intelligence;
			public int itemCount;
		}
		#endregion

		private void Reset()
		{
			// 尝试在层级中自动寻找 TradeGrid（可根据项目命名微调）
			if (tradeGrid == null)
			{
				var t = transform.parent != null ? transform.parent : transform;
				var guess = t.GetComponentInChildren<ItemGrid>(true);
				if (guess != null) tradeGrid = guess;
			}
		}

		private void OnEnable()
		{
			LoadMissionDataIfNeeded();
			AutoFindWarehouseGrid();
			SubscribeGridEvents(true);
			UpdateDisplay();
			UpdateButtonsState();
		}

		private void OnDisable()
		{
			SubscribeGridEvents(false);
		}

		private void OnValidate()
		{
			#if UNITY_EDITOR
			LoadMissionDataIfNeeded(forceReload: true);
			AutoFindWarehouseGrid();
			UpdateDisplay();
			UpdateButtonsState();
			#endif
		}

		public void SetSelectedMissionId(int missionId)
		{
			selectedMissionId = missionId;
			UpdateDisplay();
		}

		/// <summary>
		/// 由 Handout 按钮调用：若当前情报值达标则标记 Email 任务完成，返回是否成功。
		/// </summary>
		public bool TrySubmit()
		{
			int required = GetRequiredIntelligence();
			int current = ComputeCurrentIntelligenceSum();
			bool ok = current >= required && required > 0;
			if (ok && emailManager != null)
			{
				emailManager.MarkCompleted();
				emailManager.ShowCompletionResult(true);
			}
			else if (emailManager != null)
			{
				emailManager.ShowCompletionResult(false);
			}
			return ok;
		}

		/// <summary>
		/// 供 Unity 按钮 OnClick 绑定的无返回值方法。
		/// </summary>
		public void Submit()
		{
			// 已提交则直接返回，防重复
			if (hasSubmitted) return;
			bool success = TrySubmit();
			ConsumeAllItemsInTradeGrid();
			if (success && spawnRewardOnSuccess)
			{
				SpawnRewardItems();
				hasSubmitted = true;
			}
			UpdateButtonsState();
		}

		private void ConsumeAllItemsInTradeGrid()
		{
			if (tradeGrid == null || !tradeGrid.IsGridInitialized) return;
			List<Item> items = tradeGrid.GetItemsInArea(0, 0, tradeGrid.CurrentWidth, tradeGrid.CurrentHeight);
			tradeGrid.ClearGrid();
			for (int i = 0; i < items.Count; i++)
			{
				var it = items[i];
				if (it != null)
				{
					Destroy(it.gameObject);
				}
			}
		}


		private void SpawnRewardItems()
		{
			if (tradeGrid == null || rewardRandomConfig == null) return;
			// 使用随机生成管理器：强制在 tradeGrid 上以随机配置生成
			ShelfRandomItemManager.Instance.ForceRegenerateItems(tradeGrid.gameObject, tradeGrid, rewardRandomConfig);
			if (enforceCurrencyStack)
			{
				StartCoroutine(AdjustCurrencyStacksAfterSpawn());
			}
		}

		private System.Collections.IEnumerator AdjustCurrencyStacksAfterSpawn()
		{
			// 使用真实时间等待，避免 Time.timeScale == 0 时协程停滞
			float wait = 0f;
			while (tradeGrid != null && wait < 1.5f)
			{
				var itemsProbe = tradeGrid.GetItemsInArea(0, 0, tradeGrid.CurrentWidth, tradeGrid.CurrentHeight);
				if (itemsProbe != null && itemsProbe.Count > 0) break;
				yield return new WaitForSecondsRealtime(0.1f);
				wait += 0.1f;
			}

			if (tradeGrid == null) yield break;
			// 第一次修正（生成完成立即）
			yield return StartCoroutine(AdjustCurrencyStacksPass());
			// 等待 FixedItemSpawnManager 的延迟校验(≈0.2s)结束后再修正一次，防止被回写
			yield return new WaitForSecondsRealtime(0.35f);
			yield return StartCoroutine(AdjustCurrencyStacksPass());
		}

		private System.Collections.IEnumerator AdjustCurrencyStacksPass()
		{
			if (tradeGrid == null || !tradeGrid.IsGridInitialized) yield break;
			List<Item> items = tradeGrid.GetItemsInArea(0, 0, tradeGrid.CurrentWidth, tradeGrid.CurrentHeight);
			for (int i = 0; i < items.Count; i++)
			{
				var reader = items[i]?.ItemDataReader;
				var data = reader?.ItemData;
				if (reader == null || data == null) continue;
				if (data.category != ItemCategory.Currency) continue;

				int target = Mathf.Clamp(currencyStackAmount, 1, data.maxStack);
				for (int attempt = 0; attempt < 2; attempt++)
				{
					reader.SetStack(target);
					reader.currencyAmount = target;
					yield return null; // 让 UI 刷新
					if (reader.CurrentStack == target) break;
				}
			}
		}

		/// <summary>
		/// 领取奖励：将 TradeGrid 中奖励迁移到仓库网格。
		/// </summary>
		public void Claim()
		{
			if (!hasSubmitted) return;
			if (warehouseGrid == null) AutoFindWarehouseGrid();
			if (tradeGrid == null || warehouseGrid == null || !tradeGrid.IsGridInitialized || !warehouseGrid.IsGridInitialized) return;
			List<Item> items = tradeGrid.GetItemsInArea(0, 0, tradeGrid.CurrentWidth, tradeGrid.CurrentHeight);
			for (int i = 0; i < items.Count; i++)
			{
				var item = items[i];
				if (item == null) continue;
				if (TryFindPlacement(warehouseGrid, item, out var pos))
				{
					// 从交易网格取出并放入仓库
					var origin = item.OnGridPosition;
					tradeGrid.PickUpItem(origin.x, origin.y);
					warehouseGrid.PlaceItem(item, pos.x, pos.y);
				}
			}
			UpdateButtonsState();
		}

		private bool TryFindPlacement(ItemGrid targetGrid, Item item, out Vector2Int placement)
		{
			placement = default;
			if (targetGrid == null || item == null) return false;
			int maxX = Mathf.Max(0, targetGrid.CurrentWidth - item.GetWidth());
			int maxY = Mathf.Max(0, targetGrid.CurrentHeight - item.GetHeight());
			for (int x = 0; x <= maxX; x++)
			{
				for (int y = 0; y <= maxY; y++)
				{
					if (!targetGrid.HasItemConflict(x, y, item.GetWidth(), item.GetHeight()))
					{
						placement = new Vector2Int(x, y);
						return true;
					}
				}
			}
			return false;
		}

		private void UpdateButtonsState()
		{
			if (warehouseGrid == null) AutoFindWarehouseGrid();
			if (submitButton != null) submitButton.interactable = !hasSubmitted;
			if (claimButton != null) claimButton.interactable = hasSubmitted && warehouseGrid != null;
		}

		private void AutoFindWarehouseGrid()
		{
			if (warehouseGrid != null && warehouseGrid.IsGridInitialized) return;
			Transform root = transform.root != null ? transform.root : transform;
			var grids = root.GetComponentsInChildren<ItemGrid>(true);
			ItemGrid exact = null;
			ItemGrid byType = null;
			ItemGrid byNameWarehouseItemGrid = null;
			ItemGrid byNameWarehouse = null;
			ItemGrid byNameStorage = null;
			ItemGrid byNameCN = null;
			for (int i = 0; i < grids.Length; i++)
			{
				var g = grids[i];
				if (g == null) continue;
				string n = g.name;
				if (string.Equals(n, "WarehouseItemGrid", System.StringComparison.OrdinalIgnoreCase)) { exact = g; break; }
				if (byType == null && g.GridType == GridType.Storage) byType = g;
				if (byNameWarehouseItemGrid == null && n.IndexOf("WarehouseItemGrid", System.StringComparison.OrdinalIgnoreCase) >= 0) byNameWarehouseItemGrid = g;
				if (byNameWarehouse == null && n.IndexOf("Warehouse", System.StringComparison.OrdinalIgnoreCase) >= 0) byNameWarehouse = g;
				if (byNameStorage == null && n.IndexOf("Storage", System.StringComparison.OrdinalIgnoreCase) >= 0) byNameStorage = g;
				if (byNameCN == null && n.IndexOf("仓库", System.StringComparison.OrdinalIgnoreCase) >= 0) byNameCN = g;
			}
			warehouseGrid = exact ?? byType ?? byNameWarehouseItemGrid ?? byNameWarehouse ?? byNameStorage ?? byNameCN ?? warehouseGrid;
		}

		private void SubscribeGridEvents(bool subscribe)
		{
			if (tradeGrid == null) return;
			if (subscribe)
			{
				tradeGrid.OnItemPlaced += OnGridChanged;
				tradeGrid.OnItemRemoved += OnGridChanged;
				tradeGrid.OnItemMoved += OnGridMoved;
				tradeGrid.OnGridCleared += OnGridCleared;
			}
			else
			{
				tradeGrid.OnItemPlaced -= OnGridChanged;
				tradeGrid.OnItemRemoved -= OnGridChanged;
				tradeGrid.OnItemMoved -= OnGridMoved;
				tradeGrid.OnGridCleared -= OnGridCleared;
			}
		}

		private void OnGridChanged(Item item, Vector2Int _)
		{
			UpdateDisplay();
		}

		private void OnGridMoved(Item item, Vector2Int __, Vector2Int ___)
		{
			UpdateDisplay();
		}

		private void OnGridCleared(ItemGrid _)
		{
			UpdateDisplay();
		}

		private void LoadMissionDataIfNeeded(bool forceReload = false)
		{
			if (!forceReload && cachedData != null) return;
			TextAsset json = Resources.Load<TextAsset>("MissionData");
			if (json == null)
			{
				cachedData = new MissionDataRoot { missions = new List<Mission>() };
				return;
			}
			try
			{
				cachedData = JsonUtility.FromJson<MissionDataRoot>(json.text);
				if (cachedData == null || cachedData.missions == null)
				{
					cachedData = new MissionDataRoot { missions = new List<Mission>() };
				}
			}
			catch
			{
				cachedData = new MissionDataRoot { missions = new List<Mission>() };
			}
		}

		private int GetRequiredIntelligence()
		{
			if (cachedData == null || cachedData.missions == null) return 0;
			var m = cachedData.missions.Find(x => x.id == selectedMissionId);
			if (m == null || m.requirements == null) return 0;
			return Mathf.Max(0, m.requirements.intelligence);
		}

		private int ComputeCurrentIntelligenceSum()
		{
			if (tradeGrid == null || !tradeGrid.IsGridInitialized) return 0;
			List<Item> items = tradeGrid.GetItemsInArea(0, 0, tradeGrid.CurrentWidth, tradeGrid.CurrentHeight);
			int sum = 0;
			for (int i = 0; i < items.Count; i++)
			{
				var reader = items[i]?.ItemDataReader;
				var data = reader?.ItemData;
				if (data == null) continue;
				if (data.category == ItemCategory.Intelligence)
				{
					int value = Mathf.Max(0, data.intelligenceValue);
					// 若此类物品可堆叠，则乘以当前堆叠数
					int stack = Mathf.Max(1, reader.CurrentStack);
					sum += value * stack;
				}
			}
			return sum;
		}

		private void UpdateDisplay()
		{
			int required = GetRequiredIntelligence();
			int current = ComputeCurrentIntelligenceSum();

			if (progressTMP != null)
			{
				progressTMP.text = $"{current}/{required}";
				progressTMP.color = current >= required ? meetRequirementColor : defaultColor;
			}
		}
	}
}


