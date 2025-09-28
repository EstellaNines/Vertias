using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyHurtState : IState
{
    // --- 控制器引用 ---
    Enemy enemy;
    
    // --- 受伤状态参数 ---
    private float hurtDuration = 1f; // 受伤状态持续时间
    private float hurtTimer = 0f; // 受伤计时器
    
    // --- 构造函数 --- 
    public EnemyHurtState(Enemy enemy)
    {
        this.enemy = enemy;
    }
    
    // --- 状态方法 ---
    public void OnEnter()
    {
        Debug.Log($"敌人 {enemy.gameObject.name} 进入受伤状态");
        
        // 播放受伤动画
        enemy.animator.Play("Hurt");
        
        // 停止移动
        enemy.RB.velocity = Vector2.zero;
        
        // 重置受伤计时器
        hurtTimer = 0f;
    }

    public void OnUpdate()
    {
        // 检查是否死亡
        if (enemy.isDead)
        {
            enemy.transitionState(EnemyState.Dead);
            return;
        }
        
        // 更新受伤计时器
        hurtTimer += Time.deltaTime;
        
        // 检查受伤动画是否播放完毕
        AnimatorStateInfo stateInfo = enemy.animator.GetCurrentAnimatorStateInfo(0);
        bool animationFinished = stateInfo.IsName("Hurt") && stateInfo.normalizedTime >= 0.95f;
        
        // 如果受伤时间结束或动画播放完毕，退出受伤状态
        if (hurtTimer >= hurtDuration || animationFinished)
        {
            if (enemy.IsPlayerDetected() && !enemy.IsPlayerCrouching())
                enemy.transitionState(EnemyState.Aim);
            else
                enemy.transitionState(EnemyState.Patrol);
        }
    }

    public void OnFixedUpdate()
    {
        // 受伤状态下保持静止
        enemy.RB.velocity = Vector2.zero;
    }

    public void OnExit()
    {
        Debug.Log($"敌人 {enemy.gameObject.name} 退出受伤状态");
        
        // 重置受伤标记
        enemy.isHurt = false;
    }
}
