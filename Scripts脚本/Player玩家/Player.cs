using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// 玩家状态机枚举
public enum PlayerStateType
{
    Idle/*待机*/, Move/*移动*/, Attack/*攻击*/, Dodge/*闪避*/, Reload/*换弹*/, Run/*跑步*/, Crouch/*潜行*/, Crawl/*爬行*/, PickUp/*拾取*/, Die/*死亡*/, Hurt/*受伤*/
}

public class Player : MonoBehaviour
{
    // 换弹事件
    public static event System.Action<float> OnReloadStarted;
    public static event System.Action OnReloadStopped;

    // 属性
    // --- 生命值系统 ---
    [Header("生命值系统")]
    [FieldLabel("最大生命值")] public float MaxHealth = 100f; // 最大生命值
    [HideInInspector] public float CurrentHealth; // 当前生命值
    [FieldLabel("最大饱食度")] public float MaxHunger = 100f; // 最大饱食度
    [HideInInspector] public float CurrentHunger; // 当前饱食度
    [FieldLabel("最大精神值")] public float MaxMental = 100f; // 最大精神值
    [HideInInspector] public float CurrentMental; // 当前精神值
    [HideInInspector] public bool isHurt = false; // 是否受伤
    [HideInInspector] public bool isDead = false; // 是否死亡

    // --- 数值组件 ---
    [Header("数值组件")]
    public PlayerVitalStats vitalStats; // 集中管理生命/饱食/精神

    // --- 输入系统 ---
    [Header("获取玩家输入系统")]
    [FieldLabel("输入系统获取")] public PlayerInputController playerInputController; // 获取玩家输入系统
    [HideInInspector] public Vector2 InputDirection = Vector2.zero; // 记录输入方向

    // --- 移动 ---
    [Header("移动参数")]
    [FieldLabel("行走速度")] public float WalkSpeed = 3f; // 移动速度
    [HideInInspector] public float CurrentSpeed; // 当前移动速度

    // --- 跑步 ---
    [Header("跑步参数")]
    [FieldLabel("跑步速度")] public float RunSpeed = 5f; // 跑步速度
    [HideInInspector] public bool isRunning = false; // 是否正在跑步

    // --- 攻击 ---
    [Header("攻击参数")]
    [FieldLabel("攻击时移动速度")] public float FireSpeed = 1f; // 攻击时移动速度
    [FieldLabel("手部位置")] public Transform Hand;// 手部位置
    [HideInInspector] public bool isWeaponInHand = false;
    [HideInInspector] public bool isFiring = false;
    [HideInInspector] public bool isAttacking = false;
    [HideInInspector] public WeaponManager currentWeaponController; // 改为WeaponController
    [HideInInspector] public WeaponTrigger currentWeapon; // 保留兼容性
    [HideInInspector] public float fireTimer = 0f;
    [HideInInspector] public Vector3 aimDirection;
    [HideInInspector] public Vector3 worldMousePosition;
    [HideInInspector] public Camera playerCamera;

    // --- 闪避 ---
    [Header("闪避参数")]
    [FieldLabel("翻滚推力")] public float DdogeForce = 5f;
    [FieldLabel("翻滚持续时间")] public float DodgeDuration = 0.3f;
    [HideInInspector] public float DodgeTimer = 0f;
    [FieldLabel("翻滚冷却时间")] public float DodgeCooldown = 1f;
    [HideInInspector] public bool isDodgeCoolDownEnd = true;
    [HideInInspector] public bool isDodged = false;// 闪避

    // --- 潜行 ---
    [Header("潜行参数")]
    [FieldLabel("潜行颜色")] public Color crouchColor = Color.grey; // 潜行时的颜色
    [FieldLabel("潜行透明度")][Range(0, 1)] public float crouchAlpha = 0.5f; // 潜行时的透明度
    [HideInInspector] public bool isCrouching = false; // 潜行状态标志位
    [HideInInspector] public SpriteRenderer spriteRenderer; // 精灵渲染器
    [HideInInspector] public Color originalColor; // 原始颜色
    // 为了兼容多渲染器角色（身体由多个部位组成），在潜行时批量着色
    [HideInInspector] public List<SpriteRenderer> bodySpriteRenderers = new List<SpriteRenderer>();
    [HideInInspector] public List<Color> bodyOriginalColors = new List<Color>();

    // --- 拾取 ---
    [Header("拾取参数")]
    [HideInInspector] public ItemBase currentPickedItem; // 当前已拾取的物品
    [HideInInspector] public ItemBase nearbyItem; // 附近可拾取的物品
    [HideInInspector] public bool isPickingUp = false; // 是否正在拾取

