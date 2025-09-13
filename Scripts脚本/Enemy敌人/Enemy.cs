using System.Collections.Generic;
using UnityEngine;
using Pathfinding;
using UnityEngine.Events;

// 敌人状态枚举
public enum EnemyState
{
    Idle/*待机*/, Move/*移动*/, Attack/*攻击*/, Hurt/*受伤*/, Dead/*死亡*/, Patrol/*巡逻*/, Aim/*瞄准*/
}
public class Enemy : MonoBehaviour
{
    // --- 基础属性 ---
    [Header("生命值")]
    [FieldLabel("生命值")] public float Health = 100f; // 生命值
    [HideInInspector] public float currentHealth;



    [Header("移动")]
    [FieldLabel("移动速度")] public float MoveSpeed = 3f; // 移动速度
    [HideInInspector] public float currentSpeed; // 当前速度

    // --- 巡逻相关属性 ---
    [Header("巡逻")]
    [FieldLabel("巡逻点")] public Transform[] PatrolPoints; // 巡逻点数组
    [FieldLabel("巡逻点索引")] public int currentPatrolIndex = 0; // 当前巡逻点索引
    [FieldLabel("路径更新间隔")] public float pathUpdateInterval = 0.5f; // 路径更新间隔
    [FieldLabel("到达距离")] public float reachDistance = 0.5f; // 到达目标点的距离阈值
    [FieldLabel("待机时间")] public float idleTime = 4f; // 到达巡逻点后的待机时间
    [HideInInspector] public Path currentPath; // 当前路径
    [HideInInspector] public int currentWaypoint = 0; // 当前路径点索引
    [HideInInspector] public bool reachedEndOfPath = false; // 是否到达路径终点
    [HideInInspector] public bool shouldPatrol = true; // 是否应该继续巡逻

    // --- 射击相关属性 ---
    [Header("射击")]
    [FieldLabel("武器对象")] public Transform weapon;
    [FieldLabel("射击点")] public Transform firePoint; // 添加射击点变量
    [HideInInspector] public WeaponManager weaponController; // 添加武器控制器引用
    [FieldLabel("射击间隔")] public float fireRate = 0.33f; // 射击间隔，单位为秒，表示两次射击之间的时间
    [HideInInspector] public float nextFireTime = 0f; // 下次射击时间
    [FieldLabel("子弹对象池")] public EnemyBulletPool bulletPool; // 子弹对象池引用

    // --- 视野检测相关属性 ---
    [Header("视野检测")]
    [FieldLabel("视野半径")] public float viewRadius = 5f; // 视野半径
    [FieldLabel("视野角度")][Range(0, 180)] public float viewHalfAngle = 90f; // 视野半角度
    [FieldLabel("扇形边数")] public int viewAngleStep = 20; // 视野扇形划分的步数
    [FieldLabel("目标层")] public LayerMask targetLayer; // 目标层
    [FieldLabel("障碍物层")] public LayerMask obstacleLayer; // 障碍物层
    [FieldLabel("视野起点")] public GameObject eyePoint; // 视野起点
    [FieldLabel("玩家检测半径")] public float playerDetectionRadius = 10f; // 玩家检测范围
    [FieldLabel("固定视野朝向")] public bool useFixedViewDirection = false; // 是否固定视野朝向
    public enum ViewFacing { Right, Left, Up, Down }
    [FieldLabel("视野朝向")] public ViewFacing fixedViewDirection = ViewFacing.Right; // 固定视野朝向选择
    [FieldLabel("射线检测颜色")] public Color RayDetectionColor = Color.red; // 自定义可视化颜色
    [HideInInspector] public bool playerDetected = false; // 是否检测到玩家
    [HideInInspector] public GameObject player; // 玩家引用
    [HideInInspector] public Vector2 lastKnownPlayerPosition; // 玩家最后已知位置
    [HideInInspector] public bool wasFollowingPath = false; // 是否之前正在跟随路径

