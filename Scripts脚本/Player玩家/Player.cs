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
    [FieldLabel("手部位置")] public Transform Hand; // 手部位置
    [HideInInspector] public bool isWeaponInHand = false; // 武器是否在手部
    [HideInInspector] public bool isFiring = false; // 射击状态变量
    [HideInInspector] public bool isAttacking = false; // 是否正在攻击状态
    [HideInInspector] public WeaponTrigger currentWeapon; // 当前武器引用
    [HideInInspector] public float fireTimer = 0f; // 射击计时器
    [HideInInspector] public Vector3 aimDirection; // 瞄准方向
    [HideInInspector] public Vector3 worldMousePosition; // 世界鼠标位置
    [HideInInspector] public Camera playerCamera; // 玩家摄像机

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
    [FieldLabel("潜行颜色")] public Color crouchColor = Color.green; // 潜行时的颜色
    [FieldLabel("潜行透明度")] [Range(0, 1)] public float crouchAlpha = 0.5f; // 潜行时的透明度
    [HideInInspector] public bool isCrouching = false; // 潜行状态标志位
    [HideInInspector] public SpriteRenderer spriteRenderer; // 精灵渲染器
    [HideInInspector] public Color originalColor; // 原始颜色

    // --- 拾取 ---
    [Header("拾取参数")]
    [HideInInspector] public ItemBase currentPickedItem; // 当前已拾取的物品
    [HideInInspector] public ItemBase nearbyItem; // 附近可拾取的物品
    [HideInInspector] public bool isPickingUp = false; // 是否正在拾取

    // --- 视角参数 ---
    private float screenCenterX; // 屏幕中心X坐标

    // --- 内部属性组件 ---
    [HideInInspector] public Rigidbody2D PlayerRB2D;// 刚体
    [HideInInspector] public Animator AIMTOR;// 动画器
    [HideInInspector] public new Collider2D collider2D; // 碰撞器
    private IState currentState; // 当前状态
    // 字典
    private Dictionary<PlayerStateType, IState> states = new Dictionary<PlayerStateType, IState>();


    // 函数
    private void Awake()
    {
        playerInputController.EnabledGameplayInput(); // 启用玩家输入系统

        // 获取组件
        PlayerRB2D = GetComponent<Rigidbody2D>();
        AIMTOR = GetComponent<Animator>();
        collider2D = GetComponent<Collider2D>();
        Hand = transform.Find("Hand"); // 初始化手部位置
        playerCamera = Camera.main; // 获取主摄像机
        // 获取SpriteRenderer组件并保存原始颜色
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }

        // 初始化屏幕中心坐标
        screenCenterX = Screen.width / 2f;
        CurrentSpeed = WalkSpeed; // 设置初始速度为行走速度

        // 初始化生命值系统
        CurrentHealth = MaxHealth;
        CurrentHunger = MaxHunger;
        CurrentMental = MaxMental;

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
        var mousePos = Mouse.current.position.ReadValue();
        // 根据鼠标位置更新动画器方向参数（0为左，1为右）
        float horizontalDirection = (mousePos.x < screenCenterX) ? 0f : 1f;
        AIMTOR.SetFloat("Horizontial", horizontalDirection);
    }

    // 更新瞄准方向（移除层级和距离限制）
    public void UpdateAiming()
    {
        if (playerCamera == null) return;
        
        // 获取鼠标屏幕位置
        Vector3 mouseScreenPosition = Mouse.current.position.ReadValue();
        mouseScreenPosition.z = playerCamera.nearClipPlane;
        
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

    // 更新武器朝向（修改为仅处理武器翻转）
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
        // 清空控制台并显示当前状态
        Debug.ClearDeveloperConsole();
        Debug.Log($"当前状态机: {currentState.GetType().Name}");
        
        WeaponInHand();
        currentState.OnUpdate();
    }
    private void FixedUpdate()
    {
        currentState.OnFixedUpdate();
    }

    private void OnEnable() // 订阅事件
    {
        playerInputController.onMovement += Move;
        playerInputController.onFire += Fire;
        playerInputController.onDodge += Dodge;
        playerInputController.onPickup += PickUp;
        playerInputController.onRun += Run; // 修改为接收bool参数
        playerInputController.onCrouch += Crouch; // 修改为接收bool参数
        playerInputController.onReload += Reload;
    }
    private void OnDisable() // 取关事件
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
    // 获取武器
    public void WeaponInHand()
    {
        if(Hand.childCount > 0)
        {
            isWeaponInHand = true; // 武器已装备
            
            // 获取武器的WeaponTrigger组件
            Transform weaponTransform = Hand.GetChild(0);
            WeaponTrigger newWeapon = weaponTransform.GetComponent<WeaponTrigger>();
            
            // 如果武器发生变化，更新当前武器引用
            if (currentWeapon != newWeapon)
            {
                currentWeapon = newWeapon;
                Debug.Log($"切换武器: {weaponTransform.name}");
                
                // 确保新武器的发射口已正确设置
                if (currentWeapon != null && currentWeapon.Muzzle != null)
                {
                    Debug.Log($"武器发射口已识别: {currentWeapon.Muzzle.name}");
                }
                else
                {
                    Debug.LogWarning($"武器 {weaponTransform.name} 缺少发射口或WeaponTrigger组件！");
                }
            }
        }
        else
        {
            if (isWeaponInHand) // 只在状态改变时输出日志
            {
                Debug.Log("武器已卸下");
            }
            isWeaponInHand = false; // 武器未装备
            currentWeapon = null;
        }
    }

    public void Fire(bool isPressed)
    {
        isFiring = isPressed;
        
        // 将射击状态传递给当前武器
        if (currentWeapon != null)
        {
            // 确保武器有发射口
            if (currentWeapon.Muzzle != null)
            {
                currentWeapon.SetFiring(isFiring);
                
                if (isFiring)
                {
                    Debug.Log($"开始射击 - 使用武器: {currentWeapon.name}, 发射口: {currentWeapon.Muzzle.name}");
                }
            }
            else
            {
                Debug.LogWarning($"武器 {currentWeapon.name} 没有设置发射口，无法射击！");
                return;
            }
        }
        else if (isFiring)
        {
            Debug.LogWarning("没有装备武器，无法射击！");
            return;
        }
        
        // 如果开始射击且有武器，切换到攻击状态
        if (isFiring && isWeaponInHand && !isAttacking)
        {
            isAttacking = true;
            Debug.Log("开始射击，切换到攻击状态");
        }
        // 如果停止射击，标记攻击结束
        else if (!isFiring && isAttacking)
        {
            isAttacking = false;
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
        Debug.Log("换弹输入触发");
    }
    #endregion
    #region 潜行
    public void Crouch(bool isCrouchPressed)
    {
        isCrouching = isCrouchPressed;
        Debug.Log($"潜行状态: {(isCrouching ? "开启" : "关闭")}");
    }
    
    // 应用潜行视觉效果
    public void ApplyCrouchVisual()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = new Color(
                crouchColor.r,
                crouchColor.g,
                crouchColor.b,
                crouchAlpha
            );
        }
    }
    
    // 恢复原始视觉效果
    public void RestoreOriginalVisual()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }
    }
    
    // 外部访问潜行状态
    public bool IsCrouching()
    {
        return isCrouching;
    }
    #endregion
}
