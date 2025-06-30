using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("子弹属性")]
    public float BulletSpeed;
    public int BulletDamage;
    public float BulletDistance;
    public Vector3 StartPos;

    public Transform shooter; // 发射者
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

    // 添加Initialize方法，用于Enemy类调用
    public void Initialize(Vector2 direction, float speed)
    {
        // 设置子弹速度和方向
        BulletSpeed = speed;
        RB2D.velocity = direction * BulletSpeed;
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
        // 修改碰撞体检测条件：排除所有触发器（isTrigger = true 的碰撞体）
        if (!collision.isTrigger)
        {
            BulletPool bulletPool = FindObjectOfType<BulletPool>(); // 在方法开始时声明一次
            
            // 检查是否击中玩家
            Player player = collision.GetComponent<Player>();
            if (player != null && shooter != player.transform) // 确保不是玩家自己的子弹
            {
                Debug.Log($"[子弹命中] 玩家受到伤害: {BulletDamage}", this);
                player.TakeDamage(BulletDamage);
                
                if (bulletPool != null)
                {
                    bulletPool.ReturnBullet(gameObject);
                }
                return;
            }
            
            // 检查是否击中敌人
            Zombie zombie = collision.GetComponent<Zombie>();
            if (zombie != null)
            {
                Debug.Log($"[子弹命中] 敌人: {collision.name} | 伤害: {BulletDamage} | 来源: 玩家", this);
    
                // 获取子弹当前运动方向作为击退方向
                Vector2 hitDirection = RB2D.velocity.normalized;
                // 调用伤害处理方法
                zombie.TakeDamage(BulletDamage, hitDirection);
            }
            else
            {
                Debug.LogWarning($"[无命中] 碰撞到非敌人对象: {collision.name}", this);
            }
    
            if (bulletPool != null)
            {
                bulletPool.ReturnBullet(gameObject);
            }
        }
    }
}