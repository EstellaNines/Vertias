using UnityEngine;

public class EnemyAimState : IState
{
    // --- 控制器引用 ---
    Enemy enemy;
    private float aimTime = 0f;
    private float maxAimTime = 0.05f; // 瞄准时间从0.5秒减少到0.2秒，提高反应速度
    private float cooldownTime = 0.5f; // 攻击后的冷却时间
    private float cooldownTimer = 0f; // 冷却计时器
    private bool inCooldown = false; // 是否在冷却中
    
    // --- 构造函数 --- 
    public EnemyAimState(Enemy enemy)
    {
        this.enemy = enemy;
    }
    
    // --- 状态方法 ---
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
        
        // 检查是否需要进入冷却
        EnemyAttackState attackState = GetAttackState();
        if (attackState != null && attackState.GetShotsFired() >= 30)
        {
            inCooldown = true;
            cooldownTimer = 0f;
        }
    }

    public void OnExit()
    {
        // 退出瞄准状态
    }

    public void OnFixedUpdate()
    {
        // 物理更新
    }

    public void OnUpdate()
    {
        // 检查是否死亡 - 最高优先级
        if (enemy.isDead)
        {
            enemy.transitionState(EnemyState.Dead);
            return;
        }
        
        if (enemy.isHurt)
        {
            enemy.transitionState(EnemyState.Hurt); // 进入受伤状态
        }

        // 检查玩家是否已死亡
        if (enemy.IsPlayerDead())
        {
            Debug.Log("玩家已死亡，敌人停止瞄准");
            enemy.shouldPatrol = true;
            enemy.transitionState(EnemyState.Patrol);
            return;
        }
        
        // 如果玩家不再被检测到或者进入潜行状态，返回巡逻状态
        if (!enemy.IsPlayerDetected() || enemy.IsPlayerCrouching())
        {
            enemy.shouldPatrol = true; // 设置可以继续巡逻
            enemy.transitionState(EnemyState.Patrol);
            return;
        }
        
        // 如果在冷却中，处理冷却逻辑
        if (inCooldown)
        {
            cooldownTimer += Time.deltaTime;
            if (cooldownTimer >= cooldownTime)
            {
                // 冷却结束，重置射击计数并退出冷却状态
                EnemyAttackState attackState = GetAttackState();
                if (attackState != null)
                {
                    attackState.ResetShotsFired();
                }
                inCooldown = false;
            }
            return; // 在冷却中不进行瞄准和攻击
        }
        
        // 瞄准玩家
        AimAtPlayer();
        
        // 瞄准一段时间后切换到攻击状态
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
        
        // 计算朝向玩家的方向
        Vector2 playerPosition = enemy.GetPlayerPosition();
        Vector2 direction = (playerPosition - (Vector2)enemy.transform.position).normalized;
        
        // 使用Enemy类中的SetDirection方法设置方向
        enemy.SetDirection(direction);
    }
    
    // 获取攻击状态组件
    private EnemyAttackState GetAttackState()
    {
        if (enemy != null && enemy.states != null && enemy.states.TryGetValue(EnemyState.Attack, out IState state))
        {
            return state as EnemyAttackState;
        }
        return null;
    }
}