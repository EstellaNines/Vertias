using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GlobalMessaging;
using Pathfinding;

// 最小可用中立敌人：仅包含待机状态机与受伤/死亡通路
public class NeutralEnemy : MonoBehaviour
{
    [Header("基础")]
    [FieldLabel("血量")] public float Health = 100f;
    [HideInInspector] public float currentHealth;

    [Header("敌对")]
    [FieldLabel("是否敌对")] public bool isHostile = false; // 被玩家攻击后置为 true

    [Header("类型判定")]
    [FieldLabel("是否站岗型")] public bool isGuard = true; // 站岗=true，巡逻=false（与枚举联动）
    public enum NeutralType { Guard, Patrol }
    [FieldLabel("中立敌人类型")] public NeutralType neutralType = NeutralType.Guard;

    [Header("事件")]
    public UnityEngine.Events.UnityEvent OnHurt;
    public UnityEngine.Events.UnityEvent OnDeath;
    public UnityEngine.Events.UnityEvent OnBecomeHostile;

    [Header("组件")]
    [HideInInspector] public Rigidbody2D RB;
    [HideInInspector] public Animator animator;
    [HideInInspector] public Collider2D Collider;
    [HideInInspector] public bool isHurt;
    [HideInInspector] public bool isDead;
    [FieldLabel("武器Transform（可选）")] public Transform weapon; // 可选：用于朝向
    [FieldLabel("武器Sprite（可选）")] public SpriteRenderer weaponSprite; // 可选：用于翻转
    [FieldLabel("武器控制器（可选）")] public WeaponManager weaponController;
    [FieldLabel("枪口（可选）")] public Transform firePoint;
    [FieldLabel("敌人子弹池（可选）")] public EnemyBulletPool enemyBulletPool;

    [Header("移动/巡逻")]
    [FieldLabel("移动速度")] public float MoveSpeed = 3f;
    [FieldLabel("巡逻点列表")] public Transform[] patrolPoints;
    [FieldLabel("当前巡逻点索引")] public int patrolIndex = 0;
    [FieldLabel("路径更新间隔")] public float pathUpdateInterval = 0.5f;
    [FieldLabel("到达距离阈值")] public float reachDistance = 0.5f;
    [FieldLabel("到点后待机秒数")] public float idleAfterPointTime = 2f;
    [HideInInspector] public bool shouldPatrol = true;
    [HideInInspector] public Path currentPath;
    [HideInInspector] public int currentWaypoint = 0;
    [HideInInspector] public bool reachedEndOfPath = false;
    [HideInInspector] public Seeker seeker;

    [Header("敌对提示")]
    [FieldLabel("敌对提示精灵对象")] public GameObject hostileIndicator; // 默认隐藏
    [FieldLabel("提示显示时长 秒")] public float hostileIndicatorDuration = 1f;
    private Coroutine hostileIndicatorRoutine;

    [Header("感知")]
    [FieldLabel("眼睛子对象")] public Transform eye; // Eye 子对象
    [FieldLabel("听力半径")] public float hearingRadius = 12f; // 比视野更大
    [FieldLabel("玩家层")] public LayerMask playerLayer; // 用于听力Overlap检测
    [HideInInspector] public Vector2 fovForward = Vector2.right; // 仅用于FOV朝向，不影响本体朝向
    public enum DefaultFovFacing { Right, Left, Up, Down }
    [FieldLabel("默认视野朝向")] public DefaultFovFacing defaultFovFacing = DefaultFovFacing.Right;

    [Header("站岗扫视")]
    [FieldLabel("启用站岗扫视")] public bool enableGuardScan = true;
    [FieldLabel("扫视最小角度(度)")] public float scanAngleMin = -45f;
    [FieldLabel("扫视最大角度(度)")] public float scanAngleMax = 45f;
    [FieldLabel("扫视速度(Hz)")] public float scanSpeedHz = 0.2f; // 每秒来回次数


    private IState currentState;
    private readonly Dictionary<string, IState> states = new Dictionary<string, IState>();

    void Awake()
    {
        RB = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        Collider = GetComponent<Collider2D>();
        currentHealth = Health;

        // 注册状态：Idle、Patrol、Move、Aim、Attack、Hurt、Dead
        states["Idle"] = new NeutralIdleState(this);
        states["Patrol"] = new NeutralPatrolState(this);
        states["Move"] = new NeutralMoveState(this);
        states["Aim"] = new NeutralAimState(this);
        states["Attack"] = new NeutralAttackState(this);
        states["Hurt"] = new NeutralHurtState(this);
        states["Dead"] = new NeutralDeathState(this);
        Transition("Idle");

        // 默认隐藏提示精灵
        if (hostileIndicator != null) hostileIndicator.SetActive(false);

        // 初始化FOV默认朝向
        ResetFovToDefault();

        // A* 寻路组件
        seeker = GetComponent<Seeker>();

        // 尝试自动缓存武器（可选）
        CacheWeaponIfNull();

        // 缓存子弹池
        if (enemyBulletPool == null) enemyBulletPool = EnemyBulletPool.Instance;

        // 巡逻型：进入时主动开始移动到第一个巡逻点
        if (neutralType == NeutralType.Patrol && patrolPoints != null && patrolPoints.Length > 0)
        {
            Transition("Move");
        }
    }

