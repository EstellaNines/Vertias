using UnityEngine;

// 瞄准：当玩家进入听力范围时，中立敌人转向玩家；若敌对且在攻击范围内，可在后续接入 Attack
public class NeutralAimState : IState
{
    private readonly NeutralEnemy enemy;
    public NeutralAimState(NeutralEnemy enemy) { this.enemy = enemy; }

    public void OnEnter()
    {
        if (enemy.RB != null) enemy.RB.velocity = Vector2.zero;
        if (enemy.animator != null) enemy.animator.Play("Idle");
    }

    public void OnExit() { }
    public void OnFixedUpdate() { }

    public void OnUpdate()
    {
        Vector2 origin = enemy.eye != null ? (Vector2)enemy.eye.position : (Vector2)enemy.transform.position;

        // 通过物理Overlap检测玩家碰撞体（优先用层）
        Collider2D playerCol = Physics2D.OverlapCircle(origin, enemy.hearingRadius, enemy.playerLayer);
        if (playerCol == null)
        {
            // 备用：按Tag查找
            GameObject tagged = GameObject.FindGameObjectWithTag("Player");
            if (tagged != null)
            {
                float distTag = Vector2.Distance(origin, tagged.transform.position);
                if (distTag <= enemy.hearingRadius) playerCol = tagged.GetComponentInChildren<Collider2D>();
            }
        }

        if (playerCol != null)
        {
            // 若玩家处于潜行状态，则不看向玩家，恢复默认并回Idle
            var playerComp = playerCol.GetComponentInParent<Player>();
            if (playerComp != null && playerComp.IsCrouching())
            {
                enemy.ResetFovToDefault();
                enemy.Transition("Idle");
                return;
            }

            Vector2 target = playerCol.transform.position;
            Vector2 dir = (target - origin).normalized;
            // 仅更新FOV朝向，不改变本体旋转
            enemy.SetFovForward(dir);

            // 若已敌对，让武器也瞄准玩家；若处于FOV视野则进入攻击；若玩家死亡则恢复默认
            if (enemy.isHostile)
            {
                enemy.AimWeaponTowards(origin, target);
                var pComp = playerCol.GetComponentInParent<Player>();
                if (pComp != null && pComp.isDead)
                {
                    enemy.ResetFovToDefault();
                    enemy.ResetWeaponAim();
                    enemy.Transition("Idle");
                    return;
                }
                var fov = enemy.GetComponentInChildren<FOV>();
                if (enemy.IsPlayerInFov(playerCol.transform, fov))
                {
                    enemy.Transition("Attack");
                    return;
                }
            }
        }
        else
        {
            // 听力范围内无目标：根据类型回站岗或继续巡逻
            if (enemy.neutralType == NeutralEnemy.NeutralType.Patrol)
            {
                enemy.Transition("Patrol");
            }
            else
            {
                enemy.ResetFovToDefault();
                enemy.Transition("Idle");
            }
        }
    }
}


