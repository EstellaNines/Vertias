using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Pathfinding;

// 僵尸状态枚举
public enum ZombieStateType
{
    Idle, // 待机
    Patrol, // 巡逻
    Chase, // 追逐
    Attack, // 攻击
    Hurt, // 受伤
    Dead // 死亡
}

public class Zombie : MonoBehaviour
{
    // 追逐相关
    [Header("目标")]
    public Transform player; // 玩家对象

    [Header("移动追逐")]
    [FieldLabel("追逐距离")] public float ChaseDistance = 5f; // 追逐距离
    [FieldLabel("攻击距离")] public float AttackDistance = 1.5f; // 攻击距离
    [FieldLabel("追逐范围")] public float chaseRange = 5f; // 追逐范围
    [HideInInspector] public Vector2 MovementInput { get; set; }
    [FieldLabel("当前速度")] public float CurrentSpeed = 0; // 当前速度

    // A*寻路相关
    [Header("A*寻路")]
    private Seeker seeker;
    [HideInInspector] public List<Vector3> PathPointList; // 路径点列表
    [HideInInspector] public int currentIndex = 0; // 路径点索引
    [FieldLabel("路径生成间隔")] public float PathGenerateInterval = 0.5f; // 路径生成间隔
    [HideInInspector] public float PathGenerateTimer = 0f; // 计时器

    // 巡逻    
    [Header("待机巡逻")]
    public float IdleDuration; // 待机时间
    public Transform[] PatrolPoints; // 巡逻点
    public int targetPointsIndex = 0; // 巡逻点索引

    // 攻击
    [Header("攻击")]
    [HideInInspector] public bool isAttack = true; // 攻击状态确认
    [FieldLabel("伤害")] public float ZombieAttackDamage; // 僵尸攻击伤害
    [HideInInspector] public float distance; // 僵尸攻击范围
    [FieldLabel("玩家图层")] public LayerMask playerMask; // 玩家图层
    [FieldLabel("攻击冷却时间")] public float AttackCooldownDuration = 2f; // 攻击冷却时间

    // 受伤
    [Header("受伤")]
    public bool isHurt; // 受伤状态确认
    private bool _isHurt; // 私有受伤状态字段
    public bool isKnockBack = true; // 击退状态确认
    [FieldLabel("击退力度")] public float KnockBackForce = 1f; // 击退力度
    public float KnockBackDuration = 0.1f;// 击退持续时间
    [FieldLabel("击退方向")] public Vector2 KnockBackDirection; // 击退方向
    [FieldLabel("受伤动画时长")] public float HurtAnimationDuration = 0.5f;

    // 受伤事件（true=受伤开始, false=受伤结束）
    public UnityEvent<bool> OnHurt = new UnityEvent<bool>();

    // 死亡状态相关
    [Header("死亡")]
    [HideInInspector] public float currentHorizontalDirection; // 记录最后一个水平方向
    public UnityEvent OnDead = new UnityEvent(); // 死亡事件

    // 生命值
    [FieldLabel("最大生命值")] public float MaxHealth = 100f; // 最大生命值
    public float currentHealth; // 当前生命值

    // 内部变量
    private IState currentState; // 当前状态
    // 字典
    private Dictionary<ZombieStateType, IState> states = new Dictionary<ZombieStateType, IState>();
    [HideInInspector] public Collider2D physicalCollider; // 碰撞器
    [HideInInspector] public Rigidbody2D rb; // 刚体
    [HideInInspector] public Animator animator; // 动画器
    [HideInInspector] public SpriteRenderer spriteRenderer; // 精灵渲染器

    private void Awake()
    {
        seeker = GetComponent<Seeker>(); // 获取寻路组件
        rb = GetComponent<Rigidbody2D>(); // 获取刚体组件
        animator = GetComponent<Animator>(); // 获取动画组件
        physicalCollider = GetComponent<Collider2D>(); // 获取碰撞器组件
        spriteRenderer = GetComponent<SpriteRenderer>(); // 获取精灵渲染器组件

        // 实例化各种状态
        states.Add(ZombieStateType.Idle, new ZombieIdleState(this)); // 待机状态字典
        states.Add(ZombieStateType.Chase, new ZombieChaseState(this)); // 追逐状态字典
        states.Add(ZombieStateType.Attack, new ZombieAttackState(this)); // 攻击状态字典
        states.Add(ZombieStateType.Hurt, new ZombieHurtState(this)); // 受伤状态字典  
        states.Add(ZombieStateType.Dead, new ZombieDeadState(this)); // 死亡状态字典
        states.Add(ZombieStateType.Patrol, new ZombiePatrolState(this)); // 巡逻状态字典

        currentHealth = MaxHealth; // 初始化当前生命值

        // 设置默认状态为Idle
        transitionState(ZombieStateType.Idle);
    }
    
