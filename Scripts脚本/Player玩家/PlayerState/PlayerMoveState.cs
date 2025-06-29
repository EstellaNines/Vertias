using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMoveState : IState
{
    // --- 获取玩家组件 ---
    public Player player;
    // 构造函数
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
        // 仅当有输入时应用速度
        player.PlayerRB2D.velocity = player.InputDirection * player.CurrentSpeed;
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
        
        // 如果开启跑步状态，切换到跑步状态
        if (player.isRunning && player.InputDirection != Vector2.zero)
        {
            player.transitionState(PlayerStateType.Run);
            return;
        }
        
        // 当没有输入或速度接近静止时切换到待机状态
        if (player.InputDirection == Vector2.zero || player.CurrentSpeed < 0.1f)
        {
            player.transitionState(PlayerStateType.Idle);
            return;
        }
        if (player.isCrouching)
        {
            player.transitionState(PlayerStateType.Crouch);
            return;
        }
        // 闪避切换
        if (player.isDodged)
        {
            player.transitionState(PlayerStateType.Dodge);
            return;
        }
    }
}