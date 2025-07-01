using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMoveState : IState
{
    // --- ???? ---
    public Player player;
    // ????
    public PlayerMoveState(Player player)
    {
        this.player = player;
    }
    public void OnEnter()
    {
        if (player.isWeaponInHand)
        {
            player.AIMTOR.Play("Shoot_Walk");
        }
        else
        {
            player.AIMTOR.Play("Walk");
        }
    }

    public void OnExit()
    {

    }

    public void OnFixedUpdate()
    {
        // 物理更新行动速度
        player.PlayerRB2D.velocity = player.InputDirection * player.CurrentSpeed;
    }

    public void OnUpdate()
    {
        // 基础瞄准功能（视角和瞄准方向更新）
        player.UpdateBasicAiming();
        
        // 拾取
        if (player.isPickingUp)
        {
            player.transitionState(PlayerStateType.PickUp);
            return;
        }
        // 射击
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

        // 跑动
        if (player.isRunning && player.InputDirection != Vector2.zero)
        {
            player.transitionState(PlayerStateType.Run);
            return;
        }
        
        // 待机
        if (player.InputDirection == Vector2.zero || player.CurrentSpeed < 0.1f)
        {
            player.transitionState(PlayerStateType.Idle);
            return;
        }
        // 潜行
        if (player.isCrouching)
        {
            player.transitionState(PlayerStateType.Crouch);
            return;
        }
        // 翻滚
        if (player.isDodged)
        {
            player.transitionState(PlayerStateType.Dodge);
            return;
        }
    }
}