    void OnValidate()
    {
        // 同步 bool 与 enum
        // 优先根据 enum 推导 bool（当面板改动枚举时生效）
        switch (neutralType)
        {
            case NeutralType.Guard:
                isGuard = true; break;
            case NeutralType.Patrol:
                isGuard = false; break;
        }
        // 若开发者手动改了 isGuard，则回写到枚举，保持一致
        neutralType = isGuard ? NeutralType.Guard : NeutralType.Patrol;
    }

    void OnEnable()
    {
        MessagingCenter.Instance.Register<NeutralHostileMessage>(OnNeutralHostile);
        MessagingCenter.Instance.Register<NeutralScanConfigMessage>(OnScanConfig);
    }

    void OnDisable()
    {
        MessagingCenter.Instance.Unregister<NeutralHostileMessage>(OnNeutralHostile);
        MessagingCenter.Instance.Unregister<NeutralScanConfigMessage>(OnScanConfig);
    }

    private void OnNeutralHostile(NeutralHostileMessage msg)
    {
        BecomeHostile();
    }

    private void OnScanConfig(NeutralScanConfigMessage msg)
    {
        // 目标过滤：若指定了实例且不是我，忽略
        if (msg.targetInstanceId.HasValue && msg.targetInstanceId.Value >= 0 && msg.targetInstanceId.Value != gameObject.GetInstanceID())
            return;

        if (msg.enableGuardScan.HasValue) enableGuardScan = msg.enableGuardScan.Value;
        if (msg.scanAngleMin.HasValue) scanAngleMin = msg.scanAngleMin.Value;
        if (msg.scanAngleMax.HasValue) scanAngleMax = msg.scanAngleMax.Value;
        if (msg.scanSpeedHz.HasValue) scanSpeedHz = Mathf.Max(0.0001f, msg.scanSpeedHz.Value);

        // 角度约束：保持 min <= max
        if (scanAngleMin > scanAngleMax)
        {
            float t = scanAngleMin; scanAngleMin = scanAngleMax; scanAngleMax = t;
        }
    }

    void Update()
    {
        if (currentState != null) currentState.OnUpdate();
    }

    void FixedUpdate()
    {
        if (currentState != null) currentState.OnFixedUpdate();
    }

    public void Transition(string key)
    {
        if (currentState != null) currentState.OnExit();
        currentState = states[key];
        currentState.OnEnter();
    }

    public void PlayAnimation(string name)
    {
        if (animator != null) animator.Play(name);
    }

    // 设为敌对（可从子弹命中或外部调用）
    public void BecomeHostile()
    {
        if (!isHostile)
        {
            isHostile = true;
            OnBecomeHostile?.Invoke();
            // 显示提示精灵
            if (hostileIndicator != null)
            {
                if (hostileIndicatorRoutine != null) StopCoroutine(hostileIndicatorRoutine);
                hostileIndicatorRoutine = StartCoroutine(ShowHostileIndicatorOnce());
            }
        }
    }

    private IEnumerator ShowHostileIndicatorOnce()
    {
        hostileIndicator.SetActive(true);
        float t = 0f;
        while (t < hostileIndicatorDuration)
        {
            t += Time.deltaTime;
            yield return null;
        }
        hostileIndicator.SetActive(false);
        hostileIndicatorRoutine = null;
    }

    // 仅更新FOV朝向（不会改变Transform旋转）
    public void SetFovForward(Vector2 dir)
    {
        if (dir.sqrMagnitude > 0.0001f)
        {
            fovForward = dir.normalized;
            // 同步动画朝向（仅左右）
            if (animator != null)
            {
                if (Mathf.Abs(fovForward.x) >= Mathf.Abs(fovForward.y))
                {
                    float horizontal = fovForward.x < 0f ? 0f : 1f;
                    animator.SetFloat("Horizontal", horizontal);
                }
            }
        }
    }

    // 根据默认设置重置FOV朝向，并同步动画水平参数
    public void ResetFovToDefault()
    {
        Vector2 v = GetDefaultForwardVector();
        fovForward = v;
        if (animator != null)
        {
            // 左=0，右=1（上下不改值）
            if (v.x != 0f)
            {
                animator.SetFloat("Horizontal", v.x < 0f ? 0f : 1f);
            }
        }
    }

