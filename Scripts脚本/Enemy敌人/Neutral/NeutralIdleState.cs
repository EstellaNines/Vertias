using UnityEngine;

public class NeutralIdleState : IState
{
    private readonly NeutralEnemy enemy;
    private float idleTimer;
    public float idleTime = 3f;

    public NeutralIdleState(NeutralEnemy enemy) { this.enemy = enemy; }

    public void OnEnter()
    {
        if (enemy.RB != null) enemy.RB.velocity = Vector2.zero;
        enemy.PlayAnimation("Idle");
        idleTimer = 0f;
    }

    public void OnExit() { }
    public void OnFixedUpdate() { }

    public void OnUpdate()
    {
        idleTimer += Time.deltaTime;

        // 听力范围感知：玩家进入听力范围时切换到 Aim
        var playerGo = GameObject.FindGameObjectWithTag("Player");
        if (playerGo != null)
        {
            Vector2 origin = enemy.eye != null ? (Vector2)enemy.eye.position : (Vector2)enemy.transform.position;
            float dist = Vector2.Distance(origin, playerGo.transform.position);
            if (dist <= enemy.hearingRadius)
            {
                enemy.Transition("Aim");
                return;
            }
        }
        
        // 站岗型：以默认方向为中心，在[min,max]度内扫视（非敌对时）
        if (enemy.neutralType == NeutralEnemy.NeutralType.Guard && !enemy.isHostile && enemy.enableGuardScan)
        {
            Vector2 dir = enemy.EvaluateScanForward(Time.time);
            enemy.SetFovForward(dir);
        }

        // 保持待机；此处预留扩展（如：待机超时做其它事）
    }
}


