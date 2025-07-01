using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHurtState : IState
{
    // --- 获取玩家引用 ---
    public Player player;
    private AnimatorStateInfo info;

    // 构造函数
    public PlayerHurtState(Player player)
    {
        this.player = player;
    }

    public void OnEnter()
    {
        // 触发器控制播放受伤动画
        if (player.isWeaponInHand)
        {
            player.AIMTOR.SetTrigger("isShoot_Hurt?");
        }
        else
        {
            player.AIMTOR.SetTrigger("isHurt?");
        }
    }

    public void OnExit()
    {
        // 退出状态时将player脚本的isHurt的bool值改为false
        player.isHurt = false;
    }

    public void OnFixedUpdate()
    {
        // 受伤能够移动
        player.Move(player.InputDirection);
    }

    public void OnUpdate()
    {
        // 基础瞄准功能
        player.UpdateBasicAiming();

        // 检查是否死亡（最高优先级）
        if (player.isDead)
        {
            player.transitionState(PlayerStateType.Die);
            return;
        }

        // 传递动画状态信息
        info = player.AIMTOR.GetCurrentAnimatorStateInfo(0);
        if (info.normalizedTime >= 0.95f) // 当动画播放了95%就切换到待机状态
        {
            player.transitionState(PlayerStateType.Idle);
        }
    }

}