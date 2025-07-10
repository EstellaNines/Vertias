using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

[CreateAssetMenu(fileName = "PlayerInpurController")]
public class PlayerInputController : ScriptableObject, PlayerInputAction.IGamePlayActions, PlayerInputAction.IUIActions
{
    // 事件
    public event UnityAction<Vector2> onMovement; // 移动事件
    public event UnityAction<bool> onFire; // 射击事件
    public event UnityAction onDodge; // 闪避事件
    public event UnityAction<bool> onCrouch; // 潜行事件 - 修改为传递bool参数
    public event UnityAction onReload; // 换弹事件
    public event UnityAction onPickup; // 拾取事件
    public event UnityAction<bool> onRun; // 跑动事件
    public event UnityAction onOperate; // UI操作事件
    public event UnityAction onWeaponInspection; // 武器检视事件
    
    // 引用属性
    PlayerInputAction playerinputAction;

    // 函数
    private void OnEnable()
    {
        playerinputAction = new PlayerInputAction(); // 创建输入系统
        // playerinput继承了这个接入口，并传入this将playerinput注册为回调函数接收者
        playerinputAction.GamePlay.SetCallbacks(this);
        playerinputAction.UI.SetCallbacks(this); // 新增UI回调
    }

    public void EnabledGameplayInput()
    {
        playerinputAction.GamePlay.Enable(); // 启用gameplay输入
    }
    
    public void EnabledUIInput()
    {
        playerinputAction.UI.Enable(); // 启用UI输入
    }
    
    // 添加禁用输入的方法
    public void DisableGameplayInput()
    {
        playerinputAction.GamePlay.Disable(); // 禁用gameplay输入
    }
    
    public void DisableUIInput()
    {
        playerinputAction.UI.Disable(); // 禁用UI输入
    }

    // 接口
    public void OnMove(InputAction.CallbackContext context) // 移动
    {
        if (context.performed) // 持续按下
        {
            onMovement?.Invoke(context.ReadValue<Vector2>());
        }
        else if (context.canceled) // 松开按键
        {
            onMovement?.Invoke(Vector2.zero); // 触发零向量移动
        }
    }

    public void OnFire(InputAction.CallbackContext context)
    {
        bool isPressed = context.started || context.performed;
        onFire?.Invoke(isPressed);
    }
    
    public void OnPickUp(InputAction.CallbackContext context) // 拾取
    {
        if (context.started) // 按下一刻
        {
            onPickup?.Invoke();
        }
    }
    public void OnDodge(InputAction.CallbackContext context) // 闪避
    {
        if (context.started) // 按下一刻
        {
            onDodge?.Invoke();
        }
    }
    public void OnReload(InputAction.CallbackContext context) // 装弹
    {
        if (context.started) // 按下一刻
        {
            onReload?.Invoke();
        }
    }

    public void OnRun(InputAction.CallbackContext context) // 跑动
    {
        if (context.started) // 按下一刻
        {
            onRun?.Invoke(true); // 开始跑步
        }
        else if (context.canceled) // 松开按键
        {
            onRun?.Invoke(false); // 停止跑步
        }
    }
    public void OnCrouch(InputAction.CallbackContext context) // 潜行
    {
        if (context.started) // 按下一刻
        {
            onCrouch?.Invoke(true); // 开始潜行
        }
        else if (context.canceled) // 松开按键
        {
            onCrouch?.Invoke(false); // 停止潜行
        }
    }

    // 不常用操作
    public void OnLook(InputAction.CallbackContext context) // 视角变化
    {

    }
    public void OnCrawl(InputAction.CallbackContext context) // 爬行
    {

    }
    
    // UI操作接口实现
    public void OnOperate(InputAction.CallbackContext context) // UI操作（F键）
    {
        if (context.started) // 按下一刻
        {
            onOperate?.Invoke();
        }
    }

    public void OnWeaponInspection(InputAction.CallbackContext context) // 武器检视（H键）
    {
        if (context.started) // 按下一刻
        {
            onWeaponInspection?.Invoke();
        }
    }
    
    // OnNewaction方法实现
    public void OnNewaction(InputAction.CallbackContext context) // 新动作
    {
        if (context.started) // 按下一刻
        {
            // 这里可以添加新动作的逻辑，目前留空
            // 如果需要事件，可以添加相应的UnityAction事件
        }
    }
}