    // 状态切换状态方法
    public void transitionState(ZombieStateType type)
    {
        // 当前状态是否为空
        // 当前状态不为空，退出当前状态
        if (currentState != null)
        {
            currentState.OnExit();
        }
        // 通过字典的键找到对应的状态
        currentState = states[type];
        currentState.OnEnter();
    }

    private void Update()
    {
        currentState.OnUpdate();
        // 调试距离
        Debug.Log("玩家距离" + distance);
    }
    
    private void FixedUpdate()
    {
        currentState.OnFixedUpdate();
    }

    // 判断玩家是否在当前追逐范围内
    public void GetPlayerTransform()
    {
        Collider2D[] chaseCollider = Physics2D.OverlapCircleAll(transform.position, chaseRange, playerMask);
        if (chaseCollider.Length > 0) // 数组长度大于0，说明玩家在范围内
        {
            Transform potentialPlayer = chaseCollider[0].transform;
            Player playerComponent = potentialPlayer.GetComponent<Player>();
            
            // 检测玩家是否已死亡
            if (playerComponent != null && playerComponent.isDead) // 使用isDead变量而不是IsDead()方法
            {
                player = null; // 玩家已死亡，不再追踪
                Debug.Log("玩家已死亡，僵尸停止追踪");
                return;
            }
            
            player = potentialPlayer; // 获取玩家
            if (player != null)
            {
                distance = Vector2.Distance(player.position, transform.position); // 计算追逐范围内
            }
        }
        else
        {
            player = null; // 不在追逐范围内
        }
    }
    
    #region 移动
    public void move()
    {
        // 检查 rb 是否存在且未销毁
        if (MovementInput.magnitude > 0.1f && CurrentSpeed >= 0)
        {
            rb.velocity = MovementInput * CurrentSpeed; // 移动
            // 动画播放逻辑
            float horizontal = MovementInput.x > 0 ? 1f : 0f;
            animator.SetFloat("Horizontial", horizontal);
        }
        else
        {
            rb.velocity = Vector2.zero;
        }
    }
    #endregion

    #region 自动寻路
    // 自动寻路
    public void Autopath()
    {
        // 计时器的修改，实时修改路径
        PathGenerateTimer += Time.deltaTime;
        if (PathGenerateTimer >= PathGenerateInterval)
        {
            GeneratePath(player.position);
            PathGenerateTimer = 0;
        }
        // 当路径列表为空时，生成路径点列表
        if (PathPointList == null || PathPointList.Count <= 0)
        {
            GeneratePath(player.position);
        }
        // 当僵尸到达当前路径点，进入路径点列表的下一个点
        else if (Vector2.Distance(transform.position, PathPointList[currentIndex]) <= 0.1f)
        {
            currentIndex++;
            if (currentIndex >= PathPointList.Count)
                GeneratePath(player.position);
        }
    }
    
    // 路径点获取
    public void GeneratePath(Vector3 target)
    {
        currentIndex = 0;
        seeker.StartPath(transform.position, target, Path =>
        {
            PathPointList = Path.vectorPath; // 列表记录A*路径点
        }); // 起点、终点、回调函数
    }
    #endregion
    
    #region 受伤事件
    public void ZombieHurt()
    {
        isHurt = true; // 受伤
    }
    
    public bool isHurting
    {
        get => _isHurt;
        set
        {
            if (_isHurt != value)
            {
                _isHurt = value;
                OnHurt.Invoke(_isHurt);  // 统一事件调用
            }
        }
    }

    public void TakeDamage(int damage, Vector2 hitDirection)
    {
        // 血量扣除逻辑
        currentHealth -= damage;

        // 检查是否死亡
        if (currentHealth <= 0)
        {
            zombieDie();
            return;
        }

        // 设置击退方向（通过参数传递方向）
        KnockBackDirection = hitDirection.normalized;
        isHurt = true; // 设置受伤状态

        // 记录最后一次受击的方向（用于朝向）
        currentHorizontalDirection = Mathf.Sign(hitDirection.x);
    }
    #endregion
    
    #region 死亡
    public void zombieDie()
    {
        transitionState(ZombieStateType.Dead); // 切换到死亡状态
    }
    #endregion
    
    #region 攻击冷却
    public void AttackCooldown()
    {
        StartCoroutine(AttackCooldownCoroutine());
    }

    //攻击冷却时间协程
    private IEnumerator AttackCooldownCoroutine()
    {
        yield return new WaitForSeconds(AttackCooldownDuration);
        isAttack = true;
    }
    #endregion
}
