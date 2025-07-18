using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

[CreateAssetMenu(fileName = "PlayerInputController")]
public class PlayerInputController : ScriptableObject, PlayerInputAction.IGamePlayActions, PlayerInputAction.IUIActions
{
    // 输入动作系统
    private PlayerInputAction playerInputAction;

    // 游戏玩法事件
    [Header("游戏玩法事件")]
    public UnityAction<Vector2> onMovement; // 移动事件
    public UnityAction<Vector2> onLook; // 视角事件
    public UnityAction<bool> onFire; // 统一的开火事件（true=按下，false=释放）
    public UnityAction onDodge; // 闪避事件
    public UnityAction onReload; // 重新装弹事件
    public UnityAction<bool> onRun; // 奔跑事件
    public UnityAction<bool> onCrouch; // 蹲下事件
    public UnityAction onCrawl; // 爬行事件
    public UnityAction onPickup; // 拾取事件

    // UI相关事件
    [Header("UI相关事件")]
    public UnityAction onBackPack; // 背包事件
    public UnityAction onOperate; // 操作事件
    public UnityAction onWeaponInspection; // 武器检查事件
    public UnityAction onEscape; // 退出事件

    // 状态变量
    [Header("状态变量")]
    public bool isRunPressed = false; // 是否按下奔跑
    public bool isCrouchPressed = false; // 是否按下蹲下
    public bool isFirePressed = false; // 是否按下开火

    private void OnEnable()
    {
        if (playerInputAction == null)
        {
            playerInputAction = new PlayerInputAction();
        }

        // 设置回调
        playerInputAction.GamePlay.SetCallbacks(this);
        playerInputAction.UI.SetCallbacks(this);
    }

    private void OnDisable()
    {
        DisableAllInput();
    }

    // 启用游戏玩法输入
    public void EnabledGameplayInput()
    {
        playerInputAction.GamePlay.Enable();
        Debug.Log("游戏玩法输入已启用");
    }

    // 禁用游戏玩法输入
    public void DisableGameplayInput()
    {
        playerInputAction.GamePlay.Disable();
        Debug.Log("游戏玩法输入已禁用");
    }

    // 启用UI输入
    public void EnabledUIInput()
    {
        playerInputAction.UI.Enable();
        Debug.Log("UI输入已启用");
    }

    // 禁用UI输入
    public void DisableUIInput()
    {
        playerInputAction.UI.Disable();
        Debug.Log("UI输入已禁用");
    }

    // 禁用所有输入
    public void DisableAllInput()
    {
        playerInputAction?.GamePlay.Disable();
        playerInputAction?.UI.Disable();
        Debug.Log("所有输入已禁用");
    }

    // 启用所有输入
    public void EnableAllInput()
    {
        EnabledGameplayInput();
        EnabledUIInput();
        Debug.Log("所有输入已启用");
    }

    // === 游戏玩法接口实现 ===

    // 移动输入
    public void OnMove(InputAction.CallbackContext context)
    {
        Vector2 inputVector = context.ReadValue<Vector2>();
        onMovement?.Invoke(inputVector);
    }

    // 视角输入
    public void OnLook(InputAction.CallbackContext context)
    {
        Vector2 lookVector = context.ReadValue<Vector2>();
        onLook?.Invoke(lookVector);
    }

    // 统一的开火输入处理
    public void OnFire(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            // 按键开始按下
            isFirePressed = true;
            onFire?.Invoke(true);
            Debug.Log("开火按键按下");
        }
        else if (context.canceled)
        {
            // 按键释放
            isFirePressed = false;
            onFire?.Invoke(false);
            Debug.Log("开火按键释放");
        }
    }

    // 闪避输入
    public void OnDodge(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            onDodge?.Invoke();
            Debug.Log("闪避触发");
        }
    }

    // 重新装弹输入
    public void OnReload(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            onReload?.Invoke();
            Debug.Log("重新装弹触发");
        }
    }

    // 奔跑输入
    public void OnRun(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            isRunPressed = true;
            onRun?.Invoke(true);
            Debug.Log("奔跑按下");
        }
        else if (context.canceled)
        {
            isRunPressed = false;
            onRun?.Invoke(false);
            Debug.Log("奔跑释放");
        }
    }

    // 蹲下输入
    public void OnCrouch(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            isCrouchPressed = !isCrouchPressed; // 切换状态
            onCrouch?.Invoke(isCrouchPressed);
            Debug.Log($"蹲下状态: {isCrouchPressed}");
        }
    }

    // 爬行输入
    public void OnCrawl(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            onCrawl?.Invoke();
            Debug.Log("爬行触发");
        }
    }

    // 拾取输入
    public void OnPickUp(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            onPickup?.Invoke();
            Debug.Log("拾取触发");
        }
    }

    // === UI接口实现 ===

    // 操作输入
    public void OnOperate(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            onOperate?.Invoke();
            Debug.Log("操作触发");
        }
    }

    // 武器检查输入
    public void OnWeaponInspection(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            onWeaponInspection?.Invoke();
            Debug.Log("武器检查触发");
        }
    }

    // 背包输入
    public void OnBackPack(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            onBackPack?.Invoke();
            Debug.Log("背包触发");
        }
    }

    // === 辅助方法 ===

    // 获取移动输入值
    public Vector2 GetMovementInput()
    {
        if (playerInputAction != null && playerInputAction.GamePlay.enabled)
        {
            return playerInputAction.GamePlay.Move.ReadValue<Vector2>();
        }
        return Vector2.zero;
    }

    // 检查是否有移动输入
    public bool HasMovementInput()
    {
        Vector2 movement = GetMovementInput();
        return movement.magnitude > 0.1f;
    }

    // 重置输入状态
    public void ResetInputStates()
    {
        isRunPressed = false;
        isCrouchPressed = false;
        isFirePressed = false;
        Debug.Log("输入状态已重置");
    }

    // 获取输入动作
    public PlayerInputAction GetInputAction()
    {
        return playerInputAction;
    }

    // 组件销毁
    private void OnDestroy()
    {
        DisableAllInput();
        playerInputAction?.Dispose();
    }
}