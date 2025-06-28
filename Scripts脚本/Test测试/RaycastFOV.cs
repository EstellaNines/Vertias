using UnityEngine;

public class RaycastFOV : MonoBehaviour 
{
    // --- 公共字段 ---
    public float viewRadius = 5f; // 视野半径，默认值为5单位
    public int viewAngleStep = 20; // 视野扇形划分的步数，默认为20步
    [Range(0, 180)] // 限制viewHalfAngle的取值范围在0到180度之间
    public float viewHalfAngle = 90f; // 视野的半角度，默认为90度
    public LayerMask targetLayer; // 目标层掩码，用于射线检测目标
    public LayerMask obstacleLayer; // 障碍物层掩码，用于射线检测障碍物
    public GameObject eyePoint; // 视野起点的引用，通常是敌人眼睛的位置
    public EnemyDirection enemyDirection; // 敌人方向组件的引用，用于获取和设置敌人朝向

    public GameObject player; // 玩家对象的引用
    public float playerDetectionRadius = 10f; // 玩家检测范围，默认为10单位
    public Color whiteLineColor = Color.white; // 射线颜色，当玩家未潜行时为白色

    private PlayerCrouch playerCrouch; // 玩家潜行动作组件的引用
    private float baseAngleOffset; // 记录初始方向与正右方向的角度偏移
    private bool playerDetected = false; // 玩家检测状态，默认为未检测到

    void Awake() // 在游戏对象实例化完成后立即调用，用于初始化操作
    {
        if (enemyDirection == null) // 检查是否已获取EnemyDirection组件
        {
            enemyDirection = GetComponent<EnemyDirection>(); // 如果未获取，则尝试从当前对象上获取
        }

        Vector2 initialDirection = enemyDirection?.GetCurrentDirection() ?? Vector2.right; // 获取初始方向，如果enemyDirection为空则使用向右方向
        baseAngleOffset = CalculateAngle(initialDirection); // 计算初始方向与正右方向的角度偏移

        if (player != null) // 如果玩家对象存在
        {
            playerCrouch = player.GetComponent<PlayerCrouch>(); // 获取玩家的PlayerCrouch组件
        }
        else
        {
            Debug.LogWarning("未设置玩家对象"); // 如果玩家对象未设置，输出警告信息
        }
    }

    void Update() // 每帧调用一次
    {
        DrawFieldOfView(); // 调用绘制视野方法
    }

    public Vector2 GetPlayerPosition() // 返回玩家的2D位置
    {
        if (player != null) // 如果玩家对象存在
        {
            return player.transform.position; // 返回玩家的3D位置转化为2D位置
        }
        return Vector2.zero; // 不存在则返回原点
    }

