using System.Collections;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using InventorySystem;

public class WeaponManager : MonoBehaviour
{
    [Header("数据对象（可选）")]
    [FieldLabel("物品数据对象")] public InventorySystem.ItemDataSO itemData;
    public enum FireMode
    {
        FullAuto,   // 全自动：允许长按连发，也可单点单发
        SemiAuto    // 半自动：仅允许单点单发
    }
    [Header("武器基础属性")]
    [FieldLabel("武器名称")] public string weaponName = "默认武器";
    [FieldLabel("射击间隔")] public float fireRate = 0.33f;
    [FieldLabel("子弹散射角度")] public float spreadAngle = 5f;
    [FieldLabel("子弹速度")] public float bulletSpeed = 10f;
    [FieldLabel("射程")] public float range = 15f;
    [FieldLabel("伤害")] public float damage = 25f;

    [Header("开火模式")]
    [FieldLabel("开火方式")] public FireMode fireMode = FireMode.FullAuto;

    [Header("弹夹系统")]
    [FieldLabel("弹夹容量")] public int magazineCapacity = 30;
    [FieldLabel("换弹时间")] public float reloadTime = 2f;
    [HideInInspector] public int currentAmmo;
    [HideInInspector] public bool isReloading = false;
    [HideInInspector] public float reloadStartTime; // 记录换弹开始时间

    [Header("子弹预制体配置")]
    [FieldLabel("玩家子弹预制体")] public GameObject playerBulletPrefab;
    [FieldLabel("敌人子弹预制体")] public GameObject enemyBulletPrefab;
    [FieldLabel("通用子弹预制体")] public GameObject bulletPrefab;

    [Header("武器组件")]
    [FieldLabel("枪口位置")] public Transform muzzle;
    [FieldLabel("武器触发器")] public WeaponTrigger weaponTrigger;

    // 内部状态
    private Transform currentOwner; // 当前持有者
    private bool isPlayerWeapon = false; // 是否为玩家武器
    private float nextFireTime = 0f;          // 旧式节流（向后兼容）
    private float fireAccumulator = 0f;       // 稳健连发累计时间
    private const float maxBurstConsume = 3f; // 单帧最多消费的发射次数上限
    private Rigidbody2D weaponRB;
    private Collider2D weaponCollider;

    // 射击状态
    private bool isFiring = false;

    private void Awake()
    {
        InitializeWeaponComponents();
        SetupWeaponTag();
        ValidateBulletPrefabs();

        // 初始化弹药
        currentAmmo = magazineCapacity;

        // 若绑定了数据对象，则用其覆盖运行参数
        ApplyItemDataIfPresent();
    }

    private void Start()
    {
        // 检测初始持有者
        DetectOwner();
    }

    private void ApplyItemDataIfPresent()
    {
        if (itemData == null || itemData.weapon == null) return;

        // 基础数值
        weaponName = string.IsNullOrEmpty(itemData.itemName) ? weaponName : itemData.itemName;
        fireRate = itemData.weapon.fireRate > 0 ? itemData.weapon.fireRate : fireRate;
        spreadAngle = itemData.weapon.spreadAngle;
        bulletSpeed = itemData.weapon.bulletSpeed;
        range = itemData.weapon.range;
        damage = itemData.weapon.damage;
        magazineCapacity = itemData.weapon.magazineCapacity > 0 ? itemData.weapon.magazineCapacity : magazineCapacity;
        reloadTime = itemData.weapon.reloadTime > 0 ? itemData.weapon.reloadTime : reloadTime;

        // 开火方式映射
        if (!string.IsNullOrEmpty(itemData.weapon.fireMode))
        {
            if (itemData.weapon.fireMode.Equals("FullAuto", System.StringComparison.OrdinalIgnoreCase))
                fireMode = FireMode.FullAuto;
            else if (itemData.weapon.fireMode.Equals("SemiAuto", System.StringComparison.OrdinalIgnoreCase))
                fireMode = FireMode.SemiAuto;
        }

        // Resources 预制体加载（若有配置）
        var playerBullet = itemData.LoadPlayerBulletPrefabFromResources();
        if (playerBullet != null) playerBulletPrefab = playerBullet;

        var enemyBullet = itemData.LoadEnemyBulletPrefabFromResources();
        if (enemyBullet != null) enemyBulletPrefab = enemyBullet;

        // 同步 trigger 的发射参数
        if (weaponTrigger != null)
        {
            weaponTrigger.ShootInterval = fireRate;
            weaponTrigger.spreadAngle = spreadAngle;
        }

        // 重设当前弹药，使其与新弹匣容量一致（仅在初始时）
        currentAmmo = Mathf.Clamp(currentAmmo, 0, magazineCapacity);
        if (currentAmmo == 0) currentAmmo = magazineCapacity;

        // 依据参数预热多个发射间隔的对象池
        PrewarmPools(2f);
    }

