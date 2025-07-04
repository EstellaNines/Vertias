using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAttackState : IState
{
    // --- 获取玩家组件 ---
    public Player player;
    
    // 构造函数
    public PlayerAttackState(Player player)
    {
        this.player = player;
    }
    
    public void OnEnter()
    {
        // 设置攻击时的移动速度
        player.CurrentSpeed = player.FireSpeed;
        
        // 播放射击动画
        if (player.isWeaponInHand)
        {
            player.AIMTOR.Play("Shoot_Idle");
            
            // 验证当前武器控制器
            if (player.currentWeaponController != null)
            {
                Debug.Log($"进入攻击状态 - 当前武器: {player.currentWeaponController.GetWeaponName()}");
            }
            else
            {
                Debug.LogWarning("进入攻击状态但武器控制器未正确设置！");
            }
        }
        
        Debug.Log("进入攻击状态");
    }

    public void OnExit()
    {
        // 恢复正常移动速度
        player.CurrentSpeed = player.WalkSpeed;
        
        // 停止射击
        player.isFiring = false;
        player.isAttacking = false;
        
        // 确保武器停止射击
        if (player.currentWeaponController != null)
        {
            player.currentWeaponController.SetFiring(false);
        }
        
        Debug.Log("退出攻击状态");
    }

    public void OnFixedUpdate()
    {
        // 攻击状态下的物理更新
        // 可以移动但速度较慢
        player.PlayerRB2D.velocity = player.InputDirection * player.CurrentSpeed;
    }

    public void OnUpdate()
    {
        // 完整瞄准功能（包括武器朝向）
        player.UpdateFullAiming();
        
        // 检查武器弹药
        if (player.currentWeaponController != null && player.currentWeaponController.NeedsReload())
        {
            Debug.Log("弹药用尽，自动切换到换弹状态");
            player.transitionState(PlayerStateType.Reload);
            return;
        }
        
        // 处理射击动画
        if (player.isFiring && player.isWeaponInHand)
        {
            // 验证武器控制器
            if (player.currentWeaponController == null)
            {
                Debug.LogError("攻击状态下武器控制器丢失，退出攻击状态");
                player.transitionState(PlayerStateType.Idle);
                return;
            }
            
            // 检查是否可以继续射击
            if (!player.currentWeaponController.CanFire())
            {
                Debug.Log("无法继续射击，退出攻击状态");
                player.transitionState(PlayerStateType.Idle);
                return;
            }
            
            // 播放射击动画
            if (player.InputDirection != Vector2.zero)
            {
                player.AIMTOR.Play("Shoot_Walk");
            }
            else
            {
                player.AIMTOR.Play("Shoot_Idle");
            }
        }
        
        // 拾取切换（攻击时也可以拾取）
        if (player.isPickingUp)
        {
            player.transitionState(PlayerStateType.PickUp);
            return;
        }
        // 受伤
        if(player.isHurt)
        {
            player.transitionState(PlayerStateType.Hurt);
        }
        // 闪避切换（攻击时也可以闪避）
        if (player.isDodged)
        {
            player.transitionState(PlayerStateType.Dodge);
            return;
        }
        
        // 如果停止射击，根据当前状态切换
        if (!player.isFiring || !player.isWeaponInHand)
        {
            if (player.InputDirection != Vector2.zero)
            {
                if (player.isRunning)
                {
                    player.transitionState(PlayerStateType.Run);
                }
                else if (player.isCrouching)
                {
                    player.transitionState(PlayerStateType.Crouch);
                }
                else
                {
                    player.transitionState(PlayerStateType.Move);
                }
            }
            else
            {
                player.transitionState(PlayerStateType.Idle);
            }
            return;
        }
    }
}
