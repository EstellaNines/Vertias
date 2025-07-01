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
        Debug.Log("进入玩家死亡状态");
        
        // 清除受伤动画布尔值
        player.AIMTOR.SetBool("isHurting?", false);
        player.AIMTOR.SetBool("isDying?", true);
        
        // 播放死亡动画
        if (player.isWeaponInHand)
        {
            player.AIMTOR.Play("Shoot_Death"); // 如果有武器，播放持武器死亡动画
        }
        else
        {
            player.AIMTOR.Play("Death"); // 播放死亡动画
        }
        
        // 停止玩家移动
        player.PlayerRB2D.velocity = Vector2.zero;
        
        // 清除所有状态标记
        player.isHurt = false;
        player.isInHurtState = false;
        player.isAttacking = false;
        player.isFiring = false;
    }

    public void OnExit()
    {
        Debug.Log("退出死亡状态");
        // 通常死亡状态不会退出，但为了完整性保留此方法
    }

    public void OnFixedUpdate()
    {
        // 死亡状态下保持静止
        player.PlayerRB2D.velocity = Vector2.zero;
    }

    public void OnUpdate()
    {
        // 死亡状态下不处理任何输入
        // 可以在这里添加重生逻辑或游戏结束逻辑
    }
    
    // 死亡动画完成处理 - 基本接口
    public void OnDeathAnimationFinished()
    {
        Debug.Log("死亡动画完成 - 基本框架");
        // TODO: 实现死亡动画完成逻辑
    }
}