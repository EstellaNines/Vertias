using GlobalMessaging;
using InventorySystem;
using UnityEngine;

// 璐熻矗鎺ユ敹瑁呭/鍗歌浇/鍒囨崲/杈撳叆鎸囦护锛屽苟鍦� Hand 涓嬪疄渚嬪寲涓庣鐞嗕富/鍓鍣�
public class PlayerWeaponEquipController : MonoBehaviour
{
	[Header("鎸傜偣")]
	public Transform hand;

	[Header("鐘舵€�")]
	public GameObject[] equipped = new GameObject[2]; // 0=涓伙紝1=鍓�
	public int activeSlotIndex = 0;

	[Header("切换去抖")]
	[Tooltip("滚轮切换的最小间隔(秒)，避免快速滚动导致抖动切换")]
	public float scrollSwitchCooldown = 0.15f;
	private float _lastScrollSwitchTime = -999f;

	[Header("翻转维护")]
	public bool enableRuntimeFlip = true;

	private void OnEnable()
	{
		MessagingCenter.Instance.Register<WeaponMessageBus.EquipWeaponCommand>(OnEquipWeapon);
		MessagingCenter.Instance.Register<WeaponMessageBus.UnequipWeaponCommand>(OnUnequipWeapon);
		MessagingCenter.Instance.Register<WeaponMessageBus.SetActiveWeaponSlot>(OnSetActiveSlot);
		MessagingCenter.Instance.Register<WeaponMessageBus.ScrollSwitchWeapon>(OnScrollSwitch);
		MessagingCenter.Instance.Register<WeaponMessageBus.SetFiringInput>(OnSetFiringInput);
		MessagingCenter.Instance.Register<WeaponMessageBus.ReloadCommand>(OnReload);
		Debug.Log("[PlayerEquip] 注册消息完成");
	}

	private void OnDisable()
	{
		MessagingCenter.Instance.Unregister<WeaponMessageBus.EquipWeaponCommand>(OnEquipWeapon);
		MessagingCenter.Instance.Unregister<WeaponMessageBus.UnequipWeaponCommand>(OnUnequipWeapon);
		MessagingCenter.Instance.Unregister<WeaponMessageBus.SetActiveWeaponSlot>(OnSetActiveSlot);
		MessagingCenter.Instance.Unregister<WeaponMessageBus.ScrollSwitchWeapon>(OnScrollSwitch);
		MessagingCenter.Instance.Unregister<WeaponMessageBus.SetFiringInput>(OnSetFiringInput);
		MessagingCenter.Instance.Unregister<WeaponMessageBus.ReloadCommand>(OnReload);
	}

	private bool IsValidSlot(int slot) => slot == 0 || slot == 1;

	private void OnEquipWeapon(WeaponMessageBus.EquipWeaponCommand msg)
	{
		Debug.Log($"[PlayerEquip] 收到 EquipWeaponCommand: slot={msg.slotIndex}, item={(msg.itemSO!=null?msg.itemSO.itemName:"null")}");
		int slot = Mathf.Clamp(msg.slotIndex, 0, 1);
		if (msg.itemSO == null || msg.itemSO.category != ItemCategory.Weapon)
		{
			Debug.LogWarning("[PlayerEquip] 非武器或 itemSO 空，忽略");
			MessagingCenter.Instance.Send(new WeaponMessageBus.WeaponEquipFailed{ slotIndex = slot, reason = "Not a weapon"});
			return;
		}
		if (msg.itemSO.weapon == null || string.IsNullOrEmpty(msg.itemSO.weapon.weaponPrefabAddress))
		{
			Debug.LogError("[PlayerEquip] 缺少 weaponPrefabAddress");
			MessagingCenter.Instance.Send(new WeaponMessageBus.WeaponEquipFailed{ slotIndex = slot, reason = "Missing weapon address"});
			return;
		}
		if (hand == null)
		{
			Debug.LogError("[PlayerEquip] hand 未绑定");
			MessagingCenter.Instance.Send(new WeaponMessageBus.WeaponEquipFailed{ slotIndex = slot, reason = "Missing Hand transform"});
			return;
		}

		// 卸下旧武器（同槽）
		if (equipped[slot] != null)
		{
			ForceUnequip(slot);
		}

		var prefab = Resources.Load<GameObject>(msg.itemSO.weapon.weaponPrefabAddress);
		if (prefab == null)
		{
			Debug.LogError($"[PlayerEquip] Resources.Load 失败: {msg.itemSO.weapon.weaponPrefabAddress}");
			MessagingCenter.Instance.Send(new WeaponMessageBus.WeaponEquipFailed{ slotIndex = slot, reason = "Load prefab failed"});
			return;
		}
		var go = Instantiate(prefab, hand, false);
		go.transform.localPosition = Vector3.zero;
		go.transform.localRotation = Quaternion.identity;
		go.transform.localScale = Vector3.one;

		var wm = go.GetComponent<WeaponManager>();
		if (wm == null)
		{
			Debug.LogError("[PlayerEquip] WeaponManager 缺失，销毁实例");
			Destroy(go);
			MessagingCenter.Instance.Send(new WeaponMessageBus.WeaponEquipFailed{ slotIndex = slot, reason = "WeaponManager missing"});
			return;
		}
		wm.itemData = msg.itemSO;
		wm.InitializeFromItemData(true); // 统一用SO初始化，并重置弹药独立到满弹
		wm.OnPickedUp(transform);

		if (slot != activeSlotIndex)
		{
			go.SetActive(false);
			go.transform.SetAsLastSibling();
		}
		else
		{
			// 确保激活武器在 Hand 的第一个子物体位置，以便 Player.cs 正确获取
			go.transform.SetAsFirstSibling();
			ApplyFlip(go);
		}

		equipped[slot] = go;
		Debug.Log($"[PlayerEquip] 装备完成: slot={slot}, go={go.name}");
		MessagingCenter.Instance.Send(new WeaponMessageBus.WeaponEquippedEvent{ slotIndex = slot, instance = go, weaponName = wm.GetWeaponName()});
	}