    // --- 受伤参数 ---
    [Header("受伤参数")]
    [HideInInspector] public float hurtTimer = 0f; // 受伤计时器
    [HideInInspector] public bool isInHurtState = false; // 是否处于受伤状态

    // --- 视角参数 ---
    private float screenCenterX; // 屏幕中心X坐标

    // --- 内部属性组件 ---
    [HideInInspector] public Rigidbody2D PlayerRB2D;// 刚体
    [HideInInspector] public Animator AIMTOR;// 动画器
    [HideInInspector] public new Collider2D collider2D; // 碰撞器
    [HideInInspector] public IState currentState; // 当前状态
    // 字典
    private Dictionary<PlayerStateType, IState> states = new Dictionary<PlayerStateType, IState>();


    // 函数
    private void Awake()
    {
        playerInputController.EnabledGameplayInput(); // 启用玩家输入控制

        // 获取组件
        PlayerRB2D = GetComponent<Rigidbody2D>();
        AIMTOR = GetComponent<Animator>();
        collider2D = GetComponent<Collider2D>();
        Hand = transform.Find("Hand"); // 查找手部位置
        playerCamera = Camera.main; // 获取主摄像机

        // 获取SpriteRenderer组件并保存原始颜色（兼容主渲染器 + 多子渲染器）
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            // 若本体没有，则尝试在子物体查找
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                Debug.LogWarning("Player未找到SpriteRenderer组件，请检查玩家模型层级！");
            }
        }
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }

        // 收集身体相关的全部SpriteRenderer（排除武器/手部层级，避免误染武器）
        var allRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        if (allRenderers != null)
        {
            foreach (var sr in allRenderers)
            {
                if (sr == null) continue;
                // 若有手部节点，排除其子级（通常武器/手部会在此层级）
                if (Hand != null && sr.transform.IsChildOf(Hand))
                {
                    continue;
                }
                bodySpriteRenderers.Add(sr);
                bodyOriginalColors.Add(sr.color);
            }
        }


        // 初始化屏幕中心坐标
        screenCenterX = Screen.width / 2f;
        CurrentSpeed = WalkSpeed; // 设置初始速度为行走速度

        // 数值组件初始化与同步（保持旧字段兼容 FillLine.cs）
        if (vitalStats == null)
        {
            vitalStats = GetComponent<PlayerVitalStats>();
            if (vitalStats == null) vitalStats = gameObject.AddComponent<PlayerVitalStats>();
        }
        vitalStats.InitializeFromDefaults(MaxHealth, MaxHunger, MaxMental);
        // 订阅事件以同步旧字段与最大值
        vitalStats.OnHealthChanged += (value, max) => { MaxHealth = max; CurrentHealth = value; isDead = (CurrentHealth <= 0f); };
        vitalStats.OnHungerChanged += (value, max) => { MaxHunger = max; CurrentHunger = value; };
        vitalStats.OnMentalChanged += (value, max) => { MaxMental = max; CurrentMental = value; };
        // 初次同步到旧字段
        MaxHealth = vitalStats.maxHealth; CurrentHealth = vitalStats.currentHealth;
        MaxHunger = vitalStats.maxHunger; CurrentHunger = vitalStats.currentHunger;
        MaxMental = vitalStats.maxMental; CurrentMental = vitalStats.currentMental;

        // 获取状态机
        InitializeStateMachine();

        // 设置默认状态为Idle
        transitionState(PlayerStateType.Idle);

    }
    // --- 状态机开放函数（提供给任何状态使用的函数） ---
    // 用于切换状态函数
    public void transitionState(PlayerStateType type)
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

    // 更新视角方向
    public void UpdateLookDirection()
    {
        var mousePos = Mouse.current.position.ReadValue(); // 获取鼠标位置
        // 根据鼠标位置更新动画器方向参数（0为左，1为右）
        float horizontalDirection = (mousePos.x < screenCenterX) ? 0f : 1f; // 水平方向
        AIMTOR.SetFloat("Horizontial", horizontalDirection); // 设置动画器参数
    }

    // 更新瞄准方向（移除层级和距离限制）
    public void UpdateAiming()
    {
        if (playerCamera == null) return; // 如果没有相机，则返回

        // 获取鼠标屏幕位置
        Vector3 mouseScreenPosition = Mouse.current.position.ReadValue(); // 获取鼠标屏幕位置
        mouseScreenPosition.z = playerCamera.nearClipPlane; // 设置屏幕平面

        // 转换为世界坐标
        worldMousePosition = playerCamera.ScreenToWorldPoint(mouseScreenPosition);

        // 计算瞄准方向（无距离限制）
        aimDirection = (worldMousePosition - transform.position).normalized;

        // 更新Hand的旋转
        UpdateHandRotation();
    }

    // 更新Hand旋转
    public void UpdateHandRotation()
    {
        if (Hand == null) return;

        // 计算Hand应该指向的角度
        float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;

        // 设置Hand旋转
        Hand.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        // 如果有武器，处理武器翻转
        if (isWeaponInHand && currentWeapon != null)
        {
            UpdateWeaponFlip(angle);
        }
    }

    // 更新武器翻转
    public void UpdateWeaponFlip(float handAngle)
    {
        if (currentWeapon == null) return;

        Vector3 weaponScale = currentWeapon.transform.localScale;

        // 当Hand旋转角度大于90度或小于-90度时，翻转武器Y轴
        if (handAngle > 90f || handAngle < -90f)
        {
            weaponScale.y = -Mathf.Abs(weaponScale.y); // 翻转Y轴
        }
        else
        {
            weaponScale.y = Mathf.Abs(weaponScale.y); // 保持正常
        }

        currentWeapon.transform.localScale = weaponScale;
    }

    // 更新武器朝向
    public void UpdateWeaponOrientation()
    {
        if (currentWeapon == null || !isWeaponInHand) return;

        // 获取当前Hand的旋转角度
        float handAngle = Hand.eulerAngles.z;

        // 将角度转换为-180到180的范围
        if (handAngle > 180f)
        {
            handAngle -= 360f;
        }

        // 更新武器翻转
        UpdateWeaponFlip(handAngle);
    }

    // 受伤处理方法 
    public void TakeDamage(float damage)
    {
        if (isDead) return; // 如果已死亡，不再受伤

        // 通过数值组件结算伤害，事件会同步旧字段
        if (vitalStats != null)
        {
            vitalStats.ApplyDamage(damage);
        }
        else
        {
            CurrentHealth = Mathf.Max(0, CurrentHealth - damage);
        }

        // 设置受伤状态
        isHurt = true;
        isInHurtState = true;
        hurtTimer = 0f; // 重置受伤计时器

        // 检查是否死亡
        if ((vitalStats != null && vitalStats.IsDead) || CurrentHealth <= 0)
        {
            isDead = true;
            transitionState(PlayerStateType.Die);
            return;
        }

        // 如果不在受伤状态，切换到受伤状态
        if (currentState.GetType() != typeof(PlayerHurtState))
        {
            transitionState(PlayerStateType.Hurt);
        }

        Debug.Log($"玩家受到 {damage} 点伤害，当前生命值: {CurrentHealth}");
    }

    // --- 常规函数 ---
    // 状态机获取器
    private void InitializeStateMachine()
    {
        states.Add(PlayerStateType.Idle, new PlayerIdleState(this));  // 待机状态
        states.Add(PlayerStateType.Move, new PlayerMoveState(this)); // 移动状态
        states.Add(PlayerStateType.Attack, new PlayerAttackState(this)); // 攻击状态
        states.Add(PlayerStateType.Dodge, new PlayerDodgeState(this)); // 闪避状态
        states.Add(PlayerStateType.Reload, new PlayerReloadState(this)); // 换弹状态
        states.Add(PlayerStateType.Run, new PlayerRunState(this)); // 跑步状态
        states.Add(PlayerStateType.Crouch, new PlayerCrouchState(this)); // 潜行状态
        states.Add(PlayerStateType.PickUp, new PlayerPickUpState(this)); // 拾取物品状态
        states.Add(PlayerStateType.Die, new PlayerDieState(this)); // 死亡状态
        states.Add(PlayerStateType.Hurt, new PlayerHurtState(this)); // 受伤状态
    }

    private void Update()
    {
        // 清空开发者控制台并显示当前状态
        Debug.ClearDeveloperConsole();
        Debug.Log($"当前状态机: {currentState.GetType().Name}");

        WeaponInHand();

        currentState.OnUpdate();
    }
    private void FixedUpdate()
    {
        currentState.OnFixedUpdate();
    }

    // 开火状态控制
    [HideInInspector] public bool isHoldingFire = false;
    [HideInInspector] public float fireHoldTime = 0f;
    public const float HOLD_THRESHOLD = 0.2f; // 长按阈值（秒）- 改为public

    private void OnEnable() // 启用时
    {
        playerInputController.onMovement += Move;
        playerInputController.onFire += Fire; // 使用统一的开火事件
        playerInputController.onDodge += Dodge;
        playerInputController.onPickup += PickUp;
        playerInputController.onRun += Run;
        playerInputController.onCrouch += Crouch;
        playerInputController.onReload += Reload;
    }

    private void OnDisable() // 禁用时
    {
        playerInputController.onMovement -= Move;
        playerInputController.onFire -= Fire;
        playerInputController.onDodge -= Dodge;
        playerInputController.onPickup -= PickUp;
        playerInputController.onRun -= Run;
        playerInputController.onCrouch -= Crouch;
        playerInputController.onReload -= Reload;
    }

    // 完整瞄准更新（用于攻击状态）
    public void UpdateFullAiming()
    {
        UpdateAiming();
        UpdateLookDirection();
    }

    // 基础瞄准更新（用于所有状态）
    public void UpdateBasicAiming()
    {
        UpdateAiming();
        UpdateLookDirection();
    }

    // --- 功能函数 --- 
    #region 移动
    public void Move(Vector2 moveInput)
    {
        // 直接应用输入方向
        InputDirection = moveInput;

        // 立即停止移动
        CurrentSpeed = (InputDirection != Vector2.zero && !isFiring) ? WalkSpeed : 0f;

        // 更新动画参数
        UpdateLookDirection();

        // 应用物理速度
        PlayerRB2D.velocity = InputDirection * CurrentSpeed;

        // 更新速度动画参数
        float speedMagnitude = InputDirection.sqrMagnitude * (CurrentSpeed / WalkSpeed);
        AIMTOR.SetFloat("Speed", speedMagnitude);
    }
    #endregion
    #region 射击
    // 检测武器
    public void WeaponInHand()
    {
        if (Hand.childCount > 0)
        {
            isWeaponInHand = true;

            Transform weaponTransform = Hand.GetChild(0);
            WeaponManager newWeaponController = weaponTransform.GetComponent<WeaponManager>();

            if (currentWeaponController != newWeaponController)
            {
                currentWeaponController = newWeaponController;
                currentWeapon = weaponTransform.GetComponent<WeaponTrigger>(); // 保留兼容性

                if (currentWeaponController != null)
                {
                    // 通知武器控制器玩家拾取了武器
                    currentWeaponController.OnPickedUp(this.transform);
                    Debug.Log($"切换武器: {currentWeaponController.GetWeaponName()}");
                }
                else
                {
                    Debug.LogWarning($"武器 {weaponTransform.name} 缺少WeaponController组件！");
                }
            }
        }
        else
        {
            if (isWeaponInHand)
            {
                Debug.Log("武器已卸下");
            }
            isWeaponInHand = false;
            currentWeaponController = null;
            currentWeapon = null;
        }
    }

    // 单次射击方法（新增）
    public void FireSingle()
    {
        if (!isWeaponInHand || currentWeaponController == null)
        {
            Debug.LogWarning("没有装备武器，无法射击！");
            return;
        }

        // 检查是否可以射击
        if (!currentWeaponController.CanFire())
        {
            if (currentWeaponController.NeedsReload())
            {
                Debug.Log("弹药用尽，需要换弹！");
                transitionState(PlayerStateType.Reload);
                return;
            }
            else if (currentWeaponController.IsReloading())
            {
                Debug.Log("正在换弹中，无法射击");
                return;
            }
        }

        // 执行单次射击
        currentWeaponController.FireSingle();
        Debug.Log("执行单次射击");

        // 如果不在攻击状态，切换到攻击状态
        if (!isAttacking)
        {
            isAttacking = true;
            transitionState(PlayerStateType.Attack);
        }
    }

    // 连续射击方法（新增）
    public void FireContinuous(bool isContinuous)
    {
        if (!isWeaponInHand || currentWeaponController == null)
        {
            if (isContinuous)
            {
                Debug.LogWarning("没有装备武器，无法连续射击！");
            }
            return;
        }

        if (isContinuous)
        {
            // 开始连续射击
            if (!currentWeaponController.CanFire())
            {
                if (currentWeaponController.NeedsReload())
                {
                    Debug.Log("弹药用尽，需要换弹！");
                    transitionState(PlayerStateType.Reload);
                    return;
                }
                else if (currentWeaponController.IsReloading())
                {
                    Debug.Log("正在换弹中，无法射击");
                    return;
                }
            }

            currentWeaponController.SetFiring(true);
            isFiring = true;

            if (!isAttacking)
            {
                isAttacking = true;
                transitionState(PlayerStateType.Attack);
            }

            Debug.Log("开始连续射击");
        }
        else
        {
            // 停止连续射击
            currentWeaponController.SetFiring(false);
            isFiring = false;
            isAttacking = false;
            Debug.Log("停止连续射击");
        }
    }

    // 统一的开火控制方法
    public void Fire(bool isPressed)
    {
        if (!isWeaponInHand || currentWeaponController == null)
        {
            if (isPressed)
            {
                Debug.LogWarning("没有装备武器，无法射击！");
            }
            return;
        }

        if (isPressed)
        {
            // 按下开火键
            if (!currentWeaponController.CanFire())
            {
                if (currentWeaponController.NeedsReload())
                {
                    Debug.Log("弹药用尽，需要换弹！");
                    transitionState(PlayerStateType.Reload);
                    return;
                }
                else if (currentWeaponController.IsReloading())
                {
                    Debug.Log("正在换弹中，无法射击");
                    return;
                }
            }

            // 立即执行第一次射击（单次射击）
            currentWeaponController.FireSingle();

            // 设置开火状态，让状态机处理连续射击
            isHoldingFire = true;
            fireHoldTime = 0f;

            if (!isAttacking)
            {
                isAttacking = true;
                isFiring = true;
                transitionState(PlayerStateType.Attack);
            }

            Debug.Log("开始射击");
        }
        else
        {
            // 释放开火键
            isHoldingFire = false;
            currentWeaponController.SetFiring(false);
            isFiring = false;
            isAttacking = false;
            fireHoldTime = 0f;

            Debug.Log("停止射击");
        }
    }
    #endregion
    #region 闪避
    // 闪避动作
    public void Dodge()
    {
        if (!isDodged && isDodgeCoolDownEnd)  // 冷却结束时才能闪避
        {
            isDodged = true; // 开始闪避
            Debug.Log("闪避输入触发");
        }
    }
    public void DodgeOnCoolDown()
    {
        StartCoroutine(nameof(DodgeOnCoolDownCoroutine));
    }

    // 闪避冷却协程
    public IEnumerator DodgeOnCoolDownCoroutine()
    {
        yield return new WaitForSeconds(DodgeCooldown);
        isDodgeCoolDownEnd = true; // 冷却结束，设置为true
        Debug.Log("闪避冷却结束");
    }
    #endregion
    #region 拾取
    public void PickUp()
    {
        if (nearbyItem != null)
        {
            isPickingUp = true;
            Debug.Log("拾取输入触发");
        }
    }

    // 注册可拾取物品（由物品脚本调用）
    public void RegisterItem(ItemBase item)
    {
        nearbyItem = item;
    }

    // 取消注册离开触发器的物品（由物品脚本调用）
    public void UnregisterItem(ItemBase item)
    {
        if (nearbyItem == item)
        {
            nearbyItem = null;
        }
    }
    #endregion
    #region 奔跑
    public void Run(bool isRunPressed) // 修改为接收bool参数
    {
        isRunning = isRunPressed;
        Debug.Log($"奔跑状态: {(isRunning ? "开启" : "关闭")}");
    }
    #endregion
    #region 换弹
    public void Reload()
    {
        if (isWeaponInHand && currentWeaponController != null)
        {
            if (currentWeaponController.NeedsReload() || currentWeaponController.GetCurrentAmmo() < currentWeaponController.GetMagazineCapacity())
            {
                Debug.Log("换弹输入触发，切换到换弹状态");
                transitionState(PlayerStateType.Reload);
            }
            else
            {
                Debug.Log("弹夹已满，无需换弹");
            }
        }
        else
        {
            Debug.Log("没有武器，无法换弹");
        }
    }

    /// <summary>
    /// 触发换弹开始事件
    /// </summary>
    /// <param name="reloadTime">换弹时间</param>
    public void TriggerReloadStarted(float reloadTime)
    {
        OnReloadStarted?.Invoke(reloadTime);
    }

    /// <summary>
    /// 触发换弹停止事件
    /// </summary>
    public void TriggerReloadStopped()
    {
        OnReloadStopped?.Invoke();
    }
    #endregion
    #region 潜行
    public void Crouch(bool isCrouchPressed)
    {
        isCrouching = isCrouchPressed;
        Debug.Log($"潜行状态: {(isCrouching ? "开启" : "关闭")}");
    }

    // 已废弃：潜行视觉变色（保留空实现以兼容旧调用）
    public void ApplyCrouchVisual() { }

    // 已废弃：恢复原始视觉效果（保留空实现以兼容旧调用）
    public void RestoreOriginalVisual() { }

    // 外部访问潜行状态
    public bool IsCrouching()
    {
        return isCrouching;
    }
    #endregion
}
