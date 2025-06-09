using UnityEngine;

[RequireComponent(typeof(Enemy))]
public class EnemyHealth : MonoBehaviour
{
    public int MaxHealth = 100;
    private int currentHealth;

    private Enemy enemy;
    private EnemyController enemyController;

    void Awake()
    {
        currentHealth = MaxHealth;
        enemy = GetComponent<Enemy>();
        enemyController = GetComponent<EnemyController>();
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;

        // 触发受伤动画
        enemy.OnHurt?.Invoke();

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            enemy.OnDie?.Invoke(); // 触发死亡事件
        }
    }
}