	private void OnUnequipWeapon(WeaponMessageBus.UnequipWeaponCommand msg)
	{
		Debug.Log($"[PlayerEquip] 收到 UnequipWeaponCommand: slot={msg.slotIndex}");
		int slot = Mathf.Clamp(msg.slotIndex, 0, 1);
		ForceUnequip(slot);
		MessagingCenter.Instance.Send(new WeaponMessageBus.WeaponUnequippedEvent{ slotIndex = slot });
	}

	private void ForceUnequip(int slot)
	{
		var go = equipped[slot];
		if (go == null) return;
		var wm = go.GetComponent<WeaponManager>();
		if (wm != null)
		{
			wm.SetFiring(false);
			wm.OnDropped();
		}
		Destroy(go);
		equipped[slot] = null;

		int other = slot == 0 ? 1 : 0;
		if (slot == activeSlotIndex && equipped[other] != null)
		{
			DoSwitch(other);
		}
	}

	private void OnSetActiveSlot(WeaponMessageBus.SetActiveWeaponSlot msg)
	{
		Debug.Log($"[PlayerEquip] 收到 SetActiveWeaponSlot: slot={msg.slotIndex}");
		int slot = Mathf.Clamp(msg.slotIndex, 0, 1);
		if (equipped[slot] == null) { Debug.LogWarning("[PlayerEquip] 目标槽无装备"); return; }
		if (slot == activeSlotIndex) return;
		DoSwitch(slot);
	}

	private void OnScrollSwitch(WeaponMessageBus.ScrollSwitchWeapon msg)
	{
		// 去抖：限制最小切换间隔
		float now = Time.unscaledTime;
		if (now - _lastScrollSwitchTime < scrollSwitchCooldown) return;

		int dir = msg.delta == 0 ? 0 : (msg.delta > 0 ? 1 : -1);
		if (dir == 0) return;
		int next = (activeSlotIndex + dir + 2) % 2;
		if (equipped[next] == null) return;
		_lastScrollSwitchTime = now;
		DoSwitch(next);
	}

	private void DoSwitch(int next)
	{
		int from = activeSlotIndex;
		var oldGo = equipped[from];
		if (oldGo != null)
		{
			var oldWm = oldGo.GetComponent<WeaponManager>();
			if (oldWm != null) oldWm.SetFiring(false);
			oldGo.SetActive(false);
			oldGo.transform.SetAsLastSibling();
		}

		var newGo = equipped[next];
		if (newGo != null)
		{
			newGo.SetActive(true);
			newGo.transform.SetAsFirstSibling();
			ApplyFlip(newGo);
			var newWm = newGo.GetComponent<WeaponManager>();
			if (newWm != null) newWm.OnPickedUp(transform);
		}
		activeSlotIndex = next;
		Debug.Log($"[PlayerEquip] 切换武器: {from} -> {next}");
		MessagingCenter.Instance.Send(new WeaponMessageBus.WeaponSwitchedEvent{ from = from, to = next });
	}

	private void OnSetFiringInput(WeaponMessageBus.SetFiringInput msg)
	{
		var go = equipped[activeSlotIndex];
		if (go == null)
		{
			Debug.LogWarning("[PlayerEquip] 当前槽无武器，忽略开火输入");
			return;
		}
		// 仅将输入发送给当前激活的武器
		var wm = go.GetComponent<WeaponManager>();
		if (wm == null)
		{
			Debug.LogError("[PlayerEquip] 当前武器缺少 WeaponManager");
			return;
		}
		wm.SetFiring(msg.isPressed);
	}

	private void OnReload(WeaponMessageBus.ReloadCommand msg)
	{
		var go = equipped[activeSlotIndex];
		if (go == null) return;
		var wm = go.GetComponent<WeaponManager>();
		if (wm == null) return;
		wm.StartReload();
	}

	private void ApplyFlip(GameObject go)
	{
		if (hand == null || go == null) return;
		// 基于 Hand 的水平朝向判断左右：hand.right.x < 0 代表面向左侧
		bool left = hand.right.x < 0f;
		var ls = go.transform.localScale;
		ls.y = left ? -Mathf.Abs(ls.y) : Mathf.Abs(ls.y);
		go.transform.localScale = ls;
	}

	private void LateUpdate()
	{
		if (!enableRuntimeFlip) return;
		if (hand == null) return;
		var go = equipped[activeSlotIndex];
		if (go == null || !go.activeInHierarchy) return;
		ApplyFlip(go);
	}
}
