using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Enemy : MonoBehaviour
{
    public Transform player;
    [Header("追击范围")]
    public float ChaseDistance = 5f;
    public float AttackDistance = 1.5f;

    // 事件
    [Header("事件")]
    public UnityEvent<Vector2> OnMovement;
    public UnityEvent OnAttack;
    public UnityEvent OnStopAttack;
    public UnityEvent OnHurt; // 受伤事件
    public UnityEvent OnDie;  // 死亡事件

    // 内部状态
    private int health = 100;

    void FixedUpdate()
    {
        if (player == null)
            return;
        float distance = Vector2.Distance(transform.position, player.position);
        if (distance < ChaseDistance)
        {
            if (distance < AttackDistance)
            {
                // 攻击
                OnMovement?.Invoke(Vector2.zero);// 停止移动
                OnAttack?.Invoke();
            }
            else
            {
                // 追击
                Vector2 direction = (player.position - transform.position).normalized;
                OnMovement?.Invoke(direction);// 触发移动事件
                OnStopAttack?.Invoke();
            }
        }
        else
        {
            OnMovement?.Invoke(Vector2.zero);
            OnStopAttack?.Invoke();
        }

    }

    public void TakeDamage(int damage)
    {
        health -= damage;
        OnHurt?.Invoke(); // 触发受伤动画

        if (health <= 0)
        {
            OnDie?.Invoke(); // 触发死亡事件
        }
    }


}
