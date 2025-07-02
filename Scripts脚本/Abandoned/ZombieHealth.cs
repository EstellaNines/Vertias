using UnityEngine;

[RequireComponent(typeof(Zombie))]
public class ZombieHealth : MonoBehaviour
{
    // public int MaxHealth = 100;
    // private int currentHealth;

    // private Zombie zombie;
    // private ZombieController zombieController;

    // void Awake()
    // {
    //     currentHealth = MaxHealth;
    //     zombie = GetComponent<Zombie>();
    //     zombieController = GetComponent<ZombieController>();
    // }

    // public void TakeDamage(int damage)
    // {
    //     currentHealth -= damage;

    //     // 触发受伤动画
    //     zombie.OnHurt?.Invoke();

    //     if (currentHealth <= 0)
    //     {
    //         currentHealth = 0;
    //         zombie.OnDie?.Invoke(); // 触发死亡事件
    //     }
    // }
}