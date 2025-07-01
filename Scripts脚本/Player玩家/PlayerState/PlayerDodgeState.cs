using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerDodgeState : IState
{
    // --- 获取玩家组件 ---
    public Player player;
    private float DodgeTimer = 0;
    // 构造函数
    public PlayerDodgeState(Player player)
    {
        this.player = player;
    }
    public void OnEnter()
    {
        player.isDodgeCoolDownEnd = false; // 进入闪避状态时设置为冷却中
    }

    public void OnExit()
    {
        DodgeTimer = 0; // 退出时重置计时器
    }

    public void OnFixedUpdate()
    {
        player.Move(player.InputDirection);
        Dodge();
    }

    public void OnUpdate()
    {
        player.AIMTOR.SetBool("isDodging?", player.isDodged);

        // 受伤
        if(player.isHurt)
        {
            player.transitionState(PlayerStateType.Hurt);
        }
        if (player.isDodged == false)
        {
            player.transitionState(PlayerStateType.Idle);// 切换为待机状态 
        }
        
    }
    // 翻滚功能
    void Dodge()
    {
        if (DodgeTimer <= player.DodgeDuration)
        {
            player.PlayerRB2D.AddForce(player.InputDirection * player.DdogeForce, ForceMode2D.Impulse);
            DodgeTimer += Time.fixedDeltaTime;
        }
        else
        {
            player.isDodged = false;
            player.DodgeOnCoolDown();
            DodgeTimer = 0;
        }
    }
}