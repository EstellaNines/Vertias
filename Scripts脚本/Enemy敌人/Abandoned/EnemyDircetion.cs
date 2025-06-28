using UnityEngine;

// 定义EnemyDirection类，用于记录敌人方向
public class EnemyDirection : MonoBehaviour
{
    [Header("方向参数")] 
    [Tooltip("方向变化的最小速度阈值")]
    [SerializeField] private float directionThreshold = 0.1f; // 方向变化的最小速度阈值，默认为0.1

    // 当前移动方向，存储为归一化向量（-1, 0, +1）
    private Vector2 currentDirection = Vector2.right;

    // 上一帧的速度方向，用于在速度为零时保持最后的有效方向
    private Vector2 lastVelocityDirection = Vector2.right;

    // 刚体组件引用，用于获取速度信息
    private Rigidbody2D rb;

    // 当前速度，用于计算方向
    private Vector2 velocity;

    // 是否使用手动方向控制的标志
    private bool useManualDirection = false;

    // 手动设置的方向向量
    private Vector2 manualDirection = Vector2.right;

    void Awake() // 在游戏对象实例化后立即调用
    {
        // 获取刚体组件
        rb = GetComponent<Rigidbody2D>();
        // 如果未找到刚体组件，输出错误信息
        if (rb == null)
        {
            Debug.LogError($"未找到Rigidbody2D组件：{name}");
        }
    }

    void Start() // 在第一次帧更新前调用
    {
        // 调用UpdateDirection方法，初始化方向
        UpdateDirection();
    }

    void FixedUpdate() // 固定频率调用，通常用于物理相关更新
    {
        // 如果不是手动控制方向
        if (!useManualDirection)
        {
            // 从刚体组件获取当前速度
            velocity = rb.velocity;
        }

        // 更新方向信息
        UpdateDirection();
    }

    void UpdateDirection() // 更新方向的核心方法
    {
        // 根据是否使用手动方向控制，选择不同的方向更新逻辑
        if (useManualDirection)
        {
            // 使用手动设置的方向
            currentDirection = manualDirection.normalized;

            // 如果方向向量有效（非零），更新lastVelocityDirection
            if (currentDirection.sqrMagnitude > 0)
            {
                lastVelocityDirection = currentDirection;
            }
        }
        else
        {
            // 获取当前速度方向的归一化向量
            Vector2 velocityDirection = velocity.normalized;

            // 只有当速度超过阈值时才更新方向
            if (velocity.sqrMagnitude > directionThreshold * directionThreshold)
            {
                // 水平方向判断：如果水平速度分量超过阈值，更新水平方向
                if (Mathf.Abs(velocityDirection.x) > directionThreshold)
                {
                    currentDirection.x = Mathf.Sign(velocityDirection.x);
                }

                // 垂直方向判断：如果垂直速度分量超过阈值，更新垂直方向
                if (Mathf.Abs(velocityDirection.y) > directionThreshold)
                {
                    currentDirection.y = Mathf.Sign(velocityDirection.y);
                }

                // 记录当前有效方向
                lastVelocityDirection = currentDirection;
            }
            else
            {
                // 速度为零时，保持最后的有效方向
                currentDirection = lastVelocityDirection;
            }
        }

        // 确保方向向量为归一化向量（长度为1）
        if (currentDirection.sqrMagnitude > 0)
        {
            currentDirection.Normalize();
        }
    }

    /// <summary>
    /// 手动设置移动方向
    /// </summary>
    /// <param name="direction">需要归一化的方向向量</param>
    public void SetDirection(Vector2 direction)
    {
        // 设置标志为使用手动方向控制
        useManualDirection = true;
        // 将输入方向向量归一化后赋值给manualDirection
        manualDirection = direction.normalized;
    }

    /// <summary>
    /// 恢复自动方向检测
    /// </summary>
    public void ResetToAutoDetection()
    {
        // 设置标志为不使用手动方向控制
        useManualDirection = false;
    }

    /// <summary>
    /// 获取当前移动方向
    /// </summary>
    /// <returns>方向向量（x: -1/0/+1, y: -1/0/+1）</returns>
    public Vector2 GetCurrentDirection()
    {
        // 返回当前方向向量
        return currentDirection;
    }

    // 在Unity编辑器中可视化调试信息
    void OnDrawGizmos()
    {
        // 如果不是手动控制方向且刚体组件存在，获取速度
        if (!useManualDirection && rb != null)
        {
            velocity = rb.velocity;
        }

        // 绘制方向箭头，颜色为青色
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(
            transform.position, // 起点为敌人当前位置
            transform.position + (Vector3)currentDirection * 0.5f // 终点为当前位置加上方向向量（放大0.5倍）
        );

        // 输出当前方向信息到控制台，格式化保留一位小数
        Debug.Log(
            $"当前方向: ({currentDirection.x:F1}, {currentDirection.y:F1})",
            this // 关联当前脚本实例
        );
    }
}