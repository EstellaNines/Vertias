using System;
using UnityEngine;

namespace Persistence
{
	public class WeaponPersistenceService
	{
		private const string KeyVersion = "weapon/version";
		private const string KeyActiveSlot = "weapon/activeSlot";
		private const string KeySlot0 = "weapon/slots/0";
		private const string KeySlot1 = "weapon/slots/1";

		public int CurrentVersion => 1;

		private bool _pending;
		private float _nextAllowedTime;
		private readonly float _debounceSeconds;

		public WeaponPersistenceService(float debounceMs = 300f)
		{
			_debounceSeconds = Mathf.Max(0.0f, debounceMs) / 1000f;
		}

		public void Save(WeaponPersistStateDTO state)
		{
			try
			{
				ES3.Save<int>(KeyVersion, state.version);
				ES3.Save<int>(KeyActiveSlot, state.activeSlot);
				ES3.Save<WeaponSlotStateDTO>(KeySlot0, state.slots[0]);
				ES3.Save<WeaponSlotStateDTO>(KeySlot1, state.slots[1]);
			}
			catch (Exception e)
			{
				Debug.LogError($"[WeaponPersistence] Save failed: {e.Message}");
			}
		}

		public bool Load(out WeaponPersistStateDTO state)
		{
			state = new WeaponPersistStateDTO();
			try
			{
				if (!ES3.KeyExists(KeyVersion)) return false;
				int ver = ES3.Load<int>(KeyVersion);
				state.version = ver;
				// 简单版本兼容：仅支持 v1；其它版本直接尝试字段读取，失败则返回 false
				state.activeSlot = ES3.KeyExists(KeyActiveSlot) ? ES3.Load<int>(KeyActiveSlot) : 0;
				state.slots[0] = ES3.KeyExists(KeySlot0) ? ES3.Load<WeaponSlotStateDTO>(KeySlot0) : new WeaponSlotStateDTO();
				state.slots[1] = ES3.KeyExists(KeySlot1) ? ES3.Load<WeaponSlotStateDTO>(KeySlot1) : new WeaponSlotStateDTO();
				return true;
			}
			catch (Exception e)
			{
				Debug.LogWarning($"[WeaponPersistence] Load failed: {e.Message}");
				return false;
			}
		}

		public void Clear()
		{
			try
			{
				if (ES3.KeyExists(KeyVersion)) ES3.DeleteKey(KeyVersion);
				if (ES3.KeyExists(KeyActiveSlot)) ES3.DeleteKey(KeyActiveSlot);
				if (ES3.KeyExists(KeySlot0)) ES3.DeleteKey(KeySlot0);
				if (ES3.KeyExists(KeySlot1)) ES3.DeleteKey(KeySlot1);
			}
			catch (Exception e)
			{
				Debug.LogWarning($"[WeaponPersistence] Clear failed: {e.Message}");
			}
		}

		// 节流保存：在下一次允许时间调用保存
		public void ScheduleSave(Func<WeaponPersistStateDTO> stateFactory)
		{
			if (Time.unscaledTime < _nextAllowedTime)
			{
				_pending = true;
				return;
			}
			_nextAllowedTime = Time.unscaledTime + _debounceSeconds;
			_pending = false;
			SaveSafe(stateFactory);
		}

		// 在 Update/定时器中轮询，可在适配器中调用
		public void FlushIfPending(Func<WeaponPersistStateDTO> stateFactory)
		{
			if (_pending && Time.unscaledTime >= _nextAllowedTime)
			{
				_nextAllowedTime = Time.unscaledTime + _debounceSeconds;
				_pending = false;
				SaveSafe(stateFactory);
			}
		}

		public void SaveSafe(Func<WeaponPersistStateDTO> stateFactory)
		{
			try
			{
				var state = stateFactory != null ? stateFactory() : new WeaponPersistStateDTO();
				Save(state);
			}
			catch (Exception e)
			{
				Debug.LogError($"[WeaponPersistence] SaveSafe failed: {e.Message}");
			}
		}
	}
}
