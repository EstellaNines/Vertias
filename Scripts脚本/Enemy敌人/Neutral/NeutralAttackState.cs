using UnityEngine;

public class NeutralAttackState : IState
{
    private readonly NeutralEnemy enemy;
    private Transform player;
    private FOV fov;
    private float attackInterval = 0.25f; // 开火节流
    private float nextAttackTime = 0f;

    public NeutralAttackState(NeutralEnemy enemy)
    {
        this.enemy = enemy;
    }

    public void OnEnter()
    {
        var pGo = GameObject.FindGameObjectWithTag("Player");
        player = pGo != null ? pGo.transform : null;
        fov = enemy.GetComponentInChildren<FOV>();
    }

    public void OnExit() { }
    public void OnFixedUpdate() { }

    public void OnUpdate()
    {
        if (!enemy.isHostile)
        {
            enemy.Transition("Idle");
            return;
        }
        if (player == null)
        {
            var pGo = GameObject.FindGameObjectWithTag("Player");
            player = pGo != null ? pGo.transform : null;
            if (player == null) return;
        }

        // 若玩家已死亡：停止攻击并恢复默认视角
        var playerComp = player.GetComponent<Player>();
        if (playerComp != null && playerComp.isDead)
        {
            enemy.ResetFovToDefault();
            enemy.ResetWeaponAim();
            enemy.Transition("Idle");
            return;
        }

        // 若玩家处于潜行，则不攻击
        if (playerComp != null && playerComp.IsCrouching())
        {
            enemy.Transition("Idle");
            return;
        }

        // 必须在FOV内才开火
        if (!enemy.IsPlayerInFov(player, fov))
        {
            enemy.Transition("Aim");
            return;
        }

        // 瞄准武器
        Vector2 origin = enemy.eye != null ? (Vector2)enemy.eye.position : (Vector2)enemy.transform.position;
        enemy.AimWeaponTowards(origin, player.position);

        // 节流开火
        if (Time.time >= nextAttackTime)
        {
            nextAttackTime = Time.time + attackInterval;
            // 使用 WeaponManager 开火；若不可用，可扩展到手动实例化对象池子弹
            if (enemy.weaponController != null)
            {
                enemy.weaponController.FireSingle();
            }
        }
    }
}


