using UnityEngine;
using System.Collections.Generic;
using Pathfinding;
using System.Collections;

public class ZombiePatrolState : MonoBehaviour
{
    [System.Serializable]
    public enum ZombieState
    {
        Idle, // 闲置状态
        Patrol, // 巡逻状态
        Chase, // 追逐状态
        Hurt // 受伤状态
    }

    [Header("闲置和巡逻参数")]
    [SerializeField] private float idleDuration = 3f; // 闲置状态持续时间
    [SerializeField] private float patrolInterval = 2f; // 巡逻间隔时间

    [Header("巡逻点设置")]
    [SerializeField] private Transform[] patrolWaypoints; // 巡逻点，在Inspector中设置

    private ZombieState currentState = ZombieState.Patrol; // 当前状态，默认为巡逻状态
    private Transform playerTransform; // 玩家的Transform组件
    private Zombie zombieRef; // 僵尸脚本引用
    private List<Vector3> patrolPoints = new List<Vector3>(); // 巡逻点列表
    private int currentPatrolIndex = 0; // 当前巡逻点索引

    private void Awake()
    {
        zombieRef = GetComponent<Zombie>(); // 获取僵尸脚本引用
        if (zombieRef == null)
        {
            Debug.LogError($"[ZombiePatrolState] 未找到僵尸脚本组件: {name}");
            return;
        }

        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform; // 获取玩家对象的Transform组件

        // 初始化巡逻点
        InitializePatrolPoints();
    }

    private void Start()
    {
        if (patrolPoints.Count == 0)
        {
            Debug.LogWarning($"[ZombiePatrolState] {name} 没有找到任何巡逻点，请检查 patrolWaypoints");
            currentState = ZombieState.Idle; // 如果没有巡逻点，设置为闲置状态
            return;
        }

        StartCoroutine(StateMachine()); // 启动状态机协程
    }

    // 初始化巡逻点
    private void InitializePatrolPoints()
    {
        patrolPoints.Clear(); // 清空巡逻点列表

        if (patrolWaypoints != null && patrolWaypoints.Length > 0)
        {
            foreach (Transform waypoint in patrolWaypoints)
            {
                if (waypoint != null)
                {
                    patrolPoints.Add(waypoint.position); // 将巡逻点位置添加到列表中
                }
                else
                {
                    Debug.LogWarning($"[ZombiePatrolState] {name} 的 patrolWaypoints 中存在空引用");
                }
            }
            Debug.Log($"[巡逻点] 已加载 {patrolPoints.Count} 个巡逻点");
        }
        else
        {
            Debug.LogWarning($"[ZombiePatrolState] {name} 的 patrolWaypoints 未设置");
        }
    }

    // 状态机协程
    private IEnumerator StateMachine()
    {
        while (true)
        {
            switch (currentState)
            {
                case ZombieState.Idle:
                    yield return new WaitForSeconds(idleDuration); // 等待闲置时间
                    currentState = ZombieState.Patrol; // 切换到巡逻状态
                    break;

                case ZombieState.Patrol:
                    if (patrolPoints.Count > 0 && zombieRef != null)
                    {
                        Vector3 target = patrolPoints[currentPatrolIndex]; // 获取当前巡逻点目标

                        // 生成到当前巡逻点的路径
                        zombieRef.GeneratePath(target);
                        Debug.Log($"[巡逻] {name} 正在前往 {currentPatrolIndex + 1}/{patrolPoints.Count} 巡逻点");

                        // 检查是否到达巡逻点
                        if ((transform.position - target).sqrMagnitude < 0.25f) // 0.5f 的平方
                        {
                            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Count; // 切换到下一个巡逻点
                            Debug.Log($"[巡逻] {name} 到达 {currentPatrolIndex + 1} 巡逻点");
                        }
                        else
                        {
                            Debug.LogWarning($"[巡逻] {name} 正在前往 {currentPatrolIndex + 1} 巡逻点");
                        }
                    }
                    yield return new WaitForSeconds(patrolInterval); // 等待巡逻间隔时间
                    break;

                case ZombieState.Chase:
                    if (playerTransform != null)
                    {
                        zombieRef.GeneratePath(playerTransform.position); // 生成到玩家位置的路径
                    }
                    yield return null; // 立即继续协程，保持追逐状态
                    break;

                case ZombieState.Hurt:
                    yield return new WaitForSeconds(0.5f); // 等待受伤状态持续时间
                    currentState = patrolPoints.Count > 0 ? ZombieState.Patrol : ZombieState.Idle; // 受伤结束后恢复巡逻或闲置状态
                    break;
            }

            yield return CheckStateTransitions(); // 检查状态转换
        }
    }

    // 检查状态转换
    private IEnumerator CheckStateTransitions()
    {
        // 检查是否进入追逐状态
        if (playerTransform != null)
        {
            float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position); // 计算与玩家的距离

            if (distanceToPlayer < zombieRef.ChaseDistance)
            {
                if (currentState != ZombieState.Chase)
                {
                    Debug.Log($"[状态转换] {name} 进入追逐状态");
                    currentState = ZombieState.Chase; // 切换到追逐状态
                }
            }
            else if (currentState == ZombieState.Chase)
            {
                Debug.Log($"[状态转换] {name} 退出追逐状态");
                currentState = ZombieState.Patrol; // 切换回巡逻状态

                // 重置巡逻点索引
                if (patrolPoints.Count > 0)
                {
                    currentPatrolIndex = 0;
                }
            }
        }

        // 检查受伤状态
        if (zombieRef != null)
        {
            zombieRef.OnHurt.AddListener(() =>
            {
                if (currentState != ZombieState.Hurt)
                {
                    Debug.Log($"[状态转换] {name} 进入受伤状态");
                    currentState = ZombieState.Hurt; // 切换到受伤状态
                }
            });
        }

        yield return null; // 立即继续协程
    }
}