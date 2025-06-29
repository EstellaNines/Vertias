using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerIdleState : IState
{
    // --- 获取玩家组件 ---
    public Player player;
    // 构造函数
    public PlayerIdleState(Player player)
    {
        this.player = player;
    }
    public void OnEnter()
    {
        if (player.isWeaponInHand)
        {
            player.AIMTOR.Play("Shoot_Idle");
        }
        else
        {
            player.AIMTOR.Play("Idle");
        }
    }

    public void OnExit()
    {

    }

    public void OnFixedUpdate()
    {

    }

    public void OnUpdate()
    {
        // 视角变化始终存在
        player.UpdateLookDirection();
        
        // 拾取切换
        if (player.isPickingUp)
        {
            player.transitionState(PlayerStateType.PickUp);
            return;
        }
        
        // 移动切换 - 根据是否跑步决定状态
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
            return;
        }
        if (player.isCrouching)
        {
            player.transitionState(PlayerStateType.Crouch);
            return;
        }
        
        // 翻滚切换
        if (player.isDodged)
        {
            player.transitionState(PlayerStateType.Dodge);
            return;
        }
    }
}