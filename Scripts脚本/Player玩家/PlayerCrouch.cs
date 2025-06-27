// ... existing code ...
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCrouch : MonoBehaviour
{
    [Header("潜行设置")]
    [FieldLabel("潜行标志位")] public bool crouching; // 潜行标志位,用于外部检测玩家是否处于潜行状态 
    [Tooltip("潜行时应用的颜色")]
    public Color crouchColor = Color.green;

    [Tooltip("潜行时的透明度 (0-1)")]
    [Range(0, 1)]
    public float crouchAlpha = 0.5f;

    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private PlayerInputAction inputActions;

    void Awake()
    {
        // 获取SpriteRenderer组件
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogError("未找到SpriteRenderer组件");
            enabled = false;
            return;
        }

        // 保存原始颜色
        originalColor = spriteRenderer.color;

        // 初始化输入系统
        inputActions = new PlayerInputAction();
    }

    private void Update()
    {
        if (crouching)
        {
            // 潜行状态下的特殊逻辑
            Debug.Log("当前处于潜行状态");
        }
    }

    void OnEnable()
    {
        // 启用输入动作集
        inputActions.GamePlay.Enable();

        // 订阅Crouch动作事件
        inputActions.GamePlay.Crouch.started += OnCrouchStarted;
        inputActions.GamePlay.Crouch.canceled += OnCrouchCanceled;
    }

    void OnDisable()
    {
        // 取消订阅并清理
        inputActions.GamePlay.Crouch.started -= OnCrouchStarted;
        inputActions.GamePlay.Crouch.canceled -= OnCrouchCanceled;
        inputActions.GamePlay.Disable();
        inputActions.Dispose();
    }

    // Crouch动作开始时调用
    private void OnCrouchStarted(InputAction.CallbackContext context)
    {
        // 应用潜行颜色（保持RGB，调整透明度）
        spriteRenderer.color = new Color(
            crouchColor.r,
            crouchColor.g,
            crouchColor.b,
            crouchAlpha
        );

        // 设置潜行状态标志位
        crouching = true;
    }

    // Crouch动作结束时调用
    private void OnCrouchCanceled(InputAction.CallbackContext context)
    {
        // 恢复原始颜色
        spriteRenderer.color = originalColor;
        // 清除潜行状态标志位
        crouching = false;
    }

    public bool IsCrouching() // 外部访问
    {
        return crouching;
    }
}