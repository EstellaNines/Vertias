using UnityEngine;

public class NeutralHurtState : IState
{
    private readonly NeutralEnemy enemy;
    private float hurtTimer;
    public float hurtDuration = 0.8f;

    public NeutralHurtState(NeutralEnemy enemy)
    {
        this.enemy = enemy;
    }

    public void OnEnter()
    {
        if (enemy.animator != null) enemy.animator.Play("Hurt");
        if (enemy.RB != null) enemy.RB.velocity = Vector2.zero;
        hurtTimer = 0f;
    }

    public void OnExit()
    {
        enemy.isHurt = false;
    }

    public void OnFixedUpdate()
    {
        if (enemy.RB != null) enemy.RB.velocity = Vector2.zero;
    }

    public void OnUpdate()
    {
        if (enemy.isDead)
        {
            enemy.Transition("Dead");
            return;
        }

        hurtTimer += Time.deltaTime;

        if (enemy.animator != null)
        {
            var info = enemy.animator.GetCurrentAnimatorStateInfo(0);
            if (info.IsName("Hurt") && info.normalizedTime >= 0.95f)
            {
                enemy.Transition("Idle");
                return;
            }
        }

        if (hurtTimer >= hurtDuration)
        {
            enemy.Transition("Idle");
        }
    }
}


