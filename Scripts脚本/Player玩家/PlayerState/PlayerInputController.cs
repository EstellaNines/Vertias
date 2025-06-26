using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

[CreateAssetMenu(fileName = "PlayerInpurController")]
public class PlayerInputController : ScriptableObject, PlayerInputAction.IGamePlayActions
{
    // 事件
    public event UnityAction<Vector2> onMovement; // 移动事件
    public event UnityAction onFire; // 攻击事件
    public event UnityAction onDodge; // 闪避事件
    public event UnityAction onCrouch; //潜行事件
    public event UnityAction onReload; // 换弹事件
    public event UnityAction onPickup; // 拾取事件
    public event UnityAction onRun; // 跑动事件
    // 引用属性
    PlayerInputAction playerinputAction;


    // 函数
    private void OnEnable()
    {
        playerinputAction = new PlayerInputAction(); // 创建输入系统
        // playerinput继承了这个接入口，并传入this将playerinput注册为回调函数接收者
        playerinputAction.GamePlay.SetCallbacks(this);
    }

    public void EnabledGameplayInput()
    {
        playerinputAction.GamePlay.Enable(); // 启用gameplay输入
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

    public void OnFire(InputAction.CallbackContext context) // 射击
    {
        if (context.started) // 按下一刻
        {
            onFire?.Invoke();
        }
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
            onRun?.Invoke();
        }
    }
    public void OnCrouch(InputAction.CallbackContext context) // 潜行
    {
        if (context.started) // 按下一刻
        {
            onCrouch?.Invoke();
        }
    }

    // 不常用操作
    public void OnLook(InputAction.CallbackContext context) // 视角变化
    {

    }
    public void OnCrawl(InputAction.CallbackContext context) // 爬行
    {

    }

}
