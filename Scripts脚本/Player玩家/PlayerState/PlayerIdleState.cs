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
        player.AIMTOR.Play("Idle");
    }

    public void OnExit()
    {

    }

    public void OnFixedUpdate()
    {

    }

    public void OnUpdate()
    {
        // 更新视角方向
        player.UpdateLookDirection();
        // 移动切换
        if (player.InputDirection != Vector2.zero)
        {
            player.transitionState(PlayerStateType.Move); // 切换移动状态
        }
        // 翻滚切换
        if (player.isDodged)
        {
            player.transitionState(PlayerStateType.Dodge); // 翻滚切换
        
        }
    }
}