    // 预热对象池（根据当前 fireRate 估算在连续射击 expectedSustainSeconds 秒内需要的子弹数量）
    public void PrewarmPools(float expectedSustainSeconds = 2f)
    {
        int expectedShots = Mathf.CeilToInt(expectedSustainSeconds / Mathf.Max(0.0001f, fireRate));
        expectedShots = Mathf.Clamp(expectedShots, 1, 100);

        if (playerBulletPrefab != null && BulletPool.Instance != null)
        {
            BulletPool.Instance.Prewarm(playerBulletPrefab, expectedShots);
        }
        if (enemyBulletPrefab != null && EnemyBulletPool.Instance != null)
        {
            EnemyBulletPool.Instance.Prewarm(enemyBulletPrefab, expectedShots);
        }
    }

    // 从 SO 初始化（可选重置弹药），并同步触发器与预热
    public void InitializeFromItemData(bool resetAmmoToFull = true)
    {
        ApplyItemDataIfPresent();
        if (resetAmmoToFull)
        {
            currentAmmo = magazineCapacity;
            isReloading = false;
        }
        if (weaponTrigger != null)
        {
            weaponTrigger.Muzzle = muzzle;
            weaponTrigger.ShootInterval = fireRate;
            weaponTrigger.spreadAngle = spreadAngle;
        }
        PrewarmPools(2f);
    }

    private void Update()
    {
        // 更新射击逻辑（仅全自动允许按住连发）
        if (fireMode == FireMode.FullAuto)
        {
            if (isFiring)
            {
                fireAccumulator += Time.deltaTime;
                float interval = Mathf.Max(0.0001f, fireRate);
                int shots = Mathf.Min((int)(fireAccumulator / interval), (int)maxBurstConsume);
                for (int i = 0; i < shots; i++)
                {
                    if (!CanFire()) break;
                    Fire();
                    fireAccumulator -= interval;
                }
            }
            else
            {
                fireAccumulator = 0f;
            }
        }
    }

    private void OnValidate()
    {
        // 在编辑器中动态预览 SO 覆盖后的参数
        if (!Application.isPlaying)
        {
            // 确保 trigger 指向最新的 muzzle
            if (weaponTrigger != null)
            {
                weaponTrigger.Muzzle = muzzle;
            }
            ApplyItemDataIfPresent();
        }
    }

    // 验证子弹预制体配置
    private void ValidateBulletPrefabs()
    {
        // 如果没有设置专用预制体，使用通用预制体
        if (playerBulletPrefab == null && bulletPrefab != null)
        {
            if (HasTag(bulletPrefab, "PlayerBullets"))
            {
                playerBulletPrefab = bulletPrefab;
            }
        }

        if (enemyBulletPrefab == null && bulletPrefab != null)
        {
            if (HasTag(bulletPrefab, "EnemyBullets"))
            {
                enemyBulletPrefab = bulletPrefab;
            }
        }

        // 验证标签
        if (playerBulletPrefab != null && !HasTag(playerBulletPrefab, "PlayerBullets"))
        {
            Debug.LogWarning($"玩家子弹预制体 {playerBulletPrefab.name} 没有 'PlayerBullets' 标签");
        }

        if (enemyBulletPrefab != null && !HasTag(enemyBulletPrefab, "EnemyBullets"))
        {
            Debug.LogWarning($"敌人子弹预制体 {enemyBulletPrefab.name} 没有 'EnemyBullets' 标签");
        }
    }

