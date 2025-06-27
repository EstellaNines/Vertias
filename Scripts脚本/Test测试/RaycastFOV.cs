using UnityEngine;

public class RaycastFOV : MonoBehaviour
{
    public float viewRadius = 5f;
    public int viewAngleStep = 20;
    [Range(0, 180)]
    public float viewHalfAngle = 90f;
    public LayerMask targetLayer;
    public LayerMask obstacleLayer;
    public GameObject eyePoint;
    public EnemyDirection enemyDirection;

    public GameObject player; // 玩家对象
    public float playerDetectionRadius = 10f; // 检测范围
    public Color whiteLineColor = Color.white; // 白色射线颜色

    private PlayerCrouch playerCrouch;
    private float baseAngleOffset;
    // 添加玩家检测状态字段
    private bool playerDetected = false;

    void Awake()
    {
        if (enemyDirection == null)
        {
            enemyDirection = GetComponent<EnemyDirection>();
        }

        Vector2 initialDirection = enemyDirection?.GetCurrentDirection() ?? Vector2.right;
        baseAngleOffset = CalculateAngle(initialDirection);

        if (player != null)
        {
            playerCrouch = player.GetComponent<PlayerCrouch>();
        }
        else
        {
            Debug.LogWarning("未设置玩家对象");
        }
    }

    void Update()
    {
        DrawFieldOfView();
    }

    public Vector2 GetPlayerPosition()
    {
        if (player != null)
        {
            return player.transform.position;
        }
        return Vector2.zero;
    }

    void DrawFieldOfView()
    {
        Vector2 rayOrigin = eyePoint ? (Vector2)eyePoint.transform.position : (Vector2)transform.position;
        Vector2 currentDirection = enemyDirection?.GetCurrentDirection() ?? Vector2.right;
        float directionAngle = CalculateAngle(currentDirection);
        Vector2 forward = Quaternion.AngleAxis(directionAngle, Vector3.forward) * Vector2.right;
        Vector2 left = Quaternion.AngleAxis(-90, Vector3.forward) * forward;
        Vector2 right = Quaternion.AngleAxis(90, Vector3.forward) * forward;
        Vector2 forward_left = Quaternion.AngleAxis(-viewHalfAngle, Vector3.forward) * forward * viewRadius;

        bool playerDetected = false;  // 局部变量重名
        Vector2 playerDirection = Vector2.zero;

        // 绘制白色射线指向玩家
        if (player != null && playerCrouch != null)
        {
            Vector2 playerPosition = player.transform.position;
            float distanceToPlayer = Vector2.Distance(rayOrigin, playerPosition);

            if (distanceToPlayer <= playerDetectionRadius)
            {
                playerDirection = playerPosition - rayOrigin;
                RaycastHit2D obstacleHit = Physics2D.Raycast(rayOrigin, playerDirection, distanceToPlayer, obstacleLayer);
                float effectiveRadius = distanceToPlayer;

                if (obstacleHit.collider != null)
                {
                    effectiveRadius = Vector2.Distance(rayOrigin, obstacleHit.point);
#if UNITY_EDITOR
                    Debug.DrawLine(rayOrigin, obstacleHit.point, Color.yellow);
#endif
                }

                // 根据潜行状态决定射线颜色
                Color lineColor = playerCrouch.IsCrouching() ? Color.black : whiteLineColor;

#if UNITY_EDITOR
                Debug.DrawLine(rayOrigin, rayOrigin + playerDirection.normalized * effectiveRadius, lineColor);
#endif

                // 如果未潜行且无障碍物遮挡，触发目标检测逻辑
                if (!playerCrouch.IsCrouching() && effectiveRadius >= distanceToPlayer * 0.99f)
                {
                    playerDetected = true;
                    AdjustFOVToTarget(playerDirection);

                    Debug.Log("视野内检测到目标");
                    // 触发自定义逻辑
                }
            }
        }

        // 将局部变量值赋给成员变量
        this.playerDetected = playerDetected;

        // 根据检测状态选择不同的扇形视野绘制逻辑
        if (playerDetected)
        {
            // 使用玩家方向作为中心线重新计算扇形视野
            forward = playerDirection.normalized;
            float playerDirectionAngle = CalculateAngle(forward);
            forward = Quaternion.AngleAxis(playerDirectionAngle, Vector3.forward) * Vector2.right;
            left = Quaternion.AngleAxis(-90, Vector3.forward) * forward;
            right = Quaternion.AngleAxis(90, Vector3.forward) * forward;
            forward_left = Quaternion.AngleAxis(-viewHalfAngle, Vector3.forward) * forward * viewRadius;
        }

        // 原有扇形视野逻辑（根据是否检测到玩家使用不同的方向参数）
        for (int i = 0; i <= viewAngleStep; i++)
        {
            float angleStep = (viewHalfAngle * 2 / viewAngleStep) * i;
            Vector2 v = Quaternion.AngleAxis(angleStep, Vector3.forward) * forward_left;
            Vector2 pos = rayOrigin + v;

            RaycastHit2D obstacleHit = Physics2D.Raycast(rayOrigin, v, viewRadius, obstacleLayer);
            float effectiveRadius = viewRadius;

            if (obstacleHit.collider != null)
            {
                effectiveRadius = Vector2.Distance(rayOrigin, obstacleHit.point);
#if UNITY_EDITOR
                Debug.DrawLine(rayOrigin, obstacleHit.point, Color.yellow);
#endif
            }

            RaycastHit2D targetHit = Physics2D.Raycast(rayOrigin, v, effectiveRadius, targetLayer);

            Color lineColor = Color.blue;

            if (targetHit.collider != null && playerCrouch != null && playerCrouch.IsCrouching())
            {
                lineColor = Color.black;
            }
            else if (obstacleHit.collider != null)
            {
                lineColor = targetHit.collider ? Color.red : Color.yellow;
            }
            else
            {
                lineColor = targetHit.collider ? Color.green : Color.blue;
            }

#if UNITY_EDITOR
            Debug.DrawLine(rayOrigin, rayOrigin + v.normalized * effectiveRadius, lineColor);
#endif
        }
    }

    // 动态调整视野方向朝向目标
    private void AdjustFOVToTarget(Vector2 targetDirection)
    {
        float targetAngle = CalculateAngle(targetDirection);
        // 保持原有方向偏移量，更新为朝向目标的角度
        baseAngleOffset = targetAngle;

        // 如果使用EnemyDirection组件，直接更新方向
        if (enemyDirection != null)
        {
            // 将Vector2方向转换为包含Z轴的旋转
            float angle = Mathf.Atan2(targetDirection.y, targetDirection.x) * Mathf.Rad2Deg;
            Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.forward);
            enemyDirection.SetDirection(rotation * Vector2.right);
        }
    }

    // 新增公共方法获取玩家检测状态
    public bool IsPlayerDetected()
    {
        return playerDetected;
    }

    // 获取玩家潜行组件
    public PlayerCrouch GetPlayerCrouch()
    {
        return playerCrouch;
    }

    private float CalculateAngle(Vector2 direction)
    {
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        return angle < 0 ? angle + 360 : angle;
    }
}