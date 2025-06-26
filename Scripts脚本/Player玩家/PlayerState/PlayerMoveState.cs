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
        player.AIMTOR.Play("Walk");
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
        // 更新视角方向
        player.UpdateLookDirection();

        // 当没有输入或速度接近静止时切换到待机状态
        if (player.InputDirection == Vector2.zero || player.CurrentSpeed < 0.1f)
        {
            player.transitionState(PlayerStateType.Idle);
        }
        // 翻滚切换
        if (player.isDodged)
        {
            player.transitionState(PlayerStateType.Dodge); // 翻滚切换

        }
    }
}