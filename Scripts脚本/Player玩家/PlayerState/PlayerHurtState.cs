using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHurtState : IState
{
    // --- 获取玩家引用 ---
    public Player player;
    
    // 构造函数
    public PlayerHurtState(Player player)
    {
        this.player = player;
    }
    
    public void OnEnter()
    {
        Debug.Log("进入玩家受伤状态 - 基本框架");
        // TODO: 实现受伤状态进入逻辑
    }

    public void OnExit()
    {
        Debug.Log("退出受伤状态 - 基本框架");
        // TODO: 实现受伤状态退出逻辑
    }

    public void OnFixedUpdate()
    {
        // TODO: 实现受伤状态物理更新逻辑
    }

    public void OnUpdate()
    {
        // TODO: 实现受伤状态更新逻辑
    }
}