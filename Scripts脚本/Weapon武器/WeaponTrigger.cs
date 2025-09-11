using UnityEngine;

public class WeaponTrigger : MonoBehaviour
{
    // 子弹发射点
    public Transform Muzzle;

    // 兼容旧逻辑：如需使用本组件内部计时与连发，请开启；默认关闭，由 WeaponManager 统一控制
    public bool enableLegacyFiring = false;

    // 当前使用的是否为玩家子弹池
    private bool usePlayerPool = true;

    // 旧通路: 状态/计时
    private bool isFiring;            // 仅在 enableLegacyFiring=true 时使用
    public float ShootInterval;       // 由 WeaponManager 同步
    private float legacyTimer;        // 仅在 enableLegacyFiring=true 时使用

    // 子弹散射角度
    public float spreadAngle = 5f;

    // 设置子弹池类型（玩家）
    public void SetBulletPool(BulletPool playerPool, bool isPlayer)
    {
        usePlayerPool = isPlayer;
        Debug.Log($"武器触发器设置为使用 {(isPlayer ? "玩家" : "敌人")} 子弹池");
    }
    
    // 设置子弹池类型（敌人）
    public void SetBulletPool(EnemyBulletPool enemyPool, bool isPlayer)
    {
        usePlayerPool = isPlayer;
        Debug.Log($"武器触发器设置为使用 {(isPlayer ? "玩家" : "敌人")} 子弹池");
    }

    // 设置是否开始射击的方法（默认不驱动旧连发逻辑）
    public void SetFiring(bool firing)
    {
        isFiring = enableLegacyFiring ? firing : false;
        if (!gameObject.activeInHierarchy || !enabled)
        {
            isFiring = false;
        }
    }

    // 每帧更新方法（仅在启用兼容旧逻辑时生效）
    void Update()
    {
        if (!enableLegacyFiring) return;
        if (!gameObject.activeInHierarchy || !enabled) { isFiring = false; return; }

        legacyTimer += Time.deltaTime;

        // 如果正在射击且达到射击间隔时间
        if (isFiring && legacyTimer >= ShootInterval)
        {
            // 重置计时器
            legacyTimer = 0f;

            // 发射子弹（旧路径）
            Fire();
        }
    }

    // 发射子弹方法（旧通路）
    private void Fire()
    {
        GameObject bulletObj = null;
        
        // 根据当前设置选择合适的子弹池（使用单例）
        if (usePlayerPool && BulletPool.Instance != null)
        {
            bulletObj = BulletPool.Instance.GetBullet();
        }
        else if (!usePlayerPool && EnemyBulletPool.Instance != null)
        {
            bulletObj = EnemyBulletPool.Instance.GetBullet(Muzzle.position, Muzzle.rotation);
        }
        
        if (bulletObj == null) 
        {
            Debug.LogWarning($"无法从 {(usePlayerPool ? "玩家" : "敌人")} 子弹池获取子弹");
            return;
        }

        // 设置子弹位置和旋转
        bulletObj.transform.position = Muzzle.position;
        bulletObj.transform.rotation = Muzzle.rotation;

        // 添加随机散射角度
        float randomAngle = Random.Range(-spreadAngle, spreadAngle);
        bulletObj.transform.rotation = Muzzle.rotation * Quaternion.Euler(0, 0, randomAngle);

        // 获取子弹组件并写入回收回调（由池写入，这里兜底）
        Bullet bullet = bulletObj.GetComponent<Bullet>();
        if (bullet != null)
        {
            // 设置子弹起始位置
            bullet.StartPos = Muzzle.position;

            // 设置子弹发射者为武器的父对象
            bullet.shooter = this.transform.parent; // 武器的父对象通常是玩家或敌人
            Debug.Log($"[武器发射] 发射者: {bullet.shooter.name}");

            // 设置子弹速度
            Rigidbody2D rb = bulletObj.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.velocity = bulletObj.transform.right * bullet.BulletSpeed;
            }
        }

        // 激活子弹对象
        bulletObj.SetActive(true);
    }

    // 统一从对象池取弹并放置于枪口（供 WeaponManager 调用）
    public GameObject SpawnFromPool(GameObject prefab)
    {
        if (!gameObject.activeInHierarchy || !enabled || Muzzle == null) return null;
        GameObject bulletObj = null;

        // 根据当前设置选择合适的子弹池（使用单例）
        if (usePlayerPool && BulletPool.Instance != null)
        {
            bulletObj = BulletPool.Instance.GetBullet(prefab);
        }
        else if (!usePlayerPool && EnemyBulletPool.Instance != null)
        {
            bulletObj = EnemyBulletPool.Instance.GetBullet(prefab, Muzzle.position, Muzzle.rotation);
        }

        if (bulletObj == null)
        {
            Debug.LogWarning($"无法从 {(usePlayerPool ? "玩家" : "敌人")} 子弹池获取子弹（prefab: {(prefab ? prefab.name : "null")}）");
            return null;
        }

        // 设置子弹位置和旋转，并添加随机散射角度
        bulletObj.transform.position = Muzzle.position;
        float randomAngle = Random.Range(-spreadAngle, spreadAngle);
        bulletObj.transform.rotation = Muzzle.rotation * Quaternion.Euler(0, 0, randomAngle);

        bulletObj.SetActive(true);
        return bulletObj;
    }
}