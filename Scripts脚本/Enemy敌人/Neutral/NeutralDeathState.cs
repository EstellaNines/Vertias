using UnityEngine;

public class NeutralDeathState : IState
{
    private readonly NeutralEnemy enemy;
    private bool deathAnimationFinished;
    private float deathTimer;
    private const float MinDeathTime = 0.2f;

    public NeutralDeathState(NeutralEnemy enemy)
    {
        this.enemy = enemy;
    }

    public void OnEnter()
    {
        Debug.Log($"中立敌人 {enemy.gameObject.name} 进入死亡状态");

        // 播放死亡动画（只触发一次）
        if (enemy.animator != null)
        {
            enemy.animator.Play("Dead");
        }

        // 停止移动并禁用物理与碰撞
        if (enemy.RB != null)
        {
            enemy.RB.velocity = Vector2.zero;
            enemy.RB.isKinematic = true;
        }
        if (enemy.Collider != null)
        {
            enemy.Collider.enabled = false;
        }

        deathTimer = 0f;
        deathAnimationFinished = false;

        // 触发一次死亡事件（若调用方未触发）
        enemy.OnDeath?.Invoke();
    }

    public void OnExit() { }

    public void OnFixedUpdate()
    {
        if (enemy.RB != null && !enemy.RB.isKinematic)
        {
            enemy.RB.velocity = Vector2.zero;
        }
    }

    public void OnUpdate()
    {
        deathTimer += Time.deltaTime;

        if (enemy.animator != null && !deathAnimationFinished)
        {
            var info = enemy.animator.GetCurrentAnimatorStateInfo(0);
            if (info.IsName("Death") && info.normalizedTime >= 0.95f)
            {
                deathAnimationFinished = true;
            }
        }

        // 终态：不再进行其他状态切换；如需销毁可在此添加
        if (deathAnimationFinished && deathTimer >= MinDeathTime)
        {
            // 可选：Object.Destroy(enemy.gameObject);
        }
    }
}


