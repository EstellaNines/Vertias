using UnityEngine;

public class EnemyAttackState : IState
{
    // --- 控制器引用 ---
    Enemy enemy;
    private float attackTime = 0f;
    private float maxAttackTime = 5f;
    private int shotsFired = 0;
    private int maxShots = 30; // 这个值应该从武器的弹夹容量获取
    private float lastShotTime = 0f;
    private bool isReloading = false;
    private float reloadStartTime = 0f;
    private float reloadDuration = 1.5f; // 敌人固定换弹时间
    private bool firedThisFrame = false; // 本帧是否实际开火

    // --- 构造函数 --- 
    public EnemyAttackState(Enemy enemy)
    {
        this.enemy = enemy;
    }

    // --- 状态方法 ---
    public void OnEnter()
    {
        // 停止移动
        if (enemy.RB != null)
        {
            enemy.RB.velocity = Vector2.zero;
        }

        // 播放待机动画
        if (enemy.animator != null)
        {
            enemy.animator.Play("Idle");
        }

        // 重置攻击时间
        attackTime = 0f;

        // 获取武器弹夹容量
        if (enemy.weaponController != null)
        {
            maxShots = enemy.weaponController.GetMagazineCapacity();
            // 订阅开火事件以确认是否真的发射
            enemy.weaponController.OnFired += OnWeaponFired;
        }
    }

    public void OnExit()
    {
        // 退出攻击状态
        isReloading = false;
        if (enemy.weaponController != null)
        {
            enemy.weaponController.OnFired -= OnWeaponFired;
        }
    }

    public void OnFixedUpdate()
    {
        // 物理更新
    }

    public void OnUpdate()
    {
        // 检查是否死亡 - 最高优先级
        if (enemy.isDead)
        {
            enemy.transitionState(EnemyState.Dead);
            return;
        }

        // 判断是否受伤
        if (enemy.isHurt)
        {
            enemy.transitionState(EnemyState.Hurt);
            return;
        }

        // 检查玩家是否已死亡
        if (enemy.IsPlayerDead())
        {
            Debug.Log("玩家已死亡，敌人停止攻击");
            enemy.shouldPatrol = true;
            enemy.transitionState(EnemyState.Patrol);
            return;
        }

        // 检查玩家是否超出攻击范围
        if (!IsPlayerInAttackRange())
        {
            Debug.Log("玩家超出攻击范围，敌人停止攻击");
            enemy.shouldPatrol = true;
            enemy.transitionState(EnemyState.Patrol);
            return;
        }

        // 如果玩家不再被检测到或者进入潜行状态，返回巡逻状态
        if (!enemy.IsPlayerDetected() || enemy.IsPlayerCrouching())
        {
            enemy.shouldPatrol = true;
            enemy.transitionState(EnemyState.Patrol);
            return;
        }

        // 处理换弹逻辑
        if (isReloading)
        {
            if (Time.time >= reloadStartTime + reloadDuration)
            {
                // 换弹完成
                isReloading = false;
                shotsFired = 0; // 重置射击计数
                Debug.Log("敌人换弹完成");

                // 如果有武器控制器，重置其弹药
                if (enemy.weaponController != null)
                {
                    // 这里需要在WeaponManager中添加一个重置弹药的方法
                    enemy.weaponController.currentAmmo = enemy.weaponController.GetMagazineCapacity();
                }
            }
            else
            {
                // 换弹中，不进行射击
                return;
            }
        }

        // 检查是否需要换弹
        if (shotsFired >= maxShots && !isReloading)
        {
            Debug.Log("敌人开始换弹");
            isReloading = true;
            reloadStartTime = Time.time;
            return;
        }

        // 请求射击（由武器内部节流控制实际是否发射）
        if (shotsFired < maxShots && !isReloading)
        {
            firedThisFrame = false;
            enemy.Shoot();
            if (firedThisFrame)
            {
                shotsFired++;
                lastShotTime = Time.time;
            }
        }

        // 攻击一段时间后切换回瞄准状态
        attackTime += Time.deltaTime;
        if (attackTime >= maxAttackTime)
        {
            enemy.transitionState(EnemyState.Aim);
        }
    }

    // 获取已发射子弹数量
    public int GetShotsFired()
    {
        return shotsFired;
    }

    // 重置已发射子弹数量
    public void ResetShotsFired()
    {
        shotsFired = 0;
    }

    // 检查是否正在换弹
    public bool IsReloading()
    {
        return isReloading;
    }
    // 检查玩家是否在攻击范围内
    private bool IsPlayerInAttackRange()
    {
        if (enemy.player == null) return false;

        Vector2 enemyPosition = enemy.eyePoint ? (Vector2)enemy.eyePoint.transform.position : (Vector2)enemy.transform.position;
        Vector2 playerPosition = enemy.player.transform.position;
        float distanceToPlayer = Vector2.Distance(enemyPosition, playerPosition);

        // 检查距离是否在检测范围内
        return distanceToPlayer <= enemy.playerDetectionRadius;
    }

    // 事件回调：记录实际发射
    private void OnWeaponFired(WeaponManager wm)
    {
        firedThisFrame = true;
    }
}
