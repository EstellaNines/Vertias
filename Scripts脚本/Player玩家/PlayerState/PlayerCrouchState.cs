using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCrouchState : IState
{
    // --- 获取玩家组件 ---
    public Player player;
    
    // 构造函数
    public PlayerCrouchState(Player player)
    {
        this.player = player;
    }
    
    public void OnEnter()
    {
        // 应用潜行视觉效果
        player.ApplyCrouchVisual();
        
        if (player.isWeaponInHand)
        {
            player.AIMTOR.Play("Shoot_Walk"); 
        }
        else
        {
            player.AIMTOR.Play("Walk"); 
        }
        
        Debug.Log("进入潜行状态");
    }

    public void OnExit()
    {
        // 恢复原始视觉效果
        player.RestoreOriginalVisual();
        Debug.Log("退出潜行状态");
    }

    public void OnFixedUpdate()
    {
        // 潜行时可能需要降低移动速度
        float crouchSpeed = player.WalkSpeed * 0.5f; // 潜行速度为行走速度的一半
        player.PlayerRB2D.velocity = player.InputDirection * crouchSpeed;
    }

    public void OnUpdate()
    {
        // 视角变化始终存在
        player.UpdateLookDirection();
        
        // 潜行状态下的特殊逻辑
        if (player.isCrouching)
        {
        }
        
        // 拾取切换
        if (player.isPickingUp)
        {
            player.transitionState(PlayerStateType.PickUp);
            return;
        }
        
        // 如果松开潜行键，根据当前输入状态切换
        if (!player.isCrouching)
        {
            if (player.InputDirection != Vector2.zero)
            {
                if (player.isRunning)
                {
                    player.transitionState(PlayerStateType.Run);
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
        
        // 闪避切换（潜行时也可以闪避）
        if (player.isDodged)
        {
            player.transitionState(PlayerStateType.Dodge);
            return;
        }
    }
}