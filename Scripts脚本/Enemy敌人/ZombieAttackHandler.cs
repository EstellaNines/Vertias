
using UnityEngine;

public class ZombieAttackHandler : MonoBehaviour
{
    [Header("攻击参数")]
    [SerializeField] private LayerMask playerLayer; // 玩家层过滤
    [SerializeField] private float attackCooldown = 1.0f; // 攻击冷却时间
    [SerializeField] private float damage = 20f; // 伤害值

    private float lastAttackTime = 0f; // 上次攻击时间
    private BoxCollider2D attackCollider;

    private void Awake()
    {
        attackCollider = GetComponent<BoxCollider2D>();
        attackCollider.isTrigger = true; // 确保是触发器
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        // 检查是否是玩家层
        if (((1 << other.gameObject.layer) & playerLayer) != 0)
        {
            // 检查冷却时间
            if (Time.time >= lastAttackTime + attackCooldown)
            {
                lastAttackTime = Time.time;
                AttackPlayer(other);
            }
        }
    }

    private void AttackPlayer(Collider2D playerCollider)
    {
        // 触发攻击事件
        PlayerHurt playerHurt = playerCollider.GetComponent<PlayerHurt>();
        if (playerHurt != null)
        {
            playerHurt.Hurt();  // 触发玩家受伤效果
        }
    }
}
