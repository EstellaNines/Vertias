using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerRunState : IState
{
    // --- 获取玩家组件 ---
    public Player player;
    // 构造函数
    public PlayerRunState(Player player)
    {
        this.player = player;
    }
    public void OnEnter()
    {

    }

    public void OnExit()
    {

    }

    public void OnFixedUpdate()
    {

    }

    public void OnUpdate()
    {

    }
}