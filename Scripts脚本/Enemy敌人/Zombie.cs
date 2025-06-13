using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Pathfinding;

public class Zombie : MonoBehaviour
{
    public Transform player; // 玩家对象
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
    private bool isDead = false;

    // A*寻路调用
    [Header("A*寻路")]
    private Seeker seeker;
    private List<Vector3> PathPointList; // 路径点列表
    private int currentIndex = 0; // 路径点索引
    public float PathGenerateInterval = 0.5f; // 路径生成间隔
    public float PathGenerateTimer = 0f; // 计时器

    private void Awake()
    {
        seeker = GetComponent<Seeker>();
    }

    void FixedUpdate()
    {
        if (isDead) return; // 如果已死亡，直接退出，不再执行任何逻辑

        if (player == null)
            return;
        float distance = Vector2.Distance(transform.position, player.position);
        if (distance < ChaseDistance)
        {
            Autopath();
            if (PathPointList == null)
                return;

            if (distance < AttackDistance)
            {
                // 攻击
                OnMovement?.Invoke(Vector2.zero);// 停止移动
                OnAttack?.Invoke();
            }
            else
            {
                // 追击
                //Vector2 direction = (player.position - transform.position).normalized;
                Vector2 direction = (PathPointList[currentIndex] - transform.position).normalized;
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

    private void Autopath()
    {
        // 计时器间隔修改，实时修改路径
        PathGenerateTimer += Time.deltaTime;
        if (PathGenerateTimer >= PathGenerateInterval)
        {
            GeneratePath(player.position);
            PathGenerateTimer = 0;
        }
        // 当路径点为空时，进行路径计算
        if (PathPointList == null || PathPointList.Count <= 0)
        {
            GeneratePath(player.position);
        }
        // 当敌人到达当前路径点，递增路径点索引并进行索引点计算
        else if (Vector2.Distance(transform.position, PathPointList[currentIndex]) <= 0.1f)
        {
            currentIndex++;
            if (currentIndex >= PathPointList.Count)
                GeneratePath(player.position);
        }
    }
    // 路径点获取
    private void GeneratePath(Vector3 target)
    {
        currentIndex = 0;
        seeker.StartPath(transform.position, target, Path =>
        {
            PathPointList = Path.vectorPath; // 列表记录A*路径点
        }); // 起点、终点、回调函数
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