    // 检查游戏对象是否有指定标签
    private bool HasTag(GameObject obj, string tag)
    {
        return obj != null && obj.CompareTag(tag);
    }

    // 根据标签获取正确的子弹预制体
    private GameObject GetCorrectBulletPrefab(bool forPlayer)
    {
        if (forPlayer)
        {
            // 优先使用玩家专用预制体
            if (playerBulletPrefab != null && HasTag(playerBulletPrefab, "PlayerBullets"))
            {
                return playerBulletPrefab;
            }

            // 如果通用预制体有玩家标签，也可以使用
            if (bulletPrefab != null && HasTag(bulletPrefab, "PlayerBullets"))
            {
                return bulletPrefab;
            }

            Debug.LogWarning("没有找到合适的玩家子弹预制体");
            return null;
        }
        else
        {
            // 优先使用敌人专用预制体
            if (enemyBulletPrefab != null && HasTag(enemyBulletPrefab, "EnemyBullets"))
            {
                return enemyBulletPrefab;
            }

            // 如果通用预制体有敌人标签，也可以使用
            if (bulletPrefab != null && HasTag(bulletPrefab, "EnemyBullets"))
            {
                return bulletPrefab;
            }

            Debug.LogWarning("没有找到合适的敌人子弹预制体");
            return null;
        }
    }

    // 初始化武器必需组件
    private void InitializeWeaponComponents()
    {
        // 确保有WeaponTrigger组件
        if (weaponTrigger == null)
        {
            weaponTrigger = GetComponent<WeaponTrigger>();
            if (weaponTrigger == null)
            {
                weaponTrigger = gameObject.AddComponent<WeaponTrigger>();
            }
        }

        // 确保有Rigidbody2D组件
        weaponRB = GetComponent<Rigidbody2D>();
        if (weaponRB == null)
        {
            weaponRB = gameObject.AddComponent<Rigidbody2D>();
        }

        // 确保有BoxCollider2D触发器
        weaponCollider = GetComponent<BoxCollider2D>();
        if (weaponCollider == null)
        {
            weaponCollider = gameObject.AddComponent<BoxCollider2D>();
        }
        weaponCollider.isTrigger = true;

        // 查找或创建Muzzle
        if (muzzle == null)
        {
            muzzle = transform.Find("Muzzle");
            if (muzzle == null)
            {
                GameObject muzzleObj = new GameObject("Muzzle");
                muzzle = muzzleObj.transform;
                muzzle.SetParent(transform);
                muzzle.localPosition = new Vector3(1f, 0f, 0f); // 设置在武器前端
            }
        }

        // 配置WeaponTrigger
        if (weaponTrigger != null)
        {
            weaponTrigger.Muzzle = muzzle;
            weaponTrigger.ShootInterval = fireRate;
            weaponTrigger.spreadAngle = spreadAngle;
        }
    }

    // 设置武器标签
    private void SetupWeaponTag()
    {
        // 安全设置标签：若未定义将捕获异常并跳过
        try
        {
            if (!gameObject.CompareTag("Weapon"))
            {
                gameObject.tag = "Weapon";
            }
        }
        catch (System.Exception)
        {
            Debug.LogWarning("[WeaponManager] Tag 'Weapon' 未在项目中定义，已跳过设置（不影响功能）");
        }
    }

    // 检测当前持有者并配置相应的子弹池
    public void DetectOwner()
    {
        Transform parent = transform.parent;

        if (parent != null)
        {
            // 检查是否为玩家持有
            Player player = parent.GetComponentInParent<Player>();
            if (player != null)
            {
                SetOwner(player.transform, true);
                return;
            }

            // 检查是否为敌人持有
            Enemy enemy = parent.GetComponentInParent<Enemy>();
            if (enemy != null)
            {
                SetOwner(enemy.transform, false);
                return;
            }
        }

        // 如果没有持有者，设置为掉落状态
        SetAsDroppedWeapon();
    }