    // --- 方向控制相关属性 ---
    [Header("方向控制")]
    [FieldLabel("方向变化阈值")] public float directionThreshold = 0.1f; // 方向变化的最小速度阈值
    private Vector2 currentDirection = Vector2.right; // 当前移动方向
    private Vector2 lastVelocityDirection = Vector2.right; // 上一帧的速度方向
    private bool useManualDirection = false; // 是否使用手动方向控制
    private Vector2 manualDirection = Vector2.right; // 手动设置的方向向量

    // --- 受伤 ---
    public UnityEvent OnHurt; // 受伤事件
    [HideInInspector] public bool isHurt; // 是否受伤

    // --- 死亡 ---
    public UnityEvent OnDeath; // 死亡事件
    [HideInInspector] public bool isDead; // 是否死亡

    // --- 内部属性 --- 
    [HideInInspector] public Rigidbody2D RB; // 刚体
    [HideInInspector] public Animator animator; // 动画器
    [HideInInspector] public Collider2D Collider; // 碰撞器
    [HideInInspector] public Seeker seeker; // 寻路
    private IState currentState; // 当前状态
    [HideInInspector] public Dictionary<EnemyState, IState> states = new Dictionary<EnemyState, IState>(); // 状态字典（改为public）

    // --- 函数 ---
    private void Awake()
    {
        // 组件引用
        RB = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        Collider = GetComponent<Collider2D>();
        seeker = GetComponent<Seeker>();

        // 初始化生命值
        currentHealth = Health;

        // 查找并初始化武器控制器
        InitializeWeaponController();

        // 查找玩家
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
            // 移除PlayerCrouch组件的获取，因为现在使用状态机
            if (player == null)
            {
                Debug.LogWarning("未找到玩家对象");
            }
        }

        // 确保射击点存在
        if (firePoint == null)
        {
            Debug.LogWarning("未设置射击点，将使用敌人位置作为射击点");
            // 可以在这里创建一个射击点
        }

