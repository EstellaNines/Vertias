using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using InventorySystem;

namespace InventorySystem.Chatting
{
	/// <summary>
	/// 对话系统用“可交互镜像装备栏”桥接：
	/// - 镜像槽本身不参与全局注册/保存（挂 ExcludeFromEquipmentSystem）
	/// - 不克隆真实容器网格数据，而是将真实容器网格临时挂接到镜像槽的 containerParent 下显示与交互
	/// - 关闭/禁用时将真实容器网格还原至原父级与布局
	/// 适配：BackpackEquipmentSlot_Chatting、TacticalRigEquipmentSlot_Chatting
	/// 用法：挂到对话界面根物体，关联两个镜像槽，或启用自动查找
	/// </summary>
	[DisallowMultipleComponent]
	public sealed class DialogueInteractiveEquipmentMirror : MonoBehaviour
	{
		[Header("镜像槽引用（可留空，自动查找）")]
		[SerializeField] private EquipmentSlot backpackMirrorSlot;
		[SerializeField] private EquipmentSlot rigMirrorSlot;

		[Header("行为设置")]
		[SerializeField] private bool autoFindMirrorSlots = true;
		[SerializeField] private bool addExcludeFlagToMirrorSlots = true;
		[SerializeField] private bool reparentRealContainerGrid = false; // 只读镜像：不重挂真实容器网格
		[SerializeField] private bool allowInteractions = false; // 只读镜像：禁止交互

		private EquipmentSlotManager slotManager;

		// 记录被移动的真实容器网格的原 Transform 信息，便于还原
		private readonly Dictionary<ItemGrid, RectSnapshot> movedGrids = new Dictionary<ItemGrid, RectSnapshot>();

		private void OnEnable()
		{
			slotManager = EquipmentSlotManager.Instance;
			StartCoroutine(SetupAfterFrame());
			EquipmentSlotManager.OnEquipmentChanged += OnEquipmentChanged;
		}

		private void OnDisable()
		{
			EquipmentSlotManager.OnEquipmentChanged -= OnEquipmentChanged;
			RestoreAllGrids();
		}

		private IEnumerator SetupAfterFrame()
		{
			yield return null; // 等一帧确保 UI/管理器就绪

			if (autoFindMirrorSlots)
			{
				AutoFindMirrorSlotsIfNeeded();
			}

			if (addExcludeFlagToMirrorSlots)
			{
				AddExcludeFlag(backpackMirrorSlot);
				AddExcludeFlag(rigMirrorSlot);
			}

			// 背包与挂具
			AttachContainerGrid(EquipmentSlotType.Backpack, backpackMirrorSlot);
			AttachContainerGrid(EquipmentSlotType.TacticalRig, rigMirrorSlot);
		}

		private void OnEquipmentChanged(EquipmentSlotType slotType, ItemDataReader item)
		{
			// 当背包/挂具变化时，重新挂接网格
			if (slotType == EquipmentSlotType.Backpack)
			{
				AttachContainerGrid(EquipmentSlotType.Backpack, backpackMirrorSlot);
			}
			else if (slotType == EquipmentSlotType.TacticalRig)
			{
				AttachContainerGrid(EquipmentSlotType.TacticalRig, rigMirrorSlot);
			}
		}

		private void AutoFindMirrorSlotsIfNeeded()
		{
			var all = GetComponentsInChildren<EquipmentSlot>(true);
			for (int i = 0; i < all.Length; i++)
			{
				var s = all[i];
				if (s == null || s.config == null) continue;
				if (backpackMirrorSlot == null && s.config.slotType == EquipmentSlotType.Backpack) backpackMirrorSlot = s;
				if (rigMirrorSlot == null && s.config.slotType == EquipmentSlotType.TacticalRig) rigMirrorSlot = s;
			}
		}

		private void AddExcludeFlag(EquipmentSlot slot)
		{
			if (slot == null) return;
			if (slot.GetComponent<ExcludeFromEquipmentSystem>() == null)
			{
				slot.gameObject.AddComponent<ExcludeFromEquipmentSystem>();
			}
		}