    /// <summary>
    /// 设置武器持有者
    /// </summary>
    /// <param name="owner">持有者Transform</param>
    /// <param name="isPlayer">是否为玩家</param>
    public void SetOwner(Transform owner, bool isPlayer)
    {
        currentOwner = owner;
        isPlayerWeapon = isPlayer;

        // 根据持有者配置子弹池（使用单例）
        if (isPlayer)
        {
            ConfigureForPlayer();
        }
        else
        {
            ConfigureForEnemy();
        }

        Debug.Log($"武器 {weaponName} 被 {(isPlayer ? "玩家" : "敌人")} {owner.name} 持有");
    }

    // 配置为玩家武器
    private void ConfigureForPlayer()
    {
        if (weaponTrigger != null)
        {
            // 使用单例模式的子弹池
            weaponTrigger.SetBulletPool(BulletPool.Instance, true);
        }

        // 设置物理属性
        if (weaponRB != null)
        {
            weaponRB.isKinematic = true;
            weaponRB.gravityScale = 0;
        }

        // 禁用碰撞器
        if (weaponCollider != null)
        {
            weaponCollider.enabled = false;
        }
    }

    // 配置为敌人武器
    private void ConfigureForEnemy()
    {
        if (weaponTrigger != null)
        {
            // 使用单例模式的子弹池
            weaponTrigger.SetBulletPool(EnemyBulletPool.Instance, false);
        }

        // 设置物理属性
        if (weaponRB != null)
        {
            weaponRB.isKinematic = true;
            weaponRB.gravityScale = 0;
        }

        // 禁用碰撞器
        if (weaponCollider != null)
        {
            weaponCollider.enabled = false;
        }
    }

    // 设置为掉落武器状态
    public void SetAsDroppedWeapon()
    {
        currentOwner = null;
        isPlayerWeapon = false;

        // 停止射击
        SetFiring(false);
        // 若正在换弹，取消协程
        CancelReloadIfRunning();

        // 设置物理属性
        if (weaponRB != null)
        {
            weaponRB.isKinematic = false;
            weaponRB.gravityScale = 0; // 2D游戏通常不需要重力
        }

        // 启用碰撞器用于拾取检测
        if (weaponCollider != null)
        {
            weaponCollider.enabled = true;
        }

        Debug.Log($"武器 {weaponName} 已掉落");
    }

    /// <summary>
    /// 设置射击状态
    /// </summary>
    /// <param name="firing">是否射击</param>
    public void SetFiring(bool firing)
    {
        // 若对象未激活或组件禁用，则忽略任何开火指令并确保触发器关闭
        if (!gameObject.activeInHierarchy || !enabled)
        {
            isFiring = false;
            if (weaponTrigger != null) weaponTrigger.SetFiring(false);
            return;
        }

        // 半自动：去抖，仅在“按下沿”触发单发
        if (fireMode == FireMode.SemiAuto)
        {
            if (firing)
            {
                FireSingle();
            }
            isFiring = false;
            if (weaponTrigger != null) weaponTrigger.SetFiring(false);
            return;
        }

        // 全自动：允许长按连发，同时透传给 WeaponTrigger（兼容旧触发）
        isFiring = firing;
        if (weaponTrigger != null) weaponTrigger.SetFiring(firing);
    }

    // 事件化接口（音效/特效/UI 可订阅）
    public System.Action<WeaponManager> OnBeforeFire;
    public System.Action<WeaponManager> OnFired;
    public System.Action<WeaponManager> OnReloadStart;
    public System.Action<WeaponManager> OnReloadEnd;

