using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class SceneEntrance : MonoBehaviour
{
    [Tooltip("需要切换到的目标场景名称")]
    public string NewSceneName;
    
    [Tooltip("是否只能使用一次")]
    public bool oneTimeUse = true;
    
    [Header("确认UI设置")]
    [Tooltip("确认提示文本组件")]
    public TextMeshProUGUI confirmText;
    
    [Tooltip("确认提示画布对象")]
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
        
        // 初始化文本透明度
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
            // 如果是一次性使用且已经触发过，则不再响应
            if (oneTimeUse && hasTriggered)
                return;
                
            // 检查场景名称是否有效
            if (string.IsNullOrEmpty(NewSceneName))
            {
                Debug.LogWarning("场景名称不能为空，无法切换场景");
                return;
            }
            
            // 如果已经在等待确认状态，则不重复处理
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
            // 玩家离开触发区域，停止等待确认
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
        
        // 设置文本内容并设置透明度为255
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
        
        // 设置文本透明度为0
        if (confirmText != null)
        {
            Color textColor = confirmText.color;
            textColor.a = 0f; // 设置透明度为0
            confirmText.color = textColor;
        }
        
        Debug.Log("停止等待场景切换确认");
    }
    
    private void OnOperatePressed(InputAction.CallbackContext context)
    {
        // 只有在等待确认状态下才响应F键
        if (!isWaitingForConfirm)
            return;
            
        Debug.Log("玩家按下F键，确认进行场景切换");
        
        // 标记为已触发
        hasTriggered = true;
        
        // 停止等待确认
        StopWaitingForConfirm();
        
        // 执行场景切换
        TransitionInternal();
    }

    // 执行场景切换的内部方法
    public void TransitionInternal()
    {
        if (SceneLoader.instance != null)
        {
            SceneLoader.instance.TransitionToScene(NewSceneName);
        }
        else
        {
            Debug.LogError("SceneLoader实例不存在，请确保场景中存在SceneLoader对象");
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
