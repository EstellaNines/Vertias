using System;
using UnityEngine;
using GlobalMessaging;
using InventorySystem;

namespace Persistence
{
	public class PlayerWeaponPersistenceAdapter : MonoBehaviour
	{
		[Header("保存配置")]
		public bool enablePersistence = true;
		public bool enableAutosave = true;
		public float autosaveIntervalSec = 5f;
		public bool forceSaveOnQuit = true;
		public bool forceSaveOnSceneUnload = true;
		public int saveDebounceMs = 300;

		[Header("对象引用")]
		public Transform hand; // PlayerWeaponEquipController 使用的 Hand；可选，仅用于旋转读取

		private WeaponPersistenceService _service;
		private float _nextAutosaveTime;
		private bool _dirty;
		private GameObject[] _equipped = new GameObject[2];
		private int _activeSlotIndex = 0;
		private int _restoredActiveSlot = -1;

		private void Awake()
		{
			_service = new WeaponPersistenceService(saveDebounceMs);
		}

		private void OnEnable()
		{
			if (!enablePersistence) return;
			MessagingCenter.Instance.Register<WeaponMessageBus.WeaponEquippedEvent>(OnEquipped);
			MessagingCenter.Instance.Register<WeaponMessageBus.WeaponUnequippedEvent>(OnUnequipped);
			MessagingCenter.Instance.Register<WeaponMessageBus.WeaponSwitchedEvent>(OnSwitched);
			// 延后一帧再恢复，确保订阅者（如 PlayerWeaponEquipController）已完成注册
			StartCoroutine(RestoreAfterOneFrame());
			// 周期保存计时
			_nextAutosaveTime = Time.unscaledTime + autosaveIntervalSec;
		}

		private System.Collections.IEnumerator RestoreAfterOneFrame()
		{
			yield return null;
			TryRestore();
		}

		private void OnDisable()
		{
			if (!enablePersistence) return;
			MessagingCenter.Instance.Unregister<WeaponMessageBus.WeaponEquippedEvent>(OnEquipped);
			MessagingCenter.Instance.Unregister<WeaponMessageBus.WeaponUnequippedEvent>(OnUnequipped);
			MessagingCenter.Instance.Unregister<WeaponMessageBus.WeaponSwitchedEvent>(OnSwitched);
			// 组件禁用时尽量保存一次（非强制）
			ScheduleSave();
		}

		private void Update()
		{
			if (!enablePersistence) return;
			// 周期自动保存（结合脏检查与节流）
			if (enableAutosave && Time.unscaledTime >= _nextAutosaveTime)
			{
				_nextAutosaveTime = Time.unscaledTime + Mathf.Max(0.2f, autosaveIntervalSec);
				if (_dirty)
				{
					_service.SaveSafe(CreateSnapshot);
					_dirty = false;
				}
			}
			// 节流队列落盘
			_service.FlushIfPending(CreateSnapshot);
		}

		private void OnApplicationQuit()
		{
			if (!enablePersistence || !forceSaveOnQuit) return;
			// 退出前强制保存
			_service.SaveSafe(CreateSnapshot);
		}

		// 事件处理
		private void OnEquipped(WeaponMessageBus.WeaponEquippedEvent e)
		{
			if (!enablePersistence) return;
			int slot = Mathf.Clamp(e.slotIndex, 0, 1);
			_equipped[slot] = e.instance;
			_dirty = true;
			ScheduleSave();
		}

		private void OnUnequipped(WeaponMessageBus.WeaponUnequippedEvent e)
		{
			if (!enablePersistence) return;
			int slot = Mathf.Clamp(e.slotIndex, 0, 1);
			_equipped[slot] = null;
			_dirty = true;
			ScheduleSave();
		}

		private void OnSwitched(WeaponMessageBus.WeaponSwitchedEvent e)
		{
			if (!enablePersistence) return;
			_activeSlotIndex = Mathf.Clamp(e.to, 0, 1);
			_dirty = true;
			ScheduleSave();
		}

		private void ScheduleSave()
		{
			_service.ScheduleSave(CreateSnapshot);
		}

