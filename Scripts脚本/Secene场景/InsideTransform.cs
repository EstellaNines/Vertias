using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using UnityEngine.UI;
using TMPro;

public class InsideTransform : MonoBehaviour
{
    [Header("传送到外部设置")]
    [FieldLabel("外部玩家目标位置")] public Transform outsideTargetPosition; // 外部玩家传送位置
    [FieldLabel("外部摄像机目标位置")] public Transform outsideCameraTargetPosition; // 外部摄像机目标位置
    [FieldLabel("外部碰撞限制区")] public Collider2D outsideConfinerCollider; // 外部碰撞限制区域
    
    [Header("通用设置")]
    [FieldLabel("虚拟摄像机")] public CinemachineVirtualCamera virtualCamera; // 虚拟摄像机对象
    [FieldLabel("玩家输入控制器")] public PlayerInputController playerInputController; // 玩家输入控制器
    
    [Header("UI设置")]
    [FieldLabel("提示UI对象")] public GameObject promptUI; // 提示UI对象
    [FieldLabel("提示文本组件")] public TextMeshProUGUI promptText; // 提示文本组件（可选，如果UI中有文本）
    [FieldLabel("提示文本内容")] public string promptMessage = "按F键离开房间"; // 提示文本内容
    
    private bool playerInRange = false; // 玩家是否在范围内
    private GameObject playerInTrigger; // 在触发器内的玩家对象
    private bool isOperateEventBound = false; // 记录是否已绑定Operate事件

    void Start()
    {
        // 如果没有手动分配输入控制器，尝试从玩家对象获取
        if (playerInputController == null)
        {
            Player player = FindObjectOfType<Player>();
            if (player != null)
            {
                playerInputController = player.playerInputController;
            }
        }
        
        // 启用UI输入（但不在这里绑定F键事件，而是在触发器范围内绑定）
        if (playerInputController != null)
        {
            playerInputController.EnabledUIInput(); // 启用UI输入
        }
        
        // 初始化时隐藏UI
        if (promptUI != null)
        {
            promptUI.SetActive(false);
        }
        
        // 设置提示文本内容
        if (promptText != null)
        {
            promptText.text = promptMessage;
        }
    }
    
    void OnDestroy()
    {
        // 取消订阅事件
        UnbindOperateEvent();
    }
    
    // F键按下时的处理
    void OnOperatePressed()
    {
        if (playerInRange && playerInTrigger != null)
        {
            TeleportToOutside(playerInTrigger);
        }
    }
    
    // 传送到外部的方法
    void TeleportToOutside(GameObject player)
    {
        // 检查目标位置是否设置
        if (outsideTargetPosition == null)
        {
            Debug.LogWarning("传送失败：外部目标位置未设置！");
            return;
        }
        
        // 隐藏UI（传送前）
        HidePromptUI();
        
        // 玩家传送逻辑
        player.transform.position = outsideTargetPosition.position;

        // 摄像机同步逻辑
        if (virtualCamera != null && outsideCameraTargetPosition != null)
        {
            // 获取虚拟摄像机的CinemachineConfiner2D组件
            CinemachineConfiner2D confiner = virtualCamera.GetComponent<CinemachineConfiner2D>();

            // 移动摄像机位置
            virtualCamera.transform.position = outsideCameraTargetPosition.position;

            // 更新碰撞区域（直接设置Bounding Shape2D）
            if (confiner != null && outsideConfinerCollider != null)
            {
                confiner.m_BoundingShape2D = outsideConfinerCollider;
                Debug.Log("摄像机已同步至外部位置并更新碰撞区域");
            }
        }

        Debug.Log("玩家已从内部传送至外部");
    }
    
    // 显示提示UI
    void ShowPromptUI()
    {
        if (promptUI != null)
        {
            promptUI.SetActive(true);
            Debug.Log("显示离开房间提示UI");
        }
    }
    
    // 隐藏提示UI
    void HidePromptUI()
    {
        if (promptUI != null)
        {
            promptUI.SetActive(false);
            Debug.Log("隐藏离开房间提示UI");
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        // 检查碰撞对象是否带有"Player"标签
        if (collision.CompareTag("Player"))
        {
            playerInRange = true;
            playerInTrigger = collision.gameObject;
            ShowPromptUI(); // 显示UI
            BindOperateEvent(); // 绑定F键事件
            Debug.Log("玩家进入内部传送区域，按F键返回外部");
        }
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            playerInRange = false;
            playerInTrigger = null;
            HidePromptUI(); // 隐藏UI
            UnbindOperateEvent(); // 解绑F键事件
            Debug.Log("玩家离开内部传送区域");
        }
    }

    // 绑定F键（Operate）事件
    private void BindOperateEvent()
    {
        if (playerInputController != null && !isOperateEventBound)
        {
            playerInputController.onOperate += OnOperatePressed;
            isOperateEventBound = true;
            Debug.Log("已绑定F键（Operate）事件到内部传送触发器");
        }
    }

    // 解绑F键（Operate）事件
    private void UnbindOperateEvent()
    {
        if (playerInputController != null && isOperateEventBound)
        {
            playerInputController.onOperate -= OnOperatePressed;
            isOperateEventBound = false;
            Debug.Log("已解绑F键（Operate）事件从内部传送触发器");
        }
    }
}
