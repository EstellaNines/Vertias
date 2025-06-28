using UnityEngine;

public class EnemyIdleState : IState
{
// --- 控制器引用 ---
    Enemy enemy;
    private float idleTimer = 0f; // 待机计时器
    // --- 构造函数 --- 
    public EnemyIdleState(Enemy enemy)
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
        
        // 重置待机计时器
        idleTimer = 0f;
    }

    public void OnExit()
    {
        // 退出待机状态时的清理工作
    }

    public void OnFixedUpdate()
    {
        // 物理更新
    }

    public void OnUpdate()
    {
        // 检测玩家 - 提高优先级
        if (enemy.IsPlayerDetected() && !enemy.IsPlayerCrouching())
        {
            // 如果检测到玩家且玩家不在潜行状态，立即切换到瞄准状态
            enemy.transitionState(EnemyState.Aim);
            return;
        }
        
        // 更新待机计时器
        idleTimer += Time.deltaTime;
        
        // 待机时间结束后切换到巡逻状态
        if (idleTimer >= enemy.idleTime)
        {
            enemy.shouldPatrol = true; // 设置可以继续巡逻
            enemy.transitionState(EnemyState.Patrol);
        }
    }
}
