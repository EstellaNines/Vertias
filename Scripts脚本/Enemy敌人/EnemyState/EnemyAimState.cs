using UnityEngine;

public class EnemyAimState : IState
{
    // --- ���������� ---
    Enemy enemy;
    private float aimTime = 0f;
    private float maxAimTime = 0.05f; // ��׼ʱ���0.5����ٵ�0.2�룬��߷�Ӧ�ٶ�
    private float cooldownTime = 0.5f; // ���������ȴʱ��
    private float cooldownTimer = 0f; // ��ȴ��ʱ��
    private bool inCooldown = false; // �Ƿ�����ȴ��
    
    // --- ���캯�� --- 
    public EnemyAimState(Enemy enemy)
    {
        this.enemy = enemy;
    }
    
    // --- ״̬���� ---
    public void OnEnter()
    {
        // ֹͣ�ƶ�
        if (enemy.RB != null)
        {
            enemy.RB.velocity = Vector2.zero;
        }
        
        // ���Ŵ�������
        if (enemy.animator != null)
        {
            enemy.animator.Play("Idle");
        }
        
        // ������׼ʱ��
        aimTime = 0f;
        
        // ��鹥��״̬�Ƿ����ڻ���
        EnemyAttackState attackState = GetAttackState();
        if (attackState != null && attackState.IsReloading())
        {
            inCooldown = true;
            cooldownTimer = 0f;
            cooldownTime = 1.5f; // �ȴ��������
        }
        else if (attackState != null && attackState.GetShotsFired() >= 30)
        {
            inCooldown = true;
            cooldownTimer = 0f;
            cooldownTime = 0.5f; // ������ȴʱ��
        }
    }

    public void OnExit()
    {
        // �˳���׼״̬
    }

    public void OnFixedUpdate()
    {
        // ��������
    }

    public void OnUpdate()
    {
        // ����Ƿ����� - ������ȼ�
        if (enemy.isDead)
        {
            enemy.transitionState(EnemyState.Dead);
            return;
        }
        
        if (enemy.isHurt)
        {
            enemy.transitionState(EnemyState.Hurt); // ��������״̬
        }

        // �������Ƿ�������
        if (enemy.IsPlayerDead())
        {
            Debug.Log("���������������ֹͣ��׼");
            enemy.shouldPatrol = true;
            enemy.transitionState(EnemyState.Patrol);
            return;
        }
        
        // �������Ƿ񳬳�������Χ
        if (!IsPlayerInAttackRange())
        {
            Debug.Log("��ҳ���������Χ������ֹͣ��׼");
            enemy.shouldPatrol = true;
            enemy.transitionState(EnemyState.Patrol);
            return;
        }
        
        // �����Ҳ��ٱ���⵽���߽���Ǳ��״̬������Ѳ��״̬
        if (!enemy.IsPlayerDetected() || enemy.IsPlayerCrouching())
        {
            enemy.shouldPatrol = true; // ���ÿ��Լ���Ѳ��
            enemy.transitionState(EnemyState.Patrol);
            return;
        }
        
        // �������ȴ�У�������ȴ�߼�
        if (inCooldown)
        {
            cooldownTimer += Time.deltaTime;
            if (cooldownTimer >= cooldownTime)
            {
                // ��ȴ��������������������˳���ȴ״̬
                EnemyAttackState attackState = GetAttackState();
                if (attackState != null)
                {
                    attackState.ResetShotsFired();
                }
                inCooldown = false;
            }
            return; // ����ȴ�в�������׼�͹���
        }
        
        // ��׼���
        AimAtPlayer();
        
        // ��׼һ��ʱ����л�������״̬
        aimTime += Time.deltaTime;
        if (aimTime >= maxAimTime)
        {
            enemy.transitionState(EnemyState.Attack);
        }
    }
    
    // ��׼���
    private void AimAtPlayer()
    {
        if (enemy.player == null) return;
        
        // ���㳯����ҵķ���
        Vector2 playerPosition = enemy.GetPlayerPosition();
        Vector2 direction = (playerPosition - (Vector2)enemy.transform.position).normalized;
        
        // ʹ��Enemy���е�SetDirection�������÷���
        enemy.SetDirection(direction);
    }
    
    // ��ȡ����״̬���
    private EnemyAttackState GetAttackState()
    {
        if (enemy != null && enemy.states != null && enemy.states.TryGetValue(EnemyState.Attack, out IState state))
        {
            return state as EnemyAttackState;
        }
        return null;
    }

    // �������Ƿ��ڹ�����Χ��
    private bool IsPlayerInAttackRange()
    {
        if (enemy.player == null) return false;
        
        Vector2 enemyPosition = enemy.eyePoint ? (Vector2)enemy.eyePoint.transform.position : (Vector2)enemy.transform.position;
        Vector2 playerPosition = enemy.player.transform.position;
        float distanceToPlayer = Vector2.Distance(enemyPosition, playerPosition);
        
        // �������Ƿ��ڼ�ⷶΧ��
        return distanceToPlayer <= enemy.playerDetectionRadius;
    }
}