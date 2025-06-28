using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;

public class EnemyMoveState : IState
{
    // --- 控制器引用 ---
    Enemy enemy;
    private float nextPathUpdate = 0f;
    private Vector2 targetPosition;
    
    // --- 构造函数 --- 
    public EnemyMoveState(Enemy enemy)
    {
        this.enemy = enemy;
    }
    
    // --- 状态方法 ---
    public void OnEnter()
    {
        // 播放移动动画（如果有）
        if (enemy.animator != null)
        {
            enemy.animator.Play("Move"); // 播放移动动画
        }
        
        // 设置目标位置为玩家位置
        targetPosition = enemy.GetPlayerPosition();
        
        // 立即更新路径
        nextPathUpdate = 0f;
    }

    public void OnExit()
    {
        // 停止移动
        if (enemy.RB != null)
        {
            enemy.RB.velocity = Vector2.zero;
        }
        
        // 停止移动动画
        // if (enemy.animator != null)
        // {
        //     enemy.animator.SetBool("IsMoving", false);
        // }
    }

    public void OnFixedUpdate()
    {
        // 沿着路径移动
        FollowPath();
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
        else if (!enemy.IsPlayerDetected() || enemy.IsPlayerCrouching())
        {
            // 如果玩家不再被检测到或者进入潜行状态，返回巡逻状态
            enemy.transitionState(EnemyState.Patrol);
            return;
        }
        
        // 更新目标位置为玩家位置
        targetPosition = enemy.GetPlayerPosition();
        
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
        if (enemy.seeker == null) return;
        
        // 计算新路径
        enemy.seeker.StartPath(enemy.transform.position, targetPosition, OnPathComplete);
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