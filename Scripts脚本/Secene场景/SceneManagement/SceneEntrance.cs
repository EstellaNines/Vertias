using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class SceneEntrance : MonoBehaviour
{
    [Tooltip("需要过渡到新场景的名称")]
    public string NewSceneName;
    
    [Tooltip("是否只能触发一次")]
    public bool oneTimeUse = true;
    
    [Header("确认UI设置")]
    [Tooltip("确认提示文本（可选）")]
    public TextMeshProUGUI confirmText;
    
    [Tooltip("确认提示画布（可选）")]
    public GameObject confirmCanvas;
    
    [Tooltip("提示文本内容")]
    public string promptMessage = "按F键进入场景";
    
    private bool hasTriggered = false;
    private bool isWaitingForConfirm = false;
    private PlayerInputAction inputActions;
    private Coroutine confirmCoroutine;

    void Awake()
    {
        // 初始化输入系统
        inputActions = new PlayerInputAction();
        
        // 初始化文本透明度为0
        if (confirmText != null)
        {
            Color textColor = confirmText.color;
            textColor.a = 0f; // 设置透明度为0
            confirmText.color = textColor;
        }
    }
    
    void OnEnable()
    {
        inputActions.Enable();
        inputActions.UI.Operate.performed += OnOperatePressed;
    }
    
    void OnDisable()
    {
        inputActions.UI.Operate.performed -= OnOperatePressed;
        inputActions.Disable();
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            // 检查是否已经触发过（如果设置为一次性使用）
            if (oneTimeUse && hasTriggered)
                return;
                
            // 检查场景名称是否有效
            if (string.IsNullOrEmpty(NewSceneName))
            {
                Debug.LogWarning("场景名称为空，无法切换场景！");
                return;
            }
            
            // 如果已经在等待确认，不重复触发
            if (isWaitingForConfirm)
                return;
            
            // 开始等待确认
            StartWaitingForConfirm();
        }
    }
    
    void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            // 玩家离开触发器，取消等待确认
            StopWaitingForConfirm();
        }
    }
    
    private void StartWaitingForConfirm()
    {
        isWaitingForConfirm = true;
        
        // 显示确认UI
        if (confirmCanvas != null)
        {
            confirmCanvas.SetActive(true);
        }
        
        // 设置文本内容并恢复透明度到255
        if (confirmText != null)
        {
            confirmText.text = promptMessage;
            Color textColor = confirmText.color;
            textColor.a = 1f; // 设置透明度为255 (1.0f)
            confirmText.color = textColor;
        }
        
        Debug.Log($"等待玩家按F键确认进入场景: {NewSceneName}");
    }
    
    private void StopWaitingForConfirm()
    {
        if (!isWaitingForConfirm)
            return;
            
        isWaitingForConfirm = false;
        
        // 隐藏确认UI
        if (confirmCanvas != null)
        {
            confirmCanvas.SetActive(false);
        }
        
        // 将文本透明度设置为0
        if (confirmText != null)
        {
            Color textColor = confirmText.color;
            textColor.a = 0f; // 设置透明度为0
            confirmText.color = textColor;
        }
        
        Debug.Log("取消场景切换确认");
    }
    
    private void OnOperatePressed(InputAction.CallbackContext context)
    {
        // 只有在等待确认状态下才响应F键
        if (!isWaitingForConfirm)
            return;
            
        Debug.Log("玩家按下F键，确认场景切换");
        
        // 标记为已触发
        hasTriggered = true;
        
        // 停止等待确认
        StopWaitingForConfirm();
        
        // 调用场景切换
        TransitionInternal();
    }

    // 调用场景切换函数
    public void TransitionInternal()
    {
        if (SceneLoader.instance != null)
        {
            SceneLoader.instance.TransitionToScene(NewSceneName);
        }
        else
        {
            Debug.LogError("SceneLoader实例不存在！请确保场景中有SceneLoader对象。");
        }
    }
    
    void OnDestroy()
    {
        if (inputActions != null)
        {
            inputActions.Dispose();
        }
    }
}
