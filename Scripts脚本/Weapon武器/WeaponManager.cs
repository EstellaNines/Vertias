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

    [Header("武器组件")]
    [FieldLabel("枪口位置")] public Transform muzzle;
    [FieldLabel("武器触发器")] public WeaponTrigger weaponTrigger;

    [Header("子弹池配置")]
    [FieldLabel("玩家子弹池")] public BulletPool playerBulletPool;
    [FieldLabel("敌人子弹池")] public EnemyBulletPool enemyBulletPool;

    // 内部状态
    private Transform currentOwner; // 当前持有者
    private bool isPlayerWeapon = false; // 是否为玩家武器
    private float nextFireTime = 0f;
    private Rigidbody2D weaponRB;
    private Collider2D weaponCollider;

    // 射击状态
    private bool isFiring = false;
    // private float fireTimer = 0f;

    private void Awake()
    {
        InitializeWeaponComponents();
        SetupWeaponTag();
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

        // 根据持有者配置子弹池
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
        if (weaponTrigger != null && playerBulletPool != null)
        {
            weaponTrigger.SetBulletPool(playerBulletPool, true);
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
        if (weaponTrigger != null && enemyBulletPool != null)
        {
            weaponTrigger.SetBulletPool(enemyBulletPool, false);
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
        if (weaponTrigger == null || muzzle == null) return;

        // 使用当前配置的子弹池
        GameObject bullet = null;

        if (isPlayerWeapon && playerBulletPool != null)
        {
            bullet = playerBulletPool.GetBullet();
        }
        else if (!isPlayerWeapon && enemyBulletPool != null)
        {
            bullet = enemyBulletPool.GetBullet(muzzle.position, muzzle.rotation);
        }

        if (bullet != null)
        {
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
            Debug.Log($"[{weaponName}] 发射者: {(currentOwner ? currentOwner.name : "无持有者")}");
        }
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
}
