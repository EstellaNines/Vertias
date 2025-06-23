using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// 丧尸攻击状态脚本
public class ZombieAttackState : IState
{
    private Zombie zombie; // 获取主脚本
    private AnimatorStateInfo info; // 获取动画状态信息
    // 构造函数
    public ZombieAttackState(Zombie zombie)
    {
        this.zombie = zombie;
    }
    public void OnEnter()
    {
        // 判断是否可以攻击
        if (zombie.isAttack)
        {
            zombie.animator.Play("Attack"); // 播放攻击动画
            zombie.isAttack = false;
        }
        // 调试
        Debug.Log("打你");
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
        // 攻击时禁止丧尸移动
            zombie.rb.velocity = Vector2.zero; // 停止移动

        // 添加方向判定逻辑
        if (zombie.player != null)
        {
            float zombieX = zombie.transform.position.x;
            float playerX = zombie.player.position.x;

            // 判断玩家方位并设置动画参数
            float horizontal = (playerX < zombieX) ? 0f : 1f;
            zombie.animator.SetFloat("Horizontial", horizontal);
        }

        // 获取动画状态信息
        info = zombie.animator.GetCurrentAnimatorStateInfo(0);

        // 检测动画是否播放完成
        if (info.normalizedTime >= 1.0f && !zombie.isAttack)
        {
            // 启动攻击冷却协程
            zombie.StartCoroutine(zombie.AttackCooldown());
            // 状态切换回待机
            zombie.transitionState(ZombieStateType.Idle);
        }
    }
}
