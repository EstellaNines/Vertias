using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using InventorySystem;

/// <summary>
/// 检测玩家装备与容器内容：
/// - 背包装备
/// - 挂具（战术背心）装备
/// - 背包容器网格内的物品
/// - 挂具容器网格内的物品
/// - 口袋网格（PlayerItemGrid）内的物品
/// 可在运行时或编辑器中通过上下文菜单打印检测结果。
/// </summary>
public class BackpackEquipmentInspector : MonoBehaviour
{
	[Header("调试选项")]
	[SerializeField] private bool logOnStart = false;

	[System.Serializable]
	public class EquipmentDetectionResult
	{
		[FieldLabel("背包装备物品名")] public string backpackEquippedItemName;
		[FieldLabel("挂具装备物品名")] public string tacticalRigEquippedItemName;
		[FieldLabel("背包容器物品")] public List<string> backpackContainerItems = new List<string>();
		[FieldLabel("挂具容器物品")] public List<string> tacticalRigContainerItems = new List<string>();
		[FieldLabel("口袋网格物品")] public List<string> playerPocketItems = new List<string>();
	}

	private void Start()
	{
		if (logOnStart)
		{
			DetectAndPrint();
		}
	}

	/// <summary>
	/// 立即执行检测并返回检测结果
	/// </summary>
	public EquipmentDetectionResult DetectNow()
	{
		var result = new EquipmentDetectionResult();

		var slotManager = EquipmentSlotManager.Instance;
		if (slotManager == null)
		{
			Debug.LogWarning("BackpackEquipmentInspector: 未找到 EquipmentSlotManager 实例。");
			return result;
		}

		// 背包与挂具（战术背心）装备
		var backpackSlot = slotManager.GetEquipmentSlot(EquipmentSlotType.Backpack);
		var rigSlot = slotManager.GetEquipmentSlot(EquipmentSlotType.TacticalRig);

		result.backpackEquippedItemName = GetEquippedItemDisplayName(backpackSlot);
		result.tacticalRigEquippedItemName = GetEquippedItemDisplayName(rigSlot);

		// 背包容器网格
		if (backpackSlot != null && backpackSlot.ContainerGrid != null)
		{
			result.backpackContainerItems = GetGridItemDisplayNames(backpackSlot.ContainerGrid);
		}

		// 挂具容器网格
		if (rigSlot != null && rigSlot.ContainerGrid != null)
		{
			result.tacticalRigContainerItems = GetGridItemDisplayNames(rigSlot.ContainerGrid);
		}

		// 口袋网格（PlayerItemGrid）
		var pocketGrid = FindPlayerPocketGrid();
		if (pocketGrid != null)
		{
			result.playerPocketItems = GetGridItemDisplayNames(pocketGrid);
		}

		return result;
	}

	/// <summary>
	/// 在控制台打印检测结果
	/// </summary>
	[ContextMenu("检测并打印装备信息")]
	public void DetectAndPrint()
	{
		var result = DetectNow();
		Debug.Log("===== 玩家装备检测结果 =====");
		Debug.Log($"背包装备: {FormatEmpty(result.backpackEquippedItemName)}");
		Debug.Log($"挂具装备: {FormatEmpty(result.tacticalRigEquippedItemName)}");
		Debug.Log($"背包容器物品: {FormatList(result.backpackContainerItems)}");
		Debug.Log($"挂具容器物品: {FormatList(result.tacticalRigContainerItems)}");
		Debug.Log($"口袋网格物品: {FormatList(result.playerPocketItems)}");
	}

	private string GetEquippedItemDisplayName(EquipmentSlot slot)
	{
		if (slot == null || !slot.HasEquippedItem || slot.CurrentEquippedItem == null || slot.CurrentEquippedItem.ItemData == null)
		{
			return string.Empty;
		}
		var data = slot.CurrentEquippedItem.ItemData;
		return $"{data.itemName} (ID {data.GlobalId})";
	}

	private List<string> GetGridItemDisplayNames(ItemGrid grid)
	{
		var names = new List<string>();
		if (grid == null) return names;

		// 遍历网格下的所有物品读取器
		var readers = grid.GetComponentsInChildren<ItemDataReader>(true);
		foreach (var reader in readers)
		{
			if (reader != null && reader.ItemData != null)
			{
				names.Add($"{reader.ItemData.itemName} (ID {reader.ItemData.GlobalId})");
			}
		}
		return names.OrderBy(n => n).ToList();
	}

	private ItemGrid FindPlayerPocketGrid()
	{
		// 优先根据类型查找
		var allGrids = FindObjectsOfType<ItemGrid>(true);
		var gridByType = allGrids.FirstOrDefault(g => g != null && g.GridType == GridType.Player);
		if (gridByType != null) return gridByType;

		// 回退：按命名约定查找
		var go = GameObject.Find("PlayerItemGrid");
		if (go != null)
		{
			var grid = go.GetComponent<ItemGrid>();
			if (grid != null) return grid;
		}
		return null;
	}

	private string FormatEmpty(string value)
	{
		return string.IsNullOrEmpty(value) ? "(空)" : value;
	}

	private string FormatList(List<string> list)
	{
		if (list == null || list.Count == 0) return "(空)";
		return string.Join(", ", list);
	}
}


