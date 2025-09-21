using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using InventorySystem;

namespace InventorySystem.Mission
{
	/// <summary>
	/// MissionAccept 面板中的“镜像装备槽”管理：
	/// - 在面板激活时，将玩家当前已装备的 背包/挂具 克隆到本面板的两个装备槽预制件中显示
	/// - 不移动/不影响真实背包系统中的装备与交互
	/// - 关闭面板时销毁镜像并清理，避免污染背包系统
	/// 使用方法：挂载到 MissionAccept 根对象；在 Inspector 绑定两个镜像槽（或开启自动查找）。
	/// </summary>
	[DisallowMultipleComponent]
	public sealed class MissionAcceptEquipmentMirror : MonoBehaviour
	{
		[Header("镜像槽引用（可留空，自动查找）")]
		[SerializeField] private EquipmentSlot backpackMirrorSlot;   // slotType=Backpack
		[SerializeField] private EquipmentSlot rigMirrorSlot;        // slotType=TacticalRig

		[Header("行为设置")]
		[SerializeField] private bool autoFindMirrorSlots = true;
		[SerializeField] private bool containersReadOnly = true;     // 镜像容器只读
		[SerializeField] private bool blockSlotInteractions = true;  // 屏蔽镜像槽交互

		private EquipmentSlotManager slotManager;

		private void OnEnable()
		{
			slotManager = EquipmentSlotManager.Instance;
			StartCoroutine(SetupMirrorAfterFrame());
		}

		private void OnDisable()
		{
			CleanupMirrorSlot(backpackMirrorSlot);
			CleanupMirrorSlot(rigMirrorSlot);
		}

		private IEnumerator SetupMirrorAfterFrame()
		{
			// 等一帧，确保 UI 与管理器就绪
			yield return null;

			if (autoFindMirrorSlots)
			{
				AutoFindMirrorSlotsIfNeeded();
			}

			// 给镜像槽添加排除标记，避免被真实系统注册/保存
			AddExcludeFlag(backpackMirrorSlot);
			AddExcludeFlag(rigMirrorSlot);

			// 背包与挂具
			MirrorOneSlot(EquipmentSlotType.Backpack, backpackMirrorSlot);
			MirrorOneSlot(EquipmentSlotType.TacticalRig, rigMirrorSlot);
		}

		private void AddExcludeFlag(EquipmentSlot slot)
		{
			if (slot == null) return;
			if (slot.GetComponent<ExcludeFromEquipmentSystem>() == null)
			{
				slot.gameObject.AddComponent<ExcludeFromEquipmentSystem>();
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

		private void MirrorOneSlot(EquipmentSlotType type, EquipmentSlot mirrorSlot)
		{
			if (mirrorSlot == null || slotManager == null) return;

			// 真实槽位
			var realSlot = slotManager.GetEquipmentSlot(type);
			if (realSlot == null || !realSlot.HasEquippedItem || realSlot.CurrentEquippedItem == null)
			{
				// 没有装备则清理镜像
				CleanupMirrorSlot(mirrorSlot);
				return;
			}

			// 清理旧镜像
			CleanupMirrorSlot(mirrorSlot);

			// 克隆一个展示用实例（不影响原实例）
			var src = realSlot.CurrentEquippedItem.gameObject;
			var clone = Instantiate(src);
			clone.name = src.name + " (Mirror)";

			// 关键：去除/重置与网格相关的组件与状态，避免被误认为在某个网格中
			SanitizeClonedItem(clone);

			// 装备到镜像槽（不会影响原槽）
			var reader = clone.GetComponent<ItemDataReader>();
			if (reader != null)
			{
				mirrorSlot.EquipItem(reader);
			}

			// 将镜像槽设置为只读与不可交互
			if (blockSlotInteractions)
			{
				EnsureBlockedInteractions(mirrorSlot.gameObject);
			}

			// 镜像容器只读
			if (containersReadOnly && mirrorSlot.ContainerGrid != null)
			{
				var grid = mirrorSlot.ContainerGrid;
				grid.AccessLevel = GridAccessLevel.ReadOnly;
				var gi = grid.GetComponent<GridInteract>();
				if (gi != null) gi.enabled = false;
			}
		}

		private void SanitizeClonedItem(GameObject clone)
		{
			if (clone == null) return;

			// 移除或禁用可能影响网格/交互的组件
			var item = clone.GetComponent<Item>();
			if (item != null)
			{
				// 重置所有网格状态并清空引用，避免 EquipItem 误认为处在某网格
				try { item.ResetGridState(); } catch { }
				item.OnGridReference = null;
			}

			var gridInteract = clone.GetComponent<GridInteract>();
			if (gridInteract != null) gridInteract.enabled = false;

			var highlight = clone.GetComponent<ItemHighlight>();
			if (highlight != null) highlight.HideHighlight();

			// 位置/缩放初始化（EquipItem 会再次校准）
			var rt = clone.GetComponent<RectTransform>();
			if (rt != null)
			{
				rt.localScale = Vector3.one;
				rt.anchoredPosition = Vector2.zero;
			}
		}

		private void EnsureBlockedInteractions(GameObject go)
		{
			if (go == null) return;
			var cg = go.GetComponent<CanvasGroup>();
			if (cg == null) cg = go.AddComponent<CanvasGroup>();
			// 不可交互且阻止射线，避免 OnDrop/点击等
			cg.interactable = false;
			cg.blocksRaycasts = true;
		}

		private void CleanupMirrorSlot(EquipmentSlot mirrorSlot)
		{
			if (mirrorSlot == null) return;
			// 若镜像槽有装备，调用卸载；由于克隆体已无 Item 组件，卸载流程会直接销毁而不会回流到任何网格
			if (mirrorSlot.HasEquippedItem)
			{
				try { mirrorSlot.UnequipItem(); } catch { }
			}
			// 额外清理其容器网格对象（若有残留）
			var grid = mirrorSlot.ContainerGrid;
			if (grid != null)
			{
				var go = grid.gameObject;
				if (go != null) Destroy(go);
			}
		}
	}
}


