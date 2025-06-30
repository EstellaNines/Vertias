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
        // 检查玩家是否已死亡，如果死亡则不攻击
        if (zombie.player != null)
        {
            Player playerComponent = zombie.player.GetComponent<Player>();
            if (playerComponent != null && playerComponent.IsDead())
            {
                Debug.Log("玩家已死亡，丧尸停止攻击");
                zombie.transitionState(ZombieStateType.Idle);
                return;
            }
        }
        
        // 判断是否可以攻击
        if (zombie.isAttack)
        {
            zombie.animator.Play("Attack"); // 播放攻击动画
            zombie.isAttack = false;
            
            // 检查攻击范围内是否有玩家
            CheckPlayerInAttackRange();
        }
        // 调试
        Debug.Log("丧尸攻击玩家");
    }

    // 检查攻击范围内的玩家并造成伤害
    private void CheckPlayerInAttackRange()
    {
        if (zombie.player != null)
        {
            Player playerComponent = zombie.player.GetComponent<Player>();
            
            // 如果玩家已死亡，不造成伤害
            if (playerComponent != null && playerComponent.IsDead())
            {
                Debug.Log("玩家已死亡，丧尸不造成伤害");
                return;
            }
            
            float distanceToPlayer = Vector2.Distance(zombie.transform.position, zombie.player.position);
            
            // 如果玩家在攻击范围内且未死亡
            if (distanceToPlayer <= zombie.AttackDistance && playerComponent != null)
            {
                playerComponent.TakeDamage(10f); // 丧尸攻击造成10点伤害
                Debug.Log("丧尸成功攻击玩家，造成10点伤害");
            }
        }
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
