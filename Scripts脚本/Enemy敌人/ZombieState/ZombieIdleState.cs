using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// 丧尸待机状态脚本
public class ZombieIdleState : IState
{
    private Zombie zombie;
    private float Timer = 0; // 定时器
    // 构造函数
    public ZombieIdleState(Zombie zombie)
    {
        this.zombie = zombie;
    }
    public void OnEnter()
    {
        zombie.animator.Play("Idle"); // 播放待机动画
        zombie.rb.velocity = Vector2.zero; // 待机状态不能动

        // 调试
        Debug.Log("不走了");
    }

    public void OnExit()
    {

    }

    public void OnFixedUpdate()
    {

    }

    public void OnUpdate()
    {
        if (zombie.isHurt) // 判断是否受伤，则进入受伤状态
        {
            zombie.transitionState(ZombieStateType.Hurt); // 受伤状态转换
        }

        zombie.GetPlayerTransform(); // 获取玩家位置
        if (zombie.player != null) // 如果玩家不为空
        {
            if (zombie.distance > zombie.AttackDistance) // 如果玩家距离大于攻击距离,切换为追击状态
            {
                zombie.transitionState(ZombieStateType.Chase);
            }
            else if (zombie.distance <= zombie.AttackDistance) // 如果玩家距离小于等于攻击距离,切换为攻击状态
            {
                zombie.transitionState(ZombieStateType.Attack);
            }
        }
        else // 玩家为空，则切换到巡逻状态
        {
            if (Timer <= zombie.IdleDuration)
            {
                Timer += Time.deltaTime; // 计时器加一
            }
            else
            {
                Timer = 0;
                zombie.transitionState(ZombieStateType.Patrol); // 巡逻状态转换

            }
        }
    }
}