		// 构造保存镜像
		private WeaponPersistStateDTO CreateSnapshot()
		{
			var dto = new WeaponPersistStateDTO();
			dto.version = _service.CurrentVersion;
			dto.activeSlot = _activeSlotIndex;

			for (int i = 0; i < 2; i++)
			{
				var slot = new WeaponSlotStateDTO();
				var go = _equipped[i];
				if (go != null)
				{
					slot.hasWeapon = true;
					var wm = go.GetComponent<WeaponManager>();
					if (wm != null)
					{
						slot.ammo = wm.GetCurrentAmmo();
						slot.fireMode = wm.fireMode.ToString();
						slot.rotationZ = go.transform.localEulerAngles.z;
					}
					var so = wm != null ? wm.itemData : null;
					if (so != null)
					{
						slot.itemId = so.id;
						slot.itemGlobalId = so.GlobalId;
					}
					slot.isActive = (i == _activeSlotIndex);
					slot.savedAt = DateTime.UtcNow.Ticks;
				}
				else
				{
					slot.hasWeapon = false;
				}
				dto.slots[i] = slot;
			}
			return dto;
		}

		// 恢复流程
		private void TryRestore()
		{
			if (!_service.Load(out var state))
			{
				// 无存档，忽略
				return;
			}
			_restoredActiveSlot = (state.activeSlot == 0 || state.activeSlot == 1) ? state.activeSlot : -1;
			// 逐槽恢复：通过消息驱动实例化
			for (int i = 0; i < 2; i++)
			{
				var slot = state.slots[i];
				if (!slot.hasWeapon) continue;
				if (!ItemDataRepository.TryGetById(slot.itemId, out ItemDataSO so))
				{
					// id 失败，尝试 gid
					if (!ItemDataRepository.TryGetByGlobalId(slot.itemGlobalId, out so))
					{
						Debug.LogWarning($"[WeaponPersist] Restore fail: slot {i} item not found");
						continue;
					}
				}
				// 装备并实例化
				MessagingCenter.Instance.Send(new WeaponMessageBus.EquipWeaponCommand { slotIndex = i, itemSO = so });
			}
			// 首次尝试恢复激活槽（可能因实例化未完成被忽略，下一帧会再发一次）
			if (_restoredActiveSlot != -1)
			{
				MessagingCenter.Instance.Send(new WeaponMessageBus.SetActiveWeaponSlot { slotIndex = _restoredActiveSlot });
				_activeSlotIndex = _restoredActiveSlot;
			}

			// 延后一帧写入运行态（等待实例化完成）
			StartCoroutine(ApplyRuntimeStateNextFrame(state));
		}

		private System.Collections.IEnumerator ApplyRuntimeStateNextFrame(WeaponPersistStateDTO state)
		{
			yield return null;
			for (int i = 0; i < 2; i++)
			{
				var slot = state.slots[i];
				if (!slot.hasWeapon) continue;
				var go = _equipped[i];
				if (go == null) continue;
				var wm = go.GetComponent<WeaponManager>();
				if (wm == null) continue;

				// 恢复弹药
				wm.currentAmmo = Mathf.Clamp(slot.ammo, 0, wm.GetMagazineCapacity());
				// 恢复开火方式（若保存值与SO不同，以保存值为准）
				if (!string.IsNullOrEmpty(slot.fireMode))
				{
					if (string.Equals(slot.fireMode, "FullAuto", StringComparison.OrdinalIgnoreCase)) wm.fireMode = WeaponManager.FireMode.FullAuto;
					else if (string.Equals(slot.fireMode, "SemiAuto", StringComparison.OrdinalIgnoreCase)) wm.fireMode = WeaponManager.FireMode.SemiAuto;
				}
				// 基于保存角度的水平朝向来设置一次Y翻转（使用 DeltaAngle 得到有符号角度）
				float signedZ = Mathf.DeltaAngle(0f, slot.rotationZ);
				bool left = signedZ > 90f || signedZ < -90f;
				var t = go.transform;
				var ls = t.localScale;
				ls.y = left ? -Mathf.Abs(ls.y) : Mathf.Abs(ls.y);
				t.localScale = ls;
			}
			// 再次设置激活槽，确保之前未生效时能切换
			if (_restoredActiveSlot != -1)
			{
				MessagingCenter.Instance.Send(new WeaponMessageBus.SetActiveWeaponSlot { slotIndex = _restoredActiveSlot });
				_activeSlotIndex = _restoredActiveSlot;
			}
			_dirty = false;
		}
	}
}
