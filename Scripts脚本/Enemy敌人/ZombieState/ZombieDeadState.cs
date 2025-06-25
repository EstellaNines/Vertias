using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// 死亡状态机
public class ZombieDeadState : IState
{
    private Zombie zombie;
    // 构造函数
    public ZombieDeadState(Zombie zombie)
    {
        this.zombie = zombie;
    }
    public void OnEnter()
    {
        // 死亡方向
        float directionValue = (zombie.currentHorizontalDirection < 0) ? 0f : 1f;
        zombie.animator.SetFloat("Horizontal", directionValue);
        zombie.animator.Play("Death"); // 播放死亡动画
        zombie.rb.velocity = Vector2.zero; // 禁止移动
        zombie.physicalCollider.enabled = false; // 禁用碰撞
        zombie.rb.angularVelocity = 0f; // 禁用旋转


        // 调试
        Debug.Log($"[????] ??: {zombie.transform.position}", zombie);
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
