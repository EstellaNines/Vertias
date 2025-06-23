using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// 丧尸追击状态脚本
public class ZombieChaseState : IState
{
    private Zombie zombie;
    // 构造函数
    public ZombieChaseState(Zombie zombie)
    {
        this.zombie = zombie;
    }
    public void OnEnter()
    {
        zombie.animator.Play("Walk"); // 播放追击动画

        // 调试
        Debug.Log("我来了");
    }

    public void OnExit()
    {

    }

    public void OnFixedUpdate()
    {
        move();
    }

    public void OnUpdate()
    {
        if (zombie.isHurt) // 判断是否受伤，则进入受伤状态
        {
            zombie.transitionState(ZombieStateType.Hurt); // 受伤状态转换
        }
        
        zombie.GetPlayerTransform(); // 获取玩家位置
        zombie.Autopath(); // 自动寻路
        // 判断敌人是否为空
        if (zombie.player != null)
        {
            // 判断路径点表是否为空
            if (zombie.PathPointList == null || zombie.PathPointList.Count <= 0)
                return; // 直接返回不执行

            // 是否在攻击范围呢
            if (zombie.distance <= zombie.AttackDistance) // 是否处于攻击状态
            {
                zombie.transitionState(ZombieStateType.Attack); // 状态切换为攻击状态
            }
            else
            {
                // 追击玩家
                Vector2 direction = (zombie.PathPointList[zombie.currentIndex] - zombie.transform.position).normalized; // 获取当前路径点与当前位置的向量
                zombie.MovementInput = direction; // 移动方向传递到MovementInput
            }
        }
        else
        {
            // 范围外停止追击
            zombie.transitionState(ZombieStateType.Idle); // 状态切换为待机状态
        }
    }

    // 重现移动函数
    void move()
    {
        // 检查 rb 是否存在且未死亡
        if (zombie.MovementInput.magnitude > 0.1f && zombie.CurrentSpeed >= 0)
        {
            zombie.rb.velocity = zombie.MovementInput * zombie.CurrentSpeed; // 移动
            // 方向控制逻辑
            float horizontal = zombie.MovementInput.x > 0 ? 1f : 0f;
            zombie.animator.SetFloat("Horizontial", horizontal);

        }
        else
        {
            zombie.rb.velocity = Vector2.zero;
        }
    }
}
