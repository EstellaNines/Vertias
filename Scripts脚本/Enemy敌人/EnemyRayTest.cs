using System.Collections;
using UnityEngine;

public class EnemyRayTest : MonoBehaviour
{
    [Header("角色设置")]
    [SerializeField] private Transform player;
    [SerializeField] private float viewRadius = 10f; // 扇形半径
    [Range(0, 360)][SerializeField] private float fieldOfViewAngle = 90f; // 扇形角度
    [SerializeField] private int rayCount = 8; // 射线数量
    [SerializeField] private LayerMask playerLayer;

    private CapsuleCollider2D enemyCollider;

    private void Awake()
    {
        // 查找玩家对象
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
        }
        else
        {
            Debug.LogError("未找到玩家对象");
        }

        // 获取碰撞器组件
        enemyCollider = GetComponent<CapsuleCollider2D>();
        if (enemyCollider == null)
        {
            Debug.LogError("敌人缺少碰撞器组件");
        }
    }

    private void FixedUpdate()
    {
        if (player == null || enemyCollider == null) return;

        // 获取几何中心
        Vector3 boundsCenter = enemyCollider.bounds.center;

        // 计算指向玩家的角度（以敌人中心为起点）
        Vector2 directionToPlayer = (player.position - boundsCenter).normalized;
        float angleToPlayer = Mathf.Atan2(directionToPlayer.y, directionToPlayer.x) * Mathf.Rad2Deg;

        // 计算扇形起始角度（围绕玩家方向展开）
        float startAngle = angleToPlayer - fieldOfViewAngle / 2;
        float endAngle = angleToPlayer + fieldOfViewAngle / 2;

        // 计算角度步长
        float angleStep = fieldOfViewAngle / (rayCount - 1);

        // 标记是否检测到玩家
        bool detected = false;

        // 发射扇形射线
        for (int i = 0; i < rayCount; i++)
        {
            // 计算当前射线角度
            float currentAngle = startAngle + i * angleStep;

            // 转换为方向向量（角度转弧度）
            Vector2 direction = new Vector2(
                Mathf.Cos(Mathf.Deg2Rad * currentAngle),
                Mathf.Sin(Mathf.Deg2Rad * currentAngle)
            );

            // 发射射线
            RaycastHit2D hit = Physics2D.Raycast(boundsCenter, direction, viewRadius, playerLayer);

            // 可视化调试
            Color rayColor = Color.red;
            if (hit.collider?.CompareTag("Player") == true)
            {
                rayColor = Color.green;
                detected = true;
            }
            Debug.DrawRay(boundsCenter, direction * viewRadius, rayColor);
        }

        // 检测到玩家时的逻辑
        if (detected)
        {
            Debug.Log("扇形区域内发现玩家");
        }
    }

    // 在Scene视图可视化扇形区域
    private void OnDrawGizmos()
    {
        if (Application.isPlaying && enemyCollider != null && player != null)
        {
            Vector3 boundsCenter = enemyCollider.bounds.center;
            Vector3 directionToPlayer = (player.position - boundsCenter).normalized;
            float angleToPlayer = Mathf.Atan2(directionToPlayer.y, directionToPlayer.x) * Mathf.Rad2Deg;

            // 绘制检测范围圆
            Gizmos.color = new Color(0, 1, 0, 0.1f);
            Gizmos.DrawWireSphere(boundsCenter, viewRadius);

            // 绘制扇形区域
            DrawFieldOfView(boundsCenter, angleToPlayer, fieldOfViewAngle, viewRadius, Color.yellow);
        }
    }

    // 绘制扇形区域
    private void DrawFieldOfView(Vector3 center, float angle, float fieldOfView, float radius, Color color)
    {
        float segments = 20; // 绘制精度
        float halfFOV = fieldOfView / 2;

        Vector3 fromVector = Quaternion.Euler(0, 0, angle - halfFOV) * Vector3.right;
        Vector3 toVector = Quaternion.Euler(0, 0, angle + halfFOV) * Vector3.right;

        // 绘制扇形边线
        Gizmos.color = color;
        Gizmos.DrawLine(center, center + fromVector * radius);
        Gizmos.DrawLine(center, center + toVector * radius);

        // 绘制扇形弧线
        Vector3 previousPoint = center + fromVector * radius;
        for (int i = 1; i <= segments; i++)
        {
            float angleStep = (fieldOfView / segments) * i;
            Vector3 nextPoint = center + Quaternion.Euler(0, 0, angle - halfFOV + angleStep) * Vector3.right * radius;
            Gizmos.DrawLine(previousPoint, nextPoint);
            previousPoint = nextPoint;
        }
    }
}