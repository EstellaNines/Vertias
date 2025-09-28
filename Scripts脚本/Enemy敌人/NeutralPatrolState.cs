using UnityEngine;
using Pathfinding;
 

// 巡逻：按巡逻点推进，到达后进入 Idle 若干秒，再继续巡逻
public class NeutralPatrolState : IState
{
    private readonly NeutralEnemy enemy;
    private float nextPathUpdate;
    private float idleTimer;
    private bool waitingIdle;

    public NeutralPatrolState(NeutralEnemy enemy) { this.enemy = enemy; }

    public void OnEnter()
    {
        if (enemy.patrolPoints == null || enemy.patrolPoints.Length == 0)
        {
            enemy.Transition("Idle");
            return;
        }
        waitingIdle = false;
        idleTimer = 0f;
        nextPathUpdate = 0f;
        if (enemy.animator != null) enemy.animator.Play("Walk");
    }

    public void OnExit()
    {
        if (enemy.RB != null) enemy.RB.velocity = Vector2.zero;
    }

    public void OnFixedUpdate()
    {
        if (!waitingIdle) FollowPath();
    }

    public void OnUpdate()
    {
        if (waitingIdle)
        {
            idleTimer += Time.deltaTime;
            // 已移除小队等待分散逻辑
            if (idleTimer >= enemy.idleAfterPointTime)
            {
                waitingIdle = false;
                if (enemy.animator != null) enemy.animator.Play("Walk");
                // 直接继续巡逻
                UpdatePath();
            }
            return;
        }

        if (Time.time >= nextPathUpdate)
        {
            nextPathUpdate = Time.time + enemy.pathUpdateInterval;
            UpdatePath();
        }
    }

    private void UpdatePath()
    {
        if (enemy.seeker == null) return;
        if (enemy.patrolPoints == null || enemy.patrolPoints.Length == 0) return;
        Transform target = enemy.patrolPoints[enemy.patrolIndex % enemy.patrolPoints.Length];
        if (target == null) return;

        // 正常寻路
        enemy.seeker.StartPath(enemy.transform.position, target.position, p =>
        {
            OnPathComplete(p);
        });
    }

    private void OnPathComplete(Path p)
    {
        if (!p.error)
        {
            enemy.currentPath = p;
            enemy.currentWaypoint = 0;
        }
    }

    private void FollowPath()
    {
        if (enemy.RB == null) return;

        // 已移除小队编队推进逻辑

        if (enemy.currentPath == null) return;
        if (enemy.currentWaypoint >= enemy.currentPath.vectorPath.Count)
        {
            enemy.reachedEndOfPath = true;
            // 到达当前巡逻点：进入Idle计时，然后切下一个点
            waitingIdle = true;
            idleTimer = 0f;
            if (enemy.animator != null) enemy.animator.Play("Idle");
            // 巡逻停下时恢复默认视角
            enemy.ResetFovToDefault();
            enemy.patrolIndex = (enemy.patrolIndex + 1) % enemy.patrolPoints.Length;
            return;
        }
        else enemy.reachedEndOfPath = false;

        Vector2 nextPoint = enemy.currentPath.vectorPath[enemy.currentWaypoint];
        Vector2 direction = (nextPoint - (Vector2)enemy.transform.position).normalized;
        // 小队模式已移除
        // 巡逻时让FOV朝向随路径方向变化
        enemy.SetFovForward(direction);
        Vector2 velocity = direction * enemy.MoveSpeed;
        enemy.RB.MovePosition(enemy.RB.position + velocity * Time.fixedDeltaTime);

        if (enemy.animator != null)
        {
            float horizontal = direction.x < 0 ? 0f : 1f;
            enemy.animator.SetFloat("Horizontal", horizontal);
        }

        float distance = Vector2.Distance(enemy.transform.position, nextPoint);
        if (distance < enemy.reachDistance) enemy.currentWaypoint++;
    }
}


