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
        // 忽略与发射者自身或其子对象的碰撞
        if (shooter != null)
        {
            if (collision.transform == shooter || collision.transform.IsChildOf(shooter)) return;
        }

        BulletPool bulletPool = BulletPool.Instance;
        EnemyBulletPool enemyBulletPool = EnemyBulletPool.Instance;

        bool isPlayerBullet = CompareTag("PlayerBullets") || (shooter != null && shooter.GetComponentInParent<Player>() != null);
        bool isEnemyBullet = CompareTag("EnemyBullets") || (shooter != null && (shooter.GetComponentInParent<Enemy>() != null || shooter.GetComponentInParent<Zombie>() != null));

        // 敌人子弹命中玩家
        if (isEnemyBullet)
        {
            Player player = collision.GetComponentInParent<Player>();
            if (player != null)
            {
                player.TakeDamage(BulletDamage);
                if (ReturnToPool != null)
                {
                    ReturnToPool(gameObject);
                }
                else
                {
                    if (enemyBulletPool != null) enemyBulletPool.ReturnBullet(gameObject);
                    else if (bulletPool != null) bulletPool.ReturnBullet(gameObject);
                }
                return;
            }
        }

        // 玩家子弹命中敌人/僵尸
        if (isPlayerBullet)
        {
            Enemy enemy = collision.GetComponentInParent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(BulletDamage);
                if (ReturnToPool != null)
                {
                    ReturnToPool(gameObject);
                }
                else
                {
                    if (bulletPool != null) bulletPool.ReturnBullet(gameObject);
                }
                return;
            }

            Zombie zombie = collision.GetComponentInParent<Zombie>();
            if (zombie != null)
            {
                Vector2 hitDirection = (RB2D != null && RB2D.velocity.sqrMagnitude > 0.0001f) ? RB2D.velocity.normalized : (Vector2)transform.right;
                zombie.TakeDamage(BulletDamage, hitDirection);
                if (ReturnToPool != null)
                {
                    ReturnToPool(gameObject);
                }
                else
                {
                    if (bulletPool != null) bulletPool.ReturnBullet(gameObject);
                }
                return;
            }
        }

        // 非目标命中：直接回收
        if (ReturnToPool != null)
        {
            ReturnToPool(gameObject);
        }
        else
        {
            if (CompareTag("EnemyBullets"))
            {
                if (enemyBulletPool != null) enemyBulletPool.ReturnBullet(gameObject);
            }
            else
            {
                if (bulletPool != null) bulletPool.ReturnBullet(gameObject);
            }
        }
    }
}