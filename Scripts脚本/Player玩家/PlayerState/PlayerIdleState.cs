using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerIdleState : IState
{
    // --- Íæ¼Ò´ý»ú×´Ì¬Àà ---
    public Player player;
    // ¹¹Ôìº¯Êý
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
        // 视角状态更新
        player.UpdateBasicAiming();
        
        // 拾取
        if (player.isPickingUp)
        {
            player.transitionState(PlayerStateType.PickUp);
            return;
        }
        // 攻击
        if (player.isFiring && player.isWeaponInHand)
        {
            player.transitionState(PlayerStateType.Attack);
            return;
        }
        
        // 受伤
        if(player.isHurt)
        {
            player.transitionState(PlayerStateType.Hurt);
        }
        // 跑动&行走
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
        // 潜行
        if (player.isCrouching)
        {
            player.transitionState(PlayerStateType.Crouch);
            return;
        }
        
        // 闪避
        if (player.isDodged)
        {
            player.transitionState(PlayerStateType.Dodge);
            return;
        }
    }
}