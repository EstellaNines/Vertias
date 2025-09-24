using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using UnityEngine.UI;
using TMPro;

public class OutsideTransform : MonoBehaviour
{
    [Header("传送到内部设置")]
    [FieldLabel("内部玩家目标位置")] public Transform insideTargetPosition; // 内部玩家传送位置
    [FieldLabel("内部摄像机目标位置")] public Transform insideCameraTargetPosition; // 内部摄像机目标位置
    [FieldLabel("内部碰撞限制区")] public Collider2D insideConfinerCollider; // 内部碰撞限制区域

    [Header("通用设置")]
    [FieldLabel("虚拟摄像机")] public CinemachineVirtualCamera virtualCamera; // 虚拟摄像机对象
    [FieldLabel("玩家输入控制器")] public PlayerInputController playerInputController; // 玩家输入控制器

    [Header("镜头缩放设置")]
    [FieldLabel("传送后调整镜头?")] public bool adjustOrthoAfterTeleport = false; // 是否在传送后调整相机正交尺寸
    [FieldLabel("目标正交尺寸")] public float targetOrthoSize = 10f; // 传送后直接设定的 Ortho Size

    [Header("UI设置")]
    [FieldLabel("提示UI对象")] public GameObject promptUI; // 提示UI对象
    [FieldLabel("提示文本组件")] public TextMeshProUGUI promptText; // 提示文本组件（可选，如果UI中有文本）
    [FieldLabel("提示文本内容")] public string promptMessage = "按F键进入房间"; // 提示文本内容

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
            TeleportToInside(playerInTrigger);
        }
    }

    // 传送到内部的方法
    void TeleportToInside(GameObject player)
    {
        // 检查目标位置是否设置
        if (insideTargetPosition == null)
        {
            Debug.LogWarning("传送失败：内部目标位置未设置！");
            return;
        }

        // 隐藏UI（传送前）
        HidePromptUI();

        // 玩家传送逻辑
        player.transform.position = insideTargetPosition.position;

        // 摄像机同步逻辑
        if (virtualCamera != null && insideCameraTargetPosition != null)
        {
            // 获取虚拟摄像机的CinemachineConfiner2D组件
            CinemachineConfiner2D confiner = virtualCamera.GetComponent<CinemachineConfiner2D>();

            // 移动摄像机位置
            virtualCamera.transform.position = insideCameraTargetPosition.position;

            // 更新碰撞区域（直接设置Bounding Shape2D）
            if (confiner != null && insideConfinerCollider != null)
            {
                confiner.m_BoundingShape2D = insideConfinerCollider;
                Debug.Log("摄像机已同步至内部位置并更新碰撞区域");
            }

            // 传送后调整正交尺寸
            ApplyOrthoSizeAdjustment();
        }

        Debug.Log("玩家已从外部传送至内部");
    }

    // 调整虚拟摄像机正交尺寸
    private void ApplyOrthoSizeAdjustment()
    {
        if (virtualCamera == null || !adjustOrthoAfterTeleport)
        {
            return;
        }

        float current = virtualCamera.m_Lens.OrthographicSize;
        virtualCamera.m_Lens.OrthographicSize = targetOrthoSize;
        Debug.Log($"传送后已设置相机 Ortho Size: {current} -> {targetOrthoSize}");
    }

    // 显示提示UI
    void ShowPromptUI()
    {
        if (promptUI != null)
        {
            promptUI.SetActive(true);
            Debug.Log("显示进入房间提示UI");
        }
    }

    // 隐藏提示UI
    void HidePromptUI()
    {
        if (promptUI != null)
        {
            promptUI.SetActive(false);
            Debug.Log("隐藏进入房间提示UI");
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        // 仅检测玩家的 BoxCollider2D
        if (!(collision is BoxCollider2D))
        {
            return;
        }

        // 通过父级查找玩家组件，确保命中的是玩家的 BoxCollider2D
        Player playerComponent = collision.GetComponentInParent<Player>();
        if (playerComponent == null)
        {
            return;
        }

        playerInRange = true;
        playerInTrigger = playerComponent.gameObject;
        ShowPromptUI(); // 显示UI
        BindOperateEvent(); // 绑定F键事件
        Debug.Log("玩家进入外部传送区域，按F键进入内部（BoxCollider2D）");
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        // 仅处理玩家的 BoxCollider2D 离开
        if (!(collision is BoxCollider2D))
        {
            return;
        }

        Player playerComponent = collision.GetComponentInParent<Player>();
        if (playerComponent == null)
        {
            return;
        }

        // 仅当当前记录的玩家离开时才重置
        if (playerInTrigger != null && playerInTrigger == playerComponent.gameObject)
        {
            playerInRange = false;
            playerInTrigger = null;
            HidePromptUI(); // 隐藏UI
            UnbindOperateEvent(); // 解绑F键事件
            Debug.Log("玩家离开外部传送区域（BoxCollider2D）");
        }
    }

    // 绑定F键（Operate）事件
    private void BindOperateEvent()
    {
        if (playerInputController != null && !isOperateEventBound)
        {
            playerInputController.onOperate += OnOperatePressed;
            isOperateEventBound = true;
            Debug.Log("已绑定F键（Operate）事件到外部传送触发器");
        }
    }

    // 解绑F键（Operate）事件
    private void UnbindOperateEvent()
    {
        if (playerInputController != null && isOperateEventBound)
        {
            playerInputController.onOperate -= OnOperatePressed;
            isOperateEventBound = false;
            Debug.Log("已解绑F键（Operate）事件从外部传送触发器");
        }
    }
}

