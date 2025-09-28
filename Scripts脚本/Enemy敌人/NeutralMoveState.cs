using UnityEngine;
using Pathfinding;

// 行走/移动：依据 A* 路径推进，速度非0即维持该状态，控制 Horizontal（左0/右1）
public class NeutralMoveState : IState
{
    private readonly NeutralEnemy enemy;
    private float nextPathUpdate;
    private Vector2 targetPosition;

    public NeutralMoveState(NeutralEnemy enemy) { this.enemy = enemy; }

    public void OnEnter()
    {
        if (enemy.animator != null) enemy.animator.Play("Walk");
        // 目标：当前巡逻目标或玩家最后位置（此处以巡逻为主）
        UpdatePatrolTarget();
        nextPathUpdate = 0f;
    }

    public void OnExit()
    {
        if (enemy.RB != null) enemy.RB.velocity = Vector2.zero;
    }

    public void OnFixedUpdate()
    {
        FollowPath();
    }

    public void OnUpdate()
    {
        if (Time.time >= nextPathUpdate)
        {
            nextPathUpdate = Time.time + enemy.pathUpdateInterval;
            UpdatePath();
        }
    }

    private void UpdatePatrolTarget()
    {
        if (enemy.patrolPoints != null && enemy.patrolPoints.Length > 0)
        {
            Transform t = enemy.patrolPoints[enemy.patrolIndex % enemy.patrolPoints.Length];
            if (t != null) targetPosition = t.position;
        }
        else
        {
            targetPosition = enemy.transform.position;
        }
    }

    private void UpdatePath()
    {
        if (enemy.seeker == null) return;
        enemy.seeker.StartPath(enemy.transform.position, targetPosition, OnPathComplete);
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

        if (enemy.currentPath == null) return;
        if (enemy.currentWaypoint >= enemy.currentPath.vectorPath.Count)
        {
            enemy.reachedEndOfPath = true;
            // 到达初始目标：切入巡逻循环
            enemy.Transition("Patrol");
            return;
        }
        else enemy.reachedEndOfPath = false;

        Vector2 nextPoint = enemy.currentPath.vectorPath[enemy.currentWaypoint];
        Vector2 direction = (nextPoint - (Vector2)enemy.transform.position).normalized;
        // 让FOV朝向随移动方向变化
        enemy.SetFovForward(direction);
        Vector2 velocity = direction * enemy.MoveSpeed;
        enemy.RB.MovePosition(enemy.RB.position + velocity * Time.fixedDeltaTime);

        // Horizontal 左0/右1
        if (enemy.animator != null)
        {
            float horizontal = direction.x < 0 ? 0f : 1f;
            enemy.animator.SetFloat("Horizontal", horizontal);
        }

        float distance = Vector2.Distance(enemy.transform.position, nextPoint);
        if (distance < enemy.reachDistance) enemy.currentWaypoint++;
    }
}


