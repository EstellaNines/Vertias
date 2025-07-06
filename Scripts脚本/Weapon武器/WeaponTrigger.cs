using UnityEngine;

public class WeaponTrigger : MonoBehaviour
{
    // 子弹发射点
    public Transform Muzzle;

    // 当前使用的是否为玩家子弹池
    private bool usePlayerPool = true;

    // 是否正在射击
    private bool isFiring;

    // 射击间隔时间
    public float ShootInterval;

    // 射击计时器，用于控制射击间隔
    private float Timer;

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

    // 设置是否开始射击的方法
    public void SetFiring(bool firing)
    {
        isFiring = firing;
    }

    // 每帧更新方法
    void Update()
    {
        // 更新计时器
        Timer += Time.deltaTime;

        // 如果正在射击且达到射击间隔时间
        if (isFiring && Timer >= ShootInterval)
        {
            // 重置计时器
            Timer = 0;

            // 发射子弹
            Fire();
        }
    }

    // 发射子弹方法
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

        // 获取子弹组件
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
}