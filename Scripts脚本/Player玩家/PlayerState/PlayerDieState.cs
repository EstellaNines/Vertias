using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerDieState : IState
{
    // --- 获取玩家引用 ---
    public Player player;
    
    // 构造函数
    public PlayerDieState(Player player)
    {
        this.player = player;
    }
    
    public void OnEnter()
    {
        Debug.Log("进入玩家死亡状态 - 基本框架");
        // TODO: 实现死亡状态进入逻辑
    }

    public void OnExit()
    {
        Debug.Log("退出死亡状态 - 基本框架");
        // TODO: 实现死亡状态退出逻辑
    }

    public void OnFixedUpdate()
    {
        // TODO: 实现死亡状态物理更新逻辑
    }

    public void OnUpdate()
    {
        // TODO: 实现死亡状态更新逻辑
    }
    
    // 死亡动画完成处理 - 基本接口
    public void OnDeathAnimationFinished()
    {
        Debug.Log("死亡动画完成 - 基本框架");
        // TODO: 实现死亡动画完成逻辑
    }
}