		private void AttachContainerGrid(EquipmentSlotType type, EquipmentSlot mirrorSlot)
		{
			// 只读镜像：克隆真实装备为展示体，不改变真实容器网格
			if (mirrorSlot == null || slotManager == null) return;

			var realSlot = slotManager.GetEquipmentSlot(type);
			if (realSlot == null || !realSlot.HasEquippedItem || realSlot.CurrentEquippedItem == null) return;

			var src = realSlot.CurrentEquippedItem.gameObject;
			var clone = Object.Instantiate(src);
			clone.name = src.name + " (Mirror)";
			SanitizeClonedItem(clone);
			var reader = clone.GetComponent<ItemDataReader>();
			if (reader != null)
			{
				try { mirrorSlot.UnequipItem(); } catch { }
				mirrorSlot.EquipItem(reader);
			}
			// 镜像槽整体阻止交互
			EnsureBlockedInteractions(mirrorSlot.gameObject);
			// 若镜像槽生成了容器网格，则设为只读
			var mirrorGrid = mirrorSlot.ContainerGrid;
			if (mirrorGrid != null)
			{
				mirrorGrid.AccessLevel = GridAccessLevel.ReadOnly;
				var gi = mirrorGrid.GetComponent<GridInteract>();
				if (gi != null) gi.enabled = false;
			}
		}

		private void RestoreAllGrids()
		{
			if (movedGrids.Count == 0) return;
			var list = new List<ItemGrid>(movedGrids.Keys);
			for (int i = 0; i < list.Count; i++)
			{
				RestoreGrid(list[i]);
			}
		}

		private void RestoreGridForType(EquipmentSlotType type)
		{
			if (slotManager == null) return;
			var slot = slotManager.GetEquipmentSlot(type);
			if (slot == null) return;
			var grid = slot.ContainerGrid;
			if (grid != null)
			{
				RestoreGrid(grid);
			}
		}

		private void RestoreGrid(ItemGrid grid)
		{
			if (grid == null) return;
			if (!movedGrids.TryGetValue(grid, out var snap)) return;
			var rt = grid.GetComponent<RectTransform>();
			if (rt == null) { movedGrids.Remove(grid); return; }

			snap.Restore(rt);
			movedGrids.Remove(grid);
		}

		// 仅供“只读克隆”备选方案或外部复用时调用
		private void SanitizeClonedItem(GameObject clone)
		{
			if (clone == null) return;
			var item = clone.GetComponent<Item>();
			if (item != null)
			{
				try { item.ResetGridState(); } catch { }
				item.OnGridReference = null;
			}
			var gridInteract = clone.GetComponent<GridInteract>();
			if (gridInteract != null) gridInteract.enabled = false;
			var highlight = clone.GetComponent<ItemHighlight>();
			if (highlight != null) highlight.HideHighlight();
			var rt = clone.GetComponent<RectTransform>();
			if (rt != null)
			{
				rt.localScale = Vector3.one;
				rt.anchoredPosition = Vector2.zero;
			}
		}

		// 将镜像槽整体设为不可交互，用于只读模式
		private void EnsureBlockedInteractions(GameObject go)
		{
			if (go == null) return;
			var cg = go.GetComponent<CanvasGroup>();
			if (cg == null) cg = go.AddComponent<CanvasGroup>();
			cg.interactable = false;
			cg.blocksRaycasts = true;
		}

		private static void FitToParent(RectTransform rt)
		{
			if (rt == null || rt.parent == null) return;
			rt.anchorMin = new Vector2(0f, 0f);
			rt.anchorMax = new Vector2(1f, 1f);
			rt.pivot = new Vector2(0.5f, 0.5f);
			rt.anchoredPosition = Vector2.zero;
			rt.sizeDelta = Vector2.zero;
			rt.localScale = Vector3.one;
		}

		private struct RectSnapshot
		{
			public Transform parent;
			public int siblingIndex;
			public Vector2 anchorMin;
			public Vector2 anchorMax;
			public Vector2 anchoredPosition;
			public Vector2 sizeDelta;
			public Vector2 pivot;
			public Vector3 localScale;

			public static RectSnapshot Capture(RectTransform rt)
			{
				RectSnapshot s;
				s.parent = rt.parent;
				s.siblingIndex = rt.GetSiblingIndex();
				s.anchorMin = rt.anchorMin;
				s.anchorMax = rt.anchorMax;
				s.anchoredPosition = rt.anchoredPosition;
				s.sizeDelta = rt.sizeDelta;
				s.pivot = rt.pivot;
				s.localScale = rt.localScale;
				return s;
			}

			public void Restore(RectTransform rt)
			{
				if (rt == null) return;
				rt.SetParent(parent, worldPositionStays: false);
				rt.SetSiblingIndex(siblingIndex);
				rt.anchorMin = anchorMin;
				rt.anchorMax = anchorMax;
				rt.anchoredPosition = anchoredPosition;
				rt.sizeDelta = sizeDelta;
				rt.pivot = pivot;
				rt.localScale = localScale;
			}
		}
	}
}


