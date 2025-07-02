// ... existing code ...
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;

public class EnemyPatrol : MonoBehaviour
{
    [Header("巡逻配置")]
    public Transform[] PatrolPoints; // 巡逻点集合
    public float pathUpdateInterval = 0.5f; // 路径更新间隔
    public float reachThreshold = 0.1f; // 到达路径点阈值

    [Header("组件引用")]
    private Seeker seeker; // A*寻路组件
    private Rigidbody2D rb; // 刚体
    private Animator animator; // 动画器
    private List<Vector3> pathPoints = new List<Vector3>(); // 路径点列表
    private int currentPathIndex = 0; // 当前路径索引
    private Vector2 movementDirection; // 移动方向
    private float speed = 2f; // 移动速度
    private float defaultSpeed;
    private EnemyDirection enemyDirection;

    // 新增字段
    private int targetPointsIndex = 0; // 当前目标巡逻点索引

    void Awake()
    {
        // 获取必要组件
        seeker = GetComponent<Seeker>();
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        enemyDirection = GetComponent<EnemyDirection>(); // 方向器获取

        // 初始化默认速度
        defaultSpeed = speed;

        // 初始化路径点
        if (PatrolPoints != null && PatrolPoints.Length > 0)
        {
            // 随机初始目标点
            targetPointsIndex = Random.Range(0, PatrolPoints.Length);
            GenerateNewPath(PatrolPoints[targetPointsIndex].position); // 初始路径
        }
    }

    void Start()
    {
        // 启动路径更新协程
        StartCoroutine(UpdatePathRoutine());
    }

    void FixedUpdate()
    {
        FollowPath();
    }

    IEnumerator UpdatePathRoutine()
    {
        while (true)
        {
            if (PatrolPoints != null && PatrolPoints.Length > 0 &&
                currentPathIndex < PatrolPoints.Length)
            {
                // 检查是否需要更换目标点
                if (currentPathIndex >= pathPoints.Count)
                {
                    // 到达终点后选择新的目标点
                    UpdateTargetPointIndex();
                    GenerateNewPath(PatrolPoints[targetPointsIndex].position);
                }
            }
            yield return new WaitForSeconds(pathUpdateInterval);
        }
    }

    // 获取当前速度
    public float GetSpeed()
    {
        return speed;
    }

    // 获取默认速度
    public float GetDefaultSpeed()
    {
        return defaultSpeed;
    }

    // 设置当前速度
    public void SetSpeed(float newSpeed)
    {
        speed = newSpeed;

        // 如果当前有移动方向，立即应用新速度
        if (currentPathIndex < pathPoints.Count && pathPoints.Count > 0)
        {
            rb.velocity = movementDirection * speed;
        }
    }

    void GenerateNewPath(Vector3 targetPosition)
    {
        if (seeker == null) return;

        seeker.StartPath(transform.position, targetPosition, path =>
        {
            if (path.error)
            {
                Debug.LogWarning($"路径生成失败: 路径无效");
                // 路径生成失败时尝试重新生成
                UpdateTargetPointIndex();
                GenerateNewPath(PatrolPoints[targetPointsIndex].position);
                return;
            }

            // 过滤连续重复的路径点
            List<Vector3> filteredPath = new List<Vector3>();
            foreach (var point in path.vectorPath)
            {
                // 如果列表为空或当前点与上一点不同，则添加
                if (filteredPath.Count == 0 ||
                    Vector2.Distance(point, filteredPath[filteredPath.Count - 1]) > 0.01f)
                {
                    filteredPath.Add(point);
                }
            }

            pathPoints = filteredPath;
            currentPathIndex = 0;

            // 如果过滤后路径为空，尝试重新生成
            if (pathPoints.Count <= 0)
            {
                GenerateNewPath(targetPosition);
            }
        });
    }

    void UpdateTargetPointIndex()
    {
        // 生成新的目标点索引（确保不重复）
        int oldIndex = targetPointsIndex;
        while (true)
        {
            targetPointsIndex = Random.Range(0, PatrolPoints.Length);
            if (targetPointsIndex != oldIndex || PatrolPoints.Length == 1)
            {
                break;
            }
        }
    }

    void FollowPath()
    {
        if (pathPoints == null || pathPoints.Count == 0) return;

        // 到达当前路径点的判定
        if (currentPathIndex < pathPoints.Count &&
            Vector2.Distance(transform.position, pathPoints[currentPathIndex]) <= reachThreshold)
        {
            currentPathIndex++;

            // 如果到达路径终点
            if (currentPathIndex >= pathPoints.Count)
            {
                // 重置路径索引
                currentPathIndex = 0;

                // 更新目标点并重新生成路径
                UpdateTargetPointIndex();
                GenerateNewPath(PatrolPoints[targetPointsIndex].position);
                return;
            }
        }

        // 移动逻辑
        if (currentPathIndex < pathPoints.Count)
        {
            // 获取当前目标点方向
            movementDirection = (pathPoints[currentPathIndex] - transform.position).normalized;

            // 如果方向为零向量（可能卡住），尝试重新生成路径
            if (movementDirection.sqrMagnitude < 0.1f)
            {
                UpdateTargetPointIndex();
                GenerateNewPath(PatrolPoints[targetPointsIndex].position);
                return;
            }

            rb.velocity = movementDirection * speed;

            // 更新方向器方向
            if (enemyDirection != null)
            {
                enemyDirection.SetDirection(movementDirection);
            }
        }
        else
        {
            rb.velocity = Vector2.zero;

            // 停止移动时重置方向器
            if (enemyDirection != null)
            {
                enemyDirection.ResetToAutoDetection();
            }
        }
    }
}