        InitalStateMachine(); // 初始化状态机
        transitionState(EnemyState.Patrol); // 默认为巡逻状态
    }

    // 添加新方法：初始化武器控制器
    private void InitializeWeaponController()
    {
        // 如果已经手动设置了武器控制器，则跳过自动查找
        if (weaponController != null)
        {
            Debug.Log($"敌人 {gameObject.name} 已手动配置武器控制器: {weaponController.name}");
            return;
        }

        // 在子对象中查找带有WeaponManager组件的武器
        WeaponManager[] weaponManagers = GetComponentsInChildren<WeaponManager>();
        
        if (weaponManagers.Length > 0)
        {
            weaponController = weaponManagers[0]; // 使用第一个找到的武器
            
            // 设置武器的持有者为敌人
            weaponController.SetOwner(this.transform, false); // false表示不是玩家
            
            // 自动设置weapon引用为武器控制器的Transform
            weapon = weaponController.transform;
            
            // 自动查找武器的Muzzle子对象并设置为firePoint
            Transform muzzleTransform = weaponController.transform.Find("Muzzle");
            if (muzzleTransform != null)
            {
                firePoint = muzzleTransform;
                Debug.Log($"敌人 {gameObject.name} 自动找到射击点: {firePoint.name}");
            }
            else
            {
                // 如果没有找到Muzzle，检查WeaponManager的muzzle属性
                if (weaponController.muzzle != null)
                {
                    firePoint = weaponController.muzzle;
                    Debug.Log($"敌人 {gameObject.name} 使用武器管理器的射击点: {firePoint.name}");
                }
                else
                {
                    Debug.LogWarning($"敌人 {gameObject.name} 的武器 {weaponController.name} 中未找到Muzzle射击点！");
                }
            }
            
            Debug.Log($"敌人 {gameObject.name} 自动配置武器: {weapon.name}, 武器控制器: {weaponController.name}");
        }
        else
        {
            Debug.LogWarning($"敌人 {gameObject.name} 的子对象中未找到WeaponManager组件！");
        }
    }

    // 初始化状态
    private void InitalStateMachine()
    {
        // 创建状态
        states.Add(EnemyState.Idle, new EnemyIdleState(this)); // 待机状态
        states.Add(EnemyState.Move, new EnemyMoveState(this)); // 移动状态
        states.Add(EnemyState.Attack, new EnemyAttackState(this)); // 攻击状态
        states.Add(EnemyState.Hurt, new EnemyHurtState(this)); // 受伤状态
        states.Add(EnemyState.Dead, new EnemyDeathState(this)); // 死亡状态
        states.Add(EnemyState.Patrol, new EnemyPatrolState(this)); // 巡逻状态
        states.Add(EnemyState.Aim, new EnemyAimState(this)); // 瞄准状态
    }

    // 状态转换
    public void transitionState(EnemyState type)
    {
        // 当前状态不为空，退出当前状态
        if (currentState != null)
        {
            currentState.OnExit();
        }
        // 通过字典的键找到对应的状态
        currentState = states[type];
        currentState.OnEnter();
    }

    // 播放动画
    public void PlayAnimation(string animName)
    {
        if (animator != null)
        {
            animator.Play(animName);
        }
    }

    private void Update()
    {
        // 更新方向
        UpdateDirection();

        // 检测玩家
        DetectPlayer();

        // 更新当前状态
        if (currentState != null)
        {
            currentState.OnUpdate();
        }
    }

    private void FixedUpdate()
    {
        if (currentState != null)
        {
            currentState.OnFixedUpdate();
        }
    }

    // 获取当前状态
    public EnemyState GetCurrentState()
    {
        // 遍历状态字典，查找当前状态对应的枚举值
        foreach (var pair in states)
        {
            if (pair.Value == currentState)
            {
                return pair.Key;
            }
        }

        // 默认返回Idle状态
        return EnemyState.Idle;
    }

    #region 射击
    // 射击方法
    public void Shoot()
    {
        if (weaponController != null)
        {
            // 直接使用武器控制器的单发逻辑，内部自带射速节流
            weaponController.FireSingle();
        }
        else
        {
            Debug.LogWarning($"敌人 {gameObject.name} 没有配置武器控制器！");
        }
    }
    
    // 旧逻辑已移除：不再通过短暂 SetFiring 控制连发，由 FireSingle 负责节流
    #endregion
    #region 检测玩家
    // 检查玩家是否被检测到
    public bool IsPlayerDetected()
    {
        return playerDetected;
    }

    // 获取玩家位置
    public Vector2 GetPlayerPosition()
    {
        if (player != null)
        {
            return player.transform.position;
        }
        return Vector2.zero;
    }

    // 检查玩家是否在潜行状态
    public bool IsPlayerCrouching()
    {
        if (player != null)
        {
            Player playerComponent = player.GetComponent<Player>();
            if (playerComponent != null)
            {
                return playerComponent.IsCrouching();
            }
        }
        return false;
    }


    // 设置方向
    public void SetDirection(Vector2 direction)
    {
        useManualDirection = true;
        manualDirection = direction.normalized;
        currentDirection = manualDirection;

        // 不再在这里控制武器旋转，武器旋转由EnemyWeaponController处理
    }

    // 恢复自动方向检测
    public void ResetToAutoDetection()
    {
        useManualDirection = false;
    }

    // 获取当前方向
    public Vector2 GetCurrentDirection()
    {
        return currentDirection;
    }

    // 更新方向
    private void UpdateDirection()
    {
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
        else if (RB != null)
        {
            // 获取当前速度方向的归一化向量
            Vector2 velocity = RB.velocity;
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

    // 检查玩家是否已死亡
    public bool IsPlayerDead()
    {
        if (player != null)
        {
            Player playerComponent = player.GetComponent<Player>();
            if (playerComponent != null)
            {
                return playerComponent.isDead;
            }
        }
        return false;
    }

    // 视野检测
    private void DetectPlayer()
    {
        // 如果敌人已死亡，禁用玩家检测
        if (isDead)
        {
            playerDetected = false;
            return;
        }
        if (player == null) return;

        // 检查玩家是否已死亡
        if (IsPlayerDead())
        {
            playerDetected = false;
            // 如果当前在攻击或瞄准状态，切换回巡逻
            EnemyState currentState = GetCurrentState();
            if (currentState == EnemyState.Aim || currentState == EnemyState.Attack)
            {
                shouldPatrol = true;
                transitionState(EnemyState.Patrol);
            }
            Debug.Log("玩家已死亡，敌人停止检测");
            return;
        }

        Vector2 rayOrigin = eyePoint ? (Vector2)eyePoint.transform.position : (Vector2)transform.position;
        Vector2 playerPosition = player.transform.position;
        float distanceToPlayer = Vector2.Distance(rayOrigin, playerPosition);

        // 检查玩家是否在检测范围内
        if (distanceToPlayer <= playerDetectionRadius)
        {
            Vector2 directionToPlayer = (playerPosition - rayOrigin).normalized;
            float angleToPlayer = Vector2.Angle(GetViewForward(), directionToPlayer);

            // 检查玩家是否在视野角度内
            if (angleToPlayer <= viewHalfAngle)
            {
                // 发射射线检查是否有障碍物阻挡
                RaycastHit2D hit = Physics2D.Raycast(rayOrigin, directionToPlayer, distanceToPlayer, obstacleLayer);

                // 如果没有障碍物阻挡
                if (hit.collider == null)
                {
                    // 如果玩家未潜行，正常检测和攻击
                    if (!IsPlayerCrouching())
                    {
                        playerDetected = true;
                        lastKnownPlayerPosition = playerPosition; // 记录玩家最后已知位置

                        // 如果当前状态是巡逻或移动，记录正在跟随路径
                        EnemyState currentState = GetCurrentState();
                        if (currentState == EnemyState.Patrol || currentState == EnemyState.Move)
                        {
                            wasFollowingPath = true;
                        }

                        // 立即切换到瞄准状态
                        if (currentState != EnemyState.Aim && currentState != EnemyState.Attack)
                        {
                            transitionState(EnemyState.Aim);
                        }

                        return;
                    }
                    // 如果玩家潜行，敌人检测到但不攻击，继续巡逻
                    else
                    {
                        // 确保敌人不会因为检测到潜行玩家而改变状态
                        // 如果当前在攻击或瞄准状态，返回巡逻状态
                        EnemyState currentState = GetCurrentState();
                        if (currentState == EnemyState.Aim || currentState == EnemyState.Attack)
                        {
                            shouldPatrol = true;
                            transitionState(EnemyState.Patrol);
                        }
                        return;
                    }
                }
            }
        }

        // 如果之前检测到玩家，但现在没有检测到，且当前状态是瞄准或攻击
        if (playerDetected)
        {
            playerDetected = false;

            // 如果当前状态是瞄准或攻击，且之前正在跟随路径，返回到巡逻状态
            EnemyState currentState = GetCurrentState();
            if ((currentState == EnemyState.Aim || currentState == EnemyState.Attack) && wasFollowingPath)
            {
                shouldPatrol = true; // 设置可以继续巡逻
                transitionState(EnemyState.Patrol);
                wasFollowingPath = false; // 重置标志
            }
        }
    }

    #endregion
    #region 可视化检测
    // 在Unity编辑器中可视化调试信息
    void OnDrawGizmos()
    {
        // 如果敌人已死亡，禁用扇形检测可视化
        if (isDead)
        {
            return;
        }
        
        // 绘制方向箭头（使用视野前向）
        Gizmos.color = Color.cyan;
        Vector2 gizmoForward = GetViewForward();
        Gizmos.DrawLine(
            transform.position,
            transform.position + (Vector3)gizmoForward * 0.5f
        );

        // 绘制视野范围
        if (eyePoint == null) return;

        // 计算扇形的起始和结束角度
        float startAngle = -viewHalfAngle;
        float endAngle = viewHalfAngle;

        // 计算每条边线之间的角度增量
        float angleStep = (endAngle - startAngle) / 50f;

        Vector2 rayOrigin = (Vector2)eyePoint.transform.position;

        // 绘制扇形的边线
        for (int i = 0; i <= 50; i++)
        {
            float angle = startAngle + angleStep * i;
            Vector3 direction = DirectionFromAngle(angle, false);

            // 检测障碍物
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, direction, viewRadius, obstacleLayer);

            // 确定射线终点和颜色
            float rayDistance = viewRadius;
            Color rayColor = RayDetectionColor; // 默认颜色

            // 检测是否有玩家在这个方向上
            if (player != null)
            {
                Vector2 playerPosition = player.transform.position;
                Vector2 directionToPlayer = (playerPosition - rayOrigin).normalized;
                float angleToPlayer = Vector2.Angle(direction, directionToPlayer);
                float distanceToPlayer = Vector2.Distance(rayOrigin, playerPosition);

                // 如果射线方向接近玩家方向，且玩家在视野范围内
                if (angleToPlayer < 5f && distanceToPlayer <= viewRadius)
                {
                    // 检查是否有障碍物阻挡
                    RaycastHit2D playerHit = Physics2D.Raycast(rayOrigin, directionToPlayer, distanceToPlayer, obstacleLayer);
                    if (playerHit.collider == null)
                    {
                        // 根据玩家是否潜行设置不同颜色
                        if (IsPlayerCrouching())
                        {
                            rayColor = Color.black; // 检测到潜行玩家时的颜色
                        }
                        else
                        {
                            rayColor = Color.green; // 检测到非潜行玩家时的颜色
                        }
                    }
                }
            }

            // 如果检测到障碍物，调整射线长度和颜色
            if (hit.collider != null)
            {
                rayDistance = hit.distance;
                rayColor = Color.yellow; // 检测到障碍物时的颜色
            }

            Vector3 rayEndPoint = rayOrigin + (Vector2)(direction * rayDistance);

            // 设置当前射线颜色
            Gizmos.color = rayColor;

            // 绘制从视野起点到射线终点的线
            Gizmos.DrawLine(rayOrigin, rayEndPoint);

            // 如果不是第一条线，绘制与上一条线的连接
            if (i > 0)
            {
                Vector3 previousDirection = DirectionFromAngle(startAngle + angleStep * (i - 1), false);

                // 检测上一条射线的障碍物
                RaycastHit2D prevHit = Physics2D.Raycast(rayOrigin, previousDirection, viewRadius, obstacleLayer);
                float prevRayDistance = prevHit.collider != null ? prevHit.distance : viewRadius;

                Vector3 previousEndPoint = rayOrigin + (Vector2)(previousDirection * prevRayDistance);

                // 绘制扇形边缘的线
                Gizmos.DrawLine(previousEndPoint, rayEndPoint);
            }
        }
    }

    // 辅助方法：从角度获取方向向量
    private Vector3 DirectionFromAngle(float angleInDegrees, bool angleIsGlobal)
    {
        if (!angleIsGlobal)
        {
            // 基于当前朝向计算角度
            Vector2 forward = GetViewForward();
            float currentAngle = Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg;
            angleInDegrees += currentAngle;
        }

        return new Vector3(Mathf.Cos(angleInDegrees * Mathf.Deg2Rad), Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), 0);
    }
    
    // 获取用于视野与Gizmos的前向
    private Vector2 GetViewForward()
    {
        if (useFixedViewDirection)
        {
            switch (fixedViewDirection)
            {
                case ViewFacing.Left:
                    return Vector2.left;
                case ViewFacing.Up:
                    return Vector2.up;
                case ViewFacing.Down:
                    return Vector2.down;
                default:
                    return Vector2.right;
            }
        }
        return currentDirection;
    }
    #endregion
    #region 受伤
    public void enemyHurt()
    {
        isHurt = true;
    }
    // 受伤函数
    public void TakeDamage(float damage)
    {
        if (isDead) return; // 如果已经死亡，不再受伤

        currentHealth -= damage;
        Debug.Log($"敌人 {gameObject.name} 受到 {damage} 点伤害，剩余血量: {currentHealth}");

        // 触发受伤事件
        OnHurt?.Invoke();

        if (currentHealth <= 0)
        {
            // 死亡
            currentHealth = 0;
            isDead = true;
            OnDeath?.Invoke();
            transitionState(EnemyState.Dead);
        }
        else
        {
            // 受伤
            isHurt = true;
            transitionState(EnemyState.Hurt);
        }
    }

    // 获取当前血量百分比
    public float GetHealthPercentage()
    {
        return currentHealth / Health;
    }

    // 检查是否死亡
    public bool IsDead()
    {
        return isDead;
    }
    #endregion
    #region 死亡
    public void enemyDeath()
    {
        transitionState(EnemyState.Dead); // 进入死亡状态
        
    }
#endregion
}