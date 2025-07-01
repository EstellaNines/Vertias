using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerRunState : IState
{
    // --- 玩家跑步状态类 ---
    public Player player;
    // 构造函数
    public PlayerRunState(Player player)
    {
        this.player = player;
    }
    
    public void OnEnter()
    {
        // 设置玩家当前速度为跑步速度
        player.CurrentSpeed = player.RunSpeed;
        player.AIMTOR.SetFloat("Speed", player.CurrentSpeed); // 设置动画器速度参数
        
        // 根据是否持有武器播放对应的跑步动画
        if (player.isWeaponInHand)
        {
            player.AIMTOR.Play("Shoot_Run");
        }
        else
        {
            player.AIMTOR.Play("Run");
        }
    }

    public void OnExit()
    {
        // 退出时恢复为行走速度
        player.CurrentSpeed = player.WalkSpeed;
    }

    public void OnFixedUpdate()
    {
        // 在物理更新中处理玩家移动
        player.PlayerRB2D.velocity = player.InputDirection * player.CurrentSpeed;
    }

    public void OnUpdate()
    {
        // 基础瞄准功能（视角和瞄准方向更新）
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
        // 受伤
        if(player.isHurt)
        {
            player.transitionState(PlayerStateType.Hurt);
        }
        
        // 检测是否停止跑步，如果停止则切换到移动或待机状态
        if (!player.isRunning)
        {
            if (player.InputDirection != Vector2.zero)
            {
                player.transitionState(PlayerStateType.Move); // 切换到移动状态
            }
            else
            {
                player.transitionState(PlayerStateType.Idle); // 切换到待机状态
            }
            return;
        }
        
        // 如果没有输入方向则切换到待机状态
        if (player.InputDirection == Vector2.zero)
        {
            player.transitionState(PlayerStateType.Idle);
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