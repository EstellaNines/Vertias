using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHurtState : IState
{
    // --- 获取玩家引用 ---
    public Player player;
    private AnimatorStateInfo animInfo; // 动画状态信息
    private bool animationStarted = false; // 动画是否已开始
    private float hurtWalkSpeed = 0.5f; // 受伤时的行走速度
    private string currentHurtAnimation = ""; // 当前播放的受伤动画名称
    
    // 构造函数
    public PlayerHurtState(Player player)
    {
        this.player = player;
    }
    
    public void OnEnter()
    {
        Debug.Log("进入玩家受伤状态");
        
        // 设置动画器参数
        player.AIMTOR.SetBool("isHurting?", true);
        
        // 根据当前输入状态和武器状态决定播放哪个动画
        UpdateHurtAnimation();
        
        animationStarted = true;
    }

    public void OnExit()
    {
        Debug.Log("退出受伤状态");
        
        // 重置动画器参数
        player.AIMTOR.SetBool("isHurting?", false);
        
        player.EndHurtState();
        animationStarted = false;
    }

    public void OnFixedUpdate()
    {
        // 处理受伤状态下的移动
        HandleHurtMovement();
    }

    public void OnUpdate()
    {
        // 检查死亡状态 - 优先检查isDead标志
        if (player.isDead)
        {
            player.transitionState(PlayerStateType.Die);
            return;
        }
        
        // 额外检查：如果生命值归0但isDead未设置，强制设置死亡状态
        if (player.CurrentHealth <= 0)
        {
            player.isDead = true;
            // 设置动画器参数触发死亡动画
            player.AIMTOR.SetBool("isDying?", true);
            player.transitionState(PlayerStateType.Die);
            return;
        }
        
        // 根据输入状态更新受伤动画
        UpdateHurtAnimation();
        
        // 检查动画是否播放完成
        if (animationStarted)
        {
            animInfo = player.AIMTOR.GetCurrentAnimatorStateInfo(0);
            
            // 当动画播放完成时，返回对应状态
            if (animInfo.normalizedTime >= 1.0f && 
                (animInfo.IsName("Hurt") || animInfo.IsName("Shoot_Hurt") || 
                 animInfo.IsName("HurtWalk") || animInfo.IsName("Shoot_HurtWalk")))
            {
                // 根据当前状态决定下一个状态
                if (player.InputDirection != Vector2.zero)
                {
                    player.transitionState(PlayerStateType.Move);
                }
                else
                {
                    player.transitionState(PlayerStateType.Idle);
                }
            }
        }
    }
    
    // 根据输入状态和武器状态更新受伤动画
    private void UpdateHurtAnimation()
    {
        string targetAnimation = "";
        
        // 根据输入状态和武器状态决定动画
        if (player.InputDirection != Vector2.zero)
        {
            // 有输入 - 播放行走受伤动画
            if (player.HasWeaponInHand())
            {
                targetAnimation = "Shoot_HurtWalk"; // 持武器行走受伤
            }
            else
            {
                targetAnimation = "HurtWalk"; // 无武器行走受伤
            }
        }
        else
        {
            // 无输入 - 播放待机受伤动画
            if (player.HasWeaponInHand())
            {
                targetAnimation = "Shoot_Hurt"; // 持武器待机受伤
            }
            else
            {
                targetAnimation = "Hurt"; // 无武器待机受伤
            }
        }
        
        // 只有当目标动画与当前动画不同时才切换
        if (targetAnimation != currentHurtAnimation)
        {
            currentHurtAnimation = targetAnimation;
            player.AIMTOR.Play(currentHurtAnimation);
            Debug.Log($"播放受伤动画: {currentHurtAnimation}");
        }
    }
    
    // 处理受伤状态下的移动逻辑
    private void HandleHurtMovement()
    {
        if (player.PlayerRB2D != null)
        {
            // 如果有输入，以受伤速度移动；否则停止
            if (player.InputDirection != Vector2.zero)
            {
                player.PlayerRB2D.velocity = player.InputDirection * hurtWalkSpeed;
                
                // 更新视角方向
                player.UpdateLookDirection();
                
                // 更新速度动画参数（基于受伤行走速度）
                float speedMagnitude = player.InputDirection.sqrMagnitude * (hurtWalkSpeed / player.WalkSpeed);
                player.AIMTOR.SetFloat("Speed", speedMagnitude);
            }
            else
            {
                // 无输入时停止移动
                player.PlayerRB2D.velocity = Vector2.zero;
                player.AIMTOR.SetFloat("Speed", 0f);
            }
        }
    }
}