    // 取得默认朝向的单位向量
    public Vector2 GetDefaultForwardVector()
    {
        switch (defaultFovFacing)
        {
            case DefaultFovFacing.Left: return Vector2.left;
            case DefaultFovFacing.Up: return Vector2.up;
            case DefaultFovFacing.Down: return Vector2.down;
            default: return Vector2.right;
        }
    }

    // 计算在默认方向为中心、给定角度范围下的扫视朝向（度）
    public Vector2 EvaluateScanForward(float time)
    {
        Vector2 baseDir = GetDefaultForwardVector();
        float s = Mathf.Sin(2f * Mathf.PI * Mathf.Max(0.0001f, scanSpeedHz) * time); // [-1,1]
        float angle = Mathf.Lerp(scanAngleMin, scanAngleMax, (s + 1f) * 0.5f);
        return Rotate(baseDir, angle);
    }

    private static Vector2 Rotate(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos).normalized;
    }

    private void CacheWeaponIfNull()
    {
        if (weaponController == null)
        {
            weaponController = GetComponentInChildren<WeaponManager>();
            if (weaponController != null)
            {
                weapon = weaponController.transform;
                // 设置持有者为敌人
                weaponController.SetOwner(this.transform, false);
                // 查找枪口
                var muzzle = weaponController.transform.Find("Muzzle");
                if (muzzle != null) firePoint = muzzle;
                else if (weaponController.muzzle != null) firePoint = weaponController.muzzle;
                // 确保武器挂到敌人身上并跟随
                weapon.SetParent(this.transform, true);
            }
        }
        if (weapon == null && weaponController != null)
        {
            weapon = weaponController.transform;
        }
        if (weapon == null)
        {
            // 优先找带 WeaponManager 的对象，再退化为第一个 SpriteRenderer
            var wm = GetComponentInChildren<WeaponManager>();
            if (wm != null) weapon = wm.transform;
            else
            {
                var sr = GetComponentInChildren<SpriteRenderer>();
                if (sr != null && sr.transform != transform) weapon = sr.transform;
            }
        }
        if (weaponSprite == null && weapon != null)
        {
            weaponSprite = weapon.GetComponent<SpriteRenderer>();
        }
    }

    // 让武器瞄准目标（仅旋转Z，不影响本体与FOV）
    public void AimWeaponTowards(Vector2 origin, Vector2 target)
    {
        CacheWeaponIfNull();
        if (weapon == null) return;
        Vector2 dir = (target - origin).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        weapon.rotation = Quaternion.Euler(0f, 0f, angle);
        if (weaponSprite != null)
        {
            bool flip = (angle > 90f || angle < -90f);
            Vector3 scale = weaponSprite.transform.localScale;
            scale.y = flip ? -Mathf.Abs(scale.y) : Mathf.Abs(scale.y);
            weaponSprite.transform.localScale = scale;
        }
    }

    // 重置武器朝向（用于玩家死亡/退出瞄准）
    public void ResetWeaponAim()
    {
        CacheWeaponIfNull();
        if (weapon != null) weapon.localRotation = Quaternion.identity;
        if (weaponSprite != null)
        {
            Vector3 scale = weaponSprite.transform.localScale;
            scale.y = Mathf.Abs(scale.y);
            weaponSprite.transform.localScale = scale;
        }
    }

    // 判断玩家是否在FOV视野内（基于 fovForward 与 FOV 组件的视距/角度）
    public bool IsPlayerInFov(Transform player, FOV fovComp)
    {
        if (player == null) return false;
        Vector2 origin = eye != null ? (Vector2)eye.position : (Vector2)transform.position;
        Vector2 toPlayer = (Vector2)player.position - origin;
        float dist = toPlayer.magnitude;
        float maxDist = fovComp != null ? fovComp.viewDistance : hearingRadius;
        if (dist > maxDist) return false;
        Vector2 forward = fovForward.sqrMagnitude > 0.0001f ? fovForward : Vector2.right;
        float ang = Vector2.Angle(forward, toPlayer.normalized);
        float halfFov = fovComp != null ? fovComp.fov * 0.5f : 45f;
        return ang <= halfFov;
    }

    // 受伤/死亡通路（可从 UnityEvent 或外部调用）
    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            isDead = true;
            OnDeath?.Invoke();
            Transition("Dead");
        }
        else
        {
            isHurt = true;
            OnHurt?.Invoke();
            Transition("Hurt");
            // 广播敌对给其它中立敌人
            MessagingCenter.Instance.Send(new NeutralHostileMessage { sourceInstanceId = gameObject.GetInstanceID() });
        }
    }
}


