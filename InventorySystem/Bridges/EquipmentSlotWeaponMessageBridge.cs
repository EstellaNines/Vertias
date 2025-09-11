using GlobalMessaging;
using UnityEngine;

namespace InventorySystem
{
	public class EquipmentSlotWeaponMessageBridge : MonoBehaviour
	{
		[Tooltip("装备后是否将该槽位设为激活武器")]
		public bool switchActiveOnEquip = true;

		private void OnEnable()
		{
			EquipmentSlot.OnItemEquipped += HandleItemEquipped;
			EquipmentSlot.OnItemUnequipped += HandleItemUnequipped;
			Debug.Log("[Bridge] EquipmentSlotWeaponMessageBridge 已启用并开始监听装备槽事件");
		}

		private void OnDisable()
		{
			EquipmentSlot.OnItemEquipped -= HandleItemEquipped;
			EquipmentSlot.OnItemUnequipped -= HandleItemUnequipped;
			Debug.Log("[Bridge] EquipmentSlotWeaponMessageBridge 已禁用并停止监听");
		}

		private void HandleItemEquipped(EquipmentSlotType slotType, ItemDataReader itemReader)
		{
			if (itemReader == null || itemReader.ItemData == null)
			{
				Debug.LogWarning("[Bridge] 装备事件收到的 ItemDataReader 为空");
				return;
			}

			int? slotIndex = MapSlotIndex(slotType);
			if (slotIndex == null)
			{
				Debug.Log("[Bridge] 装备槽类型并非主/副武器，忽略: " + slotType);
				return;
			}

			if (itemReader.ItemData.category != ItemCategory.Weapon)
			{
				Debug.Log($"[Bridge] 装备的并非武器，忽略: {itemReader.ItemData.itemName}");
				return;
			}

			if (MessagingCenter.Instance == null)
			{
				Debug.LogError("[Bridge] MessagingCenter.Instance 为 null，无法发送装备消息");
				return;
			}

			Debug.Log($"[Bridge] 发送 EquipWeaponCommand: slot={slotIndex.Value}, item={itemReader.ItemData.itemName}");
			MessagingCenter.Instance.Send(new WeaponMessageBus.EquipWeaponCommand
			{
				slotIndex = slotIndex.Value,
				itemSO = itemReader.ItemData
			});

			if (switchActiveOnEquip)
			{
				Debug.Log($"[Bridge] 发送 SetActiveWeaponSlot: slot={slotIndex.Value}");
				MessagingCenter.Instance.Send(new WeaponMessageBus.SetActiveWeaponSlot
				{
					slotIndex = slotIndex.Value
				});
			}
		}

		private void HandleItemUnequipped(EquipmentSlotType slotType, ItemDataReader _)
		{
			int? slotIndex = MapSlotIndex(slotType);
			if (slotIndex == null) return;

			if (MessagingCenter.Instance == null)
			{
				Debug.LogError("[Bridge] MessagingCenter.Instance 为 null，无法发送卸载消息");
				return;
			}

			Debug.Log($"[Bridge] 发送 UnequipWeaponCommand: slot={slotIndex.Value}");
			MessagingCenter.Instance.Send(new WeaponMessageBus.UnequipWeaponCommand
			{
				slotIndex = slotIndex.Value
			});
		}

		private int? MapSlotIndex(EquipmentSlotType slotType)
		{
			switch (slotType)
			{
				case EquipmentSlotType.PrimaryWeapon:
					return 0;
				case EquipmentSlotType.SecondaryWeapon:
					return 1;
				default:
					return null;
			}
		}
	}
}
