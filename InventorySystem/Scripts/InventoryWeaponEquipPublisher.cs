using GlobalMessaging;
using InventorySystem;
using UnityEngine;

namespace InventorySystem.UI
{
	public class InventoryWeaponEquipPublisher : MonoBehaviour
	{
		[Tooltip("0=主武器槽, 1=副武器槽")]
		[Range(0, 1)]
		public int slotIndex = 0;

		[Tooltip("装备后是否切换到该槽位为激活武器")]
		public bool switchActiveOnEquip = true;

		// 供“装备”按钮/拖拽落入槽位时调用
		public void Equip(ItemDataSO itemSO)
		{
			if (itemSO == null)
			{
				Debug.LogWarning("[Equip] itemSO 为空");
				return;
			}

			if (itemSO.category != ItemCategory.Weapon)
			{
				Debug.LogWarning($"[Equip] 物品并非武器: {itemSO.name}");
				return;
			}

			int idx = Mathf.Clamp(slotIndex, 0, 1);

			MessagingCenter.Instance.Send(new WeaponMessageBus.EquipWeaponCommand
			{
				slotIndex = idx,
				itemSO = itemSO
			});

			if (switchActiveOnEquip)
			{
				MessagingCenter.Instance.Send(new WeaponMessageBus.SetActiveWeaponSlot
				{
					slotIndex = idx
				});
			}
		}

		// 供“卸载”按钮/从槽位移出时调用
		public void Unequip()
		{
			int idx = Mathf.Clamp(slotIndex, 0, 1);

			MessagingCenter.Instance.Send(new WeaponMessageBus.UnequipWeaponCommand
			{
				slotIndex = idx
			});
		}

		// 便捷方法，方便在 Button.onClick 直接绑定
		public void EquipToMain(ItemDataSO itemSO)
		{
			slotIndex = 0;
			Equip(itemSO);
		}

		public void EquipToSub(ItemDataSO itemSO)
		{
			slotIndex = 1;
			Equip(itemSO);
		}

		public void UnequipMain()
		{
			slotIndex = 0;
			Unequip();
		}

		public void UnequipSub()
		{
			slotIndex = 1;
			Unequip();
		}
	}
}