using System.Collections;
using UnityEngine;
using Pathfinding;

public class EnemyPatrolState : IState
{
    // --- 控制器引用 ---
    Enemy enemy;
    private float nextPathUpdate = 0f;
    private Coroutine updatePathRoutine;
    
    // --- 构造函数 --- 
    public EnemyPatrolState(Enemy enemy)
    {
        this.enemy = enemy;
    }
    
    // --- 状态方法 ---
    public void OnEnter()
    {
        // 检查是否有巡逻点
        if (enemy.PatrolPoints == null || enemy.PatrolPoints.Length == 0)
        {
            Debug.LogWarning("没有设置巡逻点，敌人将保持待机状态");
            enemy.transitionState(EnemyState.Idle);
            return;
        }
        
        // 如果不应该巡逻，直接切换到待机状态
        if (!enemy.shouldPatrol)
        {
            enemy.transitionState(EnemyState.Idle);
            return;
        }
        
        // 开始更新路径
        nextPathUpdate = 0f; // 立即更新路径
        
        // 播放移动动画
        if (enemy.animator != null)
        {
            enemy.animator.Play("Walk");
        }
    }

    public void OnExit()
    {
        // 停止路径更新
        if (enemy.RB != null)
        {
            enemy.RB.velocity = Vector2.zero;
        }
    }

    public void OnFixedUpdate()
    {
        // 沿着路径移动
        FollowPath();
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

        // 检测玩家
        if (enemy.IsPlayerDetected() && !enemy.IsPlayerCrouching())
        {
            enemy.transitionState(EnemyState.Aim);
            return;
        }
        
        // 更新路径
        if (Time.time >= nextPathUpdate)
        {
            nextPathUpdate = Time.time + enemy.pathUpdateInterval;
            UpdatePath();
        }
    }
    
    // 更新路径
    private void UpdatePath()
    {
        if (enemy.seeker == null || enemy.PatrolPoints == null || enemy.PatrolPoints.Length == 0)
            return;
            
        // 获取当前目标点
        Transform target = enemy.PatrolPoints[enemy.currentPatrolIndex];
        if (target == null) return;
        
        // 计算新路径
        enemy.seeker.StartPath(enemy.transform.position, target.position, OnPathComplete);
    }
    
    // 路径计算完成回调
    private void OnPathComplete(Path p)
    {
        if (!p.error)
        {
            enemy.currentPath = p;
            enemy.currentWaypoint = 0;
        }
    }
    
    // 沿着路径移动
    private void FollowPath()
    {
        if (enemy.currentPath == null || enemy.RB == null)
            return;
            
        // 检查是否到达路径终点
        if (enemy.currentWaypoint >= enemy.currentPath.vectorPath.Count)
        {
            enemy.reachedEndOfPath = true;
            
            // 到达当前巡逻点，切换到下一个巡逻点并进入待机状态
            enemy.currentPatrolIndex = (enemy.currentPatrolIndex + 1) % enemy.PatrolPoints.Length;
            enemy.shouldPatrol = false; // 设置不应该继续巡逻，等待待机结束
            enemy.transitionState(EnemyState.Idle); // 切换到待机状态
            return;
        }
        else
        {
            enemy.reachedEndOfPath = false;
        }
        
        // 计算移动方向
        Vector2 direction = ((Vector2)enemy.currentPath.vectorPath[enemy.currentWaypoint] - (Vector2)enemy.transform.position).normalized;
        Vector2 velocity = direction * enemy.MoveSpeed;
        
        // 应用移动
        enemy.RB.velocity = velocity;
        
        // 更新敌人朝向 - 直接使用Enemy类的SetDirection方法
        enemy.SetDirection(direction);
        
        // 根据移动方向设置动画器的Horizontal参数
        if (enemy.animator != null)
        {
            // 向左走时Horizontal=0，向右走时Horizontal=1
            float horizontalValue = direction.x < 0 ? 0 : 1;
            enemy.animator.SetFloat("Horizontal", horizontalValue);
        }
        
        // 检查是否到达当前路径点
        float distance = Vector2.Distance((Vector2)enemy.transform.position, (Vector2)enemy.currentPath.vectorPath[enemy.currentWaypoint]);
        if (distance < enemy.reachDistance)
        {
            enemy.currentWaypoint++;
        }
    }
}