using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// 丧尸受伤状态脚本
public class ZombieHurtState : IState
{

    private Zombie zombie;
    private float animationDuration; // 动画长度
    private bool hasPlayedAnimation = false; // 是否已播放动画

    private float Timer; // 击退计时器
    // 构造函数
    public ZombieHurtState(Zombie zombie)
    {
        this.zombie = zombie;
    }
    public void OnEnter()
    {
        zombie.animator.Play("Hurt"); // 播放受伤动画
        AnimationClip[] clips = zombie.animator.runtimeAnimatorController.animationClips; // 获取动画片段
        foreach (AnimationClip clip in clips) // 遍历动画片段
        {
            if (clip.name == "Hurt") // 判断当前片段名称是否为"Hurt"
            {
                animationDuration = clip.length; // 设置动画持续时间
                break;
            }
        }

        // 重置标志位
        hasPlayedAnimation = false;
        zombie.isHurt = true;
    }

    public void OnExit()
    {
        zombie.isHurt = false; // 重置为非受伤状态
    }

    public void OnFixedUpdate()
    {
        // 优先播放完整动画
        if (!hasPlayedAnimation)
        {
            if (Timer >= animationDuration)
            {
                hasPlayedAnimation = true;
                Timer = 0;
            }
            else
            {
                Timer += Time.fixedDeltaTime;
                return; // 先不处理击退
            }
        }

        // 动画播放完成后处理击退
        if (Timer <= zombie.KnockBackDuration)
        {
            zombie.rb.AddForce(zombie.KnockBackDirection * zombie.KnockBackForce, ForceMode2D.Impulse); // 击退
            Timer += Time.fixedDeltaTime;
        }
        else
        {
            Timer = 0;
            zombie.isHurt = false;
            zombie.transitionState(ZombieStateType.Idle);
        }
    }

    public void OnUpdate()
    {
        if (zombie.isKnockBack)
        {
            // 玩家在追击范围内攻击
            if (zombie.player != null)
            {
                zombie.KnockBackDirection = (zombie.transform.position - zombie.player.position).normalized;
            }
        }
        else
        {
            // 玩家在追击范围外攻击
            Transform player = GameObject.FindWithTag("player").transform;
            zombie.KnockBackDirection = (zombie.transform.position - zombie.player.position).normalized;
        }
    }
}
