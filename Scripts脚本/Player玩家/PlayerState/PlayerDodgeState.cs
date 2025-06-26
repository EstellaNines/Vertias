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

    }

    public void OnExit()
    {

    }

    public void OnFixedUpdate()
    {
        player.Move(player.InputDirection);
        Dodge();
    }

    public void OnUpdate()
    {
        player.AIMTOR.SetBool("isDodging?", player.isDodged);

        if (player.isDodged == false)
        {
            player.transitionState(PlayerStateType.Idle);// 切换为待机状态 
        }
    }

    void Dodge()
    {
        if (!player.isDodgeCoolDownEnd)
        {
            if (DodgeTimer <= player.DodgeDuration)
            {
                player.PlayerRB2D.AddForce(player.InputDirection * player.DdogeForce, ForceMode2D.Impulse);
                DodgeTimer += Time.fixedDeltaTime;
            }
            else
            {
                player.isDodged = false;
                player.isDodgeCoolDownEnd = true;
                player.DodgeOnCoolDown();
                DodgeTimer = 0;

            }
        }
        
    }
}