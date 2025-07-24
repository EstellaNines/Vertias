using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponManager : MonoBehaviour
{
    [Header("武器基础属性")]
    [FieldLabel("武器名称")] public string weaponName = "默认武器";
    [FieldLabel("射击间隔")] public float fireRate = 0.33f;
    [FieldLabel("子弹散射角度")] public float spreadAngle = 5f;
    [FieldLabel("子弹速度")] public float bulletSpeed = 10f;
    [FieldLabel("射程")] public float range = 15f;
    [FieldLabel("伤害")] public float damage = 25f;

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
    private float nextFireTime = 0f;
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
    }

    private void Start()
    {
        // 检测初始持有者
        DetectOwner();
    }

    private void Update()
    {
        // 更新射击逻辑
        if (isFiring && Time.time >= nextFireTime)
        {
            Fire();
            nextFireTime = Time.time + fireRate;
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
        if (!gameObject.CompareTag("Weapon"))
        {
            gameObject.tag = "Weapon";
        }

        // 确保有ItemBase组件用于拾取
        if (GetComponent<ItemBase>() == null)
        {
            gameObject.AddComponent<ItemBase>();
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
        isFiring = firing;

        // 同时设置WeaponTrigger的射击状态
        if (weaponTrigger != null)
        {
            weaponTrigger.SetFiring(firing);
        }
    }

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

        // 使用单例模式的子弹池
        GameObject bullet = null;

        if (isPlayerWeapon && BulletPool.Instance != null)
        {
            // 确保使用带有PlayerBullets标签的子弹预制体
            if (HasTag(correctBulletPrefab, "PlayerBullets"))
            {
                bullet = BulletPool.Instance.GetBullet(correctBulletPrefab);
            }
            else
            {
                Debug.LogError($"尝试在玩家子弹池中使用非玩家子弹预制体: {correctBulletPrefab.name}");
                return;
            }
        }
        else if (!isPlayerWeapon && EnemyBulletPool.Instance != null)
        {
            // 确保使用带有EnemyBullets标签的子弹预制体
            if (HasTag(correctBulletPrefab, "EnemyBullets"))
            {
                bullet = EnemyBulletPool.Instance.GetBullet(correctBulletPrefab, muzzle.position, muzzle.rotation);
            }
            else
            {
                Debug.LogError($"尝试在敌人子弹池中使用非敌人子弹预制体: {correctBulletPrefab.name}");
                return;
            }
        }
        else
        {
            Debug.LogError($"无法获取 {(isPlayerWeapon ? "玩家" : "敌人")} 子弹池单例");
            return;
        }

        if (bullet != null)
        {
            // 消耗弹药
            currentAmmo--;

            // 设置子弹位置和旋转
            bullet.transform.position = muzzle.position;
            bullet.transform.rotation = muzzle.rotation;

            // 添加散射
            float randomAngle = Random.Range(-spreadAngle, spreadAngle);
            bullet.transform.rotation = muzzle.rotation * Quaternion.Euler(0, 0, randomAngle);

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

            bullet.SetActive(true);
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

        StartCoroutine(ReloadCoroutine());
    }

    private System.Collections.IEnumerator ReloadCoroutine()
    {
        isReloading = true;
        reloadStartTime = Time.time; // 记录换弹开始时间
        Debug.Log($"武器 {weaponName} 开始换弹，换弹时间: {reloadTime}秒");

        yield return new WaitForSeconds(reloadTime);

        currentAmmo = magazineCapacity;
        isReloading = false;
        Debug.Log($"武器 {weaponName} 换弹完成，当前弹药: {currentAmmo}");
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
