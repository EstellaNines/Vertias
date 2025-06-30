using UnityEngine;

public class WeaponTrigger : MonoBehaviour
{
    // 子弹发射口位置
    public Transform Muzzle;

    // 子弹池
    public BulletPool bulletPool;

    // 是否正在射击标志
    private bool isFiring;

    // 射击间隔时间
    public float ShootInterval;

    // 计时器，用于控制射击频率
    private float Timer;

    //散布角度字段
    public float spreadAngle = 5f;

    // 外部设置射击状态的接口
    public void SetFiring(bool firing)
    {
        isFiring = firing;
    }

    // 每帧调用一次
    void Update()
    {
        // 计时器递增
        Timer += Time.deltaTime;

        // 如果正在射击且计时器达到射击间隔
        if (isFiring && Timer >= ShootInterval)
        {
            // 重置计时器
            Timer = 0;

            // 执行射击
            Fire();
        }
    }

    // 射击逻辑
    private void Fire()
    {
        // 从池中获取一个子弹
        GameObject bulletObj = bulletPool.GetBullet();
        if (bulletObj == null) return;

        // 设置子弹位置和旋转
        bulletObj.transform.position = Muzzle.position;
        bulletObj.transform.rotation = Muzzle.rotation;

        // 添加随机散布偏移
        float randomAngle = Random.Range(-spreadAngle, spreadAngle);
        bulletObj.transform.rotation = Muzzle.rotation * Quaternion.Euler(0, 0, randomAngle);

        // 获取子弹组件
        Bullet bullet = bulletObj.GetComponent<Bullet>();
        if (bullet != null)
        {
            // 设置子弹的初始位置
            bullet.StartPos = Muzzle.position;

            // 设置子弹发射者为玩家
            bullet.shooter = this.transform.parent; // 武器的父对象（玩家）
            Debug.Log($"[玩家开火] 发射者: {bullet.shooter.name}");

            // 设置子弹的运动方向
            Rigidbody2D rb = bulletObj.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.velocity = bulletObj.transform.right * bullet.BulletSpeed;
            }
        }

        // 启用子弹
        bulletObj.SetActive(true);
    }
}