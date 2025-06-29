using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerRunState : IState
{
    // --- ��ȡ������ ---
    public Player player;
    // ���캯��
    public PlayerRunState(Player player)
    {
        this.player = player;
    }
    
    public void OnEnter()
    {
        // �����ܲ��ٶ�
        player.CurrentSpeed = player.RunSpeed;
        player.AIMTOR.SetFloat("Speed", player.CurrentSpeed); // ���ö�������
        
        // �����Ƿ����������Ŷ�Ӧ����
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
        // �˳�ʱ�ָ������ٶ�
        player.CurrentSpeed = player.WalkSpeed;
    }

    public void OnFixedUpdate()
    {
        // ����������ʱӦ���ٶ�
        player.PlayerRB2D.velocity = player.InputDirection * player.CurrentSpeed;
    }

    public void OnUpdate()
    {
        // �ӽǱ仯ʼ�մ���
        player.UpdateLookDirection();
        
        // ʰȡ�л�
        if (player.isPickingUp)
        {
            player.transitionState(PlayerStateType.PickUp);
            return;
        }
        
        // ����ɿ��ܲ�����û�����룬��������л�״̬
        if (!player.isRunning)
        {
            if (player.InputDirection != Vector2.zero)
            {
                player.transitionState(PlayerStateType.Move); // �л����ƶ�״̬
            }
            else
            {
                player.transitionState(PlayerStateType.Idle); // �л�������״̬
            }
            return;
        }
        
        // ��û������ʱ�л�������״̬
        if (player.InputDirection == Vector2.zero)
        {
            player.transitionState(PlayerStateType.Idle);
            return;
        }
        
        // �����л�
        if (player.isDodged)
        {
            player.transitionState(PlayerStateType.Dodge);
            return;
        }
    }
}