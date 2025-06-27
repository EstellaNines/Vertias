using UnityEngine;

public class EnemyDirection : MonoBehaviour
{
    [Header("方向参数")]
    [Tooltip("方向变化的最小速度阈值")]
    [SerializeField] private float directionThreshold = 0.1f;

    // 当前方向（-1, 0, +1）
    private Vector2 currentDirection = Vector2.right;

    // 上一帧速度方向
    private Vector2 lastVelocityDirection = Vector2.right;

    private Rigidbody2D rb;
    private Vector2 velocity;
    private bool useManualDirection = false; // 是否使用手动方向控制
    private Vector2 manualDirection = Vector2.right; // 手动设置的方向

    void Awake()
    {
        // 获取刚体组件
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            Debug.LogError($"未找到Rigidbody2D组件：{name}");
        }
    }

    void Start()
    {
        // 初始方向设置
        UpdateDirection();
    }

    void FixedUpdate()
    {
        if (!useManualDirection)
        {
            // 从刚体获取速度
            velocity = rb.velocity;
        }

        // 更新方向
        UpdateDirection();
    }

    void UpdateDirection()
    {
        if (useManualDirection)
        {
            // 使用手动设置的方向
            currentDirection = manualDirection.normalized;
            if (currentDirection.sqrMagnitude > 0)
            {
                lastVelocityDirection = currentDirection;
            }
        }
        else
        {
            // 获取当前速度方向
            Vector2 velocityDirection = velocity.normalized;

            // 只有当速度超过阈值时才更新方向
            if (velocity.sqrMagnitude > directionThreshold * directionThreshold)
            {
                // 水平方向判断
                if (Mathf.Abs(velocityDirection.x) > directionThreshold)
                {
                    currentDirection.x = Mathf.Sign(velocityDirection.x);
                }

                // 垂直方向判断
                if (Mathf.Abs(velocityDirection.y) > directionThreshold)
                {
                    currentDirection.y = Mathf.Sign(velocityDirection.y);
                }

                // 记录有效方向
                lastVelocityDirection = currentDirection;
            }
            else
            {
                // 速度为0时保持最后有效方向
                currentDirection = lastVelocityDirection;
            }
        }

        // 确保方向向量标准化
        if (currentDirection.sqrMagnitude > 0)
        {
            currentDirection.Normalize();
        }
    }

    /// <summary>
    /// 手动设置移动方向
    /// </summary>
    /// <param name="direction">归一化方向向量</param>
    public void SetDirection(Vector2 direction)
    {
        useManualDirection = true;
        manualDirection = direction.normalized;
    }

    /// <summary>
    /// 恢复自动方向检测
    /// </summary>
    public void ResetToAutoDetection()
    {
        useManualDirection = false;
    }

    /// <summary>
    /// 获取当前移动方向
    /// </summary>
    /// <returns>方向向量（x: -1/0/+1, y: -1/0/+1）</returns>
    public Vector2 GetCurrentDirection()
    {
        return currentDirection;
    }

    // 可视化调试
    void OnDrawGizmos()
    {
        if (!useManualDirection && rb != null)
        {
            velocity = rb.velocity;
        }

        // 绘制方向箭头
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + (Vector3)currentDirection * 0.5f);

        // 显示方向值
        Debug.Log($"当前方向: ({currentDirection.x:F1}, {currentDirection.y:F1})", this);
    }
}