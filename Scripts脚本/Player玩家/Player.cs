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
    // --- 输入系统 ---
    [Header("获取玩家输入系统")]
    [FieldLabel("输入系统获取")] public PlayerInputController playerInputController; // 获取玩家输入系统
    [HideInInspector] public Vector2 InputDirection = Vector2.zero; // 记录输入方向

    // --- 移动 ---
    [Header("移动参数")]
    [FieldLabel("行走速度")] public float WalkSpeed = 3f; // 移动速度
    [HideInInspector] public float CurrentSpeed; // 当前移动速度


    // --- 攻击 ---
    [Header("攻击参数")]
    [HideInInspector] public bool isFiring = false; // 射击状态变量
    [FieldLabel("攻击时移动速度")] public float FireSpeed = 1f; // 攻击时移动速度

    // --- 拾取 ---
    // [Header("拾取参数")]
    // [FieldLabel("手部")] public Transform handTransform; // 手部位置的Transform
    // [HideInInspector] public ItemBase currentPickedItem; // 当前已拾取的物品
    // [HideInInspector] public ItemBase nearbyItem; // 附近可拾取的物品

    // --- 闪避 ---
    [Header("闪避参数")]
    [HideInInspector] public bool isDodged = false;// 闪避
    [FieldLabel("翻滚推力")] public float DdogeForce = 5f;
    [FieldLabel("翻滚持续时间")] public float DodgeDuration = 0.3f;
    [HideInInspector] public float DodgeTimer = 0f;
    [FieldLabel("翻滚冷却时间")] public float DodgeCooldown = 1f;
    [HideInInspector] public bool isDodgeCoolDownEnd = true;

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

        // 初始化屏幕中心坐标
        screenCenterX = Screen.width / 2f;
        CurrentSpeed = WalkSpeed; // 设置初始速度为行走速度

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
        playerInputController.onRun += Run;
        playerInputController.onCrouch += Crouch;
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
    public void Fire()
    {
        Debug.Log("射击输入触发");
    }
    #endregion
    #region 闪避
    // 闪避动作
    public void Dodge()
    {
        if (!isDodged && !isDodgeCoolDownEnd)
        {
            isDodged = true; // 开始闪避
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
        isDodgeCoolDownEnd = false;
        Debug.Log("闪避冷却结束");
    }
    #endregion
    #region 拾取
    public void PickUp()
    {
        Debug.Log("拾取输入触发");
    }
    #endregion
    #region 奔跑
    public void Run()
    {
        Debug.Log("奔跑输入触发");
    }
    #endregion
    #region 换弹
    public void Reload()
    {
        Debug.Log("换弹输入触发");
    }
    #endregion
    #region 潜行
    public void Crouch()
    {
        Debug.Log("潜行输入触发");
    }
    #endregion
}
