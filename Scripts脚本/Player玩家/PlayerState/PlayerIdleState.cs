using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerIdleState : IState
{
    // --- 玩家待机状态类 ---
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
        // 基础瞄准功能（仅视角更新）
        player.UpdateBasicAiming();
        
        // 拾取状态检测
        if (player.isPickingUp)
        {
            player.transitionState(PlayerStateType.PickUp);
            return;
        }
        // 射击状态检测
        if (player.isFiring && player.isWeaponInHand)
        {
            player.transitionState(PlayerStateType.Attack);
            return;
        }
        
        // 移动状态检测 - 根据是否按住跑步键决定状态
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
        
        // 闪避状态检测
        if (player.isDodged)
        {
            player.transitionState(PlayerStateType.Dodge);
            return;
        }
    }
}