    // 射击方法
    private void Fire()
    {
        // 检查是否有弹药
        if (currentAmmo <= 0 || isReloading)
        {
            Debug.Log($"武器 {weaponName} 无弹药或正在换弹，无法射击");
            return;
        }

        if (weaponTrigger == null || muzzle == null) return;

        // 根据武器持有者获取正确的子弹预制体
        GameObject correctBulletPrefab = GetCorrectBulletPrefab(isPlayerWeapon);

        if (correctBulletPrefab == null)
        {
            Debug.LogError($"武器 {weaponName} 无法获取正确的子弹预制体");
            return;
        }

        // 事件：开火前
        OnBeforeFire?.Invoke(this);

        // 通过 WeaponTrigger 从对象池取弹并摆放到枪口（统一路径）
        GameObject bullet = weaponTrigger != null ? weaponTrigger.SpawnFromPool(correctBulletPrefab) : null;

        if (bullet != null)
        {
            // 消耗弹药
            currentAmmo--;

            // 配置子弹组件
            Bullet bulletComponent = bullet.GetComponent<Bullet>();
            if (bulletComponent != null)
            {
                bulletComponent.StartPos = muzzle.position;
                bulletComponent.shooter = currentOwner;
                bulletComponent.BulletSpeed = bulletSpeed;
                bulletComponent.Range = range;
                bulletComponent.Damage = damage;
                bulletComponent.enabled = true;

                // 设置子弹速度
                Rigidbody2D bulletRB = bullet.GetComponent<Rigidbody2D>();
                if (bulletRB != null)
                {
                    bulletRB.velocity = bullet.transform.right * bulletSpeed;
                }
            }

            OnFired?.Invoke(this);
            Debug.Log($"[{weaponName}] 发射者: {(currentOwner ? currentOwner.name : "无持有者")} 使用子弹: {correctBulletPrefab.name} (标签: {correctBulletPrefab.tag}) 剩余弹药: {currentAmmo}");
        }
    }

    // 换弹方法
    public void StartReload()
    {
        if (isReloading || currentAmmo >= magazineCapacity)
        {
            return;
        }

			// 玩家武器：仅当玩家背包/挂具/口袋中存在匹配弹药时才允许换弹
			if (isPlayerWeapon)
			{
				if (!PlayerHasCompatibleAmmo())
				{
					Debug.LogWarning($"武器 {weaponName} 换弹被拒：未在玩家背包/挂具/口袋中找到匹配弹药");
					return;
				}
			}
        // 如果物体未激活，不启动协程，直接忽略以防错误
        if (!isActiveAndEnabled || !gameObject.activeInHierarchy)
        {
            Debug.LogWarning($"武器 {weaponName} 处于未激活状态，忽略换弹请求");
            return;
        }

        StartCoroutine(ReloadCoroutine());
    }

	/// <summary>
	/// 检查玩家装备网格（背包容器、挂具容器、口袋 PlayerItemGrid）中是否存在与本武器匹配的弹药
	/// 规则：匹配 JSON 中武器的 ammunitionOptions（字符串数组，与弹药物品名称一致），且对应弹药 currentStack > 0
	/// </summary>
	private bool PlayerHasCompatibleAmmo()
	{
		// 若没有数据对象或无可用弹药列表，则认为不可换弹（防止误判）
		var options = itemData != null ? itemData.ammunitionOptions : null;
		if (options == null || options.Length == 0)
		{
			Debug.LogWarning($"[AmmoCheck] 武器 {weaponName} 缺少 ammunitionOptions 配置");
			return false;
		}

		var optionSet = new HashSet<string>(options);
		foreach (var grid in GetPlayerAmmoRelevantGrids())
		{
			if (grid == null) continue;
			var readers = grid.GetComponentsInChildren<ItemDataReader>(true);
			foreach (var r in readers)
			{
				if (r == null || r.ItemData == null) continue;
				if (r.ItemData.category != ItemCategory.Ammunition) continue;
				if (r.CurrentStack <= 0) continue;
				// 名称匹配（与 JSON 中的 Ammunition 名称一致）
				if (optionSet.Contains(r.ItemData.itemName))
				{
					return true;
				}
			}
		}
		return false;
	}

	/// <summary>
	/// 获取玩家与弹药相关的所有网格：背包容器网格、挂具容器网格、口袋网格（GridType.Player）
	/// </summary>
	private IEnumerable<ItemGrid> GetPlayerAmmoRelevantGrids()
	{
		// 1) 装备槽中的容器：背包、挂具
		var mgr = EquipmentSlotManager.Instance;
		if (mgr != null)
		{
			var backpack = mgr.GetEquipmentSlot(EquipmentSlotType.Backpack);
			if (backpack != null && backpack.ContainerGrid != null)
				yield return backpack.ContainerGrid;
			var rig = mgr.GetEquipmentSlot(EquipmentSlotType.TacticalRig);
			if (rig != null && rig.ContainerGrid != null)
				yield return rig.ContainerGrid;
		}

		// 2) 口袋网格：类型为 Player 的 ItemGrid
		ItemGrid pocket = null;
		try
		{
			pocket = GameObject.Find("PlayerItemGrid")?.GetComponent<ItemGrid>();
		}
		catch { }
		if (pocket == null)
		{
			pocket = Object.FindObjectsOfType<ItemGrid>(true).FirstOrDefault(g => g.GridType == GridType.Player);
		}
		if (pocket != null)
			yield return pocket;
	}

