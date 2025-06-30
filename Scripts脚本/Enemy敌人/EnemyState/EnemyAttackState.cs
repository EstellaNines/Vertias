using UnityEngine;

public class EnemyAttackState : IState
{
    // --- 控制器引用 ---
    Enemy enemy;
    private float attackTime = 0f;
    private float maxAttackTime = 5f; // 攻击持续时间从3秒增加到5秒
    private int shotsFired = 0; // 已发射子弹数量
    private int maxShots = 30; // 最大发射子弹数量
    private float lastShotTime = 0f; // 上次射击时间
    
    // --- 构造函数 --- 
    public EnemyAttackState(Enemy enemy)
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
        
        // 重置攻击时间
        attackTime = 0f;
        // 不重置shotsFired，让它在状态切换之间保持
    }

    public void OnExit()
    {
        // 退出攻击状态
    }

    public void OnFixedUpdate()
    {
        // 物理更新
    }

    public void OnUpdate()
    {
        // 检查玩家是否已死亡
        if (enemy.IsPlayerDead())
        {
            Debug.Log("玩家已死亡，敌人停止攻击");
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
        
        // 瞄准玩家
        AimAtPlayer();
        
        // 射击，并计数
        if (Time.time >= enemy.nextFireTime && shotsFired < maxShots)
        {
            enemy.Shoot();
            shotsFired++;
            lastShotTime = Time.time;
        }
        
        // 攻击一段时间后或达到最大射击次数后切换回瞄准状态
        attackTime += Time.deltaTime;
        if (attackTime >= maxAttackTime || shotsFired >= maxShots)
        {
            enemy.transitionState(EnemyState.Aim);
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
    
    // 获取已发射子弹数量
    public int GetShotsFired()
    {
        return shotsFired;
    }
    
    // 重置已发射子弹数量
    public void ResetShotsFired()
    {
        shotsFired = 0;
    }
}