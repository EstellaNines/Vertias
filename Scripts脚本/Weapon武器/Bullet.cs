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
    // 从对象池取出时由发射端写入，用于快速回收归还，避免运行时查找
    public System.Action<GameObject> ReturnToPool;
    
    // 添加属性以兼容WeaponManager
    public float Range 
    { 
        get { return BulletDistance; } 
        set { BulletDistance = value; } 
    }
    
    public float Damage 
    { 
        get { return BulletDamage; } 
        set { BulletDamage = (int)value; } 
    }

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
            if (ReturnToPool != null)
            {
                ReturnToPool(gameObject);
            }
            else
            {
                // 回退逻辑：根据标签选择回收池，避免Find代价
                if (CompareTag("EnemyBullets"))
                {
                    if (EnemyBulletPool.Instance != null) EnemyBulletPool.Instance.ReturnBullet(gameObject);
                }
                else
                {
                    if (BulletPool.Instance != null) BulletPool.Instance.ReturnBullet(gameObject);
                }
            }
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        // 修改碰撞体检测条件：排除所有触发器（isTrigger = true 的碰撞体）
        if (!collision.isTrigger)
        {
            // 优先使用回调归还
            if (ReturnToPool != null)
            {
                ReturnToPool(gameObject);
                return;
            }
            BulletPool bulletPool = BulletPool.Instance;
            
            // 检查是否击中玩家
            Player player = collision.GetComponent<Player>();
            if (player != null && shooter != player.transform) // 确保不是自己的子弹
            {
                Debug.Log($"[子弹命中] 玩家受到伤害: {BulletDamage}", this);
                
                // 调用玩家的受伤处理方法
                player.TakeDamage(BulletDamage);
                
                if (bulletPool != null) bulletPool.ReturnBullet(gameObject);
                return;
            }
            
            // 检查是否击中Enemy（不需要击退）
            Enemy enemy = collision.GetComponent<Enemy>();
            if (enemy != null)
            {
                Debug.Log($"[子弹命中] 敌人: {collision.name} | 伤害: {BulletDamage} | 来源: 玩家", this);
                
                // 调用Enemy的受伤处理方法（不带击退参数）
                enemy.TakeDamage(BulletDamage);
                
                if (bulletPool != null) bulletPool.ReturnBullet(gameObject);
                return;
            }
            
            // 检查是否击中Zombie（需要击退）
            Zombie zombie = collision.GetComponent<Zombie>();
            if (zombie != null)
            {
                Debug.Log($"[子弹命中] 僵尸: {collision.name} | 伤害: {BulletDamage} | 来源: 玩家", this);
    
                // 获取子弹当前运动方向作为击退方向
                Vector2 hitDirection = RB2D.velocity.normalized;
                // 调用Zombie的受伤处理方法（带击退参数）
                zombie.TakeDamage(BulletDamage, hitDirection);
                
                if (bulletPool != null)
                {
                    bulletPool.ReturnBullet(gameObject);
                }
                return;
            }
            
            // 如果都没有命中，记录警告
            Debug.LogWarning($"[无命中] 碰撞到非目标对象: {collision.name}", this);
    
            if (ReturnToPool != null) ReturnToPool(gameObject);
            else if (bulletPool != null) bulletPool.ReturnBullet(gameObject);
        }
    }
}