    void DrawFieldOfView() // 绘制视野扇形的方法
    {
        Vector2 rayOrigin = eyePoint ? (Vector2)eyePoint.transform.position : (Vector2)transform.position; // 确定射线起点，优先使用eyePoint的位置，否则使用当前对象的位置
        Vector2 currentDirection = enemyDirection?.GetCurrentDirection() ?? Vector2.right; // 获取当前方向，如果enemyDirection为空则使用向右方向
        float directionAngle = CalculateAngle(currentDirection); // 计算当前方向与正右方向的角度
        Vector2 forward = Quaternion.AngleAxis(directionAngle, Vector3.forward) * Vector2.right; // 根据方向角度计算正前方向向量
        Vector2 left = Quaternion.AngleAxis(-90, Vector3.forward) * forward; // 根据正前方向计算左侧方向向量
        Vector2 right = Quaternion.AngleAxis(90, Vector3.forward) * forward; // 根据正前方向计算右侧方向向量
        Vector2 forward_left = Quaternion.AngleAxis(-viewHalfAngle, Vector3.forward) * forward * viewRadius; // 计算视野左侧起始射线的起点向量

        bool playerDetected = false; // 局部变量，用于本次检测中玩家是否被发现的临时状态
        Vector2 playerDirection = Vector2.zero; // 玩家方向向量

        // 绘制白色射线指向玩家（仅在Unity编辑器中可见）
        if (player != null && playerCrouch != null) // 如果玩家和玩家潜行动作组件存在
        {
            Vector2 playerPosition = player.transform.position; // 获取玩家的位置
            float distanceToPlayer = Vector2.Distance(rayOrigin, playerPosition); // 计算到玩家的距离

            if (distanceToPlayer <= playerDetectionRadius) // 如果玩家在检测范围内
            {
                playerDirection = playerPosition - rayOrigin; // 计算玩家相对于视野起点的方向向量
                RaycastHit2D obstacleHit = Physics2D.Raycast(rayOrigin, playerDirection, distanceToPlayer, obstacleLayer); // 向玩家方向射出射线，检测是否有障碍物
                float effectiveRadius = distanceToPlayer; // 初始假设射线可以到达玩家

                if (obstacleHit.collider != null) // 如果射线检测到障碍物
                {
                    effectiveRadius = Vector2.Distance(rayOrigin, obstacleHit.point); // 计算到障碍物的距离
#if UNITY_EDITOR // 在Unity编辑器中绘制黄线表示障碍物
                    Debug.DrawLine(rayOrigin, obstacleHit.point, Color.yellow);
#endif
                }

                // 根据玩家是否潜行来选择射线颜色
                Color lineColor = playerCrouch.IsCrouching() ? Color.black : whiteLineColor;

#if UNITY_EDITOR // 在Unity编辑器中绘制射线
                Debug.DrawLine(rayOrigin, rayOrigin + playerDirection.normalized * effectiveRadius, lineColor);
#endif

                // 如果玩家未潜行且射线几乎直达玩家，则标记为检测到玩家
                if (!playerCrouch.IsCrouching() && effectiveRadius >= distanceToPlayer * 0.99f)
                {
                    playerDetected = true; // 标记为检测到玩家
                    AdjustFOVToTarget(playerDirection); // 调整视野方向朝向玩家

                    Debug.Log("视野内检测到目标"); // 输出日志
                    // 此处可添加检测到玩家后的其他逻辑
                }
            }
        }

        // 将本次检测结果赋值给成员变量，供其他方法使用
        this.playerDetected = playerDetected;

        // 如果检测到玩家，则重新计算视野扇形的中心方向为玩家方向
        if (playerDetected)
        {
            // 使用玩家方向作为中心线重新计算视野方向向量
            forward = playerDirection.normalized; // 将玩家方向向量归一化
            float playerDirectionAngle = CalculateAngle(forward); // 计算玩家方向角度
            forward = Quaternion.AngleAxis(playerDirectionAngle, Vector3.forward) * Vector2.right; // 根据角度重算正前方向向量
            left = Quaternion.AngleAxis(-90, Vector3.forward) * forward; // 重算左侧方向向量
            right = Quaternion.AngleAxis(90, Vector3.forward) * forward; // 重算右侧方向向量
            forward_left = Quaternion.AngleAxis(-viewHalfAngle, Vector3.forward) * forward * viewRadius; // 重算视野左侧起始射线的起点向量
        }

        // 根据视野参数绘制视野扇形
        for (int i = 0; i <= viewAngleStep; i++) // 遍历视野扇形的每个角度步数
        {
            float angleStep = (viewHalfAngle * 2 / viewAngleStep) * i; // 计算当前步数对应的角度偏移
            Vector2 v = Quaternion.AngleAxis(angleStep, Vector3.forward) * forward_left; // 计算当前步数对应的方向向量
            Vector2 pos = rayOrigin + v; // 计算射线终点位置

            RaycastHit2D obstacleHit = Physics2D.Raycast(rayOrigin, v, viewRadius, obstacleLayer); // 射出射线检测障碍物
            float effectiveRadius = viewRadius; // 初始化最大有效射线长度为视野半径

            if (obstacleHit.collider != null) // 如果射线检测到障碍物
            {
                effectiveRadius = Vector2.Distance(rayOrigin, obstacleHit.point); // 计算到障碍物的距离
#if UNITY_EDITOR // 在Unity编辑器中绘制黄线表示障碍物
                Debug.DrawLine(rayOrigin, obstacleHit.point, Color.yellow);
#endif
            }

            RaycastHit2D targetHit = Physics2D.Raycast(rayOrigin, v, effectiveRadius, targetLayer); // 射出射线检测目标

            Color lineColor = Color.blue; // 默认射线颜色为蓝色

            if (targetHit.collider != null && playerCrouch != null && playerCrouch.IsCrouching()) // 如果检测到目标且玩家潜行中
            {
                lineColor = Color.black; // 射线颜色变为黑色表示潜行状态
            }
            else if (obstacleHit.collider != null) // 如果射线检测到障碍物
            {
                lineColor = targetHit.collider ? Color.red : Color.yellow; // 如果同时检测到目标，则为红色，否则为黄色表示障碍物
            }
            else // 如果没有检测到障碍物
            {
                lineColor = targetHit.collider ? Color.green : Color.blue; // 如果检测到目标为绿色，否则为蓝色
            }

#if UNITY_EDITOR // 在Unity编辑器中绘制视野射线
            Debug.DrawLine(rayOrigin, rayOrigin + v.normalized * effectiveRadius, lineColor);
#endif
        }
    }

    // 调整视野方向朝向目标
    private void AdjustFOVToTarget(Vector2 targetDirection)
    {
        float targetAngle = CalculateAngle(targetDirection); // 计算目标方向的角度
        // 更新baseAngleOffset，使视野重新朝向目标
        baseAngleOffset = targetAngle;

        // 如果使用EnemyDirection组件，则直接更新其方向
        if (enemyDirection != null)
        {
            // 将Vector2方向转换为包含Z轴的旋转
            float angle = Mathf.Atan2(targetDirection.y, targetDirection.x) * Mathf.Rad2Deg;
            Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.forward);
            enemyDirection.SetDirection(rotation * Vector2.right); // 更新敌人朝向为旋转后的方向
        }
    }

    // 提供玩家检测状态的公共访问方法
    public bool IsPlayerDetected()
    {
        return playerDetected; // 返回玩家是否被检测到的状态
    }

    // 提供获取玩家潜行动作组件的公共方法
    public PlayerCrouch GetPlayerCrouch()
    {
        return playerCrouch; // 返回玩家的PlayerCrouch组件
    }

    // 计算方向向量与正右方向的夹角，并转换为0-360度范围的值
    private float CalculateAngle(Vector2 direction)
    {
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg; // 计算方向向量与X轴正方向的角度（以弧度为单位）
        return angle < 0 ? angle + 360 : angle; // 将角度转换为0-360度范围
    }
}