    private System.Collections.IEnumerator ReloadCoroutine()
    {
        isReloading = true;
        reloadStartTime = Time.time; // 记录换弹开始时间
        Debug.Log($"武器 {weaponName} 开始换弹，换弹时间: {reloadTime}秒");
        OnReloadStart?.Invoke(this);

        yield return new WaitForSeconds(reloadTime);

        // 计算实际可装填数量：受库存与缺弹差额限制
        int needed = Mathf.Max(0, magazineCapacity - currentAmmo);
        int available = GetTotalCompatibleAmmoCount();
        int toLoad = Mathf.Min(needed, available);

        if (toLoad <= 0)
        {
            isReloading = false;
            Debug.LogWarning($"武器 {weaponName} 换弹结束但无可用弹药，当前弹药: {currentAmmo}/{magazineCapacity}");
            OnReloadEnd?.Invoke(this);
            yield break;
        }

        // 从玩家库存扣除弹药堆叠并装填
        int deducted = DeductAmmoFromInventory(toLoad);
        currentAmmo = Mathf.Clamp(currentAmmo + deducted, 0, magazineCapacity);
        isReloading = false;
        Debug.Log($"武器 {weaponName} 换弹完成，装入: {deducted}，当前弹药: {currentAmmo}/{magazineCapacity}");
        OnReloadEnd?.Invoke(this);
    }

    /// <summary>
    /// 统计玩家库存中与本武器匹配的弹药总数（所有堆叠求和）
    /// </summary>
    private int GetTotalCompatibleAmmoCount()
    {
        var readers = GetCompatibleAmmoReadersOrdered();
        int sum = 0;
        foreach (var r in readers)
        {
            if (r != null) sum += Mathf.Max(0, r.CurrentStack);
        }
        return sum;
    }

    /// <summary>
    /// 扣除指定数量的弹药，按发现顺序从多个堆叠中扣除；返回实际扣除量
    /// </summary>
    private int DeductAmmoFromInventory(int amount)
    {
        int remaining = Mathf.Max(0, amount);
        if (remaining == 0) return 0;

        int removed = 0;
        var readers = GetCompatibleAmmoReadersOrdered();
        foreach (var r in readers)
        {
            if (r == null || r.ItemData == null || r.CurrentStack <= 0) continue;

            int take = Mathf.Min(r.CurrentStack, remaining);
            if (take > 0)
            {
                r.RemoveStack(take);
                r.SaveRuntimeToES3();
                removed += take;
                remaining -= take;

                // 若该堆叠耗尽，销毁物体
                if (r.CurrentStack <= 0)
                {
                    try
                    {
                        var go = r.gameObject;
                        if (go != null) Destroy(go);
                    }
                    catch { }
                }

                if (remaining <= 0) break;
            }
        }
        return removed;
    }

    /// <summary>
    /// 获取按优先顺序排列的匹配弹药读取器列表（背包容器→挂具容器→口袋）
    /// </summary>
    private List<ItemDataReader> GetCompatibleAmmoReadersOrdered()
    {
        var result = new List<ItemDataReader>();
        var options = itemData != null ? itemData.ammunitionOptions : null;
        if (options == null || options.Length == 0) return result;
        var optionSet = new HashSet<string>(options);

        foreach (var grid in GetPlayerAmmoRelevantGrids())
        {
            if (grid == null) continue;
            var readers = grid.GetComponentsInChildren<ItemDataReader>(true);
            foreach (var r in readers)
            {
                if (r == null || r.ItemData == null) continue;
                if (r.ItemData.category != ItemCategory.Ammunition) continue;
                if (r.CurrentStack <= 0) continue;
                if (!optionSet.Contains(r.ItemData.itemName)) continue;
                result.Add(r);
            }
        }
        return result;
    }

    // 取消换弹（若在进行中）
    public void CancelReloadIfRunning()
    {
        if (isReloading)
        {
            StopAllCoroutines();
            isReloading = false;
        }
    }

    // 检查是否需要换弹
    public bool NeedsReload()
    {
        return currentAmmo <= 0 && !isReloading;
    }

    // 检查是否可以射击
    public bool CanFire()
    {
        return currentAmmo > 0 && !isReloading;
    }

    // 获取弹药信息
    public int GetCurrentAmmo() => currentAmmo;
    public int GetMagazineCapacity() => magazineCapacity;
    public float GetReloadTime() => reloadTime;
    public bool IsReloading() => isReloading;

    // 新增：获取玩家子弹预制体
    public GameObject GetPlayerBulletPrefab() => playerBulletPrefab;

    // 新增：获取敌人子弹预制体
    public GameObject GetEnemyBulletPrefab() => enemyBulletPrefab;

    // 新增：设置玩家子弹预制体
    public void SetPlayerBulletPrefab(GameObject newPlayerBulletPrefab)
    {
        if (newPlayerBulletPrefab != null && !HasTag(newPlayerBulletPrefab, "PlayerBullets"))
        {
            Debug.LogWarning($"设置的玩家子弹预制体 {newPlayerBulletPrefab.name} 没有 'PlayerBullets' 标签");
        }
        playerBulletPrefab = newPlayerBulletPrefab;
        Debug.Log($"武器 {weaponName} 的玩家子弹预制体已更改为: {(playerBulletPrefab ? playerBulletPrefab.name : "无")}");
    }

    // 新增：设置敌人子弹预制体
    public void SetEnemyBulletPrefab(GameObject newEnemyBulletPrefab)
    {
        if (newEnemyBulletPrefab != null && !HasTag(newEnemyBulletPrefab, "EnemyBullets"))
        {
            Debug.LogWarning($"设置的敌人子弹预制体 {newEnemyBulletPrefab.name} 没有 'EnemyBullets' 标签");
        }
        enemyBulletPrefab = newEnemyBulletPrefab;
        Debug.Log($"武器 {weaponName} 的敌人子弹预制体已更改为: {(enemyBulletPrefab ? enemyBulletPrefab.name : "无")}");
    }

    // 新增：获取武器的子弹预制体（向后兼容）
    public GameObject GetBulletPrefab() => bulletPrefab;

    // 新增：设置武器的子弹预制体（向后兼容）
    public void SetBulletPrefab(GameObject newBulletPrefab)
    {
        bulletPrefab = newBulletPrefab;
        Debug.Log($"武器 {weaponName} 的通用子弹预制体已更改为: {(bulletPrefab ? bulletPrefab.name : "无")}");
    }

    // 获取武器属性
    public float GetFireRate() => fireRate;
    public float GetDamage() => damage;
    public float GetRange() => range;
    public string GetWeaponName() => weaponName;
    public bool IsPlayerWeapon() => isPlayerWeapon;
    public Transform GetCurrentOwner() => currentOwner;

    // 当武器被拾取时调用
    public void OnPickedUp(Transform newOwner)
    {
        bool isPlayer = newOwner.GetComponent<Player>() != null;
        SetOwner(newOwner, isPlayer);
    }

    // 当武器被丢弃时调用
    public void OnDropped()
    {
        SetAsDroppedWeapon();
    }

    public float GetReloadProgress()
    {
        if (!isReloading) return 1f;

        float elapsedTime = Time.time - reloadStartTime;
        return Mathf.Clamp01(elapsedTime / reloadTime);
    }

    // 单次开火的方法
    public void FireSingle()
    {
        // 检查是否可以射击
        if (!CanFire())
        {
            Debug.Log($"武器 {weaponName} 无法射击：弹药不足或正在换弹");
            return;
        }
        
        // 检查射击间隔
        if (Time.time < nextFireTime)
        {
            return;
        }
        
        // 执行射击
        Fire();
        
        // 更新下次射击时间
        nextFireTime = Time.time + fireRate;
    }
}
