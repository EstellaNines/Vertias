using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("移动速度")]
    public float WalkSpeed;// 自定义移动速度
    public float RunSpeed;
    private PlayerInputAction PinputAction;// 输入系统
    [Header("方向值视窗")]
    public Vector2 InputDirection;// 自定义输入方向值(-1/1)
    private Rigidbody2D PlayerRB2D;// 刚体
    private Animator AIMTOR;// 动画器
    private float CurrentSpeed;
    private float screenCenterX;
    private bool isFiring = false; // 射击状态变量

    [Header("闪避")]
    public bool isDodged = false;// 闪避
    public float DdogeForce = 0f;
    public float DodgeDuration = 0f;
    public float DodgeTimer = 0f;
    public float DodgeCooldown = 0f;
    private bool isDodgeCoolDownEnd = false;

    void Awake()
    {
        // 获取组件
        PinputAction = new PlayerInputAction();
        PlayerRB2D = GetComponent<Rigidbody2D>();
        AIMTOR = GetComponent<Animator>();
        CurrentSpeed = WalkSpeed;
        screenCenterX = Screen.width / 2f;


        // 闪避
        PinputAction.GamePlay.Dodge.performed += isDodging;
    }



    // 启动输入系统
    void OnEnable()
    {
        PinputAction.Enable();
        PinputAction.GamePlay.Run.performed += OnRunPressed;
        PinputAction.GamePlay.Run.canceled += OnRunReleased;
    }
    // 关闭输入系统
    void OnDisable()
    {
        PinputAction.Disable();
        PinputAction.GamePlay.Run.performed -= OnRunPressed;
        PinputAction.GamePlay.Run.canceled -= OnRunReleased;
    }
    private void OnRunPressed(InputAction.CallbackContext callbackContext)
    {
        CurrentSpeed = RunSpeed;
    }
    private void OnRunReleased(InputAction.CallbackContext callbackContext)
    {
        CurrentSpeed = WalkSpeed;
    }
    // 外部调用以设置射击状态
    public void SetIsFiring(bool isFiring)
    {
        this.isFiring = isFiring;
    }

    // 外部查询是否正在闪避
    public bool IsDodging()
    {
        return isDodged;
    }

    void Dodge()
    {
        if (isDodgeCoolDownEnd)
        {
            DodgeTimer += Time.fixedDeltaTime;
            if (DodgeTimer >= DodgeDuration)
            {
                isDodgeCoolDownEnd = false;
                DodgeTimer = 0f;

            }
        }
        if (isDodged)
        {
            if (!isDodgeCoolDownEnd)
            {
                if (DodgeTimer <= DodgeDuration)
                {
                    PlayerRB2D.AddForce(InputDirection * DdogeForce, ForceMode2D.Impulse);
                    DodgeTimer += Time.fixedDeltaTime;
                }
                else
                {
                    isDodged = false;
                    isDodgeCoolDownEnd = true;
                    DodgeTimer = 0;

                }
            }
        }
    }
    private void isDodging(InputAction.CallbackContext context)
    {
        if (!isFiring && !isDodgeCoolDownEnd) // 仅当未射击时允许闪避
        {
            isDodged = true;
        }
    }
    private void Update()
    {
        // 读取输入系统中Move的参数(-1/1)
        InputDirection = PinputAction.GamePlay.Move.ReadValue<Vector2>();
        // 打印参数
        // Debug.Log(InputDirection);
        var mousePos = Mouse.current.position.ReadValue();
        // 计算方向并更新动画器参数
        if (mousePos.x < screenCenterX)
        {
            AIMTOR.SetFloat("Horizontial", -1f); // 鼠标在左侧，向左看
        }
        else
        {
            AIMTOR.SetFloat("Horizontial", 1f);  // 鼠标在右侧，向右看
        }
    }
    void FixedUpdate()
    {
        // 速度 = 输入系统参数 * 移动速度
        PlayerRB2D.velocity = InputDirection * CurrentSpeed;
        float Newnumber = InputDirection.sqrMagnitude * CurrentSpeed;
        Dodge();

        // 动画器赋值
        AIMTOR.SetFloat("Horizontial", InputDirection.x);// Horizontial赋值输入系统方向的X轴
        AIMTOR.SetFloat("Speed", Newnumber);// Speed赋值输入系统向量长度(-1/1)*3/5

        // 闪避

        AIMTOR.SetBool("isDodging?", isDodged);
    }

}
