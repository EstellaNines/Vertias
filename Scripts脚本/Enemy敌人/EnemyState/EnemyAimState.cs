using UnityEngine;

public class EnemyAimState : IState
{
    // --- 敌人瞄准状态 ---
    Enemy enemy;
    private float aimTime = 0f;
    private float maxAimTime = 0.05f; // 瞄准时间，可以调整这个值来控制瞄准速度
    private float cooldownTime = 0.5f; // 射击冷却时间
    private float cooldownTimer = 0f; // 冷却计时器
    private bool inCooldown = false; // 是否在冷却中

    // --- 构造函数 --- 
    public EnemyAimState(Enemy enemy)
    {
        this.enemy = enemy;
    }

    // --- 进入状态 ---
    public void OnEnter()
    {
        // 停止移动
        if (enemy.RB != null)
        {
            enemy.RB.velocity = Vector2.zero;
        }

        // 播放待机动画
        if (enemy.animator != null)
        {
            enemy.animator.Play("Idle");
        }

        // 重置瞄准时间
        aimTime = 0f;

        // 检查攻击状态是否在重新装弹
        EnemyAttackState attackState = GetAttackState();
        if (attackState != null && attackState.IsReloading())
        {
            inCooldown = true;
            cooldownTimer = 0f;
            cooldownTime = 1.5f; // 重装弹冷却时间
        }
        else if (attackState != null && attackState.GetShotsFired() >= 30)
        {
            inCooldown = true;
            cooldownTimer = 0f;
            cooldownTime = 0.5f; // 射击冷却时间
        }
    }

    public void OnExit()
    {
        // 清理瞄准状态
    }

    public void OnFixedUpdate()
    {
        // 物理更新
    }

    public void OnUpdate()
    {
        // 优先检查死亡状态 - 最高优先级
        if (enemy.isDead)
        {
            enemy.transitionState(EnemyState.Dead);
            return;
        }

        if (enemy.isHurt)
        {
            enemy.transitionState(EnemyState.Hurt); // 受伤状态
        }

        // 检查玩家是否死亡
        if (enemy.IsPlayerDead())
        {
            Debug.Log("玩家已死亡，敌人停止攻击");
            enemy.shouldPatrol = true;
            enemy.transitionState(EnemyState.Patrol);
            return;
        }

        // 检查玩家是否还在攻击范围内
        if (!IsPlayerInAttackRange())
        {
            Debug.Log("玩家不在攻击范围内，敌人停止攻击");
            enemy.shouldPatrol = true;
            enemy.transitionState(EnemyState.Patrol);
            return;
        }

        // 检查玩家是否被检测到或者玩家是否在蹲伏状态
        if (!enemy.IsPlayerDetected() || enemy.IsPlayerCrouching())
        {
            enemy.shouldPatrol = true; // 重新开始巡逻状态
            enemy.transitionState(EnemyState.Patrol);
            return;
        }

        // 处理射击冷却，如果在冷却中则等待
        if (inCooldown)
        {
            cooldownTimer += Time.deltaTime;
            if (cooldownTimer >= cooldownTime)
            {
                // 冷却结束，重置射击计数器
                EnemyAttackState attackState = GetAttackState();
                if (attackState != null)
                {
                    attackState.ResetShotsFired();
                }
                inCooldown = false;
            }
            return; // 在冷却期间不进行瞄准
        }

        // 瞄准玩家
        AimAtPlayer();

        // 瞄准计时，达到时间后切换到攻击状态
        aimTime += Time.deltaTime;
        if (aimTime >= maxAimTime)
        {
            enemy.transitionState(EnemyState.Attack);
        }
    }

    // 瞄准玩家
    private void AimAtPlayer()
    {
        if (enemy.player == null) return;

        // 计算玩家方向
        Vector2 playerPosition = enemy.GetPlayerPosition();
        Vector2 direction = (playerPosition - (Vector2)enemy.transform.position).normalized;

        // 调用Enemy的SetDirection方法来设置方向
        enemy.SetDirection(direction);
    }

    // 获取攻击状态
    private EnemyAttackState GetAttackState()
    {
        if (enemy != null && enemy.states != null && enemy.states.TryGetValue(EnemyState.Attack, out IState state))
        {
            return state as EnemyAttackState;
        }
        return null;
    }

    // 检查玩家是否在攻击范围内
    private bool IsPlayerInAttackRange()
    {
        if (enemy.player == null) return false;

        Vector2 enemyPosition = enemy.eyePoint ? (Vector2)enemy.eyePoint.transform.position : (Vector2)enemy.transform.position;
        Vector2 playerPosition = enemy.player.transform.position;
        float distanceToPlayer = Vector2.Distance(enemyPosition, playerPosition);

        // 检查玩家是否在检测范围内
        return distanceToPlayer <= enemy.playerDetectionRadius;
    }
}