using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float BulletSpeed;
    public int BulletDamage;
    public float BulletDistance;
    public Vector3 StartPos;

    private Rigidbody2D RB2D;

    void Awake()
    {
        RB2D = GetComponent<Rigidbody2D>();
    }

    void OnEnable()
    {
        // 重置子弹状态
        RB2D.velocity = transform.right * BulletSpeed;
        StartPos = transform.position;
    }

    void Update()
    {
        BulletFire();
    }

    void BulletFire()
    {
        // 计算子弹与初始位置的距离
        float distance = (transform.position - StartPos).sqrMagnitude;

        // 如果距离超过射程，返回对象池
        if (distance > BulletDistance * BulletDistance)
        {
            BulletPool bulletPool = FindObjectOfType<BulletPool>();
            if (bulletPool != null)
            {
                bulletPool.ReturnBullet(gameObject);
            }
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        // 优先尝试获取 ZombieHealth
        ZombieHealth zombieHealth = collision.GetComponent<ZombieHealth>();
        if (zombieHealth != null)
        {
            Debug.Log($"[子弹命中] 敌人: {collision.name} | 伤害: {BulletDamage}", this);
            zombieHealth.TakeDamage(BulletDamage);
        }
        else
        {
            // 回退到 Zombie（兼容旧逻辑）
            Zombie zombie = collision.GetComponent<Zombie>();
            if (zombie != null)
            {
                Debug.Log($"[子弹命中] 兼容模式敌人: {collision.name} | 伤害: {BulletDamage}", this);
                zombie.TakeDamage(BulletDamage);
            }
            else
            {
                Debug.LogWarning($"[无命中] 碰撞到非敌人对象: {collision.name}", this);
            }
        }

        // 回收子弹（保持原有逻辑）
        BulletPool bulletPool = FindObjectOfType<BulletPool>();
        if (bulletPool != null)
        {
            bulletPool.ReturnBullet(gameObject